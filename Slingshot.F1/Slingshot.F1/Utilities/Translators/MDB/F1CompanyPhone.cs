using System.Data;
using System.Linq;
using Slingshot.Core.Model;

namespace Slingshot.F1.Utilities.Translators.MDB
{
    public static class F1CompanyPhone
    {
        public static PersonPhone Translate( DataRow row )
        {
            var phone = new PersonPhone();

            try
            {
                var communicationType = row.Field<string>( "communication_type" );

                if ( communicationType != "Mobile" && !communicationType.Contains( "Phone" ) && !communicationType.Contains( "phone" ) )
                {
                    return null;
                }

                var householdId = row.Field<int>( "household_id" );
                var companyAsPersonId = F1Company.GetCompanyAsPersonId( householdId );
                var phoneType = communicationType.Replace( "Phone", string.Empty ).Replace( "phone", string.Empty ).Trim();
                var phoneNumber = new string( row.Field<string>( "communication_value" ).Where( c => char.IsDigit( c ) ).ToArray() );
                
                if ( !string.IsNullOrWhiteSpace( phoneNumber ) )
                {
                    phone.PersonId = companyAsPersonId;
                    phone.PhoneType = phoneType;
                    phone.PhoneNumber = phoneNumber;
                    return phone;
                }
            }
            catch
            {
                return null;
            }

            return null;
        }
    }
}
