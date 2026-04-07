using System;
using Slingshot.Core.Utilities;

using Slingshot.Breeze.Utilities.Translators;
using System.IO;
using CsvHelper;
using System.ComponentModel;
using System.Collections.Generic;

using Excel = Microsoft.Office.Interop.Excel;
using System.Runtime.InteropServices;
using Slingshot.Core.Model;
using System.Linq;

namespace Slingshot.Breeze.Utilities
{
    public static class BreezeApi
    {
        /// <summary>
        /// Gets or sets the most recent error message
        /// </summary>
        /// <value>
        /// The error message.
        /// </value>
        public static string ErrorMessage { get; set; }

        /// <summary>
        /// Initializes the export folder. Deletes existing CSV and image output directories
        /// </summary>
        public static void InitializeExport()
        {
            ImportPackage.InitializePackageFolder();
        }

        /// <summary>
        /// Takes a CSV filepath and adds all the breeze person records within that file to the
        /// exported slingshot data
        /// </summary>
        public static void ExportPeople( string peopleCsvFilename, BackgroundWorker worker )
        {
            try
            {
                // Reset the error message
                ErrorMessage = null;

                // Open the CSV file and begin reading the Breeze people data
                using ( var reader = File.OpenText( peopleCsvFilename ) )
                using ( var csv = new CsvReader( reader ) )
                {
                    var attributes = new List<PersonAttribute>();

                    while ( csv.Read() )
                    {
                        var recordDictionary = csv.GetRecord<dynamic>() as IDictionary<string, object>;
                        var person = BreezePerson.Translate( recordDictionary, attributes );
                        ImportPackage.WriteToPackage( person );

                        // Report percentage for progress bar
                        var percentage = ( double ) reader.BaseStream.Position / reader.BaseStream.Length;
                        worker.ReportProgress( (int)(percentage * 100) );
                    }

                    attributes.ForEach( ImportPackage.WriteToPackage );
                }
            }
            catch ( Exception ex )
            {
                ErrorMessage = ex.Message;
            }
        }

        /// <summary>
        /// Takes a CSV filepath and adds all the breeze person note records within that file to the
        /// exported slingshot data
        /// </summary>
        public static void ExportNotes( string notesCsvFilename, BackgroundWorker worker )
        {
            try
            {
                // Reset the error message
                ErrorMessage = null;

                // Open the CSV file and begin reading the Breeze people notes data
                using ( var reader = File.OpenText( notesCsvFilename ) )
                using ( var csv = new CsvReader( reader ) )
                {
                    while ( csv.Read() )
                    {
                        var recordDictionary = csv.GetRecord<dynamic>() as IDictionary<string, object>;
                        var note = BreezeNote.Translate( recordDictionary );
                        ImportPackage.WriteToPackage( note );

                        // Report percentage for progress bar
                        var percentage = ( double ) reader.BaseStream.Position / reader.BaseStream.Length;
                        worker.ReportProgress( ( int ) ( percentage * 100 ) );
                    }
                }
            }
            catch ( Exception ex )
            {
                ErrorMessage = ex.Message;
            }
        }

        public static void ExportGiving( string csvFilename, BackgroundWorker worker )
        {
            try
            {
                // Reset the error message
                ErrorMessage = null;

                // Open the CSV file and begin reading the Breeze data
                using ( var reader = File.OpenText( csvFilename ) )
                using ( var csv = new CsvReader( reader ) )
                {
                    var accounts = new List<FinancialAccount>();
                    var batches = new List<FinancialBatch>();

                    while ( csv.Read() )
                    {
                        var recordDictionary = csv.GetRecord<dynamic>() as IDictionary<string, object>;
                        var gift = BreezeGift.Translate( recordDictionary, accounts, batches );
                        
                        // These are written with the batch, don't write here or it causes CSV file lock issues
                        //ImportPackage.WriteToPackage( gift );

                        // Report percentage for progress bar
                        var percentage = ( double ) reader.BaseStream.Position / reader.BaseStream.Length;
                        worker.ReportProgress( ( int ) ( percentage * 100 ) );
                    }

                    accounts.ForEach( ImportPackage.WriteToPackage );
                    batches.OrderBy( b => b.StartDate ).ToList().ForEach( ImportPackage.WriteToPackage );
                }
            }
            catch ( Exception ex )
            {
                ErrorMessage = ex.Message;
            }
        }

        public static void ExportTags( string excelFilename, BackgroundWorker worker )
        {
            try
            {
                // Reset the error message
                ErrorMessage = null;

                // hardcode a generic group type
                const int groupTypeId = 9999;

                ImportPackage.WriteToPackage( new GroupType()
                {
                    Id = groupTypeId,
                    Name = "Breeze Tags"
                } );

                // Read excel sheet
                // Excel is not zero based, but 1 based
                // https://coderwall.com/p/app3ya/read-excel-file-in-c
                var excelApp = new Excel.Application();
                var workbook = excelApp.Workbooks.Open( excelFilename );
                const int headerRowIndex = 1;
                var sheetCount = workbook.Sheets.Count;

                for ( var sheetIndex = 1; sheetIndex <= sheetCount; sheetIndex++ )
                {
                    Excel._Worksheet worksheet = workbook.Sheets[sheetIndex];
                    var usedRange = worksheet.UsedRange;

                    int rowCount = usedRange.Rows.Count;
                    int colCount = usedRange.Columns.Count;
                    var colIndexHeaderMap = new Dictionary<int, string>();

                    // Each sheet represents a group
                    var group = new Group {
                        Id = sheetIndex,
                        Name = worksheet.Name,
                        GroupTypeId = groupTypeId
                    };

                    // Map the column indexes to the header titles in a dictionary
                    for ( int colIndex = 1; colIndex <= colCount; colIndex++ )
                    {
                        var cell = usedRange.Cells[headerRowIndex, colIndex];
                                               
                        if ( cell == null || cell.Value == null)
                        {
                            colIndexHeaderMap[colIndex] = colIndex.ToString();
                        }
                        else
                        {
                            colIndexHeaderMap[colIndex] = cell.Value2.ToString();
                        }
                    }

                    // Translate each row as a record of a tag
                    for ( var rowIndex = headerRowIndex + 1; rowIndex <= rowCount; rowIndex++ )
                    {
                        var record = new Dictionary<string, object>();

                        for ( int colIndex = 1; colIndex <= colCount; colIndex++ )
                        {
                            var cell = usedRange.Cells[rowIndex, colIndex];
                            var header = colIndexHeaderMap[colIndex];

                            if ( cell == null || cell.Value == null )
                            {
                                record[header] = string.Empty;
                            }
                            else
                            {
                                record[header] = cell.Value2.ToString();
                            }
                        }

                        var groupMember = BreezeTag.Translate( record );
                        groupMember.GroupId = group.Id;
                        group.GroupMembers.Add( groupMember );

                        // Report percentage for progress bar
                        var percentPerSheet = 100 / sheetCount;
                        var sheetPercentage = (int) (( double ) sheetIndex / sheetCount * 100);
                        var rowPercentage = ( double ) rowIndex / rowCount;
                        worker.ReportProgress( ( int ) ( sheetPercentage + ( rowPercentage * percentPerSheet ) ) );
                    }

                    //release com objects to fully kill excel process from running in the background
                    Marshal.ReleaseComObject( usedRange );
                    Marshal.ReleaseComObject( worksheet );

                    // Write the group to the package
                    ImportPackage.WriteToPackage(group);
                }

                // Garbage collection cleanup
                GC.Collect();
                GC.WaitForPendingFinalizers();

                // close and release Workbook
                workbook.Close();
                Marshal.ReleaseComObject( workbook );

                // quit and release Excel App
                excelApp.Quit();
                Marshal.ReleaseComObject( excelApp );
            }
            catch ( Exception ex )
            {
                ErrorMessage = ex.Message;
            }
        }
    }
}
