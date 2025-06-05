using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using LibElvanto.Attributes;

namespace LibElvanto.Contracts;

[ElvantoResource( "https://api.elvanto.com/v1/people/getAll.json", "people", "person", new string[] {
"gender",
"birthday",
"anniversary",
"school_grade",
"marital_status",
"home_address",
"home_address2",
"home_city",
"home_state",
"home_postcode",
"home_country",
"locations",
"family"
} )]
public class Person : ElvantoContract
{
    [JsonPropertyName( "date_added" )]
    public string? DateAdded { get; set; }

    public string DateAddedFormatted { get => FormatDate( DateAdded ); }

    [JsonPropertyName( "date_modified" )]
    public string? DateModified { get; set; }
    public string DateModifiedFormatted { get => FormatDate( DateModified ); }


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

    [JsonPropertyName( "phone" )]
    public string? Phone { get; set; }

    [JsonPropertyName( "mobile" )]
    public string? Mobile { get; set; }

    [JsonPropertyName( "status" )]
    public string? Status { get; set; }

    [JsonPropertyName( "username" )]
    public string? Username { get; set; }

    [JsonPropertyName( "last_login" )]
    public string? LastLogin { get; set; }

    [JsonPropertyName( "timezone" )]
    public string? Timezone { get; set; }

    [JsonPropertyName( "picture" )]
    public string? Picture { get; set; }

    [JsonPropertyName( "family_relationship" )]
    public string? FamilyRelationship { get; set; }

    [JsonPropertyName( "id" )]
    public string? Id { get; set; }

    [JsonPropertyName( "category_id" )]
    public string? CategoryId { get; set; }

    [JsonPropertyName( "admin" )]
    public int? Admin { get; set; }

    [JsonPropertyName( "contact" )]
    public int? Contact { get; set; }

    [JsonPropertyName( "archived" )]
    public int? Archived { get; set; }

    [JsonPropertyName( "deceased" )]
    public int? Deceased { get; set; }

    [JsonPropertyName( "volunteer" )]
    public int? Volunteer { get; set; }

    [JsonPropertyName( "family_id" )]
    public string? FamilyId { get; set; }

    [JsonPropertyName( "gender" )]
    public string? Gender { get; set; }

    [JsonPropertyName( "home_address" )]
    public string? Address { get; set; }

    [JsonPropertyName( "home_address2" )]
    public string? Address2 { get; set; }

    [JsonPropertyName( "home_city" )]
    public string? City { get; set; }

    [JsonPropertyName( "home_state" )]
    public string State { get; set; }

    [JsonPropertyName( "home_postcode" )]
    public string? PostCode { get; set; }

    [JsonPropertyName( "home_country" )]
    public string? Country { get; set; }

    public string? Grade { get; set; }

    [JsonPropertyName( "marital_status" )]
    public string? MaritalStatus { get; set; }

    [JsonPropertyName( "birthday" )]
    public string? Birthday { get; set; }

    public string BirthdayFormatted { get => FormatDate( Birthday ); }

    public IdNameContract? Campus { get; set; }

    public override void Process( JsonElement dataElement, List<string>? fields )
    {

        var customFields = fields?.Where( f => f.StartsWith( "custom_" ) ).ToList() ?? new List<string>();

        foreach ( var field in customFields )
        {
            dataElement.TryGetProperty( field, out var value );
            if ( value.ValueKind == JsonValueKind.String )
            {
                this.AttributeValues[field.Replace( "custom_", "" )] = value.ToString();
            }
            else if ( value.TryGetProperty( "name", out var valueValue ) )
            {
                this.AttributeValues[field.Replace( "custom_", "" )] = valueValue.ToString();
            }
        }

        if ( dataElement.TryGetProperty( "school_grade", out var schoolGrade ) )
        {
            if ( schoolGrade.ValueKind != JsonValueKind.String
                && schoolGrade.TryGetProperty( "name", out var gradeName ) )
            {
                this.Grade = gradeName.ToString();
            }
        }

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
    }

    public string GetRecordStatus()
    {
        if (this.Archived == 1 )
        {
            return "Inactive";
        }
        return "Active";
    }

    private string FormatDate( string? date )
    {
        if ( date == null )
        {
            return string.Empty;
        }

        try
        {
            return DateTime.Parse( date ).ToString( "M/d/yyyy" );
        }
        catch
        {
            return string.Empty;
        }
    }
}
