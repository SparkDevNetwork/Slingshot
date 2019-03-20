using System.Collections.Generic;
using System.Linq;
using Slingshot.Core.Model;

namespace Slingshot.Breeze.Utilities.Translators
{
    public static class BreezeGift
    {
        public static FinancialTransaction Translate( IDictionary<string, object> record, List<FinancialAccount> accounts, List<FinancialBatch> batches )
        {
            if ( record == null || !record.Keys.Any() )
            {
                return null;
            }

            var accountName = CsvFieldTranslators.GetString( "Fund(s)", record );
            var account = accounts.FirstOrDefault( a => a.Name == accountName );

            if ( account == null )
            {
                account = new FinancialAccount
                {
                    // CampusId
                    Id = accounts.Count + 1,
                    Name = accountName
                };

                accounts.Add( account );
            }

            // Map the properties of Person Note class to known CSV headers
            // Maybe this could be configurable to the user in the UI if the need arises
            var transactionPropertyToCsvFieldNameMap = new Dictionary<string, string> {
                { "Id", "Payment ID" },
                { "BatchId", "Batch" },
                { "AuthorizedPersonId", "Person ID" },
                { "TransactionDate", "Date" },
                // { "TransactionType", "" }
                { "TransactionSource", "Method ID" },
                { "CurrencyType", "Method ID" },
                { "Summary", "Note" },
                // TransactionCode
                { "CreatedDateTime", "Date" }
                // ModifiedByPersonId
                // ModifiedDateTime
            };

            var detailPropertyToCsvFieldNameMap = new Dictionary<string, string> {
                { "Id", "Payment ID" },
                { "TransactionId", "Payment ID" },
                // AccountId
                { "Amount", "Amount" },
                { "Summary", "Note" },
                // CreatedByPersonId
                { "CreatedDateTime", "Date" }
                // ModifiedByPersonId
                // ModifiedDateTime
            };

            // Create a person note object. Using the map, read values from the CSV record and
            // set the associated properties of the person with those values
            var transaction = new FinancialTransaction();           
            var transactionType = transaction.GetType();

            foreach ( var kvp in transactionPropertyToCsvFieldNameMap )
            {
                var propertyName = kvp.Key;
                var csvFieldName = kvp.Value;
                var property = transactionType.GetProperty( propertyName );
                var value = CsvFieldTranslators.GetValue( property.PropertyType, csvFieldName, record );

                property.SetValue( transaction, value );
            }

            var detail = new FinancialTransactionDetail
            {
                AccountId = account.Id
            };
            var detailType = detail.GetType();
            transaction.FinancialTransactionDetails.Add( detail );

            foreach ( var kvp in detailPropertyToCsvFieldNameMap )
            {
                var propertyName = kvp.Key;
                var csvFieldName = kvp.Value;
                var property = detailType.GetProperty( propertyName );
                var value = CsvFieldTranslators.GetValue( property.PropertyType, csvFieldName, record );

                property.SetValue( detail, value );
            }

            var existingBatch = batches.FirstOrDefault( b => b.Id == transaction.BatchId );

            if (existingBatch == null)
            {
                existingBatch = new FinancialBatch
                {
                    Id = transaction.BatchId,
                    Name = "Breeze Transactions",
                    Status = BatchStatus.Closed
                };

                batches.Add( existingBatch );
            }

            existingBatch.FinancialTransactions.Add( transaction );

            // Batch doesn't have a start date or this transaction is before the start date
            if ( !existingBatch.StartDate.HasValue ||
                ( transaction.TransactionDate.HasValue &&
                transaction.TransactionDate.Value < existingBatch.StartDate.Value ) )
            {
                existingBatch.StartDate = transaction.TransactionDate;
            }

            // TODO store unused values, like checknumber, as attributes when slingshot supports it

            return transaction;
        }
    }
}
