using Slingshot.Core.Model;
using Slingshot.PCO.Models.DTO;

namespace Slingshot.PCO.Utilities.Translators
{
    public static class PCOImportServiceType
    {
        // This ensures that group type ids for services start at one billion, to avoid conflicting with other group types.  If there is a group type with an
        // id over this value, the service/team export will not be run.
        public const int SERVICE_TYPE_ID_BASE = 999999999;

        public static GroupType Translate( ServiceTypeDTO inputServiceType )
        {
            if ( inputServiceType.Id <= 0 )
            {
                return null;
            }

            return new GroupType()
            {
                Id = inputServiceType.Id + SERVICE_TYPE_ID_BASE,
                Name = inputServiceType.Name
            };
        }
    }
}
