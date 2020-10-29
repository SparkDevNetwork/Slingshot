using Slingshot.Core.Model;
using Slingshot.PCO.Models.DTO;

namespace Slingshot.PCO.Utilities.Translators
{
    public static class PCOImportGroupAttribute
    {
        public static GroupAttribute Translate( TagGroupDTO tagGroup )
        {
            if ( tagGroup.Id <= 0 )
            {
                return null;
            }

            return new GroupAttribute()
            {
                Category = "PCO Tag Groups",
                FieldType = "Rock.Field.Types.TextFieldType",
                Key = tagGroup.GroupAttributeKey,
                Name = tagGroup.Name
            };
        }
    }
}
