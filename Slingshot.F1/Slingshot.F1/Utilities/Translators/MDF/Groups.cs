using System;
using System.Collections.Generic;
using System.Linq;
using OrcaMDF.Core.MetaData;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace Slingshot.F1.Utilities.Translators
{
    public static class F1Group
    {
        /// <summary>
        /// Translates the volunteer assignment data.
        /// </summary>
        /// <param name="tableData">The table data.</param>
        /// <param name="totalRows">The total rows.</param>
        private void TranslateActivityAssignment( IQueryable<Row> tableData, long totalRows = 0 )
        {
            var lookupContext = new RockContext();
            var excludedGroupTypes = new List<int> { FamilyGroupTypeId, SmallGroupTypeId, GeneralGroupTypeId };
            var importedGroupMembers = lookupContext.GroupMembers.Count( gm => gm.ForeignKey != null && !excludedGroupTypes.Contains( gm.Group.GroupTypeId ) );
            var newGroupMembers = new List<GroupMember>();
            var assignmentTerm = "Member";

            if ( totalRows == 0 )
            {
                totalRows = tableData.Count();
            }

            var completedItems = 0;
            var percentage = ( totalRows - 1 ) / 100 + 1;
            ReportProgress( 0, string.Format( "Verifying participant assignment import ({0:N0} found).", totalRows ) );

            foreach ( var row in tableData.Where( r => r != null ) )
            {
                // get the group and role data
                var ministryId = row["Ministry_ID"] as int?;
                var activityId = row["Activity_ID"] as int?;
                var rlcId = row["RLC_ID"] as int?;
                var individualId = row["Individual_ID"] as int?;
                var assignmentDate = row["AssignmentDateTime"] as DateTime?;
                var membershipStart = row["Activity_Start_Time"] as DateTime?;
                var membershipStop = row["Activity_End_Time"] as DateTime?;
                var activityTimeName = row["Activity_Time_Name"] as string;

                var groupLookupId = rlcId ?? activityId ?? ministryId;
                var assignmentGroup = ImportedGroups.FirstOrDefault( g => g.ForeignKey.Equals( groupLookupId.ToString() ) && !excludedGroupTypes.Contains( g.GroupTypeId ) );
                if ( assignmentGroup != null )
                {
                    var personKeys = GetPersonKeys( individualId, null );
                    if ( personKeys != null )
                    {
                        var isActive = membershipStop.HasValue ? membershipStop > RockDateTime.Now : true;
                        var groupTypeRole = GetGroupTypeRole( lookupContext, assignmentGroup.GroupTypeId, assignmentTerm, string.Format( "{0} imported {1}", activityTimeName, ImportDateTime ), false, 0, true, null, string.Format( "{0} {1}", activityTimeName, assignmentTerm ), ImportPersonAliasId );

                        newGroupMembers.Add( new GroupMember
                        {
                            IsSystem = false,
                            DateTimeAdded = membershipStart,
                            GroupId = assignmentGroup.Id,
                            PersonId = personKeys.PersonId,
                            GroupRoleId = groupTypeRole.Id,
                            CreatedDateTime = assignmentDate,
                            ModifiedDateTime = membershipStop,
                            GroupMemberStatus = isActive != false ? GroupMemberStatus.Active : GroupMemberStatus.Inactive,
                            ForeignKey = string.Format( "Membership imported {0}", ImportDateTime )
                        } );

                        completedItems++;
                    }
                }

                if ( completedItems % percentage < 1 )
                {
                    var percentComplete = completedItems / percentage;
                    ReportProgress( percentComplete, string.Format( "{0:N0} assignments imported ({1}% complete).", completedItems, percentComplete ) );
                }
                else if ( completedItems % ReportingNumber < 1 )
                {
                    SaveGroupMembers( newGroupMembers );
                    ReportPartialProgress();

                    // Reset lists and context
                    lookupContext = new RockContext();
                    newGroupMembers.Clear();
                }
            }

            if ( newGroupMembers.Any() )
            {
                SaveGroupMembers( newGroupMembers );
            }

            lookupContext.Dispose();
            ReportProgress( 100, string.Format( "Finished participant assignment import: {0:N0} assignments imported.", completedItems ) );
        }

        /// <summary>
        /// Translates the activity ministry data.
        /// </summary>
        /// <param name="tableData">The table data.</param>
        /// <param name="totalRows">The total rows.</param>
        private void TranslateActivityMinistry( IQueryable<Row> tableData, long totalRows = 0 )
        {
            var lookupContext = new RockContext();
            var newGroups = new List<Group>();

            const string attendanceTypeName = "Attendance History";
            var groupTypeHistory = ImportedGroupTypes.FirstOrDefault( t => t.ForeignKey.Equals( attendanceTypeName ) );
            if ( groupTypeHistory == null )
            {
                groupTypeHistory = AddGroupType( lookupContext, attendanceTypeName, string.Format( "{0} imported {1}", attendanceTypeName, ImportDateTime ), null,
                    null, GroupTypeCheckinTemplateId, true, true, true, true, typeForeignKey: attendanceTypeName );
                ImportedGroupTypes.Add( groupTypeHistory );
            }

            const string groupsParentName = "Archived Groups";
            var archivedGroupsParent = ImportedGroups.FirstOrDefault( g => g.ForeignKey.Equals( groupsParentName.RemoveWhitespace() ) );
            if ( archivedGroupsParent == null )
            {
                archivedGroupsParent = AddGroup( lookupContext, GeneralGroupTypeId, null, groupsParentName, true, null, ImportDateTime, groupsParentName.RemoveWhitespace(), true, ImportPersonAliasId );
                ImportedGroups.Add( archivedGroupsParent );
            }

            if ( totalRows == 0 )
            {
                totalRows = tableData.Count();
            }

            var completedItems = 0;
            var percentage = ( totalRows - 1 ) / 100 + 1;
            ReportProgress( 0, string.Format( "Verifying ministry import ({0:N0} found, {1:N0} already exist).", totalRows, ImportedGroupTypes.Count ) );

            foreach ( var row in tableData.OrderBy( r => r["Ministry_ID"] as int? ).ThenBy( r => r["Activity_ID"] as int? ) )
            {
                // get the ministry data
                var ministryId = row["Ministry_ID"] as int?;
                var activityId = row["Activity_ID"] as int?;
                var ministryName = row["Ministry_Name"] as string;
                var activityName = row["Activity_Name"] as string;
                var ministryActive = row["Ministry_Active"] as string;
                var activityActive = row["Activity_Active"] as string;
                int? campusId = null;

                if ( ministryId.HasValue && !string.IsNullOrWhiteSpace( ministryName ) && !ministryName.Equals( "Delete", StringComparison.CurrentCultureIgnoreCase ) )
                {
                    // check for a ministry group campus context
                    if ( ministryName.Any( n => ValidDelimiters.Contains( n ) ) )
                    {
                        campusId = campusId ?? GetCampusId( ministryName );
                        if ( campusId.HasValue )
                        {
                            // strip the campus from the ministry name to use for grouptype (use the original name on groups though)
                            ministryName = StripPrefix( ministryName, campusId );
                        }
                    }

                    // add the new grouptype if it doesn't exist
                    var currentGroupType = ImportedGroupTypes.FirstOrDefault( t => t.ForeignKey.Equals( ministryName ) );
                    if ( currentGroupType == null )
                    {
                        // save immediately so we can use the grouptype for a group
                        currentGroupType = AddGroupType( lookupContext, ministryName, string.Format( "{0} imported {1}", ministryName, ImportDateTime ), groupTypeHistory.Id,
                            null, null, true, true, true, true, typeForeignKey: ministryName );
                        ImportedGroupTypes.Add( currentGroupType );
                    }

                    // create a campus level parent for the ministry group
                    var parentGroupId = archivedGroupsParent.Id;
                    if ( campusId.HasValue )
                    {
                        var campus = CampusList.FirstOrDefault( c => c.Id == campusId );
                        var campusGroup = ImportedGroups.FirstOrDefault( g => g.ForeignKey.Equals( campus.ShortCode ) && g.ParentGroupId == parentGroupId );
                        if ( campusGroup == null )
                        {
                            campusGroup = AddGroup( lookupContext, GeneralGroupTypeId, parentGroupId, campus.Name, true, campus.Id, ImportDateTime, campus.ShortCode, true, ImportPersonAliasId );
                            ImportedGroups.Add( campusGroup );
                        }

                        parentGroupId = campusGroup.Id;
                    }

                    // add a ministry group level if it doesn't exist
                    var ministryGroup = ImportedGroups.FirstOrDefault( g => g.ForeignKey.Equals( ministryId.ToString() ) );
                    if ( ministryGroup == null )
                    {
                        // save immediately so we can use the group as a parent
                        ministryGroup = AddGroup( lookupContext, currentGroupType.Id, parentGroupId, ministryName, ministryActive.AsBoolean(), campusId, null, ministryId.ToString(), true, ImportPersonAliasId );
                        ImportedGroups.Add( ministryGroup );
                    }

                    // check for an activity group campus context
                    if ( !string.IsNullOrWhiteSpace( activityName ) && activityName.Any( n => ValidDelimiters.Contains( n ) ) )
                    {
                        campusId = campusId ?? GetCampusId( activityName );
                        if ( campusId.HasValue )
                        {
                            activityName = StripPrefix( activityName, campusId );
                        }
                    }

                    // add the child activity group if it doesn't exist
                    var activityGroup = ImportedGroups.FirstOrDefault( g => g.ForeignKey.Equals( activityId.ToString() ) );
                    if ( activityGroup == null && activityId.HasValue && !string.IsNullOrWhiteSpace( activityName ) && !activityName.Equals( "Delete", StringComparison.CurrentCultureIgnoreCase ) )
                    {
                        // don't save immediately, we'll batch add later
                        activityGroup = AddGroup( lookupContext, currentGroupType.Id, ministryGroup.Id, activityName, activityActive.AsBoolean(), campusId, null, activityId.ToString(), false, ImportPersonAliasId );
                        newGroups.Add( activityGroup );
                    }

                    completedItems++;

                    if ( completedItems % percentage < 1 )
                    {
                        var percentComplete = completedItems / percentage;
                        ReportProgress( percentComplete, string.Format( "{0:N0} ministries imported ({1}% complete).", completedItems, percentComplete ) );
                    }
                    else if ( completedItems % ReportingNumber < 1 )
                    {
                        SaveGroups( newGroups );
                        ReportPartialProgress();
                        ImportedGroups.AddRange( newGroups );

                        // Reset lists and context
                        lookupContext = new RockContext();
                        newGroups.Clear();
                    }
                }
            }

            if ( newGroups.Any() )
            {
                SaveGroups( newGroups );
                ImportedGroups.AddRange( newGroups );
            }

            lookupContext.Dispose();
            ReportProgress( 100, string.Format( "Finished ministry import: {0:N0} ministries imported.", completedItems ) );
        }

        /// <summary>
        /// Translates the activity group data.
        /// </summary>
        /// <param name="tableData">The table data.</param>
        /// <param name="totalRows">The total rows.</param>
        private void TranslateActivityGroup( IQueryable<Row> tableData, long totalRows = 0 )
        {
            var lookupContext = new RockContext();
            var newGroups = new List<Group>();

            if ( totalRows == 0 )
            {
                totalRows = tableData.Count();
            }

            var archivedScheduleName = "Archived Attendance";
            var archivedScheduleId = new ScheduleService( lookupContext ).Queryable()
                .Where( s => s.Name.Equals( archivedScheduleName, StringComparison.CurrentCultureIgnoreCase ) )
                .Select( s => (int?)s.Id ).FirstOrDefault();
            if ( !archivedScheduleId.HasValue )
            {
                var archivedSchedule = AddNamedSchedule( lookupContext, archivedScheduleName, null, null, null,
                    ImportDateTime, archivedScheduleName.RemoveSpecialCharacters(), true, ImportPersonAliasId );
                archivedScheduleId = archivedSchedule.Id;
            }

            var completedItems = 0;
            var percentage = ( totalRows - 1 ) / 100 + 1;
            ReportProgress( 0, string.Format( "Verifying activity import ({0:N0} found, {1:N0} already exist).", totalRows, ImportedGroups.Count ) );

            foreach ( var row in tableData.Where( r => r != null ) )
            {
                // get the group data
                var activityId = row["Activity_ID"] as int?;
                var activityGroupId = row["Activity_Group_ID"] as int?;
                var superGroupId = row["Activity_Super_Group_ID"] as int?;
                var activityGroupName = row["Activity_Group_Name"] as string;
                var superGroupName = row["Activity_Super_Group"] as string;
                var balanceType = row["CheckinBalanceType"] as string;

                // get the top-level activity group
                if ( activityId.HasValue && !activityGroupName.Equals( "Delete", StringComparison.CurrentCultureIgnoreCase ) )
                {
                    var parentGroup = ImportedGroups.FirstOrDefault( g => g.ForeignKey.Equals( activityId.ToString() ) );
                    if ( parentGroup != null )
                    {
                        // add a level for the super group activity if it exists
                        int? parentGroupId = parentGroup.Id;
                        if ( superGroupId.HasValue && !string.IsNullOrWhiteSpace( superGroupName ) )
                        {
                            var superGroup = ImportedGroups.FirstOrDefault( g => g.ForeignKey.Equals( superGroupId.ToString() ) );
                            if ( superGroup == null )
                            {
                                superGroup = AddGroup( lookupContext, parentGroup.GroupTypeId, parentGroupId, superGroupName, parentGroup.IsActive, parentGroup.CampusId, null, superGroupId.ToString(), true, ImportPersonAliasId, archivedScheduleId );
                                ImportedGroups.Add( superGroup );
                                // set parent guid to super group
                                parentGroupId = superGroup.Id;
                            }
                        }

                        // add the child activity group
                        if ( activityGroupId.HasValue && !string.IsNullOrWhiteSpace( activityGroupName ) )
                        {
                            var activityGroup = ImportedGroups.FirstOrDefault( g => g.ForeignKey.Equals( activityGroupId.ToString() ) );
                            if ( activityGroup == null )
                            {
                                // don't save immediately, we'll batch add later
                                activityGroup = AddGroup( null, parentGroup.GroupTypeId, parentGroupId, activityGroupName, parentGroup.IsActive, parentGroup.CampusId, null, activityGroupId.ToString(), false, ImportPersonAliasId, archivedScheduleId );
                                newGroups.Add( activityGroup );
                            }
                        }

                        // #TODO: if Rock ever supports room balancing, check the F1 BalanceType

                        completedItems++;
                        if ( completedItems % percentage < 1 )
                        {
                            var percentComplete = completedItems / percentage;
                            ReportProgress( percentComplete, string.Format( "{0:N0} activities imported ({1}% complete).", completedItems, percentComplete ) );
                        }
                        else if ( completedItems % ReportingNumber < 1 )
                        {
                            SaveGroups( newGroups );
                            ReportPartialProgress();
                            ImportedGroups.AddRange( newGroups );

                            // Reset lists and context
                            lookupContext = new RockContext();
                            newGroups.Clear();
                        }
                    }
                }
            }

            if ( newGroups.Any() )
            {
                SaveGroups( newGroups );
                ImportedGroups.AddRange( newGroups );
            }

            lookupContext.Dispose();
            ReportProgress( 100, string.Format( "Finished activity group import: {0:N0} activities imported.", completedItems ) );
        }

        /// <summary>
        /// Translates the home group membership data.
        /// </summary>
        /// <param name="tableData">The table data.</param>
        /// <param name="totalRows">The total rows.</param>
        private void TranslateGroups( IQueryable<Row> tableData, long totalRows = 0 )
        {
            var lookupContext = new RockContext();
            var newGroupMembers = new List<GroupMember>();
            var importedGroupMembers = lookupContext.GroupMembers.Count( gm => gm.ForeignKey != null && gm.Group.GroupTypeId == GeneralGroupTypeId );
            var groupRoleMember = GroupTypeCache.Get( GeneralGroupTypeId ).Roles.FirstOrDefault( r => r.Name.Equals( "Member" ) );

            var archivedScheduleName = "Archived Attendance";
            var archivedScheduleId = new ScheduleService( lookupContext ).Queryable()
                .Where( s => s.Name.Equals( archivedScheduleName, StringComparison.CurrentCultureIgnoreCase ) )
                .Select( s => (int?)s.Id ).FirstOrDefault();
            if ( !archivedScheduleId.HasValue )
            {
                var archivedSchedule = AddNamedSchedule( lookupContext, archivedScheduleName, null, null, null,
                    ImportDateTime, archivedScheduleName.RemoveSpecialCharacters(), true, ImportPersonAliasId );
                archivedScheduleId = archivedSchedule.Id;
            }

            var groupsParentName = "Archived Groups";
            var archivedGroups = ImportedGroups.FirstOrDefault( g => g.ForeignKey.Equals( groupsParentName.RemoveWhitespace() ) );
            if ( archivedGroups == null )
            {
                archivedGroups = AddGroup( lookupContext, GeneralGroupTypeId, null, groupsParentName, true, null, ImportDateTime, groupsParentName.RemoveWhitespace(), true, ImportPersonAliasId );
                ImportedGroups.Add( archivedGroups );
            }

            if ( totalRows == 0 )
            {
                totalRows = tableData.Count();
            }

            var completedItems = 0;
            var percentage = ( totalRows - 1 ) / 100 + 1;
            ReportProgress( 0, string.Format( "Verifying people groups import ({0:N0} found, {1:N0} already exist).", totalRows, importedGroupMembers ) );

            foreach ( var row in tableData.Where( r => r != null ) )
            {
                var groupId = row["Group_ID"] as int?;
                var groupName = row["Group_Name"] as string;
                var individualId = row["Individual_ID"] as int?;
                var groupCreated = row["Created_Date"] as DateTime?;
                var groupType = row["Group_Type_Name"] as string;

                // require at least a group id and name
                if ( groupId.HasValue && !string.IsNullOrWhiteSpace( groupName ) && !groupName.Equals( "Delete", StringComparison.CurrentCultureIgnoreCase ) )
                {
                    var peopleGroup = ImportedGroups.FirstOrDefault( g => g.ForeignKey.Equals( groupId.ToString() ) );
                    if ( peopleGroup == null )
                    {
                        int? campusId = null;
                        var parentGroupId = archivedGroups.Id;
                        var currentGroupTypeId = GeneralGroupTypeId;
                        if ( !string.IsNullOrWhiteSpace( groupType ) )
                        {
                            // check for a campus on the grouptype
                            campusId = GetCampusId( groupType, true, SearchDirection.Ends );
                            if ( campusId.HasValue )
                            {
                                groupType = StripSuffix( groupType, campusId );
                            }

                            // add the grouptype if it doesn't exist
                            var currentGroupType = ImportedGroupTypes.FirstOrDefault( t => t.ForeignKey.Equals( groupType, StringComparison.CurrentCultureIgnoreCase ) );
                            if ( currentGroupType == null )
                            {
                                // save immediately so we can use the grouptype for a group
                                currentGroupType = AddGroupType( lookupContext, groupType, string.Format( "{0} imported {1}", groupType, ImportDateTime ), null,
                                    null, null, true, true, true, true, typeForeignKey: groupType );
                                ImportedGroupTypes.Add( currentGroupType );
                            }

                            // create a placeholder group for the grouptype if it doesn't exist
                            var groupTypePlaceholder = ImportedGroups.FirstOrDefault( g => g.GroupTypeId == currentGroupType.Id && g.ForeignKey.Equals( groupType.RemoveWhitespace() ) );
                            if ( groupTypePlaceholder == null )
                            {
                                groupTypePlaceholder = AddGroup( lookupContext, currentGroupType.Id, archivedGroups.Id, groupType, true, null, ImportDateTime,
                                    groupType.RemoveWhitespace(), true, ImportPersonAliasId );
                                ImportedGroups.Add( groupTypePlaceholder );
                            }

                            parentGroupId = groupTypePlaceholder.Id;
                            currentGroupTypeId = currentGroupType.Id;
                        }

                        // put the current group under a campus parent if it exists
                        campusId = campusId ?? GetCampusId( groupName );
                        if ( campusId.HasValue )
                        {
                            // create a campus level parent for the home group
                            var campus = CampusList.FirstOrDefault( c => c.Id == campusId );
                            var campusGroup = ImportedGroups.FirstOrDefault( g => g.ForeignKey.Equals( campus.ShortCode ) && g.ParentGroupId == parentGroupId );
                            if ( campusGroup == null )
                            {
                                campusGroup = AddGroup( lookupContext, currentGroupTypeId, parentGroupId, campus.Name, true, campus.Id, ImportDateTime, campus.ShortCode, true, ImportPersonAliasId );
                                ImportedGroups.Add( campusGroup );
                            }

                            parentGroupId = campusGroup.Id;
                        }

                        // add the group, finally
                        peopleGroup = AddGroup( lookupContext, currentGroupTypeId, parentGroupId, groupName, true, campusId, null, groupId.ToString(), true, ImportPersonAliasId, archivedScheduleId );
                        ImportedGroups.Add( peopleGroup );
                    }

                    // add the group member
                    var personKeys = GetPersonKeys( individualId, null );
                    if ( personKeys != null )
                    {
                        newGroupMembers.Add( new GroupMember
                        {
                            IsSystem = false,
                            GroupId = peopleGroup.Id,
                            PersonId = personKeys.PersonId,
                            GroupRoleId = groupRoleMember.Id,
                            GroupMemberStatus = GroupMemberStatus.Active,
                            ForeignKey = string.Format( "Membership imported {0}", ImportDateTime )
                        } );

                        completedItems++;
                    }

                    if ( completedItems % percentage < 1 )
                    {
                        var percentComplete = completedItems / percentage;
                        ReportProgress( percentComplete, string.Format( "{0:N0} group members imported ({1}% complete).", completedItems, percentComplete ) );
                    }
                    else if ( completedItems % ReportingNumber < 1 )
                    {
                        SaveGroupMembers( newGroupMembers );
                        ReportPartialProgress();

                        // Reset lists and context
                        lookupContext = new RockContext();
                        newGroupMembers.Clear();
                    }
                }
            }

            if ( newGroupMembers.Any() )
            {
                SaveGroupMembers( newGroupMembers );
            }

            lookupContext.Dispose();
            ReportProgress( 100, string.Format( "Finished people groups import: {0:N0} members imported.", completedItems ) );
        }

        /// <summary>
        /// Translates the RLC data.
        /// </summary>
        /// <param name="tableData">The table data.</param>
        /// <param name="totalRows">The total rows.</param>
        private void TranslateRLC( IQueryable<Row> tableData, long totalRows = 0 )
        {
            var lookupContext = new RockContext();
            var importedLocations = lookupContext.Locations.AsNoTracking().Where( l => l.ForeignKey != null ).ToList();
            var newGroups = new List<Group>();

            var archivedScheduleName = "Archived Attendance";
            var archivedScheduleId = new ScheduleService( lookupContext ).Queryable()
                .Where( s => s.Name.Equals( archivedScheduleName, StringComparison.CurrentCultureIgnoreCase ) )
                .Select( s => (int?)s.Id ).FirstOrDefault();
            if ( !archivedScheduleId.HasValue )
            {
                var archivedSchedule = AddNamedSchedule( lookupContext, archivedScheduleName, null, null, null,
                    ImportDateTime, archivedScheduleName.RemoveSpecialCharacters(), true, ImportPersonAliasId );
                archivedScheduleId = archivedSchedule.Id;
            }

            if ( totalRows == 0 )
            {
                totalRows = tableData.Count();
            }

            var completedItems = 0;
            var percentage = ( totalRows - 1 ) / 100 + 1;
            ReportProgress( 0, string.Format( "Verifying group location import ({0:N0} found, {1:N0} already exist).", totalRows, importedLocations.Count ) );

            foreach ( var row in tableData.Where( r => r != null ) )
            {
                // get the group and location data
                var rlcId = row["RLC_ID"] as int?;
                var activityId = row["Activity_ID"] as int?;
                var rlcName = row["RLC_Name"] as string;
                var activityGroupId = row["Activity_Group_ID"] as int?;
                var startAgeAttribute = row["Start_Age_Date"] as DateTime?;
                var endAgeAttribute = row["End_Age_Date"] as DateTime?;
                var rlcActive = row["Is_Active"] as Boolean?;
                var roomCode = row["Room_Code"] as string;
                var roomDescription = row["Room_Desc"] as string;
                var roomName = row["Room_Name"] as string;
                var roomCapacity = row["Max_Capacity"] as int?;
                var buildingName = row["Building_Name"] as string;

                // get the parent group
                if ( activityId.HasValue && !rlcName.Equals( "Delete", StringComparison.CurrentCultureIgnoreCase ) )
                {
                    // get the mid-level activity if exists, otherwise the top-level activity
                    var lookupParentId = activityGroupId ?? activityId;

                    // add the child RLC group and locations
                    var parentGroup = ImportedGroups.FirstOrDefault( g => g.ForeignKey.Equals( lookupParentId.ToStringSafe() ) );
                    if ( parentGroup != null )
                    {
                        if ( rlcId.HasValue && !string.IsNullOrWhiteSpace( rlcName ) )
                        {
                            int? parentLocationId = null;
                            Location campusLocation = null;
                            // get the campus from the room, building, or parent
                            var rlcCampusId = GetCampusId( rlcName, false ) ?? GetCampusId( buildingName, false ) ?? parentGroup.CampusId;
                            if ( rlcCampusId.HasValue )
                            {
                                var campus = lookupContext.Campuses.FirstOrDefault( c => c.Id == rlcCampusId );
                                if ( campus != null )
                                {
                                    campusLocation = campus.Location ?? importedLocations.FirstOrDefault( l => l.ForeignKey.Equals( campus.ShortCode ) );
                                    if ( campusLocation == null )
                                    {
                                        campusLocation = AddNamedLocation( lookupContext, null, campus.Name, campus.IsActive, null, ImportDateTime, campus.ShortCode, true, ImportPersonAliasId );
                                        importedLocations.Add( campusLocation );
                                        campus.LocationId = campusLocation.Id;
                                        lookupContext.SaveChanges();
                                    }

                                    parentLocationId = campusLocation.Id;
                                }
                            }

                            // set the location structure
                            Location roomLocation = null;
                            if ( !string.IsNullOrWhiteSpace( roomName ) )
                            {
                                // get the building if it exists
                                Location buildingLocation = null;
                                if ( !string.IsNullOrWhiteSpace( buildingName ) )
                                {
                                    buildingLocation = importedLocations.FirstOrDefault( l => l.ForeignKey.Equals( buildingName ) && l.ParentLocationId == parentLocationId );
                                    if ( buildingLocation == null )
                                    {
                                        buildingLocation = AddNamedLocation( lookupContext, parentLocationId, buildingName, rlcActive, null, ImportDateTime, buildingName, true, ImportPersonAliasId );
                                        importedLocations.Add( buildingLocation );
                                    }

                                    parentLocationId = buildingLocation.Id;
                                }

                                // get the room if it exists in the current structure
                                roomLocation = importedLocations.FirstOrDefault( l => l.ForeignKey.Equals( roomName ) && l.ParentLocationId == parentLocationId );
                                if ( roomLocation == null )
                                {
                                    roomLocation = AddNamedLocation( null, parentLocationId, roomName, rlcActive, roomCapacity, ImportDateTime, roomName, true, ImportPersonAliasId );
                                    importedLocations.Add( roomLocation );
                                }
                            }

                            // create the rlc group
                            var rlcGroup = ImportedGroups.FirstOrDefault( g => g.ForeignKey.Equals( rlcId.ToString() ) );
                            if ( rlcGroup == null )
                            {
                                // don't save immediately, we'll batch add later
                                rlcGroup = AddGroup( null, parentGroup.GroupTypeId, parentGroup.Id, rlcName, rlcActive ?? true, rlcCampusId, null, rlcId.ToString(), false, ImportPersonAliasId, archivedScheduleId );

                                if ( roomLocation != null )
                                {
                                    rlcGroup.GroupLocations.Add( new GroupLocation { LocationId = roomLocation.Id } );
                                }

                                newGroups.Add( rlcGroup );
                            }
                        }

                        completedItems++;
                        if ( completedItems % percentage < 1 )
                        {
                            var percentComplete = completedItems / percentage;
                            ReportProgress( percentComplete, string.Format( "{0:N0} location groups imported ({1}% complete).", completedItems, percentComplete ) );
                        }
                        else if ( completedItems % ReportingNumber < 1 )
                        {
                            SaveGroups( newGroups );
                            ImportedGroups.AddRange( newGroups );
                            ReportPartialProgress();

                            // Reset lists and context
                            lookupContext = new RockContext();
                            newGroups.Clear();
                        }
                    }
                }
            }

            if ( newGroups.Any() )
            {
                SaveGroups( newGroups );
                ImportedGroups.AddRange( newGroups );
            }

            lookupContext.Dispose();
            ReportProgress( 100, string.Format( "Finished group location import: {0:N0} locations imported.", completedItems ) );
        }

        /// <summary>
        /// Translates the volunteer assignment data.
        /// </summary>
        /// <param name="tableData">The table data.</param>
        /// <param name="totalRows">The total rows.</param>
        private void TranslateStaffingAssignment( IQueryable<Row> tableData, long totalRows = 0 )
        {
            var lookupContext = new RockContext();
            var excludedGroupTypes = new List<int> { FamilyGroupTypeId, SmallGroupTypeId, GeneralGroupTypeId };
            var importedGroupMembers = lookupContext.GroupMembers.Count( gm => gm.ForeignKey != null && !excludedGroupTypes.Contains( gm.Group.GroupTypeId ) );
            var skippedGroups = new Dictionary<int, string>();
            var newGroupMembers = new List<GroupMember>();

            if ( totalRows == 0 )
            {
                totalRows = tableData.Count();
            }

            var completedItems = 0;
            var percentage = ( totalRows - 1 ) / 100 + 1;
            ReportProgress( 0, string.Format( "Verifying volunteer assignment import ({0:N0} found, {1:N0} already exist).", totalRows, importedGroupMembers ) );

            foreach ( var row in tableData.Where( r => r != null ) )
            {
                // get the group and role data
                var individualId = row["Individual_ID"] as int?;
                var roleTitle = row["Job_Title"] as string;
                var isActive = row["Is_Active"] as bool?;
                var ministryId = row["Ministry_ID"] as int?;
                var activityId = row["Activity_ID"] as int?;
                var activityGroupId = row["Activity_Group_ID"] as int?;
                var activityTimeName = row["Activity_Time_Name"] as string;
                var rlcId = row["RLC_ID"] as int?;
                var jobId = row["JobID"] as int?;

                var groupLookupId = rlcId ?? activityGroupId ?? activityId ?? ministryId;
                var volunteerGroup = ImportedGroups.FirstOrDefault( g => g.ForeignKey.Equals( groupLookupId.ToString() ) && !excludedGroupTypes.Contains( g.GroupTypeId ) );
                if ( volunteerGroup != null )
                {
                    var personKeys = GetPersonKeys( individualId, null );
                    if ( personKeys != null )
                    {
                        var campusId = GetCampusId( roleTitle );
                        if ( campusId.HasValue )
                        {
                            // strip the campus from the role
                            roleTitle = StripPrefix( roleTitle, campusId );
                        }

                        var isLeaderRole = !string.IsNullOrWhiteSpace( roleTitle ) ? roleTitle.ToStringSafe().EndsWith( "Leader" ) : false;
                        var groupTypeRole = GetGroupTypeRole( lookupContext, volunteerGroup.GroupTypeId, roleTitle, string.Format( "{0} imported {1}", activityTimeName, ImportDateTime ), isLeaderRole, 0, true, null, jobId.ToStringSafe(), ImportPersonAliasId );

                        newGroupMembers.Add( new GroupMember
                        {
                            IsSystem = false,
                            GroupId = volunteerGroup.Id,
                            PersonId = personKeys.PersonId,
                            GroupRoleId = groupTypeRole.Id,
                            GroupMemberStatus = isActive != false ? GroupMemberStatus.Active : GroupMemberStatus.Inactive,
                            ForeignKey = string.Format( "Membership imported {0}", ImportDateTime )
                        } );

                        completedItems++;
                    }
                }
                else
                {
                    skippedGroups.Add( (int)groupLookupId, string.Empty );
                }

                if ( completedItems % percentage < 1 )
                {
                    var percentComplete = completedItems / percentage;
                    ReportProgress( percentComplete, string.Format( "{0:N0} assignments imported ({1}% complete).", completedItems, percentComplete ) );
                }
                else if ( completedItems % ReportingNumber < 1 )
                {
                    SaveGroupMembers( newGroupMembers );
                    ReportPartialProgress();

                    // Reset lists and context
                    lookupContext = new RockContext();
                    newGroupMembers.Clear();
                }
            }

            if ( newGroupMembers.Any() )
            {
                SaveGroupMembers( newGroupMembers );
            }

            if ( skippedGroups.Any() )
            {
                ReportProgress( 0, "The following volunteer groups could not be found and were skipped:" );
                foreach ( var key in skippedGroups )
                {
                    ReportProgress( 0, string.Format( "{0}Assignments for group ID {1}.", key.Value, key ) );
                }
            }

            lookupContext.Dispose();
            ReportProgress( 100, string.Format( "Finished volunteer assignment import: {0:N0} assignments imported.", completedItems ) );
        }

        /// <summary>
        /// Saves the new groups.
        /// </summary>
        /// <param name="newGroups">The new groups.</param>
        private static void SaveGroups( List<Group> newGroups )
        {
            using ( var rockContext = new RockContext() )
            {
                // can't use bulk insert bc Group contains Members
                rockContext.Groups.AddRange( newGroups );
                rockContext.SaveChanges( DisableAuditing );
            }
        }

        /// <summary>
        /// Saves the new group members.
        /// </summary>
        /// <param name="newGroupMembers">The new group members.</param>
        private static void SaveGroupMembers( List<GroupMember> newGroupMembers )
        {
            using ( var rockContext = new RockContext() )
            {
                rockContext.BulkInsert( newGroupMembers );
            }
        }
    }
}