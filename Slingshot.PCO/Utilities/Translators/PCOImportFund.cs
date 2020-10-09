using Slingshot.Core.Model;
using Slingshot.PCO.Models;

namespace Slingshot.PCO.Utilities.Translators
{
    public static class PCOImportFund
    {
        public static FinancialAccount Translate( PCOFund inputAccount )
        {
            var financialAccount = new FinancialAccount
            {
                Id = inputAccount.Id,
                Name = inputAccount.Name,
                IsTaxDeductible = true
            };

            return financialAccount;
        }
    }
}