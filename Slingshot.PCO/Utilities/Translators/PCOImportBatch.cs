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
    public static class PCOImportBatch
    {
        public static FinancialBatch Translate( PCOBatch inputBatch )
        {
            var financialBatch = new Core.Model.FinancialBatch();
            financialBatch.Id = inputBatch.id;
            financialBatch.StartDate = inputBatch.created_at;
            financialBatch.Name = inputBatch.description;

            if( inputBatch.committed_at.HasValue )
            {
                financialBatch.Status = BatchStatus.Closed;
            }
            else
            {
                financialBatch.Status = BatchStatus.Open;
            }

            financialBatch.CreatedDateTime = inputBatch.created_at;
            financialBatch.ModifiedDateTime = inputBatch.updated_at;
            financialBatch.CreatedByPersonId = inputBatch.ownerId;

            return financialBatch;
        }
    }
}
