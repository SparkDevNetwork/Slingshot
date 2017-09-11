using System;
using System.Data;

using Slingshot.Core.Model;

namespace Slingshot.ACS.Utilities.Translators
{
    public static class AcsFinancialAccount
    {
        public static FinancialAccount Translate( DataRow row )
        {
            var financialAccount = new FinancialAccount();

            financialAccount.Id = row.Field<Int16>( "FundNumber" );
            financialAccount.Name = row.Field<string>( "FundDescription" );

            return financialAccount;
        }
    }
}
