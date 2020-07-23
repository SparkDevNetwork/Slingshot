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
    public static class F1PersonAddress
    {
        public static PersonAddress Translate(
            DataRow row
            , DataTable dtPeople )
        {

            var address = new PersonAddress();

            try
            {

                var houseHoldId = row.Field<int>( "household_id" );
                var personId = row.Field<int?>( "individual_id" );

                // Check if the address has a person tied to if. If not, set the head of household PersonId to the personId
                if ( !personId.HasValue )
                {
                    var person = dtPeople.Select( " household_position = 'Head' AND household_id = " + houseHoldId ).FirstOrDefault();
                    if ( person == null )
                    {
                        // We didn't find a household 'Head', so look for anyone else who isn't a visitor.
                        person = dtPeople.Select( " household_position <> 'Visitor' AND household_id = " + houseHoldId ).FirstOrDefault();
                    }
                    if ( person == null )
                    {
                        // We didn't find anyone who isn't a visitor, so it's okay to assign this address to the visitor.
                        person = dtPeople.Select( "household_id = " + houseHoldId ).FirstOrDefault();
                    }
                    if ( person != null )
                    {
                        personId = person.Field<int>( "individual_id" );
                    }
                }

                if ( !personId.HasValue )
                {
                    return null;
                }

                address.PersonId = personId.Value;
                address.Street1 = row.Field<string>( "address_1" );
                address.Street2 = row.Field<string>( "address_2" );
                address.City = row.Field<string>( "city" );
                address.State = row.Field<string>( "state" );
                address.PostalCode = row.Field<string>( "zip_code" );
                address.Country = row.Field<string>( "country" );

                var addressType = row.Field<string>( "address_type" );
                switch ( addressType )
                {
                    case "Primary":
                    {
                        address.AddressType = AddressType.Home;
                        address.IsMailing = true;
                        break;
                    }
                    case "Previous":
                    {
                        address.AddressType = AddressType.Previous;
                        break;
                    }
                    case "Business":
                    {
                        address.AddressType = AddressType.Work;
                        break;
                    }
                    case "Mail Returned / Incorrect":
                    {
                        address.AddressType = AddressType.Other;
                        break;
                    }
                    default:
                    {
                        address.AddressType = AddressType.Other;
                        break;
                    }
                }

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
