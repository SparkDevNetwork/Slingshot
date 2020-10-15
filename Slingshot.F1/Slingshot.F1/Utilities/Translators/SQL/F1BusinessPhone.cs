using Slingshot.Core;
using Slingshot.Core.Model;
using System.Data;
using System.Linq;

namespace Slingshot.F1.Utilities.Translators.SQL
{
    public static class F1BusinessPhone
    {
        public static BusinessPhone Translate( DataRow row )
        {
            var phone = new BusinessPhone();

            try
            {
                string phoneType = row.Field<string>( "communication_type" ).Replace( "Phone", "" ).Replace( "phone", "" ).Trim();
                string phoneNumber = new string( row.Field<string>( "communication_value" ).Where( c => char.IsDigit( c ) ).ToArray() );
                if ( !string.IsNullOrWhiteSpace( phoneNumber ) )
                {
                    phone.BusinessId = F1Business.GetCompanyAsPersonId( row.Field<int>( "HOUSEHOLD_ID" ) );
                    phone.PhoneType = phoneType;
                    phone.PhoneNumber = phoneNumber.Left( 20 );
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
