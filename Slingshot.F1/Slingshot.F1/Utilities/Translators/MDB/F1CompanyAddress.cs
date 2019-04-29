using System.Data;

using Slingshot.Core;
using Slingshot.Core.Model;

namespace Slingshot.F1.Utilities.Translators.MDB
{
    public static class F1CompanyAddress
    {
        public static PersonAddress Translate( DataRow row )
        {
            var address = new PersonAddress();

            try
            {
                var householdId = row.Field<int>( "household_id" );
                var companyAsPersonId = F1Company.GetCompanyAsPersonId( householdId );

                address.PersonId = companyAsPersonId;
                address.Street1 = row.Field<string>( "address_1" );
                address.Street2 = row.Field<string>( "address_2" );
                address.City = row.Field<string>( "city" );
                address.State = row.Field<string>( "state" );
                address.PostalCode = row.Field<string>( "zip_code" );
                address.Country = row.Field<string>( "country" );
                address.AddressType = AddressType.Work;

                // only add the address if we have a valid address
                if ( address.Street1.IsNotNullOrWhitespace() &&
                        address.City.IsNotNullOrWhitespace() &&
                        address.PostalCode.IsNotNullOrWhitespace() )
                {
                    return address;

                }

                return null;

            }
            catch
            {
                return null;
            }

        }
    }
}
