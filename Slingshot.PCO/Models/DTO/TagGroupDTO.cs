using Slingshot.Core;
using Slingshot.PCO.Models.ApiModels;

namespace Slingshot.PCO.Models.DTO
{
    public class TagGroupDTO
    {
        public int Id { get; set; }

        public bool? DisplayPublicly { get; set; }

        public bool? MultipleOptionsEnabled { get; set; }

        public string Name { get; set; }

        public int? Position { get; set; }

        public string GroupAttributeKey
        {
            get
            {
                string attributeKeyName = this.Name
                    .Replace( " ", "_" )
                    .RemoveSpecialCharacters();

                return $"PCO_{attributeKeyName}_{this.Id}";
            }
        }

        public TagGroupDTO( DataItem data )
        {
            Id = data.Id;
            DisplayPublicly = data.Item.display_publicly;
            MultipleOptionsEnabled = data.Item.multiple_options_enabled;
            Name = data.Item.name;
            Position = data.Item.position;
        }
    }
}
