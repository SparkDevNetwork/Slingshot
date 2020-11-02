using Slingshot.Core.Model;
using Slingshot.Core.Utilities;
using Slingshot.PCO.Models.DTO;
using Slingshot.PCO.Utilities.Translators;
using System;
using System.Collections.Generic;

namespace Slingshot.PCO.Utilities
{
    /// <summary>
    /// PCO API Class - Group data export methods.
    /// </summary>
    public static partial class PCOApi
    {
        /// <summary>
        /// Api Endpoint Declarations.
        /// </summary>
        internal static partial class ApiEndpoint
        {
            internal const string API_GROUPTYPES = "/groups/v2/group_types";
            internal const string API_GROUPS = "/groups/v2/group_types/{groupTypeId}/groups";
            internal const string API_GROUPMEMBERS = "/groups/v2/groups/{groupId}/memberships";
            internal const string API_GROUPTAGGROUPS = "/groups/v2/tag_groups";
            internal const string API_GROUPTAGS = "/groups/v2/groups/{groupId}/tags";
            internal const string API_GROUPLOCATIONS = "/groups/v2/groups/{groupId}/location";
            internal const string API_GROUPEVENTS = "/groups/v2/groups/{groupId}/events";
            internal const string API_GROUPATTENDANCE = "/groups/v2/events/{eventId}/attendances";
        }

        #region ExportGroups() and Related Methods

        /// <summary>
        /// Gets the group types.
        /// </summary>
        /// <returns></returns>
        public static List<GroupTypeDTO> GetGroupTypes()
        {
            var groupTypes = new List<GroupTypeDTO>();

            var apiOptions = new Dictionary<string, string>
            {
                { "order", "name" },
                { "per_page", "100" }
            };

            var groupTypeQuery = GetAPIQuery( ApiEndpoint.API_GROUPTYPES, apiOptions );

            if ( groupTypeQuery == null )
            {
                return groupTypes;
            }

            foreach ( var item in groupTypeQuery.Items )
            {
                var groupType = new GroupTypeDTO( item );
                if ( groupType != null )
                {
                    groupTypes.Add( groupType );
                }
            }

            return groupTypes;
        }

        /// <summary>
        /// Exports the groups.
        /// </summary>
        /// <param name="exportGroupTypes">The list of <see cref="GroupTypeDTO"/>s to export.</param>
        /// <param name="exportAttendance">[true] if attendance records should be exported.</param>
        public static void ExportGroups( List<GroupTypeDTO> exportGroupTypes, bool exportAttendance )
        {
            try
            {
                // Create Group Attributes from Tag Groups.
                var tagGroups = GetGroupTagGroups();
                foreach ( var tagGroup in tagGroups )
                {
                    var groupAttribute = PCOImportGroupAttribute.Translate( tagGroup );
                    ImportPackage.WriteToPackage( groupAttribute );
                }

                var groupTypes = GetGroupTypes();

                // The special "unique" group type in PCO needs to have an integer value assigned, so we will find the highest number currently used and add 1.
                int maxGroupType = 0;
                foreach ( var groupType in exportGroupTypes )
                {
                    maxGroupType = ( groupType.Id > maxGroupType ) ? groupType.Id : maxGroupType;
                }

                // Export each group type.
                foreach ( var groupType in exportGroupTypes )
                {
                    bool isUnique = false;
                    if ( groupType.Id == -1 )
                    {
                        // "Unique" group type.
                        groupType.Id = maxGroupType += 1;
                        isUnique = true;
                    }

                    ExportGroupType( groupType, tagGroups, exportAttendance, isUnique );
                }
            }
            catch ( Exception ex )
            {
                ErrorMessage = ex.Message;
            }
        }

        private static void ExportGroupType( GroupTypeDTO groupType, List<TagGroupDTO> tagGroups, bool exportAttendance, bool isUnique )
        {
            // Write the GroupType.
            var exportGroupType = PCOImportGroupType.Translate( groupType );
            ImportPackage.WriteToPackage( exportGroupType );

            // Iterate over each Group in the GroupType.
            var groups = GetGroups( groupType, isUnique );
            foreach ( var group in groups )
            {
                var importGroup = PCOImportGroup.Translate( group );
                if ( importGroup != null )
                {
                    ImportPackage.WriteToPackage( importGroup );

                    // Export GroupMembers.
                    ExportGroupMembers( importGroup );

                    // Export Group AttributeValues from Tags.
                    ExportGroupTags( importGroup, tagGroups );

                    // Export GroupAddresses.
                    if ( group.HasLocation )
                    {
                        ExportGroupLocations( importGroup );
                    }

                    // Export Attendance.
                    if ( exportAttendance )
                    {
                        ExportGroupAttendance( importGroup );
                    }
                }
            }
        }

        private static void ExportGroupMembers( Group importGroup )
        {
            var groupMembers = GetGroupMembers( importGroup.Id );
            foreach ( var groupMember in groupMembers )
            {
                var importGroupMember = PCOImportGroupMember.Translate( groupMember );
                if ( importGroupMember != null )
                {
                    ImportPackage.WriteToPackage( importGroupMember );
                }
            }
        }

        private static void ExportGroupTags( Group importGroup, List<TagGroupDTO> tagGroups )
        {
            // Each tag becomes a comma-separated value in an attribute keyed to the tag group.
            var groupTags = GetGroupTags( importGroup.Id, tagGroups );
            var groupedGroupTags = new Dictionary<string, List<TagDTO>>();
            foreach( var groupTag in groupTags )
            {
                string attributeKey = groupTag.TagGroup.GroupAttributeKey;
                if ( groupedGroupTags.ContainsKey( attributeKey ) )
                {
                    groupedGroupTags[attributeKey].Add( groupTag );
                }
                else
                {
                    groupedGroupTags.Add( attributeKey, new List<TagDTO>() { groupTag } );
                }
            }
            
            // Write the Group AttributeValues.
            foreach ( var attributeKey in groupedGroupTags.Keys )
            {
                var values = new List<string>();
                var groupedTags = groupedGroupTags[attributeKey];

                // Combine all of the tag values.
                foreach ( var groupTag in groupedTags )
                {
                    values.Add( groupTag.GroupAttributeValue );
                }

                ImportPackage.WriteToPackage(
                    new GroupAttributeValue()
                    {
                        AttributeKey = attributeKey,
                        AttributeValue = values.ToDelimited(),
                        GroupId = importGroup.Id
                    } );
            }
        }

        private static string ToDelimited( this List<string> input )
        {
            string output = string.Empty;
            foreach ( var value in input )
            {
                if ( output != string.Empty )
                {
                    output += ",";
                }

                output += value;
            }

            return output;
        }

        private static void ExportGroupLocations( Group importGroup )
        {
            var groupLocations = GetGroupLocations( importGroup.Id );
            foreach ( var groupLocation in groupLocations )
            {
                var groupAddress = PCOImportGroupAddress.Translate( groupLocation, importGroup.Id );
                if ( groupAddress != null )
                {
                    ImportPackage.WriteToPackage( groupAddress );
                }
            }
        }

        private static void ExportGroupAttendance( Group importGroup )
        {
            var groupEvents = GetGroupEvents( importGroup.Id );
            foreach ( var groupEvent in groupEvents )
            {
                var groupAttendances = GetGroupAttendance( groupEvent, importGroup.Id );
                foreach ( var groupAttendance in groupAttendances )
                {
                    var importAttendance = PCOImportGroupAttendance.Translate( groupAttendance, importGroup.Id );
                    if ( importAttendance != null )
                    {
                        ImportPackage.WriteToPackage( importAttendance );
                    }
                }

            }
        }

        private static List<GroupDTO> GetGroups( GroupTypeDTO groupType, bool isUnique )
        {
            var groups = new List<GroupDTO>();

            var apiOptions = new Dictionary<string, string>
            {
                { "per_page", "100" }
            };

            string groupTypeId = groupType.Id.ToString();
            if ( isUnique )
            {
                groupTypeId = "unique";
            }

            string groupEndPoint = ApiEndpoint.API_GROUPS.Replace( "{groupTypeId}", groupTypeId );
            var groupQuery = GetAPIQuery( groupEndPoint, apiOptions );

            if ( groupQuery == null )
            {
                return groups;
            }

            foreach ( var item in groupQuery.Items )
            {
                var group = new GroupDTO( item, groupType );
                if ( group != null )
                {
                    groups.Add( group );
                }
            }

            return groups;
        }

        private static List<GroupMemberDTO> GetGroupMembers( int groupId )
        {
            var groupMembers = new List<GroupMemberDTO>();

            var apiOptions = new Dictionary<string, string>
            {
                { "per_page", "100" }
            };

            string groupMemberEndpoint = ApiEndpoint.API_GROUPMEMBERS.Replace( "{groupId}", groupId.ToString() );
            var groupMemberQuery = GetAPIQuery( groupMemberEndpoint, apiOptions );

            if ( groupMemberQuery == null )
            {
                return groupMembers;
            }

            foreach ( var item in groupMemberQuery.Items )
            {
                var groupMember = new GroupMemberDTO( item, groupId );
                if ( groupMember != null )
                {
                    groupMembers.Add( groupMember );
                }
            }

            return groupMembers;
        }

        private static List<TagGroupDTO> GetGroupTagGroups()
        {
            var tagGroups = new List<TagGroupDTO>();

            var apiOptions = new Dictionary<string, string>
            {
                { "per_page", "100" }
            };

            var tagGroupQuery = GetAPIQuery( ApiEndpoint.API_GROUPTAGGROUPS, apiOptions );

            if ( tagGroupQuery == null )
            {
                return tagGroups;
            }

            foreach ( var item in tagGroupQuery.Items )
            {
                var groupMember = new TagGroupDTO( item );
                if ( groupMember != null )
                {
                    tagGroups.Add( groupMember );
                }
            }

            return tagGroups;
        }

        private static List<TagDTO> GetGroupTags( int groupId, List<TagGroupDTO> tagGroups )
        {
            var groupTags = new List<TagDTO>();

            var apiOptions = new Dictionary<string, string>
            {
                { "per_page", "100" }
            };

            string groupMemberEndpoint = ApiEndpoint.API_GROUPTAGS.Replace( "{groupId}", groupId.ToString() );
            var groupTagQuery = GetAPIQuery( groupMemberEndpoint, apiOptions );

            if ( groupTagQuery == null )
            {
                return groupTags;
            }

            foreach ( var item in groupTagQuery.Items )
            {
                var groupTag = new TagDTO( item, tagGroups );
                if ( groupTag != null )
                {
                    groupTags.Add( groupTag );
                }
            }

            return groupTags;
        }

        private static List<LocationDTO> GetGroupLocations( int groupId )
        {
            var groupLocations = new List<LocationDTO>();

            var apiOptions = new Dictionary<string, string>
            {
                { "per_page", "100" }
            };

            string locationEndpoint = ApiEndpoint.API_GROUPLOCATIONS.Replace( "{groupId}", groupId.ToString() );

            /* This request will ignore API errors because the locations endpoint sometimes returns 403 forbidden
             * errors, seemingly at random (it could be a permissions issue in PCO, but the cause is unconfirmed). */

            var groupLocationQuery = GetAPIQuery( locationEndpoint, apiOptions, null, null, true );

            if ( groupLocationQuery == null )
            {
                return groupLocations;
            }

            foreach ( var item in groupLocationQuery.Items )
            {
                var location = new LocationDTO( item );
                if ( location != null && location.IsValid )
                {
                    groupLocations.Add( location );
                }
            }

            return groupLocations;
        }

        private static List<GroupEventDTO> GetGroupEvents( int groupId )
        {
            var groupEvents = new List<GroupEventDTO>();

            var apiOptions = new Dictionary<string, string>
            {
                { "per_page", "100" }
            };

            string locationEndpoint = ApiEndpoint.API_GROUPEVENTS.Replace( "{groupId}", groupId.ToString() );
            var groupEventQuery = GetAPIQuery( locationEndpoint, apiOptions );

            if ( groupEventQuery == null )
            {
                return groupEvents;
            }

            foreach ( var item in groupEventQuery.Items )
            {
                var groupEvent = new GroupEventDTO( item );
                if ( groupEvent != null )
                {
                    groupEvents.Add( groupEvent );
                }
            }

            return groupEvents;
        }

        private static List<AttendanceDTO> GetGroupAttendance( GroupEventDTO groupEvent, int groupId )
        {
            var groupAttendance = new List<AttendanceDTO>();

            var apiOptions = new Dictionary<string, string>
            {
                { "per_page", "100" }
            };

            string locationEndpoint = ApiEndpoint.API_GROUPATTENDANCE.Replace( "{eventId}", groupEvent.Id.ToString() );
            var groupAttendanceQuery = GetAPIQuery( locationEndpoint, apiOptions );

            if ( groupAttendanceQuery == null )
            {
                return groupAttendance;
            }

            foreach ( var item in groupAttendanceQuery.Items )
            {
                var attendance = new AttendanceDTO( item, groupEvent, groupId );
                if ( attendance != null )
                {
                    groupAttendance.Add( attendance );
                }
            }

            return groupAttendance;
        }

        #endregion ExportGroups() and Related Methods
    }
}
