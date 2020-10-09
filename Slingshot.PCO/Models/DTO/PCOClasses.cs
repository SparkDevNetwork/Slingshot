using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using Slingshot.PCO.Models.ApiModels;

namespace Slingshot.PCO.Models.DTO
{
    public static class QueryResultExtensions
    {
        /// <summary>
        /// Matches a DataItem from the relationship by the key (Type:Id).
        /// </summary>
        /// <param name="included">The dictionary of <see cref="DataItem"/>s included in the API query.</param>
        /// <param name="relationship">The relationship <see cref="DataItem"/>.</param>
        /// <returns></returns>
        public static DataItem LocateItem( this Dictionary<string, DataItem> included, DataItem relationship )
        {
            if ( relationship == null )
            {
                // Nothing to match.
                return null;
            }

            return included.LocateItem( relationship.Type, relationship.Id );
        }

        /// <summary>
        /// Matches a DataItem directly by the key (Type:Id).
        /// </summary>
        /// <param name="included">The dictionary of <see cref="DataItem"/>s included in the API query.</param>
        /// <param name="relationship">The relationship <see cref="DataItem"/>.</param>
        /// <returns></returns>
        public static DataItem LocateItem( this Dictionary<string, DataItem> included, string type, int id )
        {
            string itemKey = $"{type}:{id}";
            if ( !included.ContainsKey( itemKey ) )
            {
                // Item isn't in the collection.
                return null;
            }

            return included[itemKey];
        }
    }

    public class QueryResult
    {
        /// <summary>
        /// The Items included in this query.
        /// </summary>
        public List<DataItem> Items { get; set; }

        /// <summary>
        /// Related items specified in the "include" option of the query.
        /// </summary>
        public Dictionary<string, DataItem> IncludedItems { get; }

        /// <summary>
        /// Constructor for new query.
        /// </summary>
        /// <param name="newItems"></param>
        public QueryResult(List<DataItem> newItems)
        {
            Items = new List<DataItem>();
            IncludedItems = new Dictionary<string, DataItem>();
            AddIncludedItems(newItems);
        }

        /// <summary>
        /// Sorts the included items by key and adds them to the dictionary.
        /// </summary>
        /// <param name="newItems">The list of included items.</param>
        private void AddIncludedItems( List<DataItem> newItems )
        {
            foreach ( var includedItem in newItems )
            {
                string key = $"{includedItem.Type}:{includedItem.Id}";
                IncludedItems.Add(key, includedItem);
            }
        }

        /// <summary>
        /// Constructor for a new page to an existing paged query.
        /// </summary>
        /// <param name="newItems"></param>
        public QueryResult( QueryResult existingResults, List<DataItem> includedItems )
        {
            Items = existingResults.Items;
            IncludedItems = existingResults.IncludedItems;
            MergeIncludedItems( includedItems );
        }

        /// <summary>
        /// Sorts the included items by key and adds them to the dictionary if they are not already there.
        /// </summary>
        /// <param name="newItems">The list of included items.</param>
        private void MergeIncludedItems( List<DataItem> newItems )
        {
            foreach ( var includedItem in newItems )
            {
                string key = $"{includedItem.Type}:{includedItem.Id}";

                if ( IncludedItems.ContainsKey( key ) )
                {
                    continue;
                }

                IncludedItems.Add( key, includedItem );
            }
        }
    }

    public class TagGroupDTO
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string FolderName { get; set; }

        public List<TagDTO> Tags { get; set; }

        public string Key
        {
            get { return string.Format( "PCOTagGroup:{0}", Id ); }
        }

        public TagGroupDTO( DataItem data )
        {
            Id = data.Id;
            Name = data.Item.name;
            FolderName = data.Item.service_type_folder_name;
            Tags = new List<TagDTO>();
            // ToDo:  Tags collection is not implemented.
        }
    }

    public class TagDTO
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Key { get { return string.Format( "PCOTag:{0}", Id ); } }

        public TagDTO( DataItem data )
        {
            Id = data.Id;
            Name = data.Item.name;
        }
    }

    public class PersonDTO
    {
        public int Id { get; set; }

        public int? RemoteId { get; set; }

        public string FirstName { get; set; }

        public string Nickname { get; set; }

        public string GivenName { get; set; }

        public string MiddleName { get; set; }

        public string LastName { get; set; }

        public string Gender { get; set; }

        public DateTime? Birthdate { get; set; }

        public DateTime? Anniversary { get; set; }

        public bool Services { get; set; }

        public string Permissions { get; set; }

        public string Tags { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public string Member { get; set; }

        public string Status { get; set; }

        public ContactDataDTO ContactData { get; set; }

        public List<TagGroupDTO> TagGroups { get; set; }

        public HouseholdDTO Household { get; set; }

        public string NamePrefix { get; set; }

        public string NameSuffix { get; set; }

        public string MaritalStatus { get; set; }

        public string InactiveReason { get; set; }

        public List<FieldDataDTO> FieldData { get; set; }

        public bool? Child { get; set; }

        public CampusDTO Campus { get; set; }

        public List<SocialProfileDTO> SocialProfiles { get; set; }

        public string School { get; set; }

        public string Avatar { get; set; }

        public bool? PassedBackgroundCheck { get; set; }

        public PersonDTO( DataItem data, Dictionary<string, DataItem> includedItems )
        {
            Services = false;
            Permissions = "Archived";
            FieldData = new List<FieldDataDTO>();
            SocialProfiles = new List<SocialProfileDTO>();

            Id = data.Id;
            RemoteId = data.Item.remote_id;
            FirstName = data.Item.first_name;
            Nickname = data.Item.nickname;
            GivenName = data.Item.given_name;
            MiddleName = data.Item.middle_name;
            LastName = data.Item.last_name;
            Gender = data.Item.gender;
            Birthdate = data.Item.birthdate;
            Anniversary = data.Item.anniversary;
            CreatedAt = data.Item.created_at;
            UpdatedAt = data.Item.updated_at;
            Member = data.Item.membership;
            Status = data.Item.status;
            Child = data.Item.child;
            PassedBackgroundCheck = data.Item.passed_background_check;

            if ( data.Item.avatar != null && !data.Item.avatar.ToString().Contains( "static" ) )
            {
                Avatar = data.Item.avatar;
            }

            SetContactInfo( data, includedItems );
            SetHousehold( data, includedItems );
            SetFieldData( data, includedItems );
            SetCampus( data, includedItems );
            SetProperties( data, includedItems );
            SetSocialProfiles( data, includedItems );
            //ToDo:  Tags collection is not initialized with data.
        }

        private void SetContactInfo( DataItem data, Dictionary<string, DataItem> included )
        {
            ContactData = new ContactDataDTO();

            if ( data.Relationships == null )
            {
                return;
            }

            // Set Email Addresses.
            if ( data.Relationships.Emails != null )
            {
                foreach ( var relationship in data.Relationships.Emails.Data )
                {
                    var item = included.LocateItem( relationship );

                    if ( item == null )
                    {
                        continue;
                    }

                    ContactData.EmailAddresses.Add( new EmailAddressDTO( item ) );
                }
            }

            // Set Addresses.
            if ( data.Relationships.Addresses != null )
            {
                foreach ( var relationship in data.Relationships.Addresses.Data )
                {
                    var item = included.LocateItem( relationship );

                    if ( item == null )
                    {
                        continue;
                    }

                    ContactData.Addresses.Add( new AddressDTO( item ) );
                }
            }

            // Set Phone Numbers.
            if ( data.Relationships.PhoneNumbers != null )
            {
                foreach ( var relationship in data.Relationships.PhoneNumbers.Data )
                {
                    var item = included.LocateItem( relationship );

                    if ( item == null )
                    {
                        continue;
                    }

                    ContactData.PhoneNumbers.Add( new PhoneNumberDTO( item ) );
                }
            }
        }

        private void SetHousehold( DataItem data, Dictionary<string, DataItem> included )
        {
            if ( data.Relationships == null || data.Relationships.Households == null )
            {
                return;
            }

            foreach ( var relationship in data.Relationships.Households.Data )
            {
                var item = included.LocateItem( relationship );

                if ( item == null )
                {
                    continue;
                }

                Household = new HouseholdDTO( item );
            }
        }

        private void SetFieldData( DataItem data, Dictionary<string, DataItem> included )
        {
            if ( data.Relationships == null || data.Relationships.FieldData == null )
            {
                return;
            }

            foreach ( var relationship in data.Relationships.FieldData.Data )
            {
                var item = included.LocateItem( relationship );

                if ( item == null )
                {
                    continue;
                }

                FieldData.Add( new FieldDataDTO( item ) );
            }
        }

        private void SetProperties( DataItem data, Dictionary<string, DataItem> included )
        {
            if ( data.Relationships == null )
            {
                return;
            }

            // Set Inactive Reason.
            if ( data.Relationships.InactiveReason != null )
            {
                foreach ( var relationship in data.Relationships.InactiveReason.Data )
                {
                    var item = included.LocateItem( relationship );

                    if ( item == null )
                    {
                        continue;
                    }

                    InactiveReason = item.Item.value;
                }
            }

            // Set Marital Status.
            if ( data.Relationships.MaritalStatus != null )
            {
                foreach ( var relationship in data.Relationships.MaritalStatus.Data )
                {
                    var item = included.LocateItem( relationship );

                    if ( item == null )
                    {
                        continue;
                    }

                    MaritalStatus = item.Item.value;
                }
            }

            // Set Name Prefix.
            if ( data.Relationships.NamePrefix != null )
            {
                foreach ( var relationship in data.Relationships.NamePrefix.Data )
                {
                    var item = included.LocateItem( relationship );

                    if ( item == null )
                    {
                        continue;
                    }

                    NamePrefix = item.Item.value;
                }
            }

            // Set School.
            if ( data.Relationships.School != null )
            {
                foreach ( var relationship in data.Relationships.School.Data )
                {
                    var item = included.LocateItem( relationship );

                    if ( item == null )
                    {
                        continue;
                    }

                    School = item.Item.value;
                }
            }

            // Set Name Suffix.
            if ( data.Relationships.NameSuffix != null )
            {
                foreach ( var relationship in data.Relationships.NameSuffix.Data )
                {
                    var item = included.LocateItem( relationship );

                    if ( item == null )
                    {
                        continue;
                    }

                    NameSuffix = item.Item.value;
                }
            }
        }

        private void SetCampus( DataItem data, Dictionary<string, DataItem> included )
        {
            if ( data.Relationships == null || data.Relationships.PrimaryCampus == null )
            {
                return;
            }

            foreach ( var relationship in data.Relationships.PrimaryCampus.Data )
            {
                var item = included.LocateItem( relationship );

                if ( item == null )
                {
                    continue;
                }

                Campus = new CampusDTO( item );
            }
        }

        private void SetSocialProfiles( DataItem data, Dictionary<string, DataItem> included )
        {
            if ( data.Relationships == null || data.Relationships.SocialProfiles == null )
            {
                return;
            }

            foreach ( var relationship in data.Relationships.SocialProfiles.Data )
            {
                var item = included.LocateItem( relationship );

                if ( item == null )
                {
                    continue;
                }

                SocialProfiles.Add( new SocialProfileDTO( item ) );
            }
        }
    }

    public class ContactDataDTO
    {
        public List<AddressDTO> Addresses { get; set; }

        public List<EmailAddressDTO> EmailAddresses { get; set; }

        public List<PhoneNumberDTO> PhoneNumbers { get; set; }

        public ContactDataDTO()
        {
            Addresses = new List<AddressDTO>();
            EmailAddresses = new List<EmailAddressDTO>();
            PhoneNumbers = new List<PhoneNumberDTO>();
        }
    }

    public class AddressDTO
    {
        public int Id { get; set; }

        public string City { get; set; }

        public string State { get; set; }

        public string Street { get; set; }

        public string Zip { get; set; }

        public string Location { get; set; }

        public AddressDTO( DataItem data )
        {
            Id = data.Id;
            City = data.Item.city;
            State = data.Item.state;
            Street = data.Item.street;
            Zip = data.Item.zip;
            Location = data.Item.location;
        }
    }

    public class EmailAddressDTO
    {
        public int Id { get; set; }

        public string Address { get; set; }

        public string Location { get; set; }

        public bool Primary { get; set; }

        public EmailAddressDTO( DataItem data )
        {
            Id = data.Id;
            Address = data.Item.address;
            Location = data.Item.location;
            Primary = data.Item.primary;
        }
    }

    public class PhoneNumberDTO
    {
        public int Id { get; set; }

        public string Number { get; set; }

        public string Location { get; set; }

        public PhoneNumberDTO( DataItem data )
        {
            Id = data.Id;
            Number = data.Item.number;
            Location = data.Item.location;
        }
    }

    public class EmailTemplateDTO
    {
        public int Id { get; set; }

        public string Kind { get; set; }

        public string Subject { get; set; }

        public EmailTemplateDTO( DataItem data )
        {
            Id = data.Id;
            Kind = data.Item.kind;
            Subject = data.Item.subject;
        }
    }

    public class HouseholdDTO
    {
        public int Id { get; set; }

        public DateTime? CreatedAt { get; set; }

        public int? MemberCount { get; set; }

        public string Name { get; set; }

        public int? PrimaryContactId { get; set; }

        public string PrimaryContactName { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public HouseholdDTO( DataItem data )
        {
            Id = data.Id;
            CreatedAt = data.Item.created_at;
            MemberCount = data.Item.member_count;
            Name = data.Item.name;
            PrimaryContactId = data.Item.primary_contact_id;
            PrimaryContactName = data.Item.primary_contact_name;
            UpdatedAt = data.Item.updated_at;
        }
    }

    public class FieldDefinitionDTO
    {
        public int Id { get; set; }

        public string DataType { get; set; }

        public string Name { get; set; }

        public int Sequence { get; set; }

        public string Slug { get; set; }

        public string Config { get; set; }

        public int TabId { get; set; }

        public DateTime? DeletedAt { get; set; }

        public TabDTO Tab { get; set; }

        public FieldDefinitionDTO( DataItem data, Dictionary<string, DataItem> includedItems )
        {
            Id = data.Id;
            DataType = data.Item.data_type;
            Name = data.Item.name;
            Sequence = data.Item.sequence;
            Slug = data.Item.slug;
            Config = data.Item.config;
            DeletedAt = data.Item.deteled_at;
            TabId = data.Item.tab_id;
            UpdateTab( data, includedItems );
        }

        private void UpdateTab( DataItem data, Dictionary<string, DataItem> includedItems )
        {
            int tabId = data.Item.tab_id;
            var tab = includedItems.LocateItem( "Tab", tabId );

            if ( tab == null )
            {
                return;
            }

            Tab = new TabDTO( tab );
        }
    }

    public class TabDTO
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int Sequence { get; set; }

        public string Slug { get; set; }

        public TabDTO( DataItem data )
        {
            Id = data.Id;
            Sequence = data.Item.sequence;
            Slug = data.Item.slug;
            Name = data.Item.name;
        }
    }

    public class FieldOptionDTO
    {
        public int Id { get; set; }

        public string Value { get; set; }

        public int Sequence { get; set; }

        public FieldOptionDTO( DataItem data )
        {
            Id = data.Id;
            Value = data.Item.value;
            Sequence = data.Item.sequence;
        }
    }

    public class FieldDataDTO
    {
        public int Id { get; set; }

        public string FileUrl { get; set; }

        public string FileContentType { get; set; }

        public string FileName { get; set; }

        public int? FileSize { get; set; }

        public string Value { get; set; }

        public int FieldDefinitionId { get; set; }

        public FieldDataDTO( DataItem data )
        {
            Id = data.Id;
            FileUrl = data.Item.file.url;
            FileContentType = data.Item.file_content_type;
            FileName = data.Item.file_name;
            FileSize = data.Item.file_size;
            Value = data.Item.value;
            SetFieldDefinitionId( data );
        }

        private void SetFieldDefinitionId( DataItem data )
        {
            if ( data.Relationships == null || data.Relationships.FieldDefinition == null )
            {
                return;
            }

            foreach ( var relationship in data.Relationships.FieldDefinition.Data )
            {
                FieldDefinitionId = relationship.Id;
            }
        }
    }

    public class CampusDTO
    {
        public int Id { get; set; }

        public string AvatarUrl { get; set; }

        public string City { get; set; }

        public string ContactEmailAddress { get; set; }

        public string Country { get; set; }

        public DateTime? CreatedAt { get; set; }

        public string Description { get; set; }

        public string Latitude { get; set; }

        public string Longitude { get; set; }

        public string Name { get; set; }

        public string PhoneNumber { get; set; }

        public string State { get; set; }

        public string Street { get; set; }

        public string TimeZone { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public string Website { get; set; }

        public string Zip { get; set; }

        public CampusDTO( DataItem data )
        {
            Id = data.Id;
            AvatarUrl = data.Item.avatar_url;
            City = data.Item.city;
            ContactEmailAddress = data.Item.contact_email_address;
            Country = data.Item.country;
            CreatedAt = data.Item.created_at;
            Description = data.Item.description;
            Latitude = data.Item.latitude;
            Longitude = data.Item.longitude;
            Name = data.Item.name;
            PhoneNumber = data.Item.phone_number;
            State = data.Item.state;
            Street = data.Item.street;
            TimeZone = data.Item.time_zone;
            UpdatedAt = data.Item.updated_at;
            Website = data.Item.website;
            Zip = data.Item.zip;
        }
    }

    public class SocialProfileDTO
    {
        public int Id { get; set; }

        public DateTime? CreatedAt { get; set; }

        public string Site { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public string Url { get; set; }

        public bool Verified { get; set; }

        public SocialProfileDTO( DataItem data )
        {
            Id = data.Id;
            CreatedAt = data.Item.created_at;
            Site = data.Item.site;
            UpdatedAt = data.Item.updated_at;
            Url = data.Item.url;
            Verified = data.Item.verified;
        }
    }

    public class BatchDTO
    {
        public int Id { get; set; }

        public DateTime? CommittedAt { get; set; }

        public DateTime? CreatedAt { get; set; }

        public string Description { get; set; }

        public int? TotalCents { get; set; }

        public string TotalCurrency { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public int? OwnerId { get; set; }

        public BatchDTO( DataItem data )
        {
            Id = data.Id;
            CommittedAt = data.Item.committed_at;
            CreatedAt = data.Item.created_at;
            Description = data.Item.description;
            TotalCents = data.Item.total_cents;
            TotalCurrency = data.Item.total_currency;
            UpdatedAt = data.Item.updated_at;
            SetOwnerId( data );
        }

        private void SetOwnerId( DataItem data )
        {
            if ( data.Relationships == null || data.Relationships.Owner == null )
            {
                return;
            }

            foreach ( var relationship in data.Relationships.Owner.Data )
            {
                if ( relationship == null )
                {
                    continue;
                }

                OwnerId = relationship.Id;
            }
        }
    }

    public class FundDTO
    {
        public int Id { get; set; }

        public string Color { get; set; }

        public DateTime? CreatedAt { get; set; }

        public bool? Deletable { get; set; }

        public string Description { get; set; }

        public string LedgerCode { get; set; }

        public string Name { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public string Visibility { get; set; }

        public FundDTO( DataItem data )
        {
            Id = data.Id;
            Color = data.Item.color;
            CreatedAt = data.Item.created_at;
            Deletable = data.Item.deltable;
            Description = data.Item.description;
            LedgerCode = data.Item.ledger_code;
            Name = data.Item.name;
            UpdatedAt = data.Item.updated_at;
            Visibility = data.Item.visibility;
        }
    }

    public class DonationDTO
    {
        public int Id { get; set; }

        public int? AmountCents { get; set; }

        public string AmountCurrency { get; set; }

        public DateTime? CreatedAt { get; set; }

        public int? FeeCents { get; set; }

        public string FeeCurrency { get; set; }

        public string PaymentBrand { get; set; }

        public DateTime? PaymentCheckDatedAt { get; set; }

        public string PaymentCheckNumber { get; set; }

        public string PaymentLastFour { get; set; }

        public string PaymentMethod { get; set; }

        public string PaymentMethodSub { get; set; }

        public string PaymentStatus { get; set; }

        public DateTime? ReceivedAt { get; set; }

        public bool? Refundable { get; set; }

        public bool? Refunded { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public List<DesignationDTO> Designations { get; set; }

        public int? BatchId { get; set; }

        public int? PersonId { get; set; }

        public DonationDTO( DataItem data, Dictionary<string, DataItem> includedItems )
        {
            Id = data.Id;
            AmountCents = data.Item.amount_cents;
            AmountCurrency = data.Item.amount_currency;
            CreatedAt = data.Item.created_at;
            FeeCents = data.Item.fee_cents;
            FeeCurrency = data.Item.fee_currency;
            PaymentBrand = data.Item.payment_brand;
            PaymentCheckDatedAt = data.Item.payment_check_dated_at;
            PaymentCheckNumber = data.Item.payment_check_number;
            PaymentLastFour = data.Item.payment_last4;
            PaymentMethod = data.Item.payment_method;
            PaymentMethodSub = data.Item.payment_method_sub;
            PaymentStatus = data.Item.payment_status;
            ReceivedAt = data.Item.received_at;
            Refundable = data.Item.refundable;
            Refunded = data.Item.refunded;
            UpdatedAt = data.Item.updated_at;
            SetBatchId( data );
            SetPersonId( data );
            SetDesignation( data, includedItems );
        }

        private void SetBatchId( DataItem data )
        {
            if ( data.Relationships == null || data.Relationships.Batch == null )
            {
                return;
            }

            foreach ( var relationship in data.Relationships.Batch.Data )
            {
                if( relationship == null )
                {
                    continue;
                }

                BatchId = relationship.Id;
            }
        }

        private void SetPersonId( DataItem data )
        {
            if ( data.Relationships == null || data.Relationships.Person == null )
            {
                return;
            }

            foreach ( var relationship in data.Relationships.Person.Data )
            {
                if ( relationship == null )
                {
                    continue;
                }

                PersonId = relationship.Id;
            }

        }

        private void SetDesignation( DataItem data, Dictionary<string, DataItem> included )
        {
            Designations = new List<DesignationDTO>();

            if ( data.Relationships == null || data.Relationships.Designations == null )
            {
                return;
            }
            foreach ( var relationship in data.Relationships.Designations.Data )
            {
                var item = included.LocateItem( relationship );

                if ( item == null )
                {
                    continue;
                }

                Designations.Add( new DesignationDTO( item ) );
            }
        }
    }

    public class DesignationDTO
    {
        public int Id { get; set; }

        public int? AmountCents { get; set; }

        public string AmountCurrency { get; set; }

        public int? FundId { get; set; }

        public DesignationDTO( DataItem data )
        {
            Id = data.Id;
            AmountCents = data.Item.amount_cents;
            AmountCurrency = data.Item.amount_currency;
            SetFundId( data );
        }

        private void SetFundId( DataItem data )
        {
            if ( data.Relationships == null || data.Relationships.Fund == null )
            {
                return;
            }

            foreach ( var relationship in data.Relationships.Fund.Data )
            {
                if ( relationship == null )
                {
                    continue;
                }

                FundId = relationship.Id;
            }
        }
    }

    public class NoteDTO
    {
        public int Id { get; set; }

        public DateTime? CreatedAt { get; set; }

        public int? CreatedById { get; set; }

        public string Note { get; set; }

        public int? NoteCategoryId { get; set; }

        public int? PersonId { get; set; }

        public NoteCategoryDTO NoteCategory { get; set; }

        public NoteDTO( DataItem data, Dictionary<string, DataItem> includedItems )
        {
            Id = data.Id;
            CreatedAt = data.Item.created_at;
            CreatedById = data.Item.created_by_id;
            Note = data.Item.note;
            NoteCategoryId = data.Item.note_category_id;
            PersonId = data.Item.person_id;
            UpdateCategory( data, includedItems );
        }

        private void UpdateCategory( DataItem data, Dictionary<string, DataItem> included )
        {
            if ( data.Relationships == null || data.Relationships.NoteCategory == null )
            {
                return;
            }
            foreach ( var relationship in data.Relationships.NoteCategory.Data )
            {
                var item = included.LocateItem( relationship );

                if ( item == null )
                {
                    continue;
                }

                NoteCategory = new NoteCategoryDTO( item );
            }
        }
    }

    public class NoteCategoryDTO
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public NoteCategoryDTO( DataItem data )
        {
            Id = data.Id;
            Name = data.Item.name;
        }
    }
}
    



