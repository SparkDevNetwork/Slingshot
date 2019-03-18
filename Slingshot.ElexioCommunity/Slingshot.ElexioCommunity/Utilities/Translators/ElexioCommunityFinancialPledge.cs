using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Slingshot.Core.Model;

namespace Slingshot.ElexioCommunity.Utilities.Translators
{
    public static class ElexioCommunityFinancialPledge
    {
        public static FinancialPledge Translate( dynamic importFinancialPledge )
        {
            var pledge = new FinancialPledge();

            pledge.Id = importFinancialPledge.pledgeId;
            pledge.PersonId = importFinancialPledge.uid;
            pledge.AccountId = importFinancialPledge.categoryId;
            pledge.StartDate = importFinancialPledge.startDate;
            pledge.EndDate = importFinancialPledge.endDate;
            pledge.TotalAmount = importFinancialPledge.totalAmount;

            string pledgeFreq = importFinancialPledge.frequency;
            if ( pledgeFreq == "yearly" )
            {
                pledge.PledgeFrequency = PledgeFrequency.Yearly;
            }
            else if ( pledgeFreq == "quarterly" )
            {
                pledge.PledgeFrequency = PledgeFrequency.Quarterly;
            }
            else if ( pledgeFreq == "monthly" )
            {
                pledge.PledgeFrequency = PledgeFrequency.Monthly;
            }
            else if ( pledgeFreq == "weekly" )
            {
                pledge.PledgeFrequency = PledgeFrequency.Weekly;
            }
            else
            {
                pledge.PledgeFrequency = PledgeFrequency.OneTime;
            }
            

            return pledge;
        }
    }
}
