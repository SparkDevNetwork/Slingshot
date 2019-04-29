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
                    phone.BusinessId = row.Field<int>( "HOUSEHOLD_ID" ) + +900000000;
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
