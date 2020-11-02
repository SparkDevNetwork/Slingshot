using Slingshot.Core.Model;
using Slingshot.PCO.Models.DTO;

namespace Slingshot.PCO.Utilities.Translators
{
    public static class PCOImportLocation
    {
        public static Location Translate( CheckInLocationDTO checkInLocation )
        {
            if ( checkInLocation.Id <= 0 )
            {
                return null;
            }

            return new Location()
            {
                Id = checkInLocation.Id,
                Name = checkInLocation.Name,
                LocationType = LocationType.MeetingLocation
            };
        }
    }
}
