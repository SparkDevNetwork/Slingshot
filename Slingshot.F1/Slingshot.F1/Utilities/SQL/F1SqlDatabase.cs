using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OrcaMDF.Framework;
using OrcaMDF.Core;
using OrcaMDF.Core.Engine;
using OrcaMDF.Core.MetaData;
using OrcaMDF.Core.MetaData.DMVs;

namespace Slingshot.F1.Utilities.SQL
{
    /// <summary>
    /// List utility methods.
    /// </summary>
    public static class ListUtilities
    {
        /// <summary>
        /// Gets the missing values.
        /// </summary>
        /// <param name="presentValues">The present values.</param>
        /// <param name="requiredValues">The required values.</param>
        /// <returns>A human readable list of missing values.</returns>
        public static string GetMissingValues( List<string> presentValues, List<string> requiredValues )
        {
            var missingTables = requiredValues.Where( n => !presentValues.Contains( n ) ).ToList();
            string nonPresentValues = string.Empty;
            foreach ( string table in missingTables )
            {
                nonPresentValues += ", " + table;
            }
            if ( nonPresentValues.StartsWith( ", " ) )
            {
                nonPresentValues = nonPresentValues.Substring( 2 );
            }
            return nonPresentValues;
        }
    }

    /// <summary>
    /// Represents the F1 SQL Database.
    /// </summary>
    [DebuggerDisplay("Database: {Name}")]
    public class F1SqlDatabase
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the tables.
        /// </summary>
        /// <value>
        /// The tables.
        /// </value>
        public F1SqlTable[] Tables { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="F1SqlDatabase"/> class.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="loadData">if set to <c>true</c> [load data].</param>
        /// <exception cref="Exception">MDF File is missing required tables: {missingTables}.</exception>
        public F1SqlDatabase( string fileName, bool loadData = false )
        {
            using ( var db = new Database( fileName ) )
            {
                Name = db.Name;

                // Only read tables specified in the allowed table list so that we don't load a bunch of 
                // meaningless data in memory.
                var allowedTables = F1SqlSchemaDefiniton.Tables.Select( n => n.ToLower() ).ToList();
                var tables = db.Dmvs.Tables
                    .Where( t => allowedTables.Contains( t.Name.ToLower() ) )
                    .OrderBy( t => t.Name );

                int tableCount = tables.Count();
                if ( tableCount != allowedTables.Count )
                {
                    // Halt and explain that the MDF file is invalid.
                    var presentTables = tables.Select( t => t.Name.ToLower() ).ToList();
                    var missingTables = ListUtilities.GetMissingValues( presentTables, allowedTables );
                    throw new Exception( $"MDF File is missing required tables: {missingTables}." );
                }

                // Get allowed data types (these are used when fetching column data).
                var allowedTypes = db.Dmvs.Types
                    .Where( t => F1SqlSchemaDefiniton.SupportedDataTypes.Contains( t.Name ) )
                    .Select( t=> t.SystemTypeID )
                    .ToList();

                // Load the tables.
                Tables = new F1SqlTable[tableCount];
                for ( int i = 0; i < tableCount; i++ )
                {
                    var table = tables.ElementAt( i );
                    Tables[i] = new F1SqlTable( db, table, allowedTypes, loadData );
                }

                // Unload the MDF file.
                db.Dispose();
            }

            //var cols = db.Dmvs.Columns
            //    .Where( c => tables.Select( t => t.ObjectID ).ToList().Contains( c.ObjectID ) ).ToList();
            //var dataTypes = cols.Select( c => c.SystemTypeID );


            //var types = db.Dmvs.Types
            //    .Where( t => dataTypes.Contains( t.SystemTypeID ) )
            //    .ToList();

            //string delTypes = string.Empty;
            //foreach (var t in types)
            //{
            //    var cols2 = db.Dmvs.Columns.Where( c => c.SystemTypeID == t.SystemTypeID );
            //    var colNames = cols2.Select( c => c.Name ).ToList();
            //    int colCount = cols2.Count();
            //    delTypes += t.Name + " = " + colCount.ToString() + Environment.NewLine;
            //}
            //var typeNames = types.Select( t => t.Name ).ToList();

        }
    }

    /// <summary>
    /// Represents a table from the F1 SQL Database.
    /// </summary>
    [DebuggerDisplay("Table: {Name}")]
    public class F1SqlTable
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the columns.
        /// </summary>
        /// <value>
        /// The columns.
        /// </value>
        public F1SqlColumn[] Columns { get; private set; }

        /// <summary>
        /// Gets the data.
        /// </summary>
        /// <value>
        /// The data.
        /// </value>
        public DataTable Data { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="F1SqlTable"/> class.
        /// </summary>
        /// <param name="db">The database.</param>
        /// <param name="table">The table.</param>
        /// <param name="allowedTypes">The allowed types.</param>
        /// <param name="loadData">if set to <c>true</c> [load data].</param>
        /// <exception cref="Exception">MDF File is missing required columns or the column's data type is invalid in table {table.Name}: {missingColumns}.</exception>
        public F1SqlTable( Database db, Table table, List<byte> allowedTypes, bool loadData = false )
        {
            Name = table.Name;

            // Only load the columns which are specified for this table and which have allowed data types.
            var allowedColumns = F1SqlSchemaDefiniton.Columns[table.Name].Select( n => n.ToLower() ).ToList();
            var columns = db.Dmvs.Columns
                .Where( c => c.ObjectID == table.ObjectID )
                .Where( c => allowedColumns.Contains( c.Name.ToLower() ) )
                .Where( c => allowedTypes.Contains( c.SystemTypeID ) )
                .OrderBy( c => c.Name );

            int columnCount = columns.Count();

            if ( columnCount != allowedColumns.Count )
            {
                // Halt and explain that the MDF file is invalid.
                var presentColumns = columns.Select( c => c.Name.ToLower() ).ToList();
                var missingColumns = ListUtilities.GetMissingValues( presentColumns, allowedColumns );
                throw new Exception( $"MDF File is missing required columns or the column's data type is invalid in table {table.Name}: {missingColumns}." );
            }

            // Load the columns.
            Columns = new F1SqlColumn[columnCount];
            for ( int i = 0; i < columnCount; i++ )
            {
                var column = columns.ElementAt( i );
                Columns[i] = new F1SqlColumn( db, column );
            }

            // Load the rows (data).
            if ( loadData )
            {
                LoadTableData( db, allowedColumns );
            }
        }

        /// <summary>
        /// Loads the table data.
        /// </summary>
        /// <param name="db">The database.</param>
        /// <param name="allowedColumns">The allowed columns.</param>
        private void LoadTableData( Database db, List<string> allowedColumns )
        {
            var scanner = new DataScanner( db );
            var rows = scanner.ScanTable( Name );

            var dt = CreateDataTable();

            foreach ( var row in rows )
            {
                var dr = dt.NewRow();
                foreach ( var col in row.Columns )
                {
                    if ( allowedColumns.Contains( col.Name.ToLower() ) )
                    {
                        object value = row[col];
                        if ( value == null )
                        {
                            value = DBNull.Value;
                        }
                        dr[col.Name] = value;
                    }

                }
                dt.Rows.Add( dr );
            }
        }

        /// <summary>
        /// Creates the data table.
        /// </summary>
        /// <returns>An empty <see cref="DataTable"/> with the specified column schema.</returns>
        private DataTable CreateDataTable()
        {
            DataTable dt = new DataTable();

            foreach ( var column in Columns )
            {
                System.Type dataType;
                switch ( column.DataType )
                {
                    case F1SqlDataType.BIT:
                        dataType = typeof( bool );
                        break;
                    case F1SqlDataType.DATETIME:
                        dataType = typeof( DateTime );
                        break;
                    case F1SqlDataType.INT:
                        dataType = typeof( int );
                        break;
                    case F1SqlDataType.MONEY:
                        dataType = typeof( decimal );
                        break;
                    case F1SqlDataType.NVARCHAR:
                        dataType = typeof( string );
                        break;
                    case F1SqlDataType.SMALLINT:
                        dataType = typeof( int );
                        break;
                    case F1SqlDataType.TEXT:
                        dataType = typeof( string );
                        break;
                    case F1SqlDataType.TINYINT:
                        dataType = typeof( int );
                        break;
                    case F1SqlDataType.VARCHAR:
                        dataType = typeof( string );
                        break;
                    default:
                        throw new Exception( $"Data type {column.DataType} cannot be parsed." );
                }

                dt.Columns.Add( column.Name, dataType );
            }

            return dt;
        }
    }

    /// <summary>
    /// Represents a column from the F1 SQL Database.
    /// </summary>
    [DebuggerDisplay("Column: {Name}")]
    public class F1SqlColumn
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the type of the data.
        /// </summary>
        /// <value>
        /// The type of the data.
        /// </value>
        public F1SqlDataType DataType { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="F1SqlColumn"/> class.
        /// </summary>
        /// <param name="db">The database.</param>
        /// <param name="column">The column.</param>
        /// <exception cref="Exception">Unsupported data type ({type.Name}) in column {column.Name}.</exception>
        public F1SqlColumn( Database db, Column column )
        {
            Name = column.Name;

            var type = db.Dmvs.Types.Where( t => t.SystemTypeID == column.SystemTypeID ).First();
            switch (type.Name)
            {
                case "bit":
                    DataType = F1SqlDataType.BIT;
                    break;
                case "datetime":
                    DataType = F1SqlDataType.DATETIME;
                    break;
                case "int":
                    DataType = F1SqlDataType.INT;
                    break;
                case "money":
                    DataType = F1SqlDataType.MONEY;
                    break;
                case "nvarchar":
                    DataType = F1SqlDataType.NVARCHAR;
                    break;
                case "smallint":
                    DataType = F1SqlDataType.SMALLINT;
                    break;
                case "text":
                    DataType = F1SqlDataType.TEXT;
                    break;
                case "tinyint":
                    DataType = F1SqlDataType.TINYINT;
                    break;
                case "varchar":
                    DataType = F1SqlDataType.VARCHAR;
                    break;
                default:
                    // Halt and explain that the MDF file is invalid.
                    throw new Exception( $"Unsupported data type ({type.Name}) in column {column.Name}." );
            }
        }
    }

    /// <summary>
    /// The data types.  See also <see cref="F1SqlAllowedDataTypes"/>.
    /// </summary>
    public enum F1SqlDataType
    {
        BIT,
        DATETIME,
        INT,
        MONEY,
        NVARCHAR,
        SMALLINT,
        TEXT,
        TINYINT,
        VARCHAR
    }

    #region Schema Definition (Table, Column, and Data Types)

    /// <summary>
    /// Schema definitions.
    /// </summary>
    public static class F1SqlSchemaDefiniton
    {
        /// <summary>
        /// The supported data types.
        /// </summary>
        public static readonly List<string> SupportedDataTypes = new List<string>
        {
            "bit",
            "datetime",
            "int",
            "money",
            "nvarchar",
            "smallint",
            "text",
            "tinyint",
            "varchar"
        };

        ///// <summary>
        ///// The tables.
        ///// </summary>
        public static List<string> Tables
        {
            get
            {
                return Columns.Select( c => c.Key ).ToList();
            }
        }

        /// <summary>
        /// The columns (for each table).
        /// </summary>
        public static readonly Dictionary<string, List<string>> Columns = new Dictionary<string, List<string>>( StringComparer.OrdinalIgnoreCase )
        {
            // Account table columns.
            { "Account", new List<string> {
                "Individual_ID",
                "Household_ID",
                "Account_Type_Name",
                "Account",
                "Routing_Number"
            } },

            // Activity_Group table columns.
            { "Activity_Group", new List<string> {
                "Activity_ID",
                "Activity_Group_ID",
                "Activity_Super_Group_ID",
                "Activity_Group_Name",
                "Activity_Super_Group",
                "CheckinBalanceType" } },

            // Activity_Schedule table columns.
            { "Activity_Schedule", new List<string> {
                "Activity_Time_Name",
                "Activity_Start_Time",
                "Activity_End_Time",
                "Activity_ID",
                "Activity_Schedule_ID" } },

            // ActivityAssignment table columns.
            { "ActivityAssignment", new List<string> {
                "ActivityAssignmentID",
                "Activity_Name",
                "Ministry_Name",
                "RLC_Name",
                "Activity_Time_Name",
                "Activity_Start_Time",
                "Activity_End_Time",
                "Individual_ID",
                "Activity_ID",
                "RLC_ID",
                "Ministry_ID",
                "AssignmentDateTime",
                "BreakoutGroupName",
                "ActivityScheduleID" } },

            // ActivityMinistry table columns.
            { "ActivityMinistry", new List<string> {
                "Ministry_ID",
                "Activity_ID",
                "Ministry_Name",
                "Activity_Name",
                "Ministry_Active",
                "Activity_Active",
                "Has_Checkin",
                "Has_MyCheckin",
                "ActivityDescription" } },

            // Attendance table columns.
            { "Attendance", new List<string> {
                "Individual_Instance_ID",
                "Activity_Instance_ID",
                "Individual_ID",
                "Activity_ID",
                "RLC_ID",
                "Start_Date_Time",
                "Tag_Comment",
                "Tag_Code",
                "CheckedInAs",
                "BreakoutGroup_Name",
                "Check_In_Time",
                "Check_Out_Time",
                "Pager_Code",
                "Job_Title",
                "Checkin_Machine_Name",
                "Activity_schedule_ID",
                "Activity_schedule_name" } },

            // Attribute table columns.
            { "Attribute", new List<string> {
                "Attribute_Group_Name",
                "Attribute_Name",
                "Attribute_ID",
                "Individual_Id",
                "Start_Date",
                "End_Date",
                "Comment",
                "Staff_Individual_ID",
                "Individual_attribute_ID",
                "Created_Date" } },

            // Authorizations table columns.
            { "Authorizations", new List<string> {
                "AuthorizationID",
                "HouseholdID",
                "ChurchID",
                "PersonName",
                "AuthorizationDate",
                "CreatedDate",
                "LastUpdatedDate" } },

            // Batch table columns.
            { "Batch", new List<string> {
                "BatchID",
                "BatchName",
                "BatchDate",
                "BatchAmount",
                "ActivityInstanceID" } },

            // Communication table columns.
            { "Communication", new List<string> {
                "Communication_ID",
                "Individual_ID",
                "Household_ID",
                "Communication_Type",
                "Communication_Value",
                "Listed",
                "Communication_Comment",
                "LastUpdatedDate" } },

            // Company table columns.
            { "Company", new List<string> {
                "Household_ID",
                "Household_Name",
                "Household_Sort_Name",
                "Last_Activity_Date",
                "Old_ID",
                "CompanyType",
                "Contact_Name",
                "Created_Date",
                "Last_Updated_Date" } },

            // ContactFormData table columns.
            { "ContactFormData", new List<string> {
                "ContactInstanceID",
                "ContactInstItemID",
                "ContactActivityDate",
                "HouseholdID",
                "ContactIndividualID",
                "ContactItemIndividualID",
                "ContactDatetime",
                "ContactNote",
                "ContactItemNote",
                "ContactFormName",
                "ContactItemName",
                "ContactItemTypeName",
                "ContactStatus",
                "ContactMinistryID",
                "RLCID",
                "ContactAssignedUserID",
                "ContactDispositionName",
                "IsContactItemConfidential",
                "InitialContactCreatedByUserID",
                "ContactItemAssignedMinistryID",
                "ContactItemAssignedUserID",
                "ContactFormLastUpdatedDate",
                "ContactItemLastUpdatedDate" } },

            // Contribution table columns.
            { "Contribution", new List<string> {
                "Individual_ID",
                "Household_ID",
                "Fund_Name",
                "Fund_is_active",
                "Fund_type",
                "Fund_GL_account",
                "Sub_Fund_Name",
                "Received_Date",
                "Amount",
                "Check_Number",
                "Pledge_Drive_Name",
                "Pledge_Drive_ID",
                "Memo",
                "Contribution_Type_Name",
                "Stated_Value",
                "True_Value",
                "Liquidation_cost",
                "ContributionID",
                "BatchID",
                "Contribution_SubType_name",
                "Activity_ID",
                "Activity_Instance_ID",
                "ActivityService" } },

            // GiftednessProgram table columns.
            { "GiftednessProgram", new List<string> {
                "GiftProgramID",
                "GiftCategoryID",
                "GiftAttributeID",
                "ProgramName",
                "ShortProgramName",
                "CategoryName",
                "AttributeName",
                "CategorySort",
                "AttributeSort",
                "ProgramActive",
                "CategoryActive",
                "AttributeActive",
                "CategoryPercentage",
                "TotalJobAttributes",
                "MinJobAttributes",
                "JobAttributeWeight",
                "TotalIndividualAttributes",
                "MinIndividualAttributes",
                "IndividualAttributeWeight" } },

            // GroupManager table columns.
            { "GroupManager", new List<string> {
                "GroupTypeID",
                "GroupID",
                "IndividualID",
                "CreatedDate",
                "AdminTypeName" } },

            // Groups table columns.
            { "Groups", new List<string> {
                "Group_Type_Name",
                "Group_Name",
                "Group_Type_ID",
                "Group_ID",
                "Individual_ID",
                "Group_Member_Type",
                "Created_Date",
                "CampusName" } },

            // GroupsAttendance table columns.
            { "GroupsAttendance", new List<string> {
                "GroupID",
                "StartDateTime",
                "EndDateTime",
                "PresentCount",
                "AbsentCount",
                "TotalCount",
                "Met",
                "Comments",
                "IsPosted",
                "IsScheduled",
                "Individual_Present",
                "IndividualID",
                "AttendanceDate",
                "CheckinDateTime",
                "CheckoutDateTime",
                "AttendanceCreatedDate",
                "GroupMemberType" } },

            // GroupsDescription table columns.
            { "GroupsDescription", new List<string> {
                "Group_Name",
                "Group_ID",
                "Group_Type_Name",
                "Description",
                "is_open",
                "start_date",
                "is_public",
                "gender_name",
                "marital_status_name",
                "start_age_range",
                "end_age_range",
                "Campus_name",
                "isSearchable",
                "Location_Name",
                "RecurrenceType",
                "ScheduleDay",
                "StartHour",
                "EndHour",
                "ScheduleDescription",
                "Address1",
                "Address2",
                "Address3",
                "City",
                "StProvince",
                "PostalCode",
                "Country",
                "HasChildcare" } },

            // Headcount table columns.
            { "Headcount", new List<string> {
                "Headcount_ID",
                "Activity_ID",
                "RLC_ID",
                "RLC_name",
                "Start_Date_Time",
                "Attendance",
                "Meeting_note",
                "Activity_schedule_ID",
                "Activity_schedule_name",
                "Activity_Instance_ID" } },

            // Household_Address table columns.
            { "Household_Address", new List<string> {
                "Household_address_ID",
                "Individual_ID",
                "Household_ID",
                "Address_Type",
                "Address_1",
                "Address_2",
                "City",
                "State",
                "Postal_Code",
                "country",
                "County",
                "Carrier_Route",
                "Delivery_Point",
                "Address_Comment",
                "Last_Updated_Date" } },

            // Individual_Household table columns.
            { "Individual_Household", new List<string> {
                "Individual_ID",
                "Household_ID",
                "Household_Name",
                "Household_Position",
                "Last_Name",
                "First_Name",
                "Middle_Name",
                "Goes_By",
                "Former_Name",
                "Prefix",
                "Suffix",
                "Gender",
                "Date_Of_Birth",
                "Marital_Status",
                "First_Record",
                "Occupation_Description",
                "Employer",
                "School_Name",
                "Former_Church",
                "Status_Name",
                "Status_Date",
                "Default_tag_comment",
                "OldFamNum",
                "OldIndividualID",
                "SubStatus_Name",
                "Bar_Code",
                "Member_Env_Code",
                "Status_Comment",
                "Occupation_Name",
                "FormerDenomination_Name",
                "Title",
                "WeblinkLogin",
                "UnsubscribeAllChurchEmail" } },

            // IndividualContactNotes table columns.
            { "IndividualContactNotes", new List<string> {
                "IndividualContactID",
                "ContactInstItemID",
                "IndividualID",
                "IndividualContactDatetime",
                "UserID",
                "IndividualContactNote",
                "ConfidentialNote",
                "ContactMethodName" } },

            // IndividualGiftedness table columns.
            { "IndividualGiftedness", new List<string> {
                "Individual_ID",
                "ConductedByIndividualID",
                "AssessmentDate",
                "GiftProgramID",
                "GiftCategoryID",
                "GiftAttributeID",
                "Rank" } },

            // Job table columns.
            { "Job", new List<string> {
                "Job_ID",
                "Ministry_ID",
                "Activity_ID",
                "Job_Title",
                "Job_Description",
                "DYD_Enabled",
                "Position_Contact",
                "Gender",
                "Marital_Status",
                "Reports_To_Individual",
                "Ministry_Purpose",
                "Requirements",
                "Training_Requirements",
                "Time_Commitment_Required",
                "Christian_Maturity_Required",
                "Comments",
                "Minimum_Age",
                "Created_Date",
                "Last_Updated_Date",
                "Contact_Individual_ID",
                "Reports_To_Individual_ID",
                "Maximum_Age",
                "Is_Active" } },

            // JobInformation table columns.
            { "JobInformation", new List<string> {
                "JobID",
                "CreatedDate",
                "LastUpdatedDate",
                "JobInformationName",
                "Sort",
                "JobInformationText" } },

            // Notes table columns.
            { "Notes", new List<string> {
                "Note_ID",
                "Household_ID",
                "Individual_ID",
                "Note_Type_Name",
                "Note_Text",
                "NoteTypeActive",
                "NoteArchived",
                "NoteCreated",
                "NoteLastUpdated",
                "NoteTextArchived",
                "NoteTextCreated",
                "NoteTextLastUpdated",
                "NoteCreatedByUserID",
                "NoteLastUpdatedByUserID" } },

            // Pledge table columns.
            { "Pledge", new List<string> {
                "Pledge_ID",
                "Individual_ID",
                "Household_ID",
                "Pledge_Drive_Name",
                "Fund_Name",
                "Sub_Fund_Name",
                "Per_Payment_Amount",
                "Pledge_Frequency_Name",
                "Total_Pledge",
                "Start_Date",
                "End_Date",
                "Pledge_drive_goal" } },

            // RelationshipManager table columns.
            { "RelationshipManager", new List<string> {
                "IND_Relationship_Member_ID",
                "Ind_Relationship_ID",
                "Individual_ID",
                "Relationship_Role_Name",
                "Is_Primary",
                "CreatedDate" } },

            // RelationshipNotes table columns.
            { "RelationshipNotes", new List<string> {
                "IND_Relationship_Member_ID",
                "start_date",
                "EndDate",
                "Ind_Relationship_Note_ID",
                "Note_text" } },

            // Requirement table columns.
            { "Requirement", new List<string> {
                "Individual_requirement_ID",
                "Individual_ID",
                "Requirement_Name",
                "Requirement_Date",
                "Requirement_Status_Name",
                "Requirement_Note",
                "Is_Confidential",
                "Is_Background_Check",
                "Is_Reference_Check",
                "UserName",
                "RequirementDocumentContentType",
                "RequirementDocumentFileExtension" } },

            // RLC table columns.
            { "RLC", new List<string> {
                "RLC_ID",
                "Activity_ID",
                "RLC_Name",
                "Activity_Group_ID",
                "Start_Age_Date",
                "End_Age_Date",
                "Is_Active",
                "Room_Code",
                "Room_Desc",
                "Room_Name",
                "Max_Capacity",
                "Building_Name" } },

            // Staffing_Assignment table columns.
            { "Staffing_Assignment", new List<string> {
                "Staffing_pref_ID",
                "Activity_Time_Name",
                "Activity_Start_Time",
                "Activity_End_Time",
                "Individual_ID",
                "Job_Title",
                "Staffing_Schedule_Name",
                "Is_Active",
                "Ministry_ID",
                "Activity_ID",
                "Activity_Group_ID",
                "RLC_ID",
                "Activity_Schedule_ID",
                "JobID" } },

            // Users table columns.
            { "Users", new List<string> {
                "UserID",
                "UserLogin",
                "FirstName",
                "MiddleName",
                "LastName",
                "GoesBy",
                "IsUserEnabled",
                "UserBio",
                "IsPastor",
                "IsStaff",
                "UserTitle",
                "UserPhone",
                "UserEmail",
                "LinkedIndividualID",
                "UserCreatedDate",
                "DepartmentName" } }
        };
    }

    #endregion Schema Definition (Table, Column, and Data Types)

}
