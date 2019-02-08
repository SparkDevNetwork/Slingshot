using System;
using System.Data;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;

using Slingshot.Core.Model;

namespace Slingshot.ServantKeeper.Utilities.Translators
{
    public static class SkFinancialContributionDetail
    {
        public static FinancialTransactionDetail Translate( DataRow row )
        {
            FinancialTransactionDetail detail = new FinancialTransactionDetail();

            // Identifiers
            detail.Id = row.Field<int>("DETAIL_ID");
            detail.TransactionId = row.Field<int>("CONTRIBUTION_ID");
            detail.AccountId = row.Field<int>("ACCOUNT_ID");

            // Other Fields
            detail.Amount = (decimal)row.Field<double>("AMT");
            detail.Summary = row.Field<string>( "NOTE" );
            detail.CreatedDateTime = row.Field<DateTime?>("CREATE_TS");
            detail.ModifiedDateTime = row.Field<DateTime?>("UPDATE_TS");

            return detail;
        }
    }
}
