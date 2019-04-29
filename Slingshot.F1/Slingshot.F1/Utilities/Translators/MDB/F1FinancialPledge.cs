using System;
using System.Collections.Generic;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using Slingshot.Core.Model;

namespace Slingshot.F1.Utilities.Translators.MDB
{
    public static class F1FinancialPledge
    {
        public static FinancialPledge Translate( DataRow row, Dictionary<int, HeadOfHousehold> headOfHouseHolds )
        {
            var pledge = new FinancialPledge();

            pledge.Id = row.Field<int>( "Pledge_id" );
            var householdId = row.Field<int?>( "household_id" );
            var individualId = row.Field<int?>( "individual_id" );

            if ( individualId.HasValue )
            {
                pledge.PersonId = individualId.Value;
            }
            else if ( householdId.HasValue && headOfHouseHolds.TryGetValue( householdId.Value, out var headOfHousehold ) )
            {
                pledge.PersonId = headOfHousehold?.IndividualId ?? 0;
            }

            pledge.TotalAmount = row.Field<decimal>( "total_pledge" );
            pledge.StartDate = row.Field<DateTime?>( "start_date" );
            pledge.EndDate = row.Field<DateTime?>( "end_date" );

            switch ( row.Field<string>( "pledge_frequency_name" ) )
            {
                case "One Time":
                    pledge.PledgeFrequency = PledgeFrequency.OneTime;
                    break;

                case "Monthly":
                    pledge.PledgeFrequency = PledgeFrequency.Monthly;
                    break;

                case "Yearly":
                    pledge.PledgeFrequency = PledgeFrequency.Yearly;
                    break;

                case "Quarterly":
                    pledge.PledgeFrequency = PledgeFrequency.Quarterly;
                    break;
                case "Twice a Month":
                    pledge.PledgeFrequency = PledgeFrequency.BiWeekly;
                    break;
                case "Weekly":
                    pledge.PledgeFrequency = PledgeFrequency.Weekly;
                    break;
                default:
                    pledge.PledgeFrequency = PledgeFrequency.OneTime;
                    break;
            }

            //Set Account Id
            if ( string.IsNullOrWhiteSpace( row.Field<string>( "sub_fund_name" ) ) )
            {
                //Use Hash to create Account ID
                MD5 md5Hasher = MD5.Create();
                var hashed = md5Hasher.ComputeHash( Encoding.UTF8.GetBytes( row.Field<string>( "fund_name" ) ) );
                var accountId = Math.Abs( BitConverter.ToInt32( hashed, 0 ) ); // used abs to ensure positive number
                if ( accountId > 0 )
                {
                    pledge.AccountId = accountId;
                }
            }
            else
            {
                //Use Hash to create Account ID
                MD5 md5Hasher = MD5.Create();
                var hashed = md5Hasher.ComputeHash( Encoding.UTF8.GetBytes( row.Field<string>( "fund_name" ) + row.Field<string>( "sub_fund_name" ) ) );
                var accountId = Math.Abs( BitConverter.ToInt32( hashed, 0 ) ); // used abs to ensure positive number
                if ( accountId > 0 )
                {
                    pledge.AccountId = accountId;
                }
            }

            return pledge;
        }
    }
}
