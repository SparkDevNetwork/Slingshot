using Slingshot.Core.Model;
using Slingshot.ServantKeeper.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Slingshot.ServantKeeper.Utilities.Translators
{
    class SKBatch
    {

        public static FinancialBatch Translate( Batch batch )
        {
            // The ID for a batch is a combination of the ID and the date
            long id = batch.Id + batch.Date.Value.Ticks;

            FinancialBatch financialBatch = new FinancialBatch();
            financialBatch.Id = Math.Abs( unchecked(( int ) id) );

            financialBatch.Name = batch.Name + ": " + batch.Notes;
            financialBatch.CreatedDateTime = batch.CreatedDate;
            financialBatch.StartDate = batch.Date;
            financialBatch.Status = BatchStatus.Closed;

            return financialBatch;

        }
    }
}
