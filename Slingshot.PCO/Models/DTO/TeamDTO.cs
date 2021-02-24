using Slingshot.PCO.Models.ApiModels;
using Slingshot.PCO.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Slingshot.PCO.Models.DTO
{
    public class TeamDTO
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int? Sequence { get; set; }

        public string ScheduleTo { get; set; }

        public string DefaultStatus { get; set; }

        public bool? DefaultPrepareNotifications { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public DateTime? ArchivedAt { get; set; }

        public bool? AssignedDirectly { get; set; }

        public bool? SecureTeam { get; set; }

        public string LastPlanFrom { get; set; }

        public string StageColor { get; set; }

        public string StageVariant { get; set; }

        public ServiceTypeDTO ServiceType { get; set; }

        public List<TeamPositionDTO> TeamPositions { get; set; }

        public List<TeamMemberDTO> TeamMembers { get; set; }

        public TeamDTO( DataItem data, Dictionary<string, DataItem> includedItems, List<ServiceTypeDTO> serviceTypes )
        {
            TeamPositions = new List<TeamPositionDTO>();
            TeamMembers = new List<TeamMemberDTO>();

            Id = data.Id;
            Name = data.Item.name;
            Sequence = data.Item.sequence;
            ScheduleTo = data.Item.schedule_to;
            DefaultStatus = data.Item.default_status;
            DefaultPrepareNotifications = data.Item.default_prepare_notifications;
            CreatedAt = data.Item.created_at;
            UpdatedAt = data.Item.updated_at;
            ArchivedAt = data.Item.archived_at;
            AssignedDirectly = data.Item.assigned_directly;
            SecureTeam = data.Item.secure_team;
            LastPlanFrom = data.Item.last_plan_from;
            StageColor = data.Item.stage_color;
            StageVariant = data.Item.stage_variant;

            SetServiceType( data, serviceTypes );
            SetTeamPositions( data, includedItems );
            SetTeamMembers( data, includedItems );
        }

        private void SetServiceType( DataItem data, List<ServiceTypeDTO> serviceTypes )
        {
            if ( data.Relationships == null || data.Relationships.ServiceType == null )
            {
                return;
            }

            foreach ( var relationship in data.Relationships.ServiceType.Data )
            {
                foreach ( var serviceType in serviceTypes )
                {
                    if ( serviceType.Id == relationship.Id )
                    {
                        ServiceType = serviceType;
                    }
                }
            }
        }

        private void SetTeamPositions( DataItem data, Dictionary<string, DataItem> included )
        {
            foreach ( string itemKey in included.Keys )
            {
                if ( itemKey.StartsWith( "TeamPosition:") )
                {
                    var importPosition = included[itemKey];
                    if ( importPosition.Relationships == null || importPosition.Relationships.Team == null )
                    {
                        continue;
                    }

                    foreach ( var relationship in importPosition.Relationships.Team.Data )
                    {
                        if ( relationship.Id == data.Id )
                        {
                            TeamPositions.Add( new TeamPositionDTO( importPosition, data.Id ) );
                        }
                    }
                }
            }
        }

        private void SetTeamMembers( DataItem data, Dictionary<string, DataItem> included )
        {
            foreach ( string itemKey in included.Keys )
            {
                if ( itemKey.StartsWith( "PersonTeamPositionAssignment:" ) )
                {
                    var importTeamMember = included[itemKey];
                    if ( importTeamMember.Relationships == null || importTeamMember.Relationships.TeamPosition == null )
                    {
                        continue;
                    }

                    foreach ( var relationship in importTeamMember.Relationships.TeamPosition.Data )
                    {
                        var teamPosition = TeamPositions.Where( p => p.Id == relationship.Id ).FirstOrDefault();
                        if ( teamPosition != null )
                        {
                            TeamMembers.Add( new TeamMemberDTO( importTeamMember, teamPosition.TeamId, teamPosition.Name ) );
                        }
                    }
                }
            }
        }
    }
}
