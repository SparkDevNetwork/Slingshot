using Slingshot.PCO.Utilities;
using System.Windows;

namespace Slingshot.PCO
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

            if ( txtApiConsumerKey.Text != string.Empty && txtApiConsumerSecret.Text != string.Empty )
            {
                PCOApi.Connect( txtApiConsumerKey.Text, txtApiConsumerSecret.Text );

                if ( PCOApi.IsConnected )
                {
                    MainWindow mainWindow = new MainWindow();
                    mainWindow.Show();
                    this.Close();
                }
                else
                {
                    lblMessage.Text = $"Could not login with the information provided. {PCOApi.ErrorMessage}";
                }
            }
            else
            {
                lblMessage.Text = "Please provide the information needed to connect.";
            }
        }
    }
}
