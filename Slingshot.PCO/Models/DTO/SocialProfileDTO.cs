using Slingshot.PCO.Models.ApiModels;
using System;

namespace Slingshot.PCO.Models.DTO
{
    public class SocialProfileDTO
    {
        public int Id { get; set; }

        public DateTime? CreatedAt { get; set; }

        public string Site { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public string Url { get; set; }

        public bool Verified { get; set; }

        public SocialProfileDTO( DataItem data )
        {
            Id = data.Id;
            CreatedAt = data.Item.created_at;
            Site = data.Item.site;
            UpdatedAt = data.Item.updated_at;
            Url = data.Item.url;
            Verified = data.Item.verified;
        }
    }
}
