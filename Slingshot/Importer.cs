using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
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
            SlingshotDirectoryName = Path.Combine( Path.GetDirectoryName( this.SlingshotFileName ), "slingshots", Path.GetFileNameWithoutExtension( this.SlingshotFileName ) );
            RockUrl = rockUrl;
            RockUserName = rockUserName;
            RockPassword = rockPassword;

            var slingshotFilesDirectory = new DirectoryInfo( this.SlingshotDirectoryName );
            if ( slingshotFilesDirectory.Exists )
            {
                slingshotFilesDirectory.Delete( true );
            }

            slingshotFilesDirectory.Create();
            if ( File.Exists( this.SlingshotFileName ) )
            {
                ZipFile.ExtractToDirectory( this.SlingshotFileName, slingshotFilesDirectory.FullName );
            }

            this.Results = new Dictionary<string, string>();
        }

        /// <summary>
        /// Gets or sets a value indicating whether [use sample photos].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [use sample photos]; otherwise, <c>false</c>.
        /// </value>
        public bool TEST_UseSamplePhotos { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [cancel photo import].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [cancel photo import]; otherwise, <c>false</c>.
        /// </value>
        public bool CancelPhotoImport { get; set; }

        /// <summary>
        /// The sample photo urls
        /// </summary>
        private List<string> SamplePhotoUrls { get; set; } = new List<string>
        {
            { "http://storage.rockrms.com/sampledata/person-images/decker_ted.jpg" },
            { "http://storage.rockrms.com/sampledata/person-images/decker_cindy.png" },
            { "http://storage.rockrms.com/sampledata/person-images/decker_noah.jpg" },
            { "http://storage.rockrms.com/sampledata/person-images/decker_alexis.jpg" },
            { "http://storage.rockrms.com/sampledata/person-images/jones_ben.jpg" },
            { "http://storage.rockrms.com/sampledata/person-images/jones_brian.jpg" },
            { "http://storage.rockrms.com/sampledata/person-images/simmons_jim.jpg" },
            { "http://storage.rockrms.com/sampledata/person-images/simmons_sarah.jpg" },
            { "http://storage.rockrms.com/sampledata/person-images/jackson_mariah.jpg" },
            { "http://storage.rockrms.com/sampledata/person-images/lowe_madison.jpg" },
            { "http://storage.rockrms.com/sampledata/person-images/lowe_craig.jpg" },
            { "http://storage.rockrms.com/sampledata/person-images/lowe_tricia.jpg" },
            { "http://storage.rockrms.com/sampledata/person-images/marble_alisha.jpg" },
            { "http://storage.rockrms.com/sampledata/person-images/marble_bill.jpg" },
            { "http://storage.rockrms.com/sampledata/person-images/miller_tom.jpg" },
            { "http://storage.rockrms.com/sampledata/person-images/foster_peter.jpg" },
            { "http://storage.rockrms.com/sampledata/person-images/foster_pamela.jpg" },
            { "http://storage.rockrms.com/sampledata/person-images/michaels_jenny.jpg" },
            { @"C:\Users\admin\Downloads\slingshots\TESTPHOTOS\Photo0.jpg" },
            { @"C:\Users\admin\Downloads\slingshots\TESTPHOTOS\Photo1.jpg" },
            { @"C:\Users\admin\Downloads\slingshots\TESTPHOTOS\Photo2.jpg" },
            { @"C:\Users\admin\Downloads\slingshots\TESTPHOTOS\Photo3.jpg" },
            { @"C:\Users\admin\Downloads\slingshots\TESTPHOTOS\Photo4.jpg" },
            { @"C:\Users\admin\Downloads\slingshots\TESTPHOTOS\Photo5.jpg" },
            { @"C:\Users\admin\Downloads\slingshots\TESTPHOTOS\Photo6.jpg" },
            { @"C:\Users\admin\Downloads\slingshots\TESTPHOTOS\Photo7.jpg" },
            { @"C:\Users\admin\Downloads\slingshots\TESTPHOTOS\Photo8.jpg" },
            { @"C:\Users\admin\Downloads\slingshots\TESTPHOTOS\Photo9.jpg" },
            { @"C:\Users\admin\Downloads\slingshots\TESTPHOTOS\Photo0.jpg" },
            { @"C:\Users\admin\Downloads\slingshots\TESTPHOTOS\Photo1.jpg" },
            { @"C:\Users\admin\Downloads\slingshots\TESTPHOTOS\Photo2.jpg" },
            { @"C:\Users\admin\Downloads\slingshots\TESTPHOTOS\Photo3.jpg" },
            { @"C:\Users\admin\Downloads\slingshots\TESTPHOTOS\Photo4.jpg" },
            { @"C:\Users\admin\Downloads\slingshots\TESTPHOTOS\Photo5.jpg" },
            { @"C:\Users\admin\Downloads\slingshots\TESTPHOTOS\Photo6.jpg" },
            { @"C:\Users\admin\Downloads\slingshots\TESTPHOTOS\Photo7.jpg" },
            { @"C:\Users\admin\Downloads\slingshots\TESTPHOTOS\Photo8.jpg" },
            { @"C:\Users\admin\Downloads\slingshots\TESTPHOTOS\Photo9.jpg" },
            { @"C:\Users\admin\Downloads\slingshots\TESTPHOTOS\Photo0.jpg" },
            { @"C:\Users\admin\Downloads\slingshots\TESTPHOTOS\Photo1.jpg" },
            { @"C:\Users\admin\Downloads\slingshots\TESTPHOTOS\Photo2.jpg" },
            { @"C:\Users\admin\Downloads\slingshots\TESTPHOTOS\Photo3.jpg" },
            { @"C:\Users\admin\Downloads\slingshots\TESTPHOTOS\Photo4.jpg" },
            { @"C:\Users\admin\Downloads\slingshots\TESTPHOTOS\Photo5.jpg" },
            { @"C:\Users\admin\Downloads\slingshots\TESTPHOTOS\Photo6.jpg" },
            { @"C:\Users\admin\Downloads\slingshots\TESTPHOTOS\Photo7.jpg" },
            { @"C:\Users\admin\Downloads\slingshots\TESTPHOTOS\Photo8.jpg" },
            { @"C:\Users\admin\Downloads\slingshots\TESTPHOTOS\Photo9.jpg" },
            { @"C:\Users\admin\Downloads\slingshots\TESTPHOTOS\Photo0.jpg" },
            { @"C:\Users\admin\Downloads\slingshots\TESTPHOTOS\Photo1.jpg" },
            { @"C:\Users\admin\Downloads\slingshots\TESTPHOTOS\Photo2.jpg" },
            { @"C:\Users\admin\Downloads\slingshots\TESTPHOTOS\Photo3.jpg" },
            { @"C:\Users\admin\Downloads\slingshots\TESTPHOTOS\Photo4.jpg" },
            { @"C:\Users\admin\Downloads\slingshots\TESTPHOTOS\Photo5.jpg" },
            { @"C:\Users\admin\Downloads\slingshots\TESTPHOTOS\Photo6.jpg" },
            { @"C:\Users\admin\Downloads\slingshots\TESTPHOTOS\Photo7.jpg" },
            { @"C:\Users\admin\Downloads\slingshots\TESTPHOTOS\Photo8.jpg" },
            { @"C:\Users\admin\Downloads\slingshots\TESTPHOTOS\Photo9.jpg" },
            { @"C:\Users\admin\Downloads\slingshots\TESTPHOTOS\Photo0.jpg" },
            { @"C:\Users\admin\Downloads\slingshots\TESTPHOTOS\Photo1.jpg" },
            { @"C:\Users\admin\Downloads\slingshots\TESTPHOTOS\Photo2.jpg" },
            { @"C:\Users\admin\Downloads\slingshots\TESTPHOTOS\Photo3.jpg" },
            { @"C:\Users\admin\Downloads\slingshots\TESTPHOTOS\Photo4.jpg" },
            { @"C:\Users\admin\Downloads\slingshots\TESTPHOTOS\Photo5.jpg" },
            { @"C:\Users\admin\Downloads\slingshots\TESTPHOTOS\Photo6.jpg" },
            { @"C:\Users\admin\Downloads\slingshots\TESTPHOTOS\Photo7.jpg" },
            { @"C:\Users\admin\Downloads\slingshots\TESTPHOTOS\Photo8.jpg" },
            { @"C:\Users\admin\Downloads\slingshots\TESTPHOTOS\Photo9.jpg" }
        };

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

        private Dictionary<Guid, Rock.Client.DefinedType> DefinedTypeLookup { get; set; }

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

        private string SlingshotDirectoryName { get; set; }

        /// <summary>
        /// Gets or sets the results.
        /// </summary>
        /// <value>
        /// The results.
        /// </value>
        public Dictionary<string, string> Results { get; set; }

        /// <summary>
        /// The exceptions
        /// </summary>
        public List<Exception> Exceptions { get; set; } = new List<Exception>();

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
        /// Gets or sets the photo batch size mb.
        /// </summary>
        /// <value>
        /// The photo batch size mb.
        /// </value>
        public int? PhotoBatchSizeMB { get; set; }

        /// <summary>
        /// Gets or sets the size of the financial transaction chunk.
        /// Just in case the Target size reports a Timeout from the SqlBulkImport API.
        /// </summary>
        /// <value>
        /// The size of the financial transaction chunk.
        /// </value>
        public int? FinancialTransactionChunkSize { get; set; }

        /// <summary>
        /// Handles the DoWork event of the BackgroundWorker control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="DoWorkEventArgs"/> instance containing the event data.</param>
        public void BackgroundWorker_DoImport( object sender, DoWorkEventArgs e )
        {
            BackgroundWorker = sender as BackgroundWorker;
            BackgroundWorker.ReportProgress( 0, "Connecting to Rock REST Api..." );

            this.RockRestClient = this.GetRockRestClient();

            // Load Slingshot Models from .slingshot
            BackgroundWorker.ReportProgress( 0, "Loading Slingshot Models..." );
            LoadSlingshotLists();

            // Load Rock Lookups
            BackgroundWorker.ReportProgress( 0, "Loading Rock Lookups..." );
            LoadLookups();

            EnsureDefinedValues();

            BackgroundWorker.ReportProgress( 0, "Updating Rock Lookups..." );

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

        private const string PREPARE_PHOTO_DATA = "Prepare Photo Data:";
        private const string UPLOADING_PHOTO_DATA = "Uploading Photo Data:";
        private const string UPLOAD_PHOTO_STATS = "Stats:";

        /// <summary>
        /// Handles the DoImportPhotos event of the BackgroundWorker control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="DoWorkEventArgs"/> instance containing the event data.</param>
        /// <exception cref="NotImplementedException"></exception>
        public void BackgroundWorker_DoImportPhotos( object sender, DoWorkEventArgs e )
        {
            BackgroundWorker = sender as BackgroundWorker;
            BackgroundWorker.ReportProgress( 0, "Connecting to Rock REST Api..." );

            this.RockRestClient = this.GetRockRestClient();

            this.Results.Clear();

            this.Results.Add( PREPARE_PHOTO_DATA, string.Empty );
            this.Results.Add( UPLOADING_PHOTO_DATA, string.Empty );
            this.Results.Add( UPLOAD_PHOTO_STATS, string.Empty );

            // Load Slingshot Models from .slingshot
            BackgroundWorker.ReportProgress( 0, "Loading Person Slingshot Models..." );
            LoadPersonSlingshotLists();

            BackgroundWorker.ReportProgress( 0, "Processing..." );

            if ( this.TEST_UseSamplePhotos )
            {
                var randomPhoto = new Random();
                int samplePhotoCount = this.SamplePhotoUrls.Count();
                foreach ( var person in this.SlingshotPersonList )
                {
                    int randomPhotoIndex = randomPhoto.Next( samplePhotoCount );
                    person.PersonPhotoUrl = this.SamplePhotoUrls[randomPhotoIndex];
                    randomPhotoIndex = randomPhoto.Next( samplePhotoCount );
                    person.FamilyImageUrl = this.SamplePhotoUrls[randomPhotoIndex];
                }
            }

            var slingshotPersonsWithPhotoList = this.SlingshotPersonList.Where( a => !string.IsNullOrEmpty( a.PersonPhotoUrl ) || !string.IsNullOrEmpty( a.FamilyImageUrl ) ).ToList();

            var photoImportList = new ConcurrentBag<Rock.Slingshot.Model.PhotoImport>();

            HashSet<int> importedFamilyPhotos = new HashSet<int>();

            long photoLoadProgress = 0;
            long photoUploadProgress = 0;
            int totalCount = slingshotPersonsWithPhotoList.Where( a => !string.IsNullOrWhiteSpace( a.PersonPhotoUrl ) ).Count()
                + slingshotPersonsWithPhotoList.Where( a => a.FamilyId.HasValue && !string.IsNullOrWhiteSpace( a.FamilyImageUrl ) ).Select( a => a.FamilyId ).Distinct().Count();

            List<Task> photoDataTasks = new List<Task>();
            int totalPhotoDataBytes = 0;
            if ( !this.PhotoBatchSizeMB.HasValue || this.PhotoBatchSizeMB.Value < 1 )
            {
                this.PhotoBatchSizeMB = 50;
            }

            int maxUploadSize = this.PhotoBatchSizeMB.Value * 1024 * 1024;
            foreach ( var slingshotPerson in slingshotPersonsWithPhotoList )
            {
                if ( this.CancelPhotoImport )
                {
                    return;
                }

                var photoDataTask = new Task( () =>
                {
                    if ( !string.IsNullOrEmpty( slingshotPerson.PersonPhotoUrl ) )
                    {
                        var personPhotoImport = new Rock.Slingshot.Model.PhotoImport { PhotoType = Rock.Slingshot.Model.PhotoImport.PhotoImportType.Person };
                        personPhotoImport.ForeignId = slingshotPerson.Id;
                        if ( SetPhotoData( personPhotoImport, slingshotPerson.PersonPhotoUrl ) )
                        {
                            photoImportList.Add( personPhotoImport );
                        }

                        Interlocked.Increment( ref photoLoadProgress );
                    }

                    if ( !string.IsNullOrEmpty( slingshotPerson.FamilyImageUrl ) && slingshotPerson.FamilyId.HasValue )
                    {
                        // make sure to only upload one photo per family
                        if ( !importedFamilyPhotos.Contains( slingshotPerson.FamilyId.Value ) )
                        {
                            importedFamilyPhotos.Add( slingshotPerson.FamilyId.Value );
                            var familyPhotoImport = new Rock.Slingshot.Model.PhotoImport { PhotoType = Rock.Slingshot.Model.PhotoImport.PhotoImportType.Family };
                            familyPhotoImport.ForeignId = slingshotPerson.FamilyId.Value;
                            if ( SetPhotoData( familyPhotoImport, slingshotPerson.FamilyImageUrl ) )
                            {
                                photoImportList.Add( familyPhotoImport );
                            }

                            Interlocked.Increment( ref photoLoadProgress );
                        }
                    }

                    this.Results[PREPARE_PHOTO_DATA] = $"{Interlocked.Read( ref photoLoadProgress )} of {totalCount}";
                    this.Results[UPLOADING_PHOTO_DATA] = $"{Interlocked.Read( ref photoUploadProgress )} of {totalCount}";

                    BackgroundWorker.ReportProgress( 0, Results );
                } );

                photoDataTask.RunSynchronously();

                totalPhotoDataBytes = photoImportList.Sum( a => a.PhotoData.Length );

                if ( this.CancelPhotoImport )
                {
                    return;
                }

                if ( totalPhotoDataBytes > maxUploadSize )
                {
                    var uploadList = photoImportList.ToList();
                    photoImportList = new ConcurrentBag<Rock.Slingshot.Model.PhotoImport>();
                    photoUploadProgress += uploadList.Count();
                    UploadPhotoImports( uploadList );
                    this.Results[PREPARE_PHOTO_DATA] = $"{Interlocked.Read( ref photoLoadProgress )} of {totalCount}";
                    this.Results[UPLOADING_PHOTO_DATA] = $"{Interlocked.Read( ref photoUploadProgress )} of {totalCount}";
                    BackgroundWorker.ReportProgress( 0, Results );

                    GC.Collect();
                }

                photoDataTasks.Add( photoDataTask );
            }

            Task.WaitAll( photoDataTasks.ToArray() );

            photoUploadProgress += photoImportList.Count();

            UploadPhotoImports( photoImportList.ToList() );

            this.Results[PREPARE_PHOTO_DATA] = $"{Interlocked.Read( ref photoLoadProgress )} of {totalCount}";
            this.Results[UPLOADING_PHOTO_DATA] = $"{Interlocked.Read( ref photoUploadProgress )} of {totalCount}";

            BackgroundWorker.ReportProgress( 0, Results );
        }

        /// <summary>
        /// Uploads the photo imports.
        /// </summary>
        /// <param name="photoImportList">The photo import list.</param>
        /// <exception cref="SlingshotPOSTFailedException"></exception>
        private void UploadPhotoImports( List<Rock.Slingshot.Model.PhotoImport> photoImportList )
        {
            RestRequest restImportRequest = new JsonNETRestRequest( "api/BulkImport/PhotoImport", Method.POST );

            restImportRequest.AddBody( photoImportList );

            BackgroundWorker.ReportProgress( 0, "Sending Photo Import to Rock..." );

            this.Results[UPLOADING_PHOTO_DATA] = $"Uploading {photoImportList.Count} photos...";

            BackgroundWorker.ReportProgress( 0, Results );

            var importResponse = this.RockRestClient.Post( restImportRequest );

            if ( importResponse.StatusCode == System.Net.HttpStatusCode.Created )
            {
                this.Results[UPLOAD_PHOTO_STATS] = ( importResponse.Content.FromJsonOrNull<string>() ?? importResponse.Content ) + Environment.NewLine + this.Results[UPLOAD_PHOTO_STATS];
                BackgroundWorker.ReportProgress( 0, Results );
            }
            else
            {
                throw new SlingshotPOSTFailedException( importResponse );
            }
        }

        /// <summary>
        /// Gets the photo data.
        /// </summary>
        /// <param name="photoUrl">The photo URL.</param>
        /// <returns></returns>
        private bool SetPhotoData( Rock.Slingshot.Model.PhotoImport photoImport, string photoUrl )
        {
            Uri photoUri;
            if ( Uri.TryCreate( photoUrl, UriKind.Absolute, out photoUri ) && photoUri?.Scheme != "file" )
            {
                try
                {
                    HttpWebRequest imageRequest = ( HttpWebRequest ) HttpWebRequest.Create( photoUri );
                    HttpWebResponse imageResponse = ( HttpWebResponse ) imageRequest.GetResponse();
                    var imageStream = imageResponse.GetResponseStream();
                    using ( MemoryStream ms = new MemoryStream() )
                    {
                        imageStream.CopyTo( ms );
                        photoImport.MimeType = imageResponse.ContentType;
                        photoImport.PhotoData = Convert.ToBase64String( ms.ToArray(), Base64FormattingOptions.None );
                        photoImport.FileName = $"Photo{photoImport.ForeignId}";
                    }
                }
                catch ( Exception ex )
                {
                    Exceptions.Add( new Exception( "Photo Get Data Error " + photoUrl, ex ) );
                    return false;
                }
            }
            else
            {
                FileInfo photoFile = new FileInfo( photoUrl );
                if ( photoFile.Exists )
                {
                    photoImport.MimeType = System.Web.MimeMapping.GetMimeMapping( photoFile.FullName );
                    photoImport.PhotoData = Convert.ToBase64String( File.ReadAllBytes( photoFile.FullName ) );
                    photoImport.FileName = photoFile.Name;
                }
            }

            return true;
        }

        #region Financial Transaction Related

        /// <summary>
        /// Submits the financial account import.
        /// </summary>
        private void SubmitFinancialAccountImport()
        {
            BackgroundWorker.ReportProgress( 0, "Preparing FinancialAccountImport..." );
            var financialAccountImportList = new List<Rock.Slingshot.Model.FinancialAccountImport>();
            var campusLookup = this.Campuses.Where( a => a.ForeignId.HasValue ).ToDictionary( k => k.ForeignId.Value, v => v.Id );
            foreach ( var slingshotFinancialAccount in this.SlingshotFinancialAccountList )
            {
                var financialAccountImport = new Rock.Slingshot.Model.FinancialAccountImport();
                financialAccountImport.FinancialAccountForeignId = slingshotFinancialAccount.Id;

                financialAccountImport.Name = slingshotFinancialAccount.Name;
                if ( string.IsNullOrWhiteSpace( slingshotFinancialAccount.Name ) )
                {
                    financialAccountImport.Name = "Unnamed Financial Account";
                }

                financialAccountImport.IsTaxDeductible = slingshotFinancialAccount.IsTaxDeductible;

                if ( slingshotFinancialAccount.CampusId.HasValue )
                {
                    financialAccountImport.CampusId = campusLookup[slingshotFinancialAccount.CampusId.Value];
                }
                         
                financialAccountImport.ParentFinancialAccountForeignId = slingshotFinancialAccount.ParentAccountId == 0 ? ( int? ) null : slingshotFinancialAccount.ParentAccountId;

                financialAccountImportList.Add( financialAccountImport );
            }

            RestRequest restImportRequest = new JsonNETRestRequest( "api/BulkImport/FinancialAccountImport", Method.POST );

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
                throw new SlingshotPOSTFailedException( importResponse );
            }
        }

        /// <summary>
        /// Submits the financial batch import.
        /// </summary>
        private void SubmitFinancialBatchImport()
        {
            BackgroundWorker.ReportProgress( 0, "Preparing FinancialBatchImport..." );
            var financialBatchImportList = new List<Rock.Slingshot.Model.FinancialBatchImport>();
            var campusLookup = this.Campuses.Where( a => a.ForeignId.HasValue ).ToDictionary( k => k.ForeignId.Value, v => v.Id );
            foreach ( var slingshotFinancialBatch in this.SlingshotFinancialBatchList )
            {
                var financialBatchImport = new Rock.Slingshot.Model.FinancialBatchImport();
                financialBatchImport.FinancialBatchForeignId = slingshotFinancialBatch.Id;

                financialBatchImport.Name = slingshotFinancialBatch.Name;
                if ( string.IsNullOrWhiteSpace( slingshotFinancialBatch.Name ) )
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
                        financialBatchImport.Status = Rock.Slingshot.Model.FinancialBatchImport.BatchStatus.Closed;
                        break;
                    case Core.Model.BatchStatus.Open:
                        financialBatchImport.Status = Rock.Slingshot.Model.FinancialBatchImport.BatchStatus.Open;
                        break;
                    case Core.Model.BatchStatus.Pending:
                        financialBatchImport.Status = Rock.Slingshot.Model.FinancialBatchImport.BatchStatus.Pending;
                        break;
                }

                financialBatchImport.CampusId = slingshotFinancialBatch.CampusId.HasValue ? campusLookup[slingshotFinancialBatch.CampusId.Value] : ( int? ) null;

                financialBatchImportList.Add( financialBatchImport );
            }

            RestRequest restImportRequest = new JsonNETRestRequest( "api/BulkImport/FinancialBatchImport", Method.POST );

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
                throw new SlingshotPOSTFailedException( importResponse );
            }
        }

        /// <summary>
        /// Submits the financial transaction import.
        /// </summary>
        private void SubmitFinancialTransactionImport()
        {
            BackgroundWorker.ReportProgress( 0, "Preparing FinancialTransactionImport..." );
            var financialTransactionImportList = new List<Rock.Slingshot.Model.FinancialTransactionImport>();
            var campusLookup = this.Campuses.Where( a => a.ForeignId.HasValue ).ToDictionary( k => k.ForeignId.Value, v => v.Id );
            foreach ( var slingshotFinancialTransaction in this.SlingshotFinancialTransactionList )
            {
                var financialTransactionImport = new Rock.Slingshot.Model.FinancialTransactionImport();
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
                    case Core.Model.CurrencyType.Unknown:
                        financialTransactionImport.CurrencyTypeValueId = this.CurrencyTypeValues[Rock.Client.SystemGuid.DefinedValue.CURRENCY_TYPE_UNKNOWN.AsGuid()].Id;
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

                financialTransactionImport.FinancialTransactionDetailImports = new List<Rock.Slingshot.Model.FinancialTransactionDetailImport>();
                foreach ( var slingshotFinancialTransactionDetail in slingshotFinancialTransaction.FinancialTransactionDetails )
                {
                    var financialTransactionDetailImport = new Rock.Slingshot.Model.FinancialTransactionDetailImport();
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

            int postChunkSize = this.FinancialTransactionChunkSize ?? int.MaxValue;

            while ( financialTransactionImportList.Any() )
            {
                RestRequest restImportRequest = new JsonNETRestRequest( "api/BulkImport/FinancialTransactionImport", Method.POST );

                int fifteenMinutesMS = ( 1000 * 60 ) * 15;
                restImportRequest.Timeout = fifteenMinutesMS;

                var postChunk = financialTransactionImportList.Take( postChunkSize ).ToList();

                restImportRequest.AddBody( postChunk );

                foreach ( var tran in postChunk.ToList() )
                {
                    financialTransactionImportList.Remove( tran );
                }

                BackgroundWorker.ReportProgress( 0, "Sending FinancialTransaction Import to Rock..." );

                var importResponse = this.RockRestClient.Post( restImportRequest );

                if ( Results.ContainsKey( "FinancialTransaction Import" ) )
                {
                    Results["FinancialTransaction Import"] += "\n" + importResponse.Content.FromJsonOrNull<string>() ?? importResponse.Content;
                }
                else
                {
                    Results.Add( "FinancialTransaction Import", importResponse.Content.FromJsonOrNull<string>() ?? importResponse.Content );
                }

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
                    throw new SlingshotPOSTFailedException( importResponse );
                }
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
            var attendanceImportList = new List<Rock.Slingshot.Model.AttendanceImport>();
            foreach ( var slingshotAttendance in this.SlingshotAttendanceList )
            {
                var attendanceImport = new Rock.Slingshot.Model.AttendanceImport();

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

            RestRequest restImportRequest = new JsonNETRestRequest( "api/BulkImport/AttendanceImport", Method.POST );

            int fifteenMinutesMS = ( 1000 * 60 ) * 15;
            restImportRequest.Timeout = fifteenMinutesMS;

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
                throw new SlingshotPOSTFailedException( importResponse );
            }
        }

        /// <summary>
        /// Submits the schedule import.
        /// </summary>
        private void SubmitScheduleImport()
        {
            BackgroundWorker.ReportProgress( 0, "Preparing ScheduleImport..." );
            var scheduleImportList = new List<Rock.Slingshot.Model.ScheduleImport>();
            foreach ( var slingshotSchedule in this.SlingshotScheduleList )
            {
                var scheduleImport = new Rock.Slingshot.Model.ScheduleImport();
                scheduleImport.ScheduleForeignId = slingshotSchedule.Id;
                scheduleImport.Name = slingshotSchedule.Name;
                scheduleImportList.Add( scheduleImport );
            }

            RestRequest restImportRequest = new JsonNETRestRequest( "api/BulkImport/ScheduleImport", Method.POST );

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
                throw new SlingshotPOSTFailedException( importResponse );
            }
        }

        /// <summary>
        /// Submits the location import.
        /// </summary>
        private void SubmitLocationImport()
        {
            BackgroundWorker.ReportProgress( 0, "Preparing LocationImport..." );
            var locationImportList = new List<Rock.Slingshot.Model.LocationImport>();
            foreach ( var slingshotLocation in this.SlingshotLocationList )
            {
                var locationImport = new Rock.Slingshot.Model.LocationImport();
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

            RestRequest restImportRequest = new JsonNETRestRequest( "api/BulkImport/LocationImport", Method.POST );

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
                throw new SlingshotPOSTFailedException( importResponse );
            }
        }

        /// <summary>
        /// Submits the group import.
        /// </summary>
        private void SubmitGroupImport()
        {
            BackgroundWorker.ReportProgress( 0, "Preparing GroupImport..." );
            var groupImportList = new List<Rock.Slingshot.Model.GroupImport>();
            var campusLookup = this.Campuses.Where( a => a.ForeignId.HasValue ).ToDictionary( k => k.ForeignId.Value, v => v.Id );
            foreach ( var slingshotGroup in this.SlingshotGroupList )
            {
                var groupImport = new Rock.Slingshot.Model.GroupImport();
                groupImport.GroupForeignId = slingshotGroup.Id;
                groupImport.GroupTypeId = this.GroupTypeLookupByForeignId[slingshotGroup.GroupTypeId].Id;

                groupImport.Name = slingshotGroup.Name;
                if ( string.IsNullOrWhiteSpace( slingshotGroup.Name ) )
                {
                    groupImport.Name = "Unnamed Group";
                }

                groupImport.Order = slingshotGroup.Order;
                if ( slingshotGroup.CampusId.HasValue )
                {
                    groupImport.CampusId = campusLookup[slingshotGroup.CampusId.Value];
                }

                groupImport.ParentGroupForeignId = slingshotGroup.ParentGroupId == 0 ? ( int? ) null : slingshotGroup.ParentGroupId;
                groupImport.GroupMemberImports = new List<Rock.Slingshot.Model.GroupMemberImport>();

                foreach ( var groupMember in slingshotGroup.GroupMembers )
                {
                    if ( !groupImport.GroupMemberImports.Any( gm => gm.PersonForeignId == groupMember.PersonId && gm.RoleName == groupMember.Role ) )
                    {
                        var groupMemberImport = new Rock.Slingshot.Model.GroupMemberImport();
                        groupMemberImport.PersonForeignId = groupMember.PersonId;
                        groupMemberImport.RoleName = groupMember.Role;
                        groupImport.GroupMemberImports.Add( groupMemberImport );
                    }
                }

                groupImportList.Add( groupImport );
            }

            RestRequest restImportRequest = new JsonNETRestRequest( "api/BulkImport/GroupImport", Method.POST );

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
                throw new SlingshotPOSTFailedException( importResponse );
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
            List<Rock.Slingshot.Model.PersonImport> personImportList = GetPersonImportList();

            RestRequest restPersonImportRequest = new JsonNETRestRequest( "api/BulkImport/PersonImport", Method.POST );
            restPersonImportRequest.AddBody( personImportList );

            int fifteenMinutesMS = ( 1000 * 60 ) * 15;
            restPersonImportRequest.Timeout = fifteenMinutesMS;

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
                throw new SlingshotPOSTFailedException( importResponse );
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
        private List<Rock.Slingshot.Model.PersonImport> GetPersonImportList()
        {
            List<Rock.Slingshot.Model.PersonImport> personImportList = new List<Rock.Slingshot.Model.PersonImport>();
            foreach ( var slingshotPerson in this.SlingshotPersonList )
            {
                var personImport = new Rock.Slingshot.Model.PersonImport();
                personImport.RecordTypeValueId = this.PersonRecordTypeValues[Rock.Client.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON.AsGuid()].Id;
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

                // slingshot doesn't include an IsEmailActive, so default it to True
                personImport.IsEmailActive = true;

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

                personImport.Note = slingshotPerson.Note;
                personImport.GivingIndividually = slingshotPerson.GiveIndividually;

                // Phone Numbers
                personImport.PhoneNumbers = new List<Rock.Slingshot.Model.PhoneNumberImport>();
                foreach ( var slingshotPersonPhone in slingshotPerson.PhoneNumbers )
                {
                    var phoneNumberImport = new Rock.Slingshot.Model.PhoneNumberImport();
                    phoneNumberImport.NumberTypeValueId = this.PhoneNumberTypeValues[slingshotPersonPhone.PhoneType].Id;
                    phoneNumberImport.Number = slingshotPersonPhone.PhoneNumber;
                    phoneNumberImport.IsMessagingEnabled = slingshotPersonPhone.IsMessagingEnabled ?? false;
                    phoneNumberImport.IsUnlisted = slingshotPersonPhone.IsUnlisted ?? false;
                    personImport.PhoneNumbers.Add( phoneNumberImport );
                }

                // Addresses
                personImport.Addresses = new List<Rock.Slingshot.Model.PersonAddressImport>();
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
                            var addressImport = new Rock.Slingshot.Model.PersonAddressImport()
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
                personImport.AttributeValues = new List<Rock.Slingshot.Model.AttributeValueImport>();
                foreach ( var slingshotPersonAttributeValue in slingshotPerson.Attributes )
                {
                    int attributeId = this.PersonAttributeKeyLookup[slingshotPersonAttributeValue.AttributeKey].Id;
                    var attributeValueImport = new Rock.Slingshot.Model.AttributeValueImport { AttributeId = attributeId, Value = slingshotPersonAttributeValue.AttributeValue };
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
            foreach ( var campus in this.SlingshotPersonList.Select( a => a.Campus ).Where( a => a.CampusId > 0 ) )
            {
                if ( !importCampuses.ContainsKey( campus.CampusId ) )
                {
                    importCampuses.Add( campus.CampusId, campus );
                }
            }

            foreach ( var importCampus in importCampuses.Where( a => !this.Campuses.Any( c => c.Name.Equals( a.Value.CampusName, StringComparison.OrdinalIgnoreCase ) ) ).Select( a => a.Value ) )
            {
                var campusToAdd = new Rock.Client.CampusEntity { ForeignId = importCampus.CampusId, Name = importCampus.CampusName, Guid = Guid.NewGuid() };

                RestRequest restCampusPostRequest = new JsonNETRestRequest( "api/Campuses", Method.POST );
                restCampusPostRequest.AddBody( campusToAdd );

                var postResponse = this.RockRestClient.Post( restCampusPostRequest );

                if ( postResponse.StatusCode != System.Net.HttpStatusCode.Created )
                {
                    throw new SlingshotPOSTFailedException( postResponse );
                }
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

                RestRequest restPostRequest = new JsonNETRestRequest( "api/GroupTypes", Method.POST );
                restPostRequest.AddBody( groupTypeToAdd );

                var postResponse = this.RockRestClient.Post( restPostRequest );

                if ( postResponse.StatusCode != System.Net.HttpStatusCode.Created )
                {
                    throw new SlingshotPOSTFailedException( postResponse );
                }
            }
        }

        /// <summary>
        /// Adds any attribute categories that are in the slingshot files (person and family attributes)
        /// </summary>
        private void AddAttributeCategories()
        {
            int entityTypeIdPerson = this.EntityTypeLookup[Rock.Client.SystemGuid.EntityType.PERSON.AsGuid()].Id;
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
                    attributeCategory.EntityTypeQualifierColumn = "EntityTypeId";
                    attributeCategory.EntityTypeQualifierValue = entityTypeIdPerson.ToString();
                    attributeCategory.Guid = Guid.NewGuid();

                    RestRequest restPostRequest = new JsonNETRestRequest( "api/Categories", Method.POST );
                    restPostRequest.AddBody( attributeCategory );

                    var postResponse = this.RockRestClient.Post<int>( restPostRequest );

                    if ( postResponse.StatusCode != System.Net.HttpStatusCode.Created )
                    {
                        throw new SlingshotPOSTFailedException( postResponse );
                    }

                    attributeCategory.Id = postResponse.Data;
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

                    RestRequest restAttributePostRequest = new JsonNETRestRequest( "api/Attributes", Method.POST );
                    restAttributePostRequest.AddBody( rockPersonAttribute );

                    var postResponse = this.RockRestClient.Post<int>( restAttributePostRequest );

                    if ( postResponse.StatusCode != System.Net.HttpStatusCode.Created )
                    {
                        throw new SlingshotPOSTFailedException( postResponse );
                    }
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

                    RestRequest restAttributePostRequest = new JsonNETRestRequest( "api/Attributes", Method.POST );
                    restAttributePostRequest.AddBody( rockFamilyAttribute );

                    var postResponse = this.RockRestClient.Post<int>( restAttributePostRequest );

                    if ( postResponse.StatusCode != System.Net.HttpStatusCode.Created )
                    {
                        throw new SlingshotPOSTFailedException( postResponse );
                    }
                }
            }
        }

        /// <summary>
        /// Adds the connection statuses.
        /// </summary>
        private void AddConnectionStatuses()
        {
            AddDefinedValues( this.SlingshotPersonList.Select( a => a.ConnectionStatus ).Where( a => !string.IsNullOrWhiteSpace( a ) ).Distinct().ToList(), this.PersonConnectionStatusValues );
        }

        /// <summary>
        /// Adds the person titles.
        /// </summary>
        private void AddPersonTitles()
        {
            AddDefinedValues( this.SlingshotPersonList.Select( a => a.Salutation ).Where( a => !string.IsNullOrWhiteSpace( a ) ).Distinct().ToList(), this.PersonTitleValues );
        }

        /// <summary>
        /// Adds the person suffixes.
        /// </summary>
        private void AddPersonSuffixes()
        {
            AddDefinedValues( this.SlingshotPersonList.Select( a => a.Suffix ).Where( a => !string.IsNullOrWhiteSpace( a ) ).Distinct().ToList(), this.PersonSuffixValues );
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

                RestRequest restDefinedValuePostRequest = new JsonNETRestRequest( "api/DefinedValues", Method.POST );
                restDefinedValuePostRequest.AddBody( definedValueToAdd );

                var postResponse = this.RockRestClient.Post( restDefinedValuePostRequest );

                if ( postResponse.StatusCode != System.Net.HttpStatusCode.Created )
                {
                    throw new SlingshotPOSTFailedException( postResponse );
                }
            }
        }

        /// <summary>
        /// Loads all the slingshot lists
        /// </summary>
        /// <returns></returns>
        private void LoadSlingshotLists()
        {
            LoadPersonSlingshotLists();

            var familyAttributesFileName = Path.Combine( this.SlingshotDirectoryName, new Slingshot.Core.Model.FamilyAttribute().GetFileName() );
            if ( File.Exists( familyAttributesFileName ) )
            {
                using ( var slingshotFileStream = File.OpenText( familyAttributesFileName ) )
                {
                    CsvReader csvReader = new CsvReader( slingshotFileStream );
                    csvReader.Configuration.HasHeaderRecord = true;
                    this.SlingshotFamilyAttributes = csvReader.GetRecords<Slingshot.Core.Model.FamilyAttribute>().ToList();
                }
            }
            else
            {
                this.SlingshotFamilyAttributes = new List<Core.Model.FamilyAttribute>();
            }

            /* Attendance */
            var attendanceFileName = Path.Combine( this.SlingshotDirectoryName, new Slingshot.Core.Model.Attendance().GetFileName() );
            if ( File.Exists( attendanceFileName ) )
            {
                using ( var slingshotFileStream = File.OpenText( attendanceFileName ) )
                {
                    CsvReader csvReader = new CsvReader( slingshotFileStream );
                    csvReader.Configuration.HasHeaderRecord = true;
                    this.SlingshotAttendanceList = csvReader.GetRecords<Slingshot.Core.Model.Attendance>().ToList();
                }
            }
            else
            {
                this.SlingshotAttendanceList = new List<Core.Model.Attendance>();
            }

            var groupFileName = Path.Combine( this.SlingshotDirectoryName, new Slingshot.Core.Model.Group().GetFileName() );
            if ( File.Exists( groupFileName ) )
            {
                using ( var slingshotFileStream = File.OpenText( groupFileName ) )
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
            }
            else
            {
                this.SlingshotGroupList = new List<Core.Model.Group>();
            }

            var groupMemberFileName = Path.Combine( this.SlingshotDirectoryName, new Slingshot.Core.Model.GroupMember().GetFileName() );
            if ( File.Exists( groupMemberFileName ) )
            {
                var groupLookup = this.SlingshotGroupList.ToDictionary( k => k.Id, v => v );
                using ( var slingshotFileStream = File.OpenText( groupMemberFileName ) )
                {
                    CsvReader csvReader = new CsvReader( slingshotFileStream );
                    csvReader.Configuration.HasHeaderRecord = true;

                    var groupMemberList = csvReader.GetRecords<Slingshot.Core.Model.GroupMember>().ToList().GroupBy( a => a.GroupId ).ToDictionary( k => k.Key, v => v.ToList() );
                    foreach ( var groupIdMembers in groupMemberList )
                    {
                        groupLookup[groupIdMembers.Key].GroupMembers = groupIdMembers.Value;
                    }
                }
            }

            var groupTypeFileName = Path.Combine( this.SlingshotDirectoryName, new Slingshot.Core.Model.GroupType().GetFileName() );
            if ( File.Exists( groupTypeFileName ) )
            {
                using ( var slingshotFileStream = File.OpenText( groupTypeFileName ) )
                {
                    CsvReader csvReader = new CsvReader( slingshotFileStream );
                    csvReader.Configuration.HasHeaderRecord = true;
                    this.SlingshotGroupTypeList = csvReader.GetRecords<Slingshot.Core.Model.GroupType>().ToList();
                }
            }
            else
            {
                this.SlingshotGroupTypeList = new List<Core.Model.GroupType>();
            }

            var locationFileName = Path.Combine( this.SlingshotDirectoryName, new Slingshot.Core.Model.Location().GetFileName() );
            if ( File.Exists( locationFileName ) )
            {
                using ( var slingshotFileStream = File.OpenText( locationFileName ) )
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
            }
            else
            {
                this.SlingshotLocationList = new List<Core.Model.Location>();
            }

            var scheduleFileName = Path.Combine( this.SlingshotDirectoryName, new Slingshot.Core.Model.Schedule().GetFileName() );
            if ( File.Exists( scheduleFileName ) )
            {
                using ( var slingshotFileStream = File.OpenText( scheduleFileName ) )
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
            }
            else
            {
                this.SlingshotScheduleList = new List<Core.Model.Schedule>();
            }


            /* Financial Transactions */
            var financialAccountFileName = Path.Combine( this.SlingshotDirectoryName, new Slingshot.Core.Model.FinancialAccount().GetFileName() );
            if ( File.Exists( financialAccountFileName ) )
            {
                using ( var slingshotFileStream = File.OpenText( financialAccountFileName ) )
                {
                    CsvReader csvReader = new CsvReader( slingshotFileStream );
                    csvReader.Configuration.HasHeaderRecord = true;
                    this.SlingshotFinancialAccountList = csvReader.GetRecords<Slingshot.Core.Model.FinancialAccount>().ToList();
                }
            }
            else
            {
                this.SlingshotFinancialAccountList = new List<Core.Model.FinancialAccount>();
            }

            var financialTransactionFileName = Path.Combine( this.SlingshotDirectoryName, new Slingshot.Core.Model.FinancialTransaction().GetFileName() );
            if ( File.Exists( financialTransactionFileName ) )
            {
                using ( var slingshotFileStream = File.OpenText( financialTransactionFileName ) )
                {
                    CsvReader csvReader = new CsvReader( slingshotFileStream );
                    csvReader.Configuration.HasHeaderRecord = true;
                    this.SlingshotFinancialTransactionList = csvReader.GetRecords<Slingshot.Core.Model.FinancialTransaction>().ToList();
                } 
            }
            else
            {
                this.SlingshotFinancialTransactionList = new List<Core.Model.FinancialTransaction>();
            }

            var financialTransactionDetailFileName = Path.Combine( this.SlingshotDirectoryName, new Slingshot.Core.Model.FinancialTransactionDetail().GetFileName() );
            if ( File.Exists( financialTransactionDetailFileName ) )
            {
                using ( var slingshotFileStream = File.OpenText( financialTransactionDetailFileName ) )
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
            }

            var financialBatchFileName = Path.Combine( this.SlingshotDirectoryName, new Slingshot.Core.Model.FinancialBatch().GetFileName() );
            if ( File.Exists( financialBatchFileName ) )
            {
                using ( var slingshotFileStream = File.OpenText( financialBatchFileName ) )
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
            else
            {
                this.SlingshotFinancialBatchList = new List<Core.Model.FinancialBatch>();
            }
        }

        /// <summary>
        /// Loads the person slingshot lists.
        /// </summary>
        private void LoadPersonSlingshotLists()
        {
            Dictionary<int, List<Slingshot.Core.Model.PersonAddress>> slingshotPersonAddressListLookup;
            Dictionary<int, List<Slingshot.Core.Model.PersonAttributeValue>> slingshotPersonAttributeValueListLookup;
            Dictionary<int, List<Slingshot.Core.Model.PersonPhone>> slingshotPersonPhoneListLookup;

            using ( var slingshotFileStream = File.OpenText( Path.Combine( this.SlingshotDirectoryName, new Slingshot.Core.Model.Person().GetFileName() ) ) )
            {
                CsvReader csvReader = new CsvReader( slingshotFileStream );
                csvReader.Configuration.HasHeaderRecord = true;
                csvReader.Configuration.WillThrowOnMissingField = false;
                this.SlingshotPersonList = csvReader.GetRecords<Slingshot.Core.Model.Person>().ToList();
            }

            using ( var slingshotFileStream = File.OpenText( Path.Combine( this.SlingshotDirectoryName, new Slingshot.Core.Model.PersonAddress().GetFileName() ) ) )
            {
                CsvReader csvReader = new CsvReader( slingshotFileStream );
                csvReader.Configuration.HasHeaderRecord = true;
                slingshotPersonAddressListLookup = csvReader.GetRecords<Slingshot.Core.Model.PersonAddress>().GroupBy( a => a.PersonId ).ToDictionary( k => k.Key, v => v.ToList() );
            }

            using ( var slingshotFileStream = File.OpenText( Path.Combine( this.SlingshotDirectoryName, new Slingshot.Core.Model.PersonAttributeValue().GetFileName() ) ) )
            {
                CsvReader csvReader = new CsvReader( slingshotFileStream );
                csvReader.Configuration.HasHeaderRecord = true;
                slingshotPersonAttributeValueListLookup = csvReader.GetRecords<Slingshot.Core.Model.PersonAttributeValue>().GroupBy( a => a.PersonId ).ToDictionary( k => k.Key, v => v.ToList() );
            }

            using ( var slingshotFileStream = File.OpenText( Path.Combine( this.SlingshotDirectoryName, new Slingshot.Core.Model.PersonPhone().GetFileName() ) ) )
            {
                CsvReader csvReader = new CsvReader( slingshotFileStream );
                csvReader.Configuration.HasHeaderRecord = true;
                slingshotPersonPhoneListLookup = csvReader.GetRecords<Slingshot.Core.Model.PersonPhone>().GroupBy( a => a.PersonId ).ToDictionary( k => k.Key, v => v.ToList() );
            }

            foreach ( var slingshotPerson in this.SlingshotPersonList )
            {
                slingshotPerson.Addresses = slingshotPersonAddressListLookup.ContainsKey( slingshotPerson.Id ) ? slingshotPersonAddressListLookup[slingshotPerson.Id] : new List<Slingshot.Core.Model.PersonAddress>();
                slingshotPerson.Attributes = slingshotPersonAttributeValueListLookup.ContainsKey( slingshotPerson.Id ) ? slingshotPersonAttributeValueListLookup[slingshotPerson.Id].ToList() : new List<Slingshot.Core.Model.PersonAttributeValue>();
                slingshotPerson.PhoneNumbers = slingshotPersonPhoneListLookup.ContainsKey( slingshotPerson.Id ) ? slingshotPersonPhoneListLookup[slingshotPerson.Id].ToList() : new List<Slingshot.Core.Model.PersonPhone>();
            }

            using ( var slingshotFileStream = File.OpenText( Path.Combine( this.SlingshotDirectoryName, new Slingshot.Core.Model.PersonAttribute().GetFileName() ) ) )
            {
                CsvReader csvReader = new CsvReader( slingshotFileStream );
                csvReader.Configuration.HasHeaderRecord = true;
                this.SlingshotPersonAttributes = csvReader.GetRecords<Slingshot.Core.Model.PersonAttribute>().ToList();
            }
        }

        /// <summary>
        /// Ensures that the defined values that we need exist on the Rock Server
        /// </summary>
        private void EnsureDefinedValues()
        {
            List<Rock.Client.DefinedValue> definedValuesToAdd = new List<Rock.Client.DefinedValue>();
            int definedTypeIdCurrencyType = this.DefinedTypeLookup[Rock.Client.SystemGuid.DefinedType.FINANCIAL_CURRENCY_TYPE.AsGuid()].Id;
            int definedTypeIdTransactionSourceType = this.DefinedTypeLookup[Rock.Client.SystemGuid.DefinedType.FINANCIAL_SOURCE_TYPE.AsGuid()].Id;

            // The following DefinedValues are not IsSystem, but are potentionally needed to do an import, so make sure they exist on the server
            if ( !this.CurrencyTypeValues.ContainsKey( Rock.Client.SystemGuid.DefinedValue.CURRENCY_TYPE_NONCASH.AsGuid() ) )
            {
                definedValuesToAdd.Add( new Rock.Client.DefinedValue
                {
                    DefinedTypeId = definedTypeIdCurrencyType,
                    Guid = Rock.Client.SystemGuid.DefinedValue.CURRENCY_TYPE_NONCASH.AsGuid(),
                    Value = "Non-Cash",
                    Description = "Used to track non-cash transactions."
                } );
            }

            if ( !this.CurrencyTypeValues.ContainsKey( Rock.Client.SystemGuid.DefinedValue.CURRENCY_TYPE_UNKNOWN.AsGuid() ) )
            {
                definedValuesToAdd.Add( new Rock.Client.DefinedValue
                {
                    DefinedTypeId = definedTypeIdCurrencyType,
                    Guid = Rock.Client.SystemGuid.DefinedValue.CURRENCY_TYPE_UNKNOWN.AsGuid(),
                    Value = "Unknown",
                    Description = "The currency type is unknown. For example, it might have been imported from a system that doesn't indicate currency type."
                } );
            }

            if ( !this.TransactionSourceTypeValues.ContainsKey( Rock.Client.SystemGuid.DefinedValue.FINANCIAL_SOURCE_TYPE_BANK_CHECK.AsGuid() ) )
            {
                definedValuesToAdd.Add( new Rock.Client.DefinedValue
                {
                    DefinedTypeId = definedTypeIdTransactionSourceType,
                    Guid = Rock.Client.SystemGuid.DefinedValue.FINANCIAL_SOURCE_TYPE_BANK_CHECK.AsGuid(),
                    Value = "Bank Checks",
                    Description = "Transactions that originated from a bank's bill pay system"
                } );
            }

            if ( !this.TransactionSourceTypeValues.ContainsKey( Rock.Client.SystemGuid.DefinedValue.FINANCIAL_SOURCE_TYPE_KIOSK.AsGuid() ) )
            {
                definedValuesToAdd.Add( new Rock.Client.DefinedValue
                {
                    DefinedTypeId = definedTypeIdTransactionSourceType,
                    Guid = Rock.Client.SystemGuid.DefinedValue.FINANCIAL_SOURCE_TYPE_KIOSK.AsGuid(),
                    Value = "Kiosk",
                    Description = "Transactions that originated from a kiosk"
                } );
            }

            if ( !this.TransactionSourceTypeValues.ContainsKey( Rock.Client.SystemGuid.DefinedValue.FINANCIAL_SOURCE_TYPE_MOBILE_APPLICATION.AsGuid() ) )
            {
                definedValuesToAdd.Add( new Rock.Client.DefinedValue
                {
                    DefinedTypeId = definedTypeIdTransactionSourceType,
                    Guid = Rock.Client.SystemGuid.DefinedValue.FINANCIAL_SOURCE_TYPE_MOBILE_APPLICATION.AsGuid(),
                    Value = "Mobile Application",
                    Description = "Transactions that originated from a mobile application"
                } );
            }

            if ( !this.TransactionSourceTypeValues.ContainsKey( Rock.Client.SystemGuid.DefinedValue.FINANCIAL_SOURCE_TYPE_ONSITE_COLLECTION.AsGuid() ) )
            {
                definedValuesToAdd.Add( new Rock.Client.DefinedValue
                {
                    DefinedTypeId = definedTypeIdTransactionSourceType,
                    Guid = Rock.Client.SystemGuid.DefinedValue.FINANCIAL_SOURCE_TYPE_ONSITE_COLLECTION.AsGuid(),
                    Value = "On-Site Collection",
                    Description = "Transactions that were collected on-site"
                } );
            }

            foreach ( var definedValueToAdd in definedValuesToAdd )
            {
                RestRequest restDefinedValuePostRequest = new JsonNETRestRequest( "api/DefinedValues", Method.POST );
                restDefinedValuePostRequest.AddBody( definedValueToAdd );

                var postResponse = this.RockRestClient.Post( restDefinedValuePostRequest );

                if ( postResponse.StatusCode != System.Net.HttpStatusCode.Created )
                {
                    throw new SlingshotPOSTFailedException( postResponse );
                }
            }
        }

        /// <summary>
        /// Loads the lookups.
        /// </summary>
        private void LoadLookups()
        {
            this.PersonRecordTypeValues = LoadDefinedValues( this.RockRestClient, Rock.Client.SystemGuid.DefinedType.PERSON_RECORD_TYPE.AsGuid() );
            this.PersonRecordStatusValues = LoadDefinedValues( this.RockRestClient, Rock.Client.SystemGuid.DefinedType.PERSON_RECORD_STATUS.AsGuid() );
            this.PersonConnectionStatusValues = LoadDefinedValues( this.RockRestClient, Rock.Client.SystemGuid.DefinedType.PERSON_CONNECTION_STATUS.AsGuid() ).Select( a => a.Value ).ToDictionary( k => k.Value, v => v );
            this.PersonTitleValues = LoadDefinedValues( this.RockRestClient, Rock.Client.SystemGuid.DefinedType.PERSON_TITLE.AsGuid() ).Select( a => a.Value ).ToDictionary( k => k.Value, v => v );
            this.PersonSuffixValues = LoadDefinedValues( this.RockRestClient, Rock.Client.SystemGuid.DefinedType.PERSON_SUFFIX.AsGuid() ).Select( a => a.Value ).ToDictionary( k => k.Value, v => v );
            this.PersonMaritalStatusValues = LoadDefinedValues( this.RockRestClient, Rock.Client.SystemGuid.DefinedType.PERSON_MARITAL_STATUS.AsGuid() );
            this.PhoneNumberTypeValues = LoadDefinedValues( this.RockRestClient, Rock.Client.SystemGuid.DefinedType.PERSON_PHONE_TYPE.AsGuid() ).Select( a => a.Value ).ToDictionary( k => k.Value, v => v );
            this.GroupLocationTypeValues = LoadDefinedValues( this.RockRestClient, Rock.Client.SystemGuid.DefinedType.GROUP_LOCATION_TYPE.AsGuid() );
            this.LocationTypeValues = LoadDefinedValues( this.RockRestClient, Rock.Client.SystemGuid.DefinedType.LOCATION_TYPE.AsGuid() );
            this.CurrencyTypeValues = LoadDefinedValues( this.RockRestClient, Rock.Client.SystemGuid.DefinedType.FINANCIAL_CURRENCY_TYPE.AsGuid() );
            this.TransactionSourceTypeValues = LoadDefinedValues( this.RockRestClient, Rock.Client.SystemGuid.DefinedType.FINANCIAL_SOURCE_TYPE.AsGuid() );
            this.TransactionTypeValues = LoadDefinedValues( this.RockRestClient, Rock.Client.SystemGuid.DefinedType.FINANCIAL_TRANSACTION_TYPE.AsGuid() );

            // EntityTypes
            RestRequest requestEntityTypes = new JsonNETRestRequest( Method.GET );
            requestEntityTypes.Resource = "api/EntityTypes";
            var requestEntityTypesResponse = this.RockRestClient.Execute( requestEntityTypes );
            if ( requestEntityTypesResponse.StatusCode != System.Net.HttpStatusCode.OK )
            {
                throw new SlingshotGETFailedException( requestEntityTypesResponse );
            }

            var entityTypes = JsonConvert.DeserializeObject<List<Rock.Client.EntityType>>( requestEntityTypesResponse.Content );
            this.EntityTypeLookup = entityTypes.ToDictionary( k => k.Guid, v => v );

            int entityTypeIdPerson = this.EntityTypeLookup[Rock.Client.SystemGuid.EntityType.PERSON.AsGuid()].Id;
            int entityTypeIdGroup = this.EntityTypeLookup[Rock.Client.SystemGuid.EntityType.GROUP.AsGuid()].Id;
            int entityTypeIdAttribute = this.EntityTypeLookup[Rock.Client.SystemGuid.EntityType.ATTRIBUTE.AsGuid()].Id;

            // DefinedTypes
            RestRequest requestDefinedTypes = new JsonNETRestRequest( Method.GET );
            requestDefinedTypes.Resource = "api/DefinedTypes";
            var requestDefinedTypesResponse = this.RockRestClient.Execute( requestDefinedTypes );
            if ( requestDefinedTypesResponse.StatusCode != System.Net.HttpStatusCode.OK )
            {
                throw new SlingshotGETFailedException( requestDefinedTypesResponse );
            }

            var definedTypes = JsonConvert.DeserializeObject<List<Rock.Client.DefinedType>>( requestDefinedTypesResponse.Content );
            this.DefinedTypeLookup = definedTypes.ToDictionary( k => k.Guid, v => v );

            // Family GroupTypeRoles
            RestRequest requestFamilyGroupType = new JsonNETRestRequest( Method.GET );
            requestFamilyGroupType.Resource = $"api/GroupTypes?$filter=Guid eq guid'{Rock.Client.SystemGuid.GroupType.GROUPTYPE_FAMILY}'&$expand=Roles";
            var familyGroupTypeResponse = this.RockRestClient.Execute( requestFamilyGroupType );
            if ( familyGroupTypeResponse.StatusCode != System.Net.HttpStatusCode.OK )
            {
                throw new SlingshotGETFailedException( familyGroupTypeResponse );
            }

            this.FamilyRoles = JsonConvert.DeserializeObject<List<Rock.Client.GroupType>>( familyGroupTypeResponse.Content ).FirstOrDefault().Roles.ToDictionary( k => k.Guid, v => v );

            // Campuses
            RestRequest requestCampuses = new JsonNETRestRequest( Method.GET );
            requestCampuses.Resource = "api/Campuses";
            var campusResponse = this.RockRestClient.Execute( requestCampuses );
            if ( campusResponse.StatusCode != System.Net.HttpStatusCode.OK )
            {
                throw new SlingshotGETFailedException( campusResponse );
            }

            this.Campuses = JsonConvert.DeserializeObject<List<Rock.Client.Campus>>( campusResponse.Content );

            // Person Attributes
            RestRequest requestPersonAttributes = new JsonNETRestRequest( Method.GET );
            requestPersonAttributes.Resource = $"api/Attributes?$filter=EntityTypeId eq {entityTypeIdPerson}&$expand=FieldType";
            var personAttributesResponse = this.RockRestClient.Execute( requestPersonAttributes );
            if ( personAttributesResponse.StatusCode != System.Net.HttpStatusCode.OK )
            {
                throw new SlingshotGETFailedException( personAttributesResponse );
            }

            var personAttributes = JsonConvert.DeserializeObject<List<Rock.Client.Attribute>>( personAttributesResponse.Content );
            this.PersonAttributeKeyLookup = personAttributes.ToDictionary( k => k.Key, v => v );

            // Family Attributes
            this.GroupTypeIdFamily = this.FamilyRoles.Select( a => a.Value.GroupTypeId.Value ).First();
            RestRequest requestFamilyAttributes = new JsonNETRestRequest( Method.GET );
            requestFamilyAttributes.Resource = $"api/Attributes?$filter=EntityTypeId eq {entityTypeIdGroup} and EntityTypeQualifierColumn eq 'GroupTypeId' and EntityTypeQualifierValue eq '{this.GroupTypeIdFamily}'&$expand=FieldType";
            var familyAttributesResponse = this.RockRestClient.Execute( requestFamilyAttributes );
            if ( familyAttributesResponse.StatusCode != System.Net.HttpStatusCode.OK )
            {
                throw new SlingshotGETFailedException( familyAttributesResponse );
            }

            var familyAttributes = JsonConvert.DeserializeObject<List<Rock.Client.Attribute>>( familyAttributesResponse.Content );
            this.FamilyAttributeKeyLookup = familyAttributes.ToDictionary( k => k.Key, v => v );

            // Attribute Categories
            RestRequest requestAttributeCategories = new JsonNETRestRequest( Method.GET );
            requestAttributeCategories.Resource = $"api/Categories?$filter=EntityTypeId eq {entityTypeIdAttribute}";
            var requestAttributeCategoriesResponse = this.RockRestClient.Execute( requestAttributeCategories );
            if ( requestAttributeCategoriesResponse.StatusCode != System.Net.HttpStatusCode.OK )
            {
                throw new SlingshotGETFailedException( requestAttributeCategoriesResponse );
            }

            this.AttributeCategoryList = JsonConvert.DeserializeObject<List<Rock.Client.Category>>( requestAttributeCategoriesResponse.Content );

            // FieldTypes
            RestRequest requestFieldTypes = new JsonNETRestRequest( Method.GET );
            requestFieldTypes.Resource = "api/FieldTypes";
            var requestFieldTypesResponse = this.RockRestClient.Execute( requestFieldTypes );
            if ( requestFieldTypesResponse.StatusCode != System.Net.HttpStatusCode.OK )
            {
                throw new SlingshotGETFailedException( requestFieldTypesResponse );
            }

            var fieldTypes = JsonConvert.DeserializeObject<List<Rock.Client.FieldType>>( requestFieldTypesResponse.Content );
            this.FieldTypeLookup = fieldTypes.ToDictionary( k => k.Class, v => v );

            // GroupTypes
            RestRequest requestGroupTypes = new JsonNETRestRequest( Method.GET );
            requestGroupTypes.Resource = "api/GroupTypes?$filter=ForeignId ne null";
            var requestGroupTypesResponse = this.RockRestClient.Execute( requestGroupTypes );
            if ( requestGroupTypesResponse.StatusCode != System.Net.HttpStatusCode.OK )
            {
                throw new SlingshotGETFailedException( requestGroupTypesResponse );
            }

            var groupTypes = JsonConvert.DeserializeObject<List<Rock.Client.GroupType>>( requestGroupTypesResponse.Content );
            this.GroupTypeLookupByForeignId = groupTypes.ToDictionary( k => k.ForeignId.Value, v => v );
        }

        /// <summary>
        /// Gets the rock rest client.
        /// </summary>
        /// <returns></returns>
        private RestClient GetRockRestClient()
        {
            RestClient restClient = new RestClient( this.RockUrl );

            restClient.CookieContainer = new System.Net.CookieContainer();

            RestRequest restLoginRequest = new JsonNETRestRequest( "api/auth/login", Method.POST );
            var loginParameters = new
            {
                UserName = this.RockUserName,
                Password = this.RockPassword
            };

            restLoginRequest.AddBody( loginParameters );
            var loginResponse = restClient.Post( restLoginRequest );
            if ( loginResponse.StatusCode != System.Net.HttpStatusCode.NoContent )
            {
                throw new SlingshotLoginFailedException( "Unable to login" );
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
            RestRequest requestDefinedType = new JsonNETRestRequest( Method.GET );

            requestDefinedType.Resource = $"api/DefinedTypes?$filter=Guid eq guid'{definedTypeGuid}'&$expand=DefinedValues";

            var definedTypeResponse = restClient.Execute( requestDefinedType );
            var definedValues = JsonConvert.DeserializeObject<List<Rock.Client.DefinedType>>( definedTypeResponse.Content ).FirstOrDefault().DefinedValues;

            return definedValues.ToList().ToDictionary( k => k.Guid, v => v );
        }
    }
}
			