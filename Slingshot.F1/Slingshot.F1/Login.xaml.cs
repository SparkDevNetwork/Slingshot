using System;
using System.Collections.Generic;
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
using System.Windows.Forms;

using Slingshot.F1.Utilities;

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

          
            gbMDFUpload.Visibility = rbMDF.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Handles the Click event of the btnLogin control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void btnLogin_Click( object sender, RoutedEventArgs e )
        {
            lblMessage.Text = string.Empty;

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
                    lblMessage.Text = $"Could not login with the information provided. {F1Api.ErrorMessage}";
                }
            }
            else
            {
                lblMessage.Text = "Please provide the information needed to connect.";
            }
        }

        private void btnUpload_Click( object sender, RoutedEventArgs e )
        {
            var fileDialog = new System.Windows.Forms.OpenFileDialog();
            var result = fileDialog.ShowDialog();

            string fileName = null;

            if( result == System.Windows.Forms.DialogResult.OK )
            {
                lblMessage.Text = string.Empty;

                fileName = fileDialog.FileName;

                if ( fileName != string.Empty && fileName.Contains( ".mdb" ) )
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
                        lblMessage.Text = $"Could not open the MDB database file. {F1Mdb.ErrorMessage}";
                    }
                }
                else
                {
                    lblMessage.Text = "Please choose a MDB database file.";
                }
            }
        }

       private void rbImportType_CheckedChanged ( object sender, EventArgs e )
       {
            if ( gbAPILogin != null && gbMDFUpload != null )
            {
                if ( rbAPI.IsChecked.Value )
                {
                    gbAPILogin.Visibility = Visibility.Visible;
                    gbMDFUpload.Visibility = Visibility.Collapsed;
                }
                else if ( rbMDF.IsChecked.Value )
                {
                    gbAPILogin.Visibility = Visibility.Collapsed;
                    gbMDFUpload.Visibility = Visibility.Visible;
                }
            }
       }
        }
}