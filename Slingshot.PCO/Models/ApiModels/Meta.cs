using Newtonsoft.Json;

namespace Slingshot.PCO.Models.ApiModels
{
    public class Meta
    {
        [JsonProperty( "total_count" )]
        public int TotalCount { get; set; }

        [JsonProperty( "count" )]
        public int Count { get; set; }
    }
}
    



