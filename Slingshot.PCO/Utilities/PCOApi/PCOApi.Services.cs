using Slingshot.Core.Model;
using Slingshot.Core.Utilities;
using Slingshot.PCO.Models.DTO;
using Slingshot.PCO.Utilities.Translators;
using System;
using System.Collections.Generic;

namespace Slingshot.PCO.Utilities
{
    /// <summary>
    /// PCO API Class - Service and Teams data export methods.
    /// </summary>
    public static partial class PCOApi
    {
        /// <summary>
        /// Api Endpoint Declarations.
        /// </summary>
        internal static partial class ApiEndpoint
        {
            internal const string API_SERVICETYPES = "/services/v2/service_types";
            internal const string API_TEAMS = "/services/v2/teams";
        }

        /// <summary>
        /// Test access to the people API.
        /// </summary>
        /// <returns></returns>
        public static bool TestServiceAccess()
        {
            var initalErrorValue = PCOApi.ErrorMessage;

            var response = ApiGet( ApiEndpoint.API_TEAMS );

            PCOApi.ErrorMessage = initalErrorValue;

            return ( response != string.Empty );
        }


        #region ExportServices() and Related Methods


        /// <summary>
        /// Exports the services.
        /// </summary>
        public static void ExportServices()
        {
            try
            {
                // Export each service type.
                var serviceTypes = GetServiceTypes();
                foreach ( var serviceType in serviceTypes )
                {
                    var exportServiceType = PCOImportServiceType.Translate( serviceType );
                    ImportPackage.WriteToPackage( exportServiceType );
                }

                // Export each team.
                var teams = GetTeams( serviceTypes );
                foreach ( var team in teams )
                {
                    var importTeam = PCOImportTeam.Translate( team );
                    if ( importTeam != null )
                    {
                        ImportPackage.WriteToPackage( importTeam );

                        // Export TeamMembers.
                        ExportTeamMembers( team );
                    }
                }
            }
            catch ( Exception ex )
            {
                ErrorMessage = ex.Message;
            }
        }

        private static void ExportTeamMembers( TeamDTO exportTeam )
        {
            foreach ( var teamMember in exportTeam.TeamMembers )
            {
                var importTeamMember = PCOImportTeamMember.Translate( teamMember );
                if ( importTeamMember != null )
                {
                    ImportPackage.WriteToPackage( importTeamMember );
                }
            }
        }

        private static List<ServiceTypeDTO> GetServiceTypes()
        {
            var serviceTypes = new List<ServiceTypeDTO>();

            var apiOptions = new Dictionary<string, string>
            {
                { "order", "name" },
                { "per_page", "100" }
            };

            var serviceTypeQuery = GetAPIQuery( ApiEndpoint.API_SERVICETYPES, apiOptions );

            if ( serviceTypeQuery == null )
            {
                return serviceTypes;
            }

            foreach ( var item in serviceTypeQuery.Items )
            {
                var serviceType = new ServiceTypeDTO( item );
                if ( serviceType != null )
                {
                    serviceTypes.Add( serviceType );
                }
            }

            return serviceTypes;
        }

        private static List<TeamDTO> GetTeams( List<ServiceTypeDTO> serviceTypes )
        {
            var teams = new List<TeamDTO>();

            var apiOptions = new Dictionary<string, string>
            {
                { "include", "person_team_position_assignments,team_positions" },
                { "per_page", "100" }
            };

            var teamQuery = GetAPIQuery( ApiEndpoint.API_TEAMS, apiOptions );

            if ( teamQuery == null )
            {
                return teams;
            }

            foreach ( var item in teamQuery.Items )
            {
                var team = new TeamDTO( item, teamQuery.IncludedItems, serviceTypes );
                if ( team != null )
                {
                    teams.Add( team );
                }
            }

            return teams;
        }

        #endregion ExportServices() and Related Methods
    }
}
