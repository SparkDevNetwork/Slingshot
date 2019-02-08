using System;
using System.Data;

using Slingshot.Core.Model;

namespace Slingshot.ServantKeeper.Utilities.Translators
{
    public static class SkFinancialAccount
    { 
        public static FinancialAccount Translate( DataRow row )
        {
            FinancialAccount account = new FinancialAccount();

            account.Id = row.Field<int>("ACCOUNT_ID");
            account.Name = row.Field<string>("ACCT_NAME");
            account.IsTaxDeductible = row.Field<string>("TAX_IND") == "1" ? true : false;

            return account;
        }
    }
}
