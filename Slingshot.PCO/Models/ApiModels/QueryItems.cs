using Newtonsoft.Json;
using Slingshot.PCO.Models.ApiModels.Json;
using System.Collections.Generic;

namespace Slingshot.PCO.Models.ApiModels
{
    public class QueryItems
    {
        [JsonProperty( "links" )]
        public Links Links { get; set; }

        [JsonProperty( "data" )]
        [JsonConverter( typeof( SingleOrArrayConverter<DataItem> ) )]
        public List<DataItem> Data { get; set; }

        [JsonProperty( "included" )]
        public List<DataItem> IncludedItems { get; set; }

        [JsonProperty( "meta" )]
        public Meta Meta { get; set; }
    }
}
