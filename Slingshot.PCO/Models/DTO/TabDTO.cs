using Slingshot.PCO.Models.ApiModels;

namespace Slingshot.PCO.Models.DTO
{
    public class TabDTO
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int Sequence { get; set; }

        public string Slug { get; set; }

        public TabDTO( DataItem data )
        {
            Id = data.Id;
            Sequence = data.Item.sequence;
            Slug = data.Item.slug;
            Name = data.Item.name;
        }
    }
}
