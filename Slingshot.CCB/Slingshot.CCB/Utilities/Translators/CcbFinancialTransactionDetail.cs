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
    public static class CcbFinancialTransactionDetail
    {
        public static FinancialTransactionDetail Translate(XElement inputTransactionDetail, int transactionId)
        {
            var financialTransactionDetail = new FinancialTransactionDetail();

            financialTransactionDetail.Id = inputTransactionDetail.Attribute( "id" ).Value.AsInteger();
            financialTransactionDetail.AccountId = inputTransactionDetail.Element( "coa" )?.Attribute( "id" )?.Value.AsInteger();
            financialTransactionDetail.Amount = inputTransactionDetail.Element( "amount" ).Value.AsDecimal();
            financialTransactionDetail.Summary = $"Contribution to: {inputTransactionDetail.Element( "coa" )?.Value} {inputTransactionDetail.Element( "note" )?.Value}";
            financialTransactionDetail.TransactionId = transactionId;

            financialTransactionDetail.CreatedDateTime = inputTransactionDetail.Element( "created" )?.Value.AsDateTime();
            financialTransactionDetail.ModifiedDateTime = inputTransactionDetail.Element( "modified" )?.Value.AsDateTime();

            financialTransactionDetail.CreatedByPersonId = inputTransactionDetail.Element( "creator" )?.Attribute("id")?.Value.AsIntegerOrNull();
            financialTransactionDetail.ModifiedByPersonId = inputTransactionDetail.Element( "modifier" )?.Attribute( "id" )?.Value.AsIntegerOrNull();

            return financialTransactionDetail;
        }
    }
}
