using Slingshot.PCO.Models.ApiModels;
using Slingshot.PCO.Utilities;

namespace Slingshot.PCO.Models.DTO
{
    public class TeamPositionDTO
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int TeamId { get; set; }

        public TeamPositionDTO( DataItem data, int teamId )
        {
            Id = data.Id;
            Name = data.Item.name;
            TeamId = teamId;
        }
    }
}
