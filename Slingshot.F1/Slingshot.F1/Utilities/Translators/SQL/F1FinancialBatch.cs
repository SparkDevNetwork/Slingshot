using Slingshot.Core.Model;
using Slingshot.F1.Utilities.SQL.DTO;

namespace Slingshot.F1.Utilities.Translators.SQL
{
    public static class F1FinancialBatch
    {
        public static FinancialBatch Translate( BatchDTO batch )
        {
            var slingshotBatch = new FinancialBatch();

            slingshotBatch.Id = batch.BatchId;
            slingshotBatch.StartDate = batch.BatchDate;
            slingshotBatch.Name = batch.BatchName;

            //There's no status in the MDF, so just marked all imported batches as closed
            slingshotBatch.Status = BatchStatus.Closed;

            return slingshotBatch;
        }
    }
}
