using System;
using System.Collections.Generic;
using System.Linq;
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
        private const string API_GROUPS = "/api.php?srv=group_profiles&modified_since={modifiedSince}&include_participants=true";

        #endregion

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
        /// <param name="peoplePerPage">The people per page.</param>
        public static void ExportGroups( List<int> selectedGroupTypes, DateTime modifiedSince, int peoplePerPage = 500 )
        {
            // write out the group types 
            WriteGroupTypes( selectedGroupTypes );

            // get groups
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
                    else {
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
            var numberOfMonths = (((today.Year - modifiedSince.Year) * 12) + today.Month - modifiedSince.Month) + 1;

            try {
                for ( int i = 0; i < numberOfMonths; i++ )
                {
                    DateTime referenceDate = today.AddMonths( ((numberOfMonths - i) - 1) * -1 );
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
            try {
                var request = new RestRequest( API_FINANCIAL_ACCOUNTS, Method.GET );
                var response = _client.Execute( request );

                XDocument xdocCustomFields = XDocument.Parse( response.Content );

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
                Name = "Is Baptized",
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

            var customFields = xdocCustomFields.Element( "ccb_api" )?.Element( "response" )?.Element( "custom_fields" );

            foreach ( var field in customFields.Elements( "custom_field" ) )
            {
                if ( field.Element( "label" ).Value.IsNotNullOrWhitespace() )
                {
                    var personAttribute = new PersonAttribute();
                    personAttribute.Key = field.Element( "name" ).Value;
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
                XDocument xdoc = XDocument.Parse( response.Content );

                var responseData = xdoc.Element( "ccb_api" )?.Element( "response" );

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
        /// Writes the group types.
        /// </summary>
        /// <param name="selectedGroupTypes">The selected group types.</param>
        public static void WriteGroupTypes( List<int> selectedGroupTypes )
        {
            // hardcode the department and director group types as these are baked into the box
            ImportPackage.WriteToPackage( new GroupType()
            {
                Id = 0,
                Name = "Department"
            } );

            ImportPackage.WriteToPackage( new GroupType()
            {
                Id = 0,
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
}
