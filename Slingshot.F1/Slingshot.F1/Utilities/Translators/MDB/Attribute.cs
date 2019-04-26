using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using OrcaMDF.Core.MetaData;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Attribute = Rock.Model.Attribute;

namespace Slingshot.F1.Utilities.Translators
{    
    public static class F1Attribute
    {
        /// <summary>
        /// Translates the people attributes to date/text attributes.
        /// Also converts attribute comments to person notes.
        /// </summary>
        /// <param name="tableData">The table data.</param>
        /// <param name="totalRows">The total rows.</param>
        private void TranslateAttribute( IQueryable<Row> tableData, long totalRows = 0 )
        {
            var lookupContext = new RockContext();
            var personService = new PersonService( lookupContext );

            var personAttributes = new AttributeService( lookupContext ).GetByEntityTypeId( PersonEntityTypeId ).Include( "Categories" ).AsNoTracking().ToList();
            var importedAttributeCount = lookupContext.AttributeValues.Count( v => v.Attribute.EntityTypeId == PersonEntityTypeId && v.ForeignKey != null );
            var baptizedHereAttribute = personAttributes.FirstOrDefault( a => a.Key.Equals( "BaptizedHere", StringComparison.InvariantCultureIgnoreCase ) );
            var newBenevolences = new List<BenevolenceRequest>();
            var peopleToUpdate = new Dictionary<int, Person>();

            if ( totalRows == 0 )
            {
                totalRows = tableData.Count();
            }

            var completedItems = 0;
            var percentage = ( totalRows - 1 ) / 100 + 1;
            ReportProgress( 0, $"Verifying attribute import ({totalRows:N0} found, {importedAttributeCount:N0} already exist)." );

            foreach ( var row in tableData.OrderBy( r => r["Attribute_Name"] ).ThenByDescending( r => r["Start_Date"] != null ).ThenBy( r => r["Start_Date"] ).ThenBy( r => r["End_Date"] ).Where( r => r != null ) )
            {
                // add the new attribute
                var attributeGroupName = row["Attribute_Group_Name"] as string;
                var attributeName = row["Attribute_Name"] as string;
                var attributeDate = row["Start_Date"] as DateTime?;
                attributeDate = attributeDate ?? row["End_Date"] as DateTime?;
                var attributeComment = row["Comment"] as string;
                var attributeCreator = row["Staff_Individual_ID"] as int?;
                int? campusId = null;

                // strip attribute group name (will become a category)
                if ( attributeGroupName.Any( n => ValidDelimiters.Contains( n ) ) )
                {
                    campusId = campusId ?? GetCampusId( attributeGroupName );
                    if ( campusId.HasValue )
                    {
                        attributeGroupName = StripPrefix( attributeGroupName, campusId );
                    }
                }

                // strip attribute name
                if ( attributeName.Any( n => ValidDelimiters.Contains( n ) ) )
                {
                    campusId = campusId ?? GetCampusId( attributeName );
                    if ( campusId.HasValue )
                    {
                        attributeName = StripPrefix( attributeName, campusId );
                    }
                }

                var personBaptizedHere = false;
                var isBenevolenceAttribute = false;
                if ( attributeName.StartsWith( "Baptism", StringComparison.CurrentCultureIgnoreCase ) )
                {   // match the core Baptism attribute
                    attributeName = "Baptism Date";
                    personBaptizedHere = attributeCreator.HasValue;
                }
                else if ( attributeName.StartsWith( "Benevolence", StringComparison.CurrentCultureIgnoreCase ) )
                {   // set a flag to create benevolence items
                    isBenevolenceAttribute = true;
                    attributeName = attributeName.Replace( "Benevolence", string.Empty ).Trim();
                }

                Attribute primaryAttribute = null, campusAttribute = null;
                // don't create custom attributes for benevolence items
                if ( !isBenevolenceAttribute )
                {
                    // create attributes if they don't exist
                    var attributeKey = attributeName.RemoveSpecialCharacters();
                    primaryAttribute = personAttributes.FirstOrDefault( a => a.Key.Equals( attributeKey, StringComparison.CurrentCultureIgnoreCase ) );
                    if ( primaryAttribute == null )
                    {
                        primaryAttribute = AddEntityAttribute( lookupContext, PersonEntityTypeId, string.Empty, string.Empty, $"{attributeKey} imported {ImportDateTime}",
                            attributeGroupName, attributeName, attributeKey, attributeDate.HasValue ? DateFieldTypeId : TextFieldTypeId, importPersonAliasId: ImportPersonAliasId
                        );

                        personAttributes.Add( primaryAttribute );
                    }
                    // attribute already exists, add the new category
                    else if ( !primaryAttribute.Categories.Any( c => c.Name.Equals( attributeGroupName ) ) )
                    {
                        var attributeCategory = GetCategory( lookupContext, AttributeEntityTypeId, null, attributeGroupName, false, "EntityTypeId", PersonEntityTypeId.ToString() );
                        primaryAttribute.Categories.Add( attributeCategory );
                    }

                    // only create a campus attribute if there was a campus prefix
                    campusAttribute = personAttributes.FirstOrDefault( a => a.Key.Equals( $"{attributeKey}Campus", StringComparison.CurrentCultureIgnoreCase ) );
                    if ( campusAttribute == null && campusId.HasValue )
                    {
                        campusAttribute = AddEntityAttribute( lookupContext, PersonEntityTypeId, string.Empty, string.Empty, $"{attributeKey}Campus imported {ImportDateTime}",
                            attributeGroupName, $"{attributeName} Campus", $"{attributeKey}Campus", CampusFieldTypeId
                        );

                        personAttributes.Add( campusAttribute );
                    }
                }

                // make sure we have a valid person to assign to
                var individualId = row["Individual_Id"] as int?;
                var matchingPerson = GetPersonKeys( individualId, null, includeVisitors: false );
                if ( matchingPerson != null )
                {
                    var person = !peopleToUpdate.ContainsKey( matchingPerson.PersonId )
                        ? personService.Queryable( includeDeceased: true ).FirstOrDefault( p => p.Id == matchingPerson.PersonId )
                        : peopleToUpdate[matchingPerson.PersonId];

                    if ( person != null )
                    {
                        int? creatorAliasId = null;
                        var noteCreator = GetPersonKeys( attributeCreator );
                        if ( noteCreator != null )
                        {
                            creatorAliasId = noteCreator.PersonAliasId;
                        }

                        if ( !isBenevolenceAttribute )
                        {
                            // could have multiple attributes assigned to this person, don't overwrite previous
                            if ( person.Attributes == null || person.AttributeValues == null )
                            {
                                person.Attributes = new Dictionary<string, AttributeCache>();
                                person.AttributeValues = new Dictionary<string, AttributeValueCache>();
                            }

                            var attributeValue = attributeDate.HasValue ? attributeDate.Value.ToString( "yyyy-MM-dd" ) : attributeComment;
                            if ( string.IsNullOrWhiteSpace( attributeValue ) )
                            {
                                // add today's date so that the attribute at least gets a value
                                attributeValue = RockDateTime.Now.ToString( "yyyy-MM-dd" );
                            }

                            AddEntityAttributeValue( lookupContext, primaryAttribute, person, attributeValue );

                            if ( personBaptizedHere )
                            {
                                AddEntityAttributeValue( lookupContext, baptizedHereAttribute, person, "Yes" );
                            }

                            // Add the campus attribute value
                            if ( campusAttribute != null && campusId.HasValue )
                            {
                                var campus = CampusList.FirstOrDefault( c => c.Id.Equals( campusId ) );
                                AddEntityAttributeValue( lookupContext, campusAttribute, person, campus.Guid.ToString() );
                            }

                            // convert the attribute comment to a person note
                            if ( !string.IsNullOrWhiteSpace( attributeComment ) )
                            {
                                // add the note to the person
                                AddEntityNote( lookupContext, PersonEntityTypeId, person.Id, attributeName, attributeComment, false, false,
                                    attributeGroupName, null, true, attributeDate, $"Imported {ImportDateTime}", creatorAliasId );
                            }
                        }
                        // benevolences require a date
                        else if ( attributeDate.HasValue )
                        {
                            var requestText = !string.IsNullOrWhiteSpace( attributeComment ) ? attributeComment : "N/A";
                            var benevolence = new BenevolenceRequest
                            {
                                CampusId = campusId,
                                RequestDateTime = attributeDate.Value,
                                FirstName = person.FirstName,
                                LastName = person.LastName,
                                Email = person.Email,
                                RequestedByPersonAliasId = person.PrimaryAliasId,
                                ConnectionStatusValueId = person.ConnectionStatusValueId,
                                CaseWorkerPersonAliasId = creatorAliasId,
                                RequestStatusValueId = ParseBenevolenceStatus( attributeName ),
                                RequestText = requestText,
                                CreatedDateTime = attributeDate.Value,
                                ModifiedDateTime = attributeDate.Value,
                                CreatedByPersonAliasId = creatorAliasId,
                                ModifiedByPersonAliasId = ImportPersonAliasId,
                                ForeignKey = $"Benevolence imported {ImportDateTime}"
                            };

                            newBenevolences.Add( benevolence );
                        }

                        // store the person lookup for this batch
                        if ( !peopleToUpdate.ContainsKey( matchingPerson.PersonId ) )
                        {
                            peopleToUpdate.Add( matchingPerson.PersonId, person );
                        }
                        else
                        {
                            peopleToUpdate[matchingPerson.PersonId] = person;
                        }
                    }
                }

                completedItems++;
                if ( completedItems % percentage < 1 )
                {
                    var percentComplete = completedItems / percentage;
                    ReportProgress( percentComplete, $"{completedItems:N0} attributes imported ({percentComplete}% complete)." );
                }
                else if ( completedItems % ReportingNumber < 1 )
                {
                    SaveAttributes( peopleToUpdate );
                    SaveBenevolenceRequests( newBenevolences );

                    // reset so context doesn't bloat
                    lookupContext.Dispose();
                    lookupContext = new RockContext();
                    personService = new PersonService( lookupContext );
                    peopleToUpdate.Clear();
                    newBenevolences.Clear();
                    ReportPartialProgress();
                }
            }

            SaveAttributes( peopleToUpdate );
            SaveBenevolenceRequests( newBenevolences );

            ReportProgress( 100, $"Finished attribute import: {completedItems:N0} attributes imported." );
        }

        /// <summary>
        /// Translates the requirement data.
        /// </summary>
        /// <param name="tableData">The table data.</param>
        /// <param name="totalRows">The total rows.</param>
        private void TranslateRequirement( IQueryable<Row> tableData, long totalRows = 0 )
        {
            var lookupContext = new RockContext();
            var importedAttributeCount = lookupContext.AttributeValues.Count( v => v.Attribute.EntityTypeId == PersonEntityTypeId && v.ForeignKey != null );
            var personAttributes = new AttributeService( lookupContext ).GetByEntityTypeId( PersonEntityTypeId )
                .Include( "Categories" ).Include( "AttributeQualifiers" ).AsNoTracking().ToList();
            var backgroundCheckedAttribute = personAttributes.FirstOrDefault( a => a.Key.Equals( "BackgroundChecked", StringComparison.CurrentCultureIgnoreCase ) );
            var peopleToUpdate = new Dictionary<int, Person>();

            if ( totalRows == 0 )
            {
                totalRows = tableData.Count();
            }

            var completedItems = 0;
            var percentage = ( totalRows - 1 ) / 100 + 1;

            ReportProgress( 0, string.Format( "Verifying requirement import ({0:N0} found, {1:N0} already exist).", totalRows, importedAttributeCount ) );

            foreach ( var row in tableData.OrderBy( r => r["Requirement_Date"] ).ThenByDescending( r => r["Individual_ID"] ).Where( r => r != null ) )
            {
                var individualId = row["Individual_ID"] as int?;
                var requirementName = row["Requirement_Name"] as string;
                var requirementDateString = row["Requirement_Date"] as string;
                var requirementStatus = row["Requirement_Status_Name"] as string;
                var isConfidential = row["Is_Confidential"] as bool?;
                var requirementDate = requirementDateString.AsDateTime();

                var confidentialCategory = isConfidential == true ? "Confidential" : string.Empty;
                // create the requirement date
                var attributeName = string.Format( "{0} Date", requirementName );
                var requirementDateAttribute = personAttributes.FirstOrDefault( a => a.Key.Equals( attributeName.RemoveSpecialCharacters() ) && a.FieldTypeId == DateFieldTypeId );
                if ( requirementDateAttribute == null )
                {
                    requirementDateAttribute = AddEntityAttribute( lookupContext, PersonEntityTypeId, string.Empty, string.Empty, string.Format( "{0} imported {1}", attributeName, ImportDateTime ),
                        confidentialCategory, attributeName, attributeName.RemoveSpecialCharacters(), DateFieldTypeId, importPersonAliasId: ImportPersonAliasId );
                    personAttributes.Add( requirementDateAttribute );
                }

                // create the requirement status
                attributeName = string.Format( "{0} Result", requirementName );
                var requirementResultAttribute = personAttributes.FirstOrDefault( a => a.Key.Equals( attributeName.RemoveSpecialCharacters() ) && a.FieldTypeId == SingleSelectFieldTypeId );
                if ( requirementResultAttribute == null )
                {
                    requirementResultAttribute = AddEntityAttribute( lookupContext, PersonEntityTypeId, string.Empty, string.Empty, string.Format( "{0} imported {1}", attributeName, ImportDateTime ),
                        confidentialCategory, attributeName, attributeName.RemoveSpecialCharacters(), SingleSelectFieldTypeId, importPersonAliasId: ImportPersonAliasId );
                    personAttributes.Add( requirementResultAttribute );
                }

                // add any custom qualifiers
                var valuesQualifier = requirementResultAttribute.AttributeQualifiers.FirstOrDefault( q => q.Key.Equals( "values", StringComparison.CurrentCultureIgnoreCase ) );
                if ( valuesQualifier != null && !valuesQualifier.Value.Contains( requirementStatus ) )
                {
                    valuesQualifier = AddAttributeQualifier( lookupContext, requirementResultAttribute.Id, requirementStatus );
                }

                // make sure we have a valid person to assign to
                var matchingPerson = GetPersonKeys( individualId, null, includeVisitors: false );
                if ( matchingPerson != null )
                {
                    var person = !peopleToUpdate.ContainsKey( matchingPerson.PersonId )
                        ? lookupContext.People.AsQueryable().AsNoTracking().FirstOrDefault( p => p.Id == matchingPerson.PersonId )
                        : peopleToUpdate[matchingPerson.PersonId];

                    if ( person != null )
                    {
                        // could have multiple attributes assigned to this person, don't overwrite previous
                        if ( person.Attributes == null || person.AttributeValues == null )
                        {
                            person.Attributes = new Dictionary<string, AttributeCache>();
                            person.AttributeValues = new Dictionary<string, AttributeValueCache>();
                        }

                        AddEntityAttributeValue( lookupContext, requirementResultAttribute, person, requirementStatus );

                        if ( requirementDate.HasValue )
                        {
                            AddEntityAttributeValue( lookupContext, requirementDateAttribute, person, requirementDate.Value.ToString( "yyyy-MM-dd" ) );
                        }

                        if ( requirementName.StartsWith( "Background Check", StringComparison.CurrentCultureIgnoreCase ) )
                        {
                            AddEntityAttributeValue( lookupContext, backgroundCheckedAttribute, person, "True" );
                        }

                        // add all the person changes to the batch
                        if ( !peopleToUpdate.ContainsKey( matchingPerson.PersonId ) )
                        {
                            peopleToUpdate.Add( matchingPerson.PersonId, person );
                        }
                        else
                        {
                            peopleToUpdate[matchingPerson.PersonId] = person;
                        }
                    }
                }

                completedItems++;
                if ( completedItems % percentage < 1 )
                {
                    var percentComplete = completedItems / percentage;
                    ReportProgress( percentComplete, $"{completedItems:N0} requirements imported ({percentComplete}% complete)." );
                }
                else if ( completedItems % ReportingNumber < 1 )
                {
                    SaveAttributes( peopleToUpdate );

                    // reset so context doesn't bloat
                    lookupContext.Dispose();
                    lookupContext = new RockContext();
                    peopleToUpdate.Clear();
                    ReportPartialProgress();
                }
            }

            SaveAttributes( peopleToUpdate );

            ReportProgress( 100, $"Finished requirement import: {completedItems:N0} requirements imported." );
        }

        /// <summary>
        /// Saves the attribute.
        /// </summary>
        /// <param name="updatedPersonList">The updated person list.</param>
        private static void SaveAttributes( Dictionary<int, Person> updatedPersonList )
        {
            if ( updatedPersonList.Count > 0 )
            {
                using ( var rockContext = new RockContext() )
                {
                    rockContext.Configuration.AutoDetectChangesEnabled = false;

                    foreach ( var person in updatedPersonList.Values.Where( p => p.Attributes != null && p.Attributes.Any() ) )
                    {
                        // don't call LoadAttributes, it only rewrites existing cache objects
                        // person.LoadAttributes( rockContext );

                        foreach ( var attributeCache in person.Attributes.Select( a => a.Value ) )
                        {
                            var personAttributeValue = rockContext.AttributeValues.FirstOrDefault( v => v.Attribute.Id == attributeCache.Id && v.EntityId == person.Id );
                            var newAttributeValue = person.AttributeValues[attributeCache.Key];

                            // set the new value and add it to the database
                            if ( personAttributeValue == null )
                            {
                                personAttributeValue = new AttributeValue
                                {
                                    AttributeId = newAttributeValue.AttributeId,
                                    EntityId = person.Id,
                                    Value = newAttributeValue.Value,
                                    ForeignKey = $"Imported {ImportDateTime}",
                                    CreatedDateTime = ImportDateTime,
                                    CreatedByPersonAliasId = ImportPersonAliasId
                                };

                                rockContext.AttributeValues.Add( personAttributeValue );
                            }
                            else if ( !personAttributeValue.Value.Equals( newAttributeValue.Value, StringComparison.CurrentCultureIgnoreCase ) )
                            {
                                personAttributeValue.Value = newAttributeValue.Value;
                                rockContext.Entry( personAttributeValue ).State = EntityState.Modified;
                            }
                        }
                    }

                    rockContext.ChangeTracker.DetectChanges();
                    rockContext.SaveChanges( DisableAuditing );
                }
            }
        }

        /// <summary>
        /// Saves the benevolence requests.
        /// </summary>
        /// <param name="newBenevolences">The new benevolences.</param>
        private static void SaveBenevolenceRequests( List<BenevolenceRequest> newBenevolences )
        {
            if ( newBenevolences.Count > 0 )
            {
                using ( var rockContext = new RockContext() )
                {
                    rockContext.BulkInsert( newBenevolences );
                }
            }
        }
    }
}