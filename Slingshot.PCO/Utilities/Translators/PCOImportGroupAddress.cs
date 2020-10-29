using Slingshot.Core.Model;
using Slingshot.PCO.Models.DTO;

namespace Slingshot.PCO.Utilities.Translators
{
    public static class PCOImportGroupAddress
    {
        public static GroupAddress Translate( LocationDTO location, int groupId )
        {
            if ( location.Id <= 0 )
            {
                return null;
            }

            return new GroupAddress()
            {
                AddressType = AddressType.Other,
                GroupId = groupId,
                City = location.City,
                Latitude = location.Latitude,
                Longitude = location.Longitude,
                State = location.State,
                PostalCode = location.Zip,
                Street1 = location.Street
            };
        }
    }
}
