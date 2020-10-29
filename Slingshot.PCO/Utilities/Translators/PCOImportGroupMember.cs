using Slingshot.Core.Model;
using Slingshot.PCO.Models.DTO;

namespace Slingshot.PCO.Utilities.Translators
{
    public static class PCOImportGroupMember
    {
        public static GroupMember Translate( GroupMemberDTO inputGroupMember )
        {
            if ( inputGroupMember.Id <= 0 )
            {
                return null;
            }

            return new GroupMember()
            {
                GroupId = inputGroupMember.GroupId,
                PersonId = inputGroupMember.PersonId,
                Role = inputGroupMember.Role
            };
        }
    }
}