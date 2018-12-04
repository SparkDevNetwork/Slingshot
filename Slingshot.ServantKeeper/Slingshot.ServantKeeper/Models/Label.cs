using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Slingshot.ServantKeeper.Attributes;

namespace Slingshot.ServantKeeper.Models
{
    public class Label
    {
        [ColumnName( "label_id" )]
        public int LabelId { get; set; }

        [ColumnName( "desc" )]
        public string Description { get; set; }
    }
}
