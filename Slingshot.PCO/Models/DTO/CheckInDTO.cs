using Slingshot.PCO.Models.ApiModels;
using Slingshot.PCO.Utilities;
using System;
using System.Collections.Generic;

namespace Slingshot.PCO.Models.DTO
{
    public class CheckInDTO
    {
        public int Id { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string MedicalNotes { get; set; }

        public string Kind { get; set; }

        public string Number { get; set; }

        public string SecurityCode { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public DateTime? CheckedOutAt { get; set; }

        public string EmergencyContactName { get; set; }

        public string EmergencyContactPhoneNumber { get; set; }

        public CheckInEventDTO Event { get; set; }

        public CheckInLocationDTO Location { get; set; }

        public int PersonId { get; set; }

        public CheckInDTO( DataItem data, Dictionary<string, DataItem> includedItems )
        {
            Id = data.Id;
            FirstName = data.Item.first_name;
            LastName = data.Item.last_name;
            MedicalNotes = data.Item.medical_notes;
            Kind = data.Item.kind;
            Number = data.Item.number;
            SecurityCode = data.Item.security_code;
            CreatedAt = data.Item.created_at;
            UpdatedAt = data.Item.updated_at;
            CheckedOutAt = data.Item.checked_out_at;
            EmergencyContactName = data.Item.emergency_contact_name;
            EmergencyContactPhoneNumber = data.Item.emergency_contact_phone_number;
            SetEvent( data, includedItems );
            SetLocation( data, includedItems );
            SetPersonId( data );
        }

        private void SetEvent( DataItem data, Dictionary<string, DataItem> includedItems )
        {
            if ( data.Relationships == null || data.Relationships.Event == null )
            {
                return;
            }

            foreach ( var relationship in data.Relationships.Event.Data )
            {
                var item = includedItems.LocateItem( relationship );

                if ( item == null )
                {
                    continue;
                }

                Event = new CheckInEventDTO( item );
            }
        }

        private void SetLocation( DataItem data, Dictionary<string, DataItem> includedItems )
        {
            if ( data.Relationships == null || data.Relationships.Location == null )
            {
                return;
            }

            foreach ( var relationship in data.Relationships.Location.Data )
            {
                var item = includedItems.LocateItem( relationship );

                if ( item == null )
                {
                    continue;
                }

                Location = new CheckInLocationDTO( item );
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
    }
}
