using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Slingshot.ServantKeeper.Attributes;

namespace Slingshot.ServantKeeper.Models
{
    public class Account
    {
        [ColumnName("acct_id")]
        public long Id { get; set; }

        [ColumnName("acct_cd")]
        public string Code { get; set; }

        [ColumnName("acct_name")]
        public string Name { get; set; }

        [ColumnName("active_ind")]
        public bool IsActive { get; set; }
        
        [ColumnName("tax_ind")]
        public bool IsTaxDeductible { get; set; }

        [ColumnName("linkto")]
        public string LinkTo { get; set; }

        [ColumnName("linkfrom")]
        public string LinkFrom { get; set; }

        [ColumnName("eff_dt")]
        [DateTimeParseString("yyyyMMdd")]
        public DateTime? EffectiveDate { get; set; }

        [ColumnName("end_dt")]
        [DateTimeParseString("yyyyMMdd")]
        public DateTime? EndDate { get; set; }

        [ColumnName("create_ts")]
        [DateTimeParseString( "yyyyMMddhhmmss" )]
        public DateTime? CreatedDate { get; set; }

        [ColumnName("update_ts")]
        [DateTimeParseString("yyyyMMddhhmmss")]
        public DateTime? UpdatedDate { get; set; }
    }
}
