using System.Data;
using System.Data.OleDb;

namespace Slingshot.F1.Utilities
{
    public partial class F1Mdb : F1Translator
    {
        /// <summary>
        /// Gets the table data.
        /// </summary>
        /// <param name="command">The SQL command to run.  (This should probably be one of the static values from the <see cref="SqlQueries"/> static class.)</param>
        /// <param name="cacheTable">A boolean flag indicating whether the result of this query should be cached in memory.
        ///     WARNING:  Causes very high memory utilization when importing large data sets.  If in doubt, DO NOT CACHE.</param>
        /// <returns></returns>
        private static DataTable GetTableData( string command, bool cacheTable = false )
        {
            if ( _TableDataCache.TryGetValue( command, out var dataTable ) )
            {
                return dataTable;
            }

            DataSet dataSet = new DataSet();
            dataTable = new DataTable();
            OleDbCommand dbCommand = new OleDbCommand( command, _dbConnection );
            OleDbDataAdapter adapter = new OleDbDataAdapter();

            adapter.SelectCommand = dbCommand;
            adapter.Fill( dataSet );

            dataTable = dataSet.Tables["Table"];
            if ( cacheTable )
            {
                _TableDataCache[command] = dataTable;
            }

            return dataTable;
        }

        /// <summary>
        /// Static SQL Query Text.
        /// </summary>
        private static class SqlQueries
        {
            public static string GROUP_TYPES
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

            public static string PEOPLE
            {
                get
                {
                    return $@"
SELECT *
FROM Individual_Household";
                }
            }

            /// <summary>
            /// Access flavor SQL to get the "highest ranking" representative of each household.
            /// The first Head is returned. If no Head, then Spouse, Child, Other, and then Visitor.
            /// </summary>
            public static string HEAD_OF_HOUSEHOLD
            {
                get
                {
                    return @"
                    SELECT
                        HeadOfHousehold.household_id,
                        FIRST(HeadOfHousehold.SubStatus_Name) AS SubStatus_Name,
                        FIRST(HeadOfHousehold.individual_id) AS individual_id
                    FROM
                        (
                            SELECT
                                household_id,
                                MAX(SWITCH(
                                    household_position = 'Head', 10,
                                    household_position = 'Spouse', 8,
                                    household_position = 'Child', 6,
                                    household_position = 'Other', 4,
                                    household_position = 'Visitor', 2)) AS role_index
                            FROM Individual_Household
                            GROUP BY household_id
                        ) AS MaxRoleOfHousehold
                        INNER JOIN (
                            SELECT
                                household_id,
                                individual_id,
                                SubStatus_Name,
                                SWITCH(
                                    household_position = 'Head', 10,
                                    household_position = 'Spouse', 8,
                                    household_position = 'Child', 6,
                                    household_position = 'Other', 4,
                                    household_position = 'Visitor', 2) AS role_index
                            FROM Individual_Household
                        ) AS HeadOfHousehold ON
                            HeadOfHousehold.household_id = MaxRoleOfHousehold.household_id
                            AND HeadOfHousehold.role_index = MaxRoleOfHousehold.role_index
                    GROUP BY
                        HeadOfHousehold.household_id;";
                }
            }

            public static string NOTES
            {
                get
                {
                    return $@"
                    SELECT *
                    FROM Notes";
                }
            }

            public static string USERS
            {
                get
                {
                    return $@"
                    SELECT *
                    FROM Users";
                }
            }

            public static string ADDRESSES
            {
                get
                {
                    return $@"
Select *
FROM Household_Address";
                }
            }

            public static string COMPANY_ADDRESSES
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

            public static string PHONE_NUMBERS
            {
                get
                {
                    return $@"
Select DISTINCT
individual_id
, household_id
, communication_type
, communication_value
, CInt( listed ) AS listed
FROM Communication
Where ( individual_id IS NOT NULL OR household_id IS NOT NULL )
AND ( communication_type = 'Mobile' OR communication_type like '%Phone%' )
";
                }
            }

            public static string COMPANY_COMMUNICATIONS
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

            public static string COMMUNICATIONS
            {
                get
                {
                    //It's important to sort this table so that we get the most recently updated record first, in case there are multiples.
                    return $@"
SELECT  Individual_Id,
        household_id,
        communication_type,
        communication_value,
        listed,
        LastUpdateDate
FROM    Communication
ORDER BY LastUpdateDate DESC 
";
                }
            }

            public static string ATTRIBUTES
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

            public static string REQUIREMENTS
            {
                get
                {
                    return $@"
SELECT DISTINCT
      [requirement_name]
  FROM [Requirement]";
                }
            }

            public static string ATTRIBUTEVALUES
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

            public static string REQUIREMENTVALUES
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

            public static string COMMUNCATION_TYPES_FOR_ATTRIBUTES
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

            public static string COMMUNCATION_ATTRIBUTE_VALUES
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

            public static string GROUPS
            {
                get
                {
                    return $@"
SELECT g.[Group_Name]
      ,g.[Group_ID]
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
  FROM (
	SELECT DISTINCT Group_Id, Group_Type_Id, Group_Name 
	FROM Groups 
) g
LEFT JOIN GroupsDescription d on g.Group_ID = d.Group_ID";
                }
            }

            public static string GROUP_MEMBERS
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

            public static string ACTIVITY_MEMBERS
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

            public static string STAFFING
            {
                get
                {
                    return @"
                    SELECT 
                        Staffing_Assignment.INDIVIDUAL_ID, 
                        IIF( 
                            ISNULL(Staffing_Assignment.RLC_ID), 
                            Staffing_Assignment.Activity_ID, 
                            Staffing_Assignment.RLC_ID
                        ) AS Group_Id
                    FROM 
                        ActivityMinistry 
                        INNER JOIN Staffing_Assignment ON 
                            ActivityMinistry.Activity_ID = Staffing_Assignment.Activity_ID 
                            AND ActivityMinistry.Ministry_ID = Staffing_Assignment.Ministry_ID;";
                }
            }

            public static string ACTIVITIES
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

            public static string FUNDS
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

            public static string PLEDGES
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

            public static string BATCHES
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

            public static string CONTRIBUTIONS
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
      , Fund_Type
      ,taxDeductible
FROM [Contribution]";
                }
            }

            public static string ATTENDANCE
            {
                get
                {
                    return $@"
SELECT DISTINCT ([Start_Date_Time]) AS [StartDateTime]
 ,[Individual_ID]
 ,[Attendance_ID]
 ,[RLC_ID] AS [GroupId]
 ,[Check_Out_Time] AS [EndDateTime]
 ,'Checked in as ' + [CheckedInAs] + '( ' + [Job_Title] + ' )' AS [Note]
FROM [Attendance]
WHERE [Start_Date_Time] IS NOT NULL

UNION ALL

SELECT [AttendanceDate] AS [StartDateTime]
 ,[IndividualID] AS [Individual_ID]
 ,NULL AS [Attendance_ID]
 ,[GroupID]
 ,NULL AS EndDateTime
 ,[Comments] AS [Note]
FROM [Groups_Attendance]
WHERE IsPresent <> 0";
                }
            }

            public static string COMPANY
            {
                get
                {
                    return $@"
SELECT 
    HOUSEHOLD_ID, 
    HOUSEHOLD_NAME, 
    LAST_ACTIVITY_DATE, 
    CompanyType, 
    CONTACT_NAME, 
    CREATED_DATE
FROM Company;

";
                }
            }
        }

    }
}
