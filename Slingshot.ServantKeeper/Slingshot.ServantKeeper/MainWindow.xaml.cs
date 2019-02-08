using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

using Slingshot.Core;
using Slingshot.Core.Utilities;

using Slingshot.ServantKeeper.Utilities;

namespace Slingshot.ServantKeeper
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
            _apiUpdateTimer.Interval = new TimeSpan(0, 2, 30);

            exportWorker.DoWork += ExportWorker_DoWork;
            exportWorker.RunWorkerCompleted += ExportWorker_RunWorkerCompleted;
            exportWorker.ProgressChanged += ExportWorker_ProgressChanged;
            exportWorker.WorkerReportsProgress = true;
        }

        #region Background Worker Events
        private void ExportWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            txtExportMessage.Text = e.UserState.ToString();
            pbProgress.Value = e.ProgressPercentage;
        }

        private void ExportWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            txtExportMessage.Text = "Export Complete";
            pbProgress.Value = 100;
        }

        private void ExportWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            exportWorker.ReportProgress(0, "");

            var exportSettings = (ExportSettings)e.Argument;

            // clear filesystem directories
            ServanrKeeperApi.InitializeExport(exportSettings.ModifiedSince, exportSettings.NoContributionsBefore);

            // export individuals and phone numbers
            if (exportSettings.ExportIndividuals)
            {
                exportWorker.ReportProgress(1, "Exporting Individuals...");
                ServanrKeeperApi.ExportIndividuals();

                if (ServanrKeeperApi.ErrorMessage.IsNotNullOrWhitespace())
                {
                    txtMessages.Text = $"Error exporting individuals: {ServanrKeeperApi.ErrorMessage}";
                }
            }

            // export contributions
            if (exportSettings.ExportContributions)
            {
                exportWorker.ReportProgress(32, "Exporting Contributions...");

                ServanrKeeperApi.ExportContributions();
                if (ServanrKeeperApi.ErrorMessage.IsNotNullOrWhitespace())
                {
                    exportWorker.ReportProgress(33, $"Error exporting contributions: {ServanrKeeperApi.ErrorMessage}");
                }
            }

            // export groups
            if (exportSettings.ExportGroups)
            {
                exportWorker.ReportProgress(54, $"Exporting Groups...");

                ServanrKeeperApi.ExportGroups(exportSettings.SelectedGroups);

                if (ServanrKeeperApi.ErrorMessage.IsNotNullOrWhitespace())
                {
                    exportWorker.ReportProgress(54, $"Error exporting groups: {ServanrKeeperApi.ErrorMessage}");
                }
            }

            // finalize the package
            ImportPackage.FinalizePackage("servantkeeper.slingshot");

            // schedule the API status to update (the status takes a few mins to update)
            _apiUpdateTimer.Start();
        }

        #endregion

        /// <summary>
        /// Handles the Tick event of the _apiUpdateTimer control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void _apiUpdateTimer_Tick(object sender, EventArgs e)
        {
            // update the api stats
            _apiUpdateTimer.Stop();
        }

        /// <summary>
        /// Handles the Loaded event of the Window control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Retrieve the Group Names from the database
            Dictionary<int,string> AllGroups = ServanrKeeperApi.GetAllGroups();

            // Populate Groups listbox control with available group names
            if (AllGroups.Any())
            {
                foreach (var group in AllGroups)
                    GroupsListBox.Items.Add(new ListBoxItem() { Content = group.Value, Uid = group.Key.ToString() });
            }

            // Initialize values
            txtImportCutOff.Text = "1/1/1900";
            txtContributionsCutOff.Text = "1/1/2017";
            Groups_Checked(null, null);
        }

        /// <summary>
        /// Handles the Click event of the btnDownloadPackage control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void DownloadPackage_Click(object sender, RoutedEventArgs e)
        {
            // Contruct the settings to be passed to the export process based on user selections
            var Settings = new ExportSettings
            {
                ModifiedSince = txtImportCutOff.Text.Length > 0 ? (DateTime)txtImportCutOff.Text.AsDateTime() : DateTime.Parse("1/1/1900"),
                NoContributionsBefore = txtContributionsCutOff.Text.Length > 0 ? (DateTime)txtContributionsCutOff.Text.AsDateTime() : DateTime.Parse("1/1/1900"),
                ExportIndividuals = cbIndividuals.IsChecked.Value,
                ExportGroups = cbGroups.IsChecked.Value,
                ExportContributions = cbContributions.IsChecked.Value,
                SelectedGroups = ""
            };

            // Retrieve the IDs of the Group export selections
            foreach (ListBoxItem item in GroupsListBox.SelectedItems)
               Settings.SelectedGroups += Settings.SelectedGroups.Length > 0 ? ", " + item.Uid : item.Uid;

            // Launch the background process that actually does the export
            exportWorker.RunWorkerAsync(Settings);
        }

        #region Windows Events

        // Expand the Groups to Export section when Groups are selected
        private void Groups_Checked(object sender, RoutedEventArgs e)
        {
            if (cbGroups.IsChecked.Value)
                gridMain.RowDefinitions[5].Height = new GridLength(1, GridUnitType.Auto);
            else
                gridMain.RowDefinitions[5].Height = new GridLength(0);
        }
        #endregion Windows Events
    }

    public class ExportSettings
    {
        public DateTime ModifiedSince { get; set; }
        public DateTime NoContributionsBefore { get; set; }
        public bool ExportIndividuals { get; set; } = true;
        public bool ExportContributions { get; set; } = false;
        public bool ExportGroups { get; set; } = false;
        public string SelectedGroups { get; set; }
    }

    public class ComboBoxItem
    {
        public string Text { get; set; }
    }
}