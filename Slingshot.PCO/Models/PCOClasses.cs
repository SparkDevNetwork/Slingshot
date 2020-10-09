using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Slingshot.PCO.Models
{
    public class SingleOrArrayConverter<T> : JsonConverter
    {
        public override bool CanConvert( Type objecType )
        {
            return ( objecType == typeof( List<T> ) );
        }

        public override object ReadJson( JsonReader reader, Type objecType, object existingValue,
            JsonSerializer serializer )
        {
            JToken token = JToken.Load( reader );
            if ( token.Type == JTokenType.Array )
            {
                return token.ToObject<List<T>>();
            }
            return new List<T> { token.ToObject<T>() };
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override void WriteJson( JsonWriter writer, object value, JsonSerializer serializer )
        {
            throw new NotImplementedException();
        }
    }

    public class PCOItemsResult
    {
        [JsonProperty( "links" )]
        public PCOLinks Links { get; set; }

        [JsonProperty( "data" )]
        [JsonConverter( typeof( SingleOrArrayConverter<PCOData> ) )]
        public List<PCOData> Data { get; set; }

        [JsonProperty( "included" )]
        public List<PCOData> IncludedItems { get; set; }

        [JsonProperty( "meta" )]
        public PCOMeta Meta { get; set; }
    }

    public class PCOLinks
    {
        [JsonProperty( "self" )]
        public string Self { get; set; }

        [JsonProperty( "prev" )]
        public string Previous { get; set; }

        [JsonProperty( "next" )]
        public string Next { get; set; }
    }

    public class PCORelationships
    {
        [JsonProperty( "tags" )]
        public PCOItemsResult Tags { get; set; }

        [JsonProperty( "emails" )]
        public PCOItemsResult Emails { get; set; }

        [JsonProperty( "addresses" )]
        public PCOItemsResult Addresses { get; set; }

        [JsonProperty( "phone_numbers" )]
        public PCOItemsResult PhoneNumbers { get; set; }

        [JsonProperty( "field_options" )]
        public PCOItemsResult FieldOptions { get; set; }

        [JsonProperty( "primary_campus" )]
        public PCOItemsResult PrimaryCampus { get; set; }

        [JsonProperty( "name_prefix" )]
        public PCOItemsResult NamePrefix { get; set; }

        [JsonProperty( "name_suffix" )]
        public PCOItemsResult NameSuffix { get; set; }

        [JsonProperty( "school" )]
        public PCOItemsResult School { get; set; }

        [JsonProperty( "social_profiles" )]
        public PCOItemsResult SocialProfiles { get; set; }

        [JsonProperty( "field_data" )]
        public PCOItemsResult FieldData { get; set; }

        [JsonProperty( "households" )]
        public PCOItemsResult Households { get; set; }

        [JsonProperty( "inactive_reason" )]
        public PCOItemsResult InactiveReason { get; set; }

        [JsonProperty( "marital_status" )]
        public PCOItemsResult MaritalStatus { get; set; }

        [JsonProperty( "field_definition" )]
        public PCOItemsResult FieldDefinition { get; set; }

        [JsonProperty( "batch" )]
        public PCOItemsResult Batch { get; set; }

        [JsonProperty( "person" )]
        public PCOItemsResult Person { get; set; }

        [JsonProperty( "designations" )]
        public PCOItemsResult Designations { get; set; }

        [JsonProperty( "fund" )]
        public PCOItemsResult Fund { get; set; }

        [JsonProperty( "owner" )]
        public PCOItemsResult Owner { get; set; }

        [JsonProperty( "note_category" )]
        public PCOItemsResult NoteCategory { get; set; }
    }

    public class PCOData
    {
        [JsonProperty( "type" )]
        public string Type { get; set; }

        [JsonProperty( "id" )]
        public int Id { get; set; }

        [JsonProperty( "attributes" )]
        public dynamic Item { get; set; }

        [JsonProperty( "relationships" )]
        public PCORelationships Relationships { get; set; }
    }

    public class PCOQueryResult
    {
        /// <summary>
        /// The Items included in this query.
        /// </summary>
        public List<PCOData> Items { get; set; }

        /// <summary>
        /// Related items specified in the "include" option of the query.
        /// </summary>
        public List<PCOData> IncludedItems { get; set; }
    }

    public class PCOMeta
    {
        [JsonProperty( "total_count" )]
        public int TotalCount { get; set; }

        [JsonProperty( "count" )]
        public int Count { get; set; }
    }

    public class PCOTagGroup
    {
        public int id { get; set; }
        public string name { get; set; }
        public string folderName { get; set; }
        public List<PCOTag> tags { get; set; }
        public string key { get { return string.Format( "PCOTagGroup:{0}", id ); } }

        public PCOTagGroup()
        {
        }

        public PCOTagGroup( PCOData data ) : this()
        {
            id = data.id;
            name = data.Item.name;
            folderName = data.Item.service_type_folder_name;
        }

        public void UpdateTags( PCOData data, List<PCOData> included )
        {
            tags = new List<PCOTag>();

            if ( data.relationships != null )
            {
                if ( data.relationships.tags != null )
                {
                    foreach ( var relationship in data.relationships.tags.data )
                    {
                        var item = included
                            .Where( i =>
                                i.type == relationship.type &&
                                i.id == relationship.id )
                            .FirstOrDefault();
                        if ( item != null )
                        {
                            tags.Add( new PCOTag( item ) );
                        }
                    }
                }
            }
        }

        public PCOPostItem GetPostItem()
        {
            var item = new PCOPostItem();
            item.data = new PCOPostData();
            item.data.type = "TagGroup";
            item.data.attributes = GetAttributes();
            return item;
        }

        public PCOPatchItem GetPatchItem()
        {
            var item = new PCOPatchItem();
            item.data = new PCOPatchData();
            item.data.type = "TagGroup";
            item.data.id = this.id;
            item.data.attributes = GetAttributes();
            return item;
        }

        private dynamic GetAttributes()
        {
            dynamic attributes = new ExpandoObject();
            attributes.name = this.name;
            return attributes;
        }
    }

    public class PCOTag
    {
        public int id { get; set; }
        public string name { get; set; }
        public string key { get { return string.Format( "PCOTag:{0}", id ); } }

        public PCOTag()
        {
        }

        public PCOTag( PCOData data ) : this()
        {
            id = data.id;
            name = data.Item.name;
        }

        public PCOPostItem GetPostItem()
        {
            var item = new PCOPostItem();
            item.data = new PCOPostData();
            item.data.type = "Tag";
            item.data.attributes = GetAttributes();
            return item;
        }

        public PCOPatchItem GetPatchItem()
        {
            var item = new PCOPatchItem();
            item.data = new PCOPatchData();
            item.data.type = "Tag";
            item.data.id = this.id;
            item.data.attributes = GetAttributes();
            return item;
        }

        private dynamic GetAttributes()
        {
            dynamic attributes = new ExpandoObject();
            attributes.name = this.name;
            return attributes;
        }
    }

    public class PCOPerson
    {
        public int id { get; set; }
        public int? legacy_id { get; set; }
        public string first_name { get; set; }
        public string nickname { get; set; }
        public string given_name { get; set; }
        public string middle_name { get; set; }
        public string last_name { get; set; }
        public string gender { get; set; }
        public DateTime? birthdate { get; set; }
        public DateTime? anniversary { get; set; }
        public bool services { get; set; }
        public string permissions { get; set; }
        public string tags { get; set; }
        public DateTime? created_at { get; set; }
        public DateTime? updated_at { get; set; }
        public string member { get; set; }
        public string status { get; set; }
        public PCOContactData contact_data { get; set; }
        public List<PCOTagGroup> tag_groups { get; set; }
        public PCOHousehold household { get; set; }
        public string name_prefix { get; set; }
        public string name_suffix { get; set; }
        public string marital_status { get; set; }
        public string inactive_reason { get; set; }
        public List<PCOFieldData> field_data { get; set; }
        public bool? child { get; set; }
        public PCOCampus campus { get; set; }
        public List<PCOSocialProfile> socialProfiles { get; set; }
        public string school { get; set; }
        public string avatar { get; set; }
        public bool? passed_background_check { get; set; }

        public PCOPerson()
        {
            services = false;
            permissions = "Archived";
            field_data = new List<PCOFieldData>();
            socialProfiles = new List<PCOSocialProfile>();
        }

        public PCOPerson( PCOData data ) : this()
        {
            id = data.id;
            first_name = data.Item.first_name;
            nickname = data.Item.nickname;
            given_name = data.Item.given_name;
            middle_name = data.Item.middle_name;
            last_name = data.Item.last_name;
            gender = data.Item.gender;
            birthdate = data.Item.birthdate;
            anniversary = data.Item.anniversary;
            created_at = data.Item.created_at;
            updated_at = data.Item.updated_at;
            member = data.Item.membership;
            status = data.Item.status;
            child = data.Item.child;
            passed_background_check = data.Item.passed_background_check;
            if ( !( data.Item.avatar == null ) && !Convert.ToString( data.Item.avatar ).Contains( "static" ) )
            {
                avatar = data.Item.avatar;
            }
        }

        public PCOPerson (PCOData data, bool GivingPerson )
        {
            id = data.id;
            first_name = data.Item.first_name;
            if( data.Item.addresses != null )
            {
                foreach( var address in data.Item.addresses )
                {
                    contact_data.addresses.Add( new PCOAddress
                    {
                        street = (Convert.ToString( address.street_line_1 ) + " " + Convert.ToString( address.street_line_2 )).Trim(),
                        city = address.city,
                        state = address.state,
                        zip = address.zip,
                        location = address.location
                    } );
                }
            }
        }

        public void UpdateServicesInfo( PCOData data )
        {
            services = true;
            legacy_id = data.Item.legacy_id;
            permissions = data.Item.permissions;
        }

        public void UpdateTags( List<PCOData> data )
        {
            tags = string.Join( ",", data.Select( t => t.id ).OrderBy( t => t ).ToArray() );
        }

        public void UpdateContactInfo( PCOData data, List<PCOData> included )
        {
            contact_data = new PCOContactData();

            if ( data.relationships != null )
            {
                if ( data.relationships.emails != null )
                {
                    foreach ( var relationship in data.relationships.emails.data )
                    {
                        var item = included
                            .Where( i =>
                                i.type == relationship.type &&
                                i.id == relationship.id &&
                                i.Item.primary == true
                                )
                            .FirstOrDefault();
                        if ( item != null )
                        {
                            contact_data.email_addresses.Add( new PCOEmailAddress( item ) );
                        }
                    }
                }

                if ( data.relationships.addresses != null )
                {
                    foreach ( var relationship in data.relationships.addresses.data )
                    {
                        var item = included
                            .Where( i =>
                                i.type == relationship.type &&
                                i.id == relationship.id )
                            .FirstOrDefault();
                        if ( item != null )
                        {
                            contact_data.addresses.Add( new PCOAddress( item ) );
                        }
                    }
                }

                if ( data.relationships.phone_numbers != null )
                {
                    foreach ( var relationship in data.relationships.phone_numbers.data )
                    {
                        var item = included
                            .Where( i =>
                                i.type == relationship.type &&
                                i.id == relationship.id )
                            .FirstOrDefault();
                        if ( item != null )
                        {
                            contact_data.phone_numbers.Add( new PCOPhoneNumber( item ) );
                        }
                    }
                }

            }
        }

        public void UpdateHouseHold( PCOData data, List<PCOData> included )
        {
            if ( data.relationships != null )
            {
                if ( data.relationships.households != null )
                {
                    foreach ( var relationship in data.relationships.households.data )
                    {
                        var item = included
                            .Where( i =>
                                i.type == relationship.type &&
                                i.id == relationship.id
                                )
                            .FirstOrDefault();
                        if ( item != null )
                        {
                            household = new PCOHousehold( item );
                        }
                    }
                }
            }
        }

        public void UpdateFieldData( PCOData data, List<PCOData> included )
        {
            if ( data.relationships != null )
            {
                if ( data.relationships.field_data != null )
                {
                    foreach ( var relationship in data.relationships.field_data.data )
                    {
                        var item = included
                            .Where( i =>
                                i.type == relationship.type &&
                                i.id == relationship.id
                                )
                            .FirstOrDefault();
                        if ( item != null )
                        {
                            field_data.Add( new PCOFieldData( item ) );
                        }
                    }
                }
            }
        }

        public void UpdateProperties( PCOData data, List<PCOData> included )
        {
            if ( data.relationships != null )
            {
                if ( data.relationships.inactive_reason != null )
                {
                    foreach ( var relationship in data.relationships.inactive_reason.data )
                    {
                        if ( relationship != null )
                        {
                            var item = included
                            .Where( i =>
                                i.type == relationship.type &&
                                i.id == relationship.id
                                )
                            .FirstOrDefault();
                            if ( item != null )
                            {
                                inactive_reason = item.Item.value;
                            }
                        }
                    }
                }
                if ( data.relationships.marital_status != null )
                {
                    foreach ( var relationship in data.relationships.marital_status.data )
                    {
                        if ( relationship != null )
                        {
                            var item = included
                            .Where( i =>
                                i.type == relationship.type &&
                                i.id == relationship.id
                                )
                            .FirstOrDefault();
                            if ( item != null )
                            {
                                marital_status = item.Item.value;
                            }
                        }
                    }
                }
                if ( data.relationships.name_prefix != null )
                {
                    foreach ( var relationship in data.relationships.name_prefix.data )
                    {
                        if ( relationship != null )
                        {
                            var item = included
                            .Where( i =>
                                i.type == relationship.type &&
                                i.id == relationship.id
                                )
                            .FirstOrDefault();
                            if ( item != null )
                            {
                                name_prefix = item.Item.value;
                            }
                        }
                    }
                }
                if ( data.relationships.school != null )
                {
                    foreach ( var relationship in data.relationships.school.data )
                    {
                        if ( relationship != null )
                        {
                            var item = included
                            .Where( i =>
                                i.type == relationship.type &&
                                i.id == relationship.id
                                )
                            .FirstOrDefault();
                            if ( item != null )
                            {
                                school = item.Item.value;
                            }
                        }
                    }
                }
                if ( data.relationships.name_suffix != null )
                {
                    foreach ( var relationship in data.relationships.name_suffix.data )
                    {
                        if ( relationship != null )
                        {
                            var item = included
                            .Where( i =>
                                i.type == relationship.type &&
                                i.id == relationship.id
                                )
                            .FirstOrDefault();
                            if ( item != null )
                            {
                                name_suffix = item.Item.value;
                            }
                        }
                    }
                }
            }
        }

        public void UpdateCampus( PCOData data, List<PCOData> included )
        {
            if ( data.relationships != null )
            {
                if ( data.relationships.primary_campus != null )
                {
                    foreach ( var relationship in data.relationships.primary_campus.data )
                    {
                        if( relationship != null)
                        {
                            var item = included
                            .Where( i =>
                                i.type == relationship.type &&
                                i.id == relationship.id
                                )
                            .FirstOrDefault();
                            if ( item != null )
                            {
                                campus = new PCOCampus( item );
                            }
                        }
                        
                    }
                }
            }
        }

        public void UpdateSocialProfiles( PCOData data, List<PCOData> included )
        {
            if ( data.relationships != null )
            {
                if ( data.relationships.social_profiles != null )
                {
                    foreach ( var relationship in data.relationships.social_profiles.data )
                    {
                        var item = included
                            .Where( i =>
                                i.type == relationship.type &&
                                i.id == relationship.id
                                )
                            .FirstOrDefault();
                        if ( item != null )
                        {
                            socialProfiles.Add( new PCOSocialProfile( item ) );
                        }
                    }
                }
            }
        }

        public PCOPostItem GetPostItem()
        {
            var item = new PCOPostItem();
            item.data = new PCOPostData();
            item.data.type = "Person";
            item.data.attributes = GetAttributes();
            return item;
        }

        public PCOPatchItem GetPatchItem()
        {
            var item = new PCOPatchItem();
            item.data = new PCOPatchData();
            item.data.type = "Person";
            item.data.id = this.id;
            item.data.attributes = GetAttributes();
            return item;
        }

        private dynamic GetAttributes()
        {
            dynamic attributes = new ExpandoObject();
            attributes.first_name = this.first_name;
            attributes.nickname = this.nickname;
            attributes.given_name = this.given_name;
            attributes.middle_name = this.middle_name;
            attributes.last_name = this.last_name;
            attributes.gender = this.gender;
            attributes.birthdate = this.birthdate;
            attributes.anniversary = this.anniversary;
            return attributes;
        }

    }

    public class RockPerson : PCOPerson
    {
        public string photo_date { get; set; }
    }

    public class PCOContactData
    {
        public List<PCOAddress> addresses { get; set; }
        public List<PCOEmailAddress> email_addresses { get; set; }
        public List<PCOPhoneNumber> phone_numbers { get; set; }

        public PCOContactData()
        {
            addresses = new List<PCOAddress>();
            email_addresses = new List<PCOEmailAddress>();
            phone_numbers = new List<PCOPhoneNumber>();
        }
    }

    public class PCOAddress
    {
        public int id { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public string street { get; set; }
        public string zip { get; set; }
        public string location { get; set; }

        public PCOAddress()
        {

        }

        public PCOAddress( PCOData data )
        {
            id = data.id;
            city = data.Item.city;
            state = data.Item.state;
            street = data.Item.street;
            zip = data.Item.zip;
            location = data.Item.location;
        }

        public PCOPostItem GetPostItem()
        {
            var item = new PCOPostItem();
            item.data = new PCOPostData();
            item.data.type = "Address";
            item.data.attributes = GetAttributes();
            return item;
        }

        public PCOPatchItem GetPatchItem()
        {
            var item = new PCOPatchItem();
            item.data = new PCOPatchData();
            item.data.type = "Address";
            item.data.id = this.id;
            item.data.attributes = GetAttributes();
            return item;
        }

        private dynamic GetAttributes()
        {
            dynamic attributes = new ExpandoObject();
            attributes.street = this.street;
            attributes.city = this.city;
            attributes.state = this.state;
            attributes.zip = this.zip;
            attributes.location = this.location;
            return attributes;
        }
    }

    public class PCOEmailAddress
    {
        public int id { get; set; }
        public string address { get; set; }
        public string location { get; set; }
        public bool primary { get; set; }

        public PCOEmailAddress()
        {

        }

        public PCOEmailAddress( PCOData data )
        {
            id = data.id;
            address = data.Item.address;
            location = data.Item.location;
            primary = data.Item.primary;
        }

        public PCOPostItem GetPostItem()
        {
            var item = new PCOPostItem();
            item.data = new PCOPostData();
            item.data.type = "Email";
            item.data.attributes = GetAttributes();
            return item;
        }

        public PCOPatchItem GetPatchItem()
        {
            var item = new PCOPatchItem();
            item.data = new PCOPatchData();
            item.data.type = "Email";
            item.data.id = this.id;
            item.data.attributes = GetAttributes();
            return item;
        }

        private dynamic GetAttributes()
        {
            dynamic attributes = new ExpandoObject();
            attributes.address = this.address;
            attributes.location = "Home";
            return attributes;
        }
    }

    public class PCOPhoneNumber
    {
        public int id { get; set; }
        public string number { get; set; }
        public string location { get; set; }

        public PCOPhoneNumber()
        {

        }

        public PCOPhoneNumber( PCOData data )
        {
            id = data.id;
            number = data.Item.number;
            location = data.Item.location;
        }

        public PCOPostItem GetPostItem()
        {
            var item = new PCOPostItem();
            item.data = new PCOPostData();
            item.data.type = "PhoneNumber";
            item.data.attributes = GetAttributes();
            return item;
        }

        public PCOPatchItem GetPatchItem()
        {

            var item = new PCOPatchItem();
            item.data = new PCOPatchData();
            item.data.type = "PhoneNumber";
            item.data.id = this.id;
            item.data.attributes = GetAttributes();
            return item;
        }

        private dynamic GetAttributes()
        {
            dynamic attributes = new ExpandoObject();
            attributes.number = this.number;
            attributes.location = this.location;
            return attributes;
        }
    }

    public class PCOEmailTemplate
    {
        public int id { get; set; }
        public string kind { get; set; }
        public string subject { get; set; }

        public PCOEmailTemplate()
        {
        }

        public PCOEmailTemplate( PCOData data )
        {
            id = data.id;
            kind = data.Item.kind;
            subject = data.Item.subject;
        }
    }

    public class PCOHousehold
    {
        public int Id { get; set; }
        public DateTime? created_at { get; set; }
        public int? member_count { get; set; }
        public string name { get; set; }
        public int? primary_contact_id { get; set; }
        public string primary_contact_name { get; set; }
        public DateTime? updated_at { get; set; }
        public PCOHousehold( PCOData data )
        {
            Id = data.id;
            created_at = data.Item.created_at;
            member_count = data.Item.member_count;
            name = data.Item.name;
            primary_contact_id = data.Item.primary_contact_id;
            primary_contact_name = data.Item.primary_contact_name;
            updated_at = data.Item.updated_at;
        }
    }


    public class PCOPostItem
    {
        public PCOPostData data { get; set; }
    }

    public class PCOPostData
    {
        public string type { get; set; }
        public dynamic attributes { get; set; }
    }

    public class PCOPatchItem
    {
        public PCOPatchData data { get; set; }
    }

    public class PCOPatchData : PCOPostData
    {
        public int id { get; set; }
    }

    public class PCOFieldDefinition
    {
        public int id { get; set; }

        public string data_type { get; set; }

        public string name { get; set; }

        public int sequence { get; set; }

        public string slug { get; set; }

        public string config { get; set; }

        public int tab_id { get; set; }

        public DateTime? deleted_at { get; set; }

        public List<PCOFieldOption> FieldOptions { get; set; }

        public PCOFieldDefinition( PCOData data )
        {
            id = data.id;
            data_type = data.Item.data_type;
            name = data.Item.name;
            sequence = data.Item.sequence;
            slug = data.Item.slug;
            //config = data.Item.config;
            deleted_at = data.Item.deteled_at;
            tab_id = data.Item.tab_id;
        }

        public void UpdateFieldOptions( PCOData data, List<PCOData> included )
        {
            if ( data.relationships != null )
            {
                if ( data.relationships.field_options != null )
                {
                    foreach ( var relationship in data.relationships.field_options.data )
                    {
                        var item = included
                            .Where( i =>
                                i.type == relationship.type &&
                                i.id == relationship.id )
                            .FirstOrDefault();
                        if ( item != null )
                        {
                            FieldOptions.Add( new PCOFieldOption( item ) );
                        }
                    }
                }
            }
        }
    }

    public class PCOFieldOption
    {
        public int id { get; set; }

        public string value { get; set; }

        public int sequence { get; set; }

        public PCOFieldOption( PCOData data )
        {
            id = data.id;
            value = data.Item.value;
            sequence = data.Item.sequence;
        }
    }

    public class PCOFieldData
    {
        public int id { get; set; }
        public string file_url { get; set; }
        public string file_content_type { get; set; }
        public string file_name { get; set; }
        public int? file_size { get; set; }
        public string value { get; set; }
        public int field_definition_id { get; set; }

        public PCOFieldData( PCOData data )
        {
            id = data.id;
            file_url = data.Item.file.url;
            file_content_type = data.Item.file_content_type;
            file_name = data.Item.file_name;
            file_size = data.Item.file_size;
            value = data.Item.value;
            if ( data.relationships != null )
            {
                if ( data.relationships.field_definition != null )
                {
                    foreach ( var relationship in data.relationships.field_definition.data )
                    {
                        field_definition_id = relationship.id;
                    }
                }
            }
        }
    }


    public class SingleOrArrayConverter<T> : JsonConverter
    {
        public override bool CanConvert( Type objecType )
        {
            return ( objecType == typeof( List<T> ) );
        }

        public override object ReadJson( JsonReader reader, Type objecType, object existingValue,
            JsonSerializer serializer )
        {
            JToken token = JToken.Load( reader );
            if ( token.Type == JTokenType.Array )
            {
                return token.ToObject<List<T>>();
            }
            return new List<T> { token.ToObject<T>() };
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override void WriteJson( JsonWriter writer, object value, JsonSerializer serializer )
        {
            throw new NotImplementedException();
        }
    }

    public class PCOCampus
    {
        public int id { get; set; }
        public string avatar_url { get; set; }
        public string city { get; set; }
        public string contact_email_address { get; set; }
        public string country { get; set; }
        public DateTime? created_at { get; set; }
        public string description { get; set; }
        public string latitude { get; set; }
        public string longitude { get; set; }
        public string name { get; set; }
        public string phone_number { get; set; }
        public string state { get; set; }
        public string street { get; set; }
        public string time_zone { get; set; }
        public DateTime? updated_at { get; set; }
        public string website { get; set; }
        public string zip { get; set; }

        public PCOCampus( PCOData data )
        {
            id = data.id;
            avatar_url = data.Item.avatar_url;
            city = data.Item.city;
            contact_email_address = data.Item.contact_email_address;
            country = data.Item.country;
            created_at = data.Item.created_at;
            description = data.Item.description;
            latitude = data.Item.latitude;
            longitude = data.Item.longitude;
            name = data.Item.name;
            phone_number = data.Item.phone_number;
            state = data.Item.state;
            street = data.Item.street;
            time_zone = data.Item.time_zone;
            updated_at = data.Item.updated_at;
            website = data.Item.website;
            zip = data.Item.zip;
        }

    }
    public class PCOSocialProfile
    {
        public int id { get; set; }
        public DateTime? created_at { get; set; }
        public string site { get; set; }
        public DateTime? updated_at { get; set; }
        public string url { get; set; }
        public bool verified { get; set; }

        public PCOSocialProfile( PCOData data )
        {
            id = data.id;
            created_at = data.Item.created_at;
            site = data.Item.site;
            updated_at = data.Item.updated_at;
            url = data.Item.url;
            verified = data.Item.verified;
        }
    }

    public class PCOBatch
    {
        public int id { get; set; }
        public DateTime? committed_at { get; set; }
        public DateTime? created_at { get; set; }
        public string description { get; set; }
        public int? total_cents { get; set; }
        public string total_currency { get; set; }
        public DateTime? updated_at { get; set; }
        public int? ownerId { get; set; }

        public PCOBatch( PCOData data )
        {
            id = data.id;
            committed_at = data.Item.committed_at;
            created_at = data.Item.created_at;
            description = data.Item.description;
            total_cents = data.Item.total_cents;
            total_currency = data.Item.total_currency;
            updated_at = data.Item.updated_at;
            if ( data.relationships != null )
            {
                if ( data.relationships.owner != null )
                {
                    foreach ( var relationship in data.relationships.owner.data )
                    {
                        if ( relationship != null )
                        {
                            ownerId = relationship.id;
                        }
                    }
                }
            }
        }
    }
    public class PCOFund
    {
        public int id { get; set; }
        public string color { get; set; }
        public DateTime? created_at { get; set; }
        public bool? deletable { get; set; }
        public string description { get; set; }
        public string ledger_code { get; set; }
        public string name { get; set; }
        public DateTime? updated_at { get; set; }
        public string visibility { get; set; }

        public PCOFund( PCOData data )
        {
            id = data.id;
            color = data.Item.color;
            created_at = data.Item.created_at;
            deletable = data.Item.deltable;
            description = data.Item.description;
            ledger_code = data.Item.ledger_code;
            name = data.Item.name;
            updated_at = data.Item.updated_at;
            visibility = data.Item.visibility;
        }

    }

    public class PCODonation
    {
        public int id { get; set; }
        public int? amount_cents { get; set; }
        public string amount_currency { get; set; }
        public DateTime? created_at { get; set; }
        public int? fee_cents { get; set; }
        public string fee_currency { get; set; }
        public string payment_brand { get; set; }
        public DateTime? payment_check_dated_at { get; set; }
        public string payment_check_number { get; set; }
        public string payment_last4 { get; set; }
        public string payment_method { get; set; }
        public string payment_method_sub { get; set; }
        public string payment_status { get; set; }
        public DateTime? received_at { get; set; }
        public bool? refundable { get; set; }
        public bool? refunded { get; set; }
        public DateTime? updated_at { get; set; }
        public List<PCODesignation> designations { get; set; }
        public int? batchId { get; set; }
        public int? personId { get; set; }

        public PCODonation( PCOData data )
        {
            id = data.id;
            amount_cents = data.Item.amount_cents;
            amount_currency = data.Item.amount_currency;
            created_at = data.Item.created_at;
            fee_cents = data.Item.fee_cents;
            fee_currency = data.Item.fee_currency;
            payment_brand = data.Item.payment_brand;
            payment_check_dated_at = data.Item.payment_check_dated_at;
            payment_check_number = data.Item.payment_check_number;
            payment_last4 = data.Item.payment_last4;
            payment_method = data.Item.payment_method;
            payment_method_sub = data.Item.payment_method_sub;
            payment_status = data.Item.payment_status;
            received_at = data.Item.received_at;
            refundable = data.Item.refundable;
            refunded = data.Item.refunded;
            updated_at = data.Item.updated_at;
            if ( data.relationships != null )
            {
                if ( data.relationships.batch != null )
                {
                    foreach ( var relationship in data.relationships.batch.data )
                    {
                        if( relationship != null )
                        {
                            batchId = relationship.id;
                        }
                    }
                }
                if ( data.relationships.person != null )
                {
                    foreach ( var relationship in data.relationships.person.data )
                    {
                        if ( relationship != null )
                        {
                            personId = relationship.id;
                        }
                    }
                }
            }
            designations = new List<PCODesignation>();
        }

        public void UpdateDesignation( PCOData data, List<PCOData> included )
        {
            if ( data.relationships != null )
            {
                if ( data.relationships.designations != null )
                {
                    foreach ( var relationship in data.relationships.designations.data )
                    {
                        var item = included
                            .Where( i =>
                                i.type == relationship.type &&
                                i.id == relationship.id
                                )
                            .FirstOrDefault();
                        if ( item != null )
                        {
                            designations.Add( new PCODesignation( item ) );
                        }
                    }
                }
            }
        }
    }

    public class PCODesignation
    {
        public int id { get; set; }
        public int? amount_cents { get; set; }
        public string amount_currency { get; set; }
        public int? fundId { get; set; }

        public PCODesignation( PCOData data )
        {
            id = data.id;
            amount_cents = data.Item.amount_cents;
            amount_currency = data.Item.amount_currency;
            if ( data.relationships != null )
            {
                if ( data.relationships.fund != null )
                {
                    foreach ( var relationship in data.relationships.fund.data )
                    {
                        if ( relationship != null )
                        {
                            fundId = relationship.id;
                        }
                    }
                }
            }
        }
    }

    public class PCONote
    {
        public int id { get; set; }
        public DateTime? created_at { get; set; }
        public int? created_by_id { get; set; }
        public string note { get; set; }
        public int? note_category_id { get; set; }
        public int? person_id { get; set; }
        public PCONoteCategory note_category { get; set; }

        public PCONote( PCOData data )
        {
            id = data.id;
            created_at = data.Item.created_at;
            created_by_id = data.Item.created_by_id;
            note = data.Item.note;
            note_category_id = data.Item.note_category_id;
            person_id = data.Item.person_id;
        }

        public void UpdateCategory( PCOData data, List<PCOData> included )
        {
            if ( data.relationships != null )
            {
                if ( data.relationships.note_category != null )
                {
                    foreach ( var relationship in data.relationships.note_category.data )
                    {
                        var item = included
                            .Where( i =>
                                i.type == relationship.type &&
                                i.id == relationship.id
                                )
                            .FirstOrDefault();
                        if ( item != null )
                        {
                            note_category = new PCONoteCategory( item );
                        }
                    }
                }
            }
        }

    }

    public class PCONoteCategory
    {
        public int id { get; set; }
        public string name { get; set; }

        public PCONoteCategory( PCOData data )
        {
            id = data.id;
            name = data.Item.name;
        }
    }
}
    



