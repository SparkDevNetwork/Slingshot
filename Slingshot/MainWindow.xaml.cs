using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Windows;
using CsvHelper;
using Microsoft.Win32;
using Newtonsoft.Json;
using RestSharp;
using Rock;
using Path = System.IO.Path;

namespace Slingshot
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// <seealso cref="System.Windows.Window" />
    /// <seealso cref="System.Windows.Markup.IComponentConnector" />
    public partial class MainWindow : Window
    {

        public Dictionary<Guid, Rock.Client.GroupTypeRole> FamilyRoles { get; private set; }

        public Dictionary<Guid, Rock.Client.DefinedValue> PersonRecordTypeValues { get; private set; }

        public Dictionary<Guid, Rock.Client.DefinedValue> PersonRecordStatusValues { get; private set; }
        public Dictionary<string, Rock.Client.DefinedValue> PersonConnectionStatusValues { get; private set; }
        public Dictionary<string, Rock.Client.DefinedValue> PersonTitleValues { get; private set; }
        public Dictionary<string, Rock.Client.DefinedValue> PersonSuffixValues { get; private set; }
        public Dictionary<Guid, Rock.Client.DefinedValue> PersonMaritalStatusValues { get; private set; }
        public Dictionary<Guid, Rock.Client.DefinedValue> PhoneNumberTypeValues { get; private set; }
        public Dictionary<Guid, Rock.Client.DefinedValue> GroupLocationTypeValues { get; private set; }
        public List<Rock.Client.Campus> Campuses { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Handles the Click event of the btnGo control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void btnGo_Click( object sender, RoutedEventArgs e )
        {
            // Load Rock Lookups
            LoadLookups();

            // Load Slingshot Models from .slingshot
            var slingshotPersonList = LoadSlingshotPersonList();

            var restClient = this.GetRockRestClient();

            // Populate Rock with stuff that comes from the Slingshot file
            AddCampuses( restClient, slingshotPersonList );
            AddConnectionStatuses( restClient, slingshotPersonList );
            AddPersonTitles( restClient, slingshotPersonList );
            AddPersonSuffixes( restClient, slingshotPersonList );

            // load lookups again in case we added some new ones
            LoadLookups();

            List<Rock.BulkUpdate.PersonImport> personImportList = new List<Rock.BulkUpdate.PersonImport>();
            foreach ( var slingshotPerson in slingshotPersonList )
            {
                var personImport = new Rock.BulkUpdate.PersonImport();
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
                    personImport.BirthYear = slingshotPerson.Birthdate.Value.Year == slingshotPerson.BirthdateNoYearMagicYear ? (int?)null : slingshotPerson.Birthdate.Value.Year;
                }

                switch ( slingshotPerson.Gender )
                {
                    case Core.Model.Gender.Male:
                        personImport.Gender = (int)Rock.Client.Enums.Gender.Male;
                        break;
                    case Core.Model.Gender.Female:
                        personImport.Gender = (int)Rock.Client.Enums.Gender.Female;
                        break;
                    case Core.Model.Gender.Unknown:
                        personImport.Gender = (int)Rock.Client.Enums.Gender.Unknown;
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
                        personImport.EmailPreference = (int)Rock.Client.Enums.EmailPreference.EmailAllowed;
                        break;
                    case Core.Model.EmailPreference.DoNotEmail:
                        personImport.EmailPreference = (int)Rock.Client.Enums.EmailPreference.DoNotEmail;
                        break;
                    case Core.Model.EmailPreference.NoMassEmails:
                        personImport.EmailPreference = (int)Rock.Client.Enums.EmailPreference.NoMassEmails;
                        break;
                }

                personImport.CreatedDateTime = slingshotPerson.CreatedDateTime;
                personImport.ModifiedDateTime = slingshotPerson.ModifiedDateTime;
                personImport.PersonPhotoUrl = slingshotPerson.PersonPhotoUrl;

                personImport.Note = slingshotPerson.Note;
                personImport.GivingIndividually = slingshotPerson.GiveIndividually ?? false;

                personImport.AttributeValues = new List<Rock.BulkUpdate.AttributeValueImport>();
                foreach( var personAttributeValue in slingshotPerson.Attributes )
                {
                    // TODO
                    //var attributeValueImport = new Rock.BulkUpdate.AttributeValueImport(personAttributeValue.);
                    //personImport.AttributeValues.Add( attributeValueImport )
                }

                personImportList.Add( personImport );
            }

            RestRequest restPersonImportRequest = new RestRequest( "api/PersonImport", Method.POST );
            restPersonImportRequest.RequestFormat = RestSharp.DataFormat.Json;
            restPersonImportRequest.AddBody( personImportList );

            // NOTE!: WebConfig needs to be configured to allow posts larger than 10MB
            var importResponse = restClient.Post( restPersonImportRequest );

            if ( importResponse.StatusCode == System.Net.HttpStatusCode.Created )
            {

            }


            /*
            foreach ( var slingshotPerson in slingshotPersonList )
            {
                var personImport = new Rock.BulkUpdate.PersonImport
                {
                    PersonForeignId = slingshotPerson.Id,
                    FamilyForeignId = slingshotPerson.FamilyId ?? 0,
                    GroupRoleId = slingshotPerson.FamilyRole == FamilyRole.Adult ? adultRoleId : childRoleId,
                    GivingIndividually = slingshotPerson.GiveIndividually ?? false,
                  //  RecordTypeValueId = personRecordTypeValueId
                };

                // Question: Can FamilyId be NULL?
                // NULL means autogenerate a family

                if ( personImport.PersonForeignId == 0 || personImport.FamilyForeignId == 0 )
                {
                    throw new Exception( "personImport.PersonForeignId == 0 || personImport.FamilyForeignId == 0" );
                }

                // TODO: Figure out stragety for Campus Lookup and mapping
                //

                switch ( slingshotPerson.RecordStatus )
                {
                    case RecordStatus.Active:
                    //    personImport.RecordStatusValueId = recordStatusValueActiveId;
                        break;

                    case RecordStatus.Inactive:
                      //  personImport.RecordStatusValueId = recordStatusValueInActiveId;
                        break;

                    case RecordStatus.Pending:
                      //  personImport.RecordStatusValueId = recordStatusValuePendingId;
                        break;
                }
                

                personImport.FirstName = slingshotPerson.FirstName;
                personImport.NickName = slingshotPerson.NickName;
                personImport.MiddleName = slingshotPerson.MiddleName;
                personImport.LastName = slingshotPerson.LastName;

                // Question: Is there an Birthday,Month,Year or a "MagicYear"? 
                // Magic Year 9999

                if ( slingshotPerson.Birthdate.HasValue )
                {
                    personImport.BirthDay = slingshotPerson.Birthdate.Value.Day;
                    personImport.BirthMonth = slingshotPerson.Birthdate.Value.Month;
                    personImport.BirthYear = slingshotPerson.Birthdate.Value.Year;
                }

                switch ( slingshotPerson.Gender )
                {
                    case Gender.Male:
                        personImport.Gender = (int)Rock.Client.Enums.Gender.Male;
                        break;
                    case Gender.Female:
                        personImport.Gender = (int)Rock.Client.Enums.Gender.Female;
                        break;
                    default:
                        personImport.Gender = (int)Rock.Client.Enums.Gender.Unknown;
                        break;
                }

                switch ( slingshotPerson.MaritalStatus )
                {
                    case MaritalStatus.Married:
                  //      personImport.MaritalStatusValueId = maritalStatusMarriedId;
                        break;
                    case MaritalStatus.Single:
                   //     personImport.MaritalStatusValueId = maritalStatusSingleId;
                        break;
                    case MaritalStatus.Divorced:
                     //   personImport.MaritalStatusValueId = maritalStatusDivorcedId;
                        break;
                    case MaritalStatus.Unknown:
                     //   personImport.MaritalStatusValueId = maritalStatusUnknownId;
                        break;
                }

                personImport.AnniversaryDate = slingshotPerson.AnniversaryDate;


                // TODO figure this out
                // personImport.GraduationYear = slingshotPerson.Grade;

    

            }
    
        }*/
        }

        /// <summary>
        /// Add any campuses that aren't in Rock yet
        /// </summary>
        /// <param name="restClient">The rest client.</param>
        /// <param name="slingshotPersonList">The slingshot person list.</param>
        private void AddCampuses( RestClient restClient, List<Core.Model.Person> slingshotPersonList )
        {
            Dictionary<int, Slingshot.Core.Model.Campus> importCampuses = new Dictionary<int, Slingshot.Core.Model.Campus>();
            foreach ( var campus in slingshotPersonList.Select( a => a.Campus ) )
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

                var postCampusResponse = restClient.Post( restCampusPostRequest );
            }
        }

        /// <summary>
        /// Adds the connection statuses.
        /// </summary>
        /// <param name="restClient">The rest client.</param>
        /// <param name="slingshotPersonList">The slingshot person list.</param>
        private void AddConnectionStatuses( RestClient restClient, List<Core.Model.Person> slingshotPersonList )
        {
            AddDefinedValues( restClient, slingshotPersonList.Select( a => a.ConnectionStatus ).Distinct().ToList(), this.PersonConnectionStatusValues );
        }

        /// <summary>
        /// Adds the person titles.
        /// </summary>
        /// <param name="restClient">The rest client.</param>
        /// <param name="slingshotPersonList">The slingshot person list.</param>
        private void AddPersonTitles( RestClient restClient, List<Core.Model.Person> slingshotPersonList )
        {
            AddDefinedValues( restClient, slingshotPersonList.Select( a => a.Salutation ).Distinct().ToList(), this.PersonTitleValues );
        }

        /// <summary>
        /// Adds the person suffixes.
        /// </summary>
        /// <param name="restClient">The rest client.</param>
        /// <param name="slingshotPersonList">The slingshot person list.</param>
        private void AddPersonSuffixes( RestClient restClient, List<Core.Model.Person> slingshotPersonList )
        {
            AddDefinedValues( restClient, slingshotPersonList.Select( a => a.Suffix ).Distinct().ToList(), this.PersonSuffixValues );
        }

        /// <summary>
        /// Adds the defined values.
        /// </summary>
        /// <param name="restClient">The rest client.</param>
        /// <param name="importDefinedValues">The import defined values.</param>
        /// <param name="currentValues">The current values.</param>
        private static void AddDefinedValues( RestClient restClient, List<string> importDefinedValues, Dictionary<string, Rock.Client.DefinedValue> currentValues )
        {
            var definedTypeId = currentValues.Select( a => a.Value.DefinedTypeId ).First();
            foreach ( var importDefinedValue in importDefinedValues.Where( value => !currentValues.Keys.Any( k => k.Equals( value, StringComparison.OrdinalIgnoreCase ) ) ) )
            {
                var definedValueToAdd = new Rock.Client.DefinedValue { DefinedTypeId = definedTypeId, Value = importDefinedValue, Guid = Guid.NewGuid() };

                RestRequest restDefinedValuePostRequest = new RestRequest( "api/DefinedValues", Method.POST );
                restDefinedValuePostRequest.RequestFormat = RestSharp.DataFormat.Json;
                restDefinedValuePostRequest.AddBody( definedValueToAdd );

                var postDefinedValueResponse = restClient.Post( restDefinedValuePostRequest );
            }
        }

        /// <summary>
        /// Loads the slingshot person list.
        /// </summary>
        /// <returns></returns>
        private List<Slingshot.Core.Model.Person> LoadSlingshotPersonList()
        {
            var slingshotFileName = tbSlingshotFileName.Text;
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

            using ( var personFileStream = File.OpenText( Path.Combine( slingshotDirectoryName, new Slingshot.Core.Model.Person().GetFileName() ) ) )
            {
                CsvReader personCsv = new CsvReader( personFileStream );
                personCsv.Configuration.HasHeaderRecord = true;
                personCsv.Configuration.WillThrowOnMissingField = false;
                slingshotPersonList = personCsv.GetRecords<Slingshot.Core.Model.Person>().ToList();
            }

            using ( var personAddressFileStream = File.OpenText( Path.Combine( slingshotDirectoryName, new Slingshot.Core.Model.PersonAddress().GetFileName() ) ) )
            {
                CsvReader personAddressCsv = new CsvReader( personAddressFileStream );
                personAddressCsv.Configuration.HasHeaderRecord = true;
                slingshotPersonAddressListLookup = personAddressCsv.GetRecords<Slingshot.Core.Model.PersonAddress>().GroupBy( a => a.PersonId ).ToDictionary( k => k.Key, v => v.ToList() );
            }

            using ( var personAttributeValueFileStream = File.OpenText( Path.Combine( slingshotDirectoryName, new Slingshot.Core.Model.PersonAttributeValue().GetFileName() ) ) )
            {
                CsvReader personAttributeValueCsv = new CsvReader( personAttributeValueFileStream );
                personAttributeValueCsv.Configuration.HasHeaderRecord = true;
                slingshotPersonAttributeValueListLookup = personAttributeValueCsv.GetRecords<Slingshot.Core.Model.PersonAttributeValue>().GroupBy( a => a.PersonId ).ToDictionary( k => k.Key, v => v.ToList() );
            }

            using ( var personPhoneFileStream = File.OpenText( Path.Combine( slingshotDirectoryName, new Slingshot.Core.Model.PersonPhone().GetFileName() ) ) )
            {
                CsvReader personPhoneCsv = new CsvReader( personPhoneFileStream );
                personPhoneCsv.Configuration.HasHeaderRecord = true;
                slingshotPersonPhoneListLookup = personPhoneCsv.GetRecords<Slingshot.Core.Model.PersonPhone>().GroupBy( a => a.PersonId ).ToDictionary( k => k.Key, v => v.ToList() );
            }

            foreach ( var slingshotPerson in slingshotPersonList )
            {
                slingshotPerson.Addresses = slingshotPersonAddressListLookup.ContainsKey( slingshotPerson.Id ) ? slingshotPersonAddressListLookup[slingshotPerson.Id] : new List<Slingshot.Core.Model.PersonAddress>();
                slingshotPerson.Attributes = slingshotPersonAttributeValueListLookup.ContainsKey( slingshotPerson.Id ) ? slingshotPersonAttributeValueListLookup[slingshotPerson.Id].ToList() : new List<Slingshot.Core.Model.PersonAttributeValue>();
                slingshotPerson.PhoneNumbers = slingshotPersonPhoneListLookup.ContainsKey( slingshotPerson.Id ) ? slingshotPersonPhoneListLookup[slingshotPerson.Id].ToList() : new List<Slingshot.Core.Model.PersonPhone>();
            }

            return slingshotPersonList;
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
            this.PhoneNumberTypeValues = LoadDefinedValues( restClient, Rock.Client.SystemGuid.DefinedType.PERSON_PHONE_TYPE.AsGuid() );
            this.GroupLocationTypeValues = LoadDefinedValues( restClient, Rock.Client.SystemGuid.DefinedType.GROUP_LOCATION_TYPE.AsGuid() );

            RestRequest requestFamilyGroupType = new RestRequest( Method.GET );
            requestFamilyGroupType.RequestFormat = RestSharp.DataFormat.Json;
            requestFamilyGroupType.Resource = $"api/GroupTypes?$filter=Guid eq guid'{Rock.Client.SystemGuid.GroupType.GROUPTYPE_FAMILY}'&$expand=Roles";

            var familyGroupTypeResponse = restClient.Execute( requestFamilyGroupType );

            this.FamilyRoles = JsonConvert.DeserializeObject<List<Rock.Client.GroupType>>( familyGroupTypeResponse.Content ).FirstOrDefault().Roles.ToDictionary( k => k.Guid, v => v );

            RestRequest requestCampuses = new RestRequest( Method.GET );
            requestCampuses.RequestFormat = RestSharp.DataFormat.Json;
            requestCampuses.Resource = $"api/Campuses";
            var campusResponse = restClient.Execute( requestCampuses );

            this.Campuses = JsonConvert.DeserializeObject<List<Rock.Client.Campus>>( campusResponse.Content );
        }

        /// <summary>
        /// Gets the rock rest client.
        /// </summary>
        /// <returns></returns>
        private RestClient GetRockRestClient()
        {
            // TODO: Prompt for URL and Login Params
            RestClient restClient = new RestClient( "http://localhost:6229" );

            restClient.CookieContainer = new System.Net.CookieContainer();

            RestRequest restLoginRequest = new RestRequest( Method.POST );
            restLoginRequest.RequestFormat = RestSharp.DataFormat.Json;
            restLoginRequest.Resource = "api/auth/login";
            var loginParameters = new
            {
                UserName = "admin",
                Password = "admin"
            };

            restLoginRequest.AddBody( loginParameters );
            var loginResponse = restClient.Post( restLoginRequest );
            return restClient;
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

        /// <summary>
        /// Handles the Click event of the btnSelectSlingshotFile control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void btnSelectSlingshotFile_Click( object sender, RoutedEventArgs e )
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.DefaultExt = ".dplx";
            dlg.Filter = "Slingshot Files (.slingshot)|*.slingshot";

            if ( dlg.ShowDialog() == true )
            {
                tbSlingshotFileName.Text = dlg.FileName;
            }
        }
    }
}
