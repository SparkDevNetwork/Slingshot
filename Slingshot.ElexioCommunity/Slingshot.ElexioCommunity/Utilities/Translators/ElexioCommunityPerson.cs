using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using Slingshot.Core;
using Slingshot.Core.Model;
using Slingshot.Core.Utilities;

namespace Slingshot.ElexioCommunity.Utilities.Translators
{
    public static class ElexioCommunityPerson
    {
        public static Person Translate( dynamic importPerson )
        {
            var person = new Person();

            person.Id = importPerson.uid;
            person.FirstName = importPerson.fname;
            person.NickName = importPerson.preferredName;
            person.LastName = importPerson.lname;
            person.Email = importPerson.mail;

            // secondary email
            string secondEmail = importPerson.secondaryEmail;
            if ( secondEmail.IsNotNullOrWhitespace() )
            {
                PersonNote emailNote = new PersonNote();
                emailNote.PersonId = person.Id;
                emailNote.NoteType = "Secondary Email";
                emailNote.Text = secondEmail;

                // offset the Id by 1 million so that these notes do not overlap.
                emailNote.Id = 1000000 + person.Id;

                ImportPackage.WriteToPackage( emailNote );
            }

            // gender
            if ( importPerson.male == "1" )
            {
                person.Gender = Gender.Male;
            }
            else
            {
                person.Gender = Gender.Female;
            }

            // marital status is not a built in field
            person.MaritalStatus = MaritalStatus.Unknown;

            // connection status is not a built in field
            person.ConnectionStatus = "Unknown";

            // family
            var _client = new RestClient( ElexioCommunityApi.ApiUrl );
            var _request = new RestRequest( ElexioCommunityApi.API_INDIVIDUAL + person.Id.ToString(), Method.GET );
            _request.AddQueryParameter( "session_id", ElexioCommunityApi.SessionId );
            var response = _client.Execute( _request );
            ElexioCommunityApi.ApiCounter++;

            dynamic data = JsonConvert.DeserializeObject( response.Content );

            JArray familyMembers = data.data.family;
            if ( familyMembers != null )
            {
                foreach ( var member in familyMembers )
                {
                    // look for person in family members
                    if ( person.Id == (int)member["uid"] )
                    {
                        // get family role
                        string role = member["relationship"].ToString();
                        if ( role == "Father" || role == "Mother" || role == "Husband" || role == "Wife" || role == "Primary" )
                        {
                            person.FamilyRole = FamilyRole.Adult;
                        }
                        else
                        {
                            person.FamilyRole = FamilyRole.Child;
                        }

                        // get family id 
                        person.FamilyId = (int)member["fid"];

                        // get giving setting
                        person.GiveIndividually = !(bool)member["givesWithFamily"];
                    }
                } 
            }

            // it is possible that the person if not in a family so one will needs to be generated
            if ( person.FamilyId == null )
            {
                person.FamilyId = person.Id + 1000000;
                person.FamilyRole = FamilyRole.Adult;
                person.GiveIndividually = true;
            }

            // phone numbers
            string homePhone = importPerson.phoneHome;
            if ( homePhone != null && homePhone.AsNumeric().IsNotNullOrWhitespace() )
            {
                var phone = new PersonPhone();
                phone.PhoneType = "Home";
                phone.PhoneNumber = homePhone.AsNumeric();
                phone.PersonId = person.Id;

                person.PhoneNumbers.Add( phone );
            }

            string cellPhone = importPerson.phoneCell;
            if ( cellPhone != null && cellPhone.AsNumeric().IsNotNullOrWhitespace() )
            {
                var phone = new PersonPhone();
                phone.PhoneType = "Mobile";
                phone.PhoneNumber = cellPhone.AsNumeric();
                phone.IsMessagingEnabled = true;
                phone.PersonId = person.Id;

                person.PhoneNumbers.Add( phone );
            }

            string workPhone = importPerson.phoneWork;
            if ( workPhone != null && workPhone.AsNumeric().IsNotNullOrWhitespace() )
            {
                var phone = new PersonPhone();
                phone.PhoneType = "Work";
                phone.PhoneNumber = workPhone.AsNumeric();
                phone.PersonId = person.Id;

                person.PhoneNumbers.Add( phone );
            }

            // address 1
            string street1 = importPerson.address;
            string city = importPerson.city;
            string state = importPerson.state;
            string postalcode = importPerson.zipcode;

            if ( street1.IsNotNullOrWhitespace() && city.IsNotNullOrWhitespace() &&
                 state.IsNotNullOrWhitespace() && postalcode.IsNotNullOrWhitespace() )
            {
                var address = new PersonAddress();
                address.PersonId = person.Id;
                address.Street1 = street1;
                address.City = city;
                address.State = state;
                address.PostalCode = postalcode;
                address.Country = importPerson.country;
                address.AddressType = AddressType.Home;

                person.Addresses.Add( address );
            }

            // address 2
            street1 = importPerson.address2;
            city = importPerson.city2;
            state = importPerson.state2;
            postalcode = importPerson.zipcode2;

            if ( street1.IsNotNullOrWhitespace() && city.IsNotNullOrWhitespace() &&
                 state.IsNotNullOrWhitespace() && postalcode.IsNotNullOrWhitespace() )
            {
                var address = new PersonAddress();
                address.PersonId = person.Id;
                address.Street1 = street1;
                address.City = city;
                address.State = state;
                address.PostalCode = postalcode;
                address.Country = importPerson.country;
                address.AddressType = AddressType.Other;

                person.Addresses.Add( address );
            }

            // envelope number
            string envelopeNumber = importPerson.envNum;
            if ( envelopeNumber.AsIntegerOrNull().HasValue && envelopeNumber.AsIntegerOrNull().Value > 0 )
            {
                person.Attributes.Add( new PersonAttributeValue()
                {
                    PersonId = person.Id,
                    AttributeKey = "core_GivingEnvelopeNumber",
                    AttributeValue = envelopeNumber
                } ); 
            }

            //// dates ////           
            // created/modified date
            string updatedDate = importPerson.updated;
            if( updatedDate.IsNotNullOrWhitespace() )
            {
                person.CreatedDateTime = UnixTimeStampToDateTime( updatedDate.AsDouble() );
                person.ModifiedDateTime = UnixTimeStampToDateTime( updatedDate.AsDouble() );
            }

            // date of birth
            string birthdate = importPerson.dateBirth;
            if ( birthdate.AsDateTime().HasValue )
            {
                person.Birthdate = birthdate.AsDateTime().Value;
            }

            // baptism date
            string baptismDate = importPerson.dateBaptism;
            if ( baptismDate.AsDateTime().HasValue )
            {
                person.Attributes.Add( new PersonAttributeValue()
                {
                    PersonId = person.Id,
                    AttributeKey = "BaptismDate",
                    AttributeValue = baptismDate.AsDateTime().Value.ToString( "o" )
                } );
            }

            // check for deceased
            string dateDied = importPerson.dateDied;
            if ( dateDied.AsDateTime().HasValue )
            {
                person.IsDeceased = true;

                person.Attributes.Add( new PersonAttributeValue()
                {
                    PersonId = person.Id,
                    AttributeKey = "DeathDate",
                    AttributeValue = dateDied.AsDateTime().Value.ToString( "o" )
                } );
            }

            // last attended
            string lastAttended = importPerson.timeLastAttended;
            if ( lastAttended.AsDouble() > 0 )
            {
                person.Attributes.Add( new PersonAttributeValue()
                {
                    PersonId = person.Id,
                    AttributeKey = "LastAttended",
                    AttributeValue = UnixTimeStampToDateTime( lastAttended.AsDouble() ).ToString( "o" )
                } );
            }

            #region Attributes

            //// attributes
            MetaData metaData = ElexioCommunityApi.MetaData;

            //// dates 1 - 10
            // date 1
            string date1 = importPerson.date1;
            if ( metaData.data.dateFieldLabels.date1 != null && date1.AsDateTime().HasValue )
            {
                person.Attributes.Add( new PersonAttributeValue()
                {
                    PersonId = person.Id,
                    AttributeKey = metaData.data.dateFieldLabels.date1.RemoveSpaces().RemoveSpecialCharacters(),
                    AttributeValue = date1.AsDateTime().Value.ToString( "o" )
                } );
            }

            // date 2
            string date2 = importPerson.date2;
            if ( metaData.data.dateFieldLabels.date2 != null && date2.AsDateTime().HasValue )
            {
                person.Attributes.Add( new PersonAttributeValue()
                {
                    PersonId = person.Id,
                    AttributeKey = metaData.data.dateFieldLabels.date2.RemoveSpaces().RemoveSpecialCharacters(),
                    AttributeValue = date2.AsDateTime().Value.ToString( "o" )
                } );
            }

            // date 3
            string date3 = importPerson.date3;
            if ( metaData.data.dateFieldLabels.date3 != null && date3.AsDateTime().HasValue )
            {
                person.Attributes.Add( new PersonAttributeValue()
                {
                    PersonId = person.Id,
                    AttributeKey = metaData.data.dateFieldLabels.date3.RemoveSpaces().RemoveSpecialCharacters(),
                    AttributeValue = date3.AsDateTime().Value.ToString( "o" )
                } );
            }

            // date 4
            string date4 = importPerson.date4;
            if ( metaData.data.dateFieldLabels.date4 != null && date4.AsDateTime().HasValue )
            {
                person.Attributes.Add( new PersonAttributeValue()
                {
                    PersonId = person.Id,
                    AttributeKey = metaData.data.dateFieldLabels.date4.RemoveSpaces().RemoveSpecialCharacters(),
                    AttributeValue = date4.AsDateTime().Value.ToString( "o" )
                } );
            }

            // date 5
            string date5 = importPerson.date5;
            if ( metaData.data.dateFieldLabels.date5 != null && date5.AsDateTime().HasValue )
            {
                person.Attributes.Add( new PersonAttributeValue()
                {
                    PersonId = person.Id,
                    AttributeKey = metaData.data.dateFieldLabels.date5.RemoveSpaces().RemoveSpecialCharacters(),
                    AttributeValue = date5.AsDateTime().Value.ToString( "o" )
                } );
            }

            // date 6
            string date6 = importPerson.date1;
            if ( metaData.data.dateFieldLabels.date6 != null && date6.AsDateTime().HasValue )
            {
                person.Attributes.Add( new PersonAttributeValue()
                {
                    PersonId = person.Id,
                    AttributeKey = metaData.data.dateFieldLabels.date6.RemoveSpaces().RemoveSpecialCharacters(),
                    AttributeValue = date6.AsDateTime().Value.ToString( "o" )
                } );
            }

            // date 7
            string date7 = importPerson.date7;
            if ( metaData.data.dateFieldLabels.date7 != null && date7.AsDateTime().HasValue )
            {
                person.Attributes.Add( new PersonAttributeValue()
                {
                    PersonId = person.Id,
                    AttributeKey = metaData.data.dateFieldLabels.date7.RemoveSpaces().RemoveSpecialCharacters(),
                    AttributeValue = date7.AsDateTime().Value.ToString( "o" )
                } );
            }

            // date 8
            string date8 = importPerson.date8;
            if ( metaData.data.dateFieldLabels.date8 != null && date8.AsDateTime().HasValue )
            {
                person.Attributes.Add( new PersonAttributeValue()
                {
                    PersonId = person.Id,
                    AttributeKey = metaData.data.dateFieldLabels.date8.RemoveSpaces().RemoveSpecialCharacters(),
                    AttributeValue = date8.AsDateTime().Value.ToString( "o" )
                } );
            }

            // date 9
            string date9 = importPerson.date9;
            if ( metaData.data.dateFieldLabels.date9 != null && date9.AsDateTime().HasValue )
            {
                person.Attributes.Add( new PersonAttributeValue()
                {
                    PersonId = person.Id,
                    AttributeKey = metaData.data.dateFieldLabels.date9.RemoveSpaces().RemoveSpecialCharacters(),
                    AttributeValue = date9.AsDateTime().Value.ToString( "o" )
                } );
            }

            // date 1
            string date10 = importPerson.date10;
            if ( metaData.data.dateFieldLabels.date10 != null && date10.AsDateTime().HasValue )
            {
                person.Attributes.Add( new PersonAttributeValue()
                {
                    PersonId = person.Id,
                    AttributeKey = metaData.data.dateFieldLabels.date10.RemoveSpaces().RemoveSpecialCharacters(),
                    AttributeValue = date10.AsDateTime().Value.ToString( "o" )
                } );
            }

            //// text 1 - 15
            // text 1
            string text1 = importPerson.text1;
            if ( metaData.data.textFieldLabels.text1 != null && text1.IsNotNullOrWhitespace() )
            {
                person.Attributes.Add( new PersonAttributeValue()
                {
                    PersonId = person.Id,
                    AttributeKey = metaData.data.textFieldLabels.text1.RemoveSpaces().RemoveSpecialCharacters(),
                    AttributeValue = importPerson.text1
                } );
            }

            // text 2
            string text2 = importPerson.text2;
            if ( metaData.data.textFieldLabels.text2 != null && text2.IsNotNullOrWhitespace() )
            {
                person.Attributes.Add( new PersonAttributeValue()
                {
                    PersonId = person.Id,
                    AttributeKey = metaData.data.textFieldLabels.text2.RemoveSpaces().RemoveSpecialCharacters(),
                    AttributeValue = importPerson.text2
                } );
            }

            // text 3
            string text3 = importPerson.text3;
            if ( metaData.data.textFieldLabels.text3 != null && text3.IsNotNullOrWhitespace() )
            {
                person.Attributes.Add( new PersonAttributeValue()
                {
                    PersonId = person.Id,
                    AttributeKey = metaData.data.textFieldLabels.text3.RemoveSpaces().RemoveSpecialCharacters(),
                    AttributeValue = importPerson.text3
                } );
            }

            // text 4
            string text4 = importPerson.text4;
            if ( metaData.data.textFieldLabels.text4 != null && text4.IsNotNullOrWhitespace() )
            {
                person.Attributes.Add( new PersonAttributeValue()
                {
                    PersonId = person.Id,
                    AttributeKey = metaData.data.textFieldLabels.text4.RemoveSpaces().RemoveSpecialCharacters(),
                    AttributeValue = importPerson.text4
                } );
            }

            // text 5
            string text5 = importPerson.text5;
            if ( metaData.data.textFieldLabels.text5 != null && text5.IsNotNullOrWhitespace() )
            {
                person.Attributes.Add( new PersonAttributeValue()
                {
                    PersonId = person.Id,
                    AttributeKey = metaData.data.textFieldLabels.text5.RemoveSpaces().RemoveSpecialCharacters(),
                    AttributeValue = importPerson.text5
                } );
            }

            // text 6
            string text6 = importPerson.text6;
            if ( metaData.data.textFieldLabels.text6 != null && text6.IsNotNullOrWhitespace() )
            {
                person.Attributes.Add( new PersonAttributeValue()
                {
                    PersonId = person.Id,
                    AttributeKey = metaData.data.textFieldLabels.text6.RemoveSpaces().RemoveSpecialCharacters(),
                    AttributeValue = importPerson.text6
                } );
            }

            // text 7
            string text7 = importPerson.text7;
            if ( metaData.data.textFieldLabels.text7 != null && text7.IsNotNullOrWhitespace() )
            {
                person.Attributes.Add( new PersonAttributeValue()
                {
                    PersonId = person.Id,
                    AttributeKey = metaData.data.textFieldLabels.text7.RemoveSpaces().RemoveSpecialCharacters(),
                    AttributeValue = importPerson.text7
                } );
            }

            // text 8
            string text8 = importPerson.text8;
            if ( metaData.data.textFieldLabels.text8 != null && text8.IsNotNullOrWhitespace() )
            {
                person.Attributes.Add( new PersonAttributeValue()
                {
                    PersonId = person.Id,
                    AttributeKey = metaData.data.textFieldLabels.text8.RemoveSpaces().RemoveSpecialCharacters(),
                    AttributeValue = importPerson.text8
                } );
            }

            // text 9
            string text9 = importPerson.text9;
            if ( metaData.data.textFieldLabels.text9 != null && text9.IsNotNullOrWhitespace() )
            {
                person.Attributes.Add( new PersonAttributeValue()
                {
                    PersonId = person.Id,
                    AttributeKey = metaData.data.textFieldLabels.text9.RemoveSpaces().RemoveSpecialCharacters(),
                    AttributeValue = importPerson.text9
                } );
            }

            // text 10
            string text10 = importPerson.text10;
            if ( metaData.data.textFieldLabels.text10 != null && text10.IsNotNullOrWhitespace() )
            {
                person.Attributes.Add( new PersonAttributeValue()
                {
                    PersonId = person.Id,
                    AttributeKey = metaData.data.textFieldLabels.text10.RemoveSpaces().RemoveSpecialCharacters(),
                    AttributeValue = importPerson.text10
                } );
            }

            // text 11
            string text11 = importPerson.text11;
            if ( metaData.data.textFieldLabels.text11 != null && text11.IsNotNullOrWhitespace() )
            {
                person.Attributes.Add( new PersonAttributeValue()
                {
                    PersonId = person.Id,
                    AttributeKey = metaData.data.textFieldLabels.text11.RemoveSpaces().RemoveSpecialCharacters(),
                    AttributeValue = importPerson.text11
                } );
            }

            // text 12
            string text12 = importPerson.text12;
            if ( metaData.data.textFieldLabels.text12 != null && text12.IsNotNullOrWhitespace() )
            {
                person.Attributes.Add( new PersonAttributeValue()
                {
                    PersonId = person.Id,
                    AttributeKey = metaData.data.textFieldLabels.text12.RemoveSpaces().RemoveSpecialCharacters(),
                    AttributeValue = importPerson.text12
                } );
            }

            // text 13
            string text13 = importPerson.text13;
            if ( metaData.data.textFieldLabels.text13 != null && text13.IsNotNullOrWhitespace() )
            {
                person.Attributes.Add( new PersonAttributeValue()
                {
                    PersonId = person.Id,
                    AttributeKey = metaData.data.textFieldLabels.text13.RemoveSpaces().RemoveSpecialCharacters(),
                    AttributeValue = importPerson.text13
                } );
            }

            // text 14
            string text14 = importPerson.text14;
            if ( metaData.data.textFieldLabels.text14 != null && text14.IsNotNullOrWhitespace() )
            {
                person.Attributes.Add( new PersonAttributeValue()
                {
                    PersonId = person.Id,
                    AttributeKey = metaData.data.textFieldLabels.text14.RemoveSpaces().RemoveSpecialCharacters(),
                    AttributeValue = importPerson.text14
                } );
            }

            // text 15
            string text15 = importPerson.text15;
            if ( metaData.data.textFieldLabels.text15 != null && text15.IsNotNullOrWhitespace() )
            {
                person.Attributes.Add( new PersonAttributeValue()
                {
                    PersonId = person.Id,
                    AttributeKey = metaData.data.textFieldLabels.text15.RemoveSpaces().RemoveSpecialCharacters(),
                    AttributeValue = importPerson.text15
                } );
            }

            #endregion

            return person;
        }

        public static DateTime UnixTimeStampToDateTime( double unixTimeStamp )
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime( 1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc );
            dtDateTime = dtDateTime.AddSeconds( unixTimeStamp ).ToLocalTime();
            return dtDateTime;
        }
    }
}
