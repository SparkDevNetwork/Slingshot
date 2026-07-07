using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using LibElvanto.Attributes;

namespace LibElvanto.Contracts;

[ElvantoResource( "https://api.elvanto.com/v1/people/categories/getAll.json", "categories", "category" )]
public class Category : ElvantoContract
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}