using Slingshot.Core.Model;
using Slingshot.PCO.Models.DTO;

namespace Slingshot.PCO.Utilities.Translators
{
    public static class PCOImportTeamMember
    {
        public static GroupMember Translate( TeamMemberDTO inputTeamMember )
        {
            if ( inputTeamMember.Id <= 0 )
            {
                return null;
            }

            return new GroupMember()
            {
                GroupId = inputTeamMember.TeamId + PCOImportTeam.TEAM_ID_BASE,
                PersonId = inputTeamMember.PersonId,
                Role = inputTeamMember.TeamPosition
            };
        }
    }
}