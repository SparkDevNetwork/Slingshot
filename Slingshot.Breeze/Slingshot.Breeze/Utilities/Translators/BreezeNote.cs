using System.Collections.Generic;
using System.Linq;
using Slingshot.Core.Model;

namespace Slingshot.Breeze.Utilities.Translators
{
    public static class BreezeNote
    {
        /// <summary>
        /// Takes a dictionary representing a CSV record/row and returns a new Person
        /// Note object with the properties set to the values indicated in the CSV record.
        /// </summary>
        /// <param name="csvRecord"></param>
        /// <returns></returns>
        public static PersonNote Translate( IDictionary<string, object> csvRecord )
        {
            if ( csvRecord == null || !csvRecord.Keys.Any() )
            {
                return null;
            }

            // Map the properties of Person Note class to known CSV headers
            // Maybe this could be configurable to the user in the UI if the need arises
            var propertyToCsvFieldNameMap = new Dictionary<string, string> {
                { "PersonId", "Breeze ID" },
                // Id
                { "NoteType", "Username" },
                // Caption
                // IsAlert
                { "IsPrivateNote", "Is Private" },
                { "Text", "Note" },
                { "DateTime", "Created On" } 
                // CreatedByPersonId
            };

            // Create a person note object. Using the map, read values from the CSV record and
            // set the associated properties of the person with those values
            var personNote = new PersonNote();
            var personNoteType = personNote.GetType();

            foreach ( var kvp in propertyToCsvFieldNameMap )
            {
                var propertyName = kvp.Key;
                var csvFieldName = kvp.Value;
                var property = personNoteType.GetProperty( propertyName );
                var value = CsvFieldTranslators.GetValue( property.PropertyType, csvFieldName, csvRecord );

                property.SetValue( personNote, value );
            }

            // Notetype is mapped to username since there is no great way to match a username to an author person
            if ( string.IsNullOrWhiteSpace( personNote.NoteType ) )
            {
                personNote.NoteType = "Anonymous";
            }

            // TODO store unused values, like username, as attributes when slingshot supports it

            return personNote;
        }
    }
}
