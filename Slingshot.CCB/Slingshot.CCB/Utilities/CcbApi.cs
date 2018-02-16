using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using RestSharp;
using RestSharp.Authenticators;
using Slingshot.CCB.Utilities.Translators;
using Slingshot.Core;
using Slingshot.Core.Model;
using Slingshot.Core.Utilities;

namespace Slingshot.CCB.Utilities
{
    /// <summary>
    /// API CCB Status
    /// </summary>
    public static class CcbApi
    {
        private static RestClient _client;
        private static int loopThreshold = 100;

        // Set CcbApi.DumpResponseToXmlFile to true to save all API Responses to XML files and include them in the slingshot package
        public static bool DumpResponseToXmlFile { get; set; }

        /// <summary>
        /// Gets or sets the last run date.
        /// </summary>
        /// <value>
        /// The last run date.
        /// </value>
        public static DateTime LastRunDate { get; set; } = DateTime.MinValue;

        /// <summary>
        /// Gets or sets the daily limit.
        /// </summary>
        /// <value>
        /// The daily limit.
        /// </value>
        public static int DailyLimit { get; set; } = -1;

        /// <summary>
        /// Gets or sets the counter.
        /// </summary>
        /// <value>
        /// The counter.
        /// </value>
        public static int Counter { get; set; } = 0;

        /// <summary>
        /// Gets or sets the domain.
        /// </summary>
        /// <value>
        /// The domain.
        /// </value>
        public static string Hostname { get; set; }

        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        /// <value>
        /// The error message.
        /// </value>
        public static string ErrorMessage { get; set; }

        /// <summary>
        /// Gets the API URL.
        /// </summary>
        /// <value>
        /// The API URL.
        /// </value>
        public static string ApiUrl
        {
            get
            {
                return $"https://{Hostname}.ccbchurch.com";
            }
        }

        /// <summary>
        /// Gets or sets the API username.
        /// </summary>
        /// <value>
        /// The API username.
        /// </value>
        public static string ApiUsername { get; set; }

        /// <summary>
        /// Gets or sets the API password.
        /// </summary>
        /// <value>
        /// The API password.
        /// </value>
        public static string ApiPassword { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance is connected.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is connected; otherwise, <c>false</c>.
        /// </value>
        public static bool IsConnected { get; private set; } = false;

        #region API Call Paths

        private const string API_STATUS = "api.php?srv=api_status";
        private const string API_INDIVIDUALS = "/api.php?srv=individual_profiles&modified_since={modifiedSince}&include_inactive=true&page={currentPage}&per_page={peoplePerPage}";
        private const string API_CUSTOM_FIELDS = "/api.php?srv=custom_field_labels";
        private const string API_FINANCIAL_ACCOUNTS = "/api.php?srv=transaction_detail_type_list";
        private const string API_FINANCIAL_BATCHES = "/api.php?srv=batch_profiles_in_date_range&date_start={startDate}&date_end={endDate}";
        private const string API_GROUP_TYPES = "/api.php?srv=group_type_list";
        private const string API_GROUPS = "/api.php?srv=group_profiles&modified_since={modifiedSince}&include_participants=true&page={currentPage}&per_page={perPage}";
        private const string API_DEPARTMENTS = "/api.php?srv=group_grouping_list";
        private const string API_EVENTS = "/api.php?srv=event_profiles&modified_since={modifiedSince}&page={currentPage}&per_page={itemsPerPage}";
        private const string API_ATTENDANCE = "/api.php?srv=attendance_profiles&start_date={startDate}&end_date={endDate}";

        #endregion API Call Paths

        /// <summary>
        /// Initializes the export.
        /// </summary>
        public static void InitializeExport()
        {
            ImportPackage.InitalizePackageFolder();
        }

        /// <summary>
        /// Connects the specified host name.
        /// </summary>
        /// <param name="hostName">Name of the host.</param>
        /// <param name="apiUsername">The API username.</param>
        /// <param name="apiPassword">The API password.</param>
        public static void Connect( string hostName, string apiUsername, string apiPassword )
        {
            Hostname = hostName;
            ApiUsername = apiUsername;
            ApiPassword = apiPassword;

            _client = new RestClient( ApiUrl );
            _client.Authenticator = new HttpBasicAuthenticator( ApiUsername, ApiPassword );
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            // getting the api status sets the IsConnect flag
            UpdateApiStatus();
        }

        /// <summary>
        /// Gets the group types.
        /// </summary>
        /// <returns></returns>
        public static List<GroupType> GetGroupTypes()
        {
            List<GroupType> groupTypes = new List<GroupType>();

            try
            {
                var request = new RestRequest( API_GROUP_TYPES, Method.GET );
                var response = _client.Execute( request );

                XDocument xdocCustomFields = XDocument.Parse( response.Content );

                if ( CcbApi.DumpResponseToXmlFile )
                {
                    xdocCustomFields.Save( Path.Combine( ImportPackage.PackageDirectory, $"API_GROUP_TYPES_ResponseLog.xml" ) );
                }

                var sourceGroupTypes = xdocCustomFields.Element( "ccb_api" )?.Element( "response" )?.Element( "items" ).Elements( "item" );

                foreach ( var sourceGroupType in sourceGroupTypes )
                {
                    var groupType = new GroupType();

                    groupType.Id = sourceGroupType.Element( "id" ).Value.AsInteger();
                    groupType.Name = sourceGroupType.Element( "name" )?.Value;

                    groupTypes.Add( groupType );
                }
            }
            catch ( Exception ex )
            {
                ErrorMessage = ex.Message;
            }

            return groupTypes;
        }

        /// <summary>
        /// Exports the groups.
        /// </summary>
        /// <param name="selectedGroupTypes">The selected group types.</param>
        /// <param name="modifiedSince">The modified since.</param>
        /// <param name="perPage">The people per page.</param>
        public static void ExportGroups( List<int> selectedGroupTypes, DateTime modifiedSince, int perPage = 500 )
        {
            // write out the group types
            WriteGroupTypes( selectedGroupTypes );

            // write departments
            ExportDeparments();

            // get groups
            try
            {
                int currentPage = 1;
                int loopCounter = 0;
                bool moreExist = true;
                while ( moreExist )
                {
                    var request = new RestRequest( API_GROUPS, Method.GET );
                    request.AddUrlSegment( "modifiedSince", modifiedSince.ToString( "yyyy-MM-dd" ) );
                    request.AddUrlSegment( "currentPage", currentPage.ToString() );
                    request.AddUrlSegment( "perPage", perPage.ToString() );

                    var response = _client.Execute( request );

                    XDocument xdoc = XDocument.Parse( response.Content );

                    if ( CcbApi.DumpResponseToXmlFile )
                    {
                        xdoc.Save( Path.Combine( ImportPackage.PackageDirectory, $"API_GROUPS_ResponseLog_{loopCounter}.xml" ) );
                    }

                    var groups = xdoc.Element( "ccb_api" )?.Element( "response" )?.Element( "groups" );

                    if ( groups != null )
                    {
                        var returnCount = groups.Attribute( "count" )?.Value.AsIntegerOrNull();

                        if ( returnCount.HasValue )
                        {
                            foreach ( var groupNode in groups.Elements() )
                            {
                                // write out the group if its type was selected for export
                                var groupTypeId = groupNode.Element( "group_type" ).Attribute( "id" ).Value.AsInteger();
                                if ( selectedGroupTypes.Contains( groupTypeId ) )
                                {
                                    var importGroups = CcbGroup.Translate( groupNode );

                                    if ( importGroups != null )
                                    {
                                        foreach ( var group in importGroups )
                                        {
                                            ImportPackage.WriteToPackage( group );
                                        }
                                    }
                                }
                            }

                            if ( returnCount != perPage )
                            {
                                moreExist = false;
                            }
                            else
                            {
                                currentPage++;
                            }
                        }
                    }
                    else
                    {
                        moreExist = false;
                    }

                    // developer safety blanket (prevents eating all the api calls for the day)
                    if ( loopCounter > loopThreshold )
                    {
                        break;
                    }
                    loopCounter++;
                }
            }
            catch ( Exception ex )
            {
                ErrorMessage = ex.Message;
            }
        }

        /// <summary>
        /// Exports the deparments.
        /// </summary>
        private static void ExportDeparments()
        {
            try
            {
                var request = new RestRequest( API_DEPARTMENTS, Method.GET );
                var response = _client.Execute( request );

                XDocument xdocCustomFields = XDocument.Parse( response.Content );

                if ( CcbApi.DumpResponseToXmlFile )
                {
                    xdocCustomFields.Save( Path.Combine( ImportPackage.PackageDirectory, $"API_DEPARTMENTS_ResponseLog.xml" ) );
                }

                var sourceDepartments = xdocCustomFields.Element( "ccb_api" )?.Element( "response" )?.Elements( "items" );

                foreach ( var sourceDepartment in sourceDepartments.Elements( "item" ) )
                {
                    var group = new Group();
                    group.Id = ( "9999" + sourceDepartment.Element( "id" ).Value ).AsInteger();
                    group.Name = sourceDepartment.Element( "name" )?.Value;
                    group.Order = sourceDepartment.Element( "order" ).Value.AsInteger();
                    group.GroupTypeId = 9999;

                    ImportPackage.WriteToPackage( group );
                }
            }
            catch ( Exception ex )
            {
                ErrorMessage = ex.Message;
            }
        }

        /// <summary>
        /// Exports the individuals.
        /// </summary>
        /// <param name="modifiedSince">The modified since.</param>
        /// <param name="peoplePerPage">The people per page.</param>
        public static void ExportIndividuals( DateTime modifiedSince, int peoplePerPage = 500 )
        {
            // write out the person attributes
            WritePersonAttributes();

            // write family attributes (always only the family photo)
            ImportPackage.WriteToPackage( new FamilyAttribute()
            {
                Name = "Family Photo",
                Key = "FamilyPhoto",
                Category = "",
                FieldType = "Rock.Field.Types.ImageFieldType"
            } );

            int currentPage = 1;
            int loopCounter = 0;
            bool moreIndividualsExist = true;

            try
            {
                while ( moreIndividualsExist )
                {
                    var request = new RestRequest( API_INDIVIDUALS, Method.GET );
                    request.AddUrlSegment( "modifiedSince", modifiedSince.ToString( "yyyy-MM-dd" ) );
                    request.AddUrlSegment( "currentPage", currentPage.ToString() );
                    request.AddUrlSegment( "peoplePerPage", peoplePerPage.ToString() );

                    var response = _client.Execute( request );

                    XDocument xdoc = XDocument.Parse( response.Content );

                    if ( CcbApi.DumpResponseToXmlFile )
                    {
                        xdoc.Save( Path.Combine( ImportPackage.PackageDirectory, $"API_INDIVIDUALS_ResponseLog_{loopCounter}.xml" ) );
                    }

                    var individuals = xdoc.Element( "ccb_api" )?.Element( "response" )?.Element( "individuals" );

                    if ( individuals != null )
                    {
                        var returnCount = individuals.Attribute( "count" )?.Value.AsIntegerOrNull();

                        if ( returnCount.HasValue )
                        {
                            foreach ( var personNode in individuals.Elements() )
                            {
                                var importPerson = CcbPerson.Translate( personNode );

                                if ( importPerson != null )
                                {
                                    ImportPackage.WriteToPackage( importPerson );
                                }
                            }

                            if ( returnCount != peoplePerPage )
                            {
                                moreIndividualsExist = false;
                            }
                            else
                            {
                                currentPage++;
                            }
                        }
                    }
                    else
                    {
                        moreIndividualsExist = false;
                    }

                    // developer safety blanket (prevents eating all the api calls for the day)
                    if ( loopCounter > loopThreshold )
                    {
                        break;
                    }
                    loopCounter++;
                }
            }
            catch ( Exception ex )
            {
                ErrorMessage = ex.Message;
            }
        }

        /// <summary>
        /// Exports the contributions.
        /// </summary>
        /// <param name="modifiedSince">The modified since.</param>
        public static void ExportContributions( DateTime modifiedSince )
        {
            // we'll make an api call for each month until the modifiedSince date
            var today = DateTime.Now;
            var numberOfMonths = ( ( ( today.Year - modifiedSince.Year ) * 12 ) + today.Month - modifiedSince.Month ) + 1;
            int loopCounter = 0;
            try
            {
                for ( int i = 0; i < numberOfMonths; i++ )
                {
                    DateTime referenceDate = today.AddMonths( ( ( numberOfMonths - i ) - 1 ) * -1 );
                    DateTime startDate = new DateTime( referenceDate.Year, referenceDate.Month, 1 );
                    DateTime endDate = new DateTime( referenceDate.Year, referenceDate.Month, DateTime.DaysInMonth( referenceDate.Year, referenceDate.Month ) );

                    // if it's the first instance set start date to the modifiedSince date
                    if ( i == 0 )
                    {
                        startDate = modifiedSince;
                    }

                    // if it's the last time through set the end dat to today's date
                    if ( i == numberOfMonths - 1 )
                    {
                        endDate = today;
                    }

                    var request = new RestRequest( API_FINANCIAL_BATCHES, Method.GET );
                    request.AddUrlSegment( "startDate", startDate.ToString( "yyyy-MM-dd" ) );
                    request.AddUrlSegment( "endDate", endDate.ToString( "yyyy-MM-dd" ) );

                    var response = _client.Execute( request );

                    XDocument xdoc = XDocument.Parse( response.Content );

                    if ( CcbApi.DumpResponseToXmlFile )
                    {
                        xdoc.Save( Path.Combine( ImportPackage.PackageDirectory, $"API_FINANCIAL_BATCHES_ResponseLog_{loopCounter++}.xml" ) );
                    }

                    var sourceBatches = xdoc.Element( "ccb_api" )?.Element( "response" )?.Element( "batches" ).Elements( "batch" );

                    foreach ( var sourceBatch in sourceBatches )
                    {
                        var importBatch = CcbFinancialBatch.Translate( sourceBatch );

                        var sourceTransactions = sourceBatch.Element( "transactions" ).Elements( "transaction" );

                        foreach ( var sourceTransaction in sourceTransactions )
                        {
                            var importTransaction = CcbFinancialTransaction.Translate( sourceTransaction, sourceBatch.Attribute( "id" ).Value.AsInteger() );

                            if ( importTransaction != null )
                            {
                                importBatch.FinancialTransactions.Add( importTransaction );

                                var sourceTransactionDetails = sourceTransaction.Element( "transaction_details" ).Elements( "transaction_detail" );
                                foreach ( var sourceTransactionDetail in sourceTransactionDetails )
                                {
                                    var importTransactionDetail = CcbFinancialTransactionDetail.Translate( sourceTransactionDetail, importTransaction.Id );

                                    if ( importTransactionDetail != null )
                                    {
                                        importTransaction.FinancialTransactionDetails.Add( importTransactionDetail );
                                    }
                                }
                            }
                        }

                        if ( importBatch != null )
                        {
                            ImportPackage.WriteToPackage( importBatch );
                        }
                    }
                }
            }
            catch ( Exception ex )
            {
                ErrorMessage = ex.Message;
            }
        }

        /// <summary>
        /// Exports the financial accounts.
        /// </summary>
        public static void ExportFinancialAccounts()
        {
            try
            {
                var request = new RestRequest( API_FINANCIAL_ACCOUNTS, Method.GET );
                var response = _client.Execute( request );

                XDocument xdocCustomFields = XDocument.Parse( response.Content );

                if ( CcbApi.DumpResponseToXmlFile )
                {
                    xdocCustomFields.Save( Path.Combine( ImportPackage.PackageDirectory, $"API_FINANCIAL_ACCOUNTS_ResponseLog.xml" ) );
                }

                var sourceAccounts = xdocCustomFields.Element( "ccb_api" )?.Element( "response" )?.Elements( "transaction_detail_types" );

                foreach ( var sourceAccount in sourceAccounts.Elements( "transaction_detail_type" ) )
                {
                    var financialAccount = new FinancialAccount();
                    financialAccount.Name = sourceAccount.Element( "name" )?.Value;
                    financialAccount.Id = (int)sourceAccount.Attribute( "id" )?.Value.AsInteger();

                    var parentAccountId = sourceAccount.Element( "parent" )?.Attribute( "id" )?.Value;
                    if ( parentAccountId.IsNotNullOrWhitespace() )
                    {
                        financialAccount.ParentAccountId = parentAccountId.AsInteger();
                    }

                    var isTaxDeductable = sourceAccount.Element( "tax_deductible" )?.Value;
                    financialAccount.IsTaxDeductible = isTaxDeductable.AsBoolean();

                    // campus
                    // ccb allows an account to have more than one campus, rock does not. if an account has more than one campus we'll just
                    // leave the campus field blank so in rock it won't be limited to just one
                    var campusList = sourceAccount.Element( "campuses" ).Elements( "campus" );
                    if ( campusList.Count() == 1 )
                    {
                        var campusId = campusList.First().Attribute( "id" )?.Value;
                        financialAccount.CampusId = campusId.AsInteger();
                    }

                    ImportPackage.WriteToPackage( financialAccount );
                }
            }
            catch ( Exception ex )
            {
                ErrorMessage = ex.Message;
            }
        }

        /// <summary>
        /// Writes the person attributes.
        /// </summary>
        public static void WritePersonAttributes()
        {
            // export person attribute list
            ImportPackage.WriteToPackage( new PersonAttribute()
            {
                Name = "Is Baptized",
                Key = "IsBaptized",
                Category = "Membership",
                FieldType = "Rock.Field.Types.BooleanFieldType"
            } );

            ImportPackage.WriteToPackage( new PersonAttribute()
            {
                Name = "Emergency Contact Name",
                Key = "EmergencyContactName",
                Category = "Safety & Security",
                FieldType = "Rock.Field.Types.TextFieldType"
            } );

            ImportPackage.WriteToPackage( new PersonAttribute()
            {
                Name = "Allergy",
                Key = "Allergy",
                Category = "Childhood Information",
                FieldType = "Rock.Field.Types.TextFieldType"
            } );

            ImportPackage.WriteToPackage( new PersonAttribute()
            {
                Name = "Confirmed No Allergies",
                Key = "ConfirmedNoAllergies",
                Category = "Childhood Information",
                FieldType = "Rock.Field.Types.BooleanFieldType"
            } );

            ImportPackage.WriteToPackage( new PersonAttribute()
            {
                Name = "Membership Date",
                Key = "MembershipDate",
                Category = "Membership",
                FieldType = "Rock.Field.Types.DateFieldType"
            } );

            // export custom fields as person attributes
            var customFieldRequest = new RestRequest( API_CUSTOM_FIELDS, Method.GET );
            var customFieldResponse = _client.Execute( customFieldRequest );

            XDocument xdocCustomFields = XDocument.Parse( customFieldResponse.Content );

            if ( CcbApi.DumpResponseToXmlFile )
            {
                xdocCustomFields.Save( Path.Combine( ImportPackage.PackageDirectory, $"API_CUSTOM_FIELDS_ResponseLog.xml" ) );
            }

            var customFields = xdocCustomFields.Element( "ccb_api" )?.Element( "response" )?.Element( "custom_fields" );

            foreach ( var field in customFields.Elements( "custom_field" ) )
            {
                if ( field.Element( "label" ).Value.IsNotNullOrWhitespace() )
                {
                    var personAttribute = new PersonAttribute();
                    personAttribute.Key = field.Element( "name" ).Value.Replace( "_ind_", "_" ); // need to strip out the '_ind' so they match what is returned from CCB on the person record
                    personAttribute.Name = field.Element( "label" ).Value;

                    if ( field.Element( "name" ).Value.Contains( "_text_" ) )
                    {
                        personAttribute.FieldType = "Rock.Field.Types.TextFieldType";
                    }
                    else if ( field.Element( "name" ).Value.Contains( "_date_" ) )
                    {
                        personAttribute.FieldType = "Rock.Field.Types.DateFieldType";
                    }

                    if ( personAttribute.FieldType.IsNotNullOrWhitespace() )
                    {
                        ImportPackage.WriteToPackage( personAttribute );
                    }
                }
            }
        }

        /// <summary>
        /// Updates the API status.
        /// </summary>
        public static void UpdateApiStatus()
        {
            var request = new RestRequest( API_STATUS, Method.GET );

            var response = _client.Execute( request );

            if ( response.StatusCode == System.Net.HttpStatusCode.OK )
            {
                XElement responseData = null;
                try
                {
                    XDocument xdoc = XDocument.Parse( response.Content );

                    responseData = xdoc.Element( "ccb_api" )?.Element( "response" );
                }
                catch ( Exception ex )
                {
                    ErrorMessage = response.Content;
                }

                if ( responseData != null )
                {
                    // check for errors
                    if ( responseData.Element( "errors" ) != null )
                    {
                        ErrorMessage = string.Empty;

                        foreach ( var errorMessage in responseData.Element( "errors" ).Elements( "error" ) )
                        {
                            ErrorMessage += errorMessage.Value;
                        }

                        IsConnected = false;
                        return;
                    }

                    if ( responseData.Element( "counter" ) != null )
                    {
                        Counter = responseData.Element( "counter" ).Value.AsInteger();
                    }

                    if ( responseData.Element( "daily_limit" ) != null )
                    {
                        DailyLimit = responseData.Element( "daily_limit" ).Value.AsInteger();
                    }
                    IsConnected = true;
                }
            }
        }

        /// <summary>
        /// Exports the attendance.
        /// </summary>
        public static void ExportAttendance( DateTime modifiedSince )
        {
            // first we need to get the 'events' so we can get the group, location and schedule information
            // since the events have a different modification date than attendance we need to load all of the
            // events so we have the details we need for the attendance
            var eventDetails = GetAttendanceEvents( new DateTime( 1900, 1, 1 ), 500 );

            // add location ids to the location fields (CCB doesn't have location ids) instead of randomly creating ids (that would not be consistant across exports)
            // we'll use a hash of the street name
            foreach ( var specificAddress in eventDetails.Select( e => new { e.LocationStreetAddress, e.LocationName } ).Distinct() )
            {
                int locationId = 1;

                if ( specificAddress.LocationName.IsNotNullOrWhitespace() || specificAddress.LocationStreetAddress.IsNotNullOrWhitespace() )
                {
                    MD5 md5Hasher = MD5.Create();
                    var hashed = md5Hasher.ComputeHash( Encoding.UTF8.GetBytes( specificAddress.LocationName + specificAddress.LocationStreetAddress ) );
                    locationId = Math.Abs( BitConverter.ToInt32( hashed, 0 ) ); // used abs to ensure positive number
                }

                foreach ( var location in eventDetails.Where( e => e.LocationStreetAddress == specificAddress.LocationStreetAddress && e.LocationName == specificAddress.LocationName ) )
                {
                    location.LocationId = locationId;
                }
            }

            // add schedule ids (CCB didn't have these either) instead of randomly creating ids (that would not be consistant across exports)
            // we'll use a hash of the schedule name
            foreach ( var specificSchedule in eventDetails.Select( e => e.ScheduleName ).Distinct() )
            {
                int scheduleId = 1;

                if ( specificSchedule.IsNotNullOrWhitespace() )
                {
                    MD5 md5Hasher = MD5.Create();
                    var hashed = md5Hasher.ComputeHash( Encoding.UTF8.GetBytes( specificSchedule ) );
                    scheduleId = Math.Abs( BitConverter.ToInt32( hashed, 0 ) ); // used abs to ensure positive number
                }

                foreach ( var location in eventDetails.Where( e => e.ScheduleName == specificSchedule ) )
                {
                    location.ScheduleId = scheduleId;
                }
            }

            // export locations, same thing with location ids we'll hash the street address
            foreach ( var location in eventDetails.Select( e => new { e.LocationName, e.LocationStreetAddress, e.LocationCity, e.LocationState, e.LocationZip, e.LocationId } ).Distinct() )
            {
                ImportPackage.WriteToPackage( new Location()
                {
                    Id = location.LocationId,
                    Name = location.LocationName,
                    Street1 = location.LocationStreetAddress,
                    City = location.LocationCity,
                    State = location.LocationState,
                    PostalCode = location.LocationZip
                } );
            }

            // export schedules
            foreach ( var schedule in eventDetails.Select( s => new { s.ScheduleId, s.ScheduleName } ).Distinct() )
            {
                ImportPackage.WriteToPackage( new Schedule()
                {
                    Id = schedule.ScheduleId,
                    Name = schedule.ScheduleName
                } );
            }

            // ok now that we have our events we can actually get the attendance data
            GetAttendance( modifiedSince, eventDetails );
        }

        private static void GetAttendance( DateTime modifiedSince, List<EventDetail> eventDetails )
        {
            // we'll make an api call for each month until the modifiedSince date
            var today = DateTime.Now;
            var numberOfMonths = ( ( ( today.Year - modifiedSince.Year ) * 12 ) + today.Month - modifiedSince.Month ) + 1;
            int loopCounter = 0;

            try
            {
                for ( int i = 0; i < numberOfMonths; i++ )
                {
                    DateTime referenceDate = today.AddMonths( ( ( numberOfMonths - i ) - 1 ) * -1 );
                    DateTime startDate = new DateTime( referenceDate.Year, referenceDate.Month, 1 );
                    DateTime endDate = new DateTime( referenceDate.Year, referenceDate.Month, DateTime.DaysInMonth( referenceDate.Year, referenceDate.Month ) );

                    // if it's the first instance set start date to the modifiedSince date
                    if ( i == 0 )
                    {
                        startDate = modifiedSince;
                    }

                    // if it's the last time through set the end dat to today's date
                    if ( i == numberOfMonths - 1 )
                    {
                        endDate = today;
                    }

                    var request = new RestRequest( API_ATTENDANCE, Method.GET );
                    request.AddUrlSegment( "startDate", startDate.ToString( "yyyy-MM-dd" ) );
                    request.AddUrlSegment( "endDate", endDate.ToString( "yyyy-MM-dd" ) );

                    var response = _client.Execute( request );

                    XDocument xdoc = XDocument.Parse( response.Content );

                    if ( CcbApi.DumpResponseToXmlFile )
                    {
                        xdoc.Save( Path.Combine( ImportPackage.PackageDirectory, $"API_ATTENDANCE_ResponseLog_{loopCounter++}.xml" ) );
                    }

                    var sourceEvents = xdoc.Element( "ccb_api" )?.Element( "response" )?.Element( "events" ).Elements( "event" );

                    if ( sourceEvents != null )
                    {
                        foreach ( var sourceEvent in sourceEvents )
                        {
                            var attendances = CcbAttendance.Translate( sourceEvent, eventDetails );

                            if ( attendances != null )
                            {
                                foreach ( var attendance in attendances )
                                {
                                    ImportPackage.WriteToPackage( attendance );
                                }
                            }
                        }
                    }
                }
            }
            catch ( Exception ex )
            {
                ErrorMessage = ex.Message;
            }
        }

        /// <summary>
        /// Gets the attendance events.
        /// </summary>
        /// <param name="modifiedSince">The modified since.</param>
        /// <param name="itemsPerPage">The items per page.</param>
        /// <returns></returns>
        private static List<EventDetail> GetAttendanceEvents( DateTime modifiedSince, int itemsPerPage )
        {
            List<EventDetail> eventDetails = new List<EventDetail>();

            int currentPage = 1;
            int loopCounter = 0;
            bool moreItemsExist = true;

            try
            {
                while ( moreItemsExist )
                {
                    var request = new RestRequest( API_EVENTS, Method.GET );
                    request.AddUrlSegment( "modifiedSince", modifiedSince.ToString( "yyyy-MM-dd" ) );
                    request.AddUrlSegment( "currentPage", currentPage.ToString() );
                    request.AddUrlSegment( "itemsPerPage", itemsPerPage.ToString() );

                    var response = _client.Execute( request );

                    if ( response.StatusCode == System.Net.HttpStatusCode.OK )
                    {
                        XDocument xdoc = XDocument.Parse( response.Content );

                        if ( CcbApi.DumpResponseToXmlFile )
                        {
                            xdoc.Save( Path.Combine( ImportPackage.PackageDirectory, $"API_EVENTS_ResponseLog_{loopCounter++}.xml" ) );
                        }

                        var returnCount = xdoc.Element( "ccb_api" )?.Element( "response" )?.Element( "events" )?.Attribute( "count" )?.Value.AsIntegerOrNull();

                        var events = xdoc.Element( "ccb_api" )?.Element( "response" )?.Element( "events" )?.Elements( "event" );

                        foreach ( var eventItem in events )
                        {
                            var eventDetail = new EventDetail();
                            eventDetails.Add( eventDetail );

                            eventDetail.EventId = eventItem.Attribute( "id" ).Value.AsInteger();
                            eventDetail.GroupId = eventItem.Element( "group" ).Attribute( "id" ).Value.AsInteger();
                            eventDetail.ScheduleName = eventItem.Element( "recurrence_description" ).Value;

                            if ( eventItem.Element( "location" ) != null && eventItem.Element( "location" ).HasElements )
                            {
                                eventDetail.LocationName = eventItem.Element( "location" ).Element( "name" ).Value;
                                eventDetail.LocationStreetAddress = eventItem.Element( "location" ).Element( "street_address" ).Value;
                                eventDetail.LocationCity = eventItem.Element( "location" ).Element( "city" ).Value;
                                eventDetail.LocationState = eventItem.Element( "location" ).Element( "state" ).Value;
                                eventDetail.LocationZip = eventItem.Element( "location" ).Element( "zip" ).Value;
                            }
                        }

                        if ( returnCount != itemsPerPage )
                        {
                            moreItemsExist = false;
                        }
                        else
                        {
                            currentPage++;
                        }
                    }

                    // developer safety blanket (prevents eating all the api calls for the day)
                    if ( loopCounter > loopThreshold )
                    {
                        break;
                    }
                    loopCounter++;
                }
            }
            catch ( Exception ex )
            {
                ErrorMessage = ex.Message;
            }

            return eventDetails;
        }

        /// <summary>
        /// Writes the group types.
        /// </summary>
        /// <param name="selectedGroupTypes">The selected group types.</param>
        public static void WriteGroupTypes( List<int> selectedGroupTypes )
        {
            // hardcode the department and director group types as these are baked into the box
            ImportPackage.WriteToPackage( new GroupType()
            {
                Id = 9999,
                Name = "Department"
            } );

            ImportPackage.WriteToPackage( new GroupType()
            {
                Id = 9998,
                Name = "Director"
            } );

            // add custom defined group types
            var groupTypes = GetGroupTypes();
            foreach ( var groupType in groupTypes.Where( t => selectedGroupTypes.Contains( t.Id ) ) )
            {
                ImportPackage.WriteToPackage( new GroupType()
                {
                    Id = groupType.Id,
                    Name = groupType.Name
                } );
            }
        }
    }

    /// <summary>
    /// Temporary class for assembling attendance information
    /// </summary>
    public class EventDetail
    {
        /// <summary>
        /// Gets or sets the event identifier.
        /// </summary>
        /// <value>
        /// The event identifier.
        /// </value>
        public int EventId { get; set; }

        /// <summary>
        /// Gets or sets the schedule identifier.
        /// </summary>
        /// <value>
        /// The schedule identifier.
        /// </value>
        public int ScheduleId { get; set; }

        /// <summary>
        /// Gets or sets the name of the schedule.
        /// </summary>
        /// <value>
        /// The name of the schedule.
        /// </value>
        public string ScheduleName { get; set; }

        /// <summary>
        /// Gets or sets the group identifier.
        /// </summary>
        /// <value>
        /// The group identifier.
        /// </value>
        public int GroupId { get; set; }

        /// <summary>
        /// Gets or sets the location identifier.
        /// </summary>
        /// <value>
        /// The location identifier.
        /// </value>
        public int LocationId { get; set; }

        /// <summary>
        /// Gets or sets the name of the location.
        /// </summary>
        /// <value>
        /// The name of the location.
        /// </value>
        public string LocationName { get; set; }

        /// <summary>
        /// Gets or sets the location street address.
        /// </summary>
        /// <value>
        /// The location street address.
        /// </value>
        public string LocationStreetAddress { get; set; }

        /// <summary>
        /// Gets or sets the location city.
        /// </summary>
        /// <value>
        /// The location city.
        /// </value>
        public string LocationCity { get; set; }

        /// <summary>
        /// Gets or sets the state of the location.
        /// </summary>
        /// <value>
        /// The state of the location.
        /// </value>
        public string LocationState { get; set; }

        /// <summary>
        /// Gets or sets the location zip.
        /// </summary>
        /// <value>
        /// The location zip.
        /// </value>
        public string LocationZip { get; set; }
    }
}