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

using Slingshot.F1.Utilities;
using Slingshot.Core;
using Slingshot.Core.Model;
using Slingshot.Core.Utilities;

namespace Slingshot.F1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //public Translator F1Translator;

        public bool DumpResponseToXmlFile = false;

        private System.Windows.Threading.DispatcherTimer _apiUpdateTimer = new System.Windows.Threading.DispatcherTimer();

        private readonly BackgroundWorker exportWorker = new BackgroundWorker();

        public List<GroupType> ExportGroupTypes { get; set; }
        public List<CheckListItem> GroupTypesCheckboxItems { get; set; } = new List<CheckListItem>();

        public F1Translator exporter { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            _apiUpdateTimer.Tick += _apiUpdateTimer_Tick;
            _apiUpdateTimer.Interval = new TimeSpan( 0, 0, 1 );

            // Set DumpResponseToXmlFile to true to save all Responses to XML files and include them in the slingshot package
            DumpResponseToXmlFile = cbDumpResponseToXmlFile.IsChecked ?? false;

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
            F1Api.InitializeExport();

            // export individuals
            if ( exportSettings.ExportIndividuals )
            {
                exportWorker.ReportProgress( 1, "Exporting Individuals..." );
                exporter.ExportIndividuals( exportSettings.ModifiedSince );

                if ( F1Api.ErrorMessage.IsNotNullOrWhitespace() )
                {
                    this.Dispatcher.Invoke( () =>
                    {
                        exportWorker.ReportProgress( 2, $"Error exporting individuals: {F1Api.ErrorMessage}" );
                    } );
                }
            }

            // export contributions
            if ( exportSettings.ExportContributions )
            {
                exportWorker.ReportProgress( 30, "Exporting Financial Accounts..." );

                exporter.ExportFinancialAccounts();
                if ( F1Api.ErrorMessage.IsNotNullOrWhitespace() )
                {
                    exportWorker.ReportProgress( 31, $"Error exporting financial accounts: {F1Api.ErrorMessage}" );
                }

                exportWorker.ReportProgress( 32, "Exporting Financial Pledges..." );

                exporter.ExportFinancialPledges();
                if ( F1Api.ErrorMessage.IsNotNullOrWhitespace() )
                {
                    exportWorker.ReportProgress( 33, $"Error exporting financial pledges: {F1Api.ErrorMessage}" );
                }

                exportWorker.ReportProgress( 34, "Exporting Financial Batches..." );

                exporter.ExportFinancialBatches( exportSettings.ModifiedSince );
                if ( F1Api.ErrorMessage.IsNotNullOrWhitespace() )
                {
                    exportWorker.ReportProgress( 35, $"Error exporting financial batches: {F1Api.ErrorMessage}" );
                }

                exportWorker.ReportProgress( 36, "Exporting Contribution Information..." );

                exporter.ExportContributions( exportSettings.ModifiedSince, exportSettings.ExportContributionImages );
                if ( F1Api.ErrorMessage.IsNotNullOrWhitespace() )
                {
                    exportWorker.ReportProgress( 37, $"Error exporting financial batches: {F1Api.ErrorMessage}" );
                }
            }

            // export group types
            if ( exportSettings.ExportGroupTypes.Count > 0 )
            {
                exportWorker.ReportProgress( 53, $"Exporting Groups..." );

                exporter.ExportGroups( ExportGroupTypes.Select( t => t.Id ).ToList() );

                if ( F1Api.ErrorMessage.IsNotNullOrWhitespace() )
                {
                    exportWorker.ReportProgress( 54, $"Error exporting groups: {F1Api.ErrorMessage}" );
                }
            }

            // export attendance
            if ( exportSettings.ExportAttendance )
            {
                exportWorker.ReportProgress( 61, "Exporting Attendance..." );
                exporter.ExportAttendance( exportSettings.ModifiedSince );

                if ( F1Api.ErrorMessage.IsNotNullOrWhitespace() )
                {
                    this.Dispatcher.Invoke( () =>
                    {
                        exportWorker.ReportProgress( 68, $"Error exporting attendance: {F1Api.ErrorMessage}" );
                    } );
                }
            }

            // export Businesses
            if ( exportSettings.ExportBusinesses )
            {
                exportWorker.ReportProgress( 71, "Exporting Businesses..." );
                exporter.ExportBusinesses( exportSettings.ModifiedSince );

                if ( F1Api.ErrorMessage.IsNotNullOrWhitespace() )
                {
                    this.Dispatcher.Invoke( () =>
                    {
                        exportWorker.ReportProgress( 72, $"Error exporting businesses: {F1Api.ErrorMessage}" );
                    } );
                }
            }


            // finalize the package
            ImportPackage.FinalizePackage( "f1-export.slingshot" );

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
            lblApiUsage.Text = $"API Usage: {F1Api.ApiCounter}";
        }

        /// <summary>
        /// Handles the Loaded event of the Window control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void Window_Loaded( object sender, RoutedEventArgs e )
        {
            lblApiUsage.Text = $"API Usage: {F1Api.ApiCounter}";
            // add group types
            ExportGroupTypes = exporter.GetGroupTypes();

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
            // launch our background export
            var exportSettings = new ExportSettings
            {
                ModifiedSince = (DateTime)txtImportCutOff.Text.AsDateTime(),
                ExportContributions = cbContributions.IsChecked.Value,
                ExportIndividuals = cbIndividuals.IsChecked.Value,
                ExportContributionImages = cbExportContribImages.IsChecked.Value,
                ExportAttendance = cbAttendance.IsChecked.Value, 
                ExportBusinesses = cbBusinesses.IsChecked.Value
            };

            // configure group types to export
            if ( cbGroups.IsChecked.Value == true )
            {
                foreach ( var selectedItem in GroupTypesCheckboxItems.Where( i => i.Checked ) )
                {
                    exportSettings.ExportGroupTypes.Add( selectedItem.Id );
                }
            }

            F1Api.DumpResponseToXmlFile = cbDumpResponseToXmlFile.IsChecked ?? false;
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

        private void cbContributions_Checked( object sender, RoutedEventArgs e )
        {
            if ( cbContributions.IsChecked.Value )
            {
                gridMain.RowDefinitions[6].Height = new GridLength( 1, GridUnitType.Auto );
            }
            else
            {
                gridMain.RowDefinitions[6].Height = new GridLength( 0 );
            }
        }

        #endregion

        private void cbAttendance_Checked( object sender, RoutedEventArgs e )
        {
        }

        private void cbBusinesses_Checked( object sender, RoutedEventArgs e )
        {

        }
    }

    public class ExportSettings
    {
        public DateTime ModifiedSince { get; set; } = DateTime.Now;

        public bool ExportIndividuals { get; set; } = true;

        public bool ExportContributions { get; set; } = true;

        public List<int> ExportGroupTypes { get; set; } = new List<int>();

        public bool ExportContributionImages { get; set; } = true;

        public bool ExportAttendance { get; set; } = true;

        public bool ExportBusinesses { get; set; } = true;
    }

    public class CheckListItem
    {
        public int Id { get; set; }

        public string Text { get; set; }

        public bool Checked { get; set; }
    }
}