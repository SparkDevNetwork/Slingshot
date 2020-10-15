using Slingshot.PCO.Models.ApiModels;

namespace Slingshot.PCO.Models.DTO
{
    public class NoteCategoryDTO
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public NoteCategoryDTO( DataItem data )
        {
            Id = data.Id;
            Name = data.Item.name;
        }
    }
}
    



