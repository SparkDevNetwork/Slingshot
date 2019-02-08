using System;
using System.Data;

using Slingshot.Core.Model;

namespace Slingshot.ServantKeeper.Utilities.Translators
{
    public static class SkFinancialBatch
    {
        public static FinancialBatch Translate(DataRow row)
        {
            FinancialBatch batch = new FinancialBatch();

            batch.Id = row.Field<int>("BATCH_ID");
            batch.Name = row.Field<string>("BATCH_NAME");
            batch.StartDate = row.Field<DateTime?>("BATCH_DT");
            batch.Status = BatchStatus.Closed;
            batch.CreatedDateTime = row.Field<DateTime?>("CREATE_TS");
            batch.ModifiedDateTime = row.Field<DateTime?>("UPDATE_TS");

            return batch;
        }
    }
}
