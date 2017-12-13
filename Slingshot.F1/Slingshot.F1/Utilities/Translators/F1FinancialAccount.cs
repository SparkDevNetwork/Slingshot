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
    public static class F1FinancialAccount
    {
        public static FinancialAccount Translate( XElement inputAccount )
        {
            var financialAccount = new FinancialAccount();

            financialAccount.Id = inputAccount.Attribute( "id" ).Value.AsInteger();
            financialAccount.Name = inputAccount.Element( "name" ).Value;

            var parentAccount = inputAccount.Element( "parentFund" );

            if ( parentAccount != null )
            {
                financialAccount.ParentAccountId = inputAccount.Element( "parentFund" ).Attribute( "id" ).Value.AsInteger();
            }

            // information isn't available so the default will be false.
            financialAccount.IsTaxDeductible = false;

            return financialAccount;
        }
    }
}
