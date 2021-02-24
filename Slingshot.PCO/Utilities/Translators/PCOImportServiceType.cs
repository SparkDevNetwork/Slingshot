using Slingshot.Core.Model;
using Slingshot.PCO.Models.DTO;

namespace Slingshot.PCO.Utilities.Translators
{
    public static class PCOImportServiceType
    {
        public static GroupType Translate( ServiceTypeDTO inputServiceType )
        {
            if ( inputServiceType.Id <= 0 )
            {
                return null;
            }

            return new GroupType()
            {
                Id = inputServiceType.Id,
                Name = inputServiceType.Name
            };
        }
    }
}
