using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using RestSharp;

namespace Slingshot
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

            // txtRockUrl.Text = "http://localhost:6229";
            // txtUsername.Text = "admin";
            // txtPassword.Password = "admin";
        }

        /// <summary>
        /// Handles the Click event of the btnLogin control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void btnLogin_Click( object sender, RoutedEventArgs e )
        {
            lblMessage.Text = string.Empty;

            if ( txtRockUrl.Text != string.Empty && txtUsername.Text != string.Empty && txtPassword.Password != string.Empty )
            {
                RestClient restClient = new RestClient( txtRockUrl.Text );

                restClient.CookieContainer = new System.Net.CookieContainer();

                RestRequest restLoginRequest = new RestRequest( Method.POST );
                restLoginRequest.RequestFormat = RestSharp.DataFormat.Json;
                restLoginRequest.Resource = "api/auth/login";
                var loginParameters = new
                {
                    UserName = txtUsername.Text,
                    Password = txtPassword.Password
                };

                restLoginRequest.AddBody( loginParameters );

                // start a background thread to Login since this could take a little while and we want a Wait cursor
                BackgroundWorker bw = new BackgroundWorker();
                bw.DoWork += delegate ( object s, DoWorkEventArgs ee )
                {
                    ee.Result = null;
                    var loginResponse = restClient.Post( restLoginRequest );

                    ee.Result = loginResponse;
                };

                // when the Background Worker is done with the Login, run this
                bw.RunWorkerCompleted += delegate ( object s, RunWorkerCompletedEventArgs ee )
                {
                    this.Cursor = null;
                    btnLogin.IsEnabled = true;
                    var loginResponse = ee.Result as IRestResponse;
                    if ( loginResponse.StatusCode == System.Net.HttpStatusCode.NoContent )
                    {
                        var mainWindow = new MainWindow()
                        {
                            RockUrl = txtRockUrl.Text,
                            RockUserName = txtUsername.Text,
                            RockPassword = txtPassword.Password
                        };

                        mainWindow.Show();
                        this.Close();
                    }
                    else
                    {
                        lblMessage.Text = $"Could not login with the information provided. {loginResponse.ErrorMessage}";
                    }
                };

                this.Cursor = Cursors.Wait;
                btnLogin.IsEnabled = false;
                bw.RunWorkerAsync();
            }
            else
            {
                lblMessage.Text = "Please provide the information needed to connect.";
            }
        }

        /// <summary>
        /// Handles the Loaded event of the Window control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void Window_Loaded( object sender, RoutedEventArgs e )
        {
            // set keyboard focus to the first input that needs a value
            if ( string.IsNullOrEmpty( txtRockUrl.Text ) )
            {
                Keyboard.Focus( txtRockUrl );
            }
            else if ( string.IsNullOrEmpty( txtUsername.Text ) )
            {
                Keyboard.Focus( txtUsername );
            }
            else
            {
                Keyboard.Focus( txtPassword );
            }
        }
    }
}
