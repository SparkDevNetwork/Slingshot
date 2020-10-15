using Slingshot.PCO.Models.ApiModels;

namespace Slingshot.PCO.Models.DTO
{
    public class TagDTO
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Key { get { return string.Format( "PCOTag:{0}", Id ); } }

        public TagDTO( DataItem data )
        {
            Id = data.Id;
            Name = data.Item.name;
        }
    }
}
