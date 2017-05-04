using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Slingshot.Core;
using Slingshot.Core.Model;

namespace Slingshot.CCB.Utilities.Translators
{
    public static class CcbFinancialTransaction
    {
        public static FinancialTransaction Translate(XElement inputTransaction, int batchId)
        {
            var financialTransaction = new FinancialTransaction();

            financialTransaction.Id = inputTransaction.Attribute( "id" ).Value.AsInteger();
            financialTransaction.BatchId = batchId;
            financialTransaction.Summary = inputTransaction.Element( "grouping" )?.Value;
            financialTransaction.TransactionCode = inputTransaction.Element( "check_number" )?.Value;
            financialTransaction.TransactionDate = inputTransaction.Element( "date" )?.Value.AsDateTime();
            financialTransaction.AuthorizedPersonId = inputTransaction.Element( "individual" )?.Attribute("id")?.Value.AsIntegerOrNull();

            // note the api doesn't tell us the currency type
            financialTransaction.CurrencyType = CurrencyType.Unknown;

            var source = inputTransaction.Element( "payment_type" )?.Value;
            switch( source )
            {
                case "Online":
                    financialTransaction.TransactionSource = TransactionSource.Website;
                    break;
                case "Cash":
                    financialTransaction.TransactionSource = TransactionSource.OnsiteCollection;
                    break;
                default:
                    financialTransaction.TransactionSource = TransactionSource.OnsiteCollection; // best default?
                    break;
            }

            financialTransaction.TransactionType = TransactionType.Contribution;

            financialTransaction.CreatedDateTime = inputTransaction.Element( "created" )?.Value.AsDateTime();
            financialTransaction.ModifiedDateTime = inputTransaction.Element( "modified" )?.Value.AsDateTime();

            financialTransaction.CreatedByPersonId = inputTransaction.Element( "creator" )?.Attribute( "id" )?.Value.AsIntegerOrNull();
            financialTransaction.ModifiedByPersonId = inputTransaction.Element( "modifier" )?.Attribute( "id" )?.Value.AsIntegerOrNull();

            return financialTransaction;
        }
    }
}
