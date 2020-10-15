using Slingshot.PCO.Models.ApiModels;
using Slingshot.PCO.Utilities;
using System;
using System.Collections.Generic;

namespace Slingshot.PCO.Models.DTO
{
    public class DonationDTO
    {
        public int Id { get; set; }

        public int? AmountCents { get; set; }

        public string AmountCurrency { get; set; }

        public DateTime? CreatedAt { get; set; }

        public int? FeeCents { get; set; }

        public string FeeCurrency { get; set; }

        public string PaymentBrand { get; set; }

        public DateTime? PaymentCheckDatedAt { get; set; }

        public string PaymentCheckNumber { get; set; }

        public string PaymentLastFour { get; set; }

        public string PaymentMethod { get; set; }

        public string PaymentMethodSub { get; set; }

        public string PaymentStatus { get; set; }

        public DateTime? ReceivedAt { get; set; }

        public bool? Refundable { get; set; }

        public bool? Refunded { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public List<DesignationDTO> Designations { get; set; }

        public int? BatchId { get; set; }

        public int? PersonId { get; set; }

        public DonationDTO( DataItem data, Dictionary<string, DataItem> includedItems )
        {
            Id = data.Id;
            AmountCents = data.Item.amount_cents;
            AmountCurrency = data.Item.amount_currency;
            CreatedAt = data.Item.created_at;
            FeeCents = data.Item.fee_cents;
            FeeCurrency = data.Item.fee_currency;
            PaymentBrand = data.Item.payment_brand;
            PaymentCheckDatedAt = data.Item.payment_check_dated_at;
            PaymentCheckNumber = data.Item.payment_check_number;
            PaymentLastFour = data.Item.payment_last4;
            PaymentMethod = data.Item.payment_method;
            PaymentMethodSub = data.Item.payment_method_sub;
            PaymentStatus = data.Item.payment_status;
            ReceivedAt = data.Item.received_at;
            Refundable = data.Item.refundable;
            Refunded = data.Item.refunded;
            UpdatedAt = data.Item.updated_at;

            SetBatchId( data );
            SetPersonId( data );
            SetDesignation( data, includedItems );
        }

        private void SetBatchId( DataItem data )
        {
            if ( data.Relationships == null || data.Relationships.Batch == null )
            {
                return;
            }

            foreach ( var relationship in data.Relationships.Batch.Data )
            {
                if( relationship == null )
                {
                    continue;
                }

                BatchId = relationship.Id;
            }
        }

        private void SetPersonId( DataItem data )
        {
            if ( data.Relationships == null || data.Relationships.Person == null )
            {
                return;
            }

            foreach ( var relationship in data.Relationships.Person.Data )
            {
                if ( relationship == null )
                {
                    continue;
                }

                PersonId = relationship.Id;
            }

        }

        private void SetDesignation( DataItem data, Dictionary<string, DataItem> included )
        {
            Designations = new List<DesignationDTO>();

            if ( data.Relationships == null || data.Relationships.Designations == null )
            {
                return;
            }
            foreach ( var relationship in data.Relationships.Designations.Data )
            {
                var item = included.LocateItem( relationship );

                if ( item == null )
                {
                    continue;
                }

                Designations.Add( new DesignationDTO( item ) );
            }
        }
    }
}
