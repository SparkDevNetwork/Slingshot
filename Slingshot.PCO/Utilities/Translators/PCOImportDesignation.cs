using Slingshot.Core.Model;
using Slingshot.PCO.Models.DTO;
using System;
using System.Collections.Generic;

namespace Slingshot.PCO.Utilities.Translators
{
    public static class PCOImportDesignation
    {
        public static List<FinancialTransactionDetail> Translate( DonationDTO inputDonation )
        {
            var transactionDetails = new List<FinancialTransactionDetail>();

            foreach ( var inputTransactionDetail in inputDonation.Designations )
            {
                var transactionDetail = new FinancialTransactionDetail
                {
                    Id = inputTransactionDetail.Id,
                    AccountId = inputTransactionDetail.FundId,
                    Amount = Convert.ToDecimal( inputTransactionDetail.AmountCents.Value / 100.00 ),
                    TransactionId = inputDonation.Id,
                    CreatedDateTime = inputDonation.CreatedAt,
                    ModifiedDateTime = inputDonation.UpdatedAt
                };

                transactionDetails.Add( transactionDetail );
            }

            return transactionDetails;
        }
    }
}
