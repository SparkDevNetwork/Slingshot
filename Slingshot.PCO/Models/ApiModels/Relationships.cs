using Newtonsoft.Json;

namespace Slingshot.PCO.Models.ApiModels
{
    public class Relationships
    {
        [JsonProperty( "tags" )]
        public QueryItems Tags { get; set; }

        [JsonProperty( "tag_group" )]
        public QueryItems TagGroup { get; set; }

        [JsonProperty( "emails" )]
        public QueryItems Emails { get; set; }

        [JsonProperty( "addresses" )]
        public QueryItems Addresses { get; set; }

        [JsonProperty( "phone_numbers" )]
        public QueryItems PhoneNumbers { get; set; }

        [JsonProperty( "field_options" )]
        public QueryItems FieldOptions { get; set; }

        [JsonProperty( "primary_campus" )]
        public QueryItems PrimaryCampus { get; set; }

        [JsonProperty( "name_prefix" )]
        public QueryItems NamePrefix { get; set; }

        [JsonProperty( "name_suffix" )]
        public QueryItems NameSuffix { get; set; }

        [JsonProperty( "school" )]
        public QueryItems School { get; set; }

        [JsonProperty( "social_profiles" )]
        public QueryItems SocialProfiles { get; set; }

        [JsonProperty( "field_data" )]
        public QueryItems FieldData { get; set; }

        [JsonProperty( "households" )]
        public QueryItems Households { get; set; }

        [JsonProperty( "inactive_reason" )]
        public QueryItems InactiveReason { get; set; }

        [JsonProperty( "marital_status" )]
        public QueryItems MaritalStatus { get; set; }

        [JsonProperty( "field_definition" )]
        public QueryItems FieldDefinition { get; set; }

        [JsonProperty( "batch" )]
        public QueryItems Batch { get; set; }

        [JsonProperty( "person" )]
        public QueryItems Person { get; set; }

        [JsonProperty( "designations" )]
        public QueryItems Designations { get; set; }

        [JsonProperty( "fund" )]
        public QueryItems Fund { get; set; }

        [JsonProperty( "owner" )]
        public QueryItems Owner { get; set; }

        [JsonProperty( "note_category" )]
        public QueryItems NoteCategory { get; set; }

        [JsonProperty( "group" )]
        public QueryItems Group { get; set; }

        [JsonProperty( "group_type" )]
        public QueryItems GroupType { get; set; }

        [JsonProperty( "location" )]
        public QueryItems Location { get; set; }

        [JsonProperty( "event" )]
        public QueryItems Event { get; set; }
   }
}