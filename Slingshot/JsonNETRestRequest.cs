using RestSharp;

namespace Slingshot
{
    /// <summary>
    // Special JSON.NET based RestSharp RestRequest that that doesn't use ISO8601 for Dates
    /// </summary>
    /// <seealso cref="RestSharp.RestRequest" />
    public class JsonNETRestRequest : RestRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JsonNETRestRequest"/> class.
        /// </summary>
        /// <param name="resource">Resource to use for this request</param>
        /// <param name="method">Method to use for this request</param>
        public JsonNETRestRequest(string resource, Method method): base(resource, method )
        {
            this.JsonSerializer = new JsonNETRestSharpSerializer();
            this.RequestFormat = DataFormat.Json;
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="JsonNETRestRequest"/> class.
        /// </summary>
        public JsonNETRestRequest():base()
        {
            this.JsonSerializer = new JsonNETRestSharpSerializer();
            this.RequestFormat = DataFormat.Json;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonNETRestRequest"/> class.
        /// </summary>
        /// <param name="method">Method to use for this request</param>
        public JsonNETRestRequest( Method method ) : base( method )
        {
            this.JsonSerializer = new JsonNETRestSharpSerializer();
            this.RequestFormat = DataFormat.Json;
        }
    }
}
