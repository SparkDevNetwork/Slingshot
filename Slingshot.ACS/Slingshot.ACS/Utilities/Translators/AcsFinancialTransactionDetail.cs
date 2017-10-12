using System;
using System.Data;

using Slingshot.Core.Model;

namespace Slingshot.ACS.Utilities.Translators
{
    public static class AcsFinancialTransactionDetail
    {
        public static FinancialTransactionDetail Translate( DataRow row )
        {
            var financialTransactionDetail = new FinancialTransactionDetail();

            financialTransactionDetail.AccountId = row.Field<Int16>( "FundNumber" );
            financialTransactionDetail.Amount = row.Field<decimal>( "Amount" );
            financialTransactionDetail.TransactionId = row.Field<int>( "TransactionID" );
            financialTransactionDetail.Summary = row.Field<string>( "GiftDescription" );

            financialTransactionDetail.CreatedDateTime = row.Field<DateTime?>( "GiftDate" );

            return financialTransactionDetail;
        }
    }
}
