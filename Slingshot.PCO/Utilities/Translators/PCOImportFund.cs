using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Slingshot.Core;
using Slingshot.Core.Model;
using Slingshot.PCO.Models;

namespace Slingshot.PCO.Utilities.Translators
{
    public static class PCOImportFund
    {
        public static FinancialAccount Translate( PCOFund inputAccount )
        {
            var financialAccount = new FinancialAccount();

            financialAccount.Id = inputAccount.id;

            financialAccount.Name = inputAccount.name;

            financialAccount.IsTaxDeductible = true;

            return financialAccount;
        }
    }
}