using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

using Slingshot.Core;
using Slingshot.Core.Model;

namespace Slingshot.F1.Utilities.Translators.MDB
{
    public static class F1Company
    {
        public static int GetCompanyAsPersonId( int householdId )
        {
            return int.MaxValue - householdId;
        }

        public static Person Translate( DataRow row, DataTable communications )
        {
            var companyAsPerson = new Person();
            var notes = new List<string>();

            try
            {
                var householdId = row.Field<int>( "HOUSEHOLD_ID" );
                companyAsPerson.FamilyId = householdId;

                // Try to avoid collisions with individual IDs but need companies 
                // to be imported as people in Rock since they have contributions
                companyAsPerson.Id = GetCompanyAsPersonId( householdId );

                // Set the last name and family name to the company name
                string name = row.Field<string>( "HOUSEHOLD_NAME" );
                if ( name.IsNotNullOrWhitespace() )
                {
                    var trimmedName = name.Left( 50 );
                    companyAsPerson.LastName = trimmedName;
                    companyAsPerson.FamilyName = trimmedName;
                }

                // Get dates
                companyAsPerson.CreatedDateTime = row.Field<DateTime?>( "CREATED_DATE" );
                companyAsPerson.ModifiedDateTime = row.Field<DateTime?>( "LAST_UPDATED_DATE" );

                // Set some default N/A values for companies
                companyAsPerson.FamilyRole = FamilyRole.Adult;
                companyAsPerson.Gender = Gender.Unknown;
                companyAsPerson.MaritalStatus = MaritalStatus.Unknown;
                companyAsPerson.RecordStatus = RecordStatus.Active;
                companyAsPerson.ConnectionStatus = "Attendee";
                companyAsPerson.EmailPreference = EmailPreference.EmailAllowed;
                companyAsPerson.IsDeceased = false;

                // Get communication values
                var emailRow = communications.Select( "household_id = " + householdId + " AND communication_type = 'Email'" ).FirstOrDefault();

                if ( emailRow != null )
                {
                    var email = emailRow.Field<string>( "communication_value" );

                    if ( email.IsNotNullOrWhitespace() )
                    {
                        companyAsPerson.Email = email;
                    }
                }
            }
            catch ( Exception ex )
            {
                notes.Add( "ERROR in Export: " + ex.Message + ": " + ex.StackTrace );
            }

            // write out import notes
            if ( notes.Count > 0 )
            {
                companyAsPerson.Note = string.Join( ",", notes );
            }

            return companyAsPerson;
        }
    }
}
