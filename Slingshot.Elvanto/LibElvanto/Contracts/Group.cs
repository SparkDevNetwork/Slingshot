using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using LibElvanto.Attributes;

namespace LibElvanto.Contracts;

[ElvantoResource( "https://api.elvanto.com/v1/groups/getAll.json", "groups", "group", new string[] {
    "people",
    "locations",
    "categories"
} )]
public class Group : ElvantoContract
{
    [JsonPropertyName( "name" )]
    public string? Name { get; set; }

    [JsonPropertyName( "description" )]
    public string? Description { get; set; }

    [JsonPropertyName( "status" )]
    public string? Status { get; set; }

    [JsonPropertyName( "meeting_address" )]
    public string? MeetingAddress { get; set; }

    [JsonPropertyName( "meeting_city" )]
    public string? MeetingCity { get; set; }

    [JsonPropertyName( "meeting_state" )]
    public string? MeetingState { get; set; }

    [JsonPropertyName( "meeting_postcode" )]
    public string? MeetingPostcode { get; set; }

    [JsonPropertyName( "meeting_country" )]
    public string? MeetingCountry { get; set; }

    [JsonPropertyName( "meeting_day" )]
    public string? MeetingDay { get; set; }

    [JsonPropertyName( "meeting_time" )]
    public string? MeetingTime { get; set; }

    [JsonPropertyName( "meeting_frequency" )]
    public string? MeetingFrequency { get; set; }

    [JsonPropertyName( "picture" )]
    public string? Picture { get; set; }

    [JsonPropertyName( "id" )]
    public string? Id { get; set; }

    public List<GroupMember> GroupMembers { get; set; } = new List<GroupMember>();

    public IdNameContract? Campus { get; set; }

    public IdNameContract? GroupType { get; set; }

    public override void Process( JsonElement dataElement, List<string>? fields )
    {
        if ( dataElement.TryGetProperty( "locations", out var locations ) )
        {
            if ( locations.ValueKind == JsonValueKind.Object
                && locations.TryGetProperty( "location", out var location )
                && location.ValueKind == JsonValueKind.Array )
            {

                var campus = location.EnumerateArray().FirstOrDefault();
                if ( campus.ValueKind == JsonValueKind.Object )
                {
                    this.Campus = campus.Deserialize<IdNameContract>();
                }
            }
        }

        if ( dataElement.TryGetProperty( "categories", out var categories ) )
        {
            if ( categories.ValueKind == JsonValueKind.Object
                && categories.TryGetProperty( "category", out var category )
                && category.ValueKind == JsonValueKind.Array )
            {

                var categoryItem = category.EnumerateArray().FirstOrDefault();
                if ( categoryItem.ValueKind == JsonValueKind.Object )
                {
                    this.GroupType = categoryItem.Deserialize<IdNameContract>();
                }
            }
        }

        if ( dataElement.TryGetProperty( "people", out var people ) )
        {
            if ( people.ValueKind == JsonValueKind.Object
                && people.TryGetProperty( "person", out var person )
                && person.ValueKind == JsonValueKind.Array )
            {

                var persons = person.EnumerateArray();
                foreach ( var personItem in persons )
                {
                    if ( personItem.ValueKind == JsonValueKind.Object )
                    {
                        var groupMember = personItem.Deserialize<GroupMember>();
                        if ( groupMember != null )
                        {
                            this.GroupMembers.Add( groupMember );
                        }
                    }
                }

            }
        }
    }

}

public class GroupMember
{
    [JsonPropertyName( "firstname" )]
    public string? Firstname { get; set; }

    [JsonPropertyName( "preferred_name" )]
    public string? PreferredName { get; set; }

    [JsonPropertyName( "middle_name" )]
    public string? MiddleName { get; set; }

    [JsonPropertyName( "lastname" )]
    public string? Lastname { get; set; }

    [JsonPropertyName( "email" )]
    public string? Email { get; set; }

    [JsonPropertyName( "mobile" )]
    public string? Mobile { get; set; }

    [JsonPropertyName( "phone" )]
    public string? Phone { get; set; }

    [JsonPropertyName( "picture" )]
    public string? Picture { get; set; }

    [JsonPropertyName( "position" )]
    public string? Position { get; set; }

    [JsonPropertyName( "id" )]
    public string? Id { get; set; }
}


