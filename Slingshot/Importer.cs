using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using CsvHelper;
using Newtonsoft.Json;
using RestSharp;
using Rock;

namespace Slingshot
{
    /// <summary>
    /// 
    /// </summary>
    public class Importer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Importer"/> class.
        /// </summary>
        /// <param name="slingshotFileName">Name of the slingshot file.</param>
        public Importer( string slingshotFileName, string rockUrl, string rockUserName, string rockPassword )
        {
            SlingshotFileName = slingshotFileName;
            RockUrl = rockUrl;
            RockUserName = rockUserName;
            RockPassword = rockPassword;
        }

        /// <summary>
        /// Gets or sets the rock URL.
        /// </summary>
        /// <value>
        /// The rock URL.
        /// </value>
        private string RockUrl { get; set; }

        /// <summary>
        /// Gets or sets the name of the rock user.
        /// </summary>
        /// <value>
        /// The name of the rock user.
        /// </value>
        private string RockUserName { get; set; }

        /// <summary>
        /// Gets or sets the rock password.
        /// </summary>
        /// <value>
        /// The rock password.
        /// </value>
        private string RockPassword { get; set; }

        /* Person Related */
        private Dictionary<Guid, Rock.Client.GroupTypeRole> FamilyRoles { get; set; }

        private Dictionary<Guid, Rock.Client.DefinedValue> PersonRecordTypeValues { get; set; }

        private Dictionary<Guid, Rock.Client.DefinedValue> PersonRecordStatusValues { get; set; }

        private Dictionary<string, Rock.Client.DefinedValue> PersonConnectionStatusValues { get; set; }

        private Dictionary<string, Rock.Client.DefinedValue> PersonTitleValues { get; set; }

        private Dictionary<string, Rock.Client.DefinedValue> PersonSuffixValues { get; set; }

        private Dictionary<Guid, Rock.Client.DefinedValue> PersonMaritalStatusValues { get; set; }

        private Dictionary<string, Rock.Client.DefinedValue> PhoneNumberTypeValues { get; set; }

        private Dictionary<Guid, Rock.Client.DefinedValue> GroupLocationTypeValues { get; set; }

        private Dictionary<Guid, Rock.Client.DefinedValue> LocationTypeValues { get; set; }

        private Dictionary<string, Rock.Client.Attribute> PersonAttributeKeyLookup { get; set; }

        private Dictionary<string, Rock.Client.Attribute> FamilyAttributeKeyLookup { get; set; }

        private List<Slingshot.Core.Model.PersonAttribute> SlingshotPersonAttributes { get; set; }

        private List<Slingshot.Core.Model.FamilyAttribute> SlingshotFamilyAttributes { get; set; }

        private List<Slingshot.Core.Model.Person> SlingshotPersonList { get; set; }

        /* Core  */
        private List<Rock.Client.Campus> Campuses { get; set; }

        private Dictionary<Guid, Rock.Client.EntityType> EntityTypeLookup { get; set; }

        private Dictionary<string, Rock.Client.FieldType> FieldTypeLookup { get; set; }

        private List<Rock.Client.Category> AttributeCategoryList { get; set; }

        // GroupType Lookup by ForeignId
        private Dictionary<int, Rock.Client.GroupType> GroupTypeLookupByForeignId { get; set; }

        /* Attendance */
        private List<Slingshot.Core.Model.Attendance> SlingshotAttendanceList { get; set; }

        private List<Slingshot.Core.Model.Group> SlingshotGroupList { get; set; }

        private List<Slingshot.Core.Model.GroupType> SlingshotGroupTypeList { get; set; }

        private List<Slingshot.Core.Model.Location> SlingshotLocationList { get; set; }

        private List<Slingshot.Core.Model.Schedule> SlingshotScheduleList { get; set; }

        /* Financial Transactions */
        private List<Slingshot.Core.Model.FinancialAccount> SlingshotFinancialAccountList { get; set; }

        private List<Slingshot.Core.Model.FinancialBatch> SlingshotFinancialBatchList { get; set; }

        private List<Slingshot.Core.Model.FinancialTransaction> SlingshotFinancialTransactionList { get; set; }

        private Dictionary<Guid, Rock.Client.DefinedValue> TransactionSourceTypeValues { get; set; }

        private Dictionary<Guid, Rock.Client.DefinedValue> TransactionTypeValues { get; set; }

        private Dictionary<Guid, Rock.Client.DefinedValue> CurrencyTypeValues { get; set; }

        /* */
        private string SlingshotFileName { get; set; }

        /// <summary>
        /// Gets or sets the results.
        /// </summary>
        /// <value>
        /// The results.
        /// </value>
        public Dictionary<string, string> Results { get; set; }

        /// <summary>
        /// Gets or sets the rock rest client.
        /// </summary>
        /// <value>
        /// The rock rest client.
        /// </value>
        private RestClient RockRestClient { get; set; }

        /// <summary>
        /// Gets or sets the background worker.
        /// </summary>
        /// <value>
        /// The background worker.
        /// </value>
        private BackgroundWorker BackgroundWorker { get; set; }

        /// <summary>
        /// Gets or sets the group type identifier family.
        /// </summary>
        /// <value>
        /// The group type identifier family.
        /// </value>
        private int GroupTypeIdFamily { get; set; }

        /// <summary>
        /// Handles the DoWork event of the BackgroundWorker control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="DoWorkEventArgs"/> instance containing the event data.</param>
        public void BackgroundWorker_DoWork( object sender, DoWorkEventArgs e )
        {
            BackgroundWorker = sender as BackgroundWorker;

            // Load Slingshot Models from .slingshot
            BackgroundWorker.ReportProgress( 0, "Loading Slingshot Models..." );
            LoadSlingshotLists();

            // Load Rock Lookups
            BackgroundWorker.ReportProgress( 0, "Loading Rock Lookups..." );
            LoadLookups();

            this.RockRestClient = this.GetRockRestClient();

            BackgroundWorker.ReportProgress( 0, "Updating Rock Lookups..." );

            this.Results = new Dictionary<string, string>();

            // Populate Rock with stuff that comes from the Slingshot file
            AddCampuses();
            AddConnectionStatuses();
            AddPersonTitles();
            AddPersonSuffixes();
            AddPhoneTypes();
            AddAttributeCategories();
            AddPersonAttributes();
            AddFamilyAttributes();

            AddGroupTypes();

            // load lookups again in case we added some new ones
            BackgroundWorker.ReportProgress( 0, "Reloading Rock Lookups..." );
            LoadLookups();

            SubmitPersonImport();

            // Attendance Related
            SubmitLocationImport();
            SubmitGroupImport();
            SubmitScheduleImport();
            SubmitAttendanceImport();

            // Financial Transaction Related
            SubmitFinancialAccountImport();
            SubmitFinancialBatchImport();
            SubmitFinancialTransactionImport();
        }

        #region Financial Transaction Related

        /// <summary>
        /// Submits the financial account import.
        /// </summary>
        private void SubmitFinancialAccountImport()
        {
            BackgroundWorker.ReportProgress( 0, "Preparing FinancialAccountImport..." );
            var financialAccountImportList = new List<Rock.Client.BulkUpdate.FinancialAccountImport>();
            var campusLookup = this.Campuses.Where( a => a.ForeignId.HasValue ).ToDictionary( k => k.ForeignId.Value, v => v.Id );
            foreach ( var slingshotFinancialAccount in this.SlingshotFinancialAccountList )
            {
                var financialAccountImport = new Rock.Client.BulkUpdate.FinancialAccountImport();
                financialAccountImport.FinancialAccountForeignId = slingshotFinancialAccount.Id;

                financialAccountImport.Name = slingshotFinancialAccount.Name;
                if ( slingshotFinancialAccount.Name.IsNullOrWhiteSpace() )
                {
                    financialAccountImport.Name = "Unnamed Financial Account";
                }

                financialAccountImport.IsTaxDeductible = slingshotFinancialAccount.IsTaxDeductible;

                financialAccountImport.CampusId = campusLookup[slingshotFinancialAccount.CampusId];
                financialAccountImport.ParentFinancialAccountForeignId = slingshotFinancialAccount.ParentAccountId == 0 ? ( int? ) null : slingshotFinancialAccount.ParentAccountId;

                financialAccountImportList.Add( financialAccountImport );
            }

            RestRequest restImportRequest = new RestRequest( "api/BulkImport/FinancialAccountImport", Method.POST ) { RequestFormat = DataFormat.Json };

            restImportRequest.AddBody( financialAccountImportList );

            BackgroundWorker.ReportProgress( 0, "Sending FinancialAccount Import to Rock..." );

            var importResponse = this.RockRestClient.Post( restImportRequest );

            Results.Add( "FinancialAccount Import", importResponse.Content.FromJsonOrNull<string>() ?? importResponse.Content );

            if ( importResponse.StatusCode == System.Net.HttpStatusCode.Created )
            {
                BackgroundWorker.ReportProgress( 0, this.Results );
            }
            else
            {
                BackgroundWorker.ReportProgress( 0, importResponse.StatusDescription );
            }
        }

        /// <summary>
        /// Submits the financial batch import.
        /// </summary>
        private void SubmitFinancialBatchImport()
        {
            BackgroundWorker.ReportProgress( 0, "Preparing FinancialBatchImport..." );
            var financialBatchImportList = new List<Rock.Client.BulkUpdate.FinancialBatchImport>();
            var campusLookup = this.Campuses.Where( a => a.ForeignId.HasValue ).ToDictionary( k => k.ForeignId.Value, v => v.Id );
            foreach ( var slingshotFinancialBatch in this.SlingshotFinancialBatchList )
            {
                var financialBatchImport = new Rock.Client.BulkUpdate.FinancialBatchImport();
                financialBatchImport.FinancialBatchForeignId = slingshotFinancialBatch.Id;

                financialBatchImport.Name = slingshotFinancialBatch.Name;
                if ( slingshotFinancialBatch.Name.IsNullOrWhiteSpace() )
                {
                    financialBatchImport.Name = "Unnamed Financial Batch";
                }

                financialBatchImport.ControlAmount = slingshotFinancialBatch.ControlAmount;
                financialBatchImport.CreatedByPersonForeignId = slingshotFinancialBatch.CreatedByPersonId;
                financialBatchImport.CreatedDateTime = slingshotFinancialBatch.CreatedDateTime;
                financialBatchImport.EndDate = slingshotFinancialBatch.EndDate;
                financialBatchImport.ModifiedByPersonForeignId = slingshotFinancialBatch.ModifiedByPersonId;
                financialBatchImport.ModifiedDateTime = slingshotFinancialBatch.ModifiedDateTime;
                financialBatchImport.StartDate = slingshotFinancialBatch.StartDate;

                switch ( slingshotFinancialBatch.Status )
                {
                    case Core.Model.BatchStatus.Closed:
                        financialBatchImport.Status = ( int ) Rock.Client.Enums.BatchStatus.Closed;
                        break;
                    case Core.Model.BatchStatus.Open:
                        financialBatchImport.Status = ( int ) Rock.Client.Enums.BatchStatus.Open;
                        break;
                    case Core.Model.BatchStatus.Pending:
                        financialBatchImport.Status = ( int ) Rock.Client.Enums.BatchStatus.Pending;
                        break;
                }

                financialBatchImport.CampusId = slingshotFinancialBatch.CampusId.HasValue ? campusLookup[slingshotFinancialBatch.CampusId.Value] : ( int? ) null;

                financialBatchImportList.Add( financialBatchImport );
            }

            RestRequest restImportRequest = new RestRequest( "api/BulkImport/FinancialBatchImport", Method.POST ) { RequestFormat = DataFormat.Json };

            restImportRequest.AddBody( financialBatchImportList );

            BackgroundWorker.ReportProgress( 0, "Sending FinancialBatch Import to Rock..." );

            var importResponse = this.RockRestClient.Post( restImportRequest );

            Results.Add( "FinancialBatch Import", importResponse.Content.FromJsonOrNull<string>() ?? importResponse.Content );

            if ( importResponse.StatusCode == System.Net.HttpStatusCode.Created )
            {
                BackgroundWorker.ReportProgress( 0, this.Results );
            }
            else
            {
                BackgroundWorker.ReportProgress( 0, importResponse.StatusDescription );
            }
        }

        /// <summary>
        /// Submits the financial transaction import.
        /// </summary>
        private void SubmitFinancialTransactionImport()
        {
            BackgroundWorker.ReportProgress( 0, "Preparing FinancialTransactionImport..." );
            var financialTransactionImportList = new List<Rock.Client.BulkUpdate.FinancialTransactionImport>();
            var campusLookup = this.Campuses.Where( a => a.ForeignId.HasValue ).ToDictionary( k => k.ForeignId.Value, v => v.Id );
            foreach ( var slingshotFinancialTransaction in this.SlingshotFinancialTransactionList )
            {
                var financialTransactionImport = new Rock.Client.BulkUpdate.FinancialTransactionImport();
                financialTransactionImport.FinancialTransactionForeignId = slingshotFinancialTransaction.Id;

                financialTransactionImport.AuthorizedPersonForeignId = slingshotFinancialTransaction.AuthorizedPersonId;
                financialTransactionImport.BatchForeignId = slingshotFinancialTransaction.BatchId;

                switch ( slingshotFinancialTransaction.CurrencyType )
                {
                    case Core.Model.CurrencyType.ACH:
                        financialTransactionImport.CurrencyTypeValueId = this.CurrencyTypeValues[Rock.Client.SystemGuid.DefinedValue.CURRENCY_TYPE_ACH.AsGuid()].Id;
                        break;
                    case Core.Model.CurrencyType.Cash:
                        financialTransactionImport.CurrencyTypeValueId = this.CurrencyTypeValues[Rock.Client.SystemGuid.DefinedValue.CURRENCY_TYPE_CASH.AsGuid()].Id;
                        break;
                    case Core.Model.CurrencyType.Check:
                        financialTransactionImport.CurrencyTypeValueId = this.CurrencyTypeValues[Rock.Client.SystemGuid.DefinedValue.CURRENCY_TYPE_CHECK.AsGuid()].Id;
                        break;
                    case Core.Model.CurrencyType.CreditCard:
                        financialTransactionImport.CurrencyTypeValueId = this.CurrencyTypeValues[Rock.Client.SystemGuid.DefinedValue.CURRENCY_TYPE_CREDIT_CARD.AsGuid()].Id;
                        break;
                    case Core.Model.CurrencyType.NonCash:
                        financialTransactionImport.CurrencyTypeValueId = this.CurrencyTypeValues[Rock.Client.SystemGuid.DefinedValue.CURRENCY_TYPE_NONCASH.AsGuid()].Id;
                        break;
                    case Core.Model.CurrencyType.Other:
                        // TODO: Do we need to support this?
                        break;
                }

                switch ( slingshotFinancialTransaction.TransactionSource )
                {
                    case Core.Model.TransactionSource.BankChecks:
                        financialTransactionImport.TransactionSourceValueId = this.TransactionSourceTypeValues[Rock.Client.SystemGuid.DefinedValue.FINANCIAL_SOURCE_TYPE_BANK_CHECK.AsGuid()].Id;
                        break;
                    case Core.Model.TransactionSource.Kiosk:
                        financialTransactionImport.TransactionSourceValueId = this.TransactionSourceTypeValues[Rock.Client.SystemGuid.DefinedValue.FINANCIAL_SOURCE_TYPE_KIOSK.AsGuid()].Id;
                        break;
                    case Core.Model.TransactionSource.MobileApplication:
                        financialTransactionImport.TransactionSourceValueId = this.TransactionSourceTypeValues[Rock.Client.SystemGuid.DefinedValue.FINANCIAL_SOURCE_TYPE_MOBILE_APPLICATION.AsGuid()].Id;
                        break;
                    case Core.Model.TransactionSource.OnsiteCollection:
                        financialTransactionImport.TransactionSourceValueId = this.TransactionSourceTypeValues[Rock.Client.SystemGuid.DefinedValue.FINANCIAL_SOURCE_TYPE_ONSITE_COLLECTION.AsGuid()].Id;
                        break;
                    case Core.Model.TransactionSource.Website:
                        financialTransactionImport.TransactionSourceValueId = this.TransactionSourceTypeValues[Rock.Client.SystemGuid.DefinedValue.FINANCIAL_SOURCE_TYPE_WEBSITE.AsGuid()].Id;
                        break;
                }

                switch ( slingshotFinancialTransaction.TransactionType )
                {
                    case Core.Model.TransactionType.Contribution:
                        financialTransactionImport.TransactionTypeValueId = this.TransactionTypeValues[Rock.Client.SystemGuid.DefinedValue.TRANSACTION_TYPE_CONTRIBUTION.AsGuid()].Id;
                        break;
                    case Core.Model.TransactionType.EventRegistration:
                        financialTransactionImport.TransactionTypeValueId = this.TransactionTypeValues[Rock.Client.SystemGuid.DefinedValue.TRANSACTION_TYPE_EVENT_REGISTRATION.AsGuid()].Id;
                        break;
                }

                financialTransactionImport.FinancialTransactionDetailImports = new List<Rock.Client.BulkUpdate.FinancialTransactionDetailImport>();
                foreach ( var slingshotFinancialTransactionDetail in slingshotFinancialTransaction.FinancialTransactionDetails )
                {
                    var financialTransactionDetailImport = new Rock.Client.BulkUpdate.FinancialTransactionDetailImport();
                    financialTransactionDetailImport.FinancialAccountForeignId = slingshotFinancialTransactionDetail.AccountId;
                    financialTransactionDetailImport.Amount = slingshotFinancialTransactionDetail.Amount;
                    financialTransactionDetailImport.CreatedByPersonForeignId = slingshotFinancialTransactionDetail.CreatedByPersonId;
                    financialTransactionDetailImport.CreatedDateTime = slingshotFinancialTransactionDetail.CreatedDateTime;
                    financialTransactionDetailImport.FinancialTransactionDetailForeignId = slingshotFinancialTransactionDetail.Id;
                    financialTransactionDetailImport.ModifiedByPersonForeignId = slingshotFinancialTransactionDetail.ModifiedByPersonId;
                    financialTransactionDetailImport.ModifiedDateTime = slingshotFinancialTransactionDetail.ModifiedDateTime;
                    financialTransactionDetailImport.Summary = slingshotFinancialTransactionDetail.Summary;
                    financialTransactionImport.FinancialTransactionDetailImports.Add( financialTransactionDetailImport );
                }

                financialTransactionImport.Summary = slingshotFinancialTransaction.Summary;
                financialTransactionImport.TransactionCode = slingshotFinancialTransaction.TransactionCode;
                financialTransactionImport.TransactionDate = slingshotFinancialTransaction.TransactionDate;
                financialTransactionImport.CreatedByPersonForeignId = slingshotFinancialTransaction.CreatedByPersonId;
                financialTransactionImport.CreatedDateTime = slingshotFinancialTransaction.CreatedDateTime;
                financialTransactionImport.ModifiedByPersonForeignId = slingshotFinancialTransaction.ModifiedByPersonId;
                financialTransactionImport.ModifiedDateTime = slingshotFinancialTransaction.ModifiedDateTime;

                financialTransactionImportList.Add( financialTransactionImport );
            }

            RestRequest restImportRequest = new RestRequest( "api/BulkImport/FinancialTransactionImport", Method.POST ) { RequestFormat = DataFormat.Json };

            restImportRequest.AddBody( financialTransactionImportList );

            BackgroundWorker.ReportProgress( 0, "Sending FinancialTransaction Import to Rock..." );

            var importResponse = this.RockRestClient.Post( restImportRequest );

            Results.Add( "FinancialTransaction Import", importResponse.Content.FromJsonOrNull<string>() ?? importResponse.Content );

            if ( importResponse.StatusCode == System.Net.HttpStatusCode.Created )
            {
                BackgroundWorker.ReportProgress( 0, this.Results );
            }
            else if ( importResponse.StatusCode == System.Net.HttpStatusCode.NotFound )
            {
                // either the endpoint doesn't exist, or the payload was too big
                int postSizeMB = financialTransactionImportList.ToJson().Length / 1024 / 1024;
                throw new SlingshotEndpointNotFoundException( $"Error posting to api/BulkImport/FinancialTransactionImport. Make sure that Rock has been updated to support FinancialTransactionImport, and also verify that Rock > Home / System Settings / System Configuration is configured to accept uploads larger than {postSizeMB}MB" );
            }
            else
            {
                BackgroundWorker.ReportProgress( 0, importResponse.StatusDescription );
            }
        }

        #endregion Financial Transaction Related

        #region Attendance Related

        /// <summary>
        /// Submits the attendance import.
        /// </summary>
        private void SubmitAttendanceImport()
        {
            BackgroundWorker.ReportProgress( 0, "Preparing AttendanceImport..." );
            var campusLookup = this.Campuses.Where( a => a.ForeignId.HasValue ).ToDictionary( k => k.ForeignId.Value, v => v.Id );
            var attendanceImportList = new List<Rock.Client.BulkUpdate.AttendanceImport>();
            foreach ( var slingshotAttendance in this.SlingshotAttendanceList )
            {
                var attendanceImport = new Rock.Client.BulkUpdate.AttendanceImport();

                //// Note: There is no Attendance.Id in slingshotAttendance
                attendanceImport.PersonForeignId = slingshotAttendance.PersonId;
                attendanceImport.GroupForeignId = slingshotAttendance.GroupId;
                attendanceImport.LocationForeignId = slingshotAttendance.LocationId;
                attendanceImport.ScheduleForeignId = slingshotAttendance.ScheduleId;
                attendanceImport.StartDateTime = slingshotAttendance.StartDateTime;
                attendanceImport.EndDateTime = slingshotAttendance.EndDateTime;
                attendanceImport.Note = slingshotAttendance.Note;
                if ( slingshotAttendance.CampusId.HasValue )
                {
                    attendanceImport.CampusId = campusLookup[slingshotAttendance.CampusId.Value];
                }

                attendanceImportList.Add( attendanceImport );
            }

            RestRequest restImportRequest = new RestRequest( "api/BulkImport/AttendanceImport", Method.POST ) { RequestFormat = DataFormat.Json };

            restImportRequest.AddBody( attendanceImportList );

            BackgroundWorker.ReportProgress( 0, "Sending Attendance Import to Rock..." );

            var importResponse = this.RockRestClient.Post( restImportRequest );

            Results.Add( "Attendance Import", importResponse.Content.FromJsonOrNull<string>() ?? importResponse.Content );

            if ( importResponse.StatusCode == System.Net.HttpStatusCode.Created )
            {
                BackgroundWorker.ReportProgress( 0, this.Results );
            }
            else if ( importResponse.StatusCode == System.Net.HttpStatusCode.NotFound )
            {
                // either the endpoint doesn't exist, or the payload was too big
                int postSizeMB = attendanceImportList.ToJson().Length / 1024 / 1024;
                throw new SlingshotEndpointNotFoundException( $"Error posting to api/BulkImport/AttendanceImport. Make sure that Rock has been updated to support AttendanceImport, and also verify that Rock > Home / System Settings / System Configuration is configured to accept uploads larger than {postSizeMB}MB" );
            }
            else
            {
                BackgroundWorker.ReportProgress( 0, importResponse.StatusDescription );
            }
        }

        /// <summary>
        /// Submits the schedule import.
        /// </summary>
        private void SubmitScheduleImport()
        {
            BackgroundWorker.ReportProgress( 0, "Preparing ScheduleImport..." );
            var scheduleImportList = new List<Rock.Client.BulkUpdate.ScheduleImport>();
            foreach ( var slingshotSchedule in this.SlingshotScheduleList )
            {
                var scheduleImport = new Rock.Client.BulkUpdate.ScheduleImport();
                scheduleImport.ScheduleForeignId = slingshotSchedule.Id;
                scheduleImport.Name = slingshotSchedule.Name;
                scheduleImportList.Add( scheduleImport );
            }

            RestRequest restImportRequest = new RestRequest( "api/BulkImport/ScheduleImport", Method.POST ) { RequestFormat = DataFormat.Json };

            restImportRequest.AddBody( scheduleImportList );

            BackgroundWorker.ReportProgress( 0, "Sending Schedule Import to Rock..." );

            var importResponse = this.RockRestClient.Post( restImportRequest );

            Results.Add( "Schedule Import", importResponse.Content.FromJsonOrNull<string>() ?? importResponse.Content );

            if ( importResponse.StatusCode == System.Net.HttpStatusCode.Created )
            {
                BackgroundWorker.ReportProgress( 0, this.Results );
            }
            else
            {
                BackgroundWorker.ReportProgress( 0, importResponse.StatusDescription );
            }
        }

        /// <summary>
        /// Submits the location import.
        /// </summary>
        private void SubmitLocationImport()
        {
            BackgroundWorker.ReportProgress( 0, "Preparing LocationImport..." );
            var locationImportList = new List<Rock.Client.BulkUpdate.LocationImport>();
            foreach ( var slingshotLocation in this.SlingshotLocationList )
            {
                var locationImport = new Rock.Client.BulkUpdate.LocationImport();
                locationImport.LocationForeignId = slingshotLocation.Id;
                locationImport.ParentLocationForeignId = slingshotLocation.ParentLocationId;
                locationImport.Name = slingshotLocation.Name;
                locationImport.IsActive = slingshotLocation.IsActive;

                // set LocationType to null since Rock usually leaves it null except for Campus, Building, and Room
                locationImport.LocationTypeValueId = null;
                locationImport.Street1 = slingshotLocation.Street1;
                locationImport.Street2 = slingshotLocation.Street2;
                locationImport.City = slingshotLocation.City;
                locationImport.County = slingshotLocation.County;
                locationImport.State = slingshotLocation.State;
                locationImport.Country = slingshotLocation.Country;
                locationImport.PostalCode = slingshotLocation.PostalCode;

                locationImportList.Add( locationImport );
            }

            RestRequest restImportRequest = new RestRequest( "api/BulkImport/LocationImport", Method.POST ) { RequestFormat = DataFormat.Json };

            restImportRequest.AddBody( locationImportList );

            BackgroundWorker.ReportProgress( 0, "Sending Location Import to Rock..." );

            var importResponse = this.RockRestClient.Post( restImportRequest );

            Results.Add( "Location Import", importResponse.Content.FromJsonOrNull<string>() ?? importResponse.Content );

            if ( importResponse.StatusCode == System.Net.HttpStatusCode.Created )
            {
                BackgroundWorker.ReportProgress( 0, this.Results );
            }
            else
            {
                BackgroundWorker.ReportProgress( 0, importResponse.StatusDescription );
            }
        }

        /// <summary>
        /// Submits the group import.
        /// </summary>
        private void SubmitGroupImport()
        {
            BackgroundWorker.ReportProgress( 0, "Preparing GroupImport..." );
            var groupImportList = new List<Rock.Client.BulkUpdate.GroupImport>();
            var campusLookup = this.Campuses.Where( a => a.ForeignId.HasValue ).ToDictionary( k => k.ForeignId.Value, v => v.Id );
            foreach ( var slingshotGroup in this.SlingshotGroupList )
            {
                var groupImport = new Rock.Client.BulkUpdate.GroupImport();
                groupImport.GroupForeignId = slingshotGroup.Id;
                groupImport.GroupTypeId = this.GroupTypeLookupByForeignId[slingshotGroup.GroupTypeId].Id;

                groupImport.Name = slingshotGroup.Name;
                if ( slingshotGroup.Name.IsNullOrWhiteSpace() )
                {
                    groupImport.Name = "Unnamed Group";
                }

                groupImport.Order = slingshotGroup.Order;
                if ( slingshotGroup.CampusId.HasValue )
                {
                    groupImport.CampusId = campusLookup[slingshotGroup.CampusId.Value];
                }

                groupImport.ParentGroupForeignId = slingshotGroup.ParentGroupId == 0 ? ( int? ) null : slingshotGroup.ParentGroupId;
                groupImport.GroupMemberImports = new List<Rock.Client.BulkUpdate.GroupMemberImport>();

                foreach ( var groupMember in slingshotGroup.GroupMembers )
                {
                    var groupMemberImport = new Rock.Client.BulkUpdate.GroupMemberImport();
                    groupMemberImport.PersonForeignId = groupMember.PersonId;
                    groupMemberImport.RoleName = groupMember.Role;
                    groupImport.GroupMemberImports.Add( groupMemberImport );
                }

                groupImportList.Add( groupImport );
            }

            RestRequest restImportRequest = new RestRequest( "api/BulkImport/GroupImport", Method.POST ) { RequestFormat = DataFormat.Json };

            restImportRequest.AddBody( groupImportList );

            BackgroundWorker.ReportProgress( 0, "Sending Group Import to Rock..." );

            var importResponse = this.RockRestClient.Post( restImportRequest );

            Results.Add( "Group Import", importResponse.Content.FromJsonOrNull<string>() ?? importResponse.Content );

            if ( importResponse.StatusCode == System.Net.HttpStatusCode.Created )
            {
                BackgroundWorker.ReportProgress( 0, this.Results );
            }
            else
            {
                BackgroundWorker.ReportProgress( 0, importResponse.StatusDescription );
            }
        }

        #endregion Attendance Related

        #region Person Related

        /// <summary>
        /// Submits the person import.
        /// </summary>
        /// <param name="bwWorker">The bw worker.</param>
        private void SubmitPersonImport()
        {
            BackgroundWorker.ReportProgress( 0, "Preparing PersonImport..." );
            List<Rock.Client.BulkUpdate.PersonImport> personImportList = GetPersonImportList();

            RestRequest restPersonImportRequest = new RestRequest( "api/BulkImport/PersonImport", Method.POST );
            restPersonImportRequest.RequestFormat = RestSharp.DataFormat.Json;

            restPersonImportRequest.AddBody( personImportList );

            int fiveMinutesMS = ( 1000 * 60 ) * 5;
            restPersonImportRequest.Timeout = fiveMinutesMS;

            BackgroundWorker.ReportProgress( 0, "Sending Person Import to Rock..." );

            var importResponse = this.RockRestClient.Post( restPersonImportRequest );

            Results.Add( "Person Import", importResponse.Content.FromJsonOrNull<string>() ?? importResponse.Content );

            if ( importResponse.StatusCode == System.Net.HttpStatusCode.Created )
            {
                BackgroundWorker.ReportProgress( 0, this.Results );
            }
            else if ( importResponse.StatusCode == System.Net.HttpStatusCode.NotFound )
            {
                // either the endpoint doesn't exist, or the payload was too big
                int postSizeMB = personImportList.ToJson().Length / 1024 / 1024;
                throw new SlingshotEndpointNotFoundException( $"Error posting to api/BulkImport/PersonImport. Make sure that Rock has been updated to support PersonImport, and also verify that Rock > Home / System Settings / System Configuration is configured to accept uploads larger than {postSizeMB}MB" );
            }
            else
            {
                BackgroundWorker.ReportProgress( 0, importResponse.StatusDescription );
            }
        }

        /// <summary>
        /// Gets the person import list.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception">personImport.PersonForeignId must be greater than 0
        /// or
        /// personImport.FamilyForeignId must be greater than 0 or null
        /// or</exception>
        private List<Rock.Client.BulkUpdate.PersonImport> GetPersonImportList()
        {
            List<Rock.Client.BulkUpdate.PersonImport> personImportList = new List<Rock.Client.BulkUpdate.PersonImport>();
            foreach ( var slingshotPerson in this.SlingshotPersonList )
            {
                var personImport = new Rock.Client.BulkUpdate.PersonImport();
                personImport.PersonForeignId = slingshotPerson.Id;
                personImport.FamilyForeignId = slingshotPerson.FamilyId;

                if ( personImport.PersonForeignId <= 0 )
                {
                    throw new Exception( "personImport.PersonForeignId must be greater than 0" );
                }

                if ( personImport.FamilyForeignId <= 0 )
                {
                    throw new Exception( "personImport.FamilyForeignId must be greater than 0 or null" );
                }

                personImport.FamilyName = slingshotPerson.FamilyName;
                personImport.FamilyImageUrl = slingshotPerson.FamilyImageUrl;

                switch ( slingshotPerson.FamilyRole )
                {
                    case Slingshot.Core.Model.FamilyRole.Adult:
                        personImport.GroupRoleId = this.FamilyRoles[Rock.Client.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_ADULT.AsGuid()].Id;
                        break;
                    case Slingshot.Core.Model.FamilyRole.Child:
                        personImport.GroupRoleId = this.FamilyRoles[Rock.Client.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_CHILD.AsGuid()].Id;
                        break;
                }

                if ( !string.IsNullOrEmpty( slingshotPerson.Campus?.CampusName ) )
                {
                    var lookupCampus = this.Campuses.Where( a => a.Name.Equals( slingshotPerson.Campus.CampusName, StringComparison.OrdinalIgnoreCase ) ).FirstOrDefault();
                    personImport.CampusId = lookupCampus?.Id;
                }

                switch ( slingshotPerson.RecordStatus )
                {
                    case Core.Model.RecordStatus.Active:
                        personImport.RecordStatusValueId = this.PersonRecordStatusValues[Rock.Client.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_ACTIVE.AsGuid()]?.Id;
                        break;
                    case Core.Model.RecordStatus.Inactive:
                        personImport.RecordStatusValueId = this.PersonRecordStatusValues[Rock.Client.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_INACTIVE.AsGuid()]?.Id;
                        break;
                    case Core.Model.RecordStatus.Pending:
                        personImport.RecordStatusValueId = this.PersonRecordStatusValues[Rock.Client.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_PENDING.AsGuid()]?.Id;
                        break;
                }

                personImport.InactiveReasonNote = slingshotPerson.InactiveReason;

                if ( !string.IsNullOrEmpty( slingshotPerson.ConnectionStatus ) )
                {
                    personImport.ConnectionStatusValueId = this.PersonConnectionStatusValues[slingshotPerson.ConnectionStatus]?.Id;
                }

                if ( !string.IsNullOrEmpty( slingshotPerson.Salutation ) )
                {
                    personImport.TitleValueId = this.PersonTitleValues[slingshotPerson.Salutation]?.Id;
                }

                if ( !string.IsNullOrEmpty( slingshotPerson.Suffix ) )
                {
                    personImport.SuffixValueId = this.PersonSuffixValues[slingshotPerson.Suffix]?.Id;
                }

                personImport.IsDeceased = slingshotPerson.IsDeceased;

                personImport.FirstName = slingshotPerson.FirstName;
                personImport.NickName = slingshotPerson.NickName;
                personImport.MiddleName = slingshotPerson.MiddleName;
                personImport.LastName = slingshotPerson.LastName;

                if ( slingshotPerson.Birthdate.HasValue )
                {
                    personImport.BirthMonth = slingshotPerson.Birthdate.Value.Month;
                    personImport.BirthDay = slingshotPerson.Birthdate.Value.Day;
                    personImport.BirthYear = slingshotPerson.Birthdate.Value.Year == slingshotPerson.BirthdateNoYearMagicYear ? ( int? ) null : slingshotPerson.Birthdate.Value.Year;
                }

                switch ( slingshotPerson.Gender )
                {
                    case Core.Model.Gender.Male:
                        personImport.Gender = ( int ) Rock.Client.Enums.Gender.Male;
                        break;
                    case Core.Model.Gender.Female:
                        personImport.Gender = ( int ) Rock.Client.Enums.Gender.Female;
                        break;
                    case Core.Model.Gender.Unknown:
                        personImport.Gender = ( int ) Rock.Client.Enums.Gender.Unknown;
                        break;
                }

                switch ( slingshotPerson.MaritalStatus )
                {
                    case Core.Model.MaritalStatus.Married:
                        personImport.MaritalStatusValueId = this.PersonMaritalStatusValues[Rock.Client.SystemGuid.DefinedValue.PERSON_MARITAL_STATUS_MARRIED.AsGuid()].Id;
                        break;
                    case Core.Model.MaritalStatus.Single:
                        personImport.MaritalStatusValueId = this.PersonMaritalStatusValues[Rock.Client.SystemGuid.DefinedValue.PERSON_MARITAL_STATUS_SINGLE.AsGuid()].Id;
                        break;
                    case Core.Model.MaritalStatus.Divorced:
                        personImport.MaritalStatusValueId = this.PersonMaritalStatusValues[Rock.Client.SystemGuid.DefinedValue.PERSON_MARITAL_STATUS_DIVORCED.AsGuid()].Id;
                        break;
                    case Core.Model.MaritalStatus.Unknown:
                        personImport.MaritalStatusValueId = null;
                        break;
                }

                personImport.AnniversaryDate = slingshotPerson.AnniversaryDate;

                personImport.Grade = slingshotPerson.Grade;
                personImport.Email = slingshotPerson.Email;

                switch ( slingshotPerson.EmailPreference )
                {
                    case Core.Model.EmailPreference.EmailAllowed:
                        personImport.EmailPreference = ( int ) Rock.Client.Enums.EmailPreference.EmailAllowed;
                        break;
                    case Core.Model.EmailPreference.DoNotEmail:
                        personImport.EmailPreference = ( int ) Rock.Client.Enums.EmailPreference.DoNotEmail;
                        break;
                    case Core.Model.EmailPreference.NoMassEmails:
                        personImport.EmailPreference = ( int ) Rock.Client.Enums.EmailPreference.NoMassEmails;
                        break;
                }

                personImport.CreatedDateTime = slingshotPerson.CreatedDateTime;
                personImport.ModifiedDateTime = slingshotPerson.ModifiedDateTime;
                personImport.PersonPhotoUrl = slingshotPerson.PersonPhotoUrl;

                personImport.Note = slingshotPerson.Note;
                personImport.GivingIndividually = slingshotPerson.GiveIndividually ?? false;

                // Phone Numbers
                personImport.PhoneNumbers = new List<Rock.Client.BulkUpdate.PhoneNumberImport>();
                foreach ( var slingshotPersonPhone in slingshotPerson.PhoneNumbers )
                {
                    var phoneNumberImport = new Rock.Client.BulkUpdate.PhoneNumberImport();
                    phoneNumberImport.NumberTypeValueId = this.PhoneNumberTypeValues[slingshotPersonPhone.PhoneType].Id;
                    phoneNumberImport.Number = slingshotPersonPhone.PhoneNumber;
                    phoneNumberImport.IsMessagingEnabled = slingshotPersonPhone.IsMessagingEnabled ?? false;
                    phoneNumberImport.IsUnlisted = slingshotPersonPhone.IsUnlisted ?? false;
                    personImport.PhoneNumbers.Add( phoneNumberImport );
                }

                // Addresses
                personImport.Addresses = new List<Rock.Client.BulkUpdate.PersonAddressImport>();
                foreach ( var slingshotPersonAddress in slingshotPerson.Addresses )
                {
                    if ( !string.IsNullOrEmpty( slingshotPersonAddress.Street1 ) )
                    {
                        int? groupLocationTypeValueId = null;
                        switch ( slingshotPersonAddress.AddressType )
                        {
                            case Core.Model.AddressType.Home:
                                groupLocationTypeValueId = this.GroupLocationTypeValues[Rock.Client.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_HOME.AsGuid()].Id;
                                break;
                            case Core.Model.AddressType.Previous:
                                groupLocationTypeValueId = this.GroupLocationTypeValues[Rock.Client.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_PREVIOUS.AsGuid()].Id;
                                break;
                            case Core.Model.AddressType.Work:
                                groupLocationTypeValueId = this.GroupLocationTypeValues[Rock.Client.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_WORK.AsGuid()].Id;
                                break;
                        }

                        if ( groupLocationTypeValueId.HasValue )
                        {
                            var addressImport = new Rock.Client.BulkUpdate.PersonAddressImport()
                            {
                                GroupLocationTypeValueId = groupLocationTypeValueId.Value,
                                IsMailingLocation = slingshotPersonAddress.AddressType == Core.Model.AddressType.Home,
                                IsMappedLocation = slingshotPersonAddress.AddressType == Core.Model.AddressType.Home,
                                Street1 = slingshotPersonAddress.Street1,
                                Street2 = slingshotPersonAddress.Street2,
                                City = slingshotPersonAddress.City,
                                State = slingshotPersonAddress.State,
                                Country = slingshotPersonAddress.Country,
                                PostalCode = slingshotPersonAddress.PostalCode,
                                Latitude = slingshotPersonAddress.Latitude.AsDoubleOrNull(),
                                Longitude = slingshotPersonAddress.Longitude.AsDoubleOrNull()
                            };

                            personImport.Addresses.Add( addressImport );
                        }
                        else
                        {
                            throw new Exception( $"Unexpected Address Type: {slingshotPersonAddress.AddressType}" );
                        }
                    }
                }

                // Attribute Values
                personImport.AttributeValues = new List<Rock.Client.BulkUpdate.AttributeValueImport>();
                foreach ( var slingshotPersonAttributeValue in slingshotPerson.Attributes )
                {
                    int attributeId = this.PersonAttributeKeyLookup[slingshotPersonAttributeValue.AttributeKey].Id;
                    var attributeValueImport = new Rock.Client.BulkUpdate.AttributeValueImport { AttributeId = attributeId, Value = slingshotPersonAttributeValue.AttributeValue };
                    personImport.AttributeValues.Add( attributeValueImport );
                }

                personImportList.Add( personImport );
            }

            return personImportList;
        }

        #endregion Person Related

        /// <summary>
        /// Add any campuses that aren't in Rock yet
        /// </summary>
        private void AddCampuses()
        {
            Dictionary<int, Slingshot.Core.Model.Campus> importCampuses = new Dictionary<int, Slingshot.Core.Model.Campus>();
            foreach ( var campus in this.SlingshotPersonList.Select( a => a.Campus ) )
            {
                if ( !importCampuses.ContainsKey( campus.CampusId ) )
                {
                    importCampuses.Add( campus.CampusId, campus );
                }
            }

            foreach ( var importCampus in importCampuses.Where( a => !this.Campuses.Any( c => c.Name.Equals( a.Value.CampusName, StringComparison.OrdinalIgnoreCase ) ) ).Select( a => a.Value ) )
            {
                var campusToAdd = new Rock.Client.CampusEntity { ForeignId = importCampus.CampusId, Name = importCampus.CampusName, Guid = Guid.NewGuid() };

                RestRequest restCampusPostRequest = new RestRequest( "api/Campuses", Method.POST );
                restCampusPostRequest.RequestFormat = RestSharp.DataFormat.Json;
                restCampusPostRequest.AddBody( campusToAdd );

                var postCampusResponse = this.RockRestClient.Post( restCampusPostRequest );
            }
        }

        /// <summary>
        /// Add any GroupTypes that aren't in Rock yet
        /// </summary>
        private void AddGroupTypes()
        {
            foreach ( var importGroupType in this.SlingshotGroupTypeList.Where( a => !this.GroupTypeLookupByForeignId.ContainsKey( a.Id ) ) )
            {
                var groupTypeToAdd = new Rock.Client.GroupType { ForeignId = importGroupType.Id, Name = importGroupType.Name, Guid = Guid.NewGuid() };
                groupTypeToAdd.ShowInGroupList = true;
                groupTypeToAdd.ShowInNavigation = true;
                groupTypeToAdd.GroupTerm = "Group";
                groupTypeToAdd.GroupMemberTerm = "Member";

                RestRequest restPostRequest = new RestRequest( "api/GroupTypes", Method.POST );
                restPostRequest.RequestFormat = RestSharp.DataFormat.Json;
                restPostRequest.AddBody( groupTypeToAdd );

                var postResponse = this.RockRestClient.Post( restPostRequest );
            }
        }

        /// <summary>
        /// Adds any attribute categories that are in the slingshot files (person and family attributes)
        /// </summary>
        private void AddAttributeCategories()
        {
            int entityTypeIdAttribute = this.EntityTypeLookup[Rock.Client.SystemGuid.EntityType.ATTRIBUTE.AsGuid()].Id;
            var attributeCategoryNames = this.SlingshotPersonAttributes.Where( a => !string.IsNullOrWhiteSpace( a.Category ) ).Select( a => a.Category ).Distinct().ToList();
            attributeCategoryNames.AddRange( this.SlingshotFamilyAttributes.Where( a => !string.IsNullOrWhiteSpace( a.Category ) ).Select( a => a.Category ).Distinct().ToList() );
            foreach ( var slingshotAttributeCategoryName in attributeCategoryNames.Distinct().ToList() )
            {
                if ( !this.AttributeCategoryList.Any( a => a.Name.Equals( slingshotAttributeCategoryName, StringComparison.OrdinalIgnoreCase ) ) )
                {
                    Rock.Client.Category attributeCategory = new Rock.Client.Category();
                    attributeCategory.Name = slingshotAttributeCategoryName;
                    attributeCategory.EntityTypeId = entityTypeIdAttribute;
                    attributeCategory.Guid = Guid.NewGuid();

                    RestRequest restPostRequest = new RestRequest( "api/Categories", Method.POST );
                    restPostRequest.RequestFormat = RestSharp.DataFormat.Json;
                    restPostRequest.AddBody( attributeCategory );

                    var restPostResponse = this.RockRestClient.Post<int>( restPostRequest );
                    attributeCategory.Id = restPostResponse.Data;
                    this.AttributeCategoryList.Add( attributeCategory );
                }
            }
        }

        /// <summary>
        /// Adds the person attributes.
        /// </summary>
        private void AddPersonAttributes()
        {
            int entityTypeIdPerson = this.EntityTypeLookup[Rock.Client.SystemGuid.EntityType.PERSON.AsGuid()].Id;

            // Add any Person Attributes to Rock that aren't in Rock yet
            // NOTE: For now, just match by Attribute.Key. Don't try to do a customizable match
            foreach ( var slingshotPersonAttribute in this.SlingshotPersonAttributes )
            {
                slingshotPersonAttribute.Key = slingshotPersonAttribute.Key;

                Rock.Client.Attribute rockPersonAttribute = this.PersonAttributeKeyLookup.Select( a => a.Value ).FirstOrDefault( a => a.Key.Equals( slingshotPersonAttribute.Key, StringComparison.OrdinalIgnoreCase ) );
                if ( rockPersonAttribute == null )
                {
                    rockPersonAttribute = new Rock.Client.Attribute();
                    rockPersonAttribute.Key = slingshotPersonAttribute.Key;
                    rockPersonAttribute.Name = slingshotPersonAttribute.Name;
                    rockPersonAttribute.Guid = Guid.NewGuid();
                    rockPersonAttribute.EntityTypeId = entityTypeIdPerson;
                    rockPersonAttribute.FieldTypeId = this.FieldTypeLookup[slingshotPersonAttribute.FieldType].Id;

                    if ( !string.IsNullOrWhiteSpace( slingshotPersonAttribute.Category ) )
                    {
                        var attributeCategory = this.AttributeCategoryList.FirstOrDefault( a => a.Name.Equals( slingshotPersonAttribute.Category, StringComparison.OrdinalIgnoreCase ) );
                        if ( attributeCategory != null )
                        {
                            rockPersonAttribute.Categories = new List<Rock.Client.Category>();
                            rockPersonAttribute.Categories.Add( attributeCategory );
                        }
                    }

                    RestRequest restAttributePostRequest = new RestRequest( "api/Attributes", Method.POST );
                    restAttributePostRequest.RequestFormat = RestSharp.DataFormat.Json;
                    restAttributePostRequest.AddBody( rockPersonAttribute );

                    var restAttributePostResponse = this.RockRestClient.Post<int>( restAttributePostRequest );
                }
            }
        }

        /// <summary>
        /// Adds the family attributes.
        /// </summary>
        private void AddFamilyAttributes()
        {
            int entityTypeIdGroup = this.EntityTypeLookup[Rock.Client.SystemGuid.EntityType.GROUP.AsGuid()].Id;

            // Add any Family Attributes to Rock that aren't in Rock yet
            // NOTE: For now, just match by Attribute.Key. Don't try to do a customizable match
            foreach ( var slingshotFamilyAttribute in this.SlingshotFamilyAttributes )
            {
                slingshotFamilyAttribute.Key = slingshotFamilyAttribute.Key;

                Rock.Client.Attribute rockFamilyAttribute = this.FamilyAttributeKeyLookup.Select( a => a.Value ).FirstOrDefault( a => a.Key.Equals( slingshotFamilyAttribute.Key, StringComparison.OrdinalIgnoreCase ) );
                if ( rockFamilyAttribute == null )
                {
                    rockFamilyAttribute = new Rock.Client.Attribute();
                    rockFamilyAttribute.Key = slingshotFamilyAttribute.Key;
                    rockFamilyAttribute.Name = slingshotFamilyAttribute.Name;
                    rockFamilyAttribute.Guid = Guid.NewGuid();
                    rockFamilyAttribute.EntityTypeId = entityTypeIdGroup;
                    rockFamilyAttribute.EntityTypeQualifierColumn = "GroupTypeId";
                    rockFamilyAttribute.EntityTypeQualifierValue = this.GroupTypeIdFamily.ToString();
                    rockFamilyAttribute.FieldTypeId = this.FieldTypeLookup[slingshotFamilyAttribute.FieldType].Id;

                    if ( !string.IsNullOrWhiteSpace( slingshotFamilyAttribute.Category ) )
                    {
                        var attributeCategory = this.AttributeCategoryList.FirstOrDefault( a => a.Name.Equals( slingshotFamilyAttribute.Category, StringComparison.OrdinalIgnoreCase ) );
                        if ( attributeCategory != null )
                        {
                            rockFamilyAttribute.Categories = new List<Rock.Client.Category>();
                            rockFamilyAttribute.Categories.Add( attributeCategory );
                        }
                    }

                    RestRequest restAttributePostRequest = new RestRequest( "api/Attributes", Method.POST );
                    restAttributePostRequest.RequestFormat = RestSharp.DataFormat.Json;
                    restAttributePostRequest.AddBody( rockFamilyAttribute );

                    var restAttributePostResponse = this.RockRestClient.Post<int>( restAttributePostRequest );
                }
            }
        }

        /// <summary>
        /// Adds the connection statuses.
        /// </summary>
        private void AddConnectionStatuses()
        {
            AddDefinedValues( this.SlingshotPersonList.Select( a => a.ConnectionStatus ).Distinct().ToList(), this.PersonConnectionStatusValues );
        }

        /// <summary>
        /// Adds the person titles.
        /// </summary>
        private void AddPersonTitles()
        {
            AddDefinedValues( this.SlingshotPersonList.Select( a => a.Salutation ).Distinct().ToList(), this.PersonTitleValues );
        }

        /// <summary>
        /// Adds the person suffixes.
        /// </summary>
        private void AddPersonSuffixes()
        {
            AddDefinedValues( this.SlingshotPersonList.Select( a => a.Suffix ).Distinct().ToList(), this.PersonSuffixValues );
        }

        /// <summary>
        /// Adds the phone types.
        /// </summary>
        private void AddPhoneTypes()
        {
            AddDefinedValues( this.SlingshotPersonList.SelectMany( a => a.PhoneNumbers ).Select( a => a.PhoneType ).Distinct().ToList(), this.PhoneNumberTypeValues );
        }

        /// <summary>
        /// Adds the defined values.
        /// </summary>
        /// <param name="importDefinedValues">The import defined values.</param>
        /// <param name="currentValues">The current values.</param>
        private void AddDefinedValues( List<string> importDefinedValues, Dictionary<string, Rock.Client.DefinedValue> currentValues )
        {
            var definedTypeId = currentValues.Select( a => a.Value.DefinedTypeId ).First();
            foreach ( var importDefinedValue in importDefinedValues.Where( value => !currentValues.Keys.Any( k => k.Equals( value, StringComparison.OrdinalIgnoreCase ) ) ) )
            {
                var definedValueToAdd = new Rock.Client.DefinedValue { DefinedTypeId = definedTypeId, Value = importDefinedValue, Guid = Guid.NewGuid() };

                RestRequest restDefinedValuePostRequest = new RestRequest( "api/DefinedValues", Method.POST );
                restDefinedValuePostRequest.RequestFormat = RestSharp.DataFormat.Json;
                restDefinedValuePostRequest.AddBody( definedValueToAdd );

                var postDefinedValueResponse = this.RockRestClient.Post( restDefinedValuePostRequest );
            }
        }

        /// <summary>
        /// Loads the slingshot person list.
        /// </summary>
        /// <returns></returns>
        private void LoadSlingshotLists()
        {
            var slingshotFileName = SlingshotFileName;
            var slingshotDirectoryName = Path.Combine( Path.GetDirectoryName( slingshotFileName ), "slingshots", Path.GetFileNameWithoutExtension( slingshotFileName ) );

            var slingshotFilesDirectory = new DirectoryInfo( slingshotDirectoryName );
            if ( slingshotFilesDirectory.Exists )
            {
                slingshotFilesDirectory.Delete( true );
            }

            slingshotFilesDirectory.Create();
            ZipFile.ExtractToDirectory( slingshotFileName, slingshotFilesDirectory.FullName );

            List<Slingshot.Core.Model.Person> slingshotPersonList;
            Dictionary<int, List<Slingshot.Core.Model.PersonAddress>> slingshotPersonAddressListLookup;
            Dictionary<int, List<Slingshot.Core.Model.PersonAttributeValue>> slingshotPersonAttributeValueListLookup;
            Dictionary<int, List<Slingshot.Core.Model.PersonPhone>> slingshotPersonPhoneListLookup;

            using ( var slingshotFileStream = File.OpenText( Path.Combine( slingshotDirectoryName, new Slingshot.Core.Model.Person().GetFileName() ) ) )
            {
                CsvReader csvReader = new CsvReader( slingshotFileStream );
                csvReader.Configuration.HasHeaderRecord = true;
                csvReader.Configuration.WillThrowOnMissingField = false;
                slingshotPersonList = csvReader.GetRecords<Slingshot.Core.Model.Person>().ToList();
            }

            using ( var slingshotFileStream = File.OpenText( Path.Combine( slingshotDirectoryName, new Slingshot.Core.Model.PersonAddress().GetFileName() ) ) )
            {
                CsvReader csvReader = new CsvReader( slingshotFileStream );
                csvReader.Configuration.HasHeaderRecord = true;
                slingshotPersonAddressListLookup = csvReader.GetRecords<Slingshot.Core.Model.PersonAddress>().GroupBy( a => a.PersonId ).ToDictionary( k => k.Key, v => v.ToList() );
            }

            using ( var slingshotFileStream = File.OpenText( Path.Combine( slingshotDirectoryName, new Slingshot.Core.Model.PersonAttributeValue().GetFileName() ) ) )
            {
                CsvReader csvReader = new CsvReader( slingshotFileStream );
                csvReader.Configuration.HasHeaderRecord = true;
                slingshotPersonAttributeValueListLookup = csvReader.GetRecords<Slingshot.Core.Model.PersonAttributeValue>().GroupBy( a => a.PersonId ).ToDictionary( k => k.Key, v => v.ToList() );
            }

            using ( var slingshotFileStream = File.OpenText( Path.Combine( slingshotDirectoryName, new Slingshot.Core.Model.PersonPhone().GetFileName() ) ) )
            {
                CsvReader csvReader = new CsvReader( slingshotFileStream );
                csvReader.Configuration.HasHeaderRecord = true;
                slingshotPersonPhoneListLookup = csvReader.GetRecords<Slingshot.Core.Model.PersonPhone>().GroupBy( a => a.PersonId ).ToDictionary( k => k.Key, v => v.ToList() );
            }

            foreach ( var slingshotPerson in slingshotPersonList )
            {
                slingshotPerson.Addresses = slingshotPersonAddressListLookup.ContainsKey( slingshotPerson.Id ) ? slingshotPersonAddressListLookup[slingshotPerson.Id] : new List<Slingshot.Core.Model.PersonAddress>();
                slingshotPerson.Attributes = slingshotPersonAttributeValueListLookup.ContainsKey( slingshotPerson.Id ) ? slingshotPersonAttributeValueListLookup[slingshotPerson.Id].ToList() : new List<Slingshot.Core.Model.PersonAttributeValue>();
                slingshotPerson.PhoneNumbers = slingshotPersonPhoneListLookup.ContainsKey( slingshotPerson.Id ) ? slingshotPersonPhoneListLookup[slingshotPerson.Id].ToList() : new List<Slingshot.Core.Model.PersonPhone>();
            }

            this.SlingshotPersonList = slingshotPersonList;

            using ( var slingshotFileStream = File.OpenText( Path.Combine( slingshotDirectoryName, new Slingshot.Core.Model.PersonAttribute().GetFileName() ) ) )
            {
                CsvReader csvReader = new CsvReader( slingshotFileStream );
                csvReader.Configuration.HasHeaderRecord = true;
                this.SlingshotPersonAttributes = csvReader.GetRecords<Slingshot.Core.Model.PersonAttribute>().ToList();
            }

            using ( var slingshotFileStream = File.OpenText( Path.Combine( slingshotDirectoryName, new Slingshot.Core.Model.FamilyAttribute().GetFileName() ) ) )
            {
                CsvReader csvReader = new CsvReader( slingshotFileStream );
                csvReader.Configuration.HasHeaderRecord = true;
                this.SlingshotFamilyAttributes = csvReader.GetRecords<Slingshot.Core.Model.FamilyAttribute>().ToList();
            }

            /* Attendance */
            using ( var slingshotFileStream = File.OpenText( Path.Combine( slingshotDirectoryName, new Slingshot.Core.Model.Attendance().GetFileName() ) ) )
            {
                CsvReader csvReader = new CsvReader( slingshotFileStream );
                csvReader.Configuration.HasHeaderRecord = true;
                this.SlingshotAttendanceList = csvReader.GetRecords<Slingshot.Core.Model.Attendance>().ToList();
            }

            using ( var slingshotFileStream = File.OpenText( Path.Combine( slingshotDirectoryName, new Slingshot.Core.Model.Group().GetFileName() ) ) )
            {
                CsvReader csvReader = new CsvReader( slingshotFileStream );
                csvReader.Configuration.HasHeaderRecord = true;
                var uniqueGroups = new Dictionary<int, Slingshot.Core.Model.Group>();

                foreach ( var group in csvReader.GetRecords<Slingshot.Core.Model.Group>().ToList() )
                {
                    if ( !uniqueGroups.ContainsKey( group.Id ) )
                    {
                        uniqueGroups.Add( group.Id, group );
                    }
                }

                this.SlingshotGroupList = uniqueGroups.Select( a => a.Value ).ToList();
            }

            var groupLookup = this.SlingshotGroupList.ToDictionary( k => k.Id, v => v );
            using ( var slingshotFileStream = File.OpenText( Path.Combine( slingshotDirectoryName, new Slingshot.Core.Model.GroupMember().GetFileName() ) ) )
            {
                CsvReader csvReader = new CsvReader( slingshotFileStream );
                csvReader.Configuration.HasHeaderRecord = true;

                var groupMemberList = csvReader.GetRecords<Slingshot.Core.Model.GroupMember>().ToList().GroupBy( a => a.GroupId ).ToDictionary( k => k.Key, v => v.ToList() );
                foreach ( var groupIdMembers in groupMemberList )
                {
                    groupLookup[groupIdMembers.Key].GroupMembers = groupIdMembers.Value;
                }
            }

            using ( var slingshotFileStream = File.OpenText( Path.Combine( slingshotDirectoryName, new Slingshot.Core.Model.GroupType().GetFileName() ) ) )
            {
                CsvReader csvReader = new CsvReader( slingshotFileStream );
                csvReader.Configuration.HasHeaderRecord = true;
                this.SlingshotGroupTypeList = csvReader.GetRecords<Slingshot.Core.Model.GroupType>().ToList();
            }

            using ( var slingshotFileStream = File.OpenText( Path.Combine( slingshotDirectoryName, new Slingshot.Core.Model.Location().GetFileName() ) ) )
            {
                CsvReader csvReader = new CsvReader( slingshotFileStream );
                csvReader.Configuration.HasHeaderRecord = true;
                var uniqueLocations = new Dictionary<int, Slingshot.Core.Model.Location>();
                foreach ( var location in csvReader.GetRecords<Slingshot.Core.Model.Location>().ToList() )
                {
                    if ( !uniqueLocations.ContainsKey( location.Id ) )
                    {
                        uniqueLocations.Add( location.Id, location );
                    }
                }

                this.SlingshotLocationList = uniqueLocations.Select( a => a.Value ).ToList();
            }

            using ( var slingshotFileStream = File.OpenText( Path.Combine( slingshotDirectoryName, new Slingshot.Core.Model.Schedule().GetFileName() ) ) )
            {
                CsvReader csvReader = new CsvReader( slingshotFileStream );
                csvReader.Configuration.HasHeaderRecord = true;

                var uniqueSchedules = new Dictionary<int, Slingshot.Core.Model.Schedule>();
                foreach ( var schedule in csvReader.GetRecords<Slingshot.Core.Model.Schedule>().ToList() )
                {
                    if ( !uniqueSchedules.ContainsKey( schedule.Id ) )
                    {
                        uniqueSchedules.Add( schedule.Id, schedule );
                    }
                }

                this.SlingshotScheduleList = uniqueSchedules.Select( a => a.Value ).ToList();
            }

            /* Financial Transactions */
            using ( var slingshotFileStream = File.OpenText( Path.Combine( slingshotDirectoryName, new Slingshot.Core.Model.FinancialAccount().GetFileName() ) ) )
            {
                CsvReader csvReader = new CsvReader( slingshotFileStream );
                csvReader.Configuration.HasHeaderRecord = true;
                this.SlingshotFinancialAccountList = csvReader.GetRecords<Slingshot.Core.Model.FinancialAccount>().ToList();
            }

            using ( var slingshotFileStream = File.OpenText( Path.Combine( slingshotDirectoryName, new Slingshot.Core.Model.FinancialTransaction().GetFileName() ) ) )
            {
                CsvReader csvReader = new CsvReader( slingshotFileStream );
                csvReader.Configuration.HasHeaderRecord = true;
                this.SlingshotFinancialTransactionList = csvReader.GetRecords<Slingshot.Core.Model.FinancialTransaction>().ToList();
            }

            using ( var slingshotFileStream = File.OpenText( Path.Combine( slingshotDirectoryName, new Slingshot.Core.Model.FinancialTransactionDetail().GetFileName() ) ) )
            {
                CsvReader csvReader = new CsvReader( slingshotFileStream );
                csvReader.Configuration.HasHeaderRecord = true;
                var slingshotFinancialTransactionDetailList = csvReader.GetRecords<Slingshot.Core.Model.FinancialTransactionDetail>().ToList();
                var slingshotFinancialTransactionLookup = this.SlingshotFinancialTransactionList.ToDictionary( k => k.Id, v => v );
                foreach ( var slingshotFinancialTransactionDetail in slingshotFinancialTransactionDetailList )
                {
                    slingshotFinancialTransactionLookup[slingshotFinancialTransactionDetail.TransactionId].FinancialTransactionDetails.Add( slingshotFinancialTransactionDetail );
                }
            }

            using ( var slingshotFileStream = File.OpenText( Path.Combine( slingshotDirectoryName, new Slingshot.Core.Model.FinancialBatch().GetFileName() ) ) )
            {
                CsvReader csvReader = new CsvReader( slingshotFileStream );
                csvReader.Configuration.HasHeaderRecord = true;
                this.SlingshotFinancialBatchList = csvReader.GetRecords<Slingshot.Core.Model.FinancialBatch>().ToList();
                var transactionsByBatch = this.SlingshotFinancialTransactionList.GroupBy( a => a.BatchId ).ToDictionary( k => k.Key, v => v.ToList() );
                foreach ( var slingshotFinancialBatch in this.SlingshotFinancialBatchList )
                {
                    if ( transactionsByBatch.ContainsKey( slingshotFinancialBatch.Id ) )
                    {
                        slingshotFinancialBatch.FinancialTransactions = transactionsByBatch[slingshotFinancialBatch.Id];
                    }
                }
            }
        }

        /// <summary>
        /// Loads the lookups.
        /// </summary>
        private void LoadLookups()
        {
            RestClient restClient = GetRockRestClient();

            this.PersonRecordTypeValues = LoadDefinedValues( restClient, Rock.Client.SystemGuid.DefinedType.PERSON_RECORD_TYPE.AsGuid() );
            this.PersonRecordStatusValues = LoadDefinedValues( restClient, Rock.Client.SystemGuid.DefinedType.PERSON_RECORD_STATUS.AsGuid() );
            this.PersonConnectionStatusValues = LoadDefinedValues( restClient, Rock.Client.SystemGuid.DefinedType.PERSON_CONNECTION_STATUS.AsGuid() ).Select( a => a.Value ).ToDictionary( k => k.Value, v => v );
            this.PersonTitleValues = LoadDefinedValues( restClient, Rock.Client.SystemGuid.DefinedType.PERSON_TITLE.AsGuid() ).Select( a => a.Value ).ToDictionary( k => k.Value, v => v );
            this.PersonSuffixValues = LoadDefinedValues( restClient, Rock.Client.SystemGuid.DefinedType.PERSON_SUFFIX.AsGuid() ).Select( a => a.Value ).ToDictionary( k => k.Value, v => v );
            this.PersonMaritalStatusValues = LoadDefinedValues( restClient, Rock.Client.SystemGuid.DefinedType.PERSON_MARITAL_STATUS.AsGuid() );
            this.PhoneNumberTypeValues = LoadDefinedValues( restClient, Rock.Client.SystemGuid.DefinedType.PERSON_PHONE_TYPE.AsGuid() ).Select( a => a.Value ).ToDictionary( k => k.Value, v => v );
            this.GroupLocationTypeValues = LoadDefinedValues( restClient, Rock.Client.SystemGuid.DefinedType.GROUP_LOCATION_TYPE.AsGuid() );
            this.LocationTypeValues = LoadDefinedValues( restClient, Rock.Client.SystemGuid.DefinedType.LOCATION_TYPE.AsGuid() );
            this.CurrencyTypeValues = LoadDefinedValues( restClient, Rock.Client.SystemGuid.DefinedType.FINANCIAL_CURRENCY_TYPE.AsGuid() );
            this.TransactionSourceTypeValues = LoadDefinedValues( restClient, Rock.Client.SystemGuid.DefinedType.FINANCIAL_SOURCE_TYPE.AsGuid() );
            this.TransactionTypeValues = LoadDefinedValues( restClient, Rock.Client.SystemGuid.DefinedType.FINANCIAL_TRANSACTION_TYPE.AsGuid() );

            // EntityTypes
            RestRequest requestEntityTypes = new RestRequest( Method.GET );
            requestEntityTypes.Resource = "api/EntityTypes";
            var requestEntityTypesResponse = restClient.Execute( requestEntityTypes );
            var entityTypes = JsonConvert.DeserializeObject<List<Rock.Client.EntityType>>( requestEntityTypesResponse.Content );
            this.EntityTypeLookup = entityTypes.ToDictionary( k => k.Guid, v => v );

            int entityTypeIdPerson = this.EntityTypeLookup[Rock.Client.SystemGuid.EntityType.PERSON.AsGuid()].Id;
            int entityTypeIdGroup = this.EntityTypeLookup[Rock.Client.SystemGuid.EntityType.GROUP.AsGuid()].Id;
            int entityTypeIdAttribute = this.EntityTypeLookup[Rock.Client.SystemGuid.EntityType.ATTRIBUTE.AsGuid()].Id;

            // Family GroupTypeRoles
            RestRequest requestFamilyGroupType = new RestRequest( Method.GET );
            requestFamilyGroupType.Resource = $"api/GroupTypes?$filter=Guid eq guid'{Rock.Client.SystemGuid.GroupType.GROUPTYPE_FAMILY}'&$expand=Roles";
            var familyGroupTypeResponse = restClient.Execute( requestFamilyGroupType );
            this.FamilyRoles = JsonConvert.DeserializeObject<List<Rock.Client.GroupType>>( familyGroupTypeResponse.Content ).FirstOrDefault().Roles.ToDictionary( k => k.Guid, v => v );

            // Campuses
            RestRequest requestCampuses = new RestRequest( Method.GET );
            requestCampuses.Resource = "api/Campuses";
            var campusResponse = restClient.Execute( requestCampuses );
            this.Campuses = JsonConvert.DeserializeObject<List<Rock.Client.Campus>>( campusResponse.Content );

            // Person Attributes
            RestRequest requestPersonAttributes = new RestRequest( Method.GET );
            requestPersonAttributes.Resource = $"api/Attributes?$filter=EntityTypeId eq {entityTypeIdPerson}&$expand=FieldType";
            var personAttributesResponse = restClient.Execute( requestPersonAttributes );
            var personAttributes = JsonConvert.DeserializeObject<List<Rock.Client.Attribute>>( personAttributesResponse.Content );
            this.PersonAttributeKeyLookup = personAttributes.ToDictionary( k => k.Key, v => v );

            // Family Attributes
            this.GroupTypeIdFamily = this.FamilyRoles.Select( a => a.Value.GroupTypeId.Value ).First();
            RestRequest requestFamilyAttributes = new RestRequest( Method.GET );
            requestFamilyAttributes.Resource = $"api/Attributes?$filter=EntityTypeId eq {entityTypeIdGroup} and EntityTypeQualifierColumn eq 'GroupTypeId' and EntityTypeQualifierValue eq '{this.GroupTypeIdFamily}'&$expand=FieldType";
            var familyAttributesResponse = restClient.Execute( requestFamilyAttributes );
            var familyAttributes = JsonConvert.DeserializeObject<List<Rock.Client.Attribute>>( familyAttributesResponse.Content );
            this.FamilyAttributeKeyLookup = familyAttributes.ToDictionary( k => k.Key, v => v );

            // Attribute Categories
            RestRequest requestAttributeCategories = new RestRequest( Method.GET );
            requestAttributeCategories.Resource = $"api/Categories?$filter=EntityTypeId eq {entityTypeIdAttribute}";
            var requestAttributeCategoriesResponse = restClient.Execute( requestAttributeCategories );
            this.AttributeCategoryList = JsonConvert.DeserializeObject<List<Rock.Client.Category>>( requestAttributeCategoriesResponse.Content );

            // FieldTypes
            RestRequest requestFieldTypes = new RestRequest( Method.GET );
            requestFieldTypes.Resource = "api/FieldTypes";
            var requestFieldTypesResponse = restClient.Execute( requestFieldTypes );
            var fieldTypes = JsonConvert.DeserializeObject<List<Rock.Client.FieldType>>( requestFieldTypesResponse.Content );
            this.FieldTypeLookup = fieldTypes.ToDictionary( k => k.Class, v => v );

            // GroupTypes
            RestRequest requestGroupTypes = new RestRequest( Method.GET );
            requestGroupTypes.Resource = "api/GroupTypes?$filter=ForeignId ne null";
            var requestGroupTypesResponse = restClient.Execute( requestGroupTypes );
            var groupTypes = JsonConvert.DeserializeObject<List<Rock.Client.GroupType>>( requestGroupTypesResponse.Content );
            this.GroupTypeLookupByForeignId = groupTypes.ToDictionary( k => k.ForeignId.Value, v => v );
        }

        /// <summary>
        /// Gets the rock rest client.
        /// </summary>
        /// <returns></returns>
        private RestClient GetRockRestClient()
        {
            // TODO: Prompt for URL and Login Params
            RestClient restClient = new RestClient( this.RockUrl );

            restClient.CookieContainer = new System.Net.CookieContainer();

            RestRequest restLoginRequest = new RestRequest( Method.POST );
            restLoginRequest.RequestFormat = RestSharp.DataFormat.Json;
            restLoginRequest.Resource = "api/auth/login";
            var loginParameters = new
            {
                UserName = this.RockUserName,
                Password = this.RockPassword
            };

            restLoginRequest.AddBody( loginParameters );
            var loginResponse = restClient.Post( restLoginRequest );
            if ( loginResponse.StatusCode != System.Net.HttpStatusCode.NoContent )
            {
                throw new Exception( "Unable to login" );
            }
            else
            {
                return restClient;
            }
        }

        /// <summary>
        /// Loads the defined values.
        /// </summary>
        /// <param name="restClient">The rest client.</param>
        /// <param name="definedTypeGuid">The defined type unique identifier.</param>
        /// <returns></returns>
        private Dictionary<Guid, Rock.Client.DefinedValue> LoadDefinedValues( RestClient restClient, Guid definedTypeGuid )
        {
            RestRequest requestDefinedType = new RestRequest( Method.GET );

            requestDefinedType.Resource = $"api/DefinedTypes?$filter=Guid eq guid'{definedTypeGuid}'&$expand=DefinedValues";

            var definedTypeResponse = restClient.Execute( requestDefinedType );
            var definedValues = JsonConvert.DeserializeObject<List<Rock.Client.DefinedType>>( definedTypeResponse.Content ).FirstOrDefault().DefinedValues;

            return definedValues.ToList().ToDictionary( k => k.Guid, v => v );
        }
    }
}
