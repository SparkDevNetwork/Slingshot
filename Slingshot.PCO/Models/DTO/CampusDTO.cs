using Slingshot.PCO.Models.ApiModels;
using Slingshot.PCO.Utilities;
using System;

namespace Slingshot.PCO.Models.DTO
{
    public class CampusDTO
    {
        public int Id { get; set; }

        public string AvatarUrl { get; set; }

        public string City { get; set; }

        public string ContactEmailAddress { get; set; }

        public string Country { get; set; }

        public DateTime? CreatedAt { get; set; }

        public string Description { get; set; }

        public string Latitude { get; set; }

        public string Longitude { get; set; }

        public string Name { get; set; }

        public string PhoneNumber { get; set; }

        public string State { get; set; }

        public string Street { get; set; }

        public string TimeZone { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public string Website { get; set; }

        public string Zip { get; set; }

        public CampusDTO( DataItem data )
        {
            Id = data.Id;
            AvatarUrl = data.Item.avatar_url;
            City = data.Item.city;
            ContactEmailAddress = data.Item.contact_email_address;
            Country = data.Item.country;
            CreatedAt = data.Item.created_at;
            Description = ( ( string ) data.Item.description ).StripHtml();
            Latitude = data.Item.latitude;
            Longitude = data.Item.longitude;
            Name = data.Item.name;
            PhoneNumber = data.Item.phone_number;
            State = data.Item.state;
            Street = data.Item.street;
            TimeZone = data.Item.time_zone;
            UpdatedAt = data.Item.updated_at;
            Website = data.Item.website;
            Zip = data.Item.zip;
        }
    }
}
