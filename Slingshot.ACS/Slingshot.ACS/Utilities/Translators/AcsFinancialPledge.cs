using System;
using System.Data;

using Slingshot.Core.Model;


namespace Slingshot.ACS.Utilities.Translators
{
    public static class AcsFinancialPledge
    {
        public static FinancialPledge Translate( DataRow row )
        {
            var financialPledge = new FinancialPledge();

            financialPledge.Id = row.Field<int>( "PledgeId" );
            financialPledge.PersonId = row.Field<int>( "IndividualId" );
            financialPledge.AccountId = row.Field<Int16>( "FundNumber" );
            financialPledge.StartDate = row.Field<DateTime?>( "StartDate" );
            financialPledge.EndDate = row.Field<DateTime?>( "StopDate" );
            financialPledge.TotalAmount = row.Field<decimal>( "TotalPled" );

            var frequency = row.Field<string>( "Freq" );
            switch ( frequency )
            {
                case "Bi-Weekly":
                    financialPledge.PledgeFrequency = PledgeFrequency.BiWeekly;
                    break;
                case "Monthly":
                    financialPledge.PledgeFrequency = PledgeFrequency.Monthly;
                    break;
                case "One Time":
                    financialPledge.PledgeFrequency = PledgeFrequency.OneTime;
                    break;
                case "Semi-Monthly":
                    financialPledge.PledgeFrequency = PledgeFrequency.TwiceAMonth;
                    break;
                case "Weekly":
                    financialPledge.PledgeFrequency = PledgeFrequency.Weekly;
                    break;
                case "Yearly":
                    financialPledge.PledgeFrequency = PledgeFrequency.Yearly;
                    break;
                default:
                    financialPledge.PledgeFrequency = PledgeFrequency.OneTime; // not sure if this is the best option
                    break;
            }

            return financialPledge;
        }
    }
}
