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
using System.Windows.Navigation;
using System.Windows.Shapes;

using Slingshot.CCB.Utilities;

namespace Slingshot.CCB
{
    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class Login : Window
    {
        public Login()
        {
            InitializeComponent();
        }

        private void btnLogin_Click( object sender, RoutedEventArgs e )
        {
            lblMessage.Text = string.Empty;

            if ( txtHostname.Text != string.Empty && txtApiPassword.Text != string.Empty && txtApiUsername.Text != string.Empty )
            {
                CcbApi.Connect( txtHostname.Text, txtApiUsername.Text, txtApiPassword.Text );

                if ( CcbApi.IsConnected )
                {
                    MainWindow mainWindow = new MainWindow();
                    mainWindow.Show();
                    this.Close();
                }
                else
                {
                    lblMessage.Text = $"Could not login with the information provided. {CcbApi.ErrorMessage}";
                }
            }
            else
            {
                lblMessage.Text = "Please provide the information needed to connect.";
            }
        }
    }
}
