using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Slingshot.ServantKeeper.Attributes;

namespace Slingshot.ServantKeeper.Models
{
    public class ContributionDetail
    {


        [ColumnName("tran_id")]
        public long Id { get; set; }

        [ColumnName("cont_id")]
        public long ContributionId { get; set; }

        [ColumnName("ind_id")]
        public long IndividualId { get; set; }

        [ColumnName("batch_id")]
        public long BatchId { get; set; }

        [ColumnName("batch_dt")]
        [DateTimeParseString("yyyyMMdd")]
        public DateTime? BatchDate { get; set; }

        [ColumnName("status")]
        public string Status { get; set; }

        [ColumnName("tax_ind")]
        public bool IsTaxDeducatible { get; set; }

        [ColumnName("cont_dt")]
        [DateTimeParseString("yyyyMMdd")]
        public DateTime? ContributionDate { get; set; }
        
        [ColumnName("acct_id")]
        public long AccountId { get; set; }
        
        [ColumnName("amt")]
        public decimal Amount { get; set; }

        [ColumnName("enc_amt")]
        public string EncodedAmount { get; set; }
        
        [ColumnName("create_ts")]
        [DateTimeParseString("yyyyMMddhhmmss")]
        public DateTime? CreatedDate { get; set; }

        [ColumnName("update_ts")]
        [DateTimeParseString("yyyyMMddhhmmss")]
        public DateTime? UpdatedDate { get; set; }
    }
}
