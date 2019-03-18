using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Slingshot.Core.Model;

namespace Slingshot.ElexioCommunity.Utilities.Translators
{
    public static class ElexioCommunityFinancialAccount
    {
        public static FinancialAccount Translate( dynamic importFinancialAccount )
        {
            var account = new FinancialAccount();

            account.Id = importFinancialAccount.id;
            account.Name = importFinancialAccount.name;
            account.IsTaxDeductible = importFinancialAccount.taxDeductible;

            return account;
        }
    }
}
