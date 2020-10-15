using Slingshot.PCO.Models.ApiModels;
using System;

namespace Slingshot.PCO.Models.DTO
{
    public class HouseholdDTO
    {
        public int Id { get; set; }

        public DateTime? CreatedAt { get; set; }

        public int? MemberCount { get; set; }

        public string Name { get; set; }

        public int? PrimaryContactId { get; set; }

        public string PrimaryContactName { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public HouseholdDTO( DataItem data )
        {
            Id = data.Id;
            CreatedAt = data.Item.created_at;
            MemberCount = data.Item.member_count;
            Name = data.Item.name;
            PrimaryContactId = data.Item.primary_contact_id;
            PrimaryContactName = data.Item.primary_contact_name;
            UpdatedAt = data.Item.updated_at;
        }
    }
}
