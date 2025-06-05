using System.Text.Json.Serialization;

namespace LibElvanto.Contracts;

public class IdNameContract
{
    [JsonPropertyName( "id" )]
    public string? Id { get; set; }

    [JsonPropertyName( "name" )]
    public string? Name { get; set; }
}
