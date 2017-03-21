using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO.Compression;
using Path = System.IO.Path;
using Microsoft.Win32;
using CsvHelper;
using Slingshot.Core.Model;
using Rock;
using RestSharp;

namespace Slingshot
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// <seealso cref="System.Windows.Window" />
    /// <seealso cref="System.Windows.Markup.IComponentConnector" />
    public partial class MainWindow : Window
    {
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
            var slingshotFileName = tbSlingshotFileName.Text;
            var slingshotDirectoryName = Path.Combine( Path.GetDirectoryName( slingshotFileName ), "slingshots", Path.GetFileNameWithoutExtension( slingshotFileName ) );

            var slingshotFilesDirectory = new DirectoryInfo( slingshotDirectoryName );
            if ( slingshotFilesDirectory.Exists )
            {
                slingshotFilesDirectory.Delete( true );
            }

            slingshotFilesDirectory.Create();
            ZipFile.ExtractToDirectory( slingshotFileName, slingshotFilesDirectory.FullName );

            /*            // TODO figure out strategy for getting defined values
                        int adultRoleId = 3;
                        int childRoleId = 4;
                        int personRecordTypeValueId = 1;
                        //int businessRecordTypeValueId = 2;

                        int recordStatusValueActiveId = 3;
                        int recordStatusValueInActiveId = 4;
                        int recordStatusValuePendingId = 5;

                        int maritalStatusMarriedId = 143;
                        int maritalStatusSingleId = 144;
                        int maritalStatusDivorcedId = 672;
                        int maritalStatusUnknownId = 673;
                        */
            List<Person> slingshotPersonList;
            Dictionary<int, List<PersonAddress>> slingshotPersonAddressListLookup;
            Dictionary<int, List<PersonAttributeValue>> slingshotPersonAttributeValueListLookup;
            Dictionary<int, List<PersonPhone>> slingshotPersonPhoneListLookup;

            using ( var personFileStream = File.OpenText( Path.Combine( slingshotDirectoryName, "person.csv" ) ) )
            {
                CsvReader personCsv = new CsvReader( personFileStream );
                personCsv.Configuration.HasHeaderRecord = true;
                slingshotPersonList = personCsv.GetRecords<Person>().ToList();
            }

            using ( var personAddressFileStream = File.OpenText( Path.Combine( slingshotDirectoryName, "person-address.csv" ) ) )
            {
                CsvReader personAddressCsv = new CsvReader( personAddressFileStream );
                personAddressCsv.Configuration.HasHeaderRecord = true;
                slingshotPersonAddressListLookup = personAddressCsv.GetRecords<PersonAddress>().GroupBy( a => a.PersonId ).ToDictionary( k => k.Key, v => v.ToList() );
            }

            using ( var personAttributeValueFileStream = File.OpenText( Path.Combine( slingshotDirectoryName, "person-attributevalue.csv" ) ) )
            {
                CsvReader personAttributeValueCsv = new CsvReader( personAttributeValueFileStream );
                personAttributeValueCsv.Configuration.HasHeaderRecord = true;
                slingshotPersonAttributeValueListLookup = personAttributeValueCsv.GetRecords<PersonAttributeValue>().GroupBy( a => a.PersonId ).ToDictionary( k => k.Key, v => v.ToList() );
            }

            using ( var personPhoneFileStream = File.OpenText( Path.Combine( slingshotDirectoryName, "person-phone.csv" ) ) )
            {
                CsvReader personPhoneCsv = new CsvReader( personPhoneFileStream );
                personPhoneCsv.Configuration.HasHeaderRecord = true;
                slingshotPersonPhoneListLookup = personPhoneCsv.GetRecords<PersonPhone>().GroupBy( a => a.PersonId ).ToDictionary( k => k.Key, v => v.ToList() );
            }

            foreach ( var slingshotPerson in slingshotPersonList )
            {
                slingshotPerson.Addresses = slingshotPersonAddressListLookup.ContainsKey( slingshotPerson.Id ) ? slingshotPersonAddressListLookup[slingshotPerson.Id] : new List<PersonAddress>();
                slingshotPerson.Attributes = slingshotPersonAttributeValueListLookup.ContainsKey( slingshotPerson.Id ) ? slingshotPersonAttributeValueListLookup[slingshotPerson.Id].ToList() : new List<PersonAttributeValue>();
                slingshotPerson.PhoneNumbers = slingshotPersonPhoneListLookup.ContainsKey( slingshotPerson.Id ) ? slingshotPersonPhoneListLookup[slingshotPerson.Id].ToList() : new List<PersonPhone>();
            }

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


            RestRequest requestDefinedValues = new RestRequest( Method.GET );
            requestDefinedValues.RequestFormat = RestSharp.DataFormat.Json;
            requestDefinedValues.Resource = $"DefinedTypes?$filter=Guid eq guid'{Rock.Client.SystemGuid.DefinedType.PERSON_RECORD_TYPE}'&$expand=DefinedValues&$select=DefinedValues/Id,DefinedValues/Guid,DefinedValues/Value";
            var personRecordTypeDefinedValues = restClient.Get<List<Rock.Client.DefinedValue>>( requestDefinedValues );



            /*RestRequest restPersonImportRequest = new RestRequest( "api/Slingshot/PostPersonImport", Method.POST );
            restPersonImportRequest.RequestFormat = RestSharp.DataFormat.Json;
            restPersonImportRequest.AddBody( slingshotPersonList );
            */


            List<Rock.BulkUpdate.PersonImport> personImportList = new List<Rock.BulkUpdate.PersonImport>();
            foreach (var slingshotPerson in slingshotPersonList)
            {
                var personImport = new Rock.BulkUpdate.PersonImport();
                personImport.PersonForeignId = slingshotPerson.Id;
                personImport.FamilyForeignId = slingshotPerson.FamilyId ?? 0;
                personImport.FamilyName = slingshotPerson.FamilyName;
                personImport.FamilyImageUrl = slingshotPerson.FamilyImageUrl;

                switch ( slingshotPerson.FamilyRole )
                {
                    case FamilyRole.Adult:
                        //personImport.GroupRoleGuid = Rock.Client.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_ADULT.AsGuid();
                        break;
                    case FamilyRole.Child:
                        //personImport.GroupRoleGuid = Rock.Client.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_CHILD.AsGuid();
                        break;
                }

                personImport.FirstName = slingshotPerson.FirstName;
                personImport.NickName = slingshotPerson.NickName;
                personImport.MiddleName = slingshotPerson.MiddleName;
                personImport.LastName = slingshotPerson.LastName;

            }

            RestRequest restPersonImportRequest = new RestRequest( "api/Slingshot/PostPersonImport", Method.POST );
            restPersonImportRequest.RequestFormat = RestSharp.DataFormat.Json;
            restPersonImportRequest.AddBody( slingshotPersonList );


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
