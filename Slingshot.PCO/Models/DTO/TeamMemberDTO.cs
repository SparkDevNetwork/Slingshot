using Slingshot.PCO.Models.ApiModels;
using System;
using System.Linq;

namespace Slingshot.PCO.Models.DTO
{
    public class TeamMemberDTO
    {
        public int Id { get; set; }

        public int PersonId { get; set; }

        public int TeamId { get; set; }

        public string TeamPosition { get; set; }

        public TeamMemberDTO( DataItem data, int teamId, string teamPosition )
        {
            Id = data.Id;
            TeamId = teamId;
            TeamPosition = teamPosition;
            SetPersonId( data );
        }

        private void SetPersonId( DataItem data )
        {
            var personRelationship = data.Relationships?.Person?.Data?.FirstOrDefault();
            if ( personRelationship == null )
            {
                return;
            }

            PersonId = personRelationship.Id;
        }
    }
}
