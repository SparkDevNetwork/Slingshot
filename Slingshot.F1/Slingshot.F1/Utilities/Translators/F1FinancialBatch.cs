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
    public static class F1FinancialBatch
    {
        public static FinancialBatch Translate( XElement inputBatch )
        {
            var financialBatch = new FinancialBatch();
            financialBatch.Id = inputBatch.Attribute( "id" ).Value.AsInteger();
            financialBatch.StartDate = inputBatch.Element( "batchDate" )?.Value.AsDateTime();
            financialBatch.Name = inputBatch.Element( "name" )?.Value;

            switch ( inputBatch.Element( "batchStatus" ).Element( "name" ).Value )
            {
                case "Saved":
                    financialBatch.Status = BatchStatus.Closed;
                    break;
                default:
                    financialBatch.Status = BatchStatus.Open;
                    break;
            }

            financialBatch.CreatedDateTime = inputBatch.Element( "createdDate" )?.Value.AsDateTime();
            financialBatch.ModifiedDateTime = inputBatch.Element( "lastUpdatedDate" )?.Value.AsDateTime();

            financialBatch.CreatedByPersonId = inputBatch.Element( "createdByPerson" )?.Attribute( "id" )?.Value.AsIntegerOrNull();
            financialBatch.ModifiedByPersonId = inputBatch.Element( "lastUpdatedByPerson" )?.Attribute( "id" )?.Value.AsIntegerOrNull();

            return financialBatch;
        }
    }
}
