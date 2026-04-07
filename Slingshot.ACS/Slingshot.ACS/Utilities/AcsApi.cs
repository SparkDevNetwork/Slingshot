using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

using Slingshot.Core;
using Slingshot.Core.Model;
using Slingshot.Core.Utilities;

using Slingshot.ACS.Utilities.Translators;

namespace Slingshot.ACS.Utilities
{
    public static class AcsApi
    {
        private static OleDbConnection _dbConnection;
        private static string _emailType;
        private static DateTime _modifiedSince;

        /// <summary>
        /// Gets or sets the file name.
        /// </summary>
        /// <value>
        /// The file name.
        /// </value>
        public static string FileName { get; set; }

        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        /// <value>
        /// The error message.
        /// </value>
        public static string ErrorMessage { get; set; }

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
        /// Gets a value indicating whether this instance is connected.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is connected; otherwise, <c>false</c>.
        /// </value>
        public static bool IsConnected { get; private set; } = false;

        /// <summary>
        /// Gets or sets the person attributes
        /// </summary>
        public static Dictionary<string, string> PersonAttributes { get; set; }

        #region SQL Queries

        public static string SQL_GROUPS
        {
            get
            {
                return $@"
SELECT DISTINCT
	SG.[GroupName]
	,SG.[GroupSort]
FROM SGRoster SG";
            }
        }

        public static string SQL_GROUPMEMBERS
        {
            get
            {
                return $@"
SELECT
	P.[IndividualId]
	,SG.[RosterID]
	,SG.[GroupName]
	,SG.[Position]
FROM SGRoster SG
        INNER JOIN people P 
            ON P.[FamilyNumber] = SG.[FamilyNumber] 
                AND P.[IndividualNumber] = SG.[IndividualNumber]";
            }
        }

        public static string SQL_PEOPLE
        {
            get
            {
                return $@"
SELECT P.*, E.[EmailAddr]
FROM people P
LEFT JOIN emails E ON (P.[FamilyNumber] = E.[FamilyNumber]
	AND P.[IndividualNumber] = E.[IndividualNumber]
	AND E.[Description] = '{ _emailType }')
WHERE P.[DateLastChanged] >= #{ _modifiedSince.ToShortDateString() }#";
            }
        }

        public static string SQL_PHONES
        {
            get
            {
                return $@"
SELECT PH.*, 
       P.[IndividualId] 
FROM   phones PH 
       INNER JOIN people P 
               ON P.[FamilyNumber] = PH.[FamilyNumber] 
                  AND P.[IndividualNumber] = PH.[IndividualNumber]
WHERE P.[DateLastChanged] >= #{ _modifiedSince.ToShortDateString() }#";
            }
        }

        public static string SQL_PEOPLE_NOTES
        {
            get
            {
                return $@"
SELECT P.[IndividualId]
      ,IC.[ComtDate]
      ,IC.[ComtType]
      ,IC.[Comment]
  FROM [IComment] IC
          INNER JOIN people P 
            ON P.[FamilyNumber] = IC.[FamilyNumber] 
                AND P.[IndividualNumber] = IC.[IndividualNumber]
GROUP BY P.[IndividualId], IC.[ComtDate], IC.[ComtType], IC.[Comment]";
            }
        }

        public static string SQL_FAMILY_NOTES
        {
            get
            {
                return $@"
SELECT FC.[FamilyNumber]
    ,FC.[ComtDate]
    ,FC.[ComtType]
    ,FC.[Comment]
 FROM [FComment] FC
 GROUP BY FC.[FamilyNumber], FC.[ComtDate], FC.[ComtType], FC.[Comment]";
            }
        }

        private const string SQL_FINANCIAL_ACCOUNTS = @"
SELECT [FundDescription], 
       [FundNumber], 
       [FundCode] 
FROM   cbgifts 
GROUP  BY [FundDescription], 
          [FundNumber], 
          [FundCode] ";

        public static string SQL_FINANCIAL_TRANSACTIONS
        {
            get
            {
                return $@"
SELECT DISTINCT G.[TransactionID], 
                G.[CheckNumber], 
                G.[GiftDate], 
                G.[PaymentType], 
                P.[IndividualId] 
FROM   cbgifts G 
       INNER JOIN people P 
               ON P.[FamilyNumber] = G.[FamilyNumber] 
                  AND P.[IndividualNumber] = G.[IndividualNumber]
WHERE G.[GiftDate] >= #{ _modifiedSince.ToShortDateString() }#";
            }
        }

        public static string SQL_FINANCIAL_TRANSACTIONDETAILS
        {
            get
            {
                return $@"
SELECT [FundNumber], 
       [Amount], 
       [TransactionID], 
       [GiftDescription], 
       [GiftDate] 
FROM   cbgifts G 
       INNER JOIN people P 
               ON P.[FamilyNumber] = G.[FamilyNumber] 
                  AND P.[IndividualNumber] = G.[IndividualNumber]
WHERE G.[GiftDate] >= #{ _modifiedSince.ToShortDateString() }#";
            }
        }

        private static string SQL_FINANCIAL_PLEDGES
        {
            get
            {
                return $@"
SELECT PL.[PledgeID],
				PL.[StartDate],
				PL.[StopDate],
				PL.[TotalPled],
				PL.[Freq],
				PL.[FundNumber],
                PL.[EntryDate],
                P.[IndividualId] 
FROM   cbPledge PL 
       INNER JOIN people P 
               ON P.[FamilyNumber] = PL.[FamilyNumber] 
                  AND P.[IndividualNumber] = PL.[IndividualNumber]
WHERE PL.[EntryDate] >= #{ _modifiedSince.ToShortDateString() }#";
            }
        }

        public const string SQL_EMAIL_TYPES = @"
SELECT [Description]
FROM [Emails]
GROUP BY [Description]";

        #endregion region


        /// <summary>
        /// Initializes the export.
        /// </summary>
        public static void InitializeExport()
        {
            ImportPackage.InitializePackageFolder();
        }

        /// <summary>
        /// Opens the specified MS Access database.
        /// </summary>
        /// <param name="fileName">Name of the file</param>
        public static void OpenConnection( string fileName )
        {
            FileName = fileName;

            _dbConnection = new OleDbConnection { ConnectionString = ConnectionString };

            AcsApi.IsConnected = true;
        }

        /// <summary>
        /// Exports the individuals.
        /// </summary>
        public static void ExportIndividuals( DateTime modifiedSince, string emailType, string campusKey )
        {
            try
            {
                _modifiedSince = modifiedSince;
                _emailType = emailType;

                //load attributes
                LoadPersonAttributes();

                // write out the person attributes
                WritePersonAttributes();

                // export people
                using ( var dtPeople = GetTableData( SQL_PEOPLE ) )
                {
                    foreach ( DataRow row in dtPeople.Rows )
                    {
                        var importPerson = AcsPerson.Translate( row, campusKey );

                        if ( importPerson != null )
                        {
                            ImportPackage.WriteToPackage( importPerson );
                        }
                    }
                }

                // export person notes
                using ( var dtPeopleNotes = GetTableData( SQL_PEOPLE_NOTES ) )
                {
                    foreach ( DataRow row in dtPeopleNotes.Rows )
                    {
                        var importNote = AcsPersonNote.Translate( row );

                        if ( importNote != null )
                        {
                            ImportPackage.WriteToPackage( importNote );
                        }
                    }
                }

                // export family notes
                using ( var dtFamilyNotes = GetTableData( SQL_FAMILY_NOTES ) )
                {
                    foreach ( DataRow row in dtFamilyNotes.Rows )
                    {
                        var importNote = AcsFamilyNote.Translate( row );

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
        /// Exports the phone numbers
        /// </summary>
        public static void ExportPhoneNumbers( DateTime modifiedSince )
        {
            try
            {
                _modifiedSince = modifiedSince;

                using ( var dtPhones = GetTableData( SQL_PHONES ) )
                {
                    foreach ( DataRow row in dtPhones.Rows )
                    {
                        var importPhone = AcsPhone.Translate( row );

                        if ( importPhone != null )
                        {
                            ImportPackage.WriteToPackage( importPhone );
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
        /// Exports the funds.
        /// </summary>
        public static void ExportFunds()
        {
            try
            {
                using ( var dtFunds = GetTableData( SQL_FINANCIAL_ACCOUNTS ) )
                {
                    foreach ( DataRow row in dtFunds.Rows )
                    {
                        var importAccount = AcsFinancialAccount.Translate( row );

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
        /// Exports any contributions.  Currently, the ACS export file doesn't include
        ///  batches.
        /// </summary>
        public static void ExportContributions( DateTime modifiedSince )
        {
            try
            {
                // since the ACS export doesn't include batches and Rock expects transactions
                //  to belong to a batch, a default batch is created.
                WriteFinancialBatch();

                using ( var dtContributions = GetTableData( SQL_FINANCIAL_TRANSACTIONS ) )
                {
                    foreach ( DataRow row in dtContributions.Rows )
                    {
                        var importFinancialTransaction = AcsFinancialTransaction.Translate( row );

                        if ( importFinancialTransaction != null )
                        {
                            ImportPackage.WriteToPackage( importFinancialTransaction );
                        }
                    }
                }

                using ( var dtContributionDetails = GetTableData( SQL_FINANCIAL_TRANSACTIONDETAILS ) )
                {
                    foreach ( DataRow row in dtContributionDetails.Rows )
                    {
                        var importFinancialTransactionDetail = AcsFinancialTransactionDetail.Translate( row );

                        if ( importFinancialTransactionDetail != null )
                        {
                            ImportPackage.WriteToPackage( importFinancialTransactionDetail );
                        }
                    }
                }

                using ( var dtPledges = GetTableData( SQL_FINANCIAL_PLEDGES ) )
                {
                    foreach ( DataRow row in dtPledges.Rows )
                    {
                        var importFinancialPledge = AcsFinancialPledge.Translate( row );

                        if ( importFinancialPledge != null )
                        {
                            ImportPackage.WriteToPackage( importFinancialPledge );
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
        /// Exports any groups found.  Currently, this export doesn't support
        ///  group hierarchies and all groups will be imported to the
        ///  root of the group viewer.
        /// </summary>
        public static void ExportGroups()
        {
            try
            {
                WriteGroupTypes();

                using ( var dtGroups = GetTableData( SQL_GROUPS ) )
                {
                    foreach ( DataRow row in dtGroups.Rows )
                    {
                        var importGroup = AcsGroup.Translate( row );

                        if ( importGroup != null )
                        {
                            ImportPackage.WriteToPackage( importGroup );
                        }
                    }
                }

                using ( var dtGroupMembers = GetTableData( SQL_GROUPMEMBERS ) )
                {
                    foreach ( DataRow row in dtGroupMembers.Rows )
                    {
                        var importGroupMember = AcsGroupMember.Translate( row );

                        if ( importGroupMember != null )
                        {
                            ImportPackage.WriteToPackage( importGroupMember );
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

        /// <summary>
        /// Loads the available person attributes.
        /// </summary>
        public static void LoadPersonAttributes()
        {
            PersonAttributes = new Dictionary<string, string>();

            var dataTable = GetTableData( SQL_PEOPLE );

            foreach ( DataColumn column in dataTable.Columns )
            {
                string columnName = column.ColumnName;

                // Person attributes always start with "Ind"
                if ( columnName.Contains( "Ind" ) && !columnName.Contains( "Individual" ) )
                {
                    PersonAttributes.Add( column.ColumnName, column.DataType.Name );
                }
            }
        }

        /// <summary>
        /// Loads the available person fields from the ACS export.
        /// </summary>
        /// <returns></returns>
        public static List<string> LoadPersonFields()
        {
            var personFields = new List<string>();

            try
            {
                var dataTable = GetTableData( SQL_PEOPLE );

                foreach ( DataColumn column in dataTable.Columns )
                {
                    string columnName = column.ColumnName;

                    // Person attributes always start with "Ind"
                    if ( ( columnName.Contains( "Ind" ) || columnName.Contains( "Fam" ) ) && 
                           !columnName.Contains( "Individual" ) && !columnName.Contains( "Family" ) )
                    {
                        personFields.Add( column.ColumnName );
                    }
                }
            }
            catch ( Exception ex )
            {
                ErrorMessage = ex.Message;
            }

            return personFields;
        }

        /// <summary>
        /// Writes the person attributes.
        /// </summary>
        public static void WritePersonAttributes()
        {
            foreach ( var attrib in PersonAttributes )
            {
                var attribute = new PersonAttribute();

                // strip out "Ind" from the attribute name and add spaces between words
                attribute.Name = ExtensionMethods.SplitCase( attrib.Key.Replace( "Ind", "" ) );
                attribute.Key = attrib.Key;
                attribute.Category = "Imported Attributes";

                switch ( attrib.Value )
                {
                    case "String":
                        attribute.FieldType = "Rock.Field.Types.TextFieldType";
                        break;
                    case "DateTime":
                        attribute.FieldType = "Rock.Field.Types.DateTimeFieldType";
                        break;
                    default:
                        attribute.FieldType = "Rock.Field.Types.TextFieldType";
                        break;
                }

                ImportPackage.WriteToPackage( attribute );
            }
        }

        /// <summary>
        /// Writes the group types.
        /// </summary>
        public static void WriteGroupTypes()
        {
            // hardcode a generic group type
            ImportPackage.WriteToPackage( new GroupType()
            {
                Id = 9999,
                Name = "Imported Group"
            } );
        }

        public static void WriteFinancialBatch()
        {
            // hardcode a generic financial batch
            ImportPackage.WriteToPackage( new FinancialBatch()
            {
                Id = 9999,
                Name = "Imported Transactions",
                Status = BatchStatus.Closed,
                StartDate = DateTime.Now
            } );
        }

        /// <summary>
        /// Gets the email types.
        /// </summary>
        /// <returns>A list of email types.</returns>
        public static List<string> GetEmailTypes()
        {
            List<string> emailTypes = new List<string>();

            try
            {
                using ( var dtEmailTypes = GetTableData( SQL_EMAIL_TYPES ) )
                {
                    foreach ( DataRow row in dtEmailTypes.Rows )
                    {
                        emailTypes.Add( row.Field<string>( "Description" ) );
                    }
                }
            }
            catch ( Exception ex )
            {
                ErrorMessage = ex.Message;
            }

            return emailTypes;
        }
    }
}
