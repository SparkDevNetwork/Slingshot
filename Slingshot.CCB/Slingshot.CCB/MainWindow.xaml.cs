using Slingshot.CCB.Utilities;
using Slingshot.Core;
using Slingshot.Core.Model;
using Slingshot.Core.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace Slingshot.CCB
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            _apiUpdateTimer.Tick += _apiUpdateTimer_Tick;
            _apiUpdateTimer.Interval = new TimeSpan( 0, 2, 30 );

            exportWorker.DoWork += ExportWorker_DoWork;
            exportWorker.RunWorkerCompleted += ExportWorker_RunWorkerCompleted;
            exportWorker.ProgressChanged += ExportWorker_ProgressChanged;
            exportWorker.WorkerReportsProgress = true;
        }

        #region Internal Fields and Properties

        private DispatcherTimer _apiUpdateTimer = new DispatcherTimer();

        private readonly BackgroundWorker exportWorker = new BackgroundWorker();

        private bool _errorHasOccurred = false;

        private List<GroupType> ExportGroupTypes { get; set; }

        private List<CheckListItem> GroupTypesCheckboxItems { get; set; } = new List<CheckListItem>();

        #endregion

        #region Background Worker Events

        /// <summary>
        /// Handles the ProgressChanged event for the ExportWorker.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ProgressChangedEventArgs"/> instance containing the event data.</param>
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

        /// <summary>
        /// Handles the RunWorkerCompleted event for the ExportWorker.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RunWorkerCompletedEventArgs"/> instance containing the event data.</param>
        private void ExportWorker_RunWorkerCompleted( object sender, RunWorkerCompletedEventArgs e )
        {
            btnDownloadPackage.IsEnabled = true;
            if ( !_errorHasOccurred )
            {
                txtExportMessage.Text = "Export Complete";
                pbProgress.Value = 100;

                if ( CcbApi.IncompleteGroups.Any() )
                {
                    string incompleteGroupIds = string.Empty;
                    foreach( int groupId in CcbApi.IncompleteGroups )
                    {
                        if ( !string.IsNullOrWhiteSpace( incompleteGroupIds ) )
                        {
                            incompleteGroupIds += ", ";
                        }
                        incompleteGroupIds += groupId.ToString();
                    }

                    txtError.Visibility = Visibility.Visible;
                    txtMessages.Visibility = Visibility.Visible;
                    if ( !string.IsNullOrWhiteSpace( txtMessages.Text ) )
                    {
                        txtMessages.Text += Environment.NewLine;
                    }
                    txtMessages.Text += "The following groups could not be downloaded from the CCB API: "
                        + incompleteGroupIds + ".  This error will not prevent the rest of the export from functioning correctly.";
                }

            }
        }

        /// <summary>
        /// Handles the DoWork event for the ExportWorker.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="DoWorkEventArgs"/> instance containing the event data.</param>
        private void ExportWorker_DoWork( object sender, DoWorkEventArgs e )
        {
            exportWorker.ReportProgress( 0, "" );

            var exportSettings = ( ExportSettings ) e.Argument;

            // clear filesystem directories
            CcbApi.InitializeExport();

            // export individuals
            if ( ( !_errorHasOccurred ) && ( exportSettings.ExportIndividuals ) )
            {
                exportWorker.ReportProgress( 1, "Exporting Individuals..." );
                CcbApi.ExportIndividuals( exportSettings.ModifiedSince );

                if ( CcbApi.ErrorMessage.IsNotNullOrWhitespace() )
                {
                    _errorHasOccurred = true;
                    this.Dispatcher.Invoke( () =>
                    {
                        exportWorker.ReportProgress( 2, $"Error exporting individuals: {CcbApi.ErrorMessage}" );
                    } );
                }
            }

            // export contributions
            if ( ( !_errorHasOccurred ) && ( exportSettings.ExportContributions ) )
            {
                exportWorker.ReportProgress( 30, "Exporting Financial Accounts..." );

                CcbApi.ExportFinancialAccounts();
                if ( CcbApi.ErrorMessage.IsNotNullOrWhitespace() )
                {
                    _errorHasOccurred = true;
                    exportWorker.ReportProgress( 31, $"Error exporting financial accounts: {CcbApi.ErrorMessage}" );
                }

                exportWorker.ReportProgress( 35, "Exporting Contribution Information..." );

                CcbApi.ExportContributions( exportSettings.ModifiedSince );
                if ( CcbApi.ErrorMessage.IsNotNullOrWhitespace() )
                {
                    _errorHasOccurred = true;
                    exportWorker.ReportProgress( 36, $"Error exporting financial batches: {CcbApi.ErrorMessage}" );
                }
            }

            // export group types
            if ( ( !_errorHasOccurred ) && ( exportSettings.ExportGroupTypes.Count > 0 ) )
            {
                exportWorker.ReportProgress( 54, $"Exporting Groups..." );

                CcbApi.ExportGroups( exportSettings.ExportGroupTypes, exportSettings.ModifiedSince );

                if ( CcbApi.ErrorMessage.IsNotNullOrWhitespace() )
                {
                    _errorHasOccurred = true;
                    exportWorker.ReportProgress( 54, $"Error exporting groups: {CcbApi.ErrorMessage}" );
                }
            }

            // export attendance 
            if ( ( !_errorHasOccurred ) && ( exportSettings.ExportAttendance ) )
            {
                exportWorker.ReportProgress( 75, $"Exporting Attendance..." );

                CcbApi.ExportAttendance( exportSettings.ModifiedSince );


                if ( CcbApi.ErrorMessage.IsNotNullOrWhitespace() )
                {
                    _errorHasOccurred = true;
                    exportWorker.ReportProgress( 75, $"Error exporting attendance: {CcbApi.ErrorMessage}" );
                }
            }

            // finalize the package
            ImportPackage.FinalizePackage( "ccb-export.slingshot" );

            // schedule the API status to update (the status takes a few mins to update)
            _apiUpdateTimer.Start();
        }

        #endregion

        #region Window/Control Events

        /// <summary>
        /// Handles the Tick event of the _apiUpdateTimer control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void _apiUpdateTimer_Tick( object sender, EventArgs e )
        {
            // update the api stats
            CcbApi.UpdateApiStatus();
            lblApiUsage.Text = $"API Usage: {CcbApi.Counter} / {CcbApi.DailyLimit}";
            _apiUpdateTimer.Stop();
        }

        /// <summary>
        /// Handles the Loaded event of the Window control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void Window_Loaded( object sender, RoutedEventArgs e )
        {
            int remainingRequests = CcbApi.DailyLimit - CcbApi.Counter;
            CcbApi.ApiRequestLimit = remainingRequests;

            lblApiUsage.Text = $"API Usage: {CcbApi.Counter} / {CcbApi.DailyLimit}";
            txtItemsPerPage.Text = CcbApi.ItemsPerPage.ToString();
            txtThrottleRate.Text = CcbApi.ApiThrottleRate.ToString();

            // add group types
            ExportGroupTypes = CcbApi.GetGroupTypes().OrderBy( t => t.Name ).ToList();

            foreach ( var groupType in ExportGroupTypes )
            {
                GroupTypesCheckboxItems.Add( new CheckListItem { Id = groupType.Id, Text = groupType.Name, Checked = true } );
            }

            cblGroupTypes.ItemsSource = GroupTypesCheckboxItems;

            txtImportCutOff.Text = new DateTime( 1998, 1, 1 ).ToShortDateString();
        }

        /// <summary>
        /// Handles the Click event of the btnDownloadPackage control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void btnDownloadPackage_Click( object sender, RoutedEventArgs e )
        {
            btnDownloadPackage.IsEnabled = false;

            // Set CcbApi.DumpResponseToXmlFile to true to save all API Responses to XML files and include them in the slingshot package
            CcbApi.DumpResponseToXmlFile = cbDumpResponseToXmlFile.IsChecked ?? false;

            // Set ConsolidateScheduleNames to true to consolidate schedules names as 'Sunday at 11:00 AM'
            CcbApi.ConsolidateScheduleNames = cbConsolidateSchedules.IsChecked ?? false;

            // Set ExportDepartmentDirectorsAsGroups to true to export Directors as groups
            CcbApi.ExportDirectorsAsGroups = cbDirectorsAsGroups.IsChecked ?? false;

            // Reset API Request Counter.
            CcbApi.ApiRequestCount = 0;

            // Set ItemsPerPage value.
            if ( InputIsValidInteger( txtItemsPerPage.Text, 1, 10000 ) )
            {
                CcbApi.ItemsPerPage = txtItemsPerPage.Text.AsInteger();
            }
            
            // Set ApiThrottleRate value.
            if ( InputIsValidInteger( txtThrottleRate.Text, 0, 14 ) )
            {
                CcbApi.ApiThrottleRate = txtThrottleRate.Text.AsInteger();
            }

            // clear result from previous export
            txtExportMessage.Text = string.Empty;
            txtMessages.Text = string.Empty;
            txtMessages.Visibility = Visibility.Collapsed;
            txtError.Visibility = Visibility.Collapsed;
            _errorHasOccurred = false;

            // create ExportSettings object for background worker.
            var exportSettings = new ExportSettings
            {
                ModifiedSince = txtImportCutOff.Text.AsDateTime(),
                ExportContributions = cbContributions.IsChecked.Value,
                ExportIndividuals = cbIndividuals.IsChecked.Value,
                ExportAttendance = cbAttendance.IsChecked.Value
            };

            // configure group types to export
            if ( cbGroups.IsChecked == true )
            {
                var selectedGroupTypes = GroupTypesCheckboxItems.Where( i => i.Checked ).Select( i => i.Id );
                exportSettings.ExportGroupTypes.AddRange( selectedGroupTypes );
            }

            // launch our background export
            exportWorker.RunWorkerAsync( exportSettings );
        }

        /// <summary>
        /// Handles the Checked event of the cbGroups control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
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

        /// <summary>
        /// Handles the PreviewTextInput event of the txtItemsPerPage control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="TextCompositionEventArgs"/> instance containing the event data.</param>
        private void txtItemsPerPage_PreviewTextInput( object sender, TextCompositionEventArgs e )
        {
            var textBox = sender as TextBox;
            string currentText = textBox.Text.Remove( textBox.SelectionStart, textBox.SelectionLength );
            e.Handled = !InputIsValidInteger( currentText + e.Text, 1, 10000 );
        }

        /// <summary>
        /// Handles the PreviewTextInput event of the txtThrottleRate control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="TextCompositionEventArgs"/> instance containing the event data.</param>
        private void txtThrottleRate_PreviewTextInput( object sender, TextCompositionEventArgs e )
        {
            var textBox = sender as TextBox;
            string currentText = textBox.Text.Remove( textBox.SelectionStart, textBox.SelectionLength );
            e.Handled = !InputIsValidInteger( currentText + e.Text, 0, 14 );
        }

        /// <summary>
        /// Validation function for TextBox inputs, ensures that input is an integer within a certain range.
        /// </summary>
        /// <param name="input">The input text.</param>
        /// <param name="minValue">The minimum permitted value (inclusive).</param>
        /// <param name="maxValue">The maximum permitted value (inclusive).</param>
        /// <returns></returns>
        public static bool InputIsValidInteger( string input, int minValue, int maxValue )
        {
            if ( input.IsNullOrWhiteSpace() )
            {
                return false;
            }

            int? i = input.AsIntegerOrNull();
            bool isInteger = ( i != null );
            bool isGreaterThanMin = i.HasValue && ( i.Value >= minValue );
            bool isLowerThanMax = i.HasValue && ( i <= maxValue );
            return isInteger && isGreaterThanMin && isLowerThanMax;
        }

        #endregion

        #region Helper Classes

        /// <summary>
        /// This class holds settings (populated from window controls) which are passed to the worker
        /// process to control the export.
        /// </summary>
        private class ExportSettings
        {
            /// <summary>
            /// The "Modified Since" date passed to the CCP API.
            /// </summary>
            public DateTime? ModifiedSince { get; set; }

            /// <summary>
            /// Indicates whether or not to request individual records from the CCB API.
            /// </summary>
            public bool ExportIndividuals { get; set; } = true;

            /// <summary>
            /// Indicates whether or not to request contribution records from the CCB API.
            /// </summary>
            public bool ExportContributions { get; set; } = true;

            /// <summary>
            /// Indicates whether or not to request attendance records from the CCB API.
            /// </summary>
            public bool ExportAttendance { get; set; } = true;

            /// <summary>
            /// A list of group types that indicates which group records to export from the CCB API.
            /// </summary>
            public List<int> ExportGroupTypes { get; set; } = new List<int>();
        }

        /// <summary>
        /// This class is used to track group checkbox items.
        /// </summary>
        private class CheckListItem
        {
            public int Id { get; set; }
            public string Text { get; set; }
            public bool Checked { get; set; }
        }

        #endregion

    }

}
