using Slingshot.Core;
using Slingshot.Core.Model;
using Slingshot.PCO.Models.DTO;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Slingshot.PCO.Utilities.Translators
{
    public static class PCOImportPerson
    {
        public static Person Translate( PersonDTO inputPerson, List<FieldDefinitionDTO> personAttributes, PersonDTO headOfHouse, PersonDTO backgroundCheckPerson )
        {
            if ( inputPerson.Id <= 0 )
            {
                return null;
            }

            var person = new Core.Model.Person
            {
                Id = inputPerson.Id,
                Salutation = inputPerson.GetSalutation(),
                FirstName = inputPerson.FirstName,
                NickName = inputPerson.Nickname,
                MiddleName = inputPerson.MiddleName,
                LastName = inputPerson.LastName,
                Suffix = inputPerson.GetSuffix(),
                Gender = GetGender( inputPerson ),
                ConnectionStatus = inputPerson.Member,
                RecordStatus = ( inputPerson.Status == "active" ) ? RecordStatus.Active : RecordStatus.Inactive,
                PhoneNumbers = inputPerson.GetPhoneNumbers(),
                Email = inputPerson.GetEmail(),
                Addresses = inputPerson.GetAddresses(),
                MaritalStatus = inputPerson.GetMaritalStatus(),
                Birthdate = inputPerson.Birthdate,
                AnniversaryDate = inputPerson.Anniversary,
                CreatedDateTime = inputPerson.CreatedAt,
                ModifiedDateTime = inputPerson.UpdatedAt,
                FamilyId = inputPerson.Household?.Id,
                FamilyName = inputPerson.Household?.Name,
                FamilyRole = ( inputPerson.Child == true ) ? FamilyRole.Child : FamilyRole.Adult,
                InactiveReason = inputPerson.InactiveReason,
                Campus = headOfHouse.GetCampus(),
                PersonSearchKeys = inputPerson.GetSearchKeys(),
                Attributes = inputPerson.GetAttributes( personAttributes, backgroundCheckPerson ),
                Note = string.Join( ",", inputPerson.GetNotes() )
            };

            return person;
        }

        #region Translation Logic

        private static Gender GetGender( this PersonDTO inputPerson )
        {
            if ( inputPerson.Gender == "M" || inputPerson.Gender == "Male" )
            {
                return Gender.Male;
            }
            else if ( inputPerson.Gender == "F" || inputPerson.Gender == "Female" )
            {
                return Gender.Female;
            }

            return Gender.Unknown;
        }

        private static string GetSalutation( this PersonDTO inputPerson )
        {
            if ( !string.IsNullOrWhiteSpace( inputPerson.NamePrefix ) )
            {
                return System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase( inputPerson.NamePrefix.ToLower() );
            }

            return string.Empty;
        }

        private static string GetSuffix( this PersonDTO inputPerson )
        {
            if ( !string.IsNullOrWhiteSpace( inputPerson.NameSuffix ) )
            {
                if ( inputPerson.NameSuffix.Equals( "Sr.", StringComparison.OrdinalIgnoreCase ) )
                {
                    return "Sr.";
                }
                else if ( inputPerson.NameSuffix.Equals( "Jr.", StringComparison.OrdinalIgnoreCase ) )
                {
                    return "Jr.";
                }
                else if ( inputPerson.NameSuffix.Equals( "Ph.D.", StringComparison.OrdinalIgnoreCase ) )
                {
                    return "Ph.D.";
                }
                else
                {
                    return inputPerson.NameSuffix;
                }
            }

            return string.Empty;
        }

        private static List<PersonPhone> GetPhoneNumbers( this PersonDTO inputPerson )
        {
            var phones = new List<PersonPhone>();
            foreach ( var number in inputPerson.ContactData.PhoneNumbers )
            {
                phones.Add( new PersonPhone
                {
                    PersonId = inputPerson.Id,
                    PhoneType = number.Location,
                    PhoneNumber = number.Number
                } );
            }

            return phones;
        }

        private static string GetEmail( this PersonDTO inputPerson )
        {
            string emailAddress = string.Empty;
            foreach ( var email in inputPerson.ContactData.EmailAddresses )
            {
                if ( email.Primary || emailAddress.IsNullOrWhiteSpace() )
                {
                    emailAddress = email.Address;
                }
            }

            return emailAddress;
        }

        private static List<PersonSearchKey> GetSearchKeys(this PersonDTO inputPerson)
        {
            var primaryEmail = GetEmail( inputPerson );

            var searchKeys = new List<PersonSearchKey>();
            foreach (var email in inputPerson.ContactData.EmailAddresses)
            {
                if (email.Address != primaryEmail)
                {
                    searchKeys.Add(new PersonSearchKey
                    {
                        PersonId = inputPerson.Id,
                        SearchValue = email.Address
                    });
                }
            }

            return searchKeys;
        }

        private static List<PersonAddress> GetAddresses( this PersonDTO inputPerson )
        {
            var addresses = new List<PersonAddress>();


            foreach ( var address in inputPerson.ContactData.Addresses )
            {
                var addressType = AddressType.Other;
                switch ( address.Location )
                {
                    case "Home":
                        addressType = AddressType.Home;
                        break;
                    case "Previous":
                        addressType = AddressType.Previous;
                        break;
                    case "Work":
                        addressType = AddressType.Work;
                        break;
                    case "Other":
                        addressType = AddressType.Other;
                        break;
                }

                var importAddress = new PersonAddress
                {
                    PersonId = inputPerson.Id,
                    Street1 = address.Street ?? string.Empty, // Null is not an acceptable value in this field.
                    City = address.City ?? string.Empty,
                    State = address.State ?? string.Empty,
                    PostalCode = address.Zip ?? string.Empty, // Null is not an acceptable value in this field.
                    AddressType = addressType
                };

                /*
                 * Shaun Cummings - 10/14/20
                 * 
                 * Addresses cannot be added to this collection unless they have a value in Street1 and PostalCode or else they will
                 * create a NullReference exception in Slingshot.Core\Utilities\ImportPackage.cs at line 367 (this is due to using the
                 * .Equals() method directly on the property, which assumes the property is not null).
                 * 
                 * */

                addresses.Add( importAddress );
            }

            return addresses;
        }

        private static MaritalStatus GetMaritalStatus( this PersonDTO inputPerson )
        {
            switch ( inputPerson.MaritalStatus )
            {
                case "Married":
                    return MaritalStatus.Married;
                case "Single":
                    return MaritalStatus.Single;
                case "Divorced":
                    return MaritalStatus.Divorced;
                default:
                    break;
            }

            return MaritalStatus.Unknown;
        }

        private static List<string> GetNotes( this PersonDTO inputPerson )
        {
            var notes = new List<string>();

            if ( inputPerson.GetMaritalStatus() == MaritalStatus.Unknown && inputPerson.MaritalStatus.IsNotNullOrWhitespace() )
            {
                notes.Add( "PCO Marital Status: " + inputPerson.MaritalStatus );
            }

            return notes;
        }

        private static List<PersonAttributeValue> GetAttributes( this PersonDTO inputPerson, List<FieldDefinitionDTO> personAttributes, PersonDTO servicePerson )
        {
            var attributeList = new List<PersonAttributeValue>();
            
            // Make sure there are no duplicate entries for a given field
            var attributes = inputPerson.FieldData
                                .GroupBy( a => a.FieldDefinitionId )
                                .Select( g => g.First() )
                                .ToList();

            var usedAttributeKeys = new List<string>();

            foreach ( var attribute in attributes )
            {
                var fieldDefinition = personAttributes.Where( f => f.Id == attribute.FieldDefinitionId ).FirstOrDefault();
                if ( fieldDefinition != null )
                {
                    attributeList.Add( new PersonAttributeValue
                    {
                        AttributeKey = fieldDefinition.Id + "_" + fieldDefinition.Slug,
                        AttributeValue = attribute.Value,
                        PersonId = inputPerson.Id
                    } );
                }
            }
                
            // Make sure there are no duplicate entries
            var profiles = inputPerson.SocialProfiles
                .GroupBy( a => a.Site )
                .Select( g => g.First() )
                .ToList();

            // add social profiles to attributes
            foreach ( var profile in profiles )
            {
                if( profile.Url.IsNotNullOrWhitespace() )
                {
                    attributeList.Add( new PersonAttributeValue
                    {
                        AttributeKey = profile.Site,
                        AttributeValue = profile.Url,
                        PersonId = inputPerson.Id
                    } );
                }
            }

            // Add School Attribute
            if ( !string.IsNullOrWhiteSpace( inputPerson.School ) )
            {
                attributeList.Add( new PersonAttributeValue
                {
                    AttributeKey = "School",
                    AttributeValue = inputPerson.School,
                    PersonId = inputPerson.Id
                } );
            }

            if ( inputPerson.RemoteId.HasValue )
            {
                attributeList.Add( new PersonAttributeValue
                {
                    AttributeKey = "RemoteId",
                    AttributeValue = inputPerson.RemoteId.Value.ToString(),
                    PersonId = inputPerson.Id
                } );
            }

            // Add background check attribute, if appropriate.
            var backgroundCheckAttribute = inputPerson.GetBackgroundCheckResult( servicePerson );
            if ( backgroundCheckAttribute != null )
            {
                attributeList.Add( backgroundCheckAttribute );
            }

            return attributeList;
        }

        private static Campus GetCampus( this PersonDTO HeadOfHouse )
        {
            if ( HeadOfHouse.Campus != null )
            {
                return new Campus
                {
                    CampusId = HeadOfHouse.Campus.Id,
                    CampusName = HeadOfHouse.Campus.Name
                };
            }

            return null;
        }

        private static PersonAttributeValue GetBackgroundCheckResult( this PersonDTO inputPerson, PersonDTO backgroundCheckPerson )
        {
            if ( backgroundCheckPerson == null || !backgroundCheckPerson.PassedBackgroundCheck.HasValue )
            {
                return null;
            }

            var result = ( backgroundCheckPerson.PassedBackgroundCheck.Value ) ? "Pass" : "Fail";
            return new PersonAttributeValue
            {
                AttributeKey = "BackgroundCheckResult",
                AttributeValue = result,
                PersonId = inputPerson.Id
            };
        }

        #endregion Translation Logic
    }
}
