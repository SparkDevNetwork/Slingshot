using System;
using System.Collections.Generic;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Linq;

using Slingshot.Core;
using Slingshot.Core.Model;

namespace Slingshot.F1.Utilities.Translators.MDB
{
    public static class F1FinancialBatch
    {
        public static FinancialBatch Translate( DataRow row )
        {
            var batch = new FinancialBatch();

            batch.Id = row.Field<int>( "BatchID" );
            batch.StartDate = row.Field<DateTime?>( "BatchDate" );
            batch.Name = row.Field<string>( "BatchName" );

            //There's no status in the MDB, so just marked all imported batches as closed
            batch.Status = BatchStatus.Closed;

            return batch;
        }
    }
}
