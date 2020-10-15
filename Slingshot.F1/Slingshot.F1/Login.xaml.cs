using Slingshot.F1.Utilities;
using System;
using System.ComponentModel;
using System.Windows;

namespace Slingshot.F1
{
    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class Login : Window
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="Login"/> class.
        /// </summary>
        public Login()
        {
            InitializeComponent();

            //gbMDBUpload.Visibility = rbMDB.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Handles the Click event of the btnLogin control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void btnLogin_Click( object sender, RoutedEventArgs e )
        {
            lblMessage_API.Text = string.Empty;

            if ( txtHostname.Text != string.Empty && txtApiPassword.Text != string.Empty && txtApiUsername.Text != string.Empty &&
                 txtApiConsumerKey.Text != string.Empty && txtApiConsumerSecret.Text != string.Empty )
            {
                F1Api.Connect( txtHostname.Text, txtApiConsumerKey.Text, txtApiConsumerSecret.Text, txtApiUsername.Text, txtApiPassword.Text );

                if ( F1Api.IsConnected )
                {
                    MainWindow mainWindow = new MainWindow();
                    mainWindow.exporter = new F1Api();
                    mainWindow.Show();
                    this.Close();
                }
                else
                {
                    lblMessage_API.Text = $"Could not login with the information provided. {F1Api.ErrorMessage}";
                }
            }
            else
            {
                lblMessage_API.Text = "Please provide the information needed to connect.";
            }
        }

        /// <summary>
        /// Handles the Click event of the btnFileUpload control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void btnFileUpload_Click( object sender, RoutedEventArgs e )
        {
            var fileDialog = new System.Windows.Forms.OpenFileDialog();
            var result = fileDialog.ShowDialog();

            string fileName = null;

            if( result == System.Windows.Forms.DialogResult.OK )
            {
                lblMessage_MDB.Text = string.Empty;

                fileName = fileDialog.FileName;

                bool isValidFileName = fileName.ToLower().Contains( ".mdb" ) ||
                    ( Environment.Is64BitProcess && fileName.ToLower().Contains( ".accdb" ) );

                if ( isValidFileName )
                {
                    F1Mdb.OpenConnection( fileName );

                    if ( F1Mdb.IsConnected )
                    {
                        MainWindow mainWindow = new MainWindow();
                        mainWindow.exporter = new F1Mdb();
                        mainWindow.Show();
                        this.Close();
                    }
                    else
                    {
                        lblMessage_MDB.Text = $"Could not open the MDB database file. {F1Mdb.ErrorMessage}";
                    }
                }
                else
                {
                    lblMessage_MDB.Text = "Please choose an Access database (MDB) file.";
                    if ( Environment.Is64BitProcess )
                    {
                        lblMessage_MDB.Text = "Please choose an Access database (MDB or ACCDB) file.";
                    }
                }
            }
        }

        /// <summary>
        /// Handles the Click event of the btnFileUpload control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void btnSqlFileUpload_Click( object sender, RoutedEventArgs e )
        {
            var fileDialog = new System.Windows.Forms.OpenFileDialog();
            var result = fileDialog.ShowDialog();

            string fileName = null;

            if( result == System.Windows.Forms.DialogResult.OK )
            {
                lblMessage_SQL.Text = string.Empty;

                fileName = fileDialog.FileName;

                bool isValidFileName = fileName.ToLower().Contains( ".mdf" );

                if ( isValidFileName )
                {
                    F1Sql.OpenConnection( fileName );

                    if ( F1Sql.IsConnected )
                    {
                        var exporter = new F1Sql();

                        BackgroundWorker exportWorker = new BackgroundWorker();

                        // Fetch group types now to avoid awkward delay when loading the next window.
                        exportWorker.DoWork += delegate ( object s2, DoWorkEventArgs e2 )
                        {
                            exporter.GetGroupTypes();
                        };

                        // Load the next window.
                        exportWorker.RunWorkerCompleted += delegate ( object s2, RunWorkerCompletedEventArgs e2 )
                        {
                            MainWindow mainWindow = new MainWindow();
                            mainWindow.exporter = exporter;
                            mainWindow.Show();
                            this.Close();
                        };

                        exportWorker.RunWorkerAsync();
                        lblMessage_SQL.Text = "Reading Group Types from MDF file, please wait.";
                        btnSqlFileUpload.IsEnabled = false;
                    }
                    else
                    {
                        lblMessage_SQL.Text = $"Could not open the SQL (MDF) database file. {F1Sql.ErrorMessage}";
                    }
                }
                else
                {
                    lblMessage_SQL.Text = "Please choose an SQL database (MDF) file.";
                }
            }
        }

        /// <summary>
        /// Handles the CheckedChanged event of the rbImportType control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void rbImportType_CheckedChanged ( object sender, EventArgs e )
        {
            if ( gbAPILogin != null && gbMDBUpload != null && gbSQLUpload != null )
            {
                if ( rbSQL.IsChecked.Value )
                {
                    gbSQLUpload.Visibility = Visibility.Visible;
                    gbAPILogin.Visibility = Visibility.Collapsed;
                    gbMDBUpload.Visibility = Visibility.Collapsed;
                }
                else if ( rbAPI.IsChecked.Value )
                {
                    gbSQLUpload.Visibility = Visibility.Collapsed;
                    gbAPILogin.Visibility = Visibility.Visible;
                    gbMDBUpload.Visibility = Visibility.Collapsed;
                }
                else if ( rbMDB.IsChecked.Value )
                {
                    gbSQLUpload.Visibility = Visibility.Collapsed;
                    gbAPILogin.Visibility = Visibility.Collapsed;
                    gbMDBUpload.Visibility = Visibility.Visible;
                }
            }
        }

    }
}