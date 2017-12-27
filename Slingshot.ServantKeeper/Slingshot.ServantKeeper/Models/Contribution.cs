using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Slingshot.ServantKeeper.Attributes;

namespace Slingshot.ServantKeeper.Models
{
    public class Contribution
    {


        [ColumnName("cont_id")]
        public long Id { get; set; }

        [ColumnName("ind_id")]
        public long IndividualId { get; set; }

        [ColumnName("batch_id")]
        public long BatchId { get; set; }

        [ColumnName("batch_dt")]
        [DateTimeParseString("yyyyMMdd")]
        public DateTime? BatchDate { get; set; }

        [ColumnName("cont_dt")]
        [DateTimeParseString("yyyyMMdd")]
        public DateTime? ContributionDate { get; set; }

        [ColumnName("post_dt")]
        [DateTimeParseString("yyyyMMdd")]
        public DateTime? PostDate { get; set; }

        [ColumnName("acct_id")]
        public long AccountId { get; set; }
        
        [ColumnName("amt")]
        public decimal Amount { get; set; }

        [ColumnName("enc_amt")]
        public string EncodedAmount { get; set; }

        [ColumnName("pay_type")]
        public string PaymentType { get; set; }

        [ColumnName("card_no")]
        public string CardNumber { get; set; }

        [ColumnName("exp_dt")]
        public string ExpDate { get; set; }

        [ColumnName("check_no")]
        public string CheckNumber { get; set; }

        [ColumnName("note")]
        public string Note { get; set; }

        [ColumnName("create_ts")]
        [DateTimeParseString("yyyyMMddhhmmss")]
        public DateTime? CreatedDate { get; set; }

        [ColumnName("update_ts")]
        [DateTimeParseString("yyyyMMddhhmmss")]
        public DateTime? UpdatedDate { get; set; }
    }
}
