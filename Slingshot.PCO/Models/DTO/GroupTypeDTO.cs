using Slingshot.PCO.Models.ApiModels;
using Slingshot.PCO.Utilities;

namespace Slingshot.PCO.Models.DTO
{
    public class GroupTypeDTO
    {
        public int Id { get; set; }

        public bool? ChurchCenterVisible { get; set; }

        public bool? ChurchCenterMapVisible { get; set; }

        public string Color { get; set; }

        public string Description { get; set; }

        public string Name { get; set; }

        public int? Position { get; set; }

        public GroupTypeDTO( DataItem data )
        {
            Id = data.Id;
            ChurchCenterVisible = data.Item.church_center_visible;
            ChurchCenterMapVisible = data.Item.church_center_map_visible;
            Color = data.Item.color;
            Description = ( ( string ) data.Item.description ).StripHtml();
            Name = data.Item.name;
            Position = data.Item.position;
        }
    }
}
