using Slingshot.Core.Model;
using Slingshot.PCO.Models.DTO;

namespace Slingshot.PCO.Utilities.Translators
{
    public static class PCOImportSchedule
    {
        public static Schedule Translate( CheckInEventDTO checkInEvent )
        {
            if ( checkInEvent.Id <= 0 )
            {
                return null;
            }

            return new Schedule()
            {
                Id = checkInEvent.Id,
                Name = checkInEvent.Name
            };
        }
    }
}
