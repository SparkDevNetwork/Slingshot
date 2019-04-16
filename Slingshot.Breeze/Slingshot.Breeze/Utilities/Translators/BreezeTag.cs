using System.Collections.Generic;
using System.Linq;
using Slingshot.Core.Model;

namespace Slingshot.Breeze.Utilities.Translators
{
    public static class BreezeTag
    {
        public static GroupMember Translate( IDictionary<string, object> record )
        {
            if ( record == null || !record.Keys.Any() )
            {
                return null;
            }

            // Map the properties of Person Note class to known CSV headers
            // Maybe this could be configurable to the user in the UI if the need arises
            var propertyToCsvFieldNameMap = new Dictionary<string, string> {
                { "PersonId", "Person ID" }
            };

            // Create a person note object. Using the map, read values from the CSV record and
            // set the associated properties of the person with those values
            var groupMember = new GroupMember();
            var groupMemberType = groupMember.GetType();

            foreach ( var kvp in propertyToCsvFieldNameMap )
            {
                var propertyName = kvp.Key;
                var csvFieldName = kvp.Value;
                var property = groupMemberType.GetProperty( propertyName );
                var value = CsvFieldTranslators.GetValue( property.PropertyType, csvFieldName, record );

                property.SetValue( groupMember, value );
            }

            // TODO store unused values, like username, as attributes when slingshot supports it

            return groupMember;
        }
    }
}
