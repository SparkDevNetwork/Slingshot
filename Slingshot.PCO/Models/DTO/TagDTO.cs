using Slingshot.PCO.Models.ApiModels;
using System.Collections.Generic;
using System.Linq;

namespace Slingshot.PCO.Models.DTO
{
    public class TagDTO
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int? position { get; set; }

        public TagGroupDTO TagGroup { get; set; }

        public string GroupAttributeValue
        {
            get
            {
                return this.Name.Replace( ",", "&comma;" );
            }
        }

        public TagDTO( DataItem data, List<TagGroupDTO> tagGroups )
        {
            Id = data.Id;
            Name = data.Item.name;
            SetTagGroup( data, tagGroups );
        }

        private void SetTagGroup( DataItem data, List<TagGroupDTO> tagGroups )
        {
            var tagGroupId = data.Relationships.TagGroup.Data.FirstOrDefault().Id;
            var tagGroup = tagGroups.Where( g => g.Id == tagGroupId ).FirstOrDefault();
            TagGroup = tagGroup;
        }
    }
}
