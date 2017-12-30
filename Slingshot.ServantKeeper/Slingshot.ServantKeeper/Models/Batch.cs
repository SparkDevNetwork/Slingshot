using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Slingshot.ServantKeeper.Attributes;

namespace Slingshot.ServantKeeper.Models
{
    public class Batch
    {

        [ColumnName( "batch_id" )]
        public long Id { get; set; }

        [ColumnName( "batch_dt" )]
        [DateTimeParseString( "yyyyMMdd" )]
        public DateTime? Date { get; set; }

        [ColumnName( "batch_name" )]
        public String Name { get; set; }

        [ColumnName( "status" )]
        public String Status { get; set; }

        [ColumnName( "notes" )]
        public String Notes { get; set; }

        [ColumnName( "create_ts" )]
        [DateTimeParseString( "yyyyMMddhhmmss" )]
        public DateTime? CreatedDate { get; set; }

        [ColumnName( "update_ts" )]
        [DateTimeParseString( "yyyyMMddhhmmss" )]
        public DateTime? UpdatedDate { get; set; }
    }
}
