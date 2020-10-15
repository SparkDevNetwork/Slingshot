using Slingshot.PCO.Models.ApiModels;

namespace Slingshot.PCO.Models.DTO
{
    public class DesignationDTO
    {
        public int Id { get; set; }

        public int? AmountCents { get; set; }

        public string AmountCurrency { get; set; }

        public int? FundId { get; set; }

        public DesignationDTO( DataItem data )
        {
            Id = data.Id;
            AmountCents = data.Item.amount_cents;
            AmountCurrency = data.Item.amount_currency;

            SetFundId( data );
        }

        private void SetFundId( DataItem data )
        {
            if ( data.Relationships == null || data.Relationships.Fund == null )
            {
                return;
            }

            foreach ( var relationship in data.Relationships.Fund.Data )
            {
                if ( relationship == null )
                {
                    continue;
                }

                FundId = relationship.Id;
            }
        }
    }
}
