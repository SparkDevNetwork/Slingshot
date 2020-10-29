using Slingshot.Core.Model;
using Slingshot.PCO.Models.DTO;

namespace Slingshot.PCO.Utilities.Translators
{
    public static class PCOImportGroupAttributeValue
    {
        public static GroupAttributeValue Translate( TagDTO tag, int groupId )
        {
            if ( tag.Id <= 0 )
            {
                return null;
            }

            return new GroupAttributeValue()
            {
                AttributeKey = tag.TagGroup.GroupAttributeKey,
                AttributeValue = tag.GroupAttributeValue,
                GroupId = groupId
            };
        }
    }
}
