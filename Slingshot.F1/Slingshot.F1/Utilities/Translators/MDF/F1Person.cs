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
            , DataTable Addresses
            , DataTable Communications
            , DataRow[] HeadOfHouseHolds
            , DataTable dtRequirementValues
            , DataTable dtAttributeValues 
            , DataTable dtCommunicationValues )
        {
            var person = new Person();
            var notes = new List<string>();
            try
            {

                var houseHouldId = row.Field<int>( "household_id" );

                // person id
                int? personId = row.Field<int?>( "individual_id" );
                if ( personId.HasValue )
                {
                    person.Id = personId.Value;
                }

                // names
                string firstName = row.Field<string>( "first_name" );
                if ( firstName.IsNotNullOrWhitespace() )
                {
                    person.FirstName = firstName;
                }

                string nickName = row.Field<string>( "goes_by" );
                if ( nickName.IsNotNullOrWhitespace() )
                {
                    person.NickName = nickName;
                }

                string middleName = row.Field<string>( "middle_name" );
                if ( middleName.IsNotNullOrWhitespace() )
                {
                    person.MiddleName = middleName;
                }

                string lastName = row.Field<string>( "last_name" );
                if ( lastName.IsNotNullOrWhitespace() )
                {
                    person.LastName = lastName;
                }

                string salutation = row.Field<string>( "prefix" );
                if ( salutation.IsNotNullOrWhitespace() )
                {
                    person.Salutation = salutation;
                }

                string suffix = row.Field<string>( "suffix" );
                if ( suffix.IsNotNullOrWhitespace() )
                {
                    person.Suffix = suffix;
                }

                string email = null;
                // email
                var emailrow = Communications.Select( "individual_id = " +  person.Id + " AND communication_type = 'Email'" ).FirstOrDefault();
                if( emailrow != null )
                {
                    email = emailrow.Field<string>( "communication_value" );
                }
                   
                if ( email.IsNotNullOrWhitespace() )
                {
                    person.Email = email;
                }

                var numbers = Communications.Select( "individual_id = " + person.Id + " AND ( communication_type = 'Mobile' OR communication_type like '%Phone%' )" );

                if( numbers != null )
                {
                    foreach ( var number in numbers.ToList() )
                    {
                        string phoneType = number.Field<string>( "communication_type" ).Replace( "Phone", "" ).Replace( "phone", "" ).Trim();
                        string phoneNumber = new string( number.Field<string>( "communication_value" ).Where( c => char.IsDigit( c ) ).ToArray() );
                        if ( !string.IsNullOrWhiteSpace( phoneNumber ) )
                        {
                            person.PhoneNumbers.Add( new PersonPhone
                            {
                                PersonId = person.Id,
                                PhoneType = phoneType,
                                PhoneNumber = phoneNumber
                            } );
                        }
                    }
                }

                var myAddresses = Addresses.Select( "household_id = " + houseHouldId + " OR individual_id = " + person.Id );

                if ( myAddresses != null )
                {
                    foreach ( var address in myAddresses.ToList() )
                    {
                        var importAddress = new PersonAddress();
                        importAddress.PersonId = person.Id;
                        importAddress.Street1 = address.Field<string>( "address_1" );
                        importAddress.Street2 = address.Field<string>( "address_2" );
                        importAddress.City = address.Field<string>( "city" );
                        importAddress.State = address.Field<string>( "state" );
                        importAddress.PostalCode = address.Field<string>( "zip_code" );
                        importAddress.Country = address.Field<string>( "country" );

                        var addressType = address.Field<string>( "address_type" );
                        switch ( addressType )
                        {
                            case "Primary":
                                {
                                    importAddress.AddressType = AddressType.Home;
                                    importAddress.IsMailing = true;
                                    break;
                                }
                            case "Previous":
                                {
                                    importAddress.AddressType = AddressType.Previous;
                                    break;
                                }
                            case "Business":
                                {
                                    importAddress.AddressType = AddressType.Work;
                                    break;
                                }
                            case "Mail Returned / Incorrect":
                                {
                                    notes.Add( "Mail Returned From Address: " + importAddress.Street1 );
                                    importAddress.AddressType = AddressType.Other;
                                    break;
                                }
                            default:
                                {
                                    importAddress.AddressType = AddressType.Other;
                                    break;
                                }
                        }

                        // only add the address if we have a valid address
                        if ( importAddress.Street1.IsNotNullOrWhitespace() &&
                                importAddress.City.IsNotNullOrWhitespace() &&
                                importAddress.PostalCode.IsNotNullOrWhitespace() )
                        {
                            person.Addresses.Add( importAddress );

                        }
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
                person.FamilyId = houseHouldId;

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
                            var hashed = md5Hasher.ComputeHash( Encoding.UTF8.GetBytes( person.FirstName + person.NickName + person.MiddleName + person.LastName ) );
                            var familyId = Math.Abs( BitConverter.ToInt32( hashed, 0 ) ); // used abs to ensure positive number
                            if ( familyId > 0 )
                            {
                                person.FamilyId = familyId;
                            }
                        }
                        notes.Add( "F1 Visitor of the " + person.FamilyName + "(" + person.FamilyId + ")" );
                        break;
                    default:
                        person.FamilyRole = FamilyRole.Child;
                        notes.Add( "Family Postion: Other" );
                        break;
                }

                // campus
                Campus campus = new Campus();
                person.Campus = campus;

                // Family members of the same family can have different campuses in F1 and Slingshot will set the family campus to the first family
                // member it see. To be consistent, we'll use the head of household's campus for the whole family.
                var headOfHousehold = HeadOfHouseHolds.Where( x => x.Field<int>("household_id") ==  person.FamilyId ).FirstOrDefault();


                if ( headOfHousehold != null )
                {
                    campus.CampusName = headOfHousehold.Field<string>( "SubStatus_Name" );
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

                // person attributes

                var attributes = dtAttributeValues.Select( "individual_id = " + person.Id );

                foreach ( var attribute in attributes )
                {

                    int attributeId = attribute.Field<int>( "Attribute_Id" );
                    string attributeName = attribute.Field<string>( "Attribute_Name" );
                    // Add the attribute value for start date (if not empty) 
                    var startDateAttributeKey = attributeId + "_" + attributeName.RemoveSpaces().RemoveSpecialCharacters() + "StartDate";
                    DateTime? startDate = attribute.Field<DateTime?>( "Start_Date" );

                    if ( startDate.HasValue )
                    {
                        person.Attributes.Add( new PersonAttributeValue
                        {
                            AttributeKey = startDateAttributeKey,
                            AttributeValue = startDate.Value.ToString( "o" ), // save as UTC date format
                            PersonId = person.Id
                        } );
                    }

                    // Add the attribute value for end date (if not empty) 
                    var endDateAttributeKey = attributeId + "_" + attributeName.RemoveSpaces().RemoveSpecialCharacters() + "EndDate";
                    DateTime? endDate = attribute.Field<DateTime?>( "End_Date" );

                    if ( endDate.HasValue )
                    {
                        person.Attributes.Add( new PersonAttributeValue
                        {
                            AttributeKey = endDateAttributeKey,
                            AttributeValue = endDate.Value.ToString( "o" ), // save as UTC date format
                            PersonId = person.Id
                        } );
                    }

                    // Add the attribute value for comment (if not empty) 
                    var commentAttributeKey = attributeId + "_" + attributeName.RemoveSpaces().RemoveSpecialCharacters() + "Comment";
                    string comment = attribute.Field<string>( "comment" );

                    if ( comment.IsNotNullOrWhitespace() )
                    {
                        person.Attributes.Add( new PersonAttributeValue
                        {
                            AttributeKey = commentAttributeKey,
                            AttributeValue = comment,
                            PersonId = person.Id
                        } );
                    }
                    // If the attribute exists but we do not have any values assigned (comment, start date, end date)
                    // then set the value to true so that we know the attribute exists.
                    else if ( !comment.IsNotNullOrWhitespace() && !startDate.HasValue && !startDate.HasValue )
                    {
                        person.Attributes.Add( new PersonAttributeValue
                        {
                            AttributeKey = commentAttributeKey,
                            AttributeValue = "True",
                            PersonId = person.Id
                        } );
                    }

                }

                // person requirements. 
                var requirements = dtRequirementValues.Select( "individual_id = " + person.Id );
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
                        AttributeKey = "School",
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

                // former Church
                string barcode = row.Field<string>( "Bar_Code" );
                if ( barcode.IsNotNullOrWhitespace() )
                {
                    person.Attributes.Add( new PersonAttributeValue
                    {
                        AttributeKey = "BarCode",
                        AttributeValue = barcode,
                        PersonId = person.Id
                    } );
                }

                // Communications That aren't phone or email
                var communicationAttributeValues = dtCommunicationValues.Select( "individual_id = " + person.Id );

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
            catch(Exception ex)
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
