using System;
using System.Data;

using Slingshot.Core.Model;

namespace Slingshot.ServantKeeper.Utilities.Translators
{
    public static class SkFinancialContribution
    {
        public static FinancialTransaction Translate( DataRow row )
        {
            int? ID;
            string field;
            FinancialTransaction transaction = new FinancialTransaction();

            // Identifiers
            transaction.Id = row.Field<int>("CONTRIBUTION_ID");
            transaction.BatchId = row.Field<int>("BATCH_ID");
            ID = row.Field<int?>("PERSON_ID");
            if (ID != null) transaction.AuthorizedPersonId = ID;  // May not be found in either the csIND or csINDBin tables

            // Other fields
            transaction.TransactionCode = row.Field<string>("CHECK_NO");
            transaction.TransactionDate = row.Field<DateTime?>( "BATCH_DT" );
            transaction.CreatedDateTime = row.Field<DateTime?>("CREATE_TS");
            transaction.ModifiedDateTime = row.Field<DateTime?>("UPDATE_TS");
            transaction.Summary = row.Field<string>("NOTE");
            if (row.Field<string>("TAX_IND") == "1") transaction.TransactionType = TransactionType.Contribution;


            // Payment types can vary from Church to Church, so using the most popular ones here
            field = row.Field<string>( "PAY_TYPE" );
            switch ( field )
            {
                case "L":  // Online
                    transaction.TransactionSource = TransactionSource.Website;
                    transaction.CurrencyType = CurrencyType.CreditCard;
                    break;
                case "C":  // Check
                    transaction.TransactionSource = TransactionSource.OnsiteCollection;
                    transaction.CurrencyType = CurrencyType.Check;
                    break;
                case "S":  // Cash
                    transaction.TransactionSource = TransactionSource.OnsiteCollection;
                    transaction.CurrencyType = CurrencyType.Cash;
                    break;
                case "O":  // Other
                    transaction.TransactionSource = TransactionSource.OnsiteCollection;
                    transaction.CurrencyType = CurrencyType.Unknown;  // Slingshot won't import the Other value
                    break;
                default:  // Unknown
                    transaction.TransactionSource = TransactionSource.OnsiteCollection;
                    transaction.CurrencyType = CurrencyType.Unknown;
                    break;
            }

            return transaction;
        }
    }
}
