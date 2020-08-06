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
    public static class F1PersonAttributeValue
    {
        public static List<PersonAttributeValue> Translate(
            DataRow row)
        {

            List<PersonAttributeValue> AttributeValues = new List<PersonAttributeValue>();

            try
            {
                // person attributes
            
                int attributeId = row.Field<int>( "Attribute_Id" );
                string attributeName = row.Field<string>( "Attribute_Name" );
                // Add the attribute value for start date (if not empty) 
                var startDateAttributeKey = attributeId.ToString() + "_" + attributeName.RemoveSpaces().RemoveSpecialCharacters() + "StartDate";
                DateTime? startDate = row.Field<DateTime?>( "Start_Date" );

                if ( startDate.HasValue )
                {
                    AttributeValues.Add( new PersonAttributeValue
                    {
                        AttributeKey = startDateAttributeKey,
                        AttributeValue = startDate.Value.ToString( "o" ), // save as UTC date format
                        PersonId = row.Field<int>( "individual_id" )
                    } );
                }

                // Add the attribute value for end date (if not empty) 
                var endDateAttributeKey = attributeId.ToString() + "_" + attributeName.RemoveSpaces().RemoveSpecialCharacters() + "EndDate";
                DateTime? endDate = row.Field<DateTime?>( "End_Date" );

                if ( endDate.HasValue )
                {
                    AttributeValues.Add( new PersonAttributeValue
                    {
                        AttributeKey = endDateAttributeKey,
                        AttributeValue = endDate.Value.ToString( "o" ), // save as UTC date format
                        PersonId = row.Field<int>( "individual_id" )
                    } );
                }

                // Add the attribute value for comment (if not empty) 
                var commentAttributeKey = attributeId.ToString() + "_" + attributeName.RemoveSpaces().RemoveSpecialCharacters() + "Comment";
                string comment = row.Field<string>( "comment" );

                if ( comment.IsNotNullOrWhitespace() )
                {
                    AttributeValues.Add( new PersonAttributeValue
                    {
                        AttributeKey = commentAttributeKey,
                        AttributeValue = comment,
                        PersonId = row.Field<int>( "individual_id" )
                    } );
                }
                // If the attribute exists but we do not have any values assigned (comment, start date, end date)
                // then set the value to true so that we know the attribute exists.
                else if ( !comment.IsNotNullOrWhitespace() && !startDate.HasValue && !startDate.HasValue )
                {
                    AttributeValues.Add( new PersonAttributeValue
                    {
                        AttributeKey = commentAttributeKey,
                        AttributeValue = "True",
                        PersonId = row.Field<int>( "individual_id" )
                    } );
                }
            }
            catch
            {
               return null;
            }

            return AttributeValues;
        }
    }
}
