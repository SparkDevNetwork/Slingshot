using Slingshot.Core.Model;
using Slingshot.ServantKeeper.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Slingshot.ServantKeeper.Utilities.Translators
{
    class SKFinancialAccount
    {
        public static FinancialAccount Translate(Account account, List<AccountLink> links)
        {
            FinancialAccount financialAccount = new FinancialAccount();
            financialAccount.Id = Math.Abs(unchecked((int)account.Id));

            financialAccount.Name = account.Name + ": " + links.Where(l => l.Id == account.Id).Select(l => l.Description).FirstOrDefault();
            financialAccount.IsTaxDeductible = account.IsTaxDeductible;

            return financialAccount;

        }
    }
}
