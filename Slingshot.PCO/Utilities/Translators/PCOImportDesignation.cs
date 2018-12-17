using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Slingshot.Core;
using Slingshot.Core.Model;
using Slingshot.PCO.Models;

namespace Slingshot.PCO.Utilities.Translators
{
    public static class PCOImportDesignation
    {
        public static List<FinancialTransactionDetail> Translate( PCODonation inputDonation )
        {
            var transactionDetails = new List<FinancialTransactionDetail>();

            foreach( var inputTransactionDetail in inputDonation.designations )
            {
                var transactionDetail = new FinancialTransactionDetail();

                transactionDetail.Id = inputTransactionDetail.id;
                
                transactionDetail.AccountId = inputTransactionDetail.fundId;

                transactionDetail.Amount = Convert.ToDecimal( inputTransactionDetail.amount_cents.Value / 100.00 );
                transactionDetail.TransactionId = inputDonation.id;

                transactionDetail.CreatedDateTime = inputDonation.created_at;

                transactionDetail.ModifiedDateTime = inputDonation.updated_at;

                transactionDetails.Add( transactionDetail );
            }

            return transactionDetails;
        }
    }
}
