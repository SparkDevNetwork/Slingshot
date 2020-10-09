using Slingshot.Core.Model;
using Slingshot.PCO.Models;

namespace Slingshot.PCO.Utilities.Translators
{
    public static class PCOImportDonation
    {
        public static FinancialTransaction Translate( PCODonation inputTransaction )
        {
            var currencyType = CurrencyType.Unknown;
            switch ( inputTransaction.PaymentMethod )
            {
                case "cash":
                    currencyType = CurrencyType.Cash;
                    break;
                case "check":
                    currencyType = CurrencyType.Check;
                    break;
                case "credit card":
                    currencyType = CurrencyType.CreditCard;
                    break;
                case "debit card":
                    currencyType = CurrencyType.CreditCard;
                    break;
                case "card":
                    currencyType = CurrencyType.CreditCard;
                    break;
                case "ach":
                    currencyType = CurrencyType.ACH;
                    break;
                case "non-cash":
                    currencyType = CurrencyType.NonCash;
                    break;
            }

            var transactionCode = string.Empty;
            if ( inputTransaction.PaymentMethod == "check" )
            {
                transactionCode = inputTransaction.PaymentCheckNumber;
            }
            else if ( inputTransaction.PaymentMethod != "cash" )
            {
                transactionCode = inputTransaction.PaymentLastFour;
            }

            var transaction = new FinancialTransaction
            {
                Id = inputTransaction.Id,
                TransactionDate = inputTransaction.ReceivedAt,
                AuthorizedPersonId = inputTransaction.PersonId,
                CreatedDateTime = inputTransaction.CreatedAt,
                ModifiedDateTime = inputTransaction.UpdatedAt,
                CurrencyType = currencyType,
                TransactionCode = transactionCode,
                BatchId = ( inputTransaction.BatchId.HasValue ) ? inputTransaction.BatchId.Value : default( int )
            };

            return transaction;
        }
    }
}
