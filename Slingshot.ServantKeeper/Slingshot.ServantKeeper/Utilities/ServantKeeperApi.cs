using Slingshot.Core.Model;
using Slingshot.Core.Utilities;
using Slingshot.ServantKeeper.Utilities.Translators;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Text.RegularExpressions;

namespace Slingshot.ServantKeeper.Utilities
{
    public static class ServanrKeeperApi
    {
        private static OleDbConnection _dbConnection;
        private static DateTime _modifiedSince;
        private static DateTime _ContributionsNotBefore;
        private static string _SelectedGroupIDs;
        private static string _SQLClause;

        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        public static string ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the User Defined Column information for a Person
        /// </summary>
        public static Dictionary<Tuple<string, string>, string> _PersonUDFColumns { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance is connected.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is connected; otherwise, <c>false</c>.
        /// </value>
        public static bool IsConnected { get; private set; } = false;


        #region SQL Queries

        private static string SQL_ALL_GROUPS
        {
            get
            { return $@"
SELECT REC_ID AS GROUP_ID, GROUP_NAME FROM csGROUP ORDER BY GROUP_NAME";
            }
        }

        private static string SQL_SELECTED_GROUPS
        {
            get
            {
                return $@"
SELECT REC_ID AS GROUP_ID, GROUP_NAME, CONDITION, UDF1 AS INC_DISABLED FROM csGROUP
WHERE REC_ID IN ({ _SelectedGroupIDs }) ORDER BY GROUP_NAME";
            }
        }

        private static string SQL_GROUPMEMBERS
        {
            get
            {
                return $@"
SELECT DISTINCT I.REC_ID AS PERSON_ID
FROM(csIND AS I INNER JOIN csFAMILY AS F ON F.FAMILY_ID = I.FAMILY_ID)
LEFT JOIN csATTR A ON A.IND_ID = I.IND_ID
WHERE { _SQLClause }";
            }
        }

        private static string SQL_PEOPLE
        {
            get
            { return $@"
SELECT F.REC_ID AS FAMILY_REC_ID, F.FAMILY_ID, F.FAM_NAME, RC.DESCS AS RELATIONSHIP,
I.REC_ID AS PERSON_REC_ID, I.IND_ID, I.ACTIVE_IND, I.TITLE, I.FIRST_NAME, I.PREFERNAME, I.MID_NAME,
I.LAST_NAME, I.SUFFIX, I.SEX, I.BIRTH_DT, MC.DESCS AS MARITAL, MS.DESCS AS MEM_STATUS, I.MEM_DT,
I.WEDDING_DT, I.JOIN_DT, I.HOW_JOIN, I.BAPTIZED, I.BAPTIZE_DT, I.EMPLOYER, JC.DESCS AS JOB, 
LC.DESCS AS LAYMAN, I.GRADE, I.ENV_NO, I.STATUS,
F.H_PHONE, F.H_UNLIST, I.W_PHONE, I.W_UNLIST, I.C_PHONE, I.C_UNLIST, I.EMAIL1, I.EMAIL1_IND, 
IIF(I.CREATE_TS > F.CREATE_TS, I.CREATE_TS, F.CREATE_TS) AS CREATE_TS,
IIF(I.UPDATE_TS > F.UPDATE_TS, I.UPDATE_TS, F.UPDATE_TS) AS UPDATE_TS,
IIF(F.ADDR2 <> '', LEFT(F.ADDR1, INSTR(1, F.ADDR1, '|')-1 ), F.ADDR1) AS ADDR1, F.ADDR2, F.CITY, F.STATE, F.ZIP, F.COUNTRY,
I.UDF1, I.UDF2, I.UDF3, I.UDF4, I.UDF5, I.UDF6, I.UDF7, I.UDF8, I.UDF9, I.UDF10, I.UDF11, I.UDF12, I.UDF13, I.UDF14, I.UDF15, I.UDF16,
I.UDF_DT1, I.UDF_DT2, I.UDF_DT3, I.UDF_DT4, I.UDF_DT5, I.UDF_DT6, I.UDF_DT7, I.UDF_DT8, I.UDF_DT9, I.UDF_DT10,
I.UDF_CHECK1, I.UDF_CHECK2, I.UDF_CHECK3, I.UDF_CHECK4, I.UDF_CHECK5, I.UDF_CHECK6, I.UDF_CHECK7, I.UDF_CHECK8, I.UDF_CHECK9, I.UDF_CHK10
FROM (((((

(SELECT REC_ID, IND_ID, TITLE, FIRST_NAME, PREFERNAME, MID_NAME, LAST_NAME, SUFFIX,
FAMILY_ID, ACTIVE_IND, SEX, BIRTH_DT, MARITAL_CD, RELAT_CD, MEM_STATUS, MEM_DT,
WEDDING_DT, JOIN_DT, HOW_JOIN, BAPTIZED, BAPTIZE_DT, EMPLOYER, JOB_CD, LAYMAN_CD,
GRADE, ENV_NO, STATUS, W_PHONE, W_UNLIST, C_PHONE, C_UNLIST, EMAIL1, EMAIL1_IND, CREATE_TS, UPDATE_TS,
UDF1, UDF2, UDF3, UDF4, UDF5, UDF6, UDF7, UDF8, UDF9, UDF10, UDF11, UDF12, UDF13, UDF14, UDF15, UDF16,
UDF_DT1, UDF_DT2, UDF_DT3, UDF_DT4, UDF_DT5, UDF_DT6, UDF_DT7, UDF_DT8, UDF_DT9, UDF_DT10,
UDF_CHECK1, UDF_CHECK2, UDF_CHECK3, UDF_CHECK4, UDF_CHECK5, UDF_CHECK6, UDF_CHECK7, UDF_CHECK8, UDF_CHECK9, UDF_CHK10
FROM csIND

UNION

SELECT REC_ID, IND_ID, TITLE, FIRST_NAME, PREFERNAME, MID_NAME, LAST_NAME, SUFFIX,
FAMILY_ID, ACTIVE_IND, SEX, BIRTH_DT, MARITAL_CD, RELAT_CD, MEM_STATUS, MEM_DT,
WEDDING_DT, JOIN_DT, HOW_JOIN, BAPTIZED, BAPTIZE_DT, EMPLOYER, JOB_CD, LAYMAN_CD,
GRADE, ENV_NO, STATUS, W_PHONE, W_UNLIST, C_PHONE, C_UNLIST, EMAIL1, EMAIL1_IND, CREATE_TS, UPDATE_TS,
UDF1, UDF2, UDF3, UDF4, UDF5, UDF6, UDF7, UDF8, UDF9, UDF10, UDF11, UDF12, UDF13, UDF14, UDF15, UDF16,
UDF_DT1, UDF_DT2, UDF_DT3, UDF_DT4, UDF_DT5, UDF_DT6, UDF_DT7, UDF_DT8, UDF_DT9, UDF_DT10,
UDF_CHECK1, UDF_CHECK2, UDF_CHECK3, UDF_CHECK4, UDF_CHECK5, UDF_CHECK6, UDF_CHECK7, UDF_CHECK8, UDF_CHECK9, UDF_CHK10
FROM csINDBin) AS I

INNER JOIN csFAMILY AS F ON F.FAMILY_ID = I.FAMILY_ID) 
LEFT JOIN csREFTBL AS MC ON MC.TBL_ID = I.MARITAL_CD) 
LEFT JOIN csREFTBL AS MS ON MS.TBL_ID = I.MEM_STATUS) 
LEFT JOIN csREFTBL AS JC ON JC.TBL_ID = I.JOB_CD) 
LEFT JOIN csREFTBL AS RC ON RC.TBL_ID = I.RELAT_CD) 
LEFT JOIN csREFTBL AS LC ON LC.TBL_ID = I.LAYMAN_CD
WHERE I.UPDATE_TS >= #{ _modifiedSince.ToShortDateString() }#";
            }
        }

        private static string SQL_UDF_COLUMNS
        {
            get
            { return $@"
SELECT U.DESCS AS UDF_NAME, RIGHT(T.FIELD_NAME, LEN(T.FIELD_NAME) - INSTR(T.FIELD_NAME, '>')) AS UDF_COLUMN, T.FIELD_TYPE
FROM csUDFLBL AS U INNER JOIN csTABLE AS T ON T.LBL_ID = U.LABEL_ID
WHERE T.TYPE = 'IND'";
            }
        }

//        private static string SQL_PEOPLE_NOTES
//        {
//            get
//            {
//                return $@"
//SELECT P.[IndividualId]
//      ,IC.[ComtDate]
//      ,IC.[ComtType]
//      ,IC.[Comment]
//  FROM [IComment] IC
//          INNER JOIN people P 
//            ON P.[FamilyNumber] = IC.[FamilyNumber] 
//                AND P.[IndividualNumber] = IC.[IndividualNumber]
//GROUP BY P.[IndividualId], IC.[ComtDate], IC.[ComtType], IC.[Comment]";
//            }
//        }

//        private static string SQL_FAMILY_NOTES
//        {
//            get
//            {
//                return $@"
//SELECT FC.[FamilyNumber]
//    ,FC.[ComtDate]
//    ,FC.[ComtType]
//    ,FC.[Comment]
// FROM [FComment] FC
// GROUP BY FC.[FamilyNumber], FC.[ComtDate], FC.[ComtType], FC.[Comment]";
//            }
//        }

        private static string SQL_FINANCIAL_ACCOUNTS
        {
            get
            { return $@"
SELECT REC_ID AS ACCOUNT_ID, ACCT_NAME, TAX_IND, ACTIVE_IND
FROM csACCT AS A
WHERE EXISTS (SELECT * FROM csCONDTL WHERE ACCT_ID = A.ACCT_ID AND BATCH_DT >= #{ _ContributionsNotBefore.ToShortDateString() }#)
ORDER BY REC_ID";
            }
        }

        private static string SQL_FINANCIAL_BATCHES
        {
            get
            { return $@"
SELECT REC_ID AS BATCH_ID, BATCH_NAME, BATCH_DT, CREATE_TS, UPDATE_TS
FROM csBATCH
WHERE UPDATE_TS >= #{ _modifiedSince.ToShortDateString() }# AND BATCH_DT >= #{ _ContributionsNotBefore.ToShortDateString() }#
ORDER BY REC_ID";
            }
        }

        private static string SQL_FINANCIAL_CONTRIBUTIONS
        {
            get   // Valid Batch record is required.
            { return $@"
SELECT CM.REC_ID AS CONTRIBUTION_ID, I.REC_ID AS PERSON_ID, B.REC_ID AS BATCH_ID, CM.BATCH_DT, CM.PAY_TYPE, CM.CHECK_NO, CM.NOTE, CM.TAX_IND, CM.CREATE_TS, CM.UPDATE_TS
FROM (csCONMST AS CM INNER JOIN csBatch AS B ON B.BATCH_KEY = CM.BATCH_KEY) LEFT JOIN
(SELECT REC_ID, IND_ID FROM csIND UNION SELECT REC_ID, IND_ID FROM csINDBin) AS I ON I.IND_ID = CM.IND_ID
WHERE CM.UPDATE_TS >= #{ _modifiedSince.ToShortDateString() }# AND CM.BATCH_DT >= #{ _ContributionsNotBefore.ToShortDateString() }#";
            }
        }

        private static string SQL_FINANCIAL_CONTRIBUTIONDETAILS
        {
            get // Valid Batch record is required. Records without one are not returned.
            { return $@"
SELECT CD.REC_ID AS DETAIL_ID, CM.REC_ID AS CONTRIBUTION_ID, A.REC_ID AS ACCOUNT_ID, CD.AMT, CD.NOTE, CD.CREATE_TS, CD.UPDATE_TS
FROM ((csCONDTL AS CD INNER JOIN csCONMST AS CM ON CD.CONT_ID = CM.CONT_ID)
INNER JOIN csACCT AS A ON CD.ACCT_ID = A.ACCT_ID)
INNER JOIN csBATCH AS B ON B.BATCH_KEY = CM.BATCH_KEY
WHERE CD.UPDATE_TS >= #{ _modifiedSince.ToShortDateString() }# AND CD.BATCH_DT >= #{ _ContributionsNotBefore.ToShortDateString() }#
ORDER BY CD.REC_ID";
            }
        }

        #endregion region


        /// <summary>
        /// Initializes the export.
        /// </summary>
        public static void InitializeExport(DateTime modifiedSince, DateTime NotBefore)
        {
            _modifiedSince = modifiedSince;
            _ContributionsNotBefore = NotBefore;

            ImportPackage.InitializePackageFolder();
        }


        /// Opens the specified MS Access database.
        /// <param name="fileName">Name of the file</param>
        public static void OpenConnection(string fileName)
        {
            _dbConnection = new OleDbConnection { ConnectionString = $"Provider=Microsoft.Jet.OLEDB.4.0;Data Source={fileName}" };
            try
            {
                _dbConnection.Open();
                IsConnected = true;
                _dbConnection.Close();
            }
            catch { IsConnected = false; }
        }


        /// <summary>
        /// Gets the group definitions.
        /// </summary>
        public static Dictionary<int,string> GetAllGroups()
        {
            Dictionary<int, string> groups = new Dictionary<int, string>();

            using (DataTable dtGroups = GetTableData(SQL_ALL_GROUPS))
            {
                foreach (DataRow row in dtGroups.Rows)
                    groups.Add(row.Field<int>("GROUP_ID"), row.Field<string>("GROUP_NAME"));
            }
            return groups;
        }


        /// <summary>
        /// Exports the individuals.
        /// </summary>
        public static void ExportIndividuals()
        {
            try
            {
                // Get the User Defined Column information for a Person
                string fieldType;

                _PersonUDFColumns = new Dictionary<Tuple<string, string>, string>();
                using (DataTable data = GetTableData(SQL_UDF_COLUMNS))
                {
                    foreach (DataRow row in data.Rows)
                    {
                        switch (row.Field<string>("FIELD_TYPE"))
                        {
                            case "C":
                                fieldType = "text"; break;
                            case "N":
                                fieldType = "number"; break;  // Checkbox = 1 or 0
                            case "D":
                                fieldType = "date"; break;
                            default:
                                continue;
                        }
                        _PersonUDFColumns.Add(new Tuple<string, string>(row.Field<string>("UDF_NAME"), fieldType), row.Field<string>("UDF_COLUMN"));
                    }
                }

                // export people
                using (var dtPeople = GetTableData(SQL_PEOPLE))
                {
                    foreach (DataRow row in dtPeople.Rows)
                    {
                        var importPerson = SkPerson.Translate(row);
                        if (importPerson is null) continue;

                        ImportPackage.WriteToPackage(importPerson);
                    }
                }

            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }


        /// <summary>
        /// Exports any contributions.
        /// </summary>
        public static void ExportContributions()
        {
            try
            {
                // Retrieve & Output the Fund Accounts
                using (var dtFunds = GetTableData(SQL_FINANCIAL_ACCOUNTS))
                {
                    foreach (DataRow row in dtFunds.Rows)
                    {
                        FinancialAccount account = SkFinancialAccount.Translate(row);
                        if (account is null) continue;

                        ImportPackage.WriteToPackage(account);
                    }
                }

                // Retrieve & Output the Batches
                using (var dtBatches = GetTableData(SQL_FINANCIAL_BATCHES))
                {
                    foreach (DataRow row in dtBatches.Rows)
                    {
                        FinancialBatch batch = SkFinancialBatch.Translate(row);
                        if (batch is null) continue;

                        ImportPackage.WriteToPackage(batch);
                    }
                }

                // Retrieve & Output the Contributions
                using (var dtContributions = GetTableData(SQL_FINANCIAL_CONTRIBUTIONS))
                {
                    foreach (DataRow row in dtContributions.Rows)
                    {
                        FinancialTransaction transaction = SkFinancialContribution.Translate(row);
                        if (transaction is null) continue;

                        ImportPackage.WriteToPackage(transaction);
                    }
                }

                // Retrieve & Output the Contribution Details
                using (var dtContributionDetails = GetTableData(SQL_FINANCIAL_CONTRIBUTIONDETAILS))
                {
                    foreach (DataRow row in dtContributionDetails.Rows)
                    {
                        FinancialTransactionDetail details = SkFinancialContributionDetail.Translate(row);
                        if (details is null) continue;

                        ImportPackage.WriteToPackage(details);
                    }
                }

            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }

        /// <summary>
        /// Exports any groups found.  Currently, this export doesn't support
        ///  group hierarchies and all groups will be imported to the
        ///  root of the group viewer.
        /// </summary>
        public static void ExportGroups(string Selections)
        {
            // Make sure some group was selected
            if (Selections.Length == 0) return;
            _SelectedGroupIDs = Selections;

            // Create a Group Type and Parent Group to house these groups
            var type = new GroupType()
            {
                Id = 9999,
                Name = "ServantKeeper"
            };
            ImportPackage.WriteToPackage(type);
            var ParentGroup = new Core.Model.Group()
            {
                Id = 9999,
                Name = "From ServantKeeper",
                IsActive = true,
                IsPublic = true,
                GroupTypeId = type.Id
            };
            ImportPackage.WriteToPackage(ParentGroup);

            // Retrieve the information for the selected Group IDs
            try
            {
                using (DataTable dtGroups = GetTableData(SQL_SELECTED_GROUPS))
                {
                    Regex regex = new Regex($@"(?:(?<logic>OR|AND)\|)?[0-9]+~~(?<table>[A-Z]+)->(?<column>[A-Z0-9_]+).*?~[=|b]~""(?<value>.*?)""~~");
                
                    // Process each selected group
                    foreach (DataRow row in dtGroups.Rows)
                    {
                        var group = SkGroup.Translate(row, ParentGroup);
                        if (group == null) continue;

                        // Initialize the Group Members SQL clause. Does this group include Disabled/Inactive people?
                        if (row.Field<string>("INC_DISABLED") == "1")
                            _SQLClause = "";
                        else
                            _SQLClause = "(I.ACTIVE_IND <> '1' OR I.ACTIVE_IND is null) AND ";

                        // Decode the Servant Keeper group definition. Construct a SQL clause to get the Members.
                        string definition = row.Field<string>("CONDITION");
                        foreach (Match match in regex.Matches(definition))
                        {
                            // Add in logic operators as needed
                            switch (match.Groups["logic"].Value)
                            {
                                case "AND": _SQLClause += " AND "; break;
                                case "OR": _SQLClause += ") OR ("; break;
                            }

                            // Groups can be defined using an csIND, csATTR or csFamily table value.
                            switch (match.Groups["table"].Value)
                            {
                                case "CSATTR":
                                    _SQLClause += "A.TBL_ID='" + match.Groups["value"].Value.Trim() + "'";
                                    break;
                                case "CSIND":
                                    _SQLClause += "I." + match.Groups["column"].Value + "='" + match.Groups["value"].Value.Trim() + "'";
                                    break;
                                case "CSFAMILY":
                                    _SQLClause += "F." + match.Groups["column"].Value + "='" + match.Groups["value"].Value.Trim() + "'";
                                    break;
                            }
                        }
                        _SQLClause = "(" + _SQLClause + ")";

                        // Retrieve the Members of this Group
                        using (DataTable dtGroupMembers = GetTableData(SQL_GROUPMEMBERS))
                            foreach (DataRow GM_row in dtGroupMembers.Rows)
                            {
                                var member = SkGroupMember.Translate(GM_row, group.Id);
                                if (member == null) continue;

                                group.GroupMembers.Add(member);
                            }

                        // Export this Group with Members
                        ImportPackage.WriteToPackage(group);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }

        /// <summary>
        /// Gets the table data.
        /// </summary>
        /// <param name="command">The SQL command to run.</param>
        /// <returns></returns>
        public static DataTable GetTableData(string command)
        {
            DataSet dataSet = new DataSet();
            DataTable dataTable = new DataTable();
            OleDbCommand dbCommand = new OleDbCommand(command, _dbConnection);

            OleDbDataAdapter adapter = new OleDbDataAdapter();
            adapter.SelectCommand = dbCommand;
            adapter.Fill(dataSet);
            dataTable = dataSet.Tables["Table"];

            return dataTable;
        }
    }
}
