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
    public static class F1Users
    {
        /// <summary>
        /// Translates the users.
        /// </summary>
        /// <param name="tableData">The table data.</param>
        /// <param name="totalRows">The total rows.</param>
        public void TranslateUsers( IQueryable<Row> tableData, long totalRows = 0 )
        {
            var lookupContext = new RockContext();
            var personService = new PersonService( lookupContext );

            var rockAuthenticatedTypeId = EntityTypeCache.Get( typeof( Rock.Security.Authentication.Database ) ).Id;

            var staffGroupId = new GroupService( lookupContext ).GetByGuid( new Guid( Rock.SystemGuid.Group.GROUP_STAFF_MEMBERS ) ).Id;

            var memberGroupRoleId = new GroupTypeRoleService( lookupContext ).Queryable()
                .Where( r => r.Guid.Equals( new Guid( Rock.SystemGuid.GroupRole.GROUPROLE_SECURITY_GROUP_MEMBER ) ) )
                .Select( r => r.Id ).FirstOrDefault();

            var userLoginService = new UserLoginService( lookupContext );
            var importedUserCount = userLoginService.Queryable().Count( u => u.ForeignId != null );

            var newUserLogins = new List<UserLogin>();
            var newStaffMembers = new List<GroupMember>();
            var updatedPersonList = new List<Person>();

            if ( totalRows == 0 )
            {
                totalRows = tableData.Count();
            }

            var completedItems = 0;
            var percentage = ( totalRows - 1 ) / 100 + 1;
            ReportProgress( 0, $"Verifying user import ({totalRows:N0} found, {importedUserCount:N0} already exist)." );

            foreach ( var row in tableData.Where( r => r != null ) )
            {
                var individualId = row["LinkedIndividualID"] as int?;
                var userName = row["UserLogin"] as string;
                var userId = row["UserID"] as int?;
                if ( userId.HasValue && individualId.HasValue && !string.IsNullOrWhiteSpace( userName ) )
                {
                    var personKeys = GetPersonKeys( individualId, null );
                    if ( personKeys != null )
                    {
                        var createdDate = row["UserCreatedDate"] as DateTime?;
                        var userEmail = row["UserEmail"] as string;
                        var userTitle = row["UserTitle"] as string;
                        var userPhone = row["UserPhone"] as string;
                        var isEnabled = row["IsUserEnabled"] as bool?;
                        var isStaff = row["IsStaff"] as bool?;
                        var isActive = isEnabled ?? false;

                        var user = AddUserLogin( lookupContext, rockAuthenticatedTypeId, personKeys.PersonId, userName.Trim(), null, isEnabled, false, createdDate, userId.ToString(), ImportPersonAliasId );
                        if ( user != null )
                        {
                            // track the user's id and person alias for use with notes
                            PortalUsers.AddOrReplace( (int)userId, personKeys.PersonAliasId );

                            if ( isStaff == true )
                            {
                                // add this user to the staff group
                                var staffMember = new GroupMember
                                {
                                    GroupId = staffGroupId,
                                    PersonId = personKeys.PersonId,
                                    GroupRoleId = memberGroupRoleId,
                                    CreatedDateTime = createdDate,
                                    CreatedByPersonAliasId = ImportPersonAliasId,
                                    GroupMemberStatus = isActive ? GroupMemberStatus.Active : GroupMemberStatus.Inactive
                                };

                                newStaffMembers.Add( staffMember );
                            }

                            // set user login email to person's primary email if one isn't set
                            if ( !string.IsNullOrWhiteSpace( userEmail ) && userEmail.IsEmail() )
                            {
                                var person = !updatedPersonList.Any( p => p.Id == personKeys.PersonId )
                                    ? personService.Queryable( includeDeceased: true ).FirstOrDefault( p => p.Id == personKeys.PersonId )
                                    : updatedPersonList.FirstOrDefault( p => p.Id == personKeys.PersonId );

                                if ( person != null && string.IsNullOrWhiteSpace( person.Email ) )
                                {
                                    person.Email = userEmail.Left( 75 );
                                    person.EmailNote = userTitle;
                                    person.IsEmailActive = isEnabled != false;
                                    person.EmailPreference = EmailPreference.EmailAllowed;
                                    lookupContext.SaveChanges( DisableAuditing );
                                    updatedPersonList.Add( person );
                                }
                            }

                            newUserLogins.Add( user );
                            completedItems++;

                            if ( completedItems % percentage < 1 )
                            {
                                var percentComplete = completedItems / percentage;
                                ReportProgress( percentComplete, $"{completedItems:N0} users imported ({percentComplete}% complete)." );
                            }
                            else if ( completedItems % ReportingNumber < 1 )
                            {
                                SaveUsers( newUserLogins, newStaffMembers );

                                updatedPersonList.Clear();
                                newUserLogins.Clear();
                                newStaffMembers.Clear();
                                ReportPartialProgress();
                            }
                        }
                    }
                }
                else
                {
                    LogException( "User Import", $"User: {userId} - UserName: {userName} is not linked to a person or already exists." );
                }
            }

            if ( newUserLogins.Any() )
            {
                SaveUsers( newUserLogins, newStaffMembers );
            }

            ReportProgress( 100, $"Finished user import: {completedItems:N0} users imported." );
        }

        /// <summary>
        /// Saves the new user logins.
        /// </summary>
        /// <param name="newUserLogins">The new user logins.</param>
        /// <param name="newStaffMembers">The new staff members.</param>
        private static void SaveUsers( List<UserLogin> newUserLogins, List<GroupMember> newStaffMembers )
        {
            using ( var rockContext = new RockContext() )
            {
                rockContext.BulkInsert( newUserLogins );
                rockContext.BulkInsert( newStaffMembers );
            }
        }
    }
}