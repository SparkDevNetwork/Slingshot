using OrcaMDF.Core.Engine;
using OrcaMDF.Core.MetaData.DMVs;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;

namespace Slingshot.F1.Utilities.SQL
{

    /*
     * 8/12/20 - Shaun
     * 
     * This file contains the objects used to read the physical F1 SQL Database (MDF) file
     * through OrcaMDF. The F1SqlDatabase, F1SqlTable, and F1SqlColumn classes are used to
     * encapsulate the data.
     * 
     * The F1SqlSchemaDefiniton class contains definitions for the expected schema,
     * allowing the other classes to avoid loading problematic data (like unsupported data
     * types or tables and columns that we are not interested in) and notify us if there
     * is a problem (missing table or column) in the database, before we run into an error
     * in the middle of an export.
     * 
     * */

    /// <summary>
    /// List utility methods.
    /// </summary>
    public static class ListUtilities
    {
        /// <summary>
        /// Compares two lists and gets the missing values from the second one.
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
        #region Public Properties/Accessors

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
        /// Gets a specific table by name.
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public F1SqlTable Table( string tableName )
        {
            foreach ( var table in Tables )
            {
                if ( table.Name.ToLower() == tableName.ToLower() )
                {
                    return table;
                }
            }

            return null;
        }

        #endregion Public Properties/Accessors

        #region Constructor

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
                    Tables[i] = new F1SqlTable( db, table, allowedTypes, fileName, loadData );
                }

                // Unload the MDF file.
                db.Dispose();
            }
        }

        #endregion Constructor
    }

    /// <summary>
    /// Represents a table from the F1 SQL Database.
    /// </summary>
    [DebuggerDisplay("Table: {Name}")]
    public class F1SqlTable
    {
        #region Private Fields and Properties

        /// <summary>
        /// The file name is stored in case we need to open the database to read data.
        /// </summary>
        private string _fileName { get; set; }

        /// <summary>
        /// The data.
        /// </summary>
        private DataTable _data;

        /// <summary>
        /// The allowed data types.
        /// </summary>
        private List<byte> _allowedTypes;

        /// <summary>
        /// Indicates that the data for this table has already been loaded.
        /// </summary>
        private bool DataLoaded { get; set; } = false;

        #endregion Private Fields and Properties

        #region Public Properties

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
        public DataTable Data
        {
            get
            {
                if ( !DataLoaded )
                {
                    using ( var db = new Database( _fileName ) )
                    {
                        LoadTableData( db );
                    }
                }
                return _data;
            }
            private set
            {
                _data = value;
            }
        }

        #endregion Public Properties

        #region Private Methods

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

        /// <summary>
        /// Loads the columns which are specified for this table and which have allowed data types.
        /// </summary>
        /// <param name="db">The database.</param>
        /// <param name="table">The table.</param>
        /// <param name="allowedTypes">The allowed types.</param>
        /// <returns></returns>
        /// <exception cref="Exception">MDF File is missing required columns or the column's data type is invalid in table {table.Name}: {missingColumns}.</exception>
        private IOrderedEnumerable<Column> GetAllowedColumns( Database db, Table table )
        {
            var allowedColumns = F1SqlSchemaDefiniton.Columns[table.Name].Select( n => n.ToLower() ).ToList();
            var columns = db.Dmvs.Columns
                .Where( c => c.ObjectID == table.ObjectID )
                .Where( c => allowedColumns.Contains( c.Name.ToLower() ) )
                .Where( c => _allowedTypes.Contains( c.SystemTypeID ) )
                .OrderBy( c => c.Name );

            int columnCount = columns.Count();
            if ( columnCount != allowedColumns.Count )
            {
                // Halt and explain that the MDF file is invalid.
                var presentColumns = columns.Select( c => c.Name.ToLower() ).ToList();
                var missingColumns = ListUtilities.GetMissingValues( presentColumns, allowedColumns );
                throw new Exception( $"MDF File is missing required columns or the column's data type is invalid in table {table.Name}: {missingColumns}." );
            }

            return columns;
        }

        /// <summary>
        /// Loads the table data.
        /// </summary>
        /// <param name="db">The database.</param>
        /// <param name="allowedColumns">The allowed columns.</param>
        private void LoadTableData( Database db )
        {
            if ( DataLoaded )
            {
                // No need to reload the data.
                return;
            }
            DataLoaded = true;

            var allowedColumns = F1SqlSchemaDefiniton.Columns[Name].Select( n => n.ToLower() ).ToList();
            var scanner = new DataScanner( db );
            var rows = scanner.ScanTable( Name );

            _data = CreateDataTable();

            foreach ( var row in rows )
            {
                bool addRow = false;
                var dr = _data.NewRow();
                foreach ( var col in row.Columns )
                {
                    if ( allowedColumns.Contains( col.Name.ToLower() ) )
                    {
                        object value = row[col];
                        if ( value == null )
                        {
                            value = DBNull.Value;
                        }
                        else
                        {
                            addRow = true;
                        }
                        dr[col.Name] = value;
                    }
                }
                if ( addRow )
                {
                    /*
                     * 8/10/2020 - Shaun
                     * This is an awkward workaround that seems to be necessary because OrcaMDF
                     * is reading some empty rows into the data for some unknown reason, which
                     * then causes problems when you read the data and expect values to be
                     * present (i.e., null values in non-nullable columns).  These empty rows
                     * do NOT show up if you attach the database to SQL server.
                     * 
                     * */

                    _data.Rows.Add( dr );
                }
            }
        }

        #endregion Private Methods

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="F1SqlTable"/> class.
        /// </summary>
        /// <param name="db">The database.</param>
        /// <param name="table">The table.</param>
        /// <param name="allowedTypes">The allowed types.</param>
        /// <param name="loadData">if set to <c>true</c> [load data].</param>
        /// <exception cref="Exception">MDF File is missing required columns or the column's data type is invalid in table {table.Name}: {missingColumns}.</exception>
        public F1SqlTable( Database db, Table table, List<byte> allowedTypes, string fileName, bool loadData = false )
        {
            Name = table.Name;
            _allowedTypes = allowedTypes;
            _fileName = fileName;

            // Only load the columns which are specified for this table and which have allowed data types.
            var columns = GetAllowedColumns( db, table );

            // Load the columns.
            Columns = new F1SqlColumn[columns.Count()];
            for ( int i = 0; i < columns.Count(); i++ )
            {
                var column = columns.ElementAt( i );
                Columns[i] = new F1SqlColumn( db, column );
            }

            // Load the rows (data).
            if ( loadData )
            {
                LoadTableData( db );
            }
        }

        #endregion Constructor
    }

    /// <summary>
    /// Represents a column from the F1 SQL Database.
    /// </summary>
    [DebuggerDisplay("Column: {Name}")]
    public class F1SqlColumn
    {
        #region Public Properties

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

        #endregion Public Properties

        #region Constructor

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

        #endregion Constructor
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
            // Activity_Group table columns.
            { "Activity_Group", new List<string> {
                "Activity_ID",
                "Activity_Group_ID",
                "Activity_Super_Group_ID",
                "Activity_Group_Name",
                "Activity_Super_Group",
                "CheckinBalanceType" } },

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

            // Batch table columns.
            { "Batch", new List<string> {
                "BatchID",
                "BatchName",
                "BatchDate",
                "BatchAmount" } },

            // Communication table columns.
            { "Communication", new List<string> {
                "Communication_ID",
                "Individual_ID",
                "Household_ID",
                "Communication_Type",
                "Communication_Value",
                "Listed",
                "LastUpdatedDate" } },

            // Company table columns.
            { "Company", new List<string> {
                "Household_ID",
                "Household_Name",
                "Last_Activity_Date",
                "CompanyType",
                "Contact_Name",
                "Created_Date" } },

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

            // Household_Address table columns.
            { "Household_Address", new List<string> {
                "Individual_ID",
                "Household_ID",
                "Address_Type",
                "Address_1",
                "Address_2",
                "City",
                "State",
                "Postal_Code",
                "country" } },

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
                "Individual_ID",
                "Activity_ID",
                "RLC_ID" } },

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
