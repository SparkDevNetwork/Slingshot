using Newtonsoft.Json;
using RestSharp;
using RestSharp.Authenticators;
using Slingshot.Core;
using Slingshot.Core.Model;
using Slingshot.Core.Utilities;
using Slingshot.PCO.Models;
using Slingshot.PCO.Utilities.Translators;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;

namespace Slingshot.PCO.Utilities
{
    /// <summary>
    /// PCO API Class.
    /// </summary>
    public static class PCOApi
    {
        private static RestClient _client;
        private static RestRequest _request;

        #region Properties

        /// <summary>
        /// Gets or sets the api counter.
        /// </summary>
        /// <value>
        /// The api counter.
        /// </value>
        public static int ApiCounter { get; set; } = 0;

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
        public static string ApiBaseUrl
        {
            get
            {
                return $"https://api.planningcenteronline.com";
            }
        }

        /// <summary>
        /// Gets or sets the API consumer key.
        /// </summary>
        /// <value>
        /// The API consumer key.
        /// </value>
        public static string ApiConsumerKey { get; set; }

        /// <summary>
        /// Gets or sets the API consumer secret.
        /// </summary>
        /// <value>
        /// The API consumer secret.
        /// </value>
        public static string ApiConsumerSecret { get; set; }

        /// <summary>
        /// Is Connected.
        /// </summary>
        /// <value>
        /// <c>true</c> if the connected; otherwise, <c>false</c>.
        /// </value>
        public static bool IsConnected { get; private set; } = false;

        #endregion Properties

        /// <summary>
        /// Api Endpoint Declarations.
        /// </summary>
        internal static class ApiEndpoint
        {
            internal const string API_MYSELF = "/people/v2/me";
            internal const string API_PEOPLE = "/people/v2/people";
            internal const string API_SERVICE_PEOPLE = "/services/v2/people";
            internal const string API_NOTES = "/people/v2/notes";
            internal const string API_FIELD_DEFINITIONS = "/people/v2/field_definitions";
            internal const string API_FUNDS = "/giving/v2/funds";
            internal const string API_BATCHES = "/giving/v2/batches";
            internal const string API_DONATIONS = "/giving/v2/donations";
        }

        /// <summary>
        /// Initializes the export.
        /// </summary>
        public static void InitializeExport()
        {
            ImportPackage.InitalizePackageFolder();
        }

        /// <summary>
        /// Connects to the PCO API.
        /// </summary>
        /// <param name="apiUsername">The API username.</param>
        /// <param name="apiPassword">The API password.</param>
        public static void Connect( string apiConsumerKey, string apiConsumerSecret )
        {
            ApiConsumerKey = apiConsumerKey;
            ApiConsumerSecret = apiConsumerSecret;
            ApiCounter = 0;

            var response = ApiGet( ApiEndpoint.API_MYSELF );
            IsConnected = ( response != string.Empty );
        }

        #region Private Data Access Methods

        /// <summary>
        /// Issues a GET request to the PCO API for the specified end point and returns the response.
        /// </summary>
        /// <param name="apiEndPoint">The API end point.</param>
        /// <param name="apiRequestOptions">An optional collection of request options.</param>
        /// <returns></returns>
        private static string ApiGet( string apiEndPoint, Dictionary<string, string> apiRequestOptions = null )
        {
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                var fullApiUrl = ApiBaseUrl + apiEndPoint + GetRequestQueryString( apiRequestOptions );
                _client = new RestClient( fullApiUrl );
                _client.Authenticator = new HttpBasicAuthenticator( ApiConsumerKey, ApiConsumerSecret );

                _request = new RestRequest( Method.GET );
                _request.AddHeader( "accept", "application/json" );

                var response = _client.Execute( _request );

                ApiCounter++;

                if ( response.StatusCode == HttpStatusCode.OK )
                {
                    return response.Content;
                }
                else if ( ( int ) response.StatusCode == 429 )
                {
                    // If we've got a 'too many requests' error, delay for a number of seconds specified by 'Retry-After

                    var retryAfter = response.Headers
                        .Where( h => h.Name.Equals( "Retry-After", StringComparison.InvariantCultureIgnoreCase ) )
                        .Select( x => ( ( string ) x.Value ).AsIntegerOrNull() )
                        .FirstOrDefault();

                    if ( !retryAfter.HasValue )
                    {
                        throw new Exception( "Received HTTP 429 response without 'Retry-After' header." );
                    }

                    int waitTime = ( retryAfter.Value * 1000 ) + 50; // Add 50ms to avoid clock synchronization issues.
                    PCOApi.ErrorMessage = $"Throttling API requests for {retryAfter} seconds";
                    Thread.Sleep( waitTime );

                    return ApiGet( apiEndPoint, apiRequestOptions );
                }

                // If we made it here, the response can be assumed to be an error.
                PCOApi.ErrorMessage = response.StatusCode + ": " + response.Content;
            }
            catch ( Exception ex )
            {
                PCOApi.ErrorMessage = ex.Message;
            }

            return string.Empty;
        }

        /// <summary>
        /// Converts a dictionary into a formatted query string.
        /// </summary>
        /// <param name="apiRequestOptions"></param>
        /// <returns></returns>
        private static string GetRequestQueryString( Dictionary<string, string> apiRequestOptions )
        {
            var requestQueryString = new System.Text.StringBuilder();
            apiRequestOptions = apiRequestOptions ?? new Dictionary<string, string>();

            foreach( string key in apiRequestOptions.Keys )
            {
                if ( requestQueryString.Length > 0 )
                {
                    requestQueryString.Append( "&" );
                }
                else
                {
                    requestQueryString.Append( "?" );
                }

                var name = WebUtility.UrlEncode( key );
                var value = WebUtility.UrlEncode( apiRequestOptions[key] );

                requestQueryString.Append( $"{ name }={ value }" );
            }

            return requestQueryString.ToString();
        }

        /// <summary>
        /// Gets the results of an API query for the specified API end point.
        /// </summary>
        /// <param name="apiEndPoint">The API end point.</param>
        /// <param name="apiRequestOptions">An optional collection of request options.</param>
        /// <param name="modifiedSince">The modified since.</param>
        /// <returns></returns>
        private static PCOQueryResult GetAPIQuery( string apiEndPoint, Dictionary<string, string> apiRequestOptions = null, DateTime? modifiedSince = null )
        {
            if ( modifiedSince.HasValue && apiRequestOptions != null )
            {
                // Add a parameter to sort records by last update, descending.
                apiRequestOptions.Add( "order", "-updated_at" );
            }

            string result = ApiGet( apiEndPoint, apiRequestOptions );
            if ( result.IsNullOrWhiteSpace() )
            {
                return null;
            }

            var itemsResult = JsonConvert.DeserializeObject<PCOItemsResult>( result );
            if ( itemsResult == null )
            {
                PCOApi.ErrorMessage = $"Error:  Unable to deserialize result retrieved from { apiEndPoint }.";
                throw new Exception( PCOApi.ErrorMessage );
            }

            var queryResult = new PCOQueryResult();
            queryResult.Items = new List<PCOData>();
            queryResult.IncludedItems = new List<PCOData>();

            // Add the included items collection.
            queryResult.IncludedItems.AddRange( itemsResult.IncludedItems );

            // Loop through each item in the results
            var continuePaging = true;
            foreach ( var itemResult in itemsResult.Data )
            {
                // If we're only looking for records updated after last update, and this record is older, stop processing                    
                DateTime? recordUpdatedAt = ( itemResult.Item.updated_at ?? itemResult.Item.created_at );
                if ( modifiedSince.HasValue && recordUpdatedAt.HasValue && recordUpdatedAt.Value <= modifiedSince.Value )
                {
                    continuePaging = false;
                    break;
                }

                queryResult.Items.Add( itemResult );
            }

            // If there are more page, and we should be paging
            string nextEndPoint = itemsResult.Links != null && itemsResult.Links.Next != null ? itemsResult.Links.Next : string.Empty;
            if ( nextEndPoint.IsNotNullOrWhitespace() && continuePaging )
            {
                nextEndPoint = nextEndPoint.Substring( ApiBaseUrl.Length );
                // Get the next page of results by doing a recursive call to this same method.
                // Note that nextEndPoint is supplied without the options dictionary, as those should already be specified in the result from PCO.
                var nextItems = GetAPIQuery( nextEndPoint, null, modifiedSince );
                queryResult.Items.AddRange( nextItems.Items );
                queryResult.IncludedItems.AddRange( nextItems.IncludedItems );
            }

            return queryResult;
        }

        #endregion Private Data Access Methods

        #region ExportIndividuals() and Related Methods

        /// <summary>
        /// Exports the individuals.
        /// </summary>
        /// <param name="modifiedSince">The modified since.</param>
        /// <param name="peoplePerPage">The people per page.</param>
        public static void ExportIndividuals( DateTime modifiedSince, int peoplePerPage = 100 )
        {
            var apiOptions = new Dictionary<string, string>
            {
                { "include", "emails,addresses,phone_numbers,field_data,households,inactive_reason,martial_status,name_prefix,name_suffix,primary_campus,school,social_profiles" },
                { "per_page", peoplePerPage.ToString() }
            };

            var PCOPeople = GetPeople( ApiEndpoint.API_PEOPLE, apiOptions, modifiedSince );
            var PCOServicePeople = GetServicePeople( modifiedSince );
            var PCONotes = GetNotes( modifiedSince );
            var headOfHouseholdMap = GetHeadOfHouseholdMap( PCOPeople );
            var personAttributes = WritePersonAttributes();

            foreach ( var person in PCOPeople )
            {
                PCOPerson headOfHouse = person; // Default headOfHouse to person, in case they are not assigned to a household in PCO.
                if( person.Household != null && headOfHouseholdMap.ContainsKey( person.Household.Id ) )
                {
                    headOfHouse = headOfHouseholdMap[person.Household.Id];
                }

                // The backgroundCheckPerson is pulled from a different API endpoint.
                PCOPerson backgroundCheckPerson = null;
                if ( PCOServicePeople != null )
                {
                    backgroundCheckPerson = PCOServicePeople.Where( x => x.Id == person.Id ).FirstOrDefault();
                }

                var importPerson = PCOImportPerson.Translate( person, personAttributes, headOfHouse, backgroundCheckPerson );
                if ( importPerson != null )
                {
                    ImportPackage.WriteToPackage( importPerson );
                }

                // save person image
                if ( person.Avatar.IsNotNullOrWhitespace() )
                {
                    WebClient client = new WebClient();

                    var path = Path.Combine( ImportPackage.ImageDirectory, "Person_" + person.Id + ".png" );
                    try
                    {
                        client.DownloadFile( new Uri( person.Avatar ), path );
                        ApiCounter++;
                    }
                    catch ( Exception ex )
                    {
                        Console.WriteLine( ex.Message );
                    }
                }
            }
            // save notes.
            if ( PCONotes != null )
            {
                foreach ( PCONote note in PCONotes )
                {
                    PersonNote importNote = PCOImportPersonNote.Translate( note );
                    if ( importNote != null )
                    {
                        ImportPackage.WriteToPackage( importNote );
                    }
                }
            }
        }

        /// <summary>
        /// Gets people from the services endpoint.
        /// </summary>
        /// <param name="modifiedSince">The modified since.</param>
        /// <param name="peoplePerPage">The people per page.</param>
        /// <returns></returns>
        private static List<PCOPerson> GetServicePeople( DateTime modifiedSince, int peoplePerPage = 100 )
        {
            var apiOptions = new Dictionary<string, string>
            {
                { "per_page", peoplePerPage.ToString() }
            };

            return GetPeople( ApiEndpoint.API_SERVICE_PEOPLE, apiOptions, modifiedSince );
        }

        /// <summary>
        /// Gets people from the specified endpoint.
        /// </summary>
        /// <param name="apiEndPoint">The API end point.</param>
        /// <param name="apiRequestOptions">A collection of request options.</param>
        /// <param name="modifiedSince">The modified since.</param>
        /// <returns></returns>
        public static List<PCOPerson> GetPeople( string apiEndPoint, Dictionary<string, string> apiRequestOptions, DateTime? modifiedSince )
        {
            var personQuery = GetAPIQuery( apiEndPoint, apiRequestOptions, modifiedSince );

            var people = new List<PCOPerson>();
            foreach ( var item in personQuery.Items )
            {
                var person = new PCOPerson( item, personQuery.IncludedItems );
                people.Add( person );
            }

            return people;
        }

        /// <summary>
        /// Maps household Ids to the PCOPerson object designated as the primary contact for that household.  This map method is used to avoid repetitive searches for the head of household for each household member.
        /// </summary>
        /// <param name="people">The list of <see cref="PCOPerson"/> records.</param>
        /// <returns></returns>
        private static Dictionary<int, PCOPerson> GetHeadOfHouseholdMap( List<PCOPerson> people )
        {
            var map = new Dictionary<int, PCOPerson>();

            foreach ( var person in people )
            {
                if ( person.Household != null  && !map.ContainsKey( person.Household.Id ) )
                {
                    if ( person.Household.PrimaryContactId == person.Id )
                    {
                        map.Add( person.Household.Id, person );
                    }
                }
            }

            return map;
        }

        /// <summary>
        /// Gets notes from PCO.
        /// </summary>
        /// <param name="modifiedSince">The modified since.</param>
        /// <returns></returns>
        public static List<PCONote> GetNotes( DateTime? modifiedSince )
        {
            var apiOptions = new Dictionary<string, string>
            {
                { "include", "category" }
            };

            var notesQuery = GetAPIQuery( ApiEndpoint.API_NOTES, apiOptions, modifiedSince );

            var notes = new List<PCONote>();

            foreach ( var item in notesQuery.Items )
            {
                var note = new PCONote( item, notesQuery.IncludedItems );
                notes.Add( note );
            }

            return notes;
        }

        /// <summary>
        /// Exports the person attributes.
        /// </summary>
        private static List<PCOFieldDefinition> WritePersonAttributes()
        {
            ImportPackage.WriteToPackage( new PersonAttribute()
            {
                Name = "Facebook",
                Key = "Facebook",
                Category = "Social Media",
                FieldType = "Rock.Field.Types.SocialMediaAccountFieldType"
            } );

            ImportPackage.WriteToPackage( new PersonAttribute()
            {
                Name = "Twitter",
                Key = "Twitter",
                Category = "Social Media",
                FieldType = "Rock.Field.Types.SocialMediaAccountFieldType"
            } );

            ImportPackage.WriteToPackage( new PersonAttribute()
            {
                Name = "Instagram",
                Key = "Instagram",
                Category = "Social Media",
                FieldType = "Rock.Field.Types.SocialMediaAccountFieldType"
            } );

            ImportPackage.WriteToPackage( new PersonAttribute()
            {
                Name = "LinkedIn",
                Key = "LinkedIn",
                Category = "Social Media",
                FieldType = "Rock.Field.Types.SocialMediaAccountFieldType"
            } );

            ImportPackage.WriteToPackage( new PersonAttribute()
            {
                Name = "School",
                Key = "School",
                Category = "Education",
                FieldType = "Rock.Field.Types.TextFieldType"
            } );

            ImportPackage.WriteToPackage( new PersonAttribute()
            {
                Name = "Background Check Result",
                Key = "BackgroundCheckResult",
                Category = "Safety & Security",
                FieldType = "Rock.Field.Types.TextFieldType"
            } );

            var attributes = new List<PCOFieldDefinition>();

            // export person attributes
            try
            {
                var fieldDefinitions = GetFieldDefinitions();

                foreach ( var fieldDefinition in fieldDefinitions )
                {
                    // get field type
                    var fieldtype = "Rock.Field.Types.TextFieldType";
                    if ( fieldDefinition.DataType == "text" )
                    {
                        fieldtype = "Rock.Field.Types.MemoFieldType";
                    }
                    else if ( fieldDefinition.DataType == "date" )
                    {
                        fieldtype = "Rock.Field.Types.DateFieldType";
                    }
                    else if ( fieldDefinition.DataType == "boolean" )
                    {
                        fieldtype = "Rock.Field.Types.BooleanFieldType";
                    }
                    else if ( fieldDefinition.DataType == "file" )
                    {
                        continue;
                        //fieldtype = "Rock.Field.Types.FileFieldType";
                    }
                    else if ( fieldDefinition.DataType == "number" )
                    {
                        fieldtype = "Rock.Field.Types.IntegerFieldType";
                    }

                    var newAttribute = new PersonAttribute()
                    {
                        Name = fieldDefinition.Name,
                        Key = fieldDefinition.Id + "_" + fieldDefinition.Slug,
                        Category = ( fieldDefinition.Tab == null ) ? "PCO Attributes" : fieldDefinition.Tab.Name,
                        FieldType = fieldtype,
                    };

                    ImportPackage.WriteToPackage( newAttribute );

                    attributes.Add( fieldDefinition );
                }
            }
            catch ( Exception ex )
            {
                ErrorMessage = ex.Message;
            }

            return attributes;
        }

        /// <summary>
        /// Get the field definitions from PCO.
        /// </summary>
        /// <returns></returns>
        private static List<PCOFieldDefinition> GetFieldDefinitions()
        {
            var apiOptions = new Dictionary<string, string>
            {
                { "include", "field_options,tab" }
            };

            var fieldQuery = GetAPIQuery( ApiEndpoint.API_FIELD_DEFINITIONS, apiOptions );

            var fields = new List<PCOFieldDefinition>();
            foreach ( var item in fieldQuery.Items )
            {
                var field = new PCOFieldDefinition( item, fieldQuery.IncludedItems );
                if ( field != null && field.DataType != "header" )
                {
                    fields.Add( field );
                }
            }
            return fields;
        }

        #endregion ExportIndividuals() and Related Methods

        #region ExportFinancialAccounts() and Related Methods

        /// <summary>
        /// Exports the accounts.
        /// </summary>
        public static void ExportFinancialAccounts()
        {
            try
            {
                var funds = GetFunds();

                foreach ( var fund in funds )
                {
                    var importFund = PCOImportFund.Translate( fund );
                    if( importFund != null )
                    {
                        ImportPackage.WriteToPackage( importFund );
                    }
                }
            }
            catch ( Exception ex )
            {
                ErrorMessage = ex.Message;
            }
        }

        /// <summary>
        /// Get the Funds from PCO.
        /// </summary>
        /// <returns></returns>
        private static List<PCOFund> GetFunds()
        {
            var fundQuery = GetAPIQuery( ApiEndpoint.API_FUNDS );

            var funds = new List<PCOFund>();
            foreach ( var item in fundQuery.Items )
            {
                var fund = new PCOFund( item );
                if ( fund != null )
                {
                    funds.Add( fund );
                }
            }

            return funds;
        }

        #endregion ExportFinancialAccounts() and Related Methods

        #region ExportFinancialBatches() and Related Methods

        /// <summary>
        /// Exports the batches.
        /// </summary>
        /// <param name="modifiedSince">The modified since.</param>
        public static void ExportFinancialBatches( DateTime modifiedSince )
        {
            try
            {

                var batches = GetBatches( modifiedSince );

                foreach ( var batch in batches )
                {
                    var importBatch = PCOImportBatch.Translate( batch );

                    if ( importBatch != null )
                    {
                        ImportPackage.WriteToPackage( importBatch );
                    }
                }

            }
            catch ( Exception ex )
            {
                ErrorMessage = ex.Message;
            }
        }

        /// <summary>
        /// Get the Batches from PCO.
        /// </summary>
        /// <param name="modifiedSince">The modified since.</param>
        /// <returns></returns>
        public static List<PCOBatch> GetBatches( DateTime? modifiedSince )
        {
            var apiOptions = new Dictionary<string, string>
            {
                { "include", "owner" },
                { "per_page", "100" }
            };

            var batchesQuery = GetAPIQuery( ApiEndpoint.API_BATCHES, apiOptions, modifiedSince );

            var batches = new List<PCOBatch>();
            foreach ( var item in batchesQuery.Items )
            {
                var batch = new PCOBatch( item );
                if ( batch != null )
                {
                    batches.Add( batch );
                }
            }

            return batches;
        }

        #endregion ExportFinancialBatches() and Related Methods

        #region ExportContributions() and Related Methods

        /// <summary>
        /// Exports the contributions.
        /// </summary>
        /// <param name="modifiedSince">The modified since.</param>
        public static void ExportContributions( DateTime modifiedSince )
        {
            try
            {
                var donations = GetDonations( modifiedSince )
                    .Where( d => d.Refunded == false && d.PaymentStatus == "succeeded" )
                    .ToList();

                foreach ( var donation in donations )
                {
                    var importTransaction = PCOImportDonation.Translate( donation );
                    var importTransactionDetails = PCOImportDesignation.Translate( donation );

                    if ( importTransaction != null )
                    {
                        ImportPackage.WriteToPackage( importTransaction );

                        foreach( var detail in importTransactionDetails )
                        {
                            if ( detail != null )
                            {
                                ImportPackage.WriteToPackage( detail );
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
        /// Gets Donations from PCO.
        /// </summary>
        /// <param name="modifiedSince">The modified since.</param>
        /// <returns></returns>
        private static List<PCODonation> GetDonations( DateTime? modifiedSince )
        {
            var apiOptions = new Dictionary<string, string>
            {
                { "include", "designations" },
                { "per_page", "100" }
            };

            var donationsQuery = GetAPIQuery( ApiEndpoint.API_DONATIONS, apiOptions, modifiedSince );

            var donations = new List<PCODonation>();
            foreach ( var item in donationsQuery.Items )
            {
                var donation = new PCODonation( item, donationsQuery.IncludedItems );
                if ( donation != null )
                {
                    donations.Add( donation );
                }
            }

            return donations;
        }

        #endregion ExportContributions() and Related Methods

        /// <summary>
        /// Gets the group types.
        /// </summary>
        /// <returns></returns>
        public static List<GroupType> GetGroupTypes()
        {
            List<GroupType> groupTypes = new List<GroupType>();

            // ToDo:  Implement this.

            return groupTypes;
        }



    }
}

