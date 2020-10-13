using Slingshot.PCO.Models.ApiModels;
using System;

namespace Slingshot.PCO.Models.DTO
{
    public class BatchDTO
    {
        public int Id { get; set; }

        public DateTime? CommittedAt { get; set; }

        public DateTime? CreatedAt { get; set; }

        public string Description { get; set; }

        public int? TotalCents { get; set; }

        public string TotalCurrency { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public int? OwnerId { get; set; }

        public BatchDTO( DataItem data )
        {
            Id = data.Id;
            CommittedAt = data.Item.committed_at;
            CreatedAt = data.Item.created_at;
            Description = data.Item.description;
            TotalCents = data.Item.total_cents;
            TotalCurrency = data.Item.total_currency;
            UpdatedAt = data.Item.updated_at;

            SetOwnerId( data );
        }

        private void SetOwnerId( DataItem data )
        {
            if ( data.Relationships == null || data.Relationships.Owner == null )
            {
                return;
            }

            foreach ( var relationship in data.Relationships.Owner.Data )
            {
                if ( relationship == null )
                {
                    continue;
                }

                OwnerId = relationship.Id;
            }
        }
    }
}
