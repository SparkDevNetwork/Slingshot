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
        public static PersonPhone Translate( DataRow row )
        {
            var phone = new PersonPhone();

            try
            {
                string phoneType = row.Field<string>( "communication_type" ).Replace( "Phone", "" ).Replace( "phone", "" ).Trim();
                string phoneNumber = new string( row.Field<string>( "communication_value" ).Where( c => char.IsDigit( c ) ).ToArray() );
                if ( !string.IsNullOrWhiteSpace( phoneNumber ) )
                {
                    phone.PersonId = row.Field<int>( "individual_id" );
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
