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
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Slingshot.CCB.Utilities;
using Slingshot.Core;
using Slingshot.Core.Model;
using Slingshot.Core.Utilities;

namespace Slingshot.CCB
{
    /// <summary>
    /// Interaction logic for Main.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        System.Windows.Threading.DispatcherTimer _apiUpdateTimer = new System.Windows.Threading.DispatcherTimer();

        private readonly BackgroundWorker exportWorker = new BackgroundWorker();

        public List<GroupType> ExportGroupTypes { get; set; }
        public List<CheckListItem> GroupTypesCheckboxItems { get; set; } = new List<CheckListItem>();

        public MainWindow()
        {
            InitializeComponent();

            _apiUpdateTimer.Tick += _apiUpdateTimer_Tick; ;
            _apiUpdateTimer.Interval = new TimeSpan( 0, 2, 30 );
            
            // Set CcbApi.DumpResponseToXmlFile to true to save all API Responses to XML files and include them in the slingshot package
            CcbApi.DumpResponseToXmlFile = cbDumpResponseToXmlFile.IsChecked ?? false;

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

            var exportSettings = (ExportSettings)e.Argument;

            // clear filesystem directories
            CcbApi.InitializeExport();

            

            // export individuals
            if ( exportSettings.ExportIndividuals )
            {
                exportWorker.ReportProgress( 1, "Exporting Individuals..." );
                CcbApi.ExportIndividuals( exportSettings.ModifiedSince );

                if ( CcbApi.ErrorMessage.IsNotNullOrWhitespace() )
                {
                    this.Dispatcher.Invoke( () =>
                    {
                        exportWorker.ReportProgress( 2, $"Error exporting individuals: {CcbApi.ErrorMessage}" );
                    } );
                }
            }

            // export contributions
            if ( exportSettings.ExportContributions )
            {
                exportWorker.ReportProgress( 30, "Exporting Financial Accounts..." );

                CcbApi.ExportFinancialAccounts();
                if ( CcbApi.ErrorMessage.IsNotNullOrWhitespace() )
                {
                    exportWorker.ReportProgress( 31, $"Error exporting financial accounts: {CcbApi.ErrorMessage}" );
                }

                exportWorker.ReportProgress( 35, "Exporting Contribution Information..." );

                CcbApi.ExportContributions( exportSettings.ModifiedSince );
                if ( CcbApi.ErrorMessage.IsNotNullOrWhitespace() )
                {
                    exportWorker.ReportProgress( 36, $"Error exporting financial batches: {CcbApi.ErrorMessage}" );
                }
            }

            // export group types
            if ( ExportGroupTypes.Count > 0 )
            {
                exportWorker.ReportProgress( 54, $"Exporting Groups..." );

                CcbApi.ExportGroups( ExportGroupTypes.Select( t => t.Id ).ToList(), exportSettings.ModifiedSince );

                if ( CcbApi.ErrorMessage.IsNotNullOrWhitespace() )
                {
                    exportWorker.ReportProgress( 54, $"Error exporting groups: {CcbApi.ErrorMessage}" );
                }
            }

            // export attendance 
            if ( exportSettings.ExportAttendance )
            {
                exportWorker.ReportProgress( 75, $"Exporting Attendance..." );

                CcbApi.ExportAttendance( exportSettings.ModifiedSince );


                if ( CcbApi.ErrorMessage.IsNotNullOrWhitespace() )
                {
                    exportWorker.ReportProgress( 75, $"Error exporting attendance: {CcbApi.ErrorMessage}" );
                }
            }

            // finalize the package
            ImportPackage.FinalizePackage( "ccb-export.slingshot" );

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
            lblApiUsage.Text = $"API Usage: {CcbApi.Counter} / {CcbApi.DailyLimit}";

            // add group types
            ExportGroupTypes = CcbApi.GetGroupTypes();

            foreach ( var groupType in ExportGroupTypes ) {
                //cblGroupTypes.Items.Add( groupType );
                GroupTypesCheckboxItems.Add( new CheckListItem { Id = groupType.Id, Text = groupType.Name, Checked = true } );
            }

            cblGroupTypes.ItemsSource = GroupTypesCheckboxItems;
            
            txtImportCutOff.Text = DateTime.Now.ToShortDateString(); // remove before flight (sets today's date as the modified since)
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
                ModifiedSince = (DateTime)txtImportCutOff.Text.AsDateTime(),
                ExportContributions = cbContributions.IsChecked.Value,
                ExportIndividuals = cbIndividuals.IsChecked.Value,
                ExportAttendance = cbAttendance.IsChecked.Value
            };

            // configure group types to export
            foreach ( var selectedItem in GroupTypesCheckboxItems.Where( i => i.Checked ))
            {
                exportSettings.ExportGroupTypes.Add( selectedItem.Id );
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

        public bool ExportAttendance { get; set; } = true;

        public List<int> ExportGroupTypes { get; set; } = new List<int>();
    }

    public class CheckListItem
    {
        public int Id { get; set; }
        public string Text { get; set; }

        public bool Checked { get; set; }
    }
}
