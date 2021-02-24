using Slingshot.PCO.Models.ApiModels;
using Slingshot.PCO.Utilities;
using System;
using System.Linq;

namespace Slingshot.PCO.Models.DTO
{
    public class GroupDTO
    {
        public int Id { get; set; }

        public DateTime? Archived { get; set; }

        public string ContactEmail { get; set; }

        public DateTime? CreatedAt { get; set; }

        public string Description { get; set; }

        public bool? EnrollmentOpen { get; set; }

        public string LocationTypePreference { get; set; }

        public int? MembershipsCount { get; set; }

        public string Name { get; set; }

        public string PublicChurchCenterWebUrl { get; set; }

        public string Schedule { get; set; }

        public string VirtualLocationUrl { get; set; }

        public GroupTypeDTO GroupType { get; set; }

        public bool HasLocation { get; set; }

        public GroupDTO( DataItem data, GroupTypeDTO groupType )
        {
            Id = data.Id;
            Archived = data.Item.archived_at;
            ContactEmail = data.Item.contact_email;
            CreatedAt = data.Item.created_at;
            Description = ( ( string ) data.Item.description ).StripHtml();
            EnrollmentOpen = data.Item.enrollment_open;
            LocationTypePreference = data.Item.location_type_preference;
            MembershipsCount = data.Item.memberships_count;
            Name = data.Item.name;
            PublicChurchCenterWebUrl = data.Item.public_church_center_web_url;
            Schedule = data.Item.schedule;
            VirtualLocationUrl = data.Item.virtual_location_url;
            GroupType = groupType;
            HasLocation = ( data.Relationships?.Locations?.Data?.FirstOrDefault() != null );
        }
    }
}
