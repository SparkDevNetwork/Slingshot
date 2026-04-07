using System;
using System.Collections.Generic;
using System.Data;
using System.Security.Cryptography;
using System.Text;

using Slingshot.Core;
using Slingshot.Core.Model;

namespace Slingshot.ACS.Utilities.Translators
{
    public static class AcsPerson
    {
        public static Person Translate( DataRow row, string campusKey )
        {
            var person = new Person();
            var notes = new List<string>();

            // person id
            int? personId = row.Field<int?>( "IndividualId" );
            if ( personId != null )
            {
                person.Id = personId.Value;
            }

            // names
            string firstName = row.Field<string>( "FirstName" );
            if ( firstName.IsNotNullOrWhitespace() )
            {
                person.FirstName = firstName;
            }

            string nickName = row.Field<string>( "GoesByName" );
            if ( nickName.IsNotNullOrWhitespace() )
            {
                person.NickName = nickName;
            }

            string middleName = row.Field<string>( "MiddleName" );
            if ( middleName.IsNotNullOrWhitespace() )
            {
                person.MiddleName = middleName;
            }

            string lastName = row.Field<string>( "LastName" );
            if ( lastName.IsNotNullOrWhitespace() )
            {
                person.LastName = lastName;
            }

            string salutation = row.Field<string>( "Title" );
            if ( salutation.IsNotNullOrWhitespace() )
            {
                person.Salutation = salutation;
            }

            string suffix = row.Field<string>( "Suffix" );
            if ( suffix.IsNotNullOrWhitespace() )
            {
                person.Suffix = suffix;
            }

            // email
            string email = row.Field<string>( "EmailAddr" );
            if ( email.IsNotNullOrWhitespace() )
            {
                person.Email = email;
            }

            // phones - the People table contains a home & preferred number but there is
            //  also a Phones table that includes other phone numbers as well.  
            var homePhone = row.Field<string>( "HomePhone" );
            
            if (!String.IsNullOrWhiteSpace( homePhone ) )
            {
                person.PhoneNumbers.Add( new PersonPhone
                {
                    PersonId = person.Id,
                    PhoneType = "Home",
                    PhoneNumber = homePhone
                } );
            }

            var preferredPhone = row.Field<string>( "PreferredPhone" );

            if ( !String.IsNullOrWhiteSpace( preferredPhone ) )
            {
                person.PhoneNumbers.Add( new PersonPhone
                {
                    PersonId = person.Id,
                    PhoneType = "Preferred",
                    PhoneNumber = preferredPhone
                } );
            }

            // addresses
            var importAddress = new PersonAddress();
            importAddress.PersonId = person.Id;
            importAddress.Street1 = row.Field<string>( "Address1" );
            importAddress.Street2 = row.Field<string>( "Address2" );
            importAddress.City = row.Field<string>( "City" );
            importAddress.State = row.Field<string>( "State" );
            importAddress.PostalCode = row.Field<string>( "ZIPCode" );
            importAddress.Country = row.Field<string>( "Country" );

            var addressType = row.Field<string>( "AddressType" );
            switch ( addressType )
            {
                case "Home":
                    {
                        importAddress.AddressType = AddressType.Home;
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
            }

            // marital status
            string maritalStatus = row.Field<string>( "MaritalStatus" );

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
                    notes.Add( "Marital Status: " + maritalStatus );
                    break;
            }

            // connection status
            string connectionStatus = row.Field<string>( "MemberStatus" );
            if ( connectionStatus.IsNotNullOrWhitespace() )
            {
                person.ConnectionStatus = connectionStatus;
            }

            // record status
            string recordStatus = row.Field<string>( "ActiveRecord" );
            if ( recordStatus.IsNotNullOrWhitespace() )
            {
                switch ( recordStatus )
                {
                    case "A":
                        person.RecordStatus = RecordStatus.Active;
                        break;
                    default:
                        person.RecordStatus = RecordStatus.Inactive;
                        break;
                }
            }

            // gives individually
            string contribRecordType = row.Field<string>( "ContribRecordType" );
            switch ( contribRecordType )
            {

                case "Combined":
                    person.GiveIndividually = false;
                    break;
                case "Individual":
                    person.GiveIndividually = true;
                    break;
                default:
                    person.GiveIndividually = true;
                    break;
            }         

            // dates
            person.Birthdate = row.Field<string>( "DateOfBirth" ).AsDateTime();
            person.CreatedDateTime = row.Field<DateTime?>( "EntryDate" );
            person.ModifiedDateTime = row.Field<DateTime?>( "DateLastChanged" );

            // family
            int? familyId = row.Field<string>( "FamilyNumber" ).AsIntegerOrNull();
            if ( familyId != null )
            {
                person.FamilyId = familyId;
            }

            string familyName = row.Field<string>( "FamilyLabelName" );
            if ( familyName.IsNotNullOrWhitespace() )
            {
                person.FamilyName = familyName;
            }

            string familyRole = row.Field<string>( "FamilyPosition" );

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
                default:
                    person.FamilyRole = FamilyRole.Child;
                    notes.Add( "Family Position: Other" );
                    break;
            }

            // campus - There isn't a built in campus in ACS, so typically an open 
            //    field is used.  We will use the selected campus field from the 
            //    settings screen (if one is selected) and treat that as the campus.
            Campus campus = new Campus();
            person.Campus = campus;

            if ( row.Table.Columns.Contains( campusKey ) )
            {
                string campusName = row.Field<string>( campusKey );
                if ( campusName.IsNotNullOrWhitespace() )
                {
                    campus.CampusName = campusName;

                    // generate a unique campus id
                    MD5 md5Hasher = MD5.Create();
                    var hashed = md5Hasher.ComputeHash( Encoding.UTF8.GetBytes( campusName ) );
                    var campusId = Math.Abs( BitConverter.ToInt32( hashed, 0 ) ); // used abs to ensure positive number
                    if ( campusId > 0 )
                    {
                        campus.CampusId = campusId;
                    }
                }
            }            

            // person attributes

            // membership date
            var membershipDate = row.Field<DateTime?>( "DateJoined" );
            if ( membershipDate != null )
            {
                person.Attributes.Add( new PersonAttributeValue
                {
                    AttributeKey = "MembershipDate",
                    AttributeValue = membershipDate.Value.ToString( "o" ),
                    PersonId = person.Id
                } );
            }

            // envelope number
            var envelopeNumber = row.Field<int?>( "EnvelopeNumber" );
            if ( envelopeNumber.HasValue )
            {
                person.Attributes.Add( new PersonAttributeValue
                {
                    AttributeKey = "core_GivingEnvelopeNumber",
                    AttributeValue = envelopeNumber.Value.ToString(),
                    PersonId = person.Id
                } );
            }

            // loop through any attributes found
            foreach ( var attrib in AcsApi.PersonAttributes )
            {
                string value;

                if ( attrib.Value == "String" )
                {
                    value = row.Field<string>( attrib.Key );
                }
                else if ( attrib.Value == "DateTime" )
                {
                    var datetime = row.Field<DateTime?>( attrib.Key );
                    if ( datetime.HasValue )
                    {
                        value = datetime.Value.ToString( "o" );
                    }
                    else
                    {
                        value = "";
                    }
                }
                else
                {
                    value = null;
                }
               
                if ( value.IsNotNullOrWhitespace() )
                {
                    person.Attributes.Add( new PersonAttributeValue
                    {
                        AttributeKey = attrib.Key,
                        AttributeValue = value.ToString(),
                        PersonId = person.Id
                    } );
                }
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
