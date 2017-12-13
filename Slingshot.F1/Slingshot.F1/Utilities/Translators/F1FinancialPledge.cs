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
    public static class F1FinancialPledge
    {
        public static FinancialPledge Translate( XElement inputPledge )
        {
            var pledge = new FinancialPledge();

            pledge.Id = inputPledge.Attribute( "id" ).Value.AsInteger();
            pledge.AccountId = inputPledge.Element( "fund" ).Attribute( "id" ).Value.AsInteger();

            pledge.StartDate = inputPledge.Element( "startDate" )?.Value.AsDateTime();
            pledge.EndDate = inputPledge.Element( "endDate" )?.Value.AsDateTime();

            pledge.CreatedDateTime = inputPledge.Element( "createdDate" )?.Value.AsDateTime();
            pledge.ModifiedDateTime = inputPledge.Element( "lastUpdatedDate" )?.Value.AsDateTime();

            // F1 doesn't store a pledge frequency
            pledge.PledgeFrequency = PledgeFrequency.OneTime; // not sure if this is the best default

            var goal = inputPledge.Element( "goal" ).Value.AsDecimalOrNull();
            if ( goal.HasValue && goal.Value > 0 )
            {
                pledge.TotalAmount = inputPledge.Element( "goal" ).Value.AsDecimal();
            }

            return pledge;
        }
    }
}
