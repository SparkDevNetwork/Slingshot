using System.ComponentModel;
using System.Windows;
using Microsoft.Win32;

namespace Slingshot
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// <seealso cref="System.Windows.Window" />
    /// <seealso cref="System.Windows.Markup.IComponentConnector" />
    public partial class MainWindow : Window
    {
        private Importer _importer = null;

        /// <summary>
        /// Gets or sets the rock URL.
        /// </summary>
        /// <value>
        /// The rock URL.
        /// </value>
        public string RockUrl { get; set; }

        /// <summary>
        /// Gets or sets the name of the rock user.
        /// </summary>
        /// <value>
        /// The name of the rock user.
        /// </value>
        public string RockUserName { get; set; }

        /// <summary>
        /// Gets or sets the rock password.
        /// </summary>
        /// <value>
        /// The rock password.
        /// </value>
        public string RockPassword { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Handles the Click event of the btnSelectSlingshotFile control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void btnSelectSlingshotFile_Click( object sender, RoutedEventArgs e )
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.DefaultExt = ".dplx";
            dlg.Filter = "Slingshot Files (.slingshot)|*.slingshot";

            if ( dlg.ShowDialog() == true )
            {
                tbSlingshotFileName.Text = dlg.FileName;
            }
        }

        /// <summary>
        /// Handles the Click event of the btnGo control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void btnGo_Click( object sender, RoutedEventArgs e )
        {
            _importer = new Importer( tbSlingshotFileName.Text, this.RockUrl, this.RockUserName, this.RockPassword );

            btnGo.IsEnabled = false;

            BackgroundWorker backgroundWorker = new BackgroundWorker();
            backgroundWorker.WorkerReportsProgress = true;
            backgroundWorker.DoWork += _importer.BackgroundWorker_DoWork;
            backgroundWorker.RunWorkerCompleted += BackgroundWorker_RunWorkerCompleted;
            backgroundWorker.ProgressChanged += BackgroundWorker_ProgressChanged;
            backgroundWorker.RunWorkerAsync();
        }

        /// <summary>
        /// Handles the ProgressChanged event of the BackgroundWorker control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ProgressChangedEventArgs"/> instance containing the event data.</param>
        private void BackgroundWorker_ProgressChanged( object sender, ProgressChangedEventArgs e )
        {
            if ( e.UserState is string )
            {
                tbResults.Text = e.UserState.ToString();
            }
            else
            {
                var resultText = string.Empty;
                foreach ( var result in _importer.Results )
                {
                    resultText += $"\n\n{result.Key}\n\n{result.Value}";
                }

                tbResults.Text = resultText.Trim();
            }
        }

        /// <summary>
        /// Handles the RunWorkerCompleted event of the BackgroundWorker control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RunWorkerCompletedEventArgs"/> instance containing the event data.</param>
        private void BackgroundWorker_RunWorkerCompleted( object sender, RunWorkerCompletedEventArgs e )
        {
            if ( e.Error != null )
            {
                if ( e.Error is SlingshotEndpointNotFoundException )
                {
                    tbResults.Text += "\n\n"  + e.Error.Message;
                }
                else
                {
                    tbResults.Text += "\n\n" + e.Error.ToString() + "\n\n" + e.Error.StackTrace;
                }
            }

            btnGo.IsEnabled = true;
        }
    }
}
