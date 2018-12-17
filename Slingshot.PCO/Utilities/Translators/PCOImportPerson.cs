using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Xml.Linq;
using Slingshot.Core;
using Slingshot.Core.Model;
using Slingshot.PCO.Models;

namespace Slingshot.PCO.Utilities.Translators
{
    public static class PCOImportPerson
    {
        public static Person Translate( PCOPerson inputPerson, List<PCOFieldDefinition> personAttributes, PCOPerson HeadOfHouse )
        {
            var person = new Core.Model.Person();
            var notes = new List<string>();

            if ( inputPerson.id > 0 )
            {
                person.Id = inputPerson.id;

                // names
                person.FirstName = inputPerson.first_name;
                person.NickName = inputPerson.nickname;
                person.MiddleName = inputPerson.middle_name;
                person.LastName = inputPerson.last_name;

                
                if ( !string.IsNullOrWhiteSpace( inputPerson.name_prefix ) )
                {
                    person.Salutation = System
                                        .Threading
                                        .Thread
                                        .CurrentThread
                                        .CurrentCulture
                                        .TextInfo
                                        .ToTitleCase( inputPerson.name_prefix.ToLower() );
                }

                if( !string.IsNullOrWhiteSpace( inputPerson.name_suffix ) )
                {
                    if ( inputPerson.name_suffix.Equals( "Sr.", StringComparison.OrdinalIgnoreCase ) )
                    {
                        person.Suffix = "Sr.";
                    }
                    else if ( inputPerson.name_suffix.Equals( "Jr.", StringComparison.OrdinalIgnoreCase ) )
                    {
                        person.Suffix = "Jr.";
                    }
                    else if ( inputPerson.name_suffix.Equals( "Ph.D.", StringComparison.OrdinalIgnoreCase ) )
                    {
                        person.Suffix = "Ph.D.";
                    }
                    else
                    {
                        person.Suffix = inputPerson.name_suffix;
                    }
                }
                
                // communcations (phone & email)
                foreach ( var number in inputPerson.contact_data.phone_numbers )
                {
                    person.PhoneNumbers.Add( new PersonPhone
                    {
                        PersonId = person.Id,
                        PhoneType = number.location,
                        PhoneNumber = number.number
                    } );
                }

                foreach ( var email in inputPerson.contact_data.email_addresses )
                {
                    person.Email = email.address;
                }

                
                // addresses
                foreach ( var address in inputPerson.contact_data.addresses )
                {
                        var importAddress = new PersonAddress();
                        importAddress.PersonId = person.Id;
                        importAddress.Street1 = address.street;
                        importAddress.City = address.city;
                        importAddress.State = address.state;
                        importAddress.PostalCode = address.zip;

                        var addressType = address.location;

                        switch ( addressType )
                        {
                            case "Home":
                                importAddress.AddressType = AddressType.Home;
                                break;
                            case "Previous":
                                importAddress.AddressType = AddressType.Previous;
                                break;
                            case "Work":
                                importAddress.AddressType = AddressType.Work;
                                break;
                            case "Other":
                                importAddress.AddressType = AddressType.Other;
                                break;

                        }

                        person.Addresses.Add( importAddress );

                }

                // gender
                var gender = inputPerson.gender;

                if ( gender == "M" || gender == "Male" )
                {
                    person.Gender = Gender.Male;
                }
                else if ( gender == "F" || gender == "Female" )
                {
                    person.Gender = Gender.Female;
                }
                else
                {
                    person.Gender = Gender.Unknown;
                }

            // marital status
            switch ( inputPerson.marital_status )
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
                    if ( inputPerson.marital_status.IsNotNullOrWhitespace() )
                    {
                        notes.Add( "maritalStatus: " + inputPerson.marital_status );
                    }
                    break;
            }
            

            // connection status
                person.ConnectionStatus = inputPerson.member;

                // record status
                if ( inputPerson.status == "active" )
                {
                    person.RecordStatus = RecordStatus.Active;
                }
                else
                {
                    person.RecordStatus = RecordStatus.Inactive;
                }

                // dates
                person.Birthdate = inputPerson.birthdate;
                person.AnniversaryDate = inputPerson.anniversary;
                person.CreatedDateTime = inputPerson.created_at;
                person.ModifiedDateTime = inputPerson.updated_at;

                // family
              
                if( inputPerson.household != null )
                {
                    person.FamilyId = inputPerson.household.Id;
                }
                

                if ( inputPerson.child.HasValue && inputPerson.child.Value )
                {
                    person.FamilyRole = FamilyRole.Child;
                }
                else
                {
                    person.FamilyRole = FamilyRole.Adult;
                }

                if( HeadOfHouse.campus != null)
                {
                    person.Campus = new Campus
                    {
                        CampusId = HeadOfHouse.campus.id,
                        CampusName = HeadOfHouse.campus.name
                    };
                }

                // person attributes
                // Make sure there are no duplicate entries for a given field
                var attributes = inputPerson.field_data
                                    .GroupBy( a => a.field_definition_id )
                                    .Select( g => g.First() )
                                    .ToList();
                var usedAttributeKeys = new List<string>();

                foreach ( var attribute in attributes )
                {
                    var field_definition = personAttributes.Where( f => f.id == attribute.field_definition_id ).FirstOrDefault();
                    if ( field_definition != null )
                    {
                        person.Attributes.Add( new PersonAttributeValue
                        {
                            AttributeKey = field_definition.id + "_" + field_definition.slug,
                            AttributeValue = attribute.value,
                            PersonId = person.Id
                        } );
                    }
                }
                
                // Make sure there are no duplicate entries
                var profiles = inputPerson.socialProfiles
                                    .GroupBy( a => a.site )
                                    .Select( g => g.First() )
                                    .ToList();
                // add social profiles to attributes
                foreach ( var profile in profiles )
                {
                    if( profile.url.IsNotNullOrWhitespace() )
                    {
                        person.Attributes.Add( new PersonAttributeValue
                        {
                            AttributeKey = profile.site,
                            AttributeValue = profile.url,
                            PersonId = person.Id
                        } );
                    }
                }

                // Add School Attribute
                if( !string.IsNullOrWhiteSpace( inputPerson.school ) )
                {
                    person.Attributes.Add( new PersonAttributeValue
                    {
                        AttributeKey = "School",
                        AttributeValue = inputPerson.school,
                        PersonId = person.Id
                    } );
                }

                // write out person notes
                if ( notes.Count() > 0 )
                {
                    person.Note = string.Join( ",", notes );
                }



            }

            return person;
        }

        public static Person AddBackgroundCheckResult( Person person, PCOPerson pcoPerson )
        {
            if ( pcoPerson.passed_background_check.HasValue )
            {

                string value;
                if( pcoPerson.passed_background_check.Value )
                {
                    value = "Pass";
                }
                else
                {
                    value = "Fail";
                }
                person.Attributes.Add( new PersonAttributeValue
                {
                    AttributeKey = "BackgroundCheckResult",
                    AttributeValue = value,
                    PersonId = person.Id
                } );
            }
            return person;
        }
    }
}
