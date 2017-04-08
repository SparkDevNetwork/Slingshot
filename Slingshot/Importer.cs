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
        public Importer( string slingshotFileName )
        {
            SlingshotFileName = slingshotFileName;
        }

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
        private List<Slingshot.Core.Model.PersonAttribute> SlingshotPersonAttributes { get; set; }
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

        /* */
        private string SlingshotFileName { get; set; }

        /// <summary>
        /// Gets or sets the results.
        /// </summary>
        /// <value>
        /// The results.
        /// </value>
        public string Results { get; set; }

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

            // Populate Rock with stuff that comes from the Slingshot file
            AddCampuses();
            AddConnectionStatuses();
            AddPersonTitles();
            AddPersonSuffixes();
            AddPhoneTypes();
            AddPersonAttributeCategories();
            AddPersonAttributes();

            AddGroupTypes();

            // load lookups again in case we added some new ones
            BackgroundWorker.ReportProgress( 0, "Reloading Rock Lookups..." );
            LoadLookups();

            //SubmitPersonImport();

            // Attendance Related
            //SubmitLocationImport();
            SubmitGroupImport();
        }

        /// <summary>
        /// Submits the location import.
        /// </summary>
        private void SubmitLocationImport()
        {
            BackgroundWorker.ReportProgress( 0, "Preparing LocationImport..." );
            var locationImportList = new List<Rock.BulkUpdate.LocationImport>();
            foreach ( var slingshotLocation in this.SlingshotLocationList )
            {
                var locationImport = new Rock.BulkUpdate.LocationImport();
                locationImport.LocationForeignId = slingshotLocation.Id;
                locationImport.ParentLocationForeignId = slingshotLocation.ParentLocationId;
                locationImport.Name = slingshotLocation.Name;
                locationImport.IsActive = slingshotLocation.IsActive;

                /* TODO: None of these line up with Core LocationTypes, just leave LocationTypeValueId null?
                switch ( slingshotLocation.LocationType )
                {
                    case Core.Model.LocationType.GeographicArea:
                        locationImport.LocationTypeValueId = this.LocationTypeValues[Rock.Client.SystemGuid.DefinedValue.LOCATION_TYPE_???.AsGuid()].Id;
                        break;
                    case Core.Model.LocationType.Home:
                        locationImport.LocationTypeValueId = this.LocationTypeValues[Rock.Client.SystemGuid.DefinedValue.LOCATION_TYPE_???.AsGuid()].Id;
                        break;
                    case Core.Model.LocationType.MeetingLocation:
                        locationImport.LocationTypeValueId = this.LocationTypeValues[Rock.Client.SystemGuid.DefinedValue.LOCATION_TYPE_??.AsGuid()].Id;
                        break;
                    case Core.Model.LocationType.Previous:
                        locationImport.LocationTypeValueId = this.LocationTypeValues[Rock.Client.SystemGuid.DefinedValue.LOCATION_TYPE_??.AsGuid()].Id;
                        break;
                    case Core.Model.LocationType.Work:
                        locationImport.LocationTypeValueId = this.LocationTypeValues[Rock.Client.SystemGuid.DefinedValue.LOCATION_TYPE_??.AsGuid()].Id;
                        break;
                }*/

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

            Results += importResponse.Content.FromJsonOrNull<string>() ?? importResponse.Content;

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
            var groupImportList = new List<Rock.BulkUpdate.GroupImport>();
            var campusLookup = this.Campuses.Where( a => a.ForeignId.HasValue ).ToDictionary( k => k.ForeignId.Value, v => v.Id );
            foreach ( var slingshotGroup in this.SlingshotGroupList )
            {
                var groupImport = new Rock.BulkUpdate.GroupImport();
                groupImport.GroupForeignId = slingshotGroup.Id;
                if ( slingshotGroup.GroupTypeId == 0 )
                {
                    groupImport.GroupTypeId = null;
                }
                else
                {
                    groupImport.GroupTypeId = this.GroupTypeLookupByForeignId[slingshotGroup.GroupTypeId].Id;
                }

                groupImport.Name = slingshotGroup.Name;
                groupImport.Order = slingshotGroup.Order;
                if ( slingshotGroup.CampusId.HasValue )
                {
                    groupImport.CampusId = campusLookup[slingshotGroup.CampusId.Value];
                }

                groupImport.ParentGroupForeignId = slingshotGroup.ParentGroupId == 0 ? ( int? ) null : slingshotGroup.ParentGroupId;
                groupImport.GroupMemberImports = new List<Rock.BulkUpdate.GroupMemberImport>();

                // TODO: It doesn't seem like Members are included yet...
                foreach ( var groupMember in slingshotGroup.GroupMembers )
                {
                    var groupMemberImport = new Rock.BulkUpdate.GroupMemberImport();
                    groupMemberImport.GroupMemberForeignId = groupMember.Id;
                    groupMemberImport.PersonForeignId = groupMember.PersonId;
                    groupMemberImport.RoleName = groupMember.Role;
                }

                groupImportList.Add( groupImport );
            }

            RestRequest restImportRequest = new RestRequest( "api/BulkImport/GroupImport", Method.POST ) { RequestFormat = DataFormat.Json };

            restImportRequest.AddBody( groupImportList );

            BackgroundWorker.ReportProgress( 0, "Sending Group Import to Rock..." );

            var importResponse = this.RockRestClient.Post( restImportRequest );

            Results += importResponse.Content.FromJsonOrNull<string>() ?? importResponse.Content;

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
        /// Submits the person import.
        /// </summary>
        /// <param name="bwWorker">The bw worker.</param>
        private void SubmitPersonImport()
        {
            BackgroundWorker.ReportProgress( 0, "Preparing PersonImport..." );
            List<Rock.BulkUpdate.PersonImport> personImportList = GetPersonImportList();

            RestRequest restPersonImportRequest = new RestRequest( "api/BulkImport/PersonImport", Method.POST );
            restPersonImportRequest.RequestFormat = RestSharp.DataFormat.Json;

            var personImportListJSON = personImportList.ToJson();
            int postSizeMB = personImportListJSON.Length / 1024 / 1024;

            restPersonImportRequest.AddBody( personImportList );
            const int fiveMinutesMS = ( 1000 * 60 ) * 5;
            restPersonImportRequest.Timeout = fiveMinutesMS;

            BackgroundWorker.ReportProgress( 0, "Sending Person Import to Rock..." );

            var importResponse = this.RockRestClient.Post( restPersonImportRequest );

            Results = importResponse.Content.FromJsonOrNull<string>() ?? importResponse.Content;

            if ( importResponse.StatusCode == System.Net.HttpStatusCode.Created )
            {
                BackgroundWorker.ReportProgress( 0, this.Results );
            }
            else if ( importResponse.StatusCode == System.Net.HttpStatusCode.NotFound )
            {
                // either the endpoint doesn't exist, or the payload was too big 
                BackgroundWorker.ReportProgress( 0, $"Error posting to api/BulkImport/PersonImport. Make sure that Rock has been updated to support PersonImport, and also verify that Rock > Home / System Settings / System Configuration is configured to accept uploads larger than {postSizeMB}MB" );
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
        private List<Rock.BulkUpdate.PersonImport> GetPersonImportList()
        {
            List<Rock.BulkUpdate.PersonImport> personImportList = new List<Rock.BulkUpdate.PersonImport>();
            foreach ( var slingshotPerson in this.SlingshotPersonList )
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
                personImport.PhoneNumbers = new List<Rock.BulkUpdate.PhoneNumberImport>();
                foreach ( var slingshotPersonPhone in slingshotPerson.PhoneNumbers )
                {
                    var phoneNumberImport = new Rock.BulkUpdate.PhoneNumberImport();
                    phoneNumberImport.NumberTypeValueId = this.PhoneNumberTypeValues[slingshotPersonPhone.PhoneType].Id;
                    phoneNumberImport.Number = slingshotPersonPhone.PhoneNumber;
                    phoneNumberImport.IsMessagingEnabled = slingshotPersonPhone.IsMessagingEnabled ?? false;
                    phoneNumberImport.IsUnlisted = slingshotPersonPhone.IsUnlisted ?? false;
                    personImport.PhoneNumbers.Add( phoneNumberImport );
                }

                // Addresses
                personImport.Addresses = new List<Rock.BulkUpdate.PersonAddressImport>();
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
                            var addressImport = new Rock.BulkUpdate.PersonAddressImport()
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
                personImport.AttributeValues = new List<Rock.BulkUpdate.AttributeValueImport>();
                foreach ( var slingshotPersonAttributeValue in slingshotPerson.Attributes )
                {
                    int attributeId = this.PersonAttributeKeyLookup[slingshotPersonAttributeValue.AttributeKey].Id;
                    var attributeValueImport = new Rock.BulkUpdate.AttributeValueImport( attributeId, slingshotPersonAttributeValue.AttributeValue );
                    personImport.AttributeValues.Add( attributeValueImport );
                }

                personImportList.Add( personImport );
            }

            return personImportList;
        }

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
        /// Adds the person attribute categories.
        /// </summary>
        private void AddPersonAttributeCategories()
        {
            int entityTypeIdAttribute = this.EntityTypeLookup[Rock.Client.SystemGuid.EntityType.ATTRIBUTE.AsGuid()].Id;
            foreach ( var slingshotAttributeCategoryName in this.SlingshotPersonAttributes.Where( a => !string.IsNullOrWhiteSpace( a.Category ) ).Select( a => a.Category ).Distinct().ToList() )
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
                // TODO, possible bug in slingshot export, this is a temp workaround
                slingshotPersonAttribute.Key = slingshotPersonAttribute.Key.Replace( "udf_ind_", "udf_" );

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
                this.SlingshotGroupList = csvReader.GetRecords<Slingshot.Core.Model.Group>().ToList();
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
                this.SlingshotScheduleList = csvReader.GetRecords<Slingshot.Core.Model.Schedule>().ToList();
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

            // EntityTypes
            RestRequest requestEntityTypes = new RestRequest( Method.GET );
            requestEntityTypes.Resource = "api/EntityTypes";
            var requestEntityTypesResponse = restClient.Execute( requestEntityTypes );
            var entityTypes = JsonConvert.DeserializeObject<List<Rock.Client.EntityType>>( requestEntityTypesResponse.Content );
            this.EntityTypeLookup = entityTypes.ToDictionary( k => k.Guid, v => v );

            int entityTypeIdPerson = this.EntityTypeLookup[Rock.Client.SystemGuid.EntityType.PERSON.AsGuid()].Id;
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
    }
}
