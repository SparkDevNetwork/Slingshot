using Slingshot.PCO.Models.ApiModels;
using Slingshot.PCO.Utilities;
using System;
using System.Collections.Generic;

namespace Slingshot.PCO.Models.DTO
{
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

        public int? Grade { get; set; }

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
            Grade = data.Item.grade;
            CreatedAt = data.Item.created_at;
            UpdatedAt = data.Item.updated_at;
            Member = data.Item.membership;
            Status = data.Item.status;
            Child = data.Item.child;
            PassedBackgroundCheck = data.Item.passed_background_check;

            SetAvatar( data );
            SetContactInfo( data, includedItems );
            SetHousehold( data, includedItems );
            SetFieldData( data, includedItems );
            SetCampus( data, includedItems );
            SetProperties( data, includedItems );
            SetSocialProfiles( data, includedItems );
            // ToDo:  Tags collection is not initialized with data.
        }

        private void SetAvatar( DataItem data )
        {
            if ( data.Item.avatar == null )
            {
                return;
            }

            var avatar = ( string ) data.Item.avatar;
            if ( avatar.Contains( "static" ) )
            {
                return;
            }

            Avatar = avatar;
        }

        private void SetGrade( DataItem data )
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
            InactiveReason = GetPropertyValue( data.Relationships.InactiveReason, included );

            // Set Marital Status.
            MaritalStatus = GetPropertyValue( data.Relationships.MaritalStatus, included );

            // Set Name Prefix.
            NamePrefix = GetPropertyValue( data.Relationships.NamePrefix, included );

            // Set School.
            School = GetPropertyValue( data.Relationships.School, included );

            // Set Name Suffix.
            NameSuffix = GetPropertyValue( data.Relationships.NameSuffix, included );
        }

        private string GetPropertyValue( QueryItems property, Dictionary<string, DataItem> included )
        {
            var propertyValue = string.Empty;

            if ( property == null )
            {
                return propertyValue;
            }

            foreach ( var relationship in property.Data )
            {
                var item = included.LocateItem( relationship );

                if ( item == null )
                {
                    continue;
                }

                propertyValue = item.Item.value;
            }

            return propertyValue;
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
}
