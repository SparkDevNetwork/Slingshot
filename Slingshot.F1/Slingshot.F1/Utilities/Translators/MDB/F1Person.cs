using System;
using System.Collections.Generic;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Linq;

using Slingshot.Core;
using Slingshot.Core.Model;

namespace Slingshot.F1.Utilities.Translators.MDB
{
    public static class F1Person
    {
        public static Person Translate(
            DataRow row
            , DataTable dtCommunications_IndividualEmails
            , DataTable dtCommunications_InfellowshipLogins
            , DataTable dtCommunications_HouseholdEmails
            , Dictionary<int, HeadOfHousehold> headOfHouseHolds
            , DataTable dtRequirementValues
            , DataTable dtCommunicationValues )
        {
            var person = new Person();
            var notes = new List<string>();
            try
            {

                var householdId = row.Field<int>( "household_id" );

                // person id
                int? personId = row.Field<int?>( "individual_id" );
                if ( personId.HasValue )
                {
                    person.Id = personId.Value;
                }

                // names
                // Limit the Name fields to 50 characters because the Rock DB has a limit of 50 characters.
                string firstName = row.Field<string>( "first_name" ).Left( 50 );
                if ( firstName.IsNotNullOrWhitespace() )
                {
                    person.FirstName = firstName;
                }

                string nickName = row.Field<string>( "goes_by" ).Left( 50 );
                if ( nickName.IsNotNullOrWhitespace() )
                {
                    person.NickName = nickName;
                }

                string middleName = row.Field<string>( "middle_name" ).Left( 50 );
                if ( middleName.IsNotNullOrWhitespace() )
                {
                    person.MiddleName = middleName;
                }

                string lastName = row.Field<string>( "last_name" ).Left( 50 );
                if ( lastName.IsNotNullOrWhitespace() )
                {
                    person.LastName = lastName;
                }

                System.Globalization.TextInfo textInfo = new System.Globalization.CultureInfo( "en-US", false ).TextInfo;
                string salutation = row.Field<string>( "prefix" );
                if ( salutation.IsNotNullOrWhitespace() )
                {
                    person.Salutation = salutation.Trim();
                }

                if ( !String.IsNullOrWhiteSpace( person.Salutation ) )
                {
                    person.Salutation = textInfo.ToTitleCase( person.Salutation.ToLower() );
                }

                string suffix = row.Field<string>( "suffix" );
                if ( !string.IsNullOrWhiteSpace( suffix ) )
                {
                    suffix = suffix.Trim();
                    if ( suffix.Equals( "Sr.", StringComparison.OrdinalIgnoreCase ) )
                    {
                        person.Suffix = "Sr.";
                    }
                    else if ( suffix.Equals( "Jr.", StringComparison.OrdinalIgnoreCase ) )
                    {
                        person.Suffix = "Jr.";
                    }
                    else if ( suffix.Equals( "Ph.D.", StringComparison.OrdinalIgnoreCase ) )
                    {
                        person.Suffix = "Ph.D.";
                    }
                    else
                    {
                        person.Suffix = suffix;
                    }
                }

                // gender 
                string gender = row.Field<string>( "Gender" );
                if ( gender.IsNotNullOrWhitespace() )
                {
                    if ( gender == "Male" )
                    {
                        person.Gender = Gender.Male;
                    }
                    else if ( gender == "Female" )
                    {
                        person.Gender = Gender.Female;
                    }
                    else
                    {
                        person.Gender = Gender.Unknown;
                    }
                }

                // marital status
                string maritalStatus = row.Field<string>( "marital_status" );

                switch ( maritalStatus )
                {
                    case "Married":
                        person.MaritalStatus = MaritalStatus.Married;
                        break;
                    case "Single":
                        person.MaritalStatus = MaritalStatus.Single;
                        break;
                    case "Divorced":
                        person.MaritalStatus = MaritalStatus.Divorced;
                        break;
                    default:
                        person.MaritalStatus = MaritalStatus.Unknown;
                        notes.Add( "Marital Status: " + maritalStatus );
                        break;
                }

                // connection status
                string connectionStatus = row.Field<string>( "status_name" );
                if ( connectionStatus.IsNotNullOrWhitespace() )
                {
                    person.ConnectionStatus = connectionStatus;
                }

                switch ( connectionStatus )
                {
                    case "Inactive Member":
                    case "Inactive":
                    case "Dropped":
                        person.RecordStatus = RecordStatus.Inactive;
                        break;
                    case "Deceased":
                        person.RecordStatus = RecordStatus.Inactive;
                        person.IsDeceased = true;
                        break;
                    default:
                        person.RecordStatus = RecordStatus.Active;
                        break;
                }

                // dates
                person.Birthdate = row.Field<DateTime?>( "date_of_birth" );
                person.CreatedDateTime = row.Field<DateTime?>( "first_record" );

                //family
                person.FamilyId = householdId;

                string familyName = row.Field<string>( "household_name" );
                if ( familyName.IsNotNullOrWhitespace() )
                {
                    person.FamilyName = familyName;
                }

                string familyRole = row.Field<string>( "household_position" );

                switch ( familyRole )
                {
                    case "Head":
                        person.FamilyRole = FamilyRole.Adult;
                        break;
                    case "Spouse":
                        person.FamilyRole = FamilyRole.Adult;
                        break;
                    case "Child":
                        person.FamilyRole = FamilyRole.Child;
                        break;
                    case "Visitor":
                        // likely the person is a visitor and should belong to their own family
                        person.FamilyRole = FamilyRole.Child;

                        // generate a new unique family id
                        if ( person.FirstName.IsNotNullOrWhitespace() || person.LastName.IsNotNullOrWhitespace() ||
                             person.MiddleName.IsNotNullOrWhitespace() || person.NickName.IsNotNullOrWhitespace() )
                        {
                            MD5 md5Hasher = MD5.Create();
                            string valueToHash = person.FirstName + person.NickName + person.MiddleName + person.LastName;
                            var hashed = md5Hasher.ComputeHash( Encoding.UTF8.GetBytes( valueToHash ) );
                            var familyId = Math.Abs( BitConverter.ToInt32( hashed, 0 ) ); // used abs to ensure positive number
                            if ( familyId > 0 )
                            {
                                person.FamilyId = familyId;
                            }
                        }
                        notes.Add( $"F1 Visitor of the { person.FamilyName } ({ person.FamilyId })" );
                        break;
                    default:
                        person.FamilyRole = FamilyRole.Child;
                        notes.Add( "Family Position: Other" );
                        break;
                }

                // email
                string email = null;
                // Communications table should be sorted by LastUpdateDate (in descending order) before this occurs.
                var emailrow = dtCommunications_IndividualEmails.Select( $"individual_id = { person.Id }" ).FirstOrDefault();
                if ( emailrow == null )
                {
                    emailrow = dtCommunications_InfellowshipLogins.Select( $"individual_id = { person.Id }" ).FirstOrDefault();
                }
                if ( emailrow == null && person.FamilyRole == FamilyRole.Adult )
                {
                    emailrow = dtCommunications_HouseholdEmails.Select( $"household_id = { person.FamilyId }" ).FirstOrDefault();
                }
                if ( emailrow != null )
                {
                    email = emailrow.Field<string>( "communication_value" );
                }

                if ( email.IsNotNullOrWhitespace() )
                {
                    person.Email = email;
                }

                // campus
                Campus campus = new Campus();
                person.Campus = campus;

                // Family members of the same family can have different campuses in F1 and Slingshot will set the family campus to the first family
                // member it see. To be consistent, we'll use the head of household's campus for the whole family.
                if ( headOfHouseHolds.TryGetValue( householdId, out var headOfHousehold ) )
                {
                    campus.CampusName = headOfHousehold?.SubStatusName;
                }
                else
                {
                    campus.CampusName = row.Field<string>( "SubStatus_Name" );
                }

                if ( !string.IsNullOrWhiteSpace( campus.CampusName ) )
                {
                    // generate a unique campus id
                    MD5 md5Hasher = MD5.Create();
                    var hashed = md5Hasher.ComputeHash( Encoding.UTF8.GetBytes( campus.CampusName ) );
                    var campusId = Math.Abs( BitConverter.ToInt32( hashed, 0 ) ); // used abs to ensure positive number
                    if ( campusId > 0 )
                    {
                        campus.CampusId = campusId;
                    }
                }

                // person requirements. 
                var requirements = dtRequirementValues.Select( $"individual_id = { person.Id }" );
                foreach ( var requirement in requirements )
                {
                    string requirementName = requirement.Field<string>( "requirement_name" );

                    // Add the attribute value for status (if not empty) 
                    var requirementStatus = requirement.Field<string>( "requirement_status_name" );
                    var requirementStatusKey = requirementName.RemoveSpaces().RemoveSpecialCharacters() + "Status";

                    if ( !string.IsNullOrWhiteSpace( requirementStatus ) )
                    {
                        person.Attributes.Add( new PersonAttributeValue
                        {
                            AttributeKey = requirementStatusKey,
                            AttributeValue = requirementStatus,
                            PersonId = person.Id
                        } );
                    }


                    // Add the attribute value for date (if not empty) 
                    DateTime? requirementDate = requirement.Field<DateTime?>( "requirement_date" );
                    var requirementDateKey = requirementName.RemoveSpaces().RemoveSpecialCharacters() + "Date";

                    if ( requirementDate.HasValue )
                    {
                        person.Attributes.Add( new PersonAttributeValue
                        {
                            AttributeKey = requirementDateKey,
                            AttributeValue = requirementDate.Value.ToString( "o" ),
                            PersonId = person.Id
                        } );
                    }
                }

                // person fields

                // occupation
                string occupation = row.Field<string>( "Occupation" );
                if ( occupation.IsNotNullOrWhitespace() )
                {
                    person.Attributes.Add( new PersonAttributeValue
                    {
                        AttributeKey = "Position",
                        AttributeValue = occupation,
                        PersonId = person.Id
                    } );
                }

                // employer
                string employer = row.Field<string>( "employer" );
                if ( employer.IsNotNullOrWhitespace() )
                {
                    person.Attributes.Add( new PersonAttributeValue
                    {
                        AttributeKey = "Employer",
                        AttributeValue = employer,
                        PersonId = person.Id
                    } );
                }

                // school
                string school = row.Field<string>( "school_name" );
                if ( school.IsNotNullOrWhitespace() )
                {
                    person.Attributes.Add( new PersonAttributeValue
                    {
                        AttributeKey = "F1School",
                        AttributeValue = school,
                        PersonId = person.Id
                    } );
                }

                // denomination
                string denomination = row.Field<string>( "FormerDenomination" );
                if ( denomination.IsNotNullOrWhitespace() )
                {
                    person.Attributes.Add( new PersonAttributeValue
                    {
                        AttributeKey = "Denomination",
                        AttributeValue = denomination,
                        PersonId = person.Id
                    } );
                }

                // former Church
                string formerChurch = row.Field<string>( "former_church" );
                if ( formerChurch.IsNotNullOrWhitespace() )
                {
                    person.Attributes.Add( new PersonAttributeValue
                    {
                        AttributeKey = "PreviousChurch",
                        AttributeValue = formerChurch,
                        PersonId = person.Id
                    } );
                }

                // Default Tag Comment
                string defaultTagComment = row.Field<string>( "Default_Tag_Comment" );
                if ( defaultTagComment.IsNotNullOrWhitespace() )
                {
                    person.Attributes.Add( new PersonAttributeValue
                    {
                        AttributeKey = "F1_Default_Tag_Comment",
                        AttributeValue = defaultTagComment,
                        PersonId = person.Id
                    } );
                }

                // Add Bar Code as Person Search Key
                string barcode = row.Field<string>( "Bar_Code" );
                if ( barcode.IsNotNullOrWhitespace() )
                {
                    person.PersonSearchKeys.Add( new PersonSearchKey
                    {
                        PersonId = person.Id,
                        SearchValue = barcode
                    } );
                }



                // Communications That aren't phone or email
                var communicationAttributeValues = dtCommunicationValues.Select( $"individual_id = { person.Id }" );

                foreach ( var attributeValue in communicationAttributeValues )
                {
                    string attributeName = attributeValue.Field<string>( "communication_type" );
                    var key = "F1" + attributeName.RemoveSpaces().RemoveSpecialCharacters();
                    string value = attributeValue.Field<string>( "communication_value" );

                    if ( !string.IsNullOrWhiteSpace( value ) )
                    {
                        person.Attributes.Add( new PersonAttributeValue
                        {
                            AttributeKey = key,
                            AttributeValue = value, // save as UTC date format
                            PersonId = person.Id
                        } );
                    }
                }


            }
            catch( Exception ex )
            {
                notes.Add( "ERROR in Export: " + ex.Message + ": " + ex.StackTrace );
            }

            // write out import notes
            if ( notes.Count > 0 )
            {
                person.Note = string.Join( ",", notes );
            }

            return person;
        }
    }
}
