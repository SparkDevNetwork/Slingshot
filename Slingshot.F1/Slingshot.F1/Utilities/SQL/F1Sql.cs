using Slingshot.Core;
using Slingshot.Core.Model;
using Slingshot.Core.Utilities;
using Slingshot.F1.Utilities.SQL;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;


namespace Slingshot.F1.Utilities
{
    /// <summary>
    /// F1 MDB Translator.
    /// </summary>

    public partial class F1Sql : F1Translator
    {

        #region Private Fields

        private static F1SqlDatabase _db;
        private static Dictionary<string, string> _RequirementNames;
        private static Dictionary<int, HeadOfHousehold> _HeadOfHouseholdMapCache = null;
        private static List<GroupType> _groupTypes;

        #endregion Private Fields

        #region Public Properties

        /// <summary>
        /// Gets or sets the file name.
        /// </summary>
        /// <value>
        /// The file name.
        /// </value>
        public static string FileName { get; set; }

        #endregion Public Properties

        #region Public Methods

        // NOTE:  More public methods are located in F1Sql.ExportMethods.cs.

        /// <summary>
        /// Opens the specified SQL Database (MDF) file and reads and validates the schema without reading the data.
        /// </summary>
        /// <param name="fileName">Name of the file</param>
        public static void OpenConnection( string fileName )
        {
            FileName = fileName;

            try
            {
                _db = new F1SqlDatabase( fileName );
                F1Sql.IsConnected = true;
            }
            catch ( Exception ex )
            {
                ErrorMessage = ex.Message;
                F1Sql.IsConnected = false;
            }
        }

        /// <summary>
        /// Opens the SQL Database (MDF) file and reads the data into memory.
        /// </summary>
        public void OpenDatabase()
        {
            _db = new F1SqlDatabase( FileName, true );
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
                Name = "Position Description",
                Key = "Position_Description",
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
            var requirementNames = _db.Table( "Requirement" ).Data.AsEnumerable()
                .Select( r => r.Field<string>( "requirement_name" ) ).Distinct().ToList();

            _RequirementNames = new Dictionary<string, string>( StringComparer.OrdinalIgnoreCase );
            foreach ( var requirementName in requirementNames )
            {
                _RequirementNames.Add( requirementName, requirementName );

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

            // Add F1 Attributes
            var databaseAttributes = _db.Table( "Attribute" ).Data.AsEnumerable()
                .Select( r => new {
                    AttributeGroupName = r.Field<string>( "Attribute_Group_Name" ),
                    AttributeName = r.Field<string>( "Attribute_Name" ),
                    AttributeId = r.Field<int>( "Attribute_Id" )
                } ).Distinct().ToList();

            foreach ( var attribute in databaseAttributes )
            {
                // comment attribute
                var personAttributeComment = new PersonAttribute()
                {
                    Name = attribute.AttributeName + " Comment",
                    Key = attribute.AttributeId + "_" + attribute.AttributeName.RemoveSpaces().RemoveSpecialCharacters() + "Comment",
                    Category = attribute.AttributeGroupName,
                    FieldType = "Rock.Field.Types.TextFieldType"
                };
                ImportPackage.WriteToPackage( personAttributeComment );

                // start date attribute
                var personAttributeStartDate = new PersonAttribute()
                {
                    Name = attribute.AttributeName + " Start Date",
                    Key = attribute.AttributeId + "_" + attribute.AttributeName.RemoveSpaces().RemoveSpecialCharacters() + "StartDate",
                    Category = attribute.AttributeGroupName,
                    FieldType = "Rock.Field.Types.DateFieldType"
                };
                ImportPackage.WriteToPackage( personAttributeStartDate );

                // end date attribute
                var personAttributeEndDate = new PersonAttribute()
                {
                    Name = attribute.AttributeName + " End Date",
                    Key = attribute.AttributeId + "_" + attribute.AttributeName.RemoveSpaces().RemoveSpecialCharacters() + "EndDate",
                    Category = attribute.AttributeGroupName,
                    FieldType = "Rock.Field.Types.DateFieldType"
                };
                ImportPackage.WriteToPackage( personAttributeEndDate );

                // Add the attributes to the list
                attributes.Add( personAttributeComment );
                attributes.Add( personAttributeStartDate );
                attributes.Add( personAttributeEndDate );
            }


            // Add F1 Communications that aren't email and phone numbers
            var communicationAttributes = _db.Table( "Communication" ).Data.AsEnumerable()
                .Where( r => r.Field<string>( "communication_type" ).ToLower() != "mobile" )
                .Where( r => r.Field<string>( "communication_type" ).ToLower() != "email" )
                .Where( r => !r.Field<string>( "communication_type" ).ToLower().Contains( "phone" ) )
                .Select( r => r.Field<string>( "communication_type" ) ).Distinct().ToList();

            foreach ( var communicationType in communicationAttributes )
            {
                var personAttribute = new PersonAttribute()
                {
                    Name = communicationType,
                    Key = "F1" + communicationType.RemoveSpaces().RemoveSpecialCharacters(),
                    Category = "Communications",
                    FieldType = "Rock.Field.Types.TextFieldType"
                };

                ImportPackage.WriteToPackage( personAttribute );
                attributes.Add( personAttribute );
            }

            return attributes;
        }

        /// <summary>
        /// Exports the business attributes.
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
            if ( _groupTypes != null )
            {
                return _groupTypes;
            }

            _groupTypes = new List<GroupType>();

            try
            {
                var dvGroups = new DataView( _db.Table( "Groups").Data );
                var dtDistinctGroupTypes = dvGroups.ToTable( true, "Group_Type_Name", "Group_Type_ID" );
                _groupTypes = dtDistinctGroupTypes.AsEnumerable()
                    .Where( r => r.Field<int?>( "Group_Type_ID" ) != null )
                    .Select( r => new GroupType {
                        Name = r.Field<string>( "Group_Type_Name" ),
                        Id = r.Field<int>( "Group_Type_ID" )
                    } ).Distinct().ToList();
            }
            catch ( Exception ex )
            {
                ErrorMessage = ex.Message;
            }

            _groupTypes.Add( new GroupType
            {
                Id = 99999904,
                Name = "F1 Activities"
            } );

            return _groupTypes;
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

        /// <summary>
        /// Cleans up cached data to release it to the operating system when the export is complete.
        /// </summary>
        public override void Cleanup()
        {
            GC.Collect();
        }

        #endregion Public Methods

    }

}
