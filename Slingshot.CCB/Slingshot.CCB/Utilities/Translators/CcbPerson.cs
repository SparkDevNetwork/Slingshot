using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Slingshot.Core;
using Slingshot.Core.Model;

namespace Slingshot.CCB.Utilities.Translators
{
    public static class CcbPerson
    {
        public static Person Translate(XElement inputPerson)
        {
            var person = new Person();
            var notes = new List<string>();

            if ( inputPerson.Attribute( "id" ) != null && inputPerson.Attribute( "id" ).Value.AsIntegerOrNull().HasValue )
            {
                person.Id = inputPerson.Attribute( "id" ).Value.AsInteger();

                // names
                if ( inputPerson.Element( "legal_first_name" ) != null && inputPerson.Element( "legal_first_name" ).Value.IsNotNullOrWhitespace() )
                {
                    person.FirstName = inputPerson.Element( "legal_first_name" ).Value;
                }
                else
                {
                    person.FirstName = inputPerson.Element( "first_name" )?.Value;
                }

                person.NickName = inputPerson.Element( "first_name" )?.Value;
                person.LastName = inputPerson.Element( "last_name" )?.Value;
                person.MiddleName = inputPerson.Element( "middle_name" )?.Value;

                person.Salutation = inputPerson.Element( "salutation" )?.Value;
                person.Suffix = inputPerson.Element( "suffix" )?.Value;

                // email
                person.Email = inputPerson.Element( "email" )?.Value;

                if ( inputPerson.Element( "receive_email_from_church" )?.Value == "false" ) {
                    person.EmailPreference = EmailPreference.NoMassEmails; // no mass emails
                }

                // phones
                var phoneList = inputPerson.Element( "phones" ).Elements( "phone" );
                foreach (var phone in phoneList )
                {
                    if ( phone.Value.IsNotNullOrWhitespace() )
                    {
                        var phoneType = phone.Attribute( "type" )?.Value;

                        switch ( phoneType )
                        {
                            case "home":
                                phoneType = "Home";
                                break;
                            case "mobile":
                                phoneType = "Mobile";
                                break;
                            case "contact":
                                phoneType = "Contact";
                                break;
                            case "work":
                                phoneType = "Work";
                                break;
                            case "emergency":
                                phoneType = "Emergency";
                                break;
                        }

                        person.PhoneNumbers.Add( new PersonPhone
                        {
                            PersonId = person.Id,
                            PhoneType = phoneType,
                            PhoneNumber = phone.Value
                        } );
                    }
                }

                // addresses
                var addressList = inputPerson.Element( "addresses" ).Elements( "address" );
                foreach( var address in addressList )
                {
                    if ( address.Element( "street_address" ) != null && address.Element( "street_address" ).Value.IsNotNullOrWhitespace() )
                    {
                        var importAddress = new PersonAddress();
                        importAddress.PersonId = person.Id;
                        importAddress.Street1 = address.Element( "street_address" ).Value;
                        importAddress.City = address.Element( "city" ).Value;
                        importAddress.State = address.Element( "state" ).Value;
                        importAddress.PostalCode = address.Element( "zip" ).Value;
                        importAddress.Latitude = address.Element( "latitude" )?.Value;
                        importAddress.Longitude = address.Element( "longitude" )?.Value;
                        importAddress.Country = address.Element( "country" )?.Value;

                        var addressType = address.Attribute( "type" ).Value;

                        switch ( addressType )
                        {
                            case "mailing":
                            case "home":
                                {
                                    importAddress.AddressType = AddressType.Home;
                                    break;
                                }
                            case "work":
                                {
                                    importAddress.AddressType = AddressType.Work;
                                    break;
                                }
                        }

                        // only add the address if we have a valid address
                        if ( importAddress.Street1.IsNotNullOrWhitespace() && importAddress.City.IsNotNullOrWhitespace() && importAddress.PostalCode.IsNotNullOrWhitespace() )
                        {
                            person.Addresses.Add( importAddress );
                        }
                    }
                }

                // gender
                var gender = inputPerson.Element( "gender" )?.Value;

                if (gender == "M" )
                {
                    person.Gender = Gender.Male;
                } else if (gender == "F" )
                {
                    person.Gender = Gender.Female;
                }

                // marital status
                var maritalStatus = inputPerson.Element( "marital_status" )?.Value;
                switch ( maritalStatus )
                {
                    case "Married":
                        person.MaritalStatus = MaritalStatus.Married;
                        break;
                    case "Single":
                        person.MaritalStatus = MaritalStatus.Single;
                        break;
                    default:
                        person.MaritalStatus = MaritalStatus.Unknown;
                        if ( maritalStatus.IsNotNullOrWhitespace() )
                        {
                            notes.Add( "maritial_status:" + maritalStatus );
                        }
                        break;
                }

                // connection status 
                var connectionStatus = inputPerson.Element( "membership_type" )?.Value;

                if ( connectionStatus.IsNotNullOrWhitespace() )
                {
                    // default to attendee - gotta provide something...
                    connectionStatus = "Attendee";
                }
                person.ConnectionStatus = connectionStatus;

                // record status
                person.RecordStatus = RecordStatus.Active;
                if ( inputPerson.Element( "active" )?.Value == "false")
                {
                    person.RecordStatus = RecordStatus.Inactive;
                }

                if ( inputPerson.Element( "deceased" ) != null && inputPerson.Element( "deceased" ).Value.IsNotNullOrWhitespace() )
                {
                    person.RecordStatus = RecordStatus.Inactive;
                    person.InactiveReason = "Deceased";
                }

                // dates
                person.Birthdate = inputPerson.Element( "birthday" )?.Value.AsDateTime();
                person.AnniversaryDate = inputPerson.Element( "anniversary" )?.Value.AsDateTime();
                person.CreatedDateTime = inputPerson.Element( "created" )?.Value.AsDateTime();
                person.ModifiedDateTime = inputPerson.Element( "modified" )?.Value.AsDateTime();


                // family
                person.FamilyId = inputPerson.Element( "family" )?.Attribute( "id" )?.Value.AsIntegerOrNull();

                if ( inputPerson.Element( "family_image" ) != null && !inputPerson.Element( "family_image" ).Value.Contains( "group-default-large.gif" ) )
                {
                    person.FamilyImageUrl = inputPerson.Element( "family_image" ).Value;
                }

                if ( inputPerson.Element( "family_position" )?.Value == "Primary Contact" || inputPerson.Element( "family_position" )?.Value == "Spouse" )
                {
                    person.FamilyRole = FamilyRole.Adult;
                }
                else if ( inputPerson.Element( "family_position" )?.Value == "Other" ) // add a note that the position was other
                {
                    person.FamilyRole = FamilyRole.Child;
                    notes.Add( "family_position:other" );
                }
                else if ( inputPerson.Element( "family_position" )?.Value == "Child" )
                {
                    person.FamilyRole = FamilyRole.Child;
                }
                                

                // photo
                if ( inputPerson.Element( "image" ) != null && !inputPerson.Element( "image" ).Value.Contains( "profile-default.gif" ) )
                {
                    person.PersonPhotoUrl = inputPerson.Element( "image" )?.Value;
                }

                // campus
                Campus campus = new Campus();
                person.Campus = campus;
                if ( inputPerson.Element( "campus" ) != null )
                {
                    campus.CampusName = inputPerson.Element( "campus" ).Value;
                    if ( inputPerson.Element( "campus" ).Attribute( "id" ) != null )
                    {
                        campus.CampusId = inputPerson.Element( "campus" ).Attribute( "id" ).Value.AsInteger();
                    }
                }

                //
                // attributes
                //

                // baptized
                ProcessBooleanAttribute( person, inputPerson.Element( "baptized" ), "IsBaptized" );

                // emergency contact name
                ProcessStringAttribute( person, inputPerson.Element( "emergency_contact_name" ), "EmergencyContactName" );

                // allergies
                ProcessStringAttribute( person, inputPerson.Element( "allergies" ), "Allergy" );

                // confirmed no allergies
                ProcessBooleanAttribute( person, inputPerson.Element( "confirmed_no_allergies" ), "ConfirmedNoAllergies" );

                // membership date
                ProcessDatetimeAttribute( person, inputPerson.Element( "membership_date" ), "MembershipDate" );

                // get custom fields
                // text fields
                foreach ( var textAttribute in inputPerson.Element( "user_defined_text_fields" )?.Elements( "user_defined_text_field" ) )
                {
                    if ( textAttribute.Element( "label" ).Value.IsNotNullOrWhitespace() && textAttribute.Element( "text" ).Value.IsNotNullOrWhitespace() )
                    {
                        person.Attributes.Add( new PersonAttributeValue
                        {
                            AttributeKey = textAttribute.Element( "name" ).Value,
                            AttributeValue = textAttribute.Element( "text" ).Value,
                            PersonId = person.Id
                        } );
                    }
                }

                // date fields
                foreach ( var dateAttribute in inputPerson.Element( "user_defined_date_fields" )?.Elements( "user_defined_date_field" ) )
                {
                    if ( dateAttribute.Element( "label" ).Value.IsNotNullOrWhitespace() && dateAttribute.Element( "date" ).Value.IsNotNullOrWhitespace() )
                    {
                        person.Attributes.Add( new PersonAttributeValue
                        {
                            AttributeKey = dateAttribute.Element( "name" ).Value,
                            AttributeValue = dateAttribute.Element( "date" ).Value.AsDateTime().ToString(),
                            PersonId = person.Id
                        } );
                    }
                }

                // dropdown fields
                /*foreach ( var dropdownAttribute in inputPerson.Element( "user_defined_pulldown_fields" )?.Elements( "user_defined_pulldown_fields" ) )
                {
                    if ( dropdownAttribute.Element( "label" ).Value.IsNotNullOrWhitespace() && dropdownAttribute.Element( "text" ).Value.IsNotNullOrWhitespace() ) // not certain this is the correct key as discovery church did not have any...
                    {
                        person.Attributes.Add( new PersonAttributeValue
                        {
                            AttributeKey = dropdownAttribute.Element( "name" ).Value,
                            AttributeValue = dropdownAttribute.Element( "text" ).Value, // not certain this is the correct key as discovery church did not have any...
                            PersonId = person.PersonId
                        } );
                    }
                }*/

                // write out person notes
                if ( notes.Count > 0 )
                {
                    person.Note = string.Join( ",", notes );
                }

                return person;
            }

            return null;
        }

        /// <summary>
        /// Processes the string attribute.
        /// </summary>
        /// <param name="person">The person.</param>
        /// <param name="element">The element.</param>
        /// <param name="attributeKey">The attribute key.</param>
        private static void ProcessStringAttribute( Person person, XElement element, string attributeKey )
        {
            if ( element != null && element.Value.IsNotNullOrWhitespace() )
            {
                person.Attributes.Add( new PersonAttributeValue { PersonId = person.Id, AttributeKey = attributeKey, AttributeValue = element.Value } );
            }
        }

        /// <summary>
        /// Processes the boolean attribute.
        /// </summary>
        /// <param name="person">The person.</param>
        /// <param name="element">The element.</param>
        /// <param name="attributeKey">The attribute key.</param>
        private static void ProcessBooleanAttribute( Person person, XElement element, string attributeKey )
        {
            if ( element != null && element.Value.IsNotNullOrWhitespace() )
            {
                person.Attributes.Add( new PersonAttributeValue { PersonId = person.Id, AttributeKey = attributeKey, AttributeValue = element.Value.AsBoolean().ToString() } );
            }
        }

        /// <summary>
        /// Processes the datetime attribute.
        /// </summary>
        /// <param name="person">The person.</param>
        /// <param name="element">The element.</param>
        /// <param name="attributeKey">The attribute key.</param>
        private static void ProcessDatetimeAttribute( Person person, XElement element, string attributeKey )
        {
            if ( element != null && element.Value.AsDateTime().HasValue )
            {
                person.Attributes.Add( new PersonAttributeValue { PersonId = person.Id, AttributeKey = attributeKey, AttributeValue = element.Value.AsDateTime().ToString() } );
            }
        }
    }
}
