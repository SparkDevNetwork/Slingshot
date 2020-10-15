using Slingshot.Core;
using Slingshot.Core.Model;
using Slingshot.Core.Utilities;
using Slingshot.PCO.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

namespace Slingshot.PCO
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DispatcherTimer _apiUpdateTimer = new DispatcherTimer();
        private Stopwatch _stopwatch = new Stopwatch();

        private readonly BackgroundWorker exportWorker = new BackgroundWorker();

        private bool _errorHasOccurred = false;

        public List<GroupType> ExportGroupTypes { get; set; }
        public List<CheckListItem> GroupTypesCheckboxItems { get; set; } = new List<CheckListItem>();

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
            string userState = e.UserState.ToString();
            txtExportMessage.Text = userState;
            pbProgress.Value = e.ProgressPercentage;

            if ( _errorHasOccurred )
            {
                if ( userState.IsNotNullOrWhitespace() )
                {
                    txtMessages.Text += userState + Environment.NewLine;
                }
                txtMessages.Visibility = Visibility.Visible;
                txtError.Visibility = Visibility.Visible;
            }
        }

        private void ExportWorker_RunWorkerCompleted( object sender, RunWorkerCompletedEventArgs e )
        {
            btnDownloadPackage.IsEnabled = true;
            if ( !_errorHasOccurred )
            {
                var elapsedTime = String.Format(
                    "{0:00}:{1:00}:{2:00}.{3:00}",
                    _stopwatch.Elapsed.Hours,
                    _stopwatch.Elapsed.Minutes,
                    _stopwatch.Elapsed.Seconds,
                    _stopwatch.Elapsed.Milliseconds / 10 );

                txtExportMessage.Text = $"Export Completed in {elapsedTime}.";
                pbProgress.Value = 100;
            }
        }

        private void ExportWorker_DoWork( object sender, DoWorkEventArgs e )
        {
            exportWorker.ReportProgress( 0, "" );
            _apiUpdateTimer.Start();
            _stopwatch.Start();

            var exportSettings = ( ExportSettings ) e.Argument;

            // clear filesystem directories
            PCOApi.InitializeExport();

            // export individuals
            if ( !_errorHasOccurred && exportSettings.ExportIndividuals )
            {
                exportWorker.ReportProgress( 1, "Exporting Individuals..." );
                PCOApi.ExportIndividuals( exportSettings.ModifiedSince );

                if ( PCOApi.ErrorMessage.IsNotNullOrWhitespace() )
                {
                    _errorHasOccurred = true;
                    this.Dispatcher.Invoke( () =>
                    {
                        exportWorker.ReportProgress( 2, $"Error exporting individuals: {PCOApi.ErrorMessage}" );
                    } );
                }
            }

            // export contributions
            if ( !_errorHasOccurred && exportSettings.ExportContributions )
            {
                exportWorker.ReportProgress( 30, "Exporting Financial Accounts..." );

                PCOApi.ExportFinancialAccounts();
                if ( PCOApi.ErrorMessage.IsNotNullOrWhitespace() )
                {
                    _errorHasOccurred = true;
                    exportWorker.ReportProgress( 31, $"Error exporting financial accounts: {PCOApi.ErrorMessage}" );
                }

                exportWorker.ReportProgress( 34, "Exporting Financial Batches..." );

                PCOApi.ExportFinancialBatches( exportSettings.ModifiedSince );
                if ( PCOApi.ErrorMessage.IsNotNullOrWhitespace() )
                {
                    _errorHasOccurred = true;
                    exportWorker.ReportProgress( 35, $"Error exporting financial batches: {PCOApi.ErrorMessage}" );
                }

                exportWorker.ReportProgress( 36, "Exporting Contribution Information..." );

                PCOApi.ExportContributions( exportSettings.ModifiedSince );
                if ( PCOApi.ErrorMessage.IsNotNullOrWhitespace() )
                {
                    _errorHasOccurred = true;
                    exportWorker.ReportProgress( 37, $"Error exporting financial batches: {PCOApi.ErrorMessage}" );
                }
            }

            // export group types
            if ( !_errorHasOccurred && exportSettings.ExportGroupTypes.Count > 0 )
            {
                exportWorker.ReportProgress( 54, $"Exporting Groups..." );

                // ToDo: This method is not yet implemented
                ////PCOApi.ExportGroups( ExportGroupTypes.Select( t => t.Id ).ToList() );

                if ( PCOApi.ErrorMessage.IsNotNullOrWhitespace() )
                {
                    _errorHasOccurred = true;
                    exportWorker.ReportProgress( 54, $"Error exporting groups: {PCOApi.ErrorMessage}" );
                }
            }

            // finalize the package
            ImportPackage.FinalizePackage( "pco-export.slingshot" );

            _apiUpdateTimer.Stop();
            _stopwatch.Stop();
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
			lblApiUsage.Text = $"API Usage: {PCOApi.ApiCounter}";
            if ( PCOApi.ApiThrottleSeconds > 0 )
            {
                lblApiUsage.Text += $" (Throttled for {PCOApi.ApiThrottleSeconds} seconds.)";
            }
        }

        /// <summary>
        /// Handles the Loaded event of the Window control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void Window_Loaded( object sender, RoutedEventArgs e )
        {
			lblApiUsage.Text = $"API Usage: {PCOApi.ApiCounter}";
            // add group types
            ExportGroupTypes = PCOApi.GetGroupTypes();

            foreach ( var groupType in ExportGroupTypes )
            {
                //cblGroupTypes.Items.Add( groupType );
                GroupTypesCheckboxItems.Add( new CheckListItem { Id = groupType.Id, Text = groupType.Name, Checked = true } );
            }

            cblGroupTypes.ItemsSource = GroupTypesCheckboxItems;

            txtImportCutOff.Text = DateTime.Now.ToShortDateString();
        }

        /// <summary>
        /// Handles the Click event of the btnDownloadPackage control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void btnDownloadPackage_Click( object sender, RoutedEventArgs e )
        {
            btnDownloadPackage.IsEnabled = false;

            // clear result from previous export
            txtExportMessage.Text = string.Empty;
            txtMessages.Text = string.Empty;
            txtMessages.Visibility = Visibility.Collapsed;
            txtError.Visibility = Visibility.Collapsed;
            _errorHasOccurred = false;

            // launch our background export
            var exportSettings = new ExportSettings
            {
                ModifiedSince = ( DateTime ) txtImportCutOff.Text.AsDateTime(),
                ExportContributions = cbContributions.IsChecked.Value,
                ExportIndividuals = cbIndividuals.IsChecked.Value
            };

            // configure group types to export
            if ( cbGroups.IsChecked.Value == true )
            {
                foreach ( var selectedItem in GroupTypesCheckboxItems.Where( i => i.Checked ) )
                {
                    exportSettings.ExportGroupTypes.Add( selectedItem.Id );
                }
            }

            exportWorker.RunWorkerAsync( exportSettings );
        }

        #region Window Events


        private void cbGroups_Checked( object sender, RoutedEventArgs e )
        {
            if ( cbGroups.IsChecked.Value )
            {
                gridMain.RowDefinitions[5].Height = new GridLength( 1, GridUnitType.Auto );

            }
            else
            {
                gridMain.RowDefinitions[5].Height = new GridLength( 0 );
            }
        }

        #endregion
    }

    public class ExportSettings
    {
        public DateTime ModifiedSince { get; set; } = DateTime.Now;

        public bool ExportIndividuals { get; set; } = true;

        public bool ExportContributions { get; set; } = true;

        public List<int> ExportGroupTypes { get; set; } = new List<int>();
    }

    public class CheckListItem
    {
        public int Id { get; set; }

        public string Text { get; set; }

        public bool Checked { get; set; }
    }
}

