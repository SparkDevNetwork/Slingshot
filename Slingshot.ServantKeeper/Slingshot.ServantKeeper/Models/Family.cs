using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Slingshot.ServantKeeper.Attributes;

namespace Slingshot.ServantKeeper.Models
{
    public class Family
    {
        [ColumnName( "family_id" )]
        public long Id { get; set; }

        [ColumnName( "fam_name" )]
        public string FamilyName { get; set; }

        [ColumnName( "create_ts" )]
        [DateTimeParseString( "yyyyMMddhhmmss" )]
        public DateTime CreatedDate { get; set; }

        [ColumnName( "addr1" )]
        public string Address1 { get; set; }

        [ColumnName( "addr2" )]
        public string Address2 { get; set; }

        [ColumnName( "city" )]
        public string City { get; set; }

        [ColumnName( "state" )]
        public string State { get; set; }

        [ColumnName( "zip" )]
        public string Zip { get; set; }

        [ColumnName( "create_ts" )]
        [DateTimeParseString( "yyyyMMddhhmmss" )]
        public DateTime? CreateDate { get; set; }

        [ColumnName( "update_ts" )]
        [DateTimeParseString( "yyyyMMddhhmmss" )]
        public DateTime? UpdateDate { get; set; }
    }
}
