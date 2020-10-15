using Newtonsoft.Json;

namespace Slingshot.PCO.Models.ApiModels
{
    public class Links
    {
        [JsonProperty( "self" )]
        public string Self { get; set; }

        [JsonProperty( "prev" )]
        public string Previous { get; set; }

        [JsonProperty( "next" )]
        public string Next { get; set; }
    }
}
