using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

using Slingshot.Core;
using Slingshot.Core.Model;
using Slingshot.Core.Utilities;

using System.IO.Compression;
using System.IO;
using Slingshot.ServantKeeper.Models;
using Slingshot.ServantKeeper.Utilities.Translators;
using System.Linq;

namespace Slingshot.ServantKeeper.Utilities
{
    public static class ServantKeeperApi
    {
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
        public static string ErrorMessage
        {
            get;
            set;
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


        public static Table GetTable( string dbFile )
        {

            using ( var file = File.OpenRead( FileName ) )
            using ( var zip = new ZipArchive( file, ZipArchiveMode.Read ) )
            {
                foreach ( var entry in zip.Entries )
                {
                    if ( entry.FullName.ToLower().EndsWith( dbFile ) )
                    {
                        using ( var stream = entry.Open() )
                        {
                            byte[] buffer = new byte[16 * 1024];
                            using ( MemoryStream ms = new MemoryStream() )
                            {
                                int read;
                                while ( ( read = stream.Read( buffer, 0, buffer.Length ) ) > 0 )
                                {
                                    ms.Write( buffer, 0, read );
                                }
                                return FixedWidthFile.Convert( ms.ToArray() );
                            }
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Initializes the export.
        /// </summary>
        public static void InitializeExport()
        {
            ImportPackage.InitalizePackageFolder();
        }

        /// <summary>
        /// Opens the specified MS Access database.
        /// </summary>
        /// <param name="fileName">Name of the file</param>
        public static void OpenConnection( string fileName )
        {
            FileName = fileName;


            IsConnected = true;
        }

        /// <summary>
        /// Exports the individuals.
        /// </summary>
        public static void ExportIndividuals( DateTime modifiedSince, string emailType, string campusKey )
        {
            try
            {
                List<Label> labels = GetTable( "csudflbl.sdb" ).Map<Label>();
                List<Field> fields = GetTable( "cstable.sdb" ).Map<Field>();

                List<Family> families = GetTable( "csfamily.udb" ).Map<Family>();
                families.AddRange( GetTable( "csfambin.udb" ).Map<Family>() );

                List<Value> values = GetTable( "csreftbl.udb" ).Map<Value>();
                List<Individual> individuals = GetTable( "csind.udb" ).Map<Individual>();

                // export inactive people
                List<Individual> inactives = GetTable( "csindbin.udb" ).Map<Individual>();
                inactives.ForEach( p => p.RecordStatus = RecordStatus.Inactive );
                individuals.AddRange( inactives.Where( i2 => !individuals.Select( i => i.Id ).ToList().Contains( i2.Id ) ).ToList() );

                // export people
                foreach ( Individual indv in individuals )
                {
                    Person person = SKPerson.Translate( indv, values, families, fields, labels );
                    if ( modifiedSince.Year <= 1 || person.CreatedDateTime > modifiedSince || person.ModifiedDateTime > modifiedSince )
                    {
                        ImportPackage.WriteToPackage( person );
                    }
                }

                //load attributes
                LoadPersonAttributes( fields, labels );

                // write out the person attributes
                WritePersonAttributes();

                // TODO: Export Person/Family Notes
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

            List<Account> accounts = GetTable( "csacct.udb" ).Map<Account>();
            List<AccountLink> links = GetTable( "csqkacct.udb" ).Map<AccountLink>();
            try
            {
                foreach ( Account account in accounts )
                {
                    var importAccount = SKFinancialAccount.Translate( account, links );

                    if ( importAccount != null )
                    {
                        ImportPackage.WriteToPackage( importAccount );
                    }
                }
            }
            catch ( Exception ex )
            {
                ErrorMessage = ex.Message;
            }
        }

        /// <summary>
        /// Exports any contributions.
        /// </summary>
        public static void ExportContributions( DateTime modifiedSince )
        {
            try
            {
                List<Batch> batches = GetTable( "csbatch.udb" ).Map<Batch>();

                try
                {
                    foreach ( Batch batch in batches )
                    {
                        var importBatch = SKBatch.Translate( batch );
                        if ( modifiedSince.Year <= 1 || importBatch.CreatedDateTime > modifiedSince || importBatch.ModifiedDateTime > modifiedSince )
                        {
                            ImportPackage.WriteToPackage( importBatch );
                        }
                    }
                }
                catch ( Exception ex )
                {
                    ErrorMessage = ex.Message;
                }

                List<Contribution> contributions = GetTable( "csconmst.udb" ).Map<Contribution>();
                List<ContributionDetail> contributionDetails = GetTable( "cscondtl.udb" ).Map<ContributionDetail>();

                foreach ( Contribution contribution in contributions )
                {
                    var importFinancialTransaction = SKContribution.Translate( contribution, contributionDetails );
                    

                    if ( modifiedSince.Year <= 1 || importFinancialTransaction.CreatedDateTime > modifiedSince || importFinancialTransaction.ModifiedDateTime > modifiedSince )
                    {
                        ImportPackage.WriteToPackage( importFinancialTransaction );

                        foreach ( var importDetail in importFinancialTransaction.FinancialTransactionDetails )
                        {
                            ImportPackage.WriteToPackage( importDetail );
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
        /// Loads the available person attributes.
        /// </summary>
        public static void LoadPersonAttributes( List<Field> fields, List<Label> labels )
        {
            PersonAttributes = new Dictionary<string, string>();

            // Handle the User Defined Fields
            var properties = typeof( Individual ).GetProperties();
            foreach ( var property in properties )
            {
                if ( property.Name.ToLower().Contains( "udf" ) )
                {
                    var attribute = property.CustomAttributes.Where( ca => ca.AttributeType.Name == "ColumnName" ).FirstOrDefault();

                    if ( attribute != null )
                    {
                        var fieldKey = ( ( string ) attribute.ConstructorArguments.FirstOrDefault().Value ).ToLower();
                        var field = fields.Where( tf => tf.Name.ToLower().Contains( fieldKey ) ).FirstOrDefault();

                        if ( field != null )
                        {
                            PersonAttributes.Add( labels.Where( l => l.LabelId == field.LabelId ).Select( l => l.Description ).DefaultIfEmpty( field.Description ).FirstOrDefault().Replace( " ", string.Empty ), property.PropertyType.Name );
                        }
                    }
                }
            }

            PersonAttributes.Add( "JoinDate", typeof( DateTime ).Name );
            PersonAttributes.Add( "HowJoined", typeof( string ).Name );
            PersonAttributes.Add( "BaptizedDate", typeof( DateTime ).Name );
            PersonAttributes.Add( "Baptized", typeof( string ).Name );
            PersonAttributes.Add( "Occupation", typeof( string ).Name );
            PersonAttributes.Add( "Employer", typeof( string ).Name );
            PersonAttributes.Add( "SundaySchool", typeof( string ).Name );

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
    }
}
