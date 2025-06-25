using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;

using Slingshot.Core;
using Slingshot.Core.Utilities;

using Slingshot.Breeze.Utilities;
using System.Windows.Forms;
using System.Windows.Media;

namespace Slingshot.Breeze
{
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer _apiUpdateTimer = new DispatcherTimer();
        private readonly BackgroundWorker _exportWorker = new BackgroundWorker();
        private bool hasErrors = false;

        public MainWindow()
        {
            InitializeComponent();

            _apiUpdateTimer.Tick += _apiUpdateTimer_Tick;
            _apiUpdateTimer.Interval = new TimeSpan( 0, 2, 30 );

            _exportWorker.DoWork += ExportWorker_DoWork;
            _exportWorker.RunWorkerCompleted += ExportWorker_RunWorkerCompleted;
            _exportWorker.ProgressChanged += ExportWorker_ProgressChanged;
            _exportWorker.WorkerReportsProgress = true;
        }

        #region Background Worker Events

        private void ExportWorker_ProgressChanged( object sender, ProgressChangedEventArgs e )
        {
            if ( e.UserState != null )
            {
                txtExportMessage.Text = e.UserState.ToString();
            }

            pbProgress.Value = e.ProgressPercentage;
        }

        private void ExportWorker_RunWorkerCompleted( object sender, RunWorkerCompletedEventArgs e )
        {
            txtExportMessage.Text = hasErrors ? "Errors Occurred" : "Export Complete";
            pbProgress.Value = 100;
            pbProgress.Foreground = hasErrors ? Brushes.Red : Brushes.Green;
            btnExecuteConversion.IsEnabled = true;
        }

        private void ExportWorker_DoWork( object sender, DoWorkEventArgs e )
        {
            _exportWorker.ReportProgress( 0, "" );

            var exportSettings = ( ExportSettings ) e.Argument;

            // clear filesystem directories
            BreezeApi.InitializeExport();

            // export individuals, phone numbers, and addresses
            _exportWorker.ReportProgress( 1, "Exporting Individuals..." );
            BreezeApi.ExportPeople( exportSettings.PersonCsvFileName, _exportWorker );

            if ( BreezeApi.ErrorMessage.IsNotNullOrWhitespace() )
            {
                ShowError( $"Error exporting individuals: {BreezeApi.ErrorMessage}" );
                return;
            }

            // export notes
            _exportWorker.ReportProgress( 1, "Exporting Notes..." );
            BreezeApi.ExportNotes( exportSettings.NotesCsvFileName, _exportWorker );

            if ( BreezeApi.ErrorMessage.IsNotNullOrWhitespace() )
            {
                ShowError( $"Error exporting notes: {BreezeApi.ErrorMessage}" );
                return;
            }

            // export gifts
            _exportWorker.ReportProgress( 1, "Exporting Gifts..." );
            BreezeApi.ExportGiving( exportSettings.GivingCsvFileName, _exportWorker );

            if ( BreezeApi.ErrorMessage.IsNotNullOrWhitespace() )
            {
                ShowError( $"Error exporting gifts: {BreezeApi.ErrorMessage}" );
                return;
            }

            // export tags
            _exportWorker.ReportProgress( 1, "Exporting Tags..." );
            BreezeApi.ExportTags( exportSettings.TagsXlsxFileName, _exportWorker );

            if ( BreezeApi.ErrorMessage.IsNotNullOrWhitespace() )
            {
                ShowError( $"Error exporting tags: {BreezeApi.ErrorMessage}" );
                return;
            }

            // finalize the package
            if ( !hasErrors )
            {
                ImportPackage.FinalizePackage( "breeze-export.slingshot" );
            }

            // schedule the API status to update (the status takes a few mins to update)
            _apiUpdateTimer.Start();
        }

        #endregion

        /// <summary>
        /// Handles the Tick event of the _apiUpdateTimer control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void _apiUpdateTimer_Tick( object sender, EventArgs e )
        {
            // update the api stats
            _apiUpdateTimer.Stop();
        }

        /// <summary>
        /// Handles the Click event of the btnDownloadPackage control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void btnExecuteConversion_Click( object sender, RoutedEventArgs e )
        {
            // launch our background export
            var exportSettings = new ExportSettings
            {
                PersonCsvFileName = tbPersonCsvFile.Text,
                NotesCsvFileName = tbNotesCsvFile.Text,
                GivingCsvFileName = tbGivingCsvFile.Text,
                TagsXlsxFileName = tbTagsXlsxFile.Text
            };

            hasErrors = false;
            txtMessages.Text = string.Empty;
            txtExportMessage.Text = string.Empty;
            pbProgress.Value = 0;
            pbProgress.Foreground = Brushes.Green;
            btnExecuteConversion.IsEnabled = false;

            _exportWorker.RunWorkerAsync( exportSettings );
        }

        private void btnPersonCsvBrowse_Click( object sender, RoutedEventArgs e )
        {
            DoFileBrowser( tbPersonCsvFile );
        }

        private void btnNotesCsvBrowse_Click( object sender, RoutedEventArgs e )
        {
            DoFileBrowser( tbNotesCsvFile );
        }

        private void btnGivingCsvBrowse_Click( object sender, RoutedEventArgs e )
        {
            DoFileBrowser( tbGivingCsvFile );
        }

        private void btnTagsXlsxBrowse_Click( object sender, RoutedEventArgs e )
        {
            DoFileBrowser( tbTagsXlsxFile );
        }

        private void DoFileBrowser( System.Windows.Controls.TextBox input )
        {
            var fileDialog = new OpenFileDialog();
            var result = fileDialog.ShowDialog();

            if ( result == System.Windows.Forms.DialogResult.OK )
            {
                var file = fileDialog.FileName;
                input.Text = file;
                input.ToolTip = file;
            }
            else
            {
                input.Text = null;
                input.ToolTip = null;
            }
        }

        private void ShowError( string errorMessage )
        {
            if ( string.IsNullOrWhiteSpace(errorMessage) )
            {
                return;
            }

            hasErrors = true;
            Dispatcher.Invoke( () => txtMessages.Text = errorMessage );
        }
    }

    public class ExportSettings
    {
        public string PersonCsvFileName { get; set; }
        public string NotesCsvFileName { get; set; }
        public string GivingCsvFileName { get; set; }
        public string TagsXlsxFileName { get; set; }
    }
}