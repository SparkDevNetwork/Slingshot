using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper;
using Slingshot.Core.Model;
using System.IO.Compression;
using Ionic.Zip;

namespace Slingshot.Core.Utilities
{
    /// <summary>
    /// Static class to write import models to the file system
    /// </summary>
    public static class ImportPackage
    {

        #region Private Fields

        private static string _appDirectory = AppDomain.CurrentDomain.BaseDirectory;
        private static string _packageDirectory = _appDirectory + "Package";
        private static string _imageDirectory = _appDirectory + "Images";

        private static Encoding _encoding;
        private static Dictionary<string, CsvWriter> csvWriters = new Dictionary<string, CsvWriter>();
        private static Dictionary<string, TextWriter> textWriters = new Dictionary<string, TextWriter>();

        private static List<FamilyAddress> _familyAddresses = new List<FamilyAddress>();

        #endregion Private Fields

        #region Public Properties

        /// <summary>
        /// Gets the package directory.
        /// </summary>
        /// <value>
        /// The package directory.
        /// </value>
        public static string PackageDirectory
        {
            get
            {
                return _packageDirectory;
            }
        }

        /// <summary>
        /// Gets the image directory.
        /// </summary>
        /// <value>
        /// The image directory.
        /// </value>
        public static string ImageDirectory
        {
            get
            {
                return _imageDirectory;
            }
        }

        #endregion Public Properties

        #region Constructor

        /// <summary>
        /// Initializes the <see cref="ImportPackage"/> class.
        /// </summary>
        static ImportPackage()
        {
            InitializePackageFolder();

            // This Encoding will ignore character conversion errors (e.g., due to
            // unsupported Unicode characters from SQL) and replace them with an
            // empty string.
            _encoding = Encoding.GetEncoding(
                "UTF-8",
                new EncoderReplacementFallback( string.Empty ),
                new DecoderExceptionFallback() );
        }

        #endregion Constructor

        #region Public Methods

        /// <summary>
        /// Initializes the package folder.
        /// </summary>
        public static void InitializePackageFolder()
        {
            // CSVs
            // delete existing package directory
            if ( Directory.Exists( _packageDirectory ) )
            {
                Directory.Delete( _packageDirectory, true );
            }

            // create fresh package directory
            Directory.CreateDirectory( _packageDirectory );

            // images
            // delete existing package directory
            if ( Directory.Exists( _imageDirectory ) )
            {
                Directory.Delete( _imageDirectory, true );
            }

            // create fresh package directory
            Directory.CreateDirectory( _imageDirectory );
        }

        /// <summary>
        /// Finalizes the package.
        /// </summary>
        /// <param name="exportFilename">The export filename.</param>
        public static void FinalizePackage( string exportFilename )
        {
            // close all csvWriters
            foreach ( var csvWriter in csvWriters )
            {
                csvWriter.Value.Dispose();
            }

            // close all textwriters
            foreach ( var textWriter in textWriters )
            {
                textWriter.Value.Close();
                textWriter.Value.Dispose();
            }

            csvWriters.Clear();
            textWriters.Clear();

            // zip CSV files
            if ( exportFilename.EndsWith( ".slingshot", StringComparison.OrdinalIgnoreCase ) )
            {
                // remove the .slingshot extension if it was specified, so we can get just the filename without it
                exportFilename = exportFilename.Substring( 0, exportFilename.Length - ".slingshot".Length );
            }

            var csvZipFile = _appDirectory + exportFilename + ".slingshot";

            if ( File.Exists( csvZipFile ) )
            {
                File.Delete( csvZipFile );
            }

            using ( ZipFile csvZip = new ZipFile() )
            {
                var csvFiles = Directory.GetFiles( _packageDirectory );

                foreach ( var file in csvFiles )
                {
                    csvZip.AddFile( file, "" );
                }

                csvZip.Save( csvZipFile );
            }

            // zip image files
            var files = Directory.GetFiles( _imageDirectory );
            if ( files.Any() )
            {
                long length = 0;
                int fileCounter = 0;

                ZipFile zip = new ZipFile();

                foreach ( var file in files )
                {
                    // over 100MB
                    if ( length < 104857600 )
                    {
                        zip.AddFile( file, "" );
                    }
                    else
                    {
                        length = 0;
                        zip.Save( _appDirectory + exportFilename + "_" + fileCounter + ".Images.slingshot" );
                        fileCounter++;
                        zip.Dispose();
                        zip = new ZipFile();
                        zip.AddFile( file, "" );
                    }

                    length += new System.IO.FileInfo( file ).Length;
                }

                zip.Save( _appDirectory + exportFilename + "_" + fileCounter + ".Images.slingshot" );
                zip.Dispose();
            }

            // delete package folder
            if ( Directory.Exists( _packageDirectory ) )
            {
                Directory.Delete( _packageDirectory, true );
            }

            // delete images folder
            if ( Directory.Exists( _imageDirectory ) )
            {
                Directory.Delete( _imageDirectory, true );
            }
        }

        /// <summary>
        /// Writes to package.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="model">The model.</param>
        public static void WriteToPackage<T>( T model ) where T : IImportModel
        {
            var importModel = ( IImportModel ) model;
            model.WriteCsvRecord();

            // if person model write out any related models
            if ( importModel is Person )
            {
                WriteRelatedModels( importModel as Person );
            }

            // if financial model write out any related models
            else if ( importModel is FinancialBatch )
            {
                WriteRelatedModels( importModel as FinancialBatch );
            }

            // if financial Transaction model write out any related models
            else if ( importModel is FinancialTransaction )
            {
                WriteRelatedModels( importModel as FinancialTransaction );
            }

            // if group model write out any related models
            else if ( importModel is Group )
            {
                WriteRelatedModels( importModel as Group );
            }

            // if business model write out any related models
            else if ( importModel is Business )
            {
                WriteRelatedModels( importModel as Business );
            }
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Gets the TextWriter.
        /// </summary>
        /// <param name="fileName">The file name.</param>
        /// <returns></returns>
        private static TextWriter GetTextWriter( string fileName )
        {
            return new StreamWriter( $@"{_packageDirectory}\{fileName}", true, _encoding );
        }

        /// <summary>
        /// Ensures that the <see cref="TextWriter"/> is in the textWriters collection.
        /// </summary>
        /// <typeparam name="T">Any type which implements <see cref="IImportModel"/>.</typeparam>
        /// <param name="model">The model.</param>
        private static void EnsureWriters<T>( this T model ) where T : IImportModel
        {
            var typeName = model.GetType().Name;
            if ( !textWriters.ContainsKey( typeName ) )
            {
                var importModel = model as IImportModel;
                textWriters.Add( typeName, GetTextWriter( importModel.GetFileName() ) );
            }

            model.EnsureCsvWriter();
        }

        /// <summary>
        /// Gets the CsvWriter.
        /// </summary>
        /// <param name="textWriter">The <see cref="TextWriter"/>.</param>
        /// <returns></returns>
        private static CsvWriter GetCsvWriter( TextWriter textWriter )
        {
            return new CsvWriter( textWriter );
        }

        /// <summary>
        /// Ensures that the <see cref="CsvWriter"/> is in the csvWriters collection.
        /// </summary>
        /// <typeparam name="T">Any type which implements <see cref="IImportModel"/>.</typeparam>
        /// <param name="model">The model.</param>
        private static void EnsureCsvWriter<T>( this T model ) where T : IImportModel
        {
            var typeName = model.GetType().Name;
            if ( !csvWriters.ContainsKey( typeName ) )
            {
                var textWriter = textWriters[typeName];
                var csvWriter = GetCsvWriter( textWriter );
                csvWriters.Add( typeName, csvWriter );
                csvWriter.WriteHeader<T>();
            }
        }

        /// <summary>
        /// Writes an <see cref="IImportModel"/> record to the CSV file.
        /// </summary>
        /// <typeparam name="T">Any type which implements <see cref="IImportModel"/>.</typeparam>
        /// <param name="model">The <see cref="IImportModel"/>.</param>
        private static void WriteCsvRecord<T>( this T model ) where T : IImportModel
        {
            model.EnsureWriters();
            var typeName = model.GetType().Name;
            var csvWriter = csvWriters[typeName];
            csvWriter.WriteRecord<T>( model );
        }

        /// <summary>
        /// Writes related models for a <see cref="Person"/>.
        /// </summary>
        /// <param name="importPerson">The <see cref="Person"/></param>
        private static void WriteRelatedModels( Person importPerson )
        {
            // person attributes
            importPerson.Attributes.ForEach( a => a.WriteCsvRecord() );

            // person phones
            importPerson.PhoneNumbers.ForEach( n => n.WriteCsvRecord() );

            // person addresses
            foreach ( var address in importPerson.Addresses )
            {
                if ( importPerson.FamilyId.HasValue )
                {
                    var familyAddress = new FamilyAddress
                    {
                        FamilyId = importPerson.FamilyId.Value,
                        Street1 = address.Street1,
                        PostalCode = address.PostalCode.Left( 5 )
                    };

                    var index = _familyAddresses.FindIndex( a => 
                        a.FamilyId == importPerson.FamilyId.Value && 
                        a.Street1.EqualsNullSafe( address.Street1, StringComparison.OrdinalIgnoreCase ) && 
                        a.PostalCode.EqualsNullSafe( address.PostalCode.Left( 5 ) ) );

                    if ( index == -1 )
                    {
                        _familyAddresses.Add( familyAddress );
                        address.WriteCsvRecord();
                    }
                }
                else
                {
                    address.WriteCsvRecord();
                }
            }

            // person search keys
            importPerson.PersonSearchKeys.ForEach( k => k.WriteCsvRecord() );
        }

        /// <summary>
        /// Writes related models for a <see cref="FinancialBatch"/>.
        /// </summary>
        /// <param name="importBatch">The <see cref="FinancialBatch"/></param>
        private static void WriteRelatedModels( FinancialBatch importBatch )
        {
            // write out financial transactions and transaction details
            foreach ( var transaction in importBatch.FinancialTransactions )
            {
                transaction.WriteCsvRecord();
                transaction.FinancialTransactionDetails.ForEach( d => d.WriteCsvRecord() );
            }
        }

        /// <summary>
        /// Writes related models for a <see cref="FinancialTransaction"/>.
        /// </summary>
        /// <param name="importTransaction">The <see cref="FinancialTransaction"/></param>
        private static void WriteRelatedModels( FinancialTransaction importTransaction )
        {
            importTransaction.FinancialTransactionDetails.ForEach( d => d.WriteCsvRecord() );
        }

        /// <summary>
        /// Writes related models for a <see cref="Group"/>.
        /// </summary>
        /// <param name="importGroup">The <see cref="Group"/></param>
        private static void WriteRelatedModels( Group importGroup )
        {
            // group members
            importGroup.GroupMembers.ForEach( m => m.WriteCsvRecord() );

            // group attributes
            importGroup.Attributes.ForEach( a => a.WriteCsvRecord() );

            // group addresses
            importGroup.Addresses.ForEach( a => a.WriteCsvRecord() );
        }

        /// <summary>
        /// Writes related models for a <see cref="Business"/>.
        /// </summary>
        /// <param name="importBusiness">The <see cref="Business"/></param>
        private static void WriteRelatedModels( Business importBusiness )
        {
            // business attributes
            importBusiness.Attributes.ForEach( a => a.WriteCsvRecord() );

            // business phones
            importBusiness.PhoneNumbers.ForEach( n => n.WriteCsvRecord() );

            // business addresses
            importBusiness.Addresses.ForEach( a => a.WriteCsvRecord() );

            // business contacts
            importBusiness.Contacts.ForEach( c => c.WriteCsvRecord() );
        }

        #endregion Private Methods

        private class FamilyAddress
        {
            public int FamilyId { get; set; }
            public string Street1 { get; set; }
            public string PostalCode { get; set; }
        }

    }
}