using Slingshot.Core.Model;
using Slingshot.PCO.Models.DTO;

namespace Slingshot.PCO.Utilities.Translators
{
    public static class PCOImportGroup
    {
        public static Group Translate( GroupDTO inputGroup )
        {
            if ( inputGroup.Id <= 0 )
            {
                return null;
            }

            return new Group()
            {
                Id = inputGroup.Id,
                IsActive = !inputGroup.Archived.HasValue,
                Description = inputGroup.Description,
                Name = inputGroup.Name,
                GroupTypeId = inputGroup.GroupType.Id,
                IsPublic = ( inputGroup.GroupType.ChurchCenterVisible == true )
            };
        }
    }
}
