using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Slingshot.Core.Model;

namespace Slingshot.Breeze.Utilities.Translators
{
    public static class BreezePerson
    {
        /// <summary>
        /// Takes a dictionary representing a CSV record/row and returns a new Person
        /// object with the properties set to the values indicated in the CSV record.
        /// </summary>
        /// <param name="csvRecord"></param>
        /// <returns></returns>
        public static Person Translate( IDictionary<string, object> csvRecord, List<PersonAttribute> attributes )
        {
            if ( csvRecord == null || !csvRecord.Keys.Any() )
            {
                return null;
            }

            // Map the properties of Person class to known CSV headers
            // Maybe this could be configurable to the user in the UI if the need arises
            var propertyToCsvFieldNameMap = new Dictionary<string, string> {
                { "Id", "Breeze ID" },
                { "FamilyId", "Family" },
                // FamilyName
                // FamilyImageUrl
                { "FamilyRole", "Family Role" },
                { "FirstName", "First Name" },
                { "NickName", "Nickname" },
                { "LastName", "Last Name" },
                { "MiddleName", "Middle Name" },
                // Salutation
                // Suffix
                { "Email", "Email" },
                { "Gender", "Gender" },
                { "MaritalStatus", "Marital Status" },
                { "Birthdate", "Birthdate" },
                // AnniversaryDate
                // RecordStatus
                // InactiveReason
                // ConnectionStatus
                { "EmailPreference", "Opt-out Graceland Emails" },
                { "CreatedDateTime", "Added Date" },
                { "ModifiedDateTime", "Record Last Updated" },
                // PersonPhotoUrl
                // Campus
                // Note
                // GiveIndividually
                // IsDeceased
                // GiveIndividually
            };

            // Keep track of which fields are used so that the remaining fields can be
            // stored as attributes
            var unusedCsvFieldNames = csvRecord.Keys.ToList();

            // Discard fields that are calculated from other fields or simply not needed
            var ignoreFieldNames = new List<string>
            {
                "Birthdate Month/Day", // Calculated from birthdate
                "Age", // Calculated from birthdate
                "Record Last Updated Month/Day", // Calculated from RecordLastUpdated
                "Years Since Record Last Updated", // Calculated from RecordLastUpdated
                "Grade" // Calculated from graduation year
            };

            foreach ( var ignoreFieldName in ignoreFieldNames )
            {
                unusedCsvFieldNames.Remove( ignoreFieldName );
            }

            // Create a person object. Using the map, read values from the CSV record and
            // set the associated properties of the person with those values
            var person = new Person();
            var personType = person.GetType();

            foreach ( var kvp in propertyToCsvFieldNameMap )
            {
                var propertyName = kvp.Key;
                var csvFieldName = kvp.Value;
                var property = personType.GetProperty( propertyName );
                var value = CsvFieldTranslators.GetValue( property.PropertyType, csvFieldName, csvRecord );

                property.SetValue( person, value );
                unusedCsvFieldNames.Remove( csvFieldName );
            }

            // Add phones if the CSV fields are set. The phone types match the breeze 
            // field names.
            foreach ( var phoneType in new[] { "Home", "Mobile", "Work" } )
            {
                AddPhone( person, csvRecord, phoneType, phoneType );
                unusedCsvFieldNames.Remove( phoneType );
            }

            // Add an address if at least one of the properties has a value
            var address = CsvFieldTranslators.GetString( "Street Address", csvRecord );
            unusedCsvFieldNames.Remove( "Street Address" );

            var city = CsvFieldTranslators.GetString( "City", csvRecord );
            unusedCsvFieldNames.Remove( "City" );

            var state = CsvFieldTranslators.GetString( "State", csvRecord );
            unusedCsvFieldNames.Remove( "State" );

            var zip = CsvFieldTranslators.GetString( "Zip", csvRecord );
            unusedCsvFieldNames.Remove( "Zip" );

            if ( !string.IsNullOrWhiteSpace( address ) ||
                !string.IsNullOrWhiteSpace( city ) ||
                !string.IsNullOrWhiteSpace( state ) ||
                !string.IsNullOrWhiteSpace( zip ) )
            {
                person.Addresses.Add( new PersonAddress
                {
                    AddressType = AddressType.Home,
                    City = city,
                    PersonId = person.Id,
                    PostalCode = zip,
                    State = state,
                    Street1 = address
                } );
            }

            // Get the grade / graduation year
            person.Grade = CsvFieldTranslators.GetGrade( "Graduation Year", csvRecord );
            unusedCsvFieldNames.Remove( "Graduation Year" );

            // For all remaining fields of the CSV, create an attribute value
            var whitespaceRegex = new Regex( @"\s+" );

            foreach ( var csvFieldName in unusedCsvFieldNames )
            {
                var key = whitespaceRegex.Replace( csvFieldName, string.Empty );
                var value = CsvFieldTranslators.GetString( csvFieldName, csvRecord );

                if ( string.IsNullOrWhiteSpace( key ) || string.IsNullOrWhiteSpace( value ) )
                {
                    continue;
                }

                var existingAttribute = attributes.FirstOrDefault( a => a.Key.Equals( key, StringComparison.OrdinalIgnoreCase ) );

                if ( existingAttribute == null )
                {
                    existingAttribute = new PersonAttribute { 
                        Key = key,
                        FieldType = "Rock.Field.Types.TextFieldType",
                        Name = csvFieldName
                    };

                    attributes.Add( existingAttribute );
                }

                person.Attributes.Add( new PersonAttributeValue
                {
                    AttributeKey = existingAttribute.Key,
                    AttributeValue = value,
                    PersonId = person.Id
                } );
            }

            return person;
        }

        /// <summary>
        /// Add a phone number to the person if the CSV record has a value for that field and
        /// phone type.
        /// </summary>
        /// <param name="person"></param>
        /// <param name="csvRecord"></param>
        /// <param name="fieldName"></param>
        /// <param name="phoneType"></param>
        private static void AddPhone( Person person, IDictionary<string, object> csvRecord, string fieldName, string phoneType )
        {
            var number = CsvFieldTranslators.GetString( fieldName, csvRecord );

            if ( string.IsNullOrWhiteSpace( number ) )
            {
                return;
            }

            person.PhoneNumbers.Add( new PersonPhone
            {
                PersonId = person.Id,
                PhoneNumber = number,
                PhoneType = phoneType
            } );
        }
    }
}
