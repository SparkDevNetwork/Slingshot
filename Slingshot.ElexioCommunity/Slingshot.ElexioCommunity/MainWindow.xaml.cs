using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Slingshot.ElexioCommunity.Utilities;
using Slingshot.Core;
using Slingshot.Core.Model;
using Slingshot.Core.Utilities;

namespace Slingshot.ElexioCommunity
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        System.Windows.Threading.DispatcherTimer _apiUpdateTimer = new System.Windows.Threading.DispatcherTimer();

        private readonly BackgroundWorker exportWorker = new BackgroundWorker();

        public MainWindow()
        {
            InitializeComponent();

            _apiUpdateTimer.Tick += _apiUpdateTimer_Tick;
            _apiUpdateTimer.Interval = new TimeSpan( 0, 0, 1 );

            exportWorker.DoWork += ExportWorker_DoWork;
            exportWorker.RunWorkerCompleted += ExportWorker_RunWorkerCompleted;
            exportWorker.ProgressChanged += ExportWorker_ProgressChanged;
            exportWorker.WorkerReportsProgress = true;
        }

        #region Background Worker Events
        private void ExportWorker_ProgressChanged( object sender, ProgressChangedEventArgs e )
        {
            txtExportMessage.Text = e.UserState.ToString();
            pbProgress.Value = e.ProgressPercentage;
        }

        private void ExportWorker_RunWorkerCompleted( object sender, RunWorkerCompletedEventArgs e )
        {
            txtExportMessage.Text = "Export Complete";
            pbProgress.Value = 100;
        }

        private void ExportWorker_DoWork( object sender, DoWorkEventArgs e )
        {
            exportWorker.ReportProgress( 0, "" );
            _apiUpdateTimer.Start();

            var exportSettings = (ExportSettings)e.Argument;

            // clear filesystem directories
            ElexioCommunityApi.InitializeExport();

            // export individuals
            if ( exportSettings.ExportIndividuals )
            {
                exportWorker.ReportProgress( 1, "Exporting Individuals..." );
                ElexioCommunityApi.ExportIndividuals( exportSettings.PersonCSVFileName );

                if ( ElexioCommunityApi.ErrorMessage.IsNotNullOrWhitespace() )
                {
                    this.Dispatcher.Invoke( () =>
                    {
                        exportWorker.ReportProgress( 4, $"Error exporting individuals: {ElexioCommunityApi.ErrorMessage}" );
                    } );
                }
            }

            // export contributions
            if ( exportSettings.ExportContributions )
            {
                exportWorker.ReportProgress( 25, "Exporting Financial Accounts..." );

                ElexioCommunityApi.ExportFinancialAccounts();
                if ( ElexioCommunityApi.ErrorMessage.IsNotNullOrWhitespace() )
                {
                    exportWorker.ReportProgress( 28, $"Error exporting financial accounts: {ElexioCommunityApi.ErrorMessage}" );
                }

                exportWorker.ReportProgress( 30, "Exporting Financial Pledges..." );

                ElexioCommunityApi.ExportFinancialPledges();
                if ( ElexioCommunityApi.ErrorMessage.IsNotNullOrWhitespace() )
                {
                    exportWorker.ReportProgress( 33, $"Error exporting financial pledges: {ElexioCommunityApi.ErrorMessage}" );
                }

                exportWorker.ReportProgress( 35, "Exporting Contribution Information..." );

                ElexioCommunityApi.ExportFinancialTransactions( exportSettings.GivingCSVFileName );
                if ( ElexioCommunityApi.ErrorMessage.IsNotNullOrWhitespace() )
                {
                    exportWorker.ReportProgress( 38, $"Error exporting financial data: {ElexioCommunityApi.ErrorMessage}" );
                }
            }

            // export groups
            if ( exportSettings.ExportGroups )
            {
                exportWorker.ReportProgress( 50, $"Exporting Groups..." );

                ElexioCommunityApi.ExportGroups();

                if ( ElexioCommunityApi.ErrorMessage.IsNotNullOrWhitespace() )
                {
                    exportWorker.ReportProgress( 53, $"Error exporting groups: {ElexioCommunityApi.ErrorMessage}" );
                }
            }

            // export attendance
            if ( exportSettings.ExportAttendance )
            {
                exportWorker.ReportProgress( 75, $"Exporting Attendance..." );

                ElexioCommunityApi.ExportAttendance();

                if ( ElexioCommunityApi.ErrorMessage.IsNotNullOrWhitespace() )
                {
                    exportWorker.ReportProgress( 78, $"Error exporting attendance: {ElexioCommunityApi.ErrorMessage}" );
                }
            }

            // finalize the package
            ImportPackage.FinalizePackage( "elexio-export.slingshot" );

            _apiUpdateTimer.Stop();
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
            lblApiUsage.Text = $"API Usage: {ElexioCommunityApi.ApiCounter}";
        }

        /// <summary>
        /// Handles the Loaded event of the Window control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void Window_Loaded( object sender, RoutedEventArgs e )
        {
            lblApiUsage.Text = $"API Usage: {ElexioCommunityApi.ApiCounter}";
        }

        /// <summary>
        /// Handles the Click event of the btnDownloadPackage control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void btnDownloadPackage_Click( object sender, RoutedEventArgs e )
        {
            // launch our background export
            var exportSettings = new ExportSettings
            {
                ExportIndividuals = cbIndividuals.IsChecked.Value,
                PersonCSVFileName = txtPersonFilename.Text,
                ExportContributions = cbContributions.IsChecked.Value,
                GivingCSVFileName = txtGivingFilename.Text,
                ExportGroups = cbGroups.IsChecked.Value,
                ExportAttendance = cbAttendance.IsChecked.Value
            };

            exportWorker.RunWorkerAsync( exportSettings );
        }

        #region Window Events

        private void PersonBrowse_Click( object sender, RoutedEventArgs e )
        {
            var fileDialog = new System.Windows.Forms.OpenFileDialog();
            var result = fileDialog.ShowDialog();

            switch ( result )
            {
                case System.Windows.Forms.DialogResult.OK:
                    var file = fileDialog.FileName;
                    txtPersonFilename.Text = file;
                    txtPersonFilename.ToolTip = file;
                    break;
                case System.Windows.Forms.DialogResult.Cancel:
                default:
                    txtPersonFilename.Text = null;
                    txtPersonFilename.ToolTip = null;
                    break;
            }
        }

        private void GivingBrowse_Click( object sender, RoutedEventArgs e )
        {
            var fileDialog = new System.Windows.Forms.OpenFileDialog();
            var result = fileDialog.ShowDialog();

            switch ( result )
            {
                case System.Windows.Forms.DialogResult.OK:
                    var file = fileDialog.FileName;
                    txtGivingFilename.Text = file;
                    txtGivingFilename.ToolTip = file;
                    break;
                case System.Windows.Forms.DialogResult.Cancel:
                default:
                    txtGivingFilename.Text = null;
                    txtGivingFilename.ToolTip = null;
                    break;
            }
        }

        #endregion
    }

    public class ExportSettings
    {
        public bool ExportIndividuals { get; set; } = true;

        public string PersonCSVFileName { get; set; } = "";

        public bool ExportContributions { get; set; } = true;

        public string GivingCSVFileName { get; set; } = "";

        public bool ExportGroups { get; set; } = true;

        public bool ExportAttendance { get; set; } = true;
    }

    public class CheckListItem
    {
        public int Id { get; set; }

        public string Text { get; set; }

        public bool Checked { get; set; }
    }
}

