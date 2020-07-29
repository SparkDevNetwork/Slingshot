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
    public static class F1PersonPhone
    {
        public static PersonPhone Translate( DataRow row, int personId )
        {
            var phone = new PersonPhone();

            try
            {
                string phoneType = row.Field<string>( "communication_type" ).Replace( "Phone", "" ).Replace( "phone", "" ).Trim();
                string phoneNumber = new string( row.Field<string>( "communication_value" ).Where( c => char.IsDigit( c ) ).ToArray() );
                if ( !string.IsNullOrWhiteSpace( phoneNumber ) )
                {
                    phone.PersonId = personId;
                    phone.PhoneType = phoneType;
                    phone.PhoneNumber = phoneNumber.Left( 20 );
                    phone.IsMessagingEnabled = false;
                    if( row.Field<Int16>( "listed" ) == 255 )
                    {
                        phone.IsUnlisted = false;
                        if( phoneType == "Mobile" || phoneType == "Cell" )
                        {
                            phone.IsMessagingEnabled = true;
                        }
                    }
                    else
                    {
                        phone.IsUnlisted = true;
                    }
                    return phone;
                }
            }
            catch
            {
                return null;
            }

            return null;
        }

        public static List<int> GetPhonePersonIds( DataRow row, DataTable dtPeople )
        {
            var personId = row.Field<int?>( "individual_id" );
            if ( personId.HasValue )
            {
                // If assigned to a specific person, just use that.
                return new List<int>() { personId.Value };
            }

            // if phone number does not have an individual_id, it must have a household_id to be exported.
            var houseHoldId = row.Field<int>( "household_id" );
            var householdRows = dtPeople.Select( $"household_id = { houseHoldId }" );
            if ( !householdRows.Any() )
            {
                // If there are no household members for this row, we can't export it.  Just return an empty list.
                return new List<int>();
            }

            var householdMembers = householdRows.CopyToDataTable();

            var personIds = new List<int>();

            var headOfHousehold = householdMembers.Select( "household_position = 'Head'" ).FirstOrDefault();
            if ( headOfHousehold != null )
            {
                // Add Head of Household.
                personIds.Add( headOfHousehold.Field<int>( "individual_id" ) );
            }

            var spouse = householdMembers.Select( "household_position = 'Spouse'" ).FirstOrDefault();
            if ( spouse != null )
            {
                // Add Head of Spouse.
                personIds.Add( spouse.Field<int>( "individual_id" ) );
            }

            if ( personIds.Any() )
            {
                // Found one or more adult records, so we're done here.
                householdMembers.Clear();
                return personIds;
            }

            var visitor = dtPeople.Select( $"household_position = 'Visitor'" ).FirstOrDefault();
            if ( visitor != null )
            {
                // We didn't find anyone who isn't a visitor, so it's okay to assign this phone number to the visitor.
                personIds.Add(visitor.Field<int>( "individual_id" ) );
            }

            householdMembers.Clear();
            return personIds;
        }
    }
}
