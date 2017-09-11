using System;
using System.Data;

using Slingshot.Core.Model;

namespace Slingshot.ACS.Utilities.Translators
{
    public static class AcsFinancialTransaction
    {
        public static FinancialTransaction Translate( DataRow row )
        {
            var financialTransaction = new FinancialTransaction();

            financialTransaction.Id = row.Field<int>( "TransactionID" );
            financialTransaction.TransactionCode = row.Field<string>( "CheckNumber" );
            financialTransaction.TransactionDate = row.Field<DateTime?>( "GiftDate" );
            financialTransaction.AuthorizedPersonId = row.Field<int>( "IndividualId" );

            // payment types can vary from Church to Church, so using the most popular ones here
            var source = row.Field<string>( "PaymentType" );
            switch ( source )
            {
                case "Online":
                    financialTransaction.TransactionSource = TransactionSource.Website;
                    break;
                case "Check":
                    financialTransaction.TransactionSource = TransactionSource.BankChecks;
                    financialTransaction.CurrencyType = CurrencyType.Check;
                    break;
                case "Credit Card":
                    financialTransaction.CurrencyType = CurrencyType.CreditCard;
                    break;
                case "Cash":
                    financialTransaction.TransactionSource = TransactionSource.OnsiteCollection;
                    financialTransaction.CurrencyType = CurrencyType.Cash;
                    break;
                default:
                    financialTransaction.TransactionSource = TransactionSource.OnsiteCollection;
                    financialTransaction.CurrencyType = CurrencyType.Unknown;
                    break;
            }

            // adding the original ACS payment type to the transaction summary for reference
            financialTransaction.Summary = "ACS PaymentType: " + source;

            financialTransaction.BatchId = 9999;

            financialTransaction.TransactionType = TransactionType.Contribution;

            financialTransaction.CreatedDateTime = row.Field<DateTime?>( "GiftDate" );

            return financialTransaction;
        }
    }
}
