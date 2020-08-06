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
        public static int GetCompanyAsPersonId( int householdId )
        {
            return int.MaxValue - householdId;
        }

        public static Business Translate( DataRow row, DataTable communications )
        {

            var business = new Business();
            var notes = new List<string>();
            try
            {
                var householdId = row.Field<int>( "HOUSEHOLD_ID" );
                business.Id = F1Business.GetCompanyAsPersonId( householdId );
                business.Name = row.Field<string>( "HOUSEHOLD_NAME" );
                business.ModifiedDateTime = row.Field<DateTime?>( "LAST_ACTIVITY_DATE" );
                business.CreatedDateTime = row.Field<DateTime>( "CREATED_DATE" );

                // Get communication values
                var emailRow = communications.Select( $"household_id = { householdId } AND communication_type = 'Email'" ).FirstOrDefault();

                if ( emailRow != null )
                {
                    var email = emailRow.Field<string>( "communication_value" );

                    if ( email.IsNotNullOrWhitespace() )
                    {
                        business.Email = email;
                    }
                }

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
            catch( Exception ex )
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
