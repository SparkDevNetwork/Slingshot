using Slingshot.Core.Model;
using Slingshot.PCO.Models.DTO;

namespace Slingshot.PCO.Utilities.Translators
{
    public static class PCOImportTeam
    {
        // This ensures that group ids for teams start at one billion, to avoid conflicting with other groups.  If there is a group with an
        // id over this value, the service/team export will not be run.
        public const int TEAM_ID_BASE = 999999999;

        public static Group Translate( TeamDTO inputTeam )
        {
            if ( inputTeam.Id <= 0 )
            {
                return null;
            }

            return new Group()
            {
                Id = inputTeam.Id + TEAM_ID_BASE,
                IsActive = !inputTeam.ArchivedAt.HasValue,
                Description = $"Service: {inputTeam.ServiceType.Name} - Team: {inputTeam.Name}",
                Name = inputTeam.Name,
                GroupTypeId = inputTeam.ServiceType.Id + PCOImportServiceType.SERVICE_TYPE_ID_BASE,
                IsPublic = !( inputTeam.SecureTeam == true )
            };
        }
    }
}
