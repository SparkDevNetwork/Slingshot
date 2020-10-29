using Slingshot.PCO.Models.ApiModels;
using System;
using System.Linq;

namespace Slingshot.PCO.Models.DTO
{
    public class GroupMemberDTO
    {
        public int Id { get; set; }

        public string AccountCenterIdentifier { get; set; }

        public string AvatarUrl { get; set; }

        public string ColorIdentifier { get; set; }

        public string EmailAddress { get; set; }

        public string FirstName { get; set; }

        public DateTime? JoinedAt { get; set; }

        public string LastName { get; set; }

        public string PhoneNumber { get; set; }

        public string Role { get; set; }

        public int GroupId { get; set; }

        public int PersonId { get; set; }

        public GroupMemberDTO( DataItem data, int groupId )
        {
            Id = data.Id;
            AccountCenterIdentifier = data.Item.account_center_identifier;
            AvatarUrl = data.Item.avatar_url;
            ColorIdentifier = data.Item.color_identifier;
            EmailAddress = data.Item.email_address;
            FirstName = data.Item.first_name;
            JoinedAt = data.Item.joined_at;
            LastName = data.Item.last_name;
            PhoneNumber = data.Item.phone_number;
            Role = data.Item.role;
            GroupId = groupId;
            SetPersonId( data );
        }

        private void SetPersonId( DataItem data )
        {
            var personRelationship = data.Relationships?.Person?.Data?.FirstOrDefault();
            if ( personRelationship == null )
            {
                return;
            }

            PersonId = personRelationship.Id;
        }
    }
}
