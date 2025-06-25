using Newtonsoft.Json;
using RestSharp;
using RestSharp.Authenticators;
using Slingshot.Core;
using Slingshot.Core.Utilities;
using Slingshot.PCO.Models.ApiModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;

namespace Slingshot.PCO.Utilities
{
    /// <summary>
    /// PCO API Class.
    /// </summary>
    public static partial class PCOApi
    {
        private static RestClient _client;
        private static RestRequest _request;

        #region Properties

        /// <summary>
        /// Gets or sets the number of seconds the api was throttled by rate limiting.
        /// </summary>
        public static int ApiThrottleSeconds { get; private set; } = 0;

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

        /// <summary>
        /// Tracks the maximum assigned group attendance id to prevent overlapping records with check-in attendance.
        /// </summary>
        public static int MaxGroupAttendanceId { get; private set; } = 0;

        #endregion Properties

        /// <summary>
        /// Api Endpoint Declarations.
        /// </summary>
        internal static partial class ApiEndpoint
        {
            internal const string API_MYSELF = "/people/v2/me";
        }

        /// <summary>
        /// Initializes the export.
        /// </summary>
        public static void InitializeExport()
        {
            PCOApi.ErrorMessage = string.Empty;
            PCOApi.ApiThrottleSeconds = 0;
            ImportPackage.InitializePackageFolder();
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
        /// <param name="apiEndpoint">The API end point.</param>
        /// <param name="apiRequestOptions">An optional collection of request options.</param>
        /// <param name="ignoreApiErrors">[true] if API errors should be ignored.</param>
        /// <returns></returns>
        private static string ApiGet( string apiEndpoint, Dictionary<string, string> apiRequestOptions = null, bool ignoreApiErrors = false )
        {
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                var fullApiUrl = ApiBaseUrl + apiEndpoint + GetRequestQueryString( apiRequestOptions );
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
                else if ( response.StatusCode == HttpStatusCode.Forbidden && !ignoreApiErrors )
                {
                    throw new Exception( $"Forbidden request: { apiEndpoint } | Message: { response.ErrorMessage } | Exception: { response.ErrorException }" );
                }
                else if ( ( int ) response.StatusCode == 429 )
                {
                    // If we've got a 'too many requests' error, delay for a number of seconds specified by 'Retry-After

                    var retryAfter = response.Headers
                        .Where( h => h.Name.Equals( "Retry-After", StringComparison.InvariantCultureIgnoreCase ) )
                        .Select( x => ( ( string ) x.Value ).AsIntegerOrNull() )
                        .FirstOrDefault();

                    if ( !retryAfter.HasValue && !ignoreApiErrors )
                    {
                        throw new Exception( "Received HTTP 429 response without 'Retry-After' header." );
                    }

                    int waitTime = ( retryAfter.Value * 1000 ) + 50; // Add 50ms to avoid clock synchronization issues.
                    PCOApi.ApiThrottleSeconds += retryAfter.Value;
                    Thread.Sleep( waitTime );

                    return ApiGet( apiEndpoint, apiRequestOptions, ignoreApiErrors );
                }

                // If we made it here, the response can be assumed to be an error.
                if ( !ignoreApiErrors )
                {
                    PCOApi.ErrorMessage = response.StatusCode + ": " + response.Content;
                }
            }
            catch ( Exception ex )
            {
                if ( !ignoreApiErrors )
                {
                    PCOApi.ErrorMessage = ex.Message;
                }
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
        /// <param name="apiEndpoint">The API end point.</param>
        /// <param name="apiRequestOptions">An optional collection of request options.</param>
        /// <param name="modifiedSince">The modified since.</param>
        /// <param name="existingResults">Previous results for this request that should be combined (for paging purposes).</param>
        /// <param name="ignoreApiErrors">[true] if API errors should be ignored.</param>
        /// <returns></returns>
        private static PCOApiQueryResult GetAPIQuery( string apiEndpoint, Dictionary<string, string> apiRequestOptions = null, DateTime? modifiedSince = null, PCOApiQueryResult existingResults = null, bool ignoreApiErrors = false )
        {
            if ( modifiedSince.HasValue && apiRequestOptions != null )
            {
                // Add a parameter to sort records by last update, descending.
                apiRequestOptions.Add( "order", "-updated_at" );
            }

            string result = ApiGet( apiEndpoint, apiRequestOptions, ignoreApiErrors );
            if ( result.IsNullOrWhiteSpace() )
            {
                return null;
            }

            result = result.CleanResult();

            var itemsResult = JsonConvert.DeserializeObject<QueryItems>( result );
            if ( itemsResult == null )
            {
                PCOApi.ErrorMessage = $"Error:  Unable to deserialize result retrieved from { apiEndpoint }.";
                throw new Exception( PCOApi.ErrorMessage );
            }


            PCOApiQueryResult queryResult;
            if ( existingResults != null )
            {
                queryResult = new PCOApiQueryResult( existingResults, itemsResult.IncludedItems );
            }
            else
            {
                queryResult = new PCOApiQueryResult( itemsResult.IncludedItems );
            }

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
            string nextEndpoint = itemsResult.Links != null && itemsResult.Links.Next != null ? itemsResult.Links.Next : string.Empty;
            if ( nextEndpoint.IsNotNullOrWhitespace() && continuePaging )
            {
                nextEndpoint = nextEndpoint.Substring( ApiBaseUrl.Length );
                // Get the next page of results by doing a recursive call to this same method.
                // Note that nextEndpoint is supplied without the options dictionary, as those should already be specified in the result from PCO.
                return GetAPIQuery( nextEndpoint, null, modifiedSince, queryResult );
            }

            return queryResult;
        }

        /// <summary>
        /// Replaces id values of "unique" with -1 to allow JSON deserialization.
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        private static string CleanResult( this string result )
        {
            return result.Replace( "\"id\":\"unique\"", "\"id\":\"-1\"" );
        }

        #endregion Private Data Access Methods
    }
}

