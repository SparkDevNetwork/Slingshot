using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibElvanto.Attributes
{
    public class ElvantoResourceAttribute : Attribute
    {
        public string Url { get => url; }
        private readonly string url;

        public string PluralName { get => pluralName; }
        private readonly string pluralName;

        public string SingleName { get => singleName; }
        private readonly string singleName;

        public List<string> Fields { get => fields?.ToList() ?? new List<string>(); }
        private readonly string[]? fields;

        public ElvantoResourceAttribute( string url, string pluralName, string singleName, string[]? fields = null )
        {
            this.url = url;
            this.pluralName = pluralName;
            this.singleName = singleName;
            this.fields = fields;
        }
    }
}
