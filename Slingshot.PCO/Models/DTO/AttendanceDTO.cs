using Slingshot.PCO.Models.ApiModels;
using Slingshot.PCO.Utilities;
using System;
using System.Linq;

namespace Slingshot.PCO.Models.DTO
{
    public class AttendanceDTO
    {
        public bool IsValid { get; set; }

        public int Id { get; set; }

        public bool? Attended { get; set; }

        public string Role { get; set; }

        public int PersonId { get; set; }

        public int GroupId { get; set; }

        public int LocationId { get; set; }

        public DateTime? StartsAt { get; set; }

        public DateTime? EndsAt { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public AttendanceDTO( DataItem data, GroupEventDTO eventData, int groupId )
        {
            IsValid = ( data.Item.attended == true );
            Id = data.Id;
            Attended = data.Item.attended;
            Role = data.Item.role;
            Description = ( ( string ) data.Item.description ).StripHtml();
            Name = data.Item.name;
            GroupId = groupId;
            SetPersonId( data );
            SetEventData( data, eventData );
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

        private void SetEventData( DataItem data, GroupEventDTO eventData )
        {
            StartsAt = eventData.StartsAt;
            EndsAt = eventData.EndsAt;
            Name = eventData.Name;
            Description = eventData.Description;
        }
    }
}
