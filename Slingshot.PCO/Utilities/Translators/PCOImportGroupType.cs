using Slingshot.Core.Model;
using Slingshot.PCO.Models.DTO;

namespace Slingshot.PCO.Utilities.Translators
{
    public static class PCOImportGroupType
    {
        public static GroupType Translate( GroupTypeDTO inputGroupType )
        {
            if ( inputGroupType.Id <= 0 )
            {
                return null;
            }

            return new GroupType()
            {
                Id = inputGroupType.Id,
                Name = inputGroupType.Name
            };
        }
    }
}
