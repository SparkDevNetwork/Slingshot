using System.IO;
using Newtonsoft.Json;
using RestSharp.Serializers;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace Slingshot
{
    /// <summary>
    /// Special JSON.NET based RestSharp Serializer that doesn't use ISO8601 for Dates
    /// derived from https://github.com/restsharp/RestSharp/blob/86b31f9adf049d7fb821de8279154f41a17b36f7/RestSharp/Serializers/JsonSerializer.cs
    /// </summary>
    /// <seealso cref="RestSharp.Serializers.ISerializer" />
    public class JsonNETRestSharpSerializer : ISerializer
    {
        private readonly JsonSerializer _serializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonNETRestSharpSerializer"/> class.
        /// </summary>
        public JsonNETRestSharpSerializer()
        {
            ContentType = "application/json";
            _serializer = new JsonSerializer
            {
                MissingMemberHandling = MissingMemberHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Include,
                DateTimeZoneHandling = DateTimeZoneHandling.Unspecified
            };
        }

        /// <summary>
        /// Default serializer with overload for allowing custom Json.NET settings
        /// </summary>
        public JsonNETRestSharpSerializer( JsonSerializer serializer )
        {
            ContentType = "application/json";
            _serializer = serializer;
        }

        /// <summary>
        /// Serializes the specified object.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns></returns>
        public string Serialize( object obj )
        {
            using ( var stringWriter = new StringWriter() )
            {
                using ( var jsonTextWriter = new JsonTextWriter( stringWriter ) )
                {
                    jsonTextWriter.Formatting = Formatting.None;
                    jsonTextWriter.QuoteChar = '"';
                    _serializer.Serialize( jsonTextWriter, obj );
                    var result = stringWriter.ToString();
                    return result;
                }
            }
        }

        /// <summary>
        /// Unused for JSON Serialization
        /// </summary>
        public string DateFormat { get; set; }

        /// <summary>
        /// Unused for JSON Serialization
        /// </summary>
        public string RootElement { get; set; }

        /// <summary>
        /// Unused for JSON Serialization
        /// </summary>
        public string Namespace { get; set; }

        /// <summary>
        /// Content type for serialized content
        /// </summary>
        public string ContentType
        {
            get; set;
        }
    }
}
