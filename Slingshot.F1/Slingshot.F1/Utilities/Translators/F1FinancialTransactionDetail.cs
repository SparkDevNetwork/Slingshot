using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Slingshot.Core;
using Slingshot.Core.Model;

namespace Slingshot.F1.Utilities.Translators
{
    public static class F1FinancialTransactionDetail
    {
        public static FinancialTransactionDetail Translate( XElement inputTransactionDetail )
        {
            var transactionDetail = new FinancialTransactionDetail();

            transactionDetail.Id = inputTransactionDetail.Attribute( "id" ).Value.AsInteger();
            transactionDetail.AccountId = inputTransactionDetail.Element( "fund" )?.Attribute( "id" )?.Value.AsInteger();
            transactionDetail.Amount = inputTransactionDetail.Element( "amount" ).Value.AsDecimal();
            transactionDetail.TransactionId = inputTransactionDetail.Attribute( "id" ).Value.AsInteger();

            transactionDetail.CreatedDateTime = inputTransactionDetail.Element( "createdDate" )?.Value.AsDateTime();
            transactionDetail.CreatedByPersonId = inputTransactionDetail.Element( "createdByPerson" ).Attribute( "id" )?.Value.AsIntegerOrNull();

            transactionDetail.ModifiedDateTime = inputTransactionDetail.Element( "lastUpdateDate" )?.Value.AsDateTime();
            transactionDetail.ModifiedByPersonId = inputTransactionDetail.Element( "lastUpdatedByPerson" ).Attribute( "id" )?.Value.AsIntegerOrNull();

            return transactionDetail;
        }
    }
}
