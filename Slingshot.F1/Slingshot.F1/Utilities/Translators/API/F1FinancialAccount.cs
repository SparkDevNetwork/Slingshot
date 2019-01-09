using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Slingshot.Core;
using Slingshot.Core.Model;

namespace Slingshot.F1.Utilities.Translators.API
{
    public static class F1FinancialAccount
    {
        public static FinancialAccount Translate( XElement inputAccount, bool? isTaxDeductible = null )
        {
            var financialAccount = new FinancialAccount();

            financialAccount.Id = inputAccount.Attribute( "id" ).Value.AsInteger();
            financialAccount.Name = inputAccount.Element( "name" ).Value;

            var parentAccount = inputAccount.Element( "parentFund" );

            if ( parentAccount != null )
            {
                financialAccount.ParentAccountId = inputAccount.Element( "parentFund" ).Attribute( "id" ).Value.AsInteger();
            }

            var fundType = inputAccount.Element( "fundType" );

            if ( fundType != null )
            {
                // If account type is "Contribution", account is tax deductible
                if ( fundType.Element( "name" ).Value == "Contribution" )
                {
                    financialAccount.IsTaxDeductible = true;
                }
                else
                {
                    financialAccount.IsTaxDeductible = false;
                }
            }
            else if ( isTaxDeductible != null )
            {
                financialAccount.IsTaxDeductible = isTaxDeductible.Value;
            }
            else
            {
                financialAccount.IsTaxDeductible = true;
            }

            return financialAccount;
        }
    }
}