using System;
using System.Data;

using Slingshot.Core.Model;

namespace Slingshot.ServantKeeper.Utilities.Translators
{
    public static class SkPhone
    {
        public static PersonPhone Translate( DataRow row )
        {
            var phone = new PersonPhone();

            phone.PersonId = row.Field<int>( "IndividualId" );

            phone.PhoneNumber = row.Field<string>( "Phone" );
            phone.IsUnlisted = row.Field<bool?>( "Unlisted" ).Value;
            
            var phoneType = row.Field<string>( "Description" );
            switch ( phoneType )
            {
                case "Cell":
                    phone.PhoneType = "Mobile";
                    break;
                case "Home":
                    phone.PhoneType = "Home";
                    break;
                case "Business":
                    phone.PhoneType = "Work";
                    break;
                default:
                    phone.PhoneType = phoneType;
                    break;
            }

            return phone;
        }
    }
}
