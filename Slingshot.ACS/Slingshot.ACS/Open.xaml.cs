using System;
using System.Collections.Generic;
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

using Slingshot.ACS.Utilities;

namespace Slingshot.ACS
{
    /// <summary>
    /// Interaction logic for Open.xaml
    /// </summary>
    public partial class Open : Window
    {
        public Open()
        {
            InitializeComponent();
        }

        private void btnOpen_Click( object sender, RoutedEventArgs e )
        {
            lblMessage.Text = string.Empty;

            if ( txtFilename.Text != string.Empty && txtFilename.Text.Contains( ".mdb" ) )
            {
                AcsApi.OpenConnection( txtFilename.Text );

                if ( AcsApi.IsConnected )
                {
                    MainWindow mainWindow = new MainWindow();
                    mainWindow.Show();
                    this.Close();
                }
                else
                {
                    lblMessage.Text = $"Could not open the MS Access database file. {AcsApi.ErrorMessage}";
                }
            }
            else
            {
                lblMessage.Text = "Please choose a MS Access database file.";
            }
        }

        private void Browse_Click( object sender, RoutedEventArgs e )
        {
            var fileDialog = new System.Windows.Forms.OpenFileDialog();
            var result = fileDialog.ShowDialog();

            switch ( result )
            {
                case System.Windows.Forms.DialogResult.OK:
                    var file = fileDialog.FileName;
                    txtFilename.Text = file;
                    txtFilename.ToolTip = file;
                    break;
                case System.Windows.Forms.DialogResult.Cancel:
                default:
                    txtFilename.Text = null;
                    txtFilename.ToolTip = null;
                    break;
            }
        }
    }
}
