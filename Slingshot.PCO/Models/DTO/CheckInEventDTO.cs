using Slingshot.PCO.Models.ApiModels;
using Slingshot.PCO.Utilities;
using System;

namespace Slingshot.PCO.Models.DTO
{
    public class CheckInEventDTO
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Frequency { get; set; }
        public bool? EnableServicesIntegration { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? ArchivedAt { get; set; }
        public string IntegrationKey { get; set; }
        public bool? LocationTimesEnabled { get; set; }

        public CheckInEventDTO( DataItem data )
        {
            Id = data.Id;
            Name = data.Item.name;
            Frequency = data.Item.frequency;
            EnableServicesIntegration = data.Item.enable_services_integration;
            CreatedAt = data.Item.created_at;
            UpdatedAt = data.Item.updated_at;
            ArchivedAt = data.Item.archived_at;
            IntegrationKey = data.Item.integration_key;
            LocationTimesEnabled = data.Item.location_times_enabled;
        }
    }
}
