using Slingshot.CCB.Utilities;
using System.Windows;
using System.Windows.Input;

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
            txtHostname.Focus();
        }

        private void btnLogin_Click( object sender, RoutedEventArgs e )
        {
            ProcessLogin();
        }
        private void ProcessLogin()
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

        private void TxtHostname_KeyDown( object sender, KeyEventArgs e )
        {
            ProcessKeyDownEvent( e );
        }
        private void TxtApiUsername_KeyDown( object sender, KeyEventArgs e )
        {
            ProcessKeyDownEvent( e );
        }
        private void TxtApiPassword_KeyDown( object sender, KeyEventArgs e )
        {
            ProcessKeyDownEvent( e );
        }

        private void ProcessKeyDownEvent( KeyEventArgs e )
        {
            if ( e.Key == Key.Enter )
            {
                ProcessLogin();
            }
            return;
        }

    }
}
