using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Slingshot.ServantKeeper.Attributes;

namespace Slingshot.ServantKeeper.Models
{
    public class Value
    {
        [ColumnName("tbl_id")]
        public long Id { get; set; }

        [ColumnName("tbl_name")]
        public string Name { get; set; }

        [ColumnName("desc")]
        public string Description { get; set; }
        
        [ColumnName("create_ts")]
        [DateTimeParseString( "yyyyMMddhhmmss" )]
        public DateTime CreatedDate { get; set; }
    }
}
