using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Slingshot.ServantKeeper.Attributes;

namespace Slingshot.ServantKeeper.Models
{
    public class AccountLink
    {
        [ColumnName("acct_id")]
        public long Id { get; set; }

        [ColumnName("qk_acct")]
        public string Description { get; set; }
    }
}
