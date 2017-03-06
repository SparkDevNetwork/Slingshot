using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Slingshot.Core;
using Slingshot.Core.Model;

namespace Slingshot.CCB.Utilities.Translators
{
    public static class CcbFinancialBatch
    {
        public static FinancialBatch Translate(XElement inputBatch )
        {
            var financialBatch = new FinancialBatch();
            financialBatch.Id = inputBatch.Attribute( "id" ).Value.AsInteger();
            financialBatch.StartDate = inputBatch.Element( "begin_date" )?.Value.AsDateTime();
            financialBatch.EndDate = inputBatch.Element( "end_date" )?.Value.AsDateTime();
            financialBatch.Name = inputBatch.Element( "source" )?.Value + " " + financialBatch.StartDate?.ToShortDateString();
            financialBatch.CampusId = inputBatch.Element( "campus" )?.Attribute( "id" ).Value.AsIntegerOrNull();

            switch ( inputBatch.Element( "status" )?.Value )
            {
                case "Closed":
                    financialBatch.Status = BatchStatus.Closed;
                    break;
                default:
                    financialBatch.Status = BatchStatus.Open;
                    break;
            }
            
            financialBatch.CreatedDateTime = inputBatch.Element( "created" )?.Value.AsDateTime();
            financialBatch.ModifiedDateTime = inputBatch.Element( "modified" )?.Value.AsDateTime();

            financialBatch.CreatedByPersonId = inputBatch.Element( "creator" )?.Attribute( "id" )?.Value.AsIntegerOrNull();
            financialBatch.ModifiedByPersonId = inputBatch.Element( "modifier" )?.Attribute( "id" )?.Value.AsIntegerOrNull();

            return financialBatch;
        }
    }
}
