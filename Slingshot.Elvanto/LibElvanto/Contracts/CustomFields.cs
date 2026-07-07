using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using LibElvanto.Attributes;

namespace LibElvanto.Contracts;

[ElvantoResource( "https://api.elvanto.com/v1/people/customFields/getAll.json", "custom_fields", "custom_field" )]
public class CustomFields : ElvantoContract
{
    [JsonPropertyName( "id" )]
    public string? Id { get; set; }

    [JsonPropertyName( "name" )]
    public string? Name { get; set; }

    [JsonPropertyName( "type" )]
    public string? Type { get; set; }

    [JsonPropertyName( "values" )]
    public dynamic? Values { get; set; }
}

public class Value
{
    [JsonPropertyName( "id" )]
    public string? Id { get; set; }

    [JsonPropertyName( "name" )]
    public string? Name { get; set; }
}

public class Values
{
    [JsonPropertyName( "value" )]
    public List<Value>? Value { get; set; }
}

