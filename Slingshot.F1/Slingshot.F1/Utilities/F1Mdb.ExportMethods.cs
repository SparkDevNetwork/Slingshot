using Slingshot.Core.Model;
using Slingshot.Core.Utilities;
using Slingshot.F1.Utilities.Translators.MDB;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Slingshot.F1.Utilities
{
    public partial class F1Mdb : F1Translator
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

        // Please review GetTableData_CallOrder.txt before modifying or copying any methods which use the GetTableData() method.

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
            using ( var dtPeople = GetTableData( SqlQueries.PEOPLE, true ) )
            {

                // export people
                using ( var dtHoh = GetTableData( SqlQueries.HEAD_OF_HOUSEHOLD, true ) )
                using ( var dtCommunications = GetTableData( SqlQueries.COMMUNICATIONS ) )
                using ( var dtCommunicationValues = GetTableData( SqlQueries.COMMUNCATION_ATTRIBUTE_VALUES ) )
                using ( var dtRequirementValues = GetTableData( SqlQueries.REQUIREMENTVALUES ) )
                {
                    // Make Requirement Names match case, as they may be in mixed cases in the F1 database.
                    foreach ( DataRow row in dtRequirementValues.Rows )
                    {
                        string requirementName = row["requirement_name"].ToString();
                        if ( _RequirementNames.ContainsKey( requirementName.ToLower() ) )
                        {
                            if ( _RequirementNames[requirementName.ToLower()] != requirementName )
                            {
                                row["requirement_name"] = _RequirementNames[requirementName.ToLower()];
                            }
                        }
                    }

                    var headOfHouseHolds = GetHeadOfHouseholdMap( dtHoh );

                    //Split communications into basic elements to make subsequent queries faster.
                    var dtCommunications_IndividualEmails =
                        dtCommunications.Select( "individual_id IS NOT NULL AND communication_type = 'Email'" ).CopyToDataTable();

                    var dtCommunications_InfellowshipLogins =
                        dtCommunications.Select( "individual_id IS NOT NULL AND communication_type = 'Infellowship Login'" ).CopyToDataTable();

                    var dtCommunications_HouseholdEmails =
                        dtCommunications.Select( "individual_id IS NULL AND household_id IS NOT NULL AND communication_type = 'Email'" ).CopyToDataTable();

                    foreach ( DataRow row in dtPeople.Rows )
                    {
                        var importPerson = F1Person.Translate( row, dtCommunications_IndividualEmails, dtCommunications_InfellowshipLogins, dtCommunications_HouseholdEmails, headOfHouseHolds, dtRequirementValues, dtCommunicationValues );

                        if ( importPerson != null )
                        {
                            ImportPackage.WriteToPackage( importPerson );
                        }
                    }

                    // Cleanup - Remember not to Clear() any cached tables.
                    dtCommunications.Clear();
                    dtCommunicationValues.Clear();
                    dtRequirementValues.Clear();
                    GC.Collect();
                }

                // export people addresses
                using ( var dtAddress = GetTableData( SqlQueries.ADDRESSES ) )
                {
                    foreach ( DataRow row in dtAddress.Rows )
                    {
                        var importAddress = F1PersonAddress.Translate( row, dtPeople );

                        if ( importAddress != null )
                        {
                            ImportPackage.WriteToPackage( importAddress );
                        }
                    }

                    // Cleanup - Remember not to Clear() any cached tables.
                    dtAddress.Clear();
                    GC.Collect();
                }
            }

            // export Attribute Values
            using ( var dtAttributeValues = GetTableData( SqlQueries.ATTRIBUTEVALUES ) )
            {
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

                // Cleanup - Remember not to Clear() any cached tables.
                dtAttributeValues.Clear();
                GC.Collect();
            }

            // export Phone Numbers
            using ( var dtPhoneNumbers = GetTableData( SqlQueries.PHONE_NUMBERS ) )
            {
                foreach ( DataRow row in dtPhoneNumbers.Rows )
                {
                    var importNumber = F1PersonPhone.Translate( row );
                    if ( importNumber != null )
                    {
                        ImportPackage.WriteToPackage( importNumber );
                    }
                }

                // Cleanup - Remember not to Clear() any cached tables.
                dtPhoneNumbers.Clear();
                GC.Collect();
            }
        }

        /// <summary>
        /// Internal method for ExportCompanies().
        /// </summary>
        private void ExportCompanies_Internal()
        {
            WriteBusinessAttributes();

            using ( var dtCommunications = GetTableData( SqlQueries.COMPANY_COMMUNICATIONS ) )
            using ( var dtCompanies = GetTableData( SqlQueries.COMPANY, true ) )
            {
                foreach ( DataRow row in dtCompanies.Rows )
                {
                    var business = F1Business.Translate( row, dtCommunications );

                    if ( business != null )
                    {
                        ImportPackage.WriteToPackage( business );
                    }
                }

                // export Phone Numbers
                foreach ( DataRow row in dtCommunications.Rows )
                {
                    var importNumber = F1BusinessPhone.Translate( row );

                    if ( importNumber != null )
                    {
                        ImportPackage.WriteToPackage( importNumber );
                    }
                }

                // Cleanup - Remember not to Clear() any cached tables.
                dtCommunications.Clear();
                GC.Collect();
            }

            using ( var dtAddress = GetTableData( SqlQueries.COMPANY_ADDRESSES ) )
            {
                foreach ( DataRow row in dtAddress.Rows )
                {
                    var importAddress = F1BusinessAddress.Translate( row );

                    if ( importAddress != null )
                    {
                        ImportPackage.WriteToPackage( importAddress );
                    }
                }

                // Cleanup - Remember not to Clear() any cached tables.
                dtAddress.Clear();
                GC.Collect();
            }
        }

        /// <summary>
        /// Internal method for ExportNotes().
        /// </summary>
        private void ExportNotes_Internal()
        {
            using ( var dtUsers = GetTableData( SqlQueries.USERS ) )
            using ( var dtNotes = GetTableData( SqlQueries.NOTES ) )
            using ( var dtHoh = GetTableData( SqlQueries.HEAD_OF_HOUSEHOLD, true ) )
            {
                var headOfHouseHoldMap = GetHeadOfHouseholdMap( dtHoh );
                var users = dtUsers.AsEnumerable().ToArray();

                foreach ( DataRow row in dtNotes.Rows )
                {
                    var importNote = F1Note.Translate( row, headOfHouseHoldMap, users );

                    if ( importNote != null )
                    {
                        ImportPackage.WriteToPackage( importNote );
                    }
                }

                // Cleanup - Remember not to Clear() any cached tables.
                dtUsers.Clear();
                dtNotes.Clear();
                GC.Collect();
            }
        }

        /// <summary>
        /// Internal method for ExportFinancialAccounts().
        /// </summary>
        private void ExportFinancialAccounts_Internal()
        {
            using ( var dtFunds = GetTableData( SqlQueries.FUNDS ) )
            {
                foreach ( DataRow row in dtFunds.Rows )
                {
                    var importAccount = F1FinancialAccount.Translate( row );

                    if ( importAccount != null )
                    {
                        ImportPackage.WriteToPackage( importAccount );
                    }
                }

                // Cleanup - Remember not to Clear() any cached tables.
                dtFunds.Clear();
                GC.Collect();
            }
        }

        /// <summary>
        /// Internal method for ExportFinancialPledges().
        /// </summary>
        private void ExportFinancialPledges_Internal()
        {
            using ( var dtPledges = GetTableData( SqlQueries.PLEDGES ) )
            {
                //Get head of house holds because in F1 pledges can be tied to indiviuals or households
                var headOfHouseHolds = GetHeadOfHouseholdMap( GetTableData( SqlQueries.HEAD_OF_HOUSEHOLD, true ) );

                foreach ( DataRow row in dtPledges.Rows )
                {
                    var importPledge = F1FinancialPledge.Translate( row, headOfHouseHolds );

                    if ( importPledge != null )
                    {
                        ImportPackage.WriteToPackage( importPledge );
                    }
                }

                // Cleanup - Remember not to Clear() any cached tables.
                dtPledges.Clear();
                GC.Collect();
            }
        }

        /// <summary>
        /// Internal method for ExportFinancialBatches().
        /// </summary>
        /// <param name="modifiedSince">The modified since.</param>
        private void ExportFinancialBatches_Internal( DateTime modifiedSince )
        {
            using ( var dtBatches = GetTableData( SqlQueries.BATCHES ) )
            {
                foreach ( DataRow row in dtBatches.Rows )
                {
                    var importBatch = F1FinancialBatch.Translate( row );

                    if ( importBatch != null )
                    {
                        ImportPackage.WriteToPackage( importBatch );
                    }
                }

                // Cleanup - Remember not to Clear() any cached tables.
                dtBatches.Clear();
                GC.Collect();
            }
        }

        /// <summary>
        /// Internal method for ExportContributions().
        /// </summary>
        /// <param name="modifiedSince">The modified since.</param>
        /// <param name="exportContribImages">Indicates whether images should be exported.  (WARNING:  Not implemented.)</param>
        private void ExportContributions_Internal( DateTime modifiedSince, bool exportContribImages )
        {
            using ( var dtHoh = GetTableData( SqlQueries.HEAD_OF_HOUSEHOLD, true ) )
            using ( var dtPeople = GetTableData( SqlQueries.PEOPLE, true ) )
            using ( var dtContributions = GetTableData( SqlQueries.CONTRIBUTIONS ) )
            {
                var headOfHouseholdMap = GetHeadOfHouseholdMap( dtHoh );

                var dtCompanies = GetTableData( SqlQueries.COMPANY, true );
                var companyIds = new HashSet<int>( dtCompanies.AsEnumerable().Select( s => s.Field<int>( "HOUSEHOLD_ID" ) ) );

                foreach ( DataRow row in dtContributions.Rows )
                {
                    var importTransaction = F1FinancialTransaction.Translate( row, headOfHouseholdMap, companyIds );

                    if ( importTransaction != null )
                    {
                        ImportPackage.WriteToPackage( importTransaction );
                    }
                }

                // Cleanup - Remember not to Clear() any cached tables.
                dtContributions.Clear();
                GC.Collect();
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

            // Export F1 Activites
            if( selectedGroupTypes.Contains( 99999904 ) )
            {
                using ( var dtActivityMembers = GetTableData( SqlQueries.ACTIVITY_MEMBERS ) )
                {
                    // Add Group Ids for Break Out Groups
                    foreach ( var member in dtActivityMembers.Select( "Group_Id is null" ) )
                    {
                        MD5 md5Hasher = MD5.Create();
                        var hashed = md5Hasher.ComputeHash( Encoding.UTF8.GetBytes( member.Field<string>( "BreakoutGroup" ) + member.Field<string>( "ParentGroupId" ) ) );
                        var groupId = Math.Abs( BitConverter.ToInt32( hashed, 0 ) ); // used abs to ensure positive number
                        if ( groupId > 0 )
                        {
                            member.SetField<int>( "Group_Id", groupId );
                        }
                    }

                    using ( var dtStaffing = GetTableData( SqlQueries.STAFFING ) )
                    using ( var dtActivites = GetTableData( SqlQueries.ACTIVITIES ) )
                    {
                   
                        foreach ( DataRow row in dtActivites.Rows )
                        {
                            var importGroup = F1Group.Translate( row, dtActivityMembers, dtStaffing );

                            if ( importGroup != null )
                            {
                                ImportPackage.WriteToPackage( importGroup );
                            }
                        }

                        // Cleanup - Remember not to Clear() any cached tables.
                        dtStaffing.Clear();
                        dtActivites.Clear();
                    }

                    // Cleanup - Remember not to Clear() any cached tables.
                    dtActivityMembers.Clear();
                    GC.Collect();
                }
            }

            using ( var dtGroups = GetTableData( SqlQueries.GROUPS ) )
            using ( var dtGroupMembers = GetTableData( SqlQueries.GROUP_MEMBERS ) )
            {
                var group_Type_Ids = string.Join( ",", selectedGroupTypes.Select( n => n.ToString() ).ToArray() );

                foreach ( DataRow row in dtGroups.Select( "Group_Type_Id in(" + group_Type_Ids + ")" ) )
                {
                    var importGroup = F1Group.Translate( row, dtGroupMembers, null );

                    if ( importGroup != null )
                    {
                        ImportPackage.WriteToPackage( importGroup );
                    }
                }

                // Cleanup - Remember not to Clear() any cached tables.
                dtGroups.Clear();
                dtGroupMembers.Clear();
                GC.Collect();
            }
        }

        /// <summary>
        /// Internal method for ExportAttendance().
        /// </summary>
        /// <param name="modifiedSince">The modified since.</param>
        private void ExportAttendance_Internal( DateTime modifiedSince )
        {
            using ( var dtAttendance = GetTableData( SqlQueries.ATTENDANCE ) )
            {
                var uniqueAttendanceIds = new List<int>();
                foreach ( DataRow row in dtAttendance.Rows )
                {
                    var importAttendance = F1Attendance.Translate( row, uniqueAttendanceIds );

                    if ( importAttendance != null )
                    {
                        ImportPackage.WriteToPackage( importAttendance );
                    }
                }

                // Cleanup - Remember not to Clear() any cached tables.
                dtAttendance.Clear();
                GC.Collect();
            }
        }

        #endregion Internal Methods

    }
}
