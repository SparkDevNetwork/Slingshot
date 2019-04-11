using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

using System.Data;
using System.Data.OleDb;

using OrcaMDF;
using OrcaMDF.Core.Engine;
using OrcaMDF.Framework;
using Slingshot.Core;
using Slingshot.Core.Model;
using Slingshot.Core.Utilities;
using Slingshot.F1.Utilities.Translators.MDB;

namespace Slingshot.F1.Utilities
{
    /// <summary>
    /// API F1 Status
    /// </summary>

    public class F1Mdb : F1Translator
    {

        private static OleDbConnection _dbConnection;

        /// <summary>
        /// Gets or sets the file name.
        /// </summary>
        /// <value>
        /// The file name.
        /// </value>
        public static string FileName { get; set; }

        /// <summary>
        /// Gets the connection string.
        /// </summary>
        /// <value>
        /// The connection string.
        /// </value>
        public static string ConnectionString
        {
            get
            {
                return $"Provider=Microsoft.Jet.OLEDB.4.0;Data Source={FileName}";
            }
        }

        /// <summary>
        /// Connects the specified host name.
        /// </summary>
        /// <param name="hostName">Name of the host.</param>
        /// <param name="apiUsername">The API username.</param>
        /// <param name="apiPassword">The API password.</param>
        /// <summary>
        /// Opens the specified MS Access database.
        /// </summary>
        /// <param name="fileName">Name of the file</param>
        public static void OpenConnection( string fileName )
        {
            FileName = fileName;

            _dbConnection = new OleDbConnection { ConnectionString = ConnectionString };

            F1Mdb.IsConnected = true;
        }

        #region SQL Queries

        public static string SQL_GROUP_TYPES
        {
            get
            {
                return $@"
SELECT DISTINCT
	Group_Type_ID as Id
	, Group_Type_Name as [Name]
FROM 
	Groups";
            }
        }

        public static string SQL_PEOPLE
        {
            get
            {
                return $@"
SELECT *
FROM Individual_Household";
            }
        }

        public static string SQL_NOTES
        {
            get
            {
                return $@"
                    SELECT *
                    FROM Notes";
            }
        }

        public static string SQL_COMPANIES
        {
            get
            {
                return $@"
                    SELECT *
                    FROM Company";
            }
        }

        public static string SQL_USERS
        {
            get
            {
                return $@"
                    SELECT *
                    FROM Users";
            }
        }

        public static string SQL_ADDRESSES
        {
            get
            {
                return $@"
Select *
FROM Household_Address";
            }
        }

        public static string SQL_COMPANY_ADDRESSES
        {
            get
            {
                return $@"
                    SELECT 
                        Household_Address.*
                    FROM 
                        Company 
                        INNER JOIN Household_Address ON Company.HOUSEHOLD_ID = Household_Address.household_id;";
            }
        }

        public static string SQL_PHONE_NUMBERS
        {
            get
            {
                return $@"
Select DISTINCT
individual_id
, communication_type
, communication_value
, listed
FROM Communication
Where individual_id is not null
AND ( communication_type = 'Mobile' OR communication_type like '%Phone%' )";
            }
        }

        public static string SQL_COMPANY_COMMUNICATIONS
        {
            get
            {
                return $@"
                    SELECT 
                        Communication.*
                    FROM 
                        Company 
                        INNER JOIN Communication ON Company.HOUSEHOLD_ID = Communication.household_id;";
            }
        }

        public static string SQL_COMMUNICATIONS
        {
            get
            {
                return $@"
Select DISTINCT
individual_id
, communication_type
, communication_value
, listed
FROM Communication
Where individual_id is not null
";
            }
        }

        public static string SQL_ATTRIBUTES
        {
            get
            {
                return $@"
SELECT DISTINCT
	Attribute_Group_Name
	, Attribute_Name
	, Attribute_Id
FROM [Attribute]";
            }
        }

        public static string SQL_REQUIREMENTS
        {
            get
            {
                return $@"
SELECT DISTINCT
      [requirement_name]
  FROM [Requirement]";
            }
        }

        public static string SQL_ATTRIBUTEVALUES
        {
            get
            {
                return $@"
SELECT
	a.*
FROM
[Attribute] a
INNER JOIN (
SELECT
	Individual_Id
	, Attribute_Id
	, Max(Individual_attribute_Id) As Id
FROM.[Attribute]
Group By Individual_Id, Attribute_Id, Attribute_Name
) b on a.Individual_attribute_Id = b.Id";
            }
        }

        public static string SQL_REQUIREMENTVALUES
        {
            get
            {
                return $@"
SELECT 
	r.individual_id
	, r.Individual_Requirement_ID
	, r.requirement_date
	, r.requirement_status_name
    , r.[requirement_name]
  FROM [Requirement] r
INNER JOIN (
SELECT
individual_id,
[requirement_name]
, Max(Individual_Requirement_ID) as Id
 FROM [Requirement]
Group By individual_id,
[requirement_name]
) b on r.Individual_Requirement_ID = b.Id

";
            }
        }

        public static string SQL_COMMUNCATION_TYPES_FOR_ATTRIBUTES
        {
            get
            {
                return $@"
SELECT DISTINCT
	communication_type
FROM Communication
Where not communication_type in('Mobile', 'Email')
and communication_type not like '%Phone%'";
            }
        }

        public static string SQL_COMMUNCATION_ATTRIBUTE_VALUES
        {
            get
            {
                return $@"
SELECT
	c.communication_type
	, c.individual_id
	, c.communication_value
, c.communication_Id
FROM Communication c
Inner Join
( SELECT 
communication_type
	,individual_id
, MAX(communication_Id) as Id
FROM Communication
Group By communication_type, individual_id
) b on c.Communication_Id = b.id
Where not c.communication_type in('Mobile', 'Email')
and c.communication_type not like '%Phone%'
and c.individual_id is not null";
            }
        }

        public static string SQL_GROUPS
        {
            get
            {
                return $@"
SELECT d.[Group_Name]
      ,d.[Group_ID]
	      , g.Group_Type_Id
      ,[Description]
      , IIF(d.is_open=0,0,1) as is_active
      ,[start_date]
      ,[is_public]
      ,[Location_Name]
      ,[ScheduleDay]
      ,[StartHour]
      ,[Address1]
      ,[Address2]
      ,[City]
      ,[StProvince]
      ,[PostalCode]
      ,[Country]
	  , null as parentGroupId
  FROM [GroupsDescription] d
INNER JOIN
(
	SELECT DISTINCT Group_Id, Group_Type_Id 
	FROM Groups 
) g on g.Group_ID = d.Group_ID";
            }
        }

        public static string SQL_GROUP_MEMBERS
        {
            get
            {
                return $@"
SELECT DISTINCT 
	Group_Id
	, Individual_ID
	, Group_Member_Type
  FROM [Groups]";
            }
        }

        public static string SQL_ACTIVITY_MEMBERS
        {
            get
            {
                return $@"
SELECT DISTINCT
	AA.Individual_ID
	,AA.RLC_ID AS Group_Id
	,'Member' AS Group_Member_Type
	, AA.BreakoutGroup
	, null as ParentGroupId 

FROM ActivityAssignment AA

WHERE
AA.RLC_ID IS NOT NULL

UNION ALL
SELECT Distinct
	 Individual_ID
	, null as Group_Id
	, 'Member' as Group_Member_Type
	,  BreakoutGroup
	, IIF(isnull(RLC_ID),Activity_ID,RLC_ID) as ParentGroupId
FROM [ActivityAssignment]
Where [BreakoutGroup] is not null
Order By Group_Id, Individual_ID";
            }
        }

        public static string SQL_ACTIVITIES
        {
            get
            {
                return $@"
SELECT DISTINCT
	G1.Ministry_Name as [Group_Name]
	, G1.Ministry_ID AS [Group_Id]
	, 99999904 as Group_Type_ID
	,null as description
	, IIF(Ministry_Active=0,0,1) as is_active
	, null as start_date
	, -1 as is_public
	, '' as Location_Name
	,'' as [ScheduleDay]
    ,null as [StartHour]
    ,'' as [Address1]
    ,'' as [Address2]
    ,'' as [City]
    ,'' as [StProvince]
    ,null as [PostalCode]
    ,'' as [Country]
	,0 AS [ParentGroupId]
FROM ActivityMinistry G1


UNION ALL

SELECT DISTINCT
	G2.Activity_Name as [Group_Name]
	,G2.Activity_ID AS [Group_Id]
	,99999904 AS [Group_Type_Id]
	,null as description
	, Activity_Active as is_active
	, null as start_date
	, -1 as is_public
	, '' as Location_Name
	,'' as [ScheduleDay]
    ,null as [StartHour]
    ,'' as [Address1]
    ,'' as [Address2]
    ,'' as [City]
    ,'' as [StProvince]
    ,'' as [PostalCode]
    ,'' as [Country]
	, G2.Ministry_ID AS [ParentGroupId]
FROM ActivityMinistry G2


UNION ALL

SELECT DISTINCT
	G3.Activity_Group_Name as [Group_Name]
	, G3.Activity_Group_ID AS [Id]
	, 99999904 AS [GroupTypeId]
	,null as description
	, 1 as is_active
	, null as start_date
	, -1 as is_public
	, '' as Location_Name
	,'' as [ScheduleDay]
    ,null as [StartHour]
    ,'' as [Address1]
    ,'' as [Address2]
    ,'' as [City]
    ,'' as [StProvince]
    ,null as [PostalCode]
    ,'' as [Country]
	,G3.Activity_ID AS [ParentGroupId]
FROM Activity_Group G3

UNION ALL

SELECT DISTINCT
	[RLC_Name] AS [Group_Name]
	, RLC.RLC_ID AS [Group_Id]
	,99999904 AS [GroupTypeId]
	,null as description
	, Is_Active
	, null as start_date
	, -1 as is_public
	, RoomName as Location_Name
	,'' as [ScheduleDay]
    ,null as [StartHour]
    ,'' as [Address1]
    ,'' as [Address2]
    ,'' as [City]
    ,'' as [StProvince]
    ,'' as [PostalCode]
    ,'' as [Country]
	, IIF(ISNull( Activity_Group_ID ), Activity_ID, Activity_Group_Id) as ParentGroupId
FROM RLC

UNION ALL

SELECT Distinct
	[BreakoutGroup] as [Name]
	, null as Group_Id
	, 99999904 as GroupTypeId
	,null as description
	, 1 as is_active
	, null as start_date
	, -1 as is_public
	, '' as Location_Name
	,'' as [ScheduleDay]
    ,null as [StartHour]
    ,'' as [Address1]
    ,'' as [Address2]
    ,'' as [City]
    ,'' as [StProvince]
    ,null as [PostalCode]
    ,'' as [Country]
	, IIF(isnull(RLC_ID),Activity_ID,RLC_ID) as ParentGroupId
FROM [ActivityAssignment]
Where [BreakoutGroup] is not null";
            }
        }

        public static string SQL_FUNDS
        {
            get
            {
                return $@"
SELECT Distinct
	fund_name
	, taxDeductible
	, null as sub_fund_name
FROM Contribution

UNION ALL

SELECT Distinct
	fund_name
	, taxDeductible
	, sub_fund_name
From Contribution
where sub_fund_name is not null";
            }
        }

        public static string SQL_PLEDGES
        {
            get
            {
                return $@"
SELECT Distinct
	individual_id
	, household_id
	, Pledge_id
	, fund_name
	, sub_fund_name
	, pledge_frequency_name
	, total_pledge
	, start_date
	, end_date
From Pledge";
            }
        }

        public static string SQL_BATCHES
        {
            get
            {
                return $@"
SELECT
BatchId
, BatchName
, BatchDate
, BatchAmount
FROM [Batch]

UNION ALL

SELECT 
	90000000 + CLng(format(Received_Date, 'yyyyMMdd')) as [BatchID]
	, 'Batch: ' + format(Received_Date, 'MMM dd, yyyy') as [BatchName]
	, Min(Received_Date) as [BatchDate]
	, SUM(Amount) as [BatchAmount]
  FROM [Contribution]
  Where BatchId is null
  group by CLng(format(Received_Date, 'yyyyMMdd')), format(Received_Date, 'MMM dd, yyyy')";
            }
        }

        public static string SQL_CONTRIBUTIONS
        {
            get
            {
                return $@"
SELECT [Individual_ID]
      ,[Household_ID]
      ,[Received_Date]
      ,[Check_Number]
      ,[Memo]
      ,[Contribution_Type_Name]
      ,[ContributionID]
      ,[BatchID]
      , Fund_Name
      , Sub_Fund_Name
      , Amount
FROM [Contribution]";
            }
        }

        public static string SQL_ATTENDANCE
        {
            get
            {
                return $@"
SELECT [Individual_ID]
      ,[RLC_ID] as [GroupId]
      ,[Check_In_Time] as [StartDateTime]
	  ,[Check_Out_Time] as [EndDateTime]
	  ,'Checked in as ' + [CheckedInAs] + '( ' + [Job_Title] + ' )' as [Note]
  FROM [Attendance]
  where Check_In_Time is not null

  UNION ALL

  SELECT  [IndividualID] as [Individual_ID]
	  ,[GroupID]
      ,AttendanceDate as StartDateTime
      ,null as EndDateTime
      ,[Comments] as [Note]
  FROM [Groups_Attendance]
where IsPresent <> 0
and AttendanceDate is not null";
            }
        }

        #endregion

        /// <summary>
        /// Exports the individuals.
        /// </summary>
        /// <param name="modifiedSince">The modified since.</param>
        /// <param name="peoplePerPage">The people per page.</param>
        public override void ExportIndividuals( DateTime modifiedSince, int peoplePerPage = 500 )
        {
            try
            {
                // write out the person attributes
                var personAttributes = WritePersonAttributes();

                var dtAddress = GetTableData( SQL_ADDRESSES );
                var dtCommunications = GetTableData( SQL_COMMUNICATIONS );
                var dtAttributeValues = GetTableData( SQL_ATTRIBUTEVALUES );
                var dtRequirementValues = GetTableData( SQL_REQUIREMENTVALUES );
                var dtCommunicationValues = GetTableData( SQL_COMMUNCATION_ATTRIBUTE_VALUES );
                var dtPhoneNumbers = GetTableData( SQL_PHONE_NUMBERS );
                

                // export people
                using ( var dtPeople = GetTableData( SQL_PEOPLE ) )
                {
                    var headOfHouseHolds = dtPeople.Select( "household_position = 'Head'" );
                    
                    foreach ( DataRow row in dtPeople.Rows )
                    {
                        var importPerson = F1Person.Translate( row, dtCommunications, headOfHouseHolds, dtRequirementValues, dtCommunicationValues );

                        if ( importPerson != null )
                        {
                            ImportPackage.WriteToPackage( importPerson );
                        }
                    }

                    // export people addresses
                    foreach ( DataRow row in dtAddress.Rows )
                    {
                        var importAddress = F1PersonAddress.Translate( row, dtPeople );

                        if ( importAddress != null )
                        {
                            ImportPackage.WriteToPackage( importAddress );
                        }
                    }

                    // export Attribute Values
                    foreach ( DataRow row in dtAttributeValues.Rows )
                    {
                        var importAttributes = F1PersonAttributeValue.Translate( row );

                        if ( importAttributes != null )
                        {
                            foreach ( PersonAttributeValue value in importAttributes )
                            {
                                ImportPackage.WriteToPackage( value );
                            }
                        }
                    }

                    // export Phone Numbers
                    foreach ( DataRow row in dtPhoneNumbers.Rows )
                    {
                        var importNumber = F1PersonPhone.Translate( row );
                        if ( importNumber != null )
                        {
                            ImportPackage.WriteToPackage( importNumber );
                        }
                    }

                }

                
            }
            catch( Exception ex )
            {
                ErrorMessage = ex.Message;
            }

        }

        /// <summary>
        /// Exports the companies.
        /// </summary>
        public override void ExportCompanies()
        {
            try
            {
                using ( var dtAddress = GetTableData( SQL_COMPANY_ADDRESSES ) )
                using ( var dtCommunications = GetTableData( SQL_COMPANY_COMMUNICATIONS ) )
                using ( var dtCompanies = GetTableData( SQL_COMPANIES ) )
                {
                    foreach ( DataRow row in dtCompanies.Rows )
                    {
                        var importCompanyAsPerson = F1Company.Translate( row, dtCommunications );

                        if ( importCompanyAsPerson != null )
                        {
                            ImportPackage.WriteToPackage( importCompanyAsPerson );
                        }
                    }

                    foreach ( DataRow row in dtAddress.Rows )
                    {
                        var importAddress = F1CompanyAddress.Translate( row );

                        if ( importAddress != null )
                        {
                            ImportPackage.WriteToPackage( importAddress );
                        }
                    }

                    // export Phone Numbers
                    foreach ( DataRow row in dtCommunications.Rows )
                    {
                        var importNumber = F1CompanyPhone.Translate( row );

                        if ( importNumber != null )
                        {
                            ImportPackage.WriteToPackage( importNumber );
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
        /// Export the people and household notes
        /// </summary>
        public override void ExportNotes()
        {
            try
            {
                using ( var dtUsers = GetTableData( SQL_USERS ) )
                using ( var dtNotes = GetTableData( SQL_NOTES ) )
                using ( var dtPeople = GetTableData( SQL_PEOPLE ) )
                {
                    var headOfHouseHoldMap = GetHeadOfHouseholdMap( dtPeople );
                    var users = dtUsers.AsEnumerable().ToArray();

                    foreach ( DataRow row in dtNotes.Rows )
                    {
                        var importNote = F1Note.Translate( row, headOfHouseHoldMap, users );

                        if ( importNote != null )
                        {
                            ImportPackage.WriteToPackage( importNote );
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
        /// Exports the accounts.
        /// </summary>
        public override void ExportFinancialAccounts()
        {
            try
            {
                using ( var dtFunds = GetTableData( SQL_FUNDS ) )
                {
                    foreach ( DataRow row in dtFunds.Rows )
                    {
                        var importAccount = F1FinancialAccount.Translate( row );

                        if ( importAccount != null )
                        {
                            ImportPackage.WriteToPackage( importAccount );
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
        /// Exports the pledges.
        /// </summary>
        public override void ExportFinancialPledges()
        {

            try
            {
                using ( var dtPledges = GetTableData( SQL_PLEDGES ) )
                {
                    //Get head of house holds because in F1 pledges can be tied to indiviuals or households
                    var headOfHouseHolds = GetHeadOfHouseholdMap( GetTableData( SQL_PEOPLE ) );

                    foreach ( DataRow row in dtPledges.Rows )
                    {
                        var importPledge = F1FinancialPledge.Translate( row, headOfHouseHolds );

                        if ( importPledge != null )
                        {
                            ImportPackage.WriteToPackage( importPledge );
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
        /// Exports the batches.
        /// </summary>
        /// <param name="modifiedSince">The modified since.</param>
        public override void ExportFinancialBatches( DateTime modifiedSince )
        {
            try
            {
                using ( var dtBatches = GetTableData( SQL_BATCHES ) )
                {
                    foreach ( DataRow row in dtBatches.Rows )
                    {
                        var importBatch = F1FinancialBatch.Translate( row );

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
        /// Exports the contributions.
        /// </summary>
        /// <param name="modifiedSince">The modified since.</param>
        public override void ExportContributions( DateTime modifiedSince, bool exportContribImages )
        {

            try
            {
                using ( var dtPeople = GetTableData( SQL_PEOPLE ) )
                using ( var dtContributions = GetTableData( SQL_CONTRIBUTIONS ) )
                {
                    var headOfHouseholdMap = GetHeadOfHouseholdMap( dtPeople );

                    var dtCompanies = GetTableData( SQL_COMPANIES );
                    var companyIds = new HashSet<int>( dtCompanies.AsEnumerable().Select( s => s.Field<int>( "HOUSEHOLD_ID" ) ) );

                    foreach ( DataRow row in dtContributions.Rows )
                    {
                        var importTransaction = F1FinancialTransaction.Translate( row, headOfHouseholdMap, companyIds );

                        if ( importTransaction != null )
                        {
                            ImportPackage.WriteToPackage( importTransaction );
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
        public override void ExportGroups( List<int> selectedGroupTypes )
        {
            // write out the group types
            WriteGroupTypes( selectedGroupTypes );

            foreach( var groupType in GetGroupTypes().Where( g => selectedGroupTypes.Contains(g.Id) ) )
            {
                int parentGroupId = 90000000 + groupType.Id ;

                ImportPackage.WriteToPackage( new Group()
                {
                    Id = parentGroupId,
                    Name = groupType.Name,
                    GroupTypeId = groupType.Id
                } );
            }

            // Export F1 Activites
            if( selectedGroupTypes.Contains( 99999904 ) )
            {
                var dtActivityMembers = GetTableData( SQL_ACTIVITY_MEMBERS );

                // Add Group Ids for Break Out Groups
                foreach( var member in dtActivityMembers.Select( "Group_Id is null" ) )
                {
                    MD5 md5Hasher = MD5.Create();
                    var hashed = md5Hasher.ComputeHash( Encoding.UTF8.GetBytes( member.Field<string>( "BreakoutGroup" ) + member.Field<string>( "ParentGroupId" ) ) );
                    var groupId = Math.Abs( BitConverter.ToInt32( hashed, 0 ) ); // used abs to ensure positive number
                    if ( groupId > 0 )
                    {
                        member.SetField<int>( "Group_Id", groupId );
                    }
                }

                using ( var dtActivites = GetTableData( SQL_ACTIVITIES ) )
                {
                   
                    foreach ( DataRow row in dtActivites.Rows )
                    {
                        var importGroup = F1Group.Translate( row, dtActivityMembers );

                        if ( importGroup != null )
                        {
                            ImportPackage.WriteToPackage( importGroup );
                        }
                    }
                }
            }

            using ( var dtGroups = GetTableData( SQL_GROUPS ) )
            {
                var group_Type_Ids = string.Join( ",", selectedGroupTypes.Select( n => n.ToString() ).ToArray() );

                var dtGroupMembers = GetTableData( SQL_GROUP_MEMBERS );

                foreach ( DataRow row in dtGroups.Select( "Group_Type_Id in(" + group_Type_Ids + ")" ) )
                {
                    var importGroup = F1Group.Translate( row, dtGroupMembers );

                    if ( importGroup != null )
                    {
                        ImportPackage.WriteToPackage( importGroup );
                    }
                }
            }



        }

        /// <summary>
        /// Exports the contributions.
        /// </summary>
        /// <param name="modifiedSince">The modified since.</param>
        public override void ExportAttendance( DateTime modifiedSince )
        {

            try
            {
                using ( var dtAttendance = GetTableData( SQL_ATTENDANCE ) )
                {
                    foreach ( DataRow row in dtAttendance.Rows )
                    {
                        var importAttendance = F1Attendance.Translate( row );

                        if ( importAttendance != null )
                        {
                            ImportPackage.WriteToPackage( importAttendance );
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

            // Add F1 Requirements
            using ( var dtRequirements = GetTableData( SQL_REQUIREMENTS ) )
            {
                
                foreach ( DataRow row in dtRequirements.Rows )
                {
                    string requirementName = row.Field<string>( "requirement_name" );
                    
                    // status attribute
                    var requirementStatus = new PersonAttribute()
                    {
                        Name = requirementName + " Status",
                        Key = ( requirementName + "Status" ).RemoveSpaces().RemoveSpecialCharacters(),
                        Category = "Requirements",
                        FieldType = "Rock.Field.Types.TextFieldType"
                    };

                    ImportPackage.WriteToPackage( requirementStatus );
                    attributes.Add( requirementStatus );

                    // date attribute
                    var requirementDate = new PersonAttribute()
                    {
                        Name = requirementName + " Date",
                        Key = ( requirementName + "Date" ).RemoveSpaces().RemoveSpecialCharacters(),
                        Category = "Requirements",
                        FieldType = "Rock.Field.Types.DateFieldType"
                    };

                    ImportPackage.WriteToPackage( requirementDate );
                    attributes.Add( requirementDate );
                }
            }

            // Add F1 Attributes
            using ( var dtAttributes = GetTableData( SQL_ATTRIBUTES ) )
            {

                foreach ( DataRow row in dtAttributes.Rows )
                {
                    string attributeGroup = row.Field<string>( "Attribute_Group_Name" );
                    string attributeName = row.Field<string>( "Attribute_Name" );
                    int attributeId = row.Field<int>( "Attribute_Id" );

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

            // Add F1 Communications that aren't email and phone numbers
            using ( var dtCommunications = GetTableData( SQL_COMMUNCATION_TYPES_FOR_ATTRIBUTES ) )
            {

                foreach ( DataRow row in dtCommunications.Rows )
                {
                    string attributeGroup = "Communications";
                    string attributeName = row.Field<string>( "communication_type" );

                    var personAttribute = new PersonAttribute()
                    {
                        Name = attributeName,
                        Key = "F1" + attributeName.RemoveSpaces().RemoveSpecialCharacters(),
                        Category = attributeGroup,
                        FieldType = "Rock.Field.Types.TextFieldType"
                    };

                    ImportPackage.WriteToPackage( personAttribute );

                    
                    attributes.Add( personAttribute );

                }
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

            groupTypes.Add( new GroupType
            {
                Id = 99999904,
                Name = "F1 Activities"
            } );


            try
            {
                using ( var dtGroupTypes = GetTableData( SQL_GROUP_TYPES ) )
                {
                    foreach ( DataRow row in dtGroupTypes.Rows )
                    {
                        groupTypes.Add( new GroupType
                        {
                            Id = row.Field<int>( "Id" ),
                            Name = row.Field<string>( "Name" )
                        } );
                    }
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

        public override List<FamilyMember> GetFamilyMembers()
        {
            return null;
        }

        /// <summary>
        /// Gets the table data.
        /// </summary>
        /// <param name="command">The SQL command to run.</param>
        /// <returns></returns>
        public static DataTable GetTableData( string command )
        {
            DataSet dataSet = new DataSet();
            DataTable dataTable = new DataTable();
            OleDbCommand dbCommand = new OleDbCommand( command, _dbConnection );
            OleDbDataAdapter adapter = new OleDbDataAdapter();

            adapter.SelectCommand = dbCommand;
            adapter.Fill( dataSet );

            dataTable = dataSet.Tables["Table"];

            return dataTable;
        }

        private static Dictionary<int, int> GetHeadOfHouseholdMap( DataTable dtPeople )
        {
            var headOfHouseholdMap = new Dictionary<int, int>();
            var headOfHouseHolds = dtPeople.Select( "household_position = 'Head'" );

            foreach ( var headOfHousehold in headOfHouseHolds )
            {
                var individualId = headOfHousehold.Field<int>( "individual_id" );
                var householdId = headOfHousehold.Field<int>( "household_id" );

                headOfHouseholdMap[householdId] = individualId;
            }

            return headOfHouseholdMap;
        }
    }
}
