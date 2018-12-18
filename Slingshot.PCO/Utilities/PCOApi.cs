using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using System.Net;

using RestSharp;
using RestSharp.Authenticators;
using RestSharp.Authenticators.OAuth;
using RestSharp.Extensions.MonoHttp;

using Newtonsoft.Json;

using Slingshot.Core;
using Slingshot.Core.Model;
using Slingshot.Core.Utilities;
using Slingshot.PCO.Utilities.Translators;
using Slingshot.PCO.Models;

namespace Slingshot.PCO.Utilities
{
    /// <summary>
    /// API F1 Status
    /// </summary>
    public static class PCOApi
    {
        private static RestClient _client;
        private static RestRequest _request;

        /// <summary>
        ///  Set F1Api.DumpResponseToXmlFile to true to save all API Responses 
        ///   to XML files and include them in the slingshot package
        /// </summary>
        /// <value>
        /// <c>true</c> if the response should get dumped to XML; otherwise, <c>false</c>.
        /// </value>
        public static bool DumpResponseToXmlFile { get; set; }

        /// <summary>
        /// Gets or sets the last run date.
        /// </summary>
        /// <value>
        /// The last run date.
        /// </value>
        public static DateTime LastRunDate { get; set; } = DateTime.MinValue;

        /// <summary>
        /// Gets or sets the api counter.
        /// </summary>
        /// <value>
        /// The api counter.
        /// </value>
        public static int ApiCounter { get; set; } = 0;

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
                return $"https://api.planningcenteronline.com/";
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
        /// Gets or sets the oauth token.
        /// </summary>
        /// <value>
        /// The oauth token.
        /// </value>

        public static bool IsConnected { get; private set; } = false;

        #region API Call Paths 

        private const string API_ACCESS_TOKEN = "/v1/PortalUser/AccessToken";
        private const string API_PEOPLE = "https://api.planningcenteronline.com/people/v2/people";
        private const string API_INDIVIDUALS_REQUIREMENTS = "/v1/Requirements";
        private const string API_ATTRIBUTE_GROUPS = "/v1/people/attributegroups";
        private const string API_ACCOUNTS = "/giving/v1/funds";
        private const string API_BATCH_TYPES = "/giving/v1/batches/batchtypes";
        private const string API_BATCHES = "/giving/v1/batches/search";
        private const string API_CONTRIBUTION_RECEIPTS = "/giving/v1/contributionreceipts/search";
        private const string API_CONTRIBUTION_RECEIPT_IMAGE = "/giving/v1/referenceimages/";
        private const string API_GROUP_TYPES = "/groups/v1/grouptypes";
        private const string API_GROUPS = "/groups/v1/grouptypes/";
        private const string API_GROUP_MEMBERS = "/groups/v1/groups/";

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
        public static void Connect( string apiConsumerKey, string apiConsumerSecret )
        {
            ApiConsumerKey = apiConsumerKey;
            ApiConsumerSecret = apiConsumerSecret;

            ApiCounter = 0;

            var me = ApiGet( "https://api.planningcenteronline.com/people/v2/me" );

            if ( me != string.Empty )
            {
                IsConnected = true;
            }
            else
            {
                IsConnected = false;
            }
        }

        private static string ApiGet( string apiEndPoint )
        {
            try
            {
                _client = new RestClient( apiEndPoint );
                _request = new RestRequest( Method.GET );
                _client.Authenticator = new HttpBasicAuthenticator( ApiConsumerKey, ApiConsumerSecret );
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
                    System.Threading.Thread.Sleep( 60000 ); //Wait 1 minute

                    _client = new RestClient( apiEndPoint );
                    _request = new RestRequest( Method.GET );
                    _client.Authenticator = OAuth1Authenticator.ForRequestToken( ApiConsumerKey, ApiConsumerSecret );
                    _request.AddHeader( "accept", "application/json" );

                    response = _client.Execute( _request );

                    ApiCounter++;

                    if ( response.StatusCode == HttpStatusCode.OK )
                    {
                        return response.Content;
                    }
                    else
                    {
                        PCOApi.ErrorMessage = response.StatusCode + ": " + response.Content;
                    }
                }
                else
                {
                    PCOApi.ErrorMessage = response.StatusCode + ": " + response.Content;
                }

            }
            catch ( Exception ex )
            {
                PCOApi.ErrorMessage = ex.Message;
            }

            return string.Empty;
        }

        /// <summary>
        /// Exports the individuals.
        /// </summary>
        /// <param name="modifiedSince">The modified since.</param>
        /// <param name="peoplePerPage">The people per page.</param>
        public static void ExportIndividuals( DateTime modifiedSince, int peoplePerPage = 100 )
        {

            var personAttributes = WritePersonAttributes();

            var PCOPeople = GetPeople( false, modifiedSince, API_PEOPLE + "?include=emails,addresses,phone_numbers,field_data,households,inactive_reason,martial_status,name_prefix,name_suffix,primary_campus,school,social_profiles&per_page=" + peoplePerPage );
            var PCOServicePeople = GetPeople( false, modifiedSince, "https://api.planningcenteronline.com/services/v2/people?per_page=" + peoplePerPage );
            var PCONotes = GetNotes( false, modifiedSince, null );

            foreach ( PCOPerson p in PCOPeople )
            {

                /*  For debugging issues with a given person
                if ( p.id == 29004938 )
                {
                    //Debug this profile
                    Console.WriteLine();
                }
                */

                PCOPerson headOfHouse;
                if( p.household is null )
                {
                    headOfHouse = p;
                }
                else
                {
                    headOfHouse = PCOPeople.Where( x => x.id == p.household.primary_contact_id ).FirstOrDefault();
                }
                
                if ( headOfHouse == null )
                {
                    headOfHouse = p;
                }
                var importPerson = PCOImportPerson.Translate( p, personAttributes, headOfHouse );

                if ( importPerson != null )
                {
                    if ( PCOServicePeople != null )
                    {
                        PCOPerson backgroundCheckPerson = PCOServicePeople.Where( x => x.id == p.id ).FirstOrDefault();
                        if ( backgroundCheckPerson != null 
                                && backgroundCheckPerson.passed_background_check.HasValue 
                                && backgroundCheckPerson.passed_background_check.Value 
                           )
                        {
                            importPerson = PCOImportPerson.AddBackgroundCheckResult( importPerson, backgroundCheckPerson );
                        }
                    }
                   
                    ImportPackage.WriteToPackage( importPerson );
                }

                // save person image
                if ( p.avatar.IsNotNullOrWhitespace() )
                {
                    WebClient client = new WebClient();

                    var path = Path.Combine( ImportPackage.ImageDirectory, "Person_" + p.id + ".png" );

                    try
                    {
                        client.DownloadFile( new Uri( p.avatar ), path );
                        ApiCounter++;
                    }
                    catch (Exception ex)
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
        /// Exports the accounts.
        /// </summary>
        public static void ExportFinancialAccounts()
        {
            try
            {
                var funds = GetFunds( false, null, null );

                /*
                XDocument xdoc = XDocument.Parse( response.Content );

                if ( PCOApi.DumpResponseToXmlFile )
                {
                    xdoc.Save( Path.Combine( ImportPackage.PackageDirectory, $"API_FINANCIAL_ACCOUNTS_ResponseLog.xml" ) );
                }
                */

                // process accounts
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
        /// Exports the pledges.
        /// </summary>
        /// 

        /*  PCO Doesn't Have Pledges
        public static void ExportFinancialPledges()
        {
            int loopCounter = 0;
            try
            {
                foreach ( var accountId in AccountIds )
                {
                    //_client.Authenticator = OAuth1Authenticator.ForProtectedResource( ApiConsumerKey, ApiConsumerSecret, OAuthToken, OAuthSecret );
                    _request = new RestRequest( API_ACCOUNTS + "/" + accountId.ToString() + "/pledgedrives", Method.GET );
                    _request.AddHeader( "content-type", "application/xml" );
                    var response = _client.Execute( _request );
                    ApiCounter++;

                    var xdoc = XDocument.Parse( response.Content );

                    if ( PCOApi.DumpResponseToXmlFile )
                    {
                        xdoc.Save( Path.Combine( ImportPackage.PackageDirectory, $"API_FINANCIAL_PLEDGES_ResponseLog_{loopCounter}.xml" ) );
                    }

                    var pledges = xdoc.Element( "pledgeDrives" );

                    foreach ( var pledgeNode in pledges.Elements() )
                    {
                        
                        var importPledge = F1FinancialPledge.Translate( pledgeNode );

                        if ( importPledge != null )
                        {
                            ImportPackage.WriteToPackage( importPledge );
                        }
                        
                    }

                    loopCounter++;
                }
            }
            catch ( Exception ex )
            {
                ErrorMessage = ex.Message;
            }

        }

        */

        /// <summary>
        /// Exports the batches.
        /// </summary>
        /// <param name="modifiedSince">The modified since.</param>
        public static void ExportFinancialBatches( DateTime modifiedSince )
        {
            try
            {

                var batches = GetBatches( false, modifiedSince, null );

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
        /// Exports the contributions.
        /// </summary>
        /// <param name="modifiedSince">The modified since.</param>
        public static void ExportContributions( DateTime modifiedSince, bool exportContribImages )
        {
            try
            {
                var donations = GetDonations( false, modifiedSince, null )
                                    .Where(d => d.refunded == false && d.payment_status == "succeeded")
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
        /// Exports the groups.
        /// </summary>
        /// <param name="selectedGroupTypes">The selected group types.</param>
        /// <param name="modifiedSince">The modified since.</param>
        /// <param name="perPage">The people per page.</param>

        /* NOT Implemented Yet
        public static void ExportGroups( List<int> selectedGroupTypes )
        {
            // write out the group types 
            WriteGroupTypes( selectedGroupTypes );

            // get groups
            try
            {
                int loopCounter = 0;

                foreach ( var selectedGroupType in selectedGroupTypes )
                {
                    //_client.Authenticator = OAuth1Authenticator.ForProtectedResource( ApiConsumerKey, ApiConsumerSecret, OAuthToken, OAuthSecret );
                    _request = new RestRequest( API_GROUPS + selectedGroupType.ToString() + "/groups", Method.GET );
                    _request.AddHeader( "content-type", "application/xml" );

                    var response = _client.Execute( _request );
                    ApiCounter++;

                    XDocument xdoc = XDocument.Parse( response.Content );

                    var groups = xdoc.Elements( "groups" );

                    if ( groups.Elements().Any() )
                    {
                        // since we don't have a group heirarchy to work with, add a parent
                        //  group for each group type for organizational purposes
                        int parentGroupId = 99 + groups.Elements().FirstOrDefault().Element( "groupType" ).Attribute( "id" ).Value.AsInteger();

                        ImportPackage.WriteToPackage( new Group()
                        {
                            Id = parentGroupId,
                            Name = groups.Elements().FirstOrDefault().Element( "groupType" ).Element( "name" ).Value,
                            GroupTypeId = groups.Elements().FirstOrDefault().Element( "groupType" ).Attribute( "id" ).Value.AsInteger()
                        } );

                        foreach ( var groupNode in groups.Elements() )
                        {
                            string groupId = groupNode.Attribute( "id" ).Value;

                            //_client.Authenticator = OAuth1Authenticator.ForProtectedResource( ApiConsumerKey, ApiConsumerSecret, OAuthToken, OAuthSecret );
                            _request = new RestRequest( API_GROUP_MEMBERS + groupId + "/members", Method.GET );
                            _request.AddHeader( "content-type", "application/xml" );

                            response = _client.Execute( _request );
                            ApiCounter++;

                            xdoc = XDocument.Parse( response.Content );

                            if ( PCOApi.DumpResponseToXmlFile )
                            {
                                xdoc.Save( Path.Combine( ImportPackage.PackageDirectory, $"API_GROUPS_ResponseLog_{loopCounter}.xml" ) );
                            }

                            var membersNode = xdoc.Elements( "members" );
                            /*
                            var importGroup = F1Group.Translate( groupNode, parentGroupId, membersNode );

                            if ( importGroup != null )
                            {
                                ImportPackage.WriteToPackage( importGroup );
                            }

                            // developer safety blanket (prevents eating all the api calls for the day) 
                            if ( loopCounter > loopThreshold )
                            {
                                break;
                            }
                            loopCounter++;
                           
                        }
                    }
                }
            }
            catch ( Exception ex )
            {
                ErrorMessage = ex.Message;
            }

        }
        */
        /// <summary>
        /// Exports the person attributes.
        /// </summary>
        public static List<PCOFieldDefinition> WritePersonAttributes()
        {
            // export person fields as attributes
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

                var Fields = GetFields( false, null, null );

                foreach ( var field in Fields )
                {
                    // get field type
                    var fieldtype = "Rock.Field.Types.TextFieldType";
                    if ( field.data_type == "text" )
                    {
                        fieldtype = "Rock.Field.Types.MemoFieldType";
                    }
                    else if ( field.data_type == "date" )
                    {
                        fieldtype = "Rock.Field.Types.DateFieldType";
                    }
                    else if ( field.data_type == "boolean" )
                    {
                        fieldtype = "Rock.Field.Types.BooleanFieldType";
                    }
                    else if ( field.data_type == "file" )
                    {
                        continue;
                        //fieldtype = "Rock.Field.Types.FileFieldType";
                    }
                    else if ( field.data_type == "number" )
                    {
                        fieldtype = "Rock.Field.Types.IntegerFieldType";
                    }

                    var newAttribute = new PersonAttribute()
                    {
                        Name = field.name,
                        Key = field.id + "_" + field.slug,
                        Category = "PCO Attributes",
                        FieldType = fieldtype,
                    };

                    ImportPackage.WriteToPackage( newAttribute );

                    // Add the attributes to the list
                    attributes.Add( field );

                }
            }
            catch ( Exception ex )
            {
                ErrorMessage = ex.Message;
            }

            return attributes;
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
                //_client.Authenticator = OAuth1Authenticator.ForProtectedResource( ApiConsumerKey, ApiConsumerSecret, OAuthToken, OAuthSecret );
                _request = new RestRequest( API_GROUP_TYPES, Method.GET );
                _request.AddHeader( "content-type", "application/xml" );

                var response = _client.Execute( _request );
                ApiCounter++;

                XDocument xdoc = XDocument.Parse( response.Content );

                if ( PCOApi.DumpResponseToXmlFile )
                {
                    xdoc.Save( Path.Combine( ImportPackage.PackageDirectory, $"API_GROUP_TYPES_ResponseLog.xml" ) );
                }

                var sourceGroupTypes = xdoc.Element( "groupTypes" );

                foreach ( var sourceGroupType in sourceGroupTypes.Elements() )
                {
                    var groupType = new GroupType();

                    groupType.Id = sourceGroupType.Attribute( "id" ).Value.AsInteger();
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
        /// Writes the group types.
        /// </summary>
        /// <param name="selectedGroupTypes">The selected group types.</param>
        public static void WriteGroupTypes( List<int> selectedGroupTypes )
        {
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

        private static bool PCOGetItems( string apiEndPoint, List<PCOData> dataItems, List<PCOData> includedItems, DateTime? updatedAfter, bool continuePaging, string saveEndPointAs, string saveNextEndPointAs )
        {
            // Save the current endpoing
            if ( saveEndPointAs.IsNotNullOrWhitespace() )
            {
                saveEndPointAs = apiEndPoint;
            }

            // Verify that the result dictionaries are not null
            dataItems = dataItems ?? new List<PCOData>();
            includedItems = includedItems ?? new List<PCOData>();

            // Query Planning Center
            string result = ApiGet( apiEndPoint );

            // If we got results
            if ( !string.IsNullOrWhiteSpace( result ) )
            {
                // Check to see if we can deserialize the data
                var searchResult = JsonConvert.DeserializeObject<PCOItemsResult>( result );
                if ( searchResult != null )
                {
                    // Save the next endpoint
                    string nextEndPoint = searchResult.links != null && searchResult.links.next != null ? searchResult.links.next : string.Empty;
                    if ( nextEndPoint.IsNotNullOrWhitespace() && saveNextEndPointAs.IsNotNullOrWhitespace() )
                    {
                        saveNextEndPointAs = nextEndPoint;
                    }

                    includedItems.AddRange( searchResult.included );

                    // Loop through each item in the results
                    foreach ( var dataItem in searchResult.data )
                    {
                        // Get the last updated or created date
                        DateTime? recordUpdatedAt = ( dataItem.Item.updated_at ?? dataItem.Item.created_at );

                        // If we're only looking for records updated after last update, and this record is older, stop processing                    
                        if ( updatedAfter.HasValue && recordUpdatedAt.HasValue && recordUpdatedAt.Value <= updatedAfter.Value )
                        {
                            continuePaging = false;
                            break;
                        }

                        // Append the results to the dictionaries
                        dataItems.Add( dataItem );
                    }

                    // If there are more page, and we should be paging
                    if ( nextEndPoint.IsNotNullOrWhitespace() && continuePaging )
                    {
                        // Get the next page of results by doing a recursive call to this same method.
                        return PCOGetItems( nextEndPoint, dataItems, includedItems, updatedAfter, true, saveEndPointAs, saveNextEndPointAs );
                    }

                    // We're good
                    return true;
                }
            }

            // We didn't get what we expected so return false
            return false;
        }

        public static List<PCOFund> GetFunds( bool importing, DateTime? updatedAfter, string apiEndPoint )
        {
            // Create variables to store the results 
            var dataItems = new List<PCOData>();
            var includedItems = new List<PCOData>();

            // Set the endpoint if not passed from parameter
            apiEndPoint = apiEndPoint ?? "https://api.planningcenteronline.com/giving/v2/funds";

            // Query Planning Center for people
            if ( PCOGetItems( apiEndPoint, dataItems, includedItems, updatedAfter,
                !importing, "", "" ) )
            {
                // Create variable to store the people
                var funds = new List<PCOFund>();

                // Loop through each item in the result of api call
                foreach ( var item in dataItems )
                {
                    // Create the person record
                    var fund = new PCOFund( item );
                    if ( fund != null )
                    {
                        // Add to list of results
                        funds.Add( fund );

                    }
                }

                // return the list of people
                return funds;
            }

            // An error occurred trying to query people so return null
            return null;
        }

        public static List<PCOBatch> GetBatches( bool importing, DateTime? updatedAfter, string apiEndPoint )
        {
            // Create variables to store the results 
            var dataItems = new List<PCOData>();
            var includedItems = new List<PCOData>();

            // Set the endpoint if not passed from parameter
            apiEndPoint = apiEndPoint ?? "https://api.planningcenteronline.com/giving/v2/batches?include=owner&per_page=100";

            // Query Planning Center for people
            if ( PCOGetItems( apiEndPoint, dataItems, includedItems, updatedAfter,
                !importing, "", "" ) )
            {
                // Create variable to store the people
                var batches = new List<PCOBatch>();

                // Loop through each item in the result of api call
                foreach ( var item in dataItems )
                {
                    // Create the person record
                    var batch = new PCOBatch( item );
                    if ( batch != null )
                    {
                        // Add to list of results
                        batches.Add( batch );

                    }
                }

                // return the list of people
                return batches;
            }

            // An error occurred trying to query people so return null
            return null;
        }

        public static List<PCODonation> GetDonations( bool importing, DateTime? updatedAfter, string apiEndPoint )
        {
            // Create variables to store the results 
            var dataItems = new List<PCOData>();
            var includedItems = new List<PCOData>();

            // Set the endpoint if not passed from parameter
            apiEndPoint = apiEndPoint ?? "https://api.planningcenteronline.com/giving/v2/donations?include=designations&per_page=100";

            // If not importing and querying only changes, add a parameter to specify that the results should be sored in descending order of last update
            if ( !importing && updatedAfter.HasValue )
            {
                apiEndPoint = apiEndPoint + "&order=-updated_at";
            }

            // Query Planning Center for people
            if ( PCOGetItems( apiEndPoint, dataItems, includedItems, updatedAfter,
                !importing, "", "" ) )
            {
                // Create variable to store the people
                var donations = new List<PCODonation>();

                // Loop through each item in the result of api call
                foreach ( var item in dataItems )
                {
                    // Create the person record
                    var donation = new PCODonation( item );
                    if ( donation != null )
                    {
                        // Update Designations
                        donation.UpdateDesignation( item, includedItems );
                        
                        // Add to list of results
                        donations.Add( donation );

                    }
                }

                // return the list of people
                return donations;
            }

            // An error occurred trying to query people so return null
            return null;
        }

        public static List<PCOPerson> GetPeople( bool importing, DateTime? updatedAfter, string apiEndPoint )
        {
            // Create variables to store the results 
            var dataItems = new List<PCOData>();
            var includedItems = new List<PCOData>();

            // Set the endpoint if not passed from parameter
            apiEndPoint = apiEndPoint ?? "https://api.planningcenteronline.com/people/v2/people?include=addresses,emails,field_data,households,inactive_reason,marital_status,name_prefix,name_suffix,phone_numbers,social_profiles,school,primary_campus&per_page=100";

            // If not importing and querying only changes, add a parameter to specify that the results should be sored in descending order of last update
            if ( !importing && updatedAfter.HasValue )
            {
                apiEndPoint = apiEndPoint + "&order=-updated_at";
            }

            // Query Planning Center for people
            if ( PCOGetItems( apiEndPoint, dataItems, includedItems, updatedAfter,
                !importing, "", "" ) )
            {
                // Create variable to store the people
                var people = new List<PCOPerson>();

                // Loop through each item in the result of api call
                foreach ( var item in dataItems )
                {
                    PCOPerson person;
                    if (apiEndPoint.Contains("giving"))
                    {
                        person = new PCOPerson( item, true );
                    }
                    else
                    {
                        person = new PCOPerson( item );
                        if (person != null )
                        {
                            // Update the contact information
                            person.UpdateContactInfo( item, includedItems );
                            person.UpdateHouseHold( item, includedItems );
                            person.UpdateFieldData( item, includedItems );
                            person.UpdateCampus( item, includedItems );
                            person.UpdateProperties( item, includedItems );
                            person.UpdateSocialProfiles( item, includedItems );
                        }
                    }
                    // Create the person record
                    
                    if ( person != null )
                    {

                        // Add to list of results
                        people.Add( person );

                    }
                }

                // return the list of people
                return people;

            }

            // An error occurred trying to query people so return null
            return null;
        }

        public static List<PCOFieldDefinition> GetFields( bool importing, DateTime? updatedAfter, string apiEndPoint )
        {
            // Create variables to store the results 
            var dataItems = new List<PCOData>();
            var includedItems = new List<PCOData>();

            // Set the endpoint if not passed from parameter
            apiEndPoint = apiEndPoint ?? "https://api.planningcenteronline.com/people/v2/field_definitions?include=field_options";

            // Query Planning Center for people
            if ( PCOGetItems( apiEndPoint, dataItems, includedItems, updatedAfter,
                !importing, "", "" ) )
            {
                // Create variable to store the people
                var fields = new List<PCOFieldDefinition>();

                // Loop through each item in the result of api call
                foreach ( var item in dataItems )
                {
                    // Create the person record
                    var field = new PCOFieldDefinition( item );
                    if ( field != null && field.data_type != "header" )
                    {
                        // Update the contact information
                        //field.UpdateFieldOptions( item, includedItems );

                        // Add to list of results
                        fields.Add( field );

                    }
                }

                // return the list of people
                return fields;

            }

            // An error occurred trying to query people so return null
            return null;
        }

        public static List<PCONote> GetNotes( bool importing, DateTime? updatedAfter, string apiEndPoint )
        {
            // Create variables to store the results 
            var dataItems = new List<PCOData>();
            var includedItems = new List<PCOData>();

            // Set the endpoint if not passed from parameter
            apiEndPoint = apiEndPoint ?? "https://api.planningcenteronline.com/people/v2/notes?include=category";

            // Query Planning Center for people
            if ( PCOGetItems( apiEndPoint, dataItems, includedItems, updatedAfter,
                !importing, "", "" ) )
            {
                // Create variable to store the people
                var notes = new List<PCONote>();

                // Loop through each item in the result of api call
                foreach ( var item in dataItems )
                {
                    // Create the person record
                    var note = new PCONote( item );
                    if ( note != null )
                    {
                        // Update the contact information
                        note.UpdateCategory( item, includedItems );
                        

                        // Add to list of results
                        notes.Add( note );

                    }
                }

                // return the list of people
                return notes;

            }

            // An error occurred trying to query people so return null
            return null;
        }
    }
}

