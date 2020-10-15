using Newtonsoft.Json;

namespace Slingshot.PCO.Models.ApiModels
{
    public class DataItem
    {
        [JsonProperty( "type" )]
        public string Type { get; set; }

        [JsonProperty( "id" )]
        public int Id { get; set; }

        [JsonProperty( "attributes" )]
        public dynamic Item { get; set; }

        [JsonProperty( "relationships" )]
        public Relationships Relationships { get; set; }
    }
}
