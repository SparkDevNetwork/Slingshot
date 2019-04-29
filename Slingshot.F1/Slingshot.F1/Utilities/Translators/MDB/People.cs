using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using OrcaMDF.Core.MetaData;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace Slingshot.F1.Utilities.Translators
{
    public class People : F1Component, IFellowshipOne
    {
        /// <summary>
        /// Translates the specified table data.
        /// </summary>
        /// <param name="tableData">The table data.</param>
        public void Translate( IQueryable<Row> tableData )
        {
        }
    }

    public static class F1Person
    {
        /// <summary>
        /// Translates the company.
        /// </summary>
        /// <param name="tableData">The table data.</param>
        /// <param name="totalRows">The total rows.</param>
        private void TranslateCompany( IQueryable<Row> tableData, long totalRows = 0 )
        {
            var lookupContext = new RockContext();
            var businessList = new List<Group>();

            var importedCompanyCount = new PersonService( lookupContext ).Queryable().Count( p => p.ForeignId != null && p.RecordTypeValueId == BusinessRecordTypeId );

            if ( totalRows == 0 )
            {
                totalRows = tableData.Count();
            }

            var completedItems = importedCompanyCount;
            var percentage = ( totalRows - 1 ) / 100 + 1;
            ReportProgress( 0, $"Verifying company import ({totalRows:N0} found, {importedCompanyCount:N0} already exist)." );

            foreach ( var row in tableData.Where( r => r != null ) )
            {
                var householdId = row["Household_ID"] as int?;
                if ( GetPersonKeys( null, householdId ) == null )
                {
                    var businessGroup = new Group();
                    var businessPerson = new Person
                    {
                        CreatedByPersonAliasId = ImportPersonAliasId,
                        CreatedDateTime = row["Created_Date"] as DateTime?,
                        ModifiedDateTime = row["Last_Updated_Date"] as DateTime?,
                        RecordTypeValueId = BusinessRecordTypeId,
                        RecordStatusValueId = ActivePersonRecordStatusId
                    };

                    var businessName = row["Household_Name"] as string;
                    if ( !string.IsNullOrWhiteSpace( businessName ) )
                    {
                        businessName = businessName.Replace( "&#39;", "'" );
                        businessName = businessName.Replace( "&amp;", "&" );
                        businessPerson.LastName = businessName.Left( 50 );
                        businessGroup.Name = businessName.Left( 50 );
                    }

                    businessPerson.Attributes = new Dictionary<string, AttributeCache>();
                    businessPerson.AttributeValues = new Dictionary<string, AttributeValueCache>();
                    AddEntityAttributeValue( lookupContext, HouseholdIdAttribute, businessPerson, householdId.ToString() );

                    var groupMember = new GroupMember
                    {
                        Person = businessPerson,
                        GroupRoleId = FamilyAdultRoleId,
                        GroupMemberStatus = GroupMemberStatus.Active
                    };
                    businessGroup.Members.Add( groupMember );
                    businessGroup.GroupTypeId = FamilyGroupTypeId;
                    businessGroup.ForeignKey = householdId.ToString();
                    businessGroup.ForeignId = householdId;
                    businessList.Add( businessGroup );

                    completedItems++;
                    if ( completedItems % percentage < 1 )
                    {
                        var percentComplete = completedItems / percentage;
                        ReportProgress( percentComplete, $"{completedItems - importedCompanyCount:N0} companies imported ({percentComplete}% complete)." );
                    }
                    else if ( completedItems % ReportingNumber < 1 )
                    {
                        SaveCompanies( businessList );
                        ReportPartialProgress();
                        businessList.Clear();
                    }
                }
            }

            if ( businessList.Any() )
            {
                SaveCompanies( businessList );
            }

            ReportProgress( 100, $"Finished company import: {completedItems - importedCompanyCount:N0} companies imported." );
        }

        /// <summary>
        /// Saves the companies.
        /// </summary>
        /// <param name="businessList">The business list.</param>
        private static void SaveCompanies( List<Group> businessList )
        {
            var rockContext = new RockContext();
            // using wrap transaction bc lot of different saves happening
            rockContext.WrapTransaction( () =>
            {
                rockContext.Configuration.AutoDetectChangesEnabled = false;
                rockContext.Groups.AddRange( businessList );
                rockContext.SaveChanges( DisableAuditing );

                foreach ( var newBusiness in businessList )
                {
                    foreach ( var groupMember in newBusiness.Members )
                    {
                        // don't call LoadAttributes, it only rewrites existing cache objects
                        // groupMember.Person.LoadAttributes( rockContext );

                        foreach ( var attributeCache in groupMember.Person.Attributes.Select( a => a.Value ) )
                        {
                            var existingValue = rockContext.AttributeValues.FirstOrDefault( v => v.Attribute.Key == attributeCache.Key && v.EntityId == groupMember.Person.Id );
                            var newAttributeValue = groupMember.Person.AttributeValues[attributeCache.Key];

                            // set the new value and add it to the database
                            if ( existingValue == null )
                            {
                                existingValue = new AttributeValue
                                {
                                    AttributeId = newAttributeValue.AttributeId,
                                    EntityId = groupMember.Person.Id,
                                    Value = newAttributeValue.Value
                                };

                                rockContext.AttributeValues.Add( existingValue );
                            }
                            else
                            {
                                existingValue.Value = newAttributeValue.Value;
                                rockContext.Entry( existingValue ).State = EntityState.Modified;
                            }
                        }

                        if ( !groupMember.Person.Aliases.Any( a => a.AliasPersonId == groupMember.Person.Id ) )
                        {
                            groupMember.Person.Aliases.Add( new PersonAlias { AliasPersonId = groupMember.Person.Id, AliasPersonGuid = groupMember.Person.Guid } );
                        }

                        groupMember.Person.GivingGroupId = newBusiness.Id;
                    }
                }

                rockContext.ChangeTracker.DetectChanges();
                rockContext.SaveChanges( DisableAuditing );

                if ( businessList.Any() )
                {
                    var groupMembers = businessList.SelectMany( gm => gm.Members );
                    ImportedPeople.AddRange( groupMembers.Select( m => new PersonKeys
                    {
                        PersonAliasId = (int)m.Person.PrimaryAliasId,
                        PersonId = m.Person.Id,
                        PersonForeignId = null,
                        GroupForeignId = m.Group.ForeignId,
                        FamilyRoleId = FamilyRole.Adult
                    } ).ToList()
                    );
                }
            } );
        }

        /// <summary>
        /// Translates the person.
        /// </summary>
        /// <param name="tableData">The table data.</param>
        /// <param name="totalRows">The total rows.</param>
        public void TranslatePerson( IQueryable<Row> tableData, long totalRows = 0 )
        {
            var lookupContext = new RockContext();

            // Marital statuses: Married, Single, Separated, etc
            var maritalStatusTypes = DefinedTypeCache.Get( new Guid( Rock.SystemGuid.DefinedType.PERSON_MARITAL_STATUS ), lookupContext ).DefinedValues;

            // Connection statuses: Member, Visitor, Attendee, etc
            var connectionStatusTypes = DefinedTypeCache.Get( new Guid( Rock.SystemGuid.DefinedType.PERSON_CONNECTION_STATUS ), lookupContext ).DefinedValues;

            // Title type: Mr., Mrs., Dr., etc
            var titleTypes = DefinedTypeCache.Get( new Guid( Rock.SystemGuid.DefinedType.PERSON_TITLE ), lookupContext ).DefinedValues;

            // Suffix type: Sr., Jr., II, etc
            var suffixTypes = DefinedTypeCache.Get( new Guid( Rock.SystemGuid.DefinedType.PERSON_SUFFIX ), lookupContext ).DefinedValues;

            // Look up additional Person attributes (existing)
            var personAttributes = new AttributeService( lookupContext ).GetByEntityTypeId( PersonEntityTypeId ).AsNoTracking().ToList();

            // F1 attributes: IndividualId, HouseholdId
            // Core attributes: PreviousChurch, Membership Date, First Visit, Allergy, Employer, Position, School
            var previousChurchAttribute = personAttributes.FirstOrDefault( a => a.Key.Equals( "PreviousChurch", StringComparison.InvariantCultureIgnoreCase ) );
            var membershipDateAttribute = personAttributes.FirstOrDefault( a => a.Key.Equals( "MembershipDate", StringComparison.InvariantCultureIgnoreCase ) );
            var firstVisitAttribute = personAttributes.FirstOrDefault( a => a.Key.Equals( "FirstVisit", StringComparison.InvariantCultureIgnoreCase ) );
            var allergyNoteAttribute = personAttributes.FirstOrDefault( a => a.Key.Equals( "Allergy", StringComparison.InvariantCultureIgnoreCase ) );
            var employerAttribute = personAttributes.FirstOrDefault( a => a.Key.Equals( "Employer", StringComparison.InvariantCultureIgnoreCase ) );
            var positionAttribute = personAttributes.FirstOrDefault( a => a.Key.Equals( "Position", StringComparison.InvariantCultureIgnoreCase ) );
            var schoolAttribute = personAttributes.FirstOrDefault( a => a.Key.Equals( "School", StringComparison.InvariantCultureIgnoreCase ) );
            var barcodeAttribute = AddEntityAttribute( lookupContext, PersonEntityTypeId, string.Empty, string.Empty, string.Empty, string.Empty,
                "Personal Barcode", "PersonalBarcode", TextFieldTypeId, true );

            var familyList = new List<Group>();
            var visitorList = new List<Group>();
            var previousNamesList = new Dictionary<Guid, string>();
            var householdCampusList = new List<string>();

            if ( totalRows == 0 )
            {
                totalRows = tableData.Count();
            }

            var importedPeopleCount = ImportedPeople.Count;
            var completedItems = importedPeopleCount;
            var percentage = ( totalRows - 1 ) / 100 + 1;
            ReportProgress( 0, $"Verifying person import ({totalRows:N0} found, {importedPeopleCount:N0} already exist)." );

            foreach ( var groupedRows in tableData.GroupBy( r => r["Household_ID"] as int? ) )
            {
                var familyGroup = new Group();
                householdCampusList.Clear();

                foreach ( var row in groupedRows.Where( r => r != null ) )
                {
                    var familyRoleId = FamilyRole.Adult;
                    var currentCampus = string.Empty;
                    var individualId = row["Individual_ID"] as int?;
                    var householdId = row["Household_ID"] as int?;
                    var personKeys = GetPersonKeys( individualId, householdId );
                    if ( personKeys == null )
                    {
                        var person = new Person();
                        person.FirstName = row["First_Name"] as string;
                        person.MiddleName = row["Middle_Name"] as string;
                        person.NickName = row["Goes_By"] as string ?? person.FirstName;
                        person.LastName = row["Last_Name"] as string;
                        person.IsDeceased = false;

                        var DOB = row["Date_Of_Birth"] as DateTime?;
                        if ( DOB.HasValue )
                        {
                            var birthDate = (DateTime)DOB;
                            person.BirthDay = birthDate.Day;
                            person.BirthMonth = birthDate.Month;
                            person.BirthYear = birthDate.Year;
                        }

                        person.CreatedByPersonAliasId = ImportPersonAliasId;
                        person.RecordTypeValueId = PersonRecordTypeId;
                        person.ForeignKey = individualId.ToString();
                        person.ForeignId = individualId;

                        var gender = row["Gender"] as string;
                        if ( !string.IsNullOrWhiteSpace( gender ) )
                        {
                            person.Gender = (Gender)Enum.Parse( typeof( Gender ), gender );
                        }

                        var prefix = row["Prefix"] as string;
                        if ( !string.IsNullOrWhiteSpace( prefix ) )
                        {
                            prefix = prefix.RemoveSpecialCharacters();
                            person.TitleValueId = titleTypes.Where( s => prefix.Equals( s.Value.RemoveSpecialCharacters(), StringComparison.CurrentCultureIgnoreCase ) )
                                .Select( s => (int?)s.Id ).FirstOrDefault();

                            if ( !person.TitleValueId.HasValue )
                            {
                                var newTitle = AddDefinedValue( lookupContext, Rock.SystemGuid.DefinedType.PERSON_TITLE, prefix );
                                if ( newTitle != null )
                                {
                                    titleTypes.Add( newTitle );
                                    person.TitleValueId = newTitle.Id;
                                }
                            }
                        }

                        var suffix = row["Suffix"] as string;
                        if ( !string.IsNullOrWhiteSpace( suffix ) )
                        {
                            suffix = suffix.RemoveSpecialCharacters();
                            person.SuffixValueId = suffixTypes.Where( s => suffix.Equals( s.Value.RemoveSpecialCharacters(), StringComparison.CurrentCultureIgnoreCase ) )
                                .Select( s => (int?)s.Id ).FirstOrDefault();

                            if ( !person.SuffixValueId.HasValue )
                            {
                                var newSuffix = AddDefinedValue( lookupContext, Rock.SystemGuid.DefinedType.PERSON_SUFFIX, suffix );
                                if ( newSuffix != null )
                                {
                                    suffixTypes.Add( newSuffix );
                                    person.SuffixValueId = newSuffix.Id;
                                }
                            }
                        }

                        var maritalStatus = row["Marital_Status"] as string;
                        if ( !string.IsNullOrWhiteSpace( maritalStatus ) )
                        {
                            maritalStatus = maritalStatus.RemoveSpecialCharacters();
                            person.MaritalStatusValueId = maritalStatusTypes.Where( s => maritalStatus.Equals( s.Value.RemoveSpecialCharacters(), StringComparison.CurrentCultureIgnoreCase ) )
                                .Select( dv => (int?)dv.Id ).FirstOrDefault();

                            if ( !person.MaritalStatusValueId.HasValue )
                            {
                                var newMaritalStatus = AddDefinedValue( lookupContext, Rock.SystemGuid.DefinedType.PERSON_MARITAL_STATUS, maritalStatus );
                                if ( newMaritalStatus != null )
                                {
                                    maritalStatusTypes.Add( newMaritalStatus );
                                    person.MaritalStatusValueId = newMaritalStatus.Id;
                                }
                            }
                        }
                        else
                        {
                            person.MaritalStatusValueId = maritalStatusTypes.Where( dv => dv.Value.Equals( "Unknown", StringComparison.CurrentCultureIgnoreCase ) )
                                .Select( dv => (int?)dv.Id ).FirstOrDefault();
                        }

                        var familyRole = row["Household_Position"] as string;
                        if ( !string.IsNullOrWhiteSpace( familyRole ) )
                        {
                            if ( familyRole.Equals( "Visitor", StringComparison.CurrentCultureIgnoreCase ) )
                            {
                                familyRoleId = FamilyRole.Visitor;
                            }
                            else if ( familyRole.Equals( "Child", StringComparison.CurrentCultureIgnoreCase ) || person.Age < 18 )
                            {
                                familyRoleId = FamilyRole.Child;
                            }
                        }

                        var memberStatus = row["Status_Name"] as string;
                        if ( !string.IsNullOrWhiteSpace( memberStatus ) )
                        {
                            memberStatus = memberStatus.Trim();
                            if ( memberStatus.Equals( "Member", StringComparison.CurrentCultureIgnoreCase ) )
                            {
                                person.ConnectionStatusValueId = MemberConnectionStatusId;
                                person.RecordStatusValueId = ActivePersonRecordStatusId;
                            }
                            else if ( memberStatus.Equals( "Visitor", StringComparison.CurrentCultureIgnoreCase ) )
                            {
                                person.ConnectionStatusValueId = VisitorConnectionStatusId;
                                person.RecordStatusValueId = ActivePersonRecordStatusId;
                            }
                            else if ( memberStatus.Equals( "Deceased", StringComparison.CurrentCultureIgnoreCase ) )
                            {
                                person.IsDeceased = true;
                                person.RecordStatusReasonValueId = DeceasedPersonRecordReasonId;
                                person.RecordStatusValueId = InactivePersonRecordStatusId;
                            }
                            else if ( memberStatus.Equals( "Dropped", StringComparison.CurrentCultureIgnoreCase ) || memberStatus.StartsWith( "Inactive", StringComparison.CurrentCultureIgnoreCase ) )
                            {
                                person.RecordStatusReasonValueId = NoActivityPersonRecordReasonId;
                                person.RecordStatusValueId = InactivePersonRecordStatusId;
                            }
                            else
                            {
                                // create user-defined connection type if it doesn't exist
                                person.RecordStatusValueId = ActivePersonRecordStatusId;
                                person.ConnectionStatusValueId = connectionStatusTypes.Where( dv => dv.Value.Equals( memberStatus, StringComparison.CurrentCultureIgnoreCase ) )
                                    .Select( dv => (int?)dv.Id ).FirstOrDefault();

                                if ( !person.ConnectionStatusValueId.HasValue )
                                {
                                    var newConnectionStatus = AddDefinedValue( lookupContext, Rock.SystemGuid.DefinedType.PERSON_CONNECTION_STATUS, memberStatus );
                                    if ( newConnectionStatus != null )
                                    {
                                        connectionStatusTypes.Add( newConnectionStatus );
                                        person.ConnectionStatusValueId = newConnectionStatus.Id;
                                    }
                                }
                            }
                        }
                        else
                        {
                            person.ConnectionStatusValueId = VisitorConnectionStatusId;
                            person.RecordStatusValueId = ActivePersonRecordStatusId;
                        }

                        var campus = row["SubStatus_Name"] as string;
                        if ( !string.IsNullOrWhiteSpace( campus ) )
                        {
                            currentCampus = campus;
                        }

                        var status_comment = row["Status_Comment"] as string;
                        if ( !string.IsNullOrWhiteSpace( status_comment ) )
                        {
                            person.SystemNote = status_comment;
                        }

                        var previousName = row["Former_Name"] as string;
                        if ( !string.IsNullOrWhiteSpace( previousName ) )
                        {
                            previousNamesList.Add( person.Guid, previousName );
                        }

                        // set a flag to keep visitors from receiving household info
                        person.ReviewReasonNote = familyRoleId.ToString();

                        // Translate F1 attributes
                        person.Attributes = new Dictionary<string, AttributeCache>();
                        person.AttributeValues = new Dictionary<string, AttributeValueCache>();

                        // IndividualId already defined in scope
                        AddEntityAttributeValue( lookupContext, IndividualIdAttribute, person, individualId.ToString() );

                        // HouseholdId already defined in scope
                        AddEntityAttributeValue( lookupContext, HouseholdIdAttribute, person, householdId.ToString() );

                        var previousChurch = row["Former_Church"] as string;
                        if ( !string.IsNullOrWhiteSpace( previousChurch ) )
                        {
                            AddEntityAttributeValue( lookupContext, previousChurchAttribute, person, previousChurch );
                        }

                        var employer = row["Employer"] as string;
                        if ( !string.IsNullOrWhiteSpace( employer ) )
                        {
                            AddEntityAttributeValue( lookupContext, employerAttribute, person, employer );
                        }

                        var position = row["Occupation_Name"] as string ?? row["Occupation_Description"] as string;
                        if ( !string.IsNullOrWhiteSpace( position ) )
                        {
                            AddEntityAttributeValue( lookupContext, positionAttribute, person, position );
                        }

                        var school = row["School_Name"] as string;
                        if ( !string.IsNullOrWhiteSpace( school ) )
                        {
                            AddEntityAttributeValue( lookupContext, schoolAttribute, person, school );
                        }

                        var firstVisit = row["First_Record"] as DateTime?;
                        if ( firstVisit.HasValue )
                        {
                            person.CreatedDateTime = firstVisit;
                            AddEntityAttributeValue( lookupContext, firstVisitAttribute, person, firstVisit.Value.ToString( "yyyy-MM-dd" ) );
                        }

                        // Only import membership date if they are a member
                        var membershipDate = row["Status_Date"] as DateTime?;
                        if ( membershipDate.HasValue && memberStatus.Contains( "member" ) )
                        {
                            AddEntityAttributeValue( lookupContext, membershipDateAttribute, person, membershipDate.Value.ToString( "yyyy-MM-dd" ) );
                        }

                        var checkinNote = row["Default_tag_comment"] as string;
                        if ( !string.IsNullOrWhiteSpace( checkinNote ) )
                        {
                            AddEntityAttributeValue( lookupContext, allergyNoteAttribute, person, checkinNote );
                        }

                        var barcode = row["Bar_Code"] as string;
                        if ( !string.IsNullOrWhiteSpace( barcode ) )
                        {
                            AddEntityAttributeValue( lookupContext, barcodeAttribute, person, barcode );
                        }

                        var groupMember = new GroupMember
                        {
                            Person = person,
                            GroupRoleId = familyRoleId != FamilyRole.Child ? FamilyAdultRoleId : FamilyChildRoleId,
                            GroupMemberStatus = GroupMemberStatus.Active
                        };

                        if ( familyRoleId != FamilyRole.Visitor )
                        {
                            householdCampusList.Add( currentCampus );
                            familyGroup.Members.Add( groupMember );
                            familyGroup.ForeignKey = householdId.ToString();
                            familyGroup.ForeignId = householdId;
                        }
                        else
                        {
                            var visitorGroup = new Group
                            {
                                GroupTypeId = FamilyGroupTypeId,
                                ForeignKey = householdId.ToString(),
                                ForeignId = householdId,
                                Name = person.LastName + " Family",
                                CampusId = GetCampusId( currentCampus )
                            };
                            visitorGroup.Members.Add( groupMember );
                            familyList.Add( visitorGroup );
                            completedItems += visitorGroup.Members.Count;

                            visitorList.Add( visitorGroup );
                        }
                    }
                }

                if ( familyGroup.Members.Any() )
                {
                    familyGroup.Name = familyGroup.Members.OrderByDescending( p => p.Person.Age )
                        .FirstOrDefault().Person.LastName + " Family";
                    familyGroup.GroupTypeId = FamilyGroupTypeId;

                    var primaryCampusTag = householdCampusList.GroupBy( c => c ).OrderByDescending( c => c.Count() )
                        .Select( c => c.Key ).FirstOrDefault();
                    if ( !string.IsNullOrWhiteSpace( primaryCampusTag ) )
                    {
                        familyGroup.CampusId = GetCampusId( primaryCampusTag );
                    }

                    familyList.Add( familyGroup );
                    completedItems += familyGroup.Members.Count;
                    // average family has 2.3 members, so fudge the math a little
                    if ( completedItems % percentage < 2 )
                    {
                        var percentComplete = completedItems / percentage;
                        ReportProgress( percentComplete, $"{completedItems - importedPeopleCount:N0} people imported ({percentComplete}% complete)." );
                    }
                    else if ( completedItems % ReportingNumber < 1 )
                    {
                        SavePeople( familyList, visitorList, previousNamesList );

                        familyList.Clear();
                        visitorList.Clear();
                        previousNamesList.Clear();
                        ReportPartialProgress();
                    }
                }
            }

            // Save any remaining families in the batch
            if ( familyList.Any() )
            {
                SavePeople( familyList, visitorList, previousNamesList );
            }

            ReportProgress( 100, $"Finished person import: {completedItems - importedPeopleCount:N0} people imported." );
        }

        /// <summary>
        /// Saves the people.
        /// </summary>
        /// <param name="familyList">The family list.</param>
        /// <param name="visitorList">The visitor list.</param>
        /// <param name="previousNamesList">The previous names list.</param>
        private static void SavePeople( List<Group> familyList, List<Group> visitorList, Dictionary<Guid, string> previousNamesList )
        {
            var rockContext = new RockContext();
            var groupMemberService = new GroupMemberService( rockContext );
            rockContext.WrapTransaction( () =>
            {
                rockContext.Configuration.AutoDetectChangesEnabled = false;
                rockContext.Groups.AddRange( familyList );
                rockContext.SaveChanges( DisableAuditing );

                foreach ( var familyGroups in familyList.GroupBy( g => g.ForeignId ) )
                {
                    var visitorsExist = familyGroups.Count() > 1;
                    foreach ( var newFamilyGroup in familyGroups )
                    {
                        foreach ( var groupMember in newFamilyGroup.Members )
                        {
                            // don't call LoadAttributes, it only rewrites existing cache objects
                            // groupMember.Person.LoadAttributes( rockContext );

                            var memberPersonAttributeValues = groupMember.Person.Attributes.Select( a => a.Value )
                                .Select( a => new AttributeValue
                                {
                                    AttributeId = a.Id,
                                    EntityId = groupMember.Person.Id,
                                    Value = groupMember.Person.AttributeValues[a.Key].Value
                                } ).ToList();

                            rockContext.AttributeValues.AddRange( memberPersonAttributeValues );

                            // add a default person alias
                            if ( !groupMember.Person.Aliases.Any( a => a.AliasPersonId == groupMember.Person.Id ) )
                            {
                                groupMember.Person.Aliases.Add( new PersonAlias
                                {
                                    AliasPersonId = groupMember.Person.Id,
                                    AliasPersonGuid = groupMember.Person.Guid,
                                    ForeignId = groupMember.Person.ForeignId,
                                    ForeignKey = groupMember.Person.ForeignKey
                                } );
                            }

                            // assign the previous name
                            if ( previousNamesList.Any( l => l.Key.Equals( groupMember.Person.Guid ) ) )
                            {
                                var newPreviousName = new PersonPreviousName
                                {
                                    LastName = previousNamesList[groupMember.Person.Guid],
                                    PersonAlias = groupMember.Person.Aliases.FirstOrDefault()
                                };

                                rockContext.PersonPreviousNames.Add( newPreviousName );
                            }

                            // assign the giving group
                            if ( groupMember.GroupRoleId != FamilyChildRoleId )
                            {
                                groupMember.Person.GivingGroupId = newFamilyGroup.Id;
                            }

                            // Add known relationship group
                            var knownGroupMember = new GroupMember
                            {
                                PersonId = groupMember.Person.Id,
                                GroupRoleId = KnownRelationshipOwnerRoleId
                            };

                            var knownRelationshipGroup = new Group
                            {
                                Name = KnownRelationshipGroupType.Name,
                                GroupTypeId = KnownRelationshipGroupType.Id,
                                IsPublic = true
                            };

                            knownRelationshipGroup.Members.Add( knownGroupMember );
                            rockContext.Groups.Add( knownRelationshipGroup );

                            // Add implied relationship group
                            var impliedGroupMember = new GroupMember
                            {
                                PersonId = groupMember.Person.Id,
                                GroupRoleId = ImpliedRelationshipOwnerRoleId
                            };

                            var impliedGroup = new Group
                            {
                                Name = ImpliedRelationshipGroupType.Name,
                                GroupTypeId = ImpliedRelationshipGroupType.Id,
                                IsPublic = true
                            };

                            impliedGroup.Members.Add( impliedGroupMember );
                            rockContext.Groups.Add( impliedGroup );

                            if ( visitorsExist )
                            {
                                // if this is a visitor, then add relationships to the family member(s)
                                if ( visitorList.Where( v => v.ForeignId == newFamilyGroup.ForeignId )
                                        .Any( v => v.Members.Any( m => m.Person.ForeignId.Equals( groupMember.Person.ForeignId ) ) ) )
                                {
                                    var familyMembers = familyGroups.Except( visitorList ).SelectMany( g => g.Members );
                                    foreach ( var familyMember in familyMembers.Select( m => m.Person ) )
                                    {
                                        var invitedByMember = new GroupMember
                                        {
                                            PersonId = familyMember.Id,
                                            GroupRoleId = InvitedByKnownRelationshipId
                                        };

                                        knownRelationshipGroup.Members.Add( invitedByMember );

                                        if ( groupMember.Person.Age < 15 && familyMember.Age > 15 )
                                        {
                                            var allowCheckinMember = new GroupMember
                                            {
                                                PersonId = familyMember.Id,
                                                GroupRoleId = AllowCheckInByKnownRelationshipId
                                            };

                                            knownRelationshipGroup.Members.Add( allowCheckinMember );
                                        }
                                    }
                                }
                                else
                                {   // not a visitor, add the visitors to the family member's known relationship
                                    var visitors = visitorList.Where( v => v.ForeignId == newFamilyGroup.ForeignId )
                                        .SelectMany( g => g.Members ).ToList();
                                    foreach ( var visitor in visitors.Select( g => g.Person ) )
                                    {
                                        var inviteeMember = new GroupMember
                                        {
                                            PersonId = visitor.Id,
                                            GroupRoleId = InviteeKnownRelationshipId
                                        };

                                        knownRelationshipGroup.Members.Add( inviteeMember );

                                        // if visitor can be checked in and this person is considered an adult
                                        if ( visitor.Age < 18 && groupMember.Person.Age > 18 )
                                        {
                                            var canCheckInMember = new GroupMember
                                            {
                                                PersonId = visitor.Id,
                                                GroupRoleId = CanCheckInKnownRelationshipId
                                            };

                                            knownRelationshipGroup.Members.Add( canCheckInMember );
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                rockContext.ChangeTracker.DetectChanges();
                rockContext.SaveChanges( DisableAuditing );
            } ); // end wrap transaction

            // add the new people to our tracking list
            if ( familyList.Any() )
            {
                var familyMembers = familyList.SelectMany( gm => gm.Members );
                ImportedPeople.AddRange( familyMembers.Select( m => new PersonKeys
                {
                    PersonAliasId = (int)m.Person.PrimaryAliasId,
                    PersonId = m.Person.Id,
                    PersonForeignId = m.Person.ForeignId,
                    GroupForeignId = m.Group.ForeignId,
                    FamilyRoleId = m.Person.ReviewReasonNote.ConvertToEnum<FamilyRole>()
                } ).ToList()
                );
            }

            if ( visitorList.Any() )
            {
                var visitors = visitorList.SelectMany( gm => gm.Members );
                ImportedPeople.AddRange( visitors.Select( m => new PersonKeys
                {
                    PersonAliasId = (int)m.Person.PrimaryAliasId,
                    PersonId = m.Person.Id,
                    PersonForeignId = m.Person.ForeignId,
                    GroupForeignId = m.Group.ForeignId,
                    FamilyRoleId = m.Person.ReviewReasonNote.ConvertToEnum<FamilyRole>()
                } ).ToList()
                );
            }
        }
    }
}