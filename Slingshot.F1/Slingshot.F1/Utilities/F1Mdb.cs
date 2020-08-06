using Slingshot.Core;
using Slingshot.Core.Model;
using Slingshot.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;

namespace Slingshot.F1.Utilities
{
    /// <summary>
    /// F1 MDB Translator.
    /// </summary>

    public partial class F1Mdb : F1Translator
    {

        #region Private Fields

        private static OleDbConnection _dbConnection;
        private Dictionary<string, string> _RequirementNames;
        private static Dictionary<string, DataTable> _TableDataCache = new Dictionary<string, DataTable>();
        private static Dictionary<int, HeadOfHousehold> _HeadOfHouseholdMapCache = null;

        #endregion Private Fields

        #region Public Properties

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
                if ( Environment.Is64BitProcess )
                {
                    return $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={FileName}";
                }

                return $"Provider=Microsoft.Jet.OLEDB.4.0;Data Source={FileName}";
            }
        }

        #endregion Public Properties

        #region Public Methods

        // NOTE:  More public methods are located in F1Mdb.ExportMethods.cs.

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
                Name = "Default Tag Comment",
                Key = "F1_Default_Tag_Comment",
                Category = "Childhood Information",
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
                Name = "F1 School",
                Key = "F1School",
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

            var attributes = new List<PersonAttribute>();

            // Add F1 Requirements
            using ( var dtRequirements = GetTableData( SqlQueries.REQUIREMENTS ) )
            {
                _RequirementNames = new Dictionary<string, string>();
                foreach ( DataRow row in dtRequirements.Rows )
                {
                    string requirementName = row.Field<string>( "requirement_name" );
                    _RequirementNames.Add( requirementName.ToLower(), requirementName );

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

                // Cleanup - Remember not to Clear() any cached tables.
                dtRequirements.Clear();
                GC.Collect();
            }

            // Add F1 Attributes
            using ( var dtAttributes = GetTableData( SqlQueries.ATTRIBUTES ) )
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
                        Key = attributeId.ToString() + "_" + attributeName.RemoveSpaces().RemoveSpecialCharacters() + "Comment",
                        Category = attributeGroup,
                        FieldType = "Rock.Field.Types.TextFieldType"
                    };

                    ImportPackage.WriteToPackage( personAttributeComment );

                    // start date attribute
                    var personAttributeStartDate = new PersonAttribute()
                    {
                        Name = attributeName + " Start Date",
                        Key = attributeId.ToString() + "_" + attributeName.RemoveSpaces().RemoveSpecialCharacters() + "StartDate",
                        Category = attributeGroup,
                        FieldType = "Rock.Field.Types.DateFieldType"
                    };

                    ImportPackage.WriteToPackage( personAttributeStartDate );

                    // end date attribute
                    var personAttributeEndDate = new PersonAttribute()
                    {
                        Name = attributeName + " End Date",
                        Key = attributeId.ToString() + "_" + attributeName.RemoveSpaces().RemoveSpecialCharacters() + "EndDate",
                        Category = attributeGroup,
                        FieldType = "Rock.Field.Types.DateFieldType"
                    };

                    ImportPackage.WriteToPackage( personAttributeEndDate );

                    // Add the attributes to the list
                    attributes.Add( personAttributeComment );
                    attributes.Add( personAttributeStartDate );
                    attributes.Add( personAttributeEndDate );
                }

                // Cleanup - Remember not to Clear() any cached tables.
                dtAttributes.Clear();
                GC.Collect();
            }

            // Add F1 Communications that aren't email and phone numbers
            using ( var dtCommunications = GetTableData( SqlQueries.COMMUNCATION_TYPES_FOR_ATTRIBUTES ) )
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

                // Cleanup - Remember not to Clear() any cached tables.
                dtCommunications.Clear();
                GC.Collect();
            }

            return attributes;
        }

        /// <summary>
        /// Exports the person attributes.
        /// </summary>
        public void WriteBusinessAttributes()
        {
            // export business fields as attributes
            ImportPackage.WriteToPackage( new BusinessAttribute()
            {
                Name = "Company Type",
                Key = "CompanyType",
                Category = "Business",
                FieldType = "Rock.Field.Types.TextFieldType"
            } );

            ImportPackage.WriteToPackage( new BusinessAttribute()
            {
                Name = "Contact Name",
                Key = "ContactName",
                Category = "Business",
                FieldType = "Rock.Field.Types.TextFieldType"
            } );
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
                using ( var dtGroupTypes = GetTableData( SqlQueries.GROUP_TYPES, true ) )
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
        /// Cleans up cached data to release it to the operating system when the export is complete.
        /// </summary>
        public override void Cleanup()
        {
            foreach ( var dataTable in _TableDataCache.Values )
            {
                dataTable.Clear();
            }
            GC.Collect();
        }

        #endregion Public Methods

    }

}
