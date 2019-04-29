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
    public static class F1Business
    {
        public static Business Translate( DataRow row )
        {

            var business = new Business();
            var notes = new List<string>();
            try
            {
                // Add 900,000,000 to HouseHold_ID to insure it doesn't conflict with any Indiviual Ids, because in Rock, business are people, not families.
                business.Id = row.Field<int>( "HOUSEHOLD_ID" ) + 900000000;
                business.Name = row.Field<string>( "HOUSEHOLD_NAME" );
                business.ModifiedDateTime = row.Field<DateTime?>( "LAST_ACTIVITY_DATE" );
                business.CreatedDateTime = row.Field<DateTime>( "CREATED_DATE" );

                string companyType = row.Field<string>( "CompanyType" );
                if ( companyType.IsNotNullOrWhitespace() )
                {
                    business.Attributes.Add( new BusinessAttributeValue
                    {
                        AttributeKey = "CompanyType",
                        AttributeValue = companyType,
                        BusinessId = business.Id
                    } );
                }

                string contactName = row.Field<string>( "CONTACT_NAME" );
                if ( contactName.IsNotNullOrWhitespace() )
                {
                    business.Attributes.Add( new BusinessAttributeValue
                    {
                        AttributeKey = "ContactName",
                        AttributeValue = contactName,
                        BusinessId = business.Id
                    } );
                }
            }
            catch(Exception ex)
            {
                notes.Add( "ERROR in Export: " + ex.Message + ": " + ex.StackTrace );
            }

            // write out import notes
            if ( notes.Count > 0 )
            {
                business.Note = string.Join( ",", notes );
            }

            return business;
        }
    }
}
