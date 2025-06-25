using Slingshot.Core.Model;
using Slingshot.Core.Utilities;
using Slingshot.F1.Utilities;
using Slingshot.F1.Utilities.SQL.DTO;
using Slingshot.F1.Utilities.Translators.SQL;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;

namespace Slingshot.F1.Utilities
{
    public partial class F1Sql : F1Translator
    {

        #region Try/Catch Delegate Wrapper

        /// <summary>
        /// This method can be called to execute an action with standard error handling, eliminating the need to wrap the logic of the method in a 
        /// try/catch block.
        /// </summary>
        /// <param name="method">The method to execute.</param>
        private void ExecuteMethodWithErrorHandling( Action method )
        {
            try
            {
                method();
            }
            catch ( Exception ex )
            {
                ErrorMessage = ex.Message;
            }
        }

        #endregion Try/Catch Delegate Wrapper

        #region Public Methods

        /*
         * 8/12/20 - Shaun
         * 
         * These publicly exposed methods are just wrappers for the "Internal" methods found below.
         * 
         * */

        /// <summary>
        /// Exports the individuals.
        /// </summary>
        /// <param name="modifiedSince">The modified since.</param>
        /// <param name="peoplePerPage">The people per page.</param>
        public override void ExportIndividuals( DateTime modifiedSince, int peoplePerPage = 500 )
        {
            ExecuteMethodWithErrorHandling( delegate () { ExportIndividuals_Internal( modifiedSince, peoplePerPage ); } );
        }

        /// <summary>
        /// Exports the companies.
        /// </summary>
        public override void ExportCompanies()
        {
            ExecuteMethodWithErrorHandling( delegate () { ExportCompanies_Internal(); } );
        }

        /// <summary>
        /// Export the people and household notes
        /// </summary>
        public override void ExportNotes()
        {
            ExecuteMethodWithErrorHandling( delegate () { ExportNotes_Internal(); } );
        }

        /// <summary>
        /// Exports the accounts.
        /// </summary>
        public override void ExportFinancialAccounts()
        {
            ExecuteMethodWithErrorHandling( delegate () { ExportFinancialAccounts_Internal(); } );
        }

        /// <summary>
        /// Exports the pledges.
        /// </summary>
        public override void ExportFinancialPledges()
        {
            ExecuteMethodWithErrorHandling( delegate () { ExportFinancialPledges_Internal(); } );
        }

        /// <summary>
        /// Exports the batches.
        /// </summary>
        /// <param name="modifiedSince">The modified since.</param>
        public override void ExportFinancialBatches( DateTime modifiedSince )
        {
            ExecuteMethodWithErrorHandling( delegate () { ExportFinancialBatches_Internal( modifiedSince ); } );
        }

        /// <summary>
        /// Exports the contributions.
        /// </summary>
        /// <param name="modifiedSince">The modified since.</param>
        /// <param name="exportContribImages">Indicates whether images should be exported.  (WARNING:  Not implemented.)</param>
        public override void ExportContributions( DateTime modifiedSince, bool exportContribImages )
        {
            ExecuteMethodWithErrorHandling( delegate () { ExportContributions_Internal( modifiedSince, exportContribImages ); } );
        }

        /// <summary>
        /// Exports the groups.
        /// </summary>
        /// <param name="selectedGroupTypes">The selected group types.</param>
        public override void ExportGroups( List<int> selectedGroupTypes )
        {
            ExecuteMethodWithErrorHandling( delegate () { ExportGroups_Internal( selectedGroupTypes ); } );
        }

        /// <summary>
        /// Exports the contributions.
        /// </summary>
        /// <param name="modifiedSince">The modified since.</param>
        public override void ExportAttendance( DateTime modifiedSince )
        {
            ExecuteMethodWithErrorHandling( delegate () { ExportAttendance_Internal( modifiedSince ); } );
        }

        #endregion Public Methods

        #region Internal Methods

        /// <summary>
        /// Internal method for ExportIndividuals().
        /// </summary>
        /// <param name="modifiedSince">The modified since.</param>
        /// <param name="peoplePerPage">The people per page.</param>
        private void ExportIndividuals_Internal( DateTime modifiedSince, int peoplePerPage = 500 )
        {
            // write out the person attributes
            WritePersonAttributes();

            // export people (and their addresses).
            var dtPeople = _db.Table( "Individual_Household" ).Data;
            DataView dvCommunication = _db.Table( "Communication" ).Data.DefaultView;
            dvCommunication.Sort = "LastUpdatedDate"; // Communications must be sorted by LastUpdatedDate to ensure we get the right values.
            var dtCommunications = dvCommunication.ToTable();

            // export people

            var dtOtherCommunications = dtCommunications
                .Select( "Communication_Type NOT IN ('Mobile', 'Email') AND Communication_Type NOT LIKE '%Phone%' AND Individual_ID IS NOT NULL" );

            var dtCommunicationAttributeValueFilter =
                from a in dtOtherCommunications
                group a by new
                {
                    CommunicationType = a.Field<string>( "communication_type" ),
                    IndividualId = a.Field<int>( "individual_id" )
                }
                into g
                select ( from t2 in g select t2.Field<int>( "Communication_Id" ) ).Max();

            var dtCommunicationAttributeValues = ( from values in dtOtherCommunications
                                                   join filter in dtCommunicationAttributeValueFilter
                                                   on values.Field<int>( "Communication_Id" ) equals filter
                                                   select values ).ToList().CopyToDataTable_Safe( dtCommunications );



            var dtRawRequirementValues = _db.Table( "Requirement" ).Data.AsEnumerable();
            var dtRequirementValueFilter =
                from a in dtRawRequirementValues
                group a by new
                {
                    RequirementName = a.Field<string>( "requirement_name" ),
                    IndividualId = a.Field<int>( "individual_id" )
                }
                into g
                select ( from t2 in g select t2.Field<int>( "Individual_Requirement_ID" ) ).Max();
                //new {
                //    CommunicationId = ( from t2 in g
                //                        select t2.Field<int>( "Communication_Id" ) ).Max()
                //};

            var dtRequirementValues = ( from values in dtRawRequirementValues
                                        join filter in dtRequirementValueFilter
                                        on values.Field<int>( "Individual_Requirement_ID" ) equals filter
                                        select values ).ToList().CopyToDataTable_Safe( _db.Table( "Requirement" ).Data );

            // Make Requirement Names match case, as they may be in mixed cases in the F1 database.
            foreach ( DataRow row in dtRequirementValues.Rows )
            {
                string requirementName = row["requirement_name"].ToString();
                if ( _RequirementNames.ContainsKey( requirementName ) )
                {
                    if ( _RequirementNames[requirementName] != requirementName )
                    {
                        row["requirement_name"] = _RequirementNames[requirementName];
                    }
                }
            }

            var headOfHouseHolds = GetHeadOfHouseholdMap( dtPeople );

            //Split communications into basic elements to make subsequent queries faster.
            var individualEmailRows = dtCommunications.Select( "individual_id IS NOT NULL AND communication_type = 'Email'" );
            var dtCommunications_IndividualEmails = individualEmailRows.CopyToDataTable_Safe( dtCommunications );

            var infellowshipLoginRows = dtCommunications.Select( "individual_id IS NOT NULL AND communication_type = 'Infellowship Login'" );
            var dtCommunications_InfellowshipLogins = infellowshipLoginRows.CopyToDataTable_Safe( dtCommunications );

            var householdEmails = dtCommunications.Select( "individual_id IS NULL AND household_id IS NOT NULL AND communication_type = 'Email'" );
            var dtCommunications_HouseholdEmails = householdEmails.CopyToDataTable_Safe( dtCommunications );

            foreach ( DataRow row in dtPeople.Rows )
            {
                var importPerson = F1Person.Translate( row, dtCommunications_IndividualEmails, dtCommunications_InfellowshipLogins, dtCommunications_HouseholdEmails, headOfHouseHolds, dtRequirementValues, dtCommunicationAttributeValues );

                if ( importPerson != null )
                {
                    ImportPackage.WriteToPackage( importPerson );
                }
            }

            // export people addresses
            var dtAddress = _db.Table( "Household_Address" ).Data;
            foreach ( DataRow row in dtAddress.Rows )
            {
                var importAddress = F1PersonAddress.Translate( row, dtPeople );

                if ( importAddress != null )
                {
                    ImportPackage.WriteToPackage( importAddress );
                }
            }

            // export people attributes
            var individualAttributes =
                from a in _db.Table("Attribute").Data.AsEnumerable()
                group a by new
                {
                    IndividualId = a.Field<int>( "Individual_Id" ),
                    AttributeId = a.Field<int>( "Attribute_Id" ),
                    AttributeName = a.Field<string>( "Attribute_Name" )
                }
                into g
                select new
                {
                    g.Key.IndividualId,
                    g.Key.AttributeId,
                    g.Key.AttributeName,
                    IndividualAttributeId = ( from t2 in g
                                              select t2.Field<int>( "Individual_attribute_Id" ) )
                                              .Max()
                };

            var dtAttributeValues = ( from table1 in _db.Table( "Attribute" ).Data.AsEnumerable()
                                      join table2 in individualAttributes
                                      on table1.Field<int>( "Individual_attribute_Id" ) equals table2.IndividualAttributeId
                                      select table1 ).ToList().CopyToDataTable_Safe( _db.Table( "Attribute" ).Data );

            foreach ( DataRow row in dtAttributeValues.Rows )
            {
                var importAttributes = F1PersonAttributeValue.Translate( row );

                if ( importAttributes != null )
                {
                    foreach ( PersonAttributeValue value in importAttributes )
                    {
                        ImportPackage.WriteToPackage( value );
                    }
                }
            }

            // export Phone Numbers
            var phoneNumbers = dtCommunications.Select( "(individual_id IS NOT NULL OR household_id IS NOT NULL) AND (communication_type = 'Mobile' OR communication_type like '%Phone%')" );

            foreach ( DataRow row in phoneNumbers )
            {
                //Household phone numbers may be assigned to multiple person records (i.e., "Head" and "Spouse").
                var personIds = F1PersonPhone.GetPhonePersonIds( row, dtPeople );
                foreach ( int personId in personIds )
                {
                    var importNumber = F1PersonPhone.Translate( row, personId );
                    if ( importNumber != null )
                    {
                        ImportPackage.WriteToPackage( importNumber );
                    }
                }
            }

            // Cleanup.
            phoneNumbers = null;
            dtCommunications.Clear();
            dtAttributeValues.Clear();
            GC.Collect();
        }

        /// <summary>
        /// Internal method for ExportCompanies().
        /// </summary>
        private void ExportCompanies_Internal()
        {
            WriteBusinessAttributes();

            var dtCompany = _db.Table( "Company" ).Data;

            if ( dtCompany.Rows.Count == 0 )
            {
                // Nothing to export.
                return;
            }

            DataView dvCommunication = _db.Table( "Communication" ).Data.DefaultView;
            dvCommunication.Sort = "LastUpdatedDate"; // Communications must be sorted by LastUpdatedDate to ensure we get the right values.

            var companyCommunications = ( from table1 in dvCommunication.ToTable().AsEnumerable()
                                          join table2 in dtCompany.AsEnumerable()
                                          on ( int ) table1["household_id"] equals ( int ) table2["household_id"]
                                          select table1 );

            var dtCompanyCommunications = companyCommunications.CopyToDataTable_Safe( _db.Table( "Communication" ).Data );

            foreach ( DataRow row in dtCompany.Rows )
            {
                var business = F1Business.Translate( row, dtCompanyCommunications );

                if ( business != null )
                {
                    ImportPackage.WriteToPackage( business );
                }
            }

            // export Phone Numbers
            foreach ( DataRow row in dtCompanyCommunications.Rows )
            {
                var importNumber = F1BusinessPhone.Translate( row );

                if ( importNumber != null )
                {
                    ImportPackage.WriteToPackage( importNumber );
                }
            }

            // Cleanup.
            dtCompanyCommunications.Clear();
            GC.Collect();

            var dtAddress = _db.Table( "Household_Address" ).Data;

            var companyAddresses = ( from table1 in dtAddress.AsEnumerable()
                                     join table2 in dtCompany.AsEnumerable()
                                     on ( int ) table1["household_id"] equals ( int ) table2["household_id"]
                                     select table1 );

            var dtCompanyAddresses = companyAddresses.CopyToDataTable_Safe( dtAddress );

            foreach ( DataRow row in dtCompanyAddresses.Rows )
            {
                var importAddress = F1BusinessAddress.Translate( row );

                if ( importAddress != null )
                {
                    ImportPackage.WriteToPackage( importAddress );
                }
            }

            // Cleanup.
            dtCompanyAddresses.Clear();
            GC.Collect();
        }

        /// <summary>
        /// Internal method for ExportNotes().
        /// </summary>
        private void ExportNotes_Internal()
        {
            var dtUsers = _db.Table( "Users" ).Data;
            var dtNotes = _db.Table( "Notes" ).Data;

            var headOfHouseHoldMap = GetHeadOfHouseholdMap( _db.Table( "Company" ).Data );
            var users = dtUsers.AsEnumerable().ToArray();

            foreach ( DataRow row in dtNotes.Rows )
            {
                var importNote = F1Note.Translate( row, headOfHouseHoldMap, users );

                if ( importNote != null )
                {
                    ImportPackage.WriteToPackage( importNote );
                }
            }

            // Cleanup
            dtUsers.Clear();
            dtNotes.Clear();
            GC.Collect();
        }

        /// <summary>
        /// Internal method for ExportFinancialAccounts().
        /// </summary>
        private void ExportFinancialAccounts_Internal()
        {
            var dvFunds = new DataView( _db.Table( "Contribution" ).Data );

            // Get primary funds.
            var dtPrimaryFunds = dvFunds.ToTable( true, "fund_name");

            // Get sub-funds.
            dvFunds.RowFilter = "ISNULL(sub_fund_name, 'Null Column') <> 'Null Column'";
            var dtFunds = dvFunds.ToTable( true, "fund_name", "sub_fund_name" );

            // Merge primary and sub-funds.
            dtFunds.Merge( dtPrimaryFunds );

            foreach ( DataRow row in dtFunds.Rows )
            {
                var importAccount = F1FinancialAccount.Translate( row );

                if ( importAccount != null )
                {
                    ImportPackage.WriteToPackage( importAccount );
                }
            }

            // Cleanup.
            dtFunds.Clear();
            GC.Collect();
        }

        /// <summary>
        /// Internal method for ExportFinancialPledges().
        /// </summary>
        private void ExportFinancialPledges_Internal()
        {
            //ToDo:  Test this export method.
            var dvPledges = new DataView( _db.Table( "Pledge" ).Data );
            var dtPledges = dvPledges.ToTable( true, "individual_id", "household_id", "Pledge_id", "fund_name", "sub_fund_name", "pledge_frequency_name", "total_pledge", "start_date", "end_date" );
            
            //Get head of house holds because in F1 pledges can be tied to individuals or households
            var headOfHouseHolds = GetHeadOfHouseholdMap( _db.Table( "Company" ).Data );

            foreach ( DataRow row in dtPledges.Rows )
            {
                var importPledge = F1FinancialPledge.Translate( row, headOfHouseHolds );

                if ( importPledge != null )
                {
                    ImportPackage.WriteToPackage( importPledge );
                }
            }

            // Cleanup.
            dtPledges.Clear();
            GC.Collect();
        }

        /// <summary>
        /// Internal method for ExportFinancialBatches().
        /// </summary>
        /// <param name="modifiedSince">The modified since.</param>
        private void ExportFinancialBatches_Internal( DateTime modifiedSince )
        {
            var batches = _db.Table( "Batch" ).Data.AsEnumerable()
                .Select( b => new BatchDTO {
                    BatchId = b.Field<int>( "BatchId" ),
                    BatchName = b.Field<string>( "BatchName" ),
                    BatchDate = b.Field<DateTime>( "BatchDate" ),
                    BatchAmount = b.Field<decimal>( "BatchAmount" )
                } ).ToList();

            var batchesFromContribution = _db.Table( "Contribution" ).Data.AsEnumerable()
                .Where( c => c.Field<int?>( "BatchId" ) == null )
                .Select( c => new {
                    BatchId = 90000000 + Int32.Parse( c.Field<DateTime>( "Received_Date" ).ToString( "yyyyMMdd" ) ),
                    BatchName = "Batch: " + c.Field<DateTime>( "Received_Date" ).ToString( "MMM dd, yyyy" ),
                    BatchDate = c.Field<DateTime>( "Received_Date" ),
                    BatchAmount = c.Field<decimal>( "Amount" )
                } )
                .GroupBy( c => c.BatchId  )
                .Select( x => new BatchDTO
                {
                    BatchId = x.First().BatchId,
                    BatchName = x.First().BatchName,
                    BatchDate = x.Min( z => z.BatchDate ),
                    BatchAmount = x.Sum( z => z.BatchAmount )
                } ).ToList();

            batches = batches.Concat( batchesFromContribution ).ToList();

            foreach ( var batch in batches )
            {
                var importBatch = F1FinancialBatch.Translate( batch );

                if ( importBatch != null )
                {
                    ImportPackage.WriteToPackage( importBatch );
                }
            }
        }

        /// <summary>
        /// Internal method for ExportContributions().
        /// </summary>
        /// <param name="modifiedSince">The modified since.</param>
        /// <param name="exportContribImages">Indicates whether images should be exported.  (WARNING:  Not implemented.)</param>
        private void ExportContributions_Internal( DateTime modifiedSince, bool exportContribImages )
        {
            var dtContributions = _db.Table( "Contribution" ).Data;
            var headOfHouseholdMap = GetHeadOfHouseholdMap( _db.Table( "Company" ).Data );

            var dtCompany = _db.Table( "Company" ).Data;
            var companyIds = new HashSet<int>( dtCompany.AsEnumerable().Select( s => s.Field<int>( "Household_ID" ) ) );

            foreach ( DataRow row in dtContributions.Rows )
            {
                var importTransaction = F1FinancialTransaction.Translate( row, headOfHouseholdMap, companyIds );

                if ( importTransaction != null )
                {
                    ImportPackage.WriteToPackage( importTransaction );
                }
            }
        }

        /// <summary>
        /// Internal method for ExportGroups().
        /// </summary>
        /// <param name="selectedGroupTypes">The selected group types.</param>
        private void ExportGroups_Internal( List<int> selectedGroupTypes )
        {
            // write out the group types
            WriteGroupTypes( selectedGroupTypes );

            foreach( var groupType in GetGroupTypes().Where( g => selectedGroupTypes.Contains( g.Id ) ) )
            {
                int parentGroupId = 90000000 + groupType.Id ;

                ImportPackage.WriteToPackage( new Group()
                {
                    Id = parentGroupId,
                    Name = groupType.Name,
                    GroupTypeId = groupType.Id
                } );
            }

            // Export F1 Activities
            if( selectedGroupTypes.Contains( 99999904 ) )
            {
                var dvActivityAssignment = new DataView( _db.Table( "ActivityAssignment" ).Data );

                dvActivityAssignment.RowFilter = "ISNULL(RLC_ID, 0) <> 0";
                var dtRlcGroupMembers = dvActivityAssignment.ToTable( true, "Individual_ID", "RLC_ID", "BreakoutGroupName" );

                dvActivityAssignment.RowFilter = "ISNULL(BreakoutGroupName,'Null Column') <> 'Null Column'";
                var dtBreakoutGroupMembers = dvActivityAssignment.ToTable( true, "Individual_ID", "BreakoutGroupName", "RLC_ID", "Activity_ID" );

                MD5 md5Hasher = MD5.Create();
                var rlcAssignments = dtRlcGroupMembers.AsEnumerable().Select( r => new GroupMemberDTO
                {
                    IndividualId = r.Field<int>( "Individual_ID" ),
                    GroupId = r.Field<int>( "RLC_ID" ),
                    BreakoutGroupName = r.Field<string>( "BreakoutGroupName" ),
                    GroupMemberType = "Member"
                } );

                var breakoutGroupAssignments = dtBreakoutGroupMembers.AsEnumerable().Select( r => new GroupMemberDTO
                {
                    IndividualId = r.Field<int>( "Individual_ID" ),
                    BreakoutGroupName = r.Field<string>( "BreakoutGroupName" ),
                    ParentGroupId = r.Field<int?>( "RLC_ID" ) ?? r.Field<int?>( "Activity_ID" ),
                    GroupMemberType = "Member",
                    GroupIdHasher = md5Hasher // Add Group Ids for Break Out Groups
                } );

                var activityMembers = rlcAssignments.Concat( breakoutGroupAssignments ).ToList();

                var dvActivityMinistry = new DataView( _db.Table( "ActivityMinistry" ).Data );
                var dtMinistries = dvActivityMinistry.ToTable( true, "Ministry_Name", "Ministry_ID", "Ministry_Active" );
                var ministryGroups = dtMinistries.AsEnumerable().Select( r => new GroupDTO
                {
                    GroupName = r.Field<string>( "Ministry_Name" ),
                    GroupId = r.Field<int>( "Ministry_ID" ),
                    GroupTypeId = 99999904,
                    Description = null,
                    IsActive = !( r.Field<bool?>( "Ministry_Active" ) == false ),
                    StartDate = null,
                    IsPublic = false,
                    LocationName = string.Empty,
                    ScheduleDay = string.Empty,
                    StartHour = null,
                    Address1 = string.Empty,
                    Address2 = string.Empty,
                    City = string.Empty,
                    StateProvince = string.Empty,
                    PostalCode = null,
                    Country = string.Empty,
                    ParentGroupId = 0
                } );

                var dtMinistryActivities = dvActivityMinistry.ToTable( true, "Activity_Name", "Activity_ID", "Activity_Active", "Ministry_ID" );
                var ministryActivityGroups = dtMinistryActivities.AsEnumerable().Select( r => new GroupDTO
                {
                    GroupName = r.Field<string>( "Activity_Name" ),
                    GroupId = r.Field<int>( "Activity_ID" ),
                    GroupTypeId = 99999904,
                    Description = null,
                    IsActive = !( r.Field<bool?>( "Activity_Active" ) == false ),
                    StartDate = null,
                    IsPublic = false,
                    LocationName = string.Empty,
                    ScheduleDay = string.Empty,
                    StartHour = null,
                    Address1 = string.Empty,
                    Address2 = string.Empty,
                    City = string.Empty,
                    StateProvince = string.Empty,
                    PostalCode = null,
                    Country = string.Empty,
                    ParentGroupId = r.Field<int?>( "Ministry_ID" )
                } );

                var dvActivityGroup = new DataView( _db.Table( "Activity_Group" ).Data );
                var dtActivities = dvActivityGroup.ToTable( true, "Activity_Group_Name", "Activity_Group_ID", "Activity_ID" );
                var activityGroups = dtActivities.AsEnumerable().Select( r => new GroupDTO
                {
                    GroupName = r.Field<string>( "Activity_Group_Name" ),
                    GroupId = r.Field<int>( "Activity_Group_ID" ),
                    GroupTypeId = 99999904,
                    Description = null,
                    IsActive = true,
                    StartDate = null,
                    IsPublic = false,
                    LocationName = string.Empty,
                    ScheduleDay = string.Empty,
                    StartHour = null,
                    Address1 = string.Empty,
                    Address2 = string.Empty,
                    City = string.Empty,
                    StateProvince = string.Empty,
                    PostalCode = null,
                    Country = string.Empty,
                    ParentGroupId = r.Field<int?>( "Activity_ID" )
                } );

                var dvRLC = new DataView( _db.Table( "RLC" ).Data );
                var dtRLC = dvRLC.ToTable( true, "RLC_Name", "RLC_ID", "Is_Active", "Room_Name", "Activity_Group_ID", "Activity_ID" );
                var rlcGroups = dtRLC.AsEnumerable().Select( r => new GroupDTO
                {
                    GroupName = r.Field<string>( "RLC_Name" ),
                    GroupId = r.Field<int>( "RLC_ID" ),
                    GroupTypeId = 99999904,
                    Description = null,
                    IsActive = r.Field<bool>( "Is_Active" ),
                    StartDate = null,
                    IsPublic = false,
                    LocationName = r.Field<string>( "Room_Name" ),
                    ScheduleDay = string.Empty,
                    StartHour = null,
                    Address1 = string.Empty,
                    Address2 = string.Empty,
                    City = string.Empty,
                    StateProvince = string.Empty,
                    PostalCode = null,
                    Country = string.Empty,
                    ParentGroupId = r.Field<int?>( "Activity_Group_ID" ) ?? r.Field<int?>( "Activity_ID" )
                } );

                dvActivityAssignment.RowFilter = "ISNULL(BreakoutGroupName,'Null Column') <> 'Null Column'"; // This is already set, but it's good to have it here as a reminder.
                var dtActivityAssignmentGruops = dvActivityAssignment.ToTable( true, "BreakoutGroupName", "RLC_ID", "Activity_ID" );
                var activityAssignmentGroups = dtActivityAssignmentGruops.AsEnumerable().Select( r => new GroupDTO
                {
                    GroupName = r.Field<string>( "BreakoutGroupName" ),
                    GroupId = null,
                    GroupTypeId = 99999904,
                    Description = null,
                    IsActive = true,
                    StartDate = null,
                    IsPublic = false,
                    LocationName = string.Empty,
                    ScheduleDay = string.Empty,
                    StartHour = null,
                    Address1 = string.Empty,
                    Address2 = string.Empty,
                    City = string.Empty,
                    StateProvince = string.Empty,
                    PostalCode = null,
                    Country = string.Empty,
                    ParentGroupId = r.Field<int?>( "RLC_ID" ) ?? r.Field<int?>( "Activity_ID" )
                } );

                var activities =
                    ministryGroups
                    .Concat( ministryActivityGroups )
                    .Concat( activityGroups )
                    .Concat( rlcGroups )
                    .Concat( activityAssignmentGroups )
                    .ToList();

                var dtStaffing = _db.Table( "Staffing_Assignment" ).Data;
                foreach ( var row in activities )
                {
                    var importGroup = F1Group.Translate( row, activityMembers, dtStaffing );

                    if ( importGroup != null )
                    {
                        ImportPackage.WriteToPackage( importGroup );
                    }
                }
                // Cleanup.
                GC.Collect();
            }

            // Export F1 Groups
            var groupMembersQry = _db.Table( "Groups").Data.AsEnumerable()
                .Select( r => new GroupMemberDTO
                {
                    IndividualId = r.Field<int>( "Individual_ID" ),
                    GroupId = r.Field<int?>( "Group_Id" ),
                    GroupMemberType = r.Field<string>("Group_Member_Type")
                } );

            var groupMembers = groupMembersQry.Distinct().ToList();

            var dvGroups = new DataView( _db.Table( "Groups").Data );
            var dtGroupsDistinct = dvGroups.ToTable( true, "Group_Id", "Group_Type_Id", "Group_Name" );
            var dtGroupsDescription = _db.Table( "GroupsDescription" ).Data;

            var groups = ( from table1 in dtGroupsDistinct.AsEnumerable()
                           join table2 in dtGroupsDescription.AsEnumerable()
                           on ( int ) table1["Group_ID"] equals ( int ) table2["Group_ID"]
                           into groupsWithDescriptions
                           from output in groupsWithDescriptions.DefaultIfEmpty()
                           where selectedGroupTypes.Contains( ( int ) table1["Group_Type_Id"] )
                           select new GroupDTO
                           {
                               GroupName = table1.Field<string>( "Group_Name" ),
                               GroupId = table1.Field<int>( "Group_Id" ),
                               GroupTypeId = table1.Field<int>( "Group_Type_Id" ),
                               IsActive = !( output?.Field<bool?>( "is_open" ) == false ),
                               StartDate = output?.Field<DateTime>( "start_date" ),
                               IsPublic = output?.Field<bool?>( "is_open" ),
                               LocationName = output?.Field<string>( "Location_Name" ),
                               ScheduleDay = output?.Field<string>( "ScheduleDay" ),
                               StartHour = output?.Field<string>( "StartHour" ),
                               Address1 = output?.Field<string>( "Address1" ),
                               Address2 = output?.Field<string>( "Address2" ),
                               City = output?.Field<string>( "City" ),
                               StateProvince = output?.Field<string>( "StProvince" ),
                               PostalCode = output?.Field<string>( "PostalCode" ),
                               Country = output?.Field<string>( "Country" ),
                               ParentGroupId = null
                           } ).ToList();

            foreach ( var group in groups )
            {
                var importGroup = F1Group.Translate( group, groupMembers, null );

                if ( importGroup != null )
                {
                    ImportPackage.WriteToPackage( importGroup );
                }
            }

            // Cleanup.
            GC.Collect();
        }

        /// <summary>
        /// Internal method for ExportAttendance().
        /// </summary>
        /// <param name="modifiedSince">The modified since.</param>
        private void ExportAttendance_Internal( DateTime modifiedSince )
        {
            var dtAttendance = _db.Table( "Attendance" ).Data;

            var attendanceData_AssignedIds = dtAttendance.AsEnumerable()
                    .Where( r => r.Field<DateTime?>( "Start_Date_Time" ) != null )
                    .Where( r => r.Field<int?>( "Individual_Instance_ID" ) != null )
                    .Select( r => new AttendanceDTO {
                        IndividualId = r.Field<int>( "Individual_ID" ),
                        AttendanceId = r.Field<int>( "Individual_Instance_ID" ),
                        GroupId = r.Field<int?>( "RLC_ID" ),
                        StartDateTime = r.Field<DateTime>( "Start_Date_Time" ),
                        EndDateTime = r.Field<DateTime?>( "Check_Out_Time" ),
                        CheckedInAs = r.Field<string>( "CheckedInAs" ),
                        JobTitle = r.Field<string>( "Job_Title" )
                    } ).Distinct().ToList();

            var attendanceData_NullIds = dtAttendance.AsEnumerable()
                    .Where( r => r.Field<DateTime?>( "Start_Date_Time" ) != null )
                    .Where( r => r.Field<int?>( "Individual_Instance_ID" ) == null )
                    .Select( r => new AttendanceDTO {
                        IndividualId = r.Field<int>( "Individual_ID" ),
                        AttendanceId = null,
                        GroupId = r.Field<int?>( "RLC_ID" ),
                        StartDateTime = r.Field<DateTime>( "Start_Date_Time" ),
                        EndDateTime = r.Field<DateTime?>( "Check_Out_Time" ),
                        CheckedInAs = r.Field<string>( "CheckedInAs" ),
                        JobTitle = r.Field<string>( "Job_Title" )
                    } ).Distinct().ToList();

            var groupAttendanceData = _db.Table( "GroupsAttendance" ).Data.AsEnumerable()
                //.Where( r => r.Field<bool>( "IsPresent" ) != false ) //ToDo:  This should be uncommented when F1 updates the data set.
                .Select( r => new AttendanceDTO {
                    IndividualId = r.Field<int>( "Individual_ID" ),
                    AttendanceId = null,
                    GroupId = r.Field<int?>( "GroupID" ),
                    StartDateTime = r.Field<DateTime>( "AttendanceDate" ),
                    EndDateTime = r.Field<DateTime?>( "EndDateTime" ),
                    CheckedInAs = r.Field<string>( "CheckedInAs" ),
                    JobTitle = r.Field<string>( "Job_Title" ),
                } ).Distinct().ToList();

            attendanceData_NullIds = attendanceData_NullIds.Concat( groupAttendanceData ).ToList();

            var uniqueAttendanceIds = new List<int>();

            // Process rows with assigned Individual_Instance_ID values, first, to ensure their AttendanceId is preserved.
            foreach ( var attendance in attendanceData_AssignedIds )
            {
                var importAttendance = F1Attendance.Translate( attendance, uniqueAttendanceIds );
                if ( importAttendance != null )
                {
                    ImportPackage.WriteToPackage( importAttendance );
                }
            }

            // Process remaining rows without assigned Individual_Instance_ID values.  These records will have AttendanceId
            // values generated by an MD5 hash of the Individual_ID, GroupId, and StartDateTime.  Collisions for
            // that hash are relatively common and in cases where that occurs, a random value will be used, instead.
            foreach ( var attendance in attendanceData_NullIds )
            {
                var importAttendance = F1Attendance.Translate( attendance, uniqueAttendanceIds );
                if ( importAttendance != null )
                {
                    ImportPackage.WriteToPackage( importAttendance );
                }
            }

        }

        #endregion Internal Methods

    }
}
