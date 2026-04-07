using RestSharp;
using RestSharp.Authenticators;
using RestSharp.Extensions.MonoHttp;
using Slingshot.Core;
using Slingshot.Core.Model;
using Slingshot.Core.Utilities;
using Slingshot.F1.Utilities.Translators.API;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Slingshot.F1.Utilities
{
    /// <summary>
    /// API F1 Status
    /// </summary>
    public class F1Api : F1Translator
    {
        private static int loopThreshold = 100000000;
        private static List<int> AccountIds;
        private static List<FamilyMember> familyMembers = new List<FamilyMember>();
        private static RestClient _client;
        private static RestRequest _request;

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
        /// Gets the API URL.
        /// </summary>
        /// <value>
        /// The API URL.
        /// </value>
        public static string ApiUrl
        {
            get
            {
                return $"https://{Hostname}.fellowshiponeapi.com";
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
        /// Gets or sets the oauth token.
        /// </summary>
        /// <value>
        /// The oauth token.
        /// </value>
        public static string OAuthToken { get; set; }

        /// <summary>
        /// Gets or sets the oauth secret.
        /// </summary>
        /// <value>
        /// The oauth secret.
        /// </value>
        public static string OAuthSecret { get; set; }

        #region API Call Paths

        private const string API_ACCESS_TOKEN = "/v1/PortalUser/AccessToken";
        private const string API_INDIVIDUAL = "/v1/People";
        private const string API_INDIVIDUALS = "/v1/People/Search";
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
        /// Connects the specified host name.
        /// </summary>
        /// <param name="hostName">Name of the host.</param>
        /// <param name="apiUsername">The API username.</param>
        /// <param name="apiPassword">The API password.</param>
        public static void Connect( string hostName, string apiConsumerKey, string apiConsumerSecret, string apiUsername, string apiPassword )
        {
            Hostname = hostName;
            ApiConsumerKey = apiConsumerKey;
            ApiConsumerSecret = apiConsumerSecret;
            ApiUsername = apiUsername;
            ApiPassword = apiPassword;

            _client = new RestClient( ApiUrl );
            _client.Authenticator = OAuth1Authenticator.ForRequestToken( ApiConsumerKey, ApiConsumerSecret );
            _request = new RestRequest( API_ACCESS_TOKEN, Method.POST );
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            // hash the username/password and add it to the body of the request
            var loginBytes = System.Text.Encoding.UTF8.GetBytes( apiUsername + " " + apiPassword );
            var loginHash = Convert.ToBase64String( loginBytes );

            _request.AddParameter( "ec", loginHash, ParameterType.RequestBody );

            // getting the api status sets the IsConnect flag
            UpdateApiStatus();
        }

        /// <summary>
        /// Updates the API status.
        /// </summary>
        public static void UpdateApiStatus()
        {
            // execute the request to get a oauth token and secret
            var response = _client.Execute( _request );

            var qs = HttpUtility.ParseQueryString( response.Content );
            OAuthToken = qs["oauth_token"];
            OAuthSecret = qs["oauth_token_secret"];

            if ( response.StatusCode == System.Net.HttpStatusCode.OK )
            {
                IsConnected = true;
            }
            else
            {
                ErrorMessage = response.Content;
            }
        }

        /// <summary>
        /// Exports the individuals.
        /// </summary>
        /// <param name="modifiedSince">The modified since.</param>
        /// <param name="peoplePerPage">The people per page.</param>
        public override void ExportIndividuals( DateTime modifiedSince, int peoplePerPage = 500 )
        {
            TextInfo textInfo = new CultureInfo( "en-US", false ).TextInfo;

            HashSet<int> personIds = new HashSet<int>();

            // if empty, build head of household lookups
            if ( !familyMembers.Any() )
            {
                familyMembers = GetFamilyMembers();
            }

            // write out the person attributes
            var personAttributes = WritePersonAttributes();

            int currentPage = 1;
            int loopCounter = 0;
            bool moreIndividualsExist = true;

            try
            {
                while ( moreIndividualsExist )
                {
                    _client.Authenticator = OAuth1Authenticator.ForProtectedResource( ApiConsumerKey, ApiConsumerSecret, OAuthToken, OAuthSecret );
                    _request = new RestRequest( API_INDIVIDUALS, Method.GET );
                    _request.AddQueryParameter( "lastUpdatedDate", modifiedSince.ToShortDateString() );
                    _request.AddQueryParameter( "recordsPerPage", peoplePerPage.ToString() );
                    _request.AddQueryParameter( "page", currentPage.ToString() );
                    _request.AddQueryParameter( "include", "addresses,attributes,communications,requirements" );
                    _request.AddHeader( "content-type", "application/vnd.fellowshiponeapi.com.people.people.v2+xml" );

                    var response = _client.Execute( _request );
                    ApiCounter++;

                    XDocument xdoc = XDocument.Parse( response.Content );

                    if ( F1Api.DumpResponseToXmlFile )
                    {
                        xdoc.Save( Path.Combine( ImportPackage.PackageDirectory, $"API_INDIVIDUALS_ResponseLog_{loopCounter}.xml" ) );
                    }

                    var records = xdoc.Element( "results" );

                    if ( records != null )
                    {
                        var returnCount = records.Attribute( "count" )?.Value.AsIntegerOrNull();
                        var additionalPages = records.Attribute( "additionalPages" ).Value.AsInteger();

                        if ( returnCount.HasValue )
                        {
                            foreach ( var personNode in records.Elements() )
                            {
                                if ( personNode.Attribute( "id" ) != null && personNode.Attribute( "id" ).Value.AsIntegerOrNull().HasValue )
                                {
                                    // If a person is updated during an export, the person could be returned
                                    //  twice by the API.
                                    if ( !personIds.Contains( personNode.Attribute( "id" ).Value.AsInteger() ) )
                                    {
                                        var importPerson = F1Person.Translate( personNode, familyMembers, personAttributes, textInfo );

                                        if ( importPerson != null )
                                        {
                                            ImportPackage.WriteToPackage( importPerson );
                                        }

                                        // save person image
                                        var personId = personNode.Attribute( "id" ).Value;
                                        var imageURI = personNode.Attribute( "imageURI" )?.Value;
                                        if ( imageURI.IsNotNullOrWhitespace() )
                                        {
                                            // building the URI manually since the imageURI doesn't return a valid image
                                            _request = new RestRequest( API_INDIVIDUAL + "/" + personId + "/Images", Method.GET );

                                            var image = _client.DownloadData( _request );
                                            ApiCounter++;

                                            var path = Path.Combine( ImportPackage.ImageDirectory, "Person_" + personId + ".jpg" );
                                            File.WriteAllBytes( path, image );
                                        }

                                        personIds.Add( personNode.Attribute( "id" ).Value.AsInteger() );
                                    }
                                }
                            }

                            if ( additionalPages <= 0 && returnCount <= 0 )
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
        /// Export the people and household notes
        /// </summary>
        public override void ExportNotes()
        {
            // Unimplemented for API export
        }

        /// <summary>
        /// Export the companies
        /// </summary>
        public override void ExportCompanies()
        {
            // Unimplemented for API export
        }

        /// <summary>
        /// Exports the accounts.
        /// </summary>
        public override void ExportFinancialAccounts()
        {
            try
            {
                AccountIds = new List<int>();

                _client.Authenticator = OAuth1Authenticator.ForProtectedResource( ApiConsumerKey, ApiConsumerSecret, OAuthToken, OAuthSecret );
                _request = new RestRequest( API_ACCOUNTS, Method.GET );
                _request.AddHeader( "content-type", "application/xml" );
                var response = _client.Execute( _request );
                ApiCounter++;

                XDocument xdoc = XDocument.Parse( response.Content );

                if ( F1Api.DumpResponseToXmlFile )
                {
                    xdoc.Save( Path.Combine( ImportPackage.PackageDirectory, $"API_FINANCIAL_ACCOUNTS_ResponseLog.xml" ) );
                }

                var accounts = xdoc.Element( "funds" );

                int loopCounter = 0;

                // process accounts
                foreach ( var accountNode in accounts.Elements() )
                {
                    var importAccount = F1FinancialAccount.Translate( accountNode );

                    if ( importAccount != null )
                    {
                        ImportPackage.WriteToPackage( importAccount );
                    }

                    AccountIds.Add( importAccount.Id );

                    // process sub accounts of this account
                    _client.Authenticator = OAuth1Authenticator.ForProtectedResource( ApiConsumerKey, ApiConsumerSecret, OAuthToken, OAuthSecret );
                    _request = new RestRequest( API_ACCOUNTS + "/" + importAccount.Id.ToString() + "/subFunds", Method.GET );
                    _request.AddHeader( "content-type", "application/xml" );
                    var subResponse = _client.Execute( _request );
                    ApiCounter++;

                    xdoc = XDocument.Parse( subResponse.Content );

                    if ( F1Api.DumpResponseToXmlFile )
                    {
                        xdoc.Save( Path.Combine( ImportPackage.PackageDirectory, $"API_FINANCIAL_ACCOUNTS_ResponseLog.xml" ) );
                    }

                    var subAccounts = xdoc.Element( "subFunds" );
                    foreach ( var subAccountNode in subAccounts.Elements() )
                    {
                        var importSubAccount = F1FinancialAccount.Translate( subAccountNode, importAccount.IsTaxDeductible );

                        if ( importSubAccount != null )
                        {
                            ImportPackage.WriteToPackage( importSubAccount );
                        }

                        AccountIds.Add( importSubAccount.Id );
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
        /// Exports the pledges.
        /// </summary>
        public override void ExportFinancialPledges()
        {
            int loopCounter = 0;
            try
            {
                foreach ( var accountId in AccountIds )
                {
                    _client.Authenticator = OAuth1Authenticator.ForProtectedResource( ApiConsumerKey, ApiConsumerSecret, OAuthToken, OAuthSecret );
                    _request = new RestRequest( API_ACCOUNTS + "/" + accountId.ToString() + "/pledgedrives", Method.GET );
                    _request.AddHeader( "content-type", "application/xml" );
                    var response = _client.Execute( _request );
                    ApiCounter++;

                    var xdoc = XDocument.Parse( response.Content );

                    if ( F1Api.DumpResponseToXmlFile )
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

        /// <summary>
        /// Exports the batches.
        /// </summary>
        /// <param name="modifiedSince">The modified since.</param>
        public override void ExportFinancialBatches( DateTime modifiedSince )
        {
            try
            {
                // first, we need to find out what batch types are available
                _client.Authenticator = OAuth1Authenticator.ForProtectedResource( ApiConsumerKey, ApiConsumerSecret, OAuthToken, OAuthSecret );
                _request = new RestRequest( API_BATCH_TYPES, Method.GET );
                _request.AddHeader( "content-type", "application/xml" );
                var batchTypeResponse = _client.Execute( _request );
                ApiCounter++;

                XDocument xBatchTypeDoc = XDocument.Parse( batchTypeResponse.Content );
                var batchTypes = xBatchTypeDoc.Elements( "batchTypes" );

                // process all batches for each batch type
                foreach ( var batchType in batchTypes.Elements() )
                {
                    int batchCurrentPage = 1;
                    int batchLoopCounter = 0;
                    bool moreBatchesExist = true;

                    while ( moreBatchesExist )
                    {
                        _request = new RestRequest( API_BATCHES, Method.GET );
                        _request.AddQueryParameter( "lastUpdatedDate", modifiedSince.ToShortDateString() );
                        _request.AddQueryParameter( "batchTypeID", batchType.Attribute( "id" ).Value );
                        _request.AddQueryParameter( "recordsPerPage", "1000" );
                        _request.AddQueryParameter( "page", batchCurrentPage.ToString() );
                        _request.AddHeader( "content-type", "application/xml" );

                        var batchRepsonse = _client.Execute( _request );
                        ApiCounter++;

                        XDocument xBatchDoc = XDocument.Parse( batchRepsonse.Content );

                        if ( F1Api.DumpResponseToXmlFile )
                        {
                            xBatchDoc.Save( Path.Combine( ImportPackage.PackageDirectory, $"API_FINANCIAL_BATCHES_ResponseLog_{batchLoopCounter++}.xml" ) );
                        }

                        var batches = xBatchDoc.Element( "results" );

                        if ( batches != null )
                        {
                            var batchReturnCount = batches.Attribute( "count" )?.Value.AsIntegerOrNull();
                            var batchAdditionalPages = batches.Attribute( "additionalPages" ).Value.AsInteger();

                            if ( batchReturnCount.HasValue )
                            {
                                // process all batches
                                foreach ( var batchNode in batches.Elements() )
                                {
                                    var importBatch = F1FinancialBatch.Translate( batchNode );

                                    if ( importBatch != null )
                                    {
                                        ImportPackage.WriteToPackage( importBatch );
                                    }
                                }

                                if ( batchAdditionalPages <= 0 )
                                {
                                    moreBatchesExist = false;
                                }
                                else
                                {
                                    batchCurrentPage++;
                                }
                            }
                        }
                        else
                        {
                            moreBatchesExist = false;
                        }

                        // developer safety blanket (prevents eating all the api calls for the day)
                        if ( batchLoopCounter > loopThreshold )
                        {
                            break;
                        }
                        batchLoopCounter++;
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
        public override void ExportContributions( DateTime modifiedSince, bool exportContribImages )
        {
            HashSet<int> transactionIds = new HashSet<int>();
            List<Task> tasks = new List<Task>();

            // if empty, build head of household lookups
            if ( !familyMembers.Any() )
            {
                familyMembers = GetFamilyMembers();
            }

            // we'll make an api call for each month until the modifiedSince date
            var today = DateTime.Now;
            var numberOfMonths = ( ( ( today.Year - modifiedSince.Year ) * 12 ) + today.Month - modifiedSince.Month ) + 1;
            try
            {
                for ( int i = 0; i < numberOfMonths; i++ )
                {
                    DateTime referenceDate = today.AddMonths( ( ( numberOfMonths - i ) - 1 ) * -1 );
                    DateTime startDate = new DateTime( referenceDate.Year, referenceDate.Month, 1 );
                    DateTime endDate = new DateTime( referenceDate.Year, referenceDate.Month, DateTime.DaysInMonth( referenceDate.Year, referenceDate.Month ) );
                    endDate = endDate.AddDays( 1 );

                    // if it's the first instance set start date to the modifiedSince date
                    if ( i == 0 )
                    {
                        startDate = modifiedSince;
                    }

                    // if it's the last time through set the end date to today's date
                    if ( i == numberOfMonths - 1 )
                    {
                        endDate = today.AddDays( 1 );
                    }

                    int transactionCurrentPage = 1;
                    int transactionLoopCounter = 0;
                    bool moreTransactionsExist = true;

                    while ( moreTransactionsExist )
                    {
                        _request = new RestRequest( API_CONTRIBUTION_RECEIPTS, Method.GET );
                        _request.AddQueryParameter( "startReceivedDate", startDate.ToString( "yyyy-MM-dd" ) );
                        _request.AddQueryParameter( "endReceivedDate", endDate.ToString( "yyyy-MM-dd" ) );
                        _request.AddQueryParameter( "recordsPerPage", "1000" );
                        _request.AddQueryParameter( "page", transactionCurrentPage.ToString() );
                        _request.AddHeader( "content-type", "application/xml" );

                        var response = _client.Execute( _request );
                        ApiCounter++;

                        XDocument xTransactionsDoc = XDocument.Parse( response.Content );

                        if ( F1Api.DumpResponseToXmlFile )
                        {
                            xTransactionsDoc.Save( Path.Combine( ImportPackage.PackageDirectory, $"API_FINANCIAL_TRANSACTIONS_ResponseLog_{transactionLoopCounter++}.xml" ) );
                        }

                        var sourceTransactions = xTransactionsDoc.Element( "results" );

                        if ( sourceTransactions != null )
                        {
                            var transactionReturnCount = sourceTransactions.Attribute( "count" )?.Value.AsIntegerOrNull();
                            var transactionAdditionalPages = sourceTransactions.Attribute( "additionalPages" ).Value.AsInteger();

                            if ( transactionReturnCount.HasValue )
                            {
                                foreach ( var sourceTransaction in sourceTransactions.Elements() )
                                {
                                    // If a transaction is updated during an export, the transaction could be returned
                                    //  twice by the API.  Also, since there is a slight overlap in dates, this ensures
                                    //  that a transaction only gets imported once.
                                    if ( !transactionIds.Contains( sourceTransaction.Attribute( "id" ).Value.AsInteger() ) )
                                    {
                                        var importTransaction = F1FinancialTransaction.Translate( sourceTransaction, familyMembers );
                                        var importTransactionDetail = F1FinancialTransactionDetail.Translate( sourceTransaction );

                                        if ( importTransaction != null )
                                        {
                                            ImportPackage.WriteToPackage( importTransaction );
                                        }

                                        if ( importTransactionDetail != null )
                                        {
                                            ImportPackage.WriteToPackage( importTransactionDetail );
                                        }

                                        // save check image
                                        if ( exportContribImages )
                                        {
                                            var checkImageId = sourceTransaction.Element( "referenceImage" ).Attribute( "id" )?.Value;
                                            if ( checkImageId.IsNotNullOrWhitespace() )
                                            {

                                                var task = Task.Run( () =>
                                                {
                                                    var client = new RestClient( ApiUrl );
                                                    client.Authenticator = OAuth1Authenticator.ForProtectedResource( ApiConsumerKey, ApiConsumerSecret, OAuthToken, OAuthSecret );
                                                    var imageRequest = new RestRequest( API_CONTRIBUTION_RECEIPT_IMAGE + checkImageId, Method.GET );
                                                    var image = client.DownloadData( imageRequest );
                                                    ApiCounter++;

                                                    if ( image != null )
                                                    {
                                                        var transactionId = sourceTransaction.Attribute( "id" ).Value;
                                                        var path = Path.Combine( ImportPackage.ImageDirectory, "FinancialTransaction_" + transactionId + "_0.jpg" );
                                                        File.WriteAllBytes( path, image );
                                                    }
                                                } );

                                                tasks.Add( task );
                                            }
                                        }

                                        transactionIds.Add( sourceTransaction.Attribute( "id" ).Value.AsInteger() );
                                    }
                                }

                                if ( transactionAdditionalPages <= 0 )
                                {
                                    moreTransactionsExist = false;
                                }
                                else
                                {
                                    transactionCurrentPage++;
                                }
                            }
                        }
                        else
                        {
                            moreTransactionsExist = false;
                        }

                        // developer safety blanket (prevents eating all the api calls for the day)
                        if ( transactionLoopCounter > loopThreshold )
                        {
                            break;
                        }
                        transactionLoopCounter++;
                    }
                }

                // wait till all images are downloaded
                Task.WaitAll( tasks.ToArray() );
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
        public override void ExportGroups( List<int> selectedGroupTypes )
        {
            // write out the group types
            WriteGroupTypes( selectedGroupTypes );

            // get groups
            try
            {
                int loopCounter = 0;

                foreach ( var selectedGroupType in selectedGroupTypes )
                {
                    _client.Authenticator = OAuth1Authenticator.ForProtectedResource( ApiConsumerKey, ApiConsumerSecret, OAuthToken, OAuthSecret );
                    _request = new RestRequest( API_GROUPS + selectedGroupType.ToString() + "/groups", Method.GET );
                    _request.AddHeader( "content-type", "application/xml" );

                    var response = _client.Execute( _request );
                    ApiCounter++;

                    XDocument xdoc = XDocument.Parse( response.Content );

                    var groups = xdoc.Elements( "groups" );

                    if ( groups.Elements().Any() )
                    {
                        // since we don't have a group hierarchy to work with, add a parent
                        //  group for each group type for organizational purposes
                        int parentGroupId = 99 + groups.Elements().FirstOrDefault().Element( "groupType" ).Attribute( "id" ).Value.AsInteger();

                        ImportPackage.WriteToPackage( new Group()
                        {
                            Id = parentGroupId,
                            Name = groups.Elements().FirstOrDefault().Element( "groupType" ).Element( "name" ).Value,
                            GroupTypeId = groups.Elements().FirstOrDefault().Element( "groupType" ).Attribute( "id" ).Value.AsInteger(),
                            IsActive = true
                        } );

                        foreach ( var groupNode in groups.Elements() )
                        {
                            string groupId = groupNode.Attribute( "id" ).Value;

                            _client.Authenticator = OAuth1Authenticator.ForProtectedResource( ApiConsumerKey, ApiConsumerSecret, OAuthToken, OAuthSecret );
                            _request = new RestRequest( API_GROUP_MEMBERS + groupId + "/members", Method.GET );
                            _request.AddHeader( "content-type", "application/xml" );

                            response = _client.Execute( _request );
                            ApiCounter++;

                            xdoc = XDocument.Parse( response.Content );

                            if ( F1Api.DumpResponseToXmlFile )
                            {
                                xdoc.Save( Path.Combine( ImportPackage.PackageDirectory, $"API_GROUPS_ResponseLog_{loopCounter}.xml" ) );
                            }

                            var membersNode = xdoc.Elements( "members" );

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

        public override void ExportAttendance( DateTime modifiedSince )
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Exports the person attributes.
        /// </summary>
        public override List<PersonAttribute> WritePersonAttributes()
        {
            // export person fields as attributes
            ImportPackage.WriteToPackage( new PersonAttribute()
            {
                Name = "Position",
                Key = "Position",
                Category = "Employment",
                FieldType = "Rock.Field.Types.TextFieldType"
            } );

            ImportPackage.WriteToPackage( new PersonAttribute()
            {
                Name = "Employer",
                Key = "Employer",
                Category = "Employment",
                FieldType = "Rock.Field.Types.TextFieldType"
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
                Name = "Denomination",
                Key = "Denomination",
                Category = "Visit Information",
                FieldType = "Rock.Field.Types.TextFieldType"
            } );

            ImportPackage.WriteToPackage( new PersonAttribute()
            {
                Name = "PreviousChurch",
                Key = "PreviousChurch",
                Category = "Visit Information",
                FieldType = "Rock.Field.Types.TextFieldType"
            } );

            ImportPackage.WriteToPackage( new PersonAttribute()
            {
                Name = "Bar Code",
                Key = "BarCode",
                Category = "Childhood Information",
                FieldType = "Rock.Field.Types.TextFieldType"
            } );

            var attributes = new List<PersonAttribute>();

            // export person requirements
            try
            {
                _client.Authenticator = OAuth1Authenticator.ForProtectedResource( ApiConsumerKey, ApiConsumerSecret, OAuthToken, OAuthSecret );
                _request = new RestRequest( API_INDIVIDUALS_REQUIREMENTS, Method.GET );
                _request.AddHeader( "content-type", "application/xml" );

                var response = _client.Execute( _request );
                ApiCounter++;

                XDocument xdoc = XDocument.Parse( response.Content );

                var requirements = xdoc.Element( "requirements" );

                if ( requirements != null )
                {
                    // create 2 Rock attributes for every one F1 requirement (status & date)
                    foreach ( var sourceRequirement in requirements.Elements() )
                    {
                        string requirementName = sourceRequirement.Element( "name" ).Value;
                        string requirementId = sourceRequirement.Attribute( "id" ).Value;

                        // status attribute
                        var requirementStatus = new PersonAttribute()
                        {
                            Name = requirementName + " Status",
                            Key = ( requirementId + "_" + requirementName + " Status" ).RemoveSpaces().RemoveSpecialCharacters(),
                            Category = "Requirements",
                            FieldType = "Rock.Field.Types.TextFieldType"
                        };

                        ImportPackage.WriteToPackage( requirementStatus );
                        attributes.Add( requirementStatus );

                        // date attribute
                        var requirementDate = new PersonAttribute()
                        {
                            Name = requirementName + " Date",
                            Key = ( requirementId + "_" + requirementName + " Date" ).RemoveSpaces().RemoveSpecialCharacters(),
                            Category = "Requirements",
                            FieldType = "Rock.Field.Types.DateFieldType"
                        };

                        ImportPackage.WriteToPackage( requirementDate );
                        attributes.Add( requirementDate );
                    }
                }
            }
            catch ( Exception ex )
            {
                ErrorMessage = ex.Message;
            }

            // export person attributes
            try
            {
                _client.Authenticator = OAuth1Authenticator.ForProtectedResource( ApiConsumerKey, ApiConsumerSecret, OAuthToken, OAuthSecret );
                _request = new RestRequest( API_ATTRIBUTE_GROUPS, Method.GET );
                _request.AddHeader( "content-type", "application/xml" );

                Console.WriteLine( API_ATTRIBUTE_GROUPS );

                var response = _client.Execute( _request );
                ApiCounter++;

                XDocument xdoc = XDocument.Parse( response.Content );

                var attributeGroups = xdoc.Element( "attributeGroups" );

                if ( attributeGroups != null )
                {
                    foreach ( var sourceAttributeGroup in attributeGroups.Elements() )
                    {
                        // create 3 Rock attributes for every one F1 attribute (comment, start date & end date)
                        foreach ( var attribute in sourceAttributeGroup.Elements( "attribute" ) )
                        {
                            string attributeGroup = sourceAttributeGroup.Element( "name" ).Value;
                            string attributeName = attribute.Element( "name" ).Value;
                            string attributeId = attribute.Attribute( "id" ).Value;

                            // comment attribute
                            var personAttributeComment = new PersonAttribute()
                            {
                                Name = attributeName + " Comment",
                                Key = attributeId + "_" + attributeName.RemoveSpaces().RemoveSpecialCharacters() + "Comment",
                                Category = attributeGroup,
                                FieldType = "Rock.Field.Types.TextFieldType"
                            };

                            ImportPackage.WriteToPackage( personAttributeComment );

                            // start date attribute
                            var personAttributeStartDate = new PersonAttribute()
                            {
                                Name = attributeName + " Start Date",
                                Key = attributeId + "_" + attributeName.RemoveSpaces().RemoveSpecialCharacters() + "StartDate",
                                Category = attributeGroup,
                                FieldType = "Rock.Field.Types.DateFieldType"
                            };

                            ImportPackage.WriteToPackage( personAttributeStartDate );

                            // end date attribute
                            var personAttributeEndDate = new PersonAttribute()
                            {
                                Name = attributeName + " End Date",
                                Key = attributeId + "_" + attributeName.RemoveSpaces().RemoveSpecialCharacters() + "EndDate",
                                Category = attributeGroup,
                                FieldType = "Rock.Field.Types.DateFieldType"
                            };

                            ImportPackage.WriteToPackage( personAttributeEndDate );

                            // Add the attributes to the list
                            attributes.Add( personAttributeComment );
                            attributes.Add( personAttributeStartDate );
                            attributes.Add( personAttributeEndDate );
                        }
                    }
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
        public override List<GroupType> GetGroupTypes()
        {
            List<GroupType> groupTypes = new List<GroupType>();

            try
            {
                _client.Authenticator = OAuth1Authenticator.ForProtectedResource( ApiConsumerKey, ApiConsumerSecret, OAuthToken, OAuthSecret );
                _request = new RestRequest( API_GROUP_TYPES, Method.GET );
                _request.AddHeader( "content-type", "application/xml" );

                var response = _client.Execute( _request );
                ApiCounter++;

                XDocument xdoc = XDocument.Parse( response.Content );

                if ( F1Api.DumpResponseToXmlFile )
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
        public override void WriteGroupTypes( List<int> selectedGroupTypes )
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

        /// <summary>
        /// Gets the family members.
        /// </summary>
        /// <returns></returns>
        public List<FamilyMember> GetFamilyMembers()
        {
            var headOfHouseholds = new List<FamilyMember>();
            HashSet<int> personIds = new HashSet<int>();

            int currentPage = 1;
            int loopCounter = 0;
            bool moreIndividualsExist = true;

            try
            {
                while ( moreIndividualsExist )
                {
                    _client.Authenticator = OAuth1Authenticator.ForProtectedResource( ApiConsumerKey, ApiConsumerSecret, OAuthToken, OAuthSecret );
                    _request = new RestRequest( API_INDIVIDUALS, Method.GET );
                    _request.AddQueryParameter( "lastUpdatedDate", "1/1/1901" );
                    _request.AddQueryParameter( "recordsPerPage", "10000" );
                    _request.AddQueryParameter( "page", currentPage.ToString() );
                    _request.AddHeader( "content-type", "application/vnd.fellowshiponeapi.com.people.people.v2+xml" );

                    var response = _client.Execute( _request );
                    ApiCounter++;

                    XDocument xdoc = XDocument.Parse( response.Content );

                    var records = xdoc.Element( "results" );

                    if ( records != null )
                    {
                        var returnCount = records.Attribute( "count" )?.Value.AsIntegerOrNull();
                        var additionalPages = records.Attribute( "additionalPages" ).Value.AsInteger();

                        if ( returnCount.HasValue )
                        {
                            foreach ( var personNode in records.Elements() )
                            {
                                if ( personNode.Attribute( "id" ) != null && personNode.Attribute( "id" ).Value.AsIntegerOrNull().HasValue )
                                {
                                    // If a person is updated during an export, the person could be returned
                                    //  twice by the API.  A check is done to ensure the person doesn't get imported twice.
                                    if ( !personIds.Contains( personNode.Attribute( "id" ).Value.AsInteger() ) )
                                    {
                                        var householdMemberType = personNode.Element( "householdMemberType" ).Attribute( "id" ).Value.AsIntegerOrNull();
                                        if ( householdMemberType.HasValue )
                                        {
                                            FamilyMember member = new FamilyMember();

                                            member.PersonId = personNode.Attribute( "id" ).Value.AsInteger();
                                            member.HouseholdId = personNode.Attribute( "householdID" ).Value.AsInteger();
                                            member.FamilyRoleId = householdMemberType.Value;
                                            var campusName = personNode.Element( "status" ).Element( "subStatus" ).Element( "name" )?.Value;

                                            if ( campusName.IsNotNullOrWhitespace() )
                                            {
                                                string campusNameTrimmed = campusName.Trim();
                                                member.HouseholdCampusName = campusNameTrimmed;

                                                // generate a unique campus id
                                                MD5 md5Hasher = MD5.Create();
                                                var hashed = md5Hasher.ComputeHash( Encoding.UTF8.GetBytes( campusNameTrimmed ) );
                                                var campusId = Math.Abs( BitConverter.ToInt32( hashed, 0 ) ); // used abs to ensure positive number
                                                if ( campusId > 0 )
                                                {
                                                    member.HouseholdCampusId = campusId;
                                                }
                                            }

                                            familyMembers.Add( member );
                                        }

                                        personIds.Add( personNode.Attribute( "id" ).Value.AsInteger() );
                                    }
                                }
                            }

                            if ( additionalPages <= 0 && returnCount <= 0 )
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

            return familyMembers;
        }
    }
}