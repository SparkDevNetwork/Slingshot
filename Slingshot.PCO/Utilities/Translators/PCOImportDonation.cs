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
    public static class PCOImportDonation
    {
        
        public static FinancialTransaction Translate( PCODonation inputTransaction )
        {
            var transaction = new FinancialTransaction();

            transaction.Id = inputTransaction.id;

            if ( inputTransaction.batchId.HasValue )
            {
                transaction.BatchId = inputTransaction.batchId.Value;
            }
            if( inputTransaction.payment_method == "check" )
            {

                transaction.TransactionCode = inputTransaction.payment_check_number;

            } else if ( inputTransaction.payment_method != "cash" )
            {

                transaction.TransactionCode = inputTransaction.payment_last4;

            }

            transaction.TransactionDate = inputTransaction.received_at;

            transaction.AuthorizedPersonId = inputTransaction.personId;
            
            switch ( inputTransaction.payment_method )
            {
                case "cash":
                    transaction.CurrencyType = CurrencyType.Cash;
                    break;
                case "check":
                    transaction.CurrencyType = CurrencyType.Check;
                    break;
                case "credit card":
                case "debit card":
                case "card":
                    transaction.CurrencyType = CurrencyType.CreditCard;
                    break;
                case "ach":
                    transaction.CurrencyType = CurrencyType.ACH;
                    break;
                case "non-cash":
                    transaction.CurrencyType = CurrencyType.NonCash;
                    break;
                default:
                    transaction.CurrencyType = CurrencyType.Unknown;
                    break;
            }

            transaction.CreatedDateTime = inputTransaction.created_at;

            transaction.ModifiedDateTime = inputTransaction.updated_at;

            return transaction;
        }
    }
}
