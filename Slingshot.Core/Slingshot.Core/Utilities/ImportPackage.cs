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
        static string _appDirectory = AppDomain.CurrentDomain.BaseDirectory;
        static string _packageDirectory = _appDirectory + "Package";

        static Dictionary<string, CsvWriter> csvWriters = new Dictionary<string, CsvWriter>();
        static Dictionary<string, TextWriter> textWriters = new Dictionary<string, TextWriter>();

        /// <summary>
        /// Initializes the <see cref="ImportPackage"/> class.
        /// </summary>
        static ImportPackage()
        {
            InitalizePackageFolder();
        }

        /// <summary>
        /// Initalizes the package folder.
        /// </summary>
        public static void InitalizePackageFolder()
        {
            // delete existing package directory
            if ( Directory.Exists( _packageDirectory ) )
            {
                Directory.Delete( _packageDirectory, true );
            }

            // create fresh package directory
            Directory.CreateDirectory( _packageDirectory );
        }

        /// <summary>
        /// Writes to package.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="model">The model.</param>
        public static void WriteToPackage<T>(T model )
        {
            var typeName = model.GetType().ToString();

            if ( model is IImportModel )
            {
                var importModel = (IImportModel)model;
                // check if a textwriter is needed for this model type
                if ( !textWriters.ContainsKey( typeName ) )
                {
                    if ( !Directory.Exists( _packageDirectory ) )
                    {
                        InitalizePackageFolder();
                    }

                    textWriters.Add( typeName, (TextWriter)File.CreateText( $@"{_packageDirectory}\{importModel.GetFileName()}" ) );

                    // if model is for person create related writers
                    if ( importModel is Person )
                    {
                        // person attributes
                        var personAttributeValue = new PersonAttributeValue();
                        textWriters.Add( personAttributeValue.GetType().Name, (TextWriter)File.CreateText( $@"{_packageDirectory}\{personAttributeValue.GetFileName()}" ) );

                        // person phones
                        var personPhone = new PersonPhone();
                        textWriters.Add( personPhone.GetType().Name, (TextWriter)File.CreateText( $@"{_packageDirectory}\{personPhone.GetFileName()}" ) );

                        // person addresses
                        var personAddress = new PersonAddress();
                        textWriters.Add( personAddress.GetType().Name, (TextWriter)File.CreateText( $@"{_packageDirectory}\{personAddress.GetFileName()}" ) );
                    }

                    // if model is for financial batch create related writers
                    if ( importModel is FinancialBatch )
                    {
                        // financial transactions
                        var financialTransaction = new FinancialTransaction();
                        textWriters.Add( financialTransaction.GetType().Name, (TextWriter)File.CreateText( $@"{_packageDirectory}\{financialTransaction.GetFileName()}" ) );

                        // financial transation details
                        var financialTransactionDetail = new FinancialTransactionDetail();
                        textWriters.Add( financialTransactionDetail.GetType().Name, (TextWriter)File.CreateText( $@"{_packageDirectory}\{financialTransactionDetail.GetFileName()}" ) );

                    }

                    // if model is for group create related writers
                    if ( importModel is Group )
                    {
                        // group member
                        var groupMember = new GroupMember();
                        textWriters.Add( groupMember.GetType().Name, ( TextWriter ) File.CreateText( $@"{_packageDirectory}\{groupMember.GetFileName()}" ) );
                    }
                }

                var txtWriter = textWriters[typeName];

                // check if a csvwriter is needed for this model type
                if ( !csvWriters.ContainsKey( typeName ) )
                {
                    var newCsvWriter = new CsvWriter( txtWriter );
                    csvWriters.Add( typeName, newCsvWriter );
                    newCsvWriter.WriteHeader<T>();
                    //newCsvWriter.Configuration.QuoteAllFields = true;

                    // if model is for person create related writers
                    if ( importModel is Person )
                    {
                        // person attributes
                        var personAttributeValue = new PersonAttributeValue();
                        var newPersonAttributeValueCsvWriter = new CsvWriter( textWriters[ personAttributeValue.GetType().Name ] );
                        csvWriters.Add( personAttributeValue.GetType().Name, newPersonAttributeValueCsvWriter );
                        newPersonAttributeValueCsvWriter.WriteHeader<PersonAttributeValue>();

                        // person phones
                        var personPhone = new PersonPhone();
                        var newPersonPhoneCsvWriter = new CsvWriter( textWriters[personPhone.GetType().Name] );
                        csvWriters.Add( personPhone.GetType().Name, newPersonPhoneCsvWriter );
                        newPersonPhoneCsvWriter.WriteHeader<PersonPhone>();

                        // person addresses
                        var personAddress = new PersonAddress();
                        var newPersonAddressCsvWriter = new CsvWriter( textWriters[personAddress.GetType().Name] );
                        csvWriters.Add( personAddress.GetType().Name, newPersonAddressCsvWriter );
                        newPersonAddressCsvWriter.WriteHeader<PersonAddress>();
                    }

                    // if model is for financial batch create related writers
                    if ( importModel is FinancialBatch )
                    {
                        // financial transaction
                        var financialTransaction = new FinancialTransaction();
                        var newFinancialTransactionCsvWriter = new CsvWriter( textWriters[financialTransaction.GetType().Name] );
                        csvWriters.Add( financialTransaction.GetType().Name, newFinancialTransactionCsvWriter );
                        newFinancialTransactionCsvWriter.WriteHeader<FinancialTransaction>();

                        // financial transaction detail
                        var financialTransactionDetail = new FinancialTransactionDetail();
                        var newFinancialTransactionDetailCsvWriter = new CsvWriter( textWriters[financialTransactionDetail.GetType().Name] );
                        csvWriters.Add( financialTransactionDetail.GetType().Name, newFinancialTransactionDetailCsvWriter );
                        newFinancialTransactionDetailCsvWriter.WriteHeader<FinancialTransactionDetail>();
                    }

                    // if model is for group create related writers
                    if ( importModel is Group )
                    {
                        // group member
                        var groupMember = new GroupMember();
                        var newGroupMemberCsvWriter = new CsvWriter( textWriters[groupMember.GetType().Name] );
                        csvWriters.Add( groupMember.GetType().Name, newGroupMemberCsvWriter );
                        newGroupMemberCsvWriter.WriteHeader<GroupMember>();
                    }
                }

                var csvWriter = csvWriters[typeName];

                csvWriter.WriteRecord<T>( model );

                // if person model write out any related models
                if ( importModel is Person )
                {
                    // person attributes
                    var personAttributeValue = new PersonAttributeValue();
                    var csvPersonAttributeValueWriter = csvWriters[personAttributeValue.GetType().Name];

                    if ( csvPersonAttributeValueWriter != null )
                    {
                        foreach ( var attribute in ((Person)importModel).Attributes )
                        {
                            csvPersonAttributeValueWriter.WriteRecord<PersonAttributeValue>( attribute );
                        }
                    }

                    // person phones
                    var personPhone = new PersonPhone();
                    var csvPersonPhoneWriter = csvWriters[personPhone.GetType().Name];

                    if ( csvPersonPhoneWriter != null )
                    {
                        foreach( var phone in ((Person)importModel).PhoneNumbers )
                        {
                            csvPersonPhoneWriter.WriteRecord<PersonPhone>( phone );
                        }
                    }

                    // person addresses
                    var personAddress = new PersonAddress();
                    var csvPersonAddressWriter = csvWriters[personAddress.GetType().Name];

                    if ( csvPersonAddressWriter != null )
                    {
                        foreach ( var address in ((Person)importModel).Addresses )
                        {
                            csvPersonAddressWriter.WriteRecord<PersonAddress>( address );
                        }
                    }
                }

                // if financial model write out any related models
                if ( importModel is FinancialBatch )
                {
                    // write out financial transactions and transaction details
                    var financialTransaction = new FinancialTransaction();
                    var csvFinancialTransactionWriter = csvWriters[financialTransaction.GetType().Name];

                    var financialTransactionDetail = new FinancialTransactionDetail();
                    var csvFinancialTransactionDetailWriter = csvWriters[financialTransactionDetail.GetType().Name];

                    if ( csvFinancialTransactionWriter != null && csvFinancialTransactionDetailWriter != null )
                    {
                        foreach ( var transaction in ((FinancialBatch)importModel).FinancialTransactions )
                        {
                            csvFinancialTransactionWriter.WriteRecord<FinancialTransaction>( transaction );

                            foreach( var transactionDetail in transaction.FinancialTransactionDetails )
                            {
                                csvFinancialTransactionDetailWriter.WriteRecord<FinancialTransactionDetail>( transactionDetail );
                            }
                        }
                    }
                }

                // if group model write out any related models
                if ( importModel is Group )
                {
                    // group members
                    var groupMember = new GroupMember();
                    var csvGroupMemberWriter = csvWriters[groupMember.GetType().Name];

                    if ( csvGroupMemberWriter != null )
                    {
                        foreach ( var groupMemberItem in ( ( Group ) importModel ).GroupMembers )
                        {
                            csvGroupMemberWriter.WriteRecord<GroupMember>( groupMemberItem );
                        }
                    }
                }
            }
        }

        public static void FinalizePackage( string exportFilename )
        {
            // close all csvWriters
            foreach(var csvWriter in csvWriters )
            {
                csvWriter.Value.Dispose();
            }

            // close all textwriters
            foreach(var textWriter in textWriters )
            {
                textWriter.Value.Close();
                textWriter.Value.Dispose();
            }

            csvWriters.Clear();
            textWriters.Clear();

            // zip files
            var zipFile = _appDirectory + exportFilename;
            if (File.Exists( zipFile ) )
            {
                File.Delete( zipFile );
            }

            using ( ZipFile zip = new ZipFile() )
            {
                var files = Directory.GetFiles( _packageDirectory );

                foreach (var file in files )
                {
                    zip.AddFile( file, "" );
                }

                zip.Save( zipFile );
            }

            // delete package folder
            if ( Directory.Exists( _packageDirectory ) )
            {
                Directory.Delete( _packageDirectory, true );
            }
        }
    }
}
