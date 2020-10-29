using Slingshot.PCO.Models.ApiModels;
using Slingshot.PCO.Utilities;
using System;

namespace Slingshot.PCO.Models.DTO
{
    public class FundDTO
    {
        public int Id { get; set; }

        public string Color { get; set; }

        public DateTime? CreatedAt { get; set; }

        public bool? Deletable { get; set; }

        public string Description { get; set; }

        public string LedgerCode { get; set; }

        public string Name { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public string Visibility { get; set; }

        public FundDTO( DataItem data )
        {
            Id = data.Id;
            Color = data.Item.color;
            CreatedAt = data.Item.created_at;
            Deletable = data.Item.deltable;
            Description = ( ( string ) data.Item.description ).StripHtml();
            LedgerCode = data.Item.ledger_code;
            Name = data.Item.name;
            UpdatedAt = data.Item.updated_at;
            Visibility = data.Item.visibility;
        }
    }
}
