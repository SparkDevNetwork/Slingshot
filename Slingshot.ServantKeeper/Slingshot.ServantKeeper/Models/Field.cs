using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Slingshot.ServantKeeper.Attributes;

namespace Slingshot.ServantKeeper.Models
{
    public class Field
    {
        [ColumnName( "lk_key" )]
        public string Key { get; set; }

        [ColumnName( "lbl_id" )]
        public int LabelId { get; set; }

        [ColumnName( "field_name" )]
        public string Name { get; set; }

        [ColumnName( "desc" )]
        public string Description { get; set; }
    }
}
