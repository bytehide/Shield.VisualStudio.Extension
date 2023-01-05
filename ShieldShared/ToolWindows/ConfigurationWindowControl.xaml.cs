using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using IdentityModel.OidcClient;
using IdentityModel.OidcClient.Browser;
using Shield.Client;
using ShieldVSExtension.Configuration;
using ShieldVSExtension.InternalSecureStorage;
using ShieldVSExtension.UI_Extensions;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using Microsoft.VisualBasic.Devices;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.Web.UI.WebControls;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace ShieldVSExtension.ToolWindows
{
    public partial class ConfigurationWindowControl : Window
    {
        private readonly ConfigurationViewModel _viewModel;
        private const string ExtensionConfigurationFile = "ExtensionConfiguration";

        private EnvDTE.OutputWindowPane Pane { get; set; }

        public SecureLocalStorage LocalStorage { get; set; }

        private ShieldExtensionConfiguration ExtensionConfiguration { get; }

        private void UpdateUserInfo()
        {
            ShieldUserName.Content = ExtensionConfiguration.Email;
            ShieldUserEdition.Content = string.Concat(ExtensionConfiguration.Edition[0].ToString().ToUpper(), ExtensionConfiguration.Edition.Substring(1));
        }

        // A sample assembly reference class that would exist in the `Core` project.
        public static class CoreAssembly
        {
            public static readonly Assembly Reference = typeof(CoreAssembly).Assembly;
            public static readonly Version Version = Reference.GetName().Version;
        }

        public ConfigurationWindowControl(ConfigurationViewModel viewModel)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            InitializeComponent();

            _viewModel = viewModel;
            DataContext = viewModel;

            LocalStorage = new SecureLocalStorage(
                new CustomLocalStorageConfig(null, "ByteHideShieldForVisualStudio").WithDefaultKeyBuilder()
            );

            ExtensionConfiguration = LocalStorage.Exists(ExtensionConfigurationFile) ?
                LocalStorage.Get<ShieldExtensionConfiguration>(ExtensionConfigurationFile) :
                new ShieldExtensionConfiguration();

            
            VersionLabel.Content = $"Version {CoreAssembly.Version}";

            if (!string.IsNullOrEmpty(ExtensionConfiguration.ApiToken))
                try
                {
                    _ = ShieldClient.CreateInstance(ExtensionConfiguration.ApiToken);
                    _viewModel.IsValidClient = true;

                    UpdateUserInfo();
                    //ApiKeyBox.Password = ExtensionConfiguration.ApiToken;
                    //ConnectButton.Content = ExtensionConfiguration.ApiToken != ApiKeyBox.Password ? "Connect and save" : "Retry connection";
                }
                catch (Exception)
                {
                    _viewModel.IsValidClient = false;
                }
            else _viewModel.IsValidClient = false;

            if (!_viewModel.IsValidClient)
                ShieldControl.SelectedIndex = 1;

            try
            {
                var dte = Services.Services.Dte2;
                if (dte == null) throw new ArgumentNullException(nameof(dte));
                Pane = dte.ToolWindows.OutputWindow.OutputWindowPanes.Add("ByteHide Auth");
            }
            catch (Exception)
            {

                throw;
            }

        }

        public static string GetRequestPostData(HttpListenerRequest request)
        {
            if (!request.HasEntityBody)
            {
                return null;
            }

            using (var body = request.InputStream)
            {
                using (var reader = new System.IO.StreamReader(body, request.ContentEncoding))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        public static int GetRandomUnusedPort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        const string clientID = "98254c09-fe05-4a5c-ae39-08c286721b10";

        const string authorizationEndpoint = "https://cloud.bytehide.com/auth";

        public static string randomDataBase64url(uint length)
        {
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            byte[] bytes = new byte[length];
            rng.GetBytes(bytes);
            return base64urlencodeNoPadding(bytes);
        }

        /// <summary>
        /// Returns the SHA256 hash of the input string.
        /// </summary>
        /// <param name="inputStirng"></param>
        /// <returns></returns>
        public static byte[] sha256(string inputStirng)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(inputStirng);
            SHA256Managed sha256 = new SHA256Managed();
            return sha256.ComputeHash(bytes);
        }

        /// <summary>
        /// Base64url no-padding encodes the given input buffer.
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static string base64urlencodeNoPadding(byte[] buffer)
        {
            string base64 = Convert.ToBase64String(buffer);

            // Converts base64 to base64url.
            base64 = base64.Replace("+", "-");
            base64 = base64.Replace("/", "_");
            // Strips padding.
            base64 = base64.Replace("=", "");

            return base64;
        }

        //private void ChangeLoginStatus(bool loading = true)
        //{
        //    ConnectButton.Visibility = Visibility.Hidden;
        //    ConnectButton.Content = "logging in...";
        //}

        private void WriteLine(string message)
        {
            try
            {
                Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
                Pane?.OutputString(message + Environment.NewLine);
                ActivePane();
            }
            catch 
            {

            }
          
        }

        private void ActivePane()
        {
            try
            {
                Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            Pane?.Activate();
            }
            catch 
            {

            }
        }

        internal class ApplicationResponse
        {
            public string token { get; set; }
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                WriteLine("Logging into your account...");

                // Generates state and PKCE values.
                string state = randomDataBase64url(32);
                string code_verifier = randomDataBase64url(32);
                string code_challenge = base64urlencodeNoPadding(sha256(code_verifier));
                const string code_challenge_method = "SHA256";

                // Creates a redirect URI using an available port on the loopback address.
                string redirectURI = string.Format("http://{0}:{1}/", IPAddress.Loopback, GetRandomUnusedPort());
               

                // Creates an HttpListener to listen for requests on that redirect URI.
                var http = new HttpListener();
                http.Prefixes.Add(redirectURI);
            
                http.Start();

                // Creates the OAuth 2.0 authorization request.
                string authorizationRequest = string.Format("{0}?redirect_uri={1}&client_id={2}&state={3}&code_challenge={4}&code_challenge_method={5}",
                    authorizationEndpoint,
                    Uri.EscapeDataString(redirectURI),
                    clientID,
                    state,
                    code_challenge,
                    code_challenge_method);
                
                // Opens request in the browser.
                ProcessStartInfo processStartInfo = new ProcessStartInfo(authorizationRequest);

                Process browser = new Process();
                browser.StartInfo = processStartInfo;

                if (!browser.Start())
                {
                    System.Windows.MessageBox.Show("Failed to open the browser, see the visual studio console called \"ByteHide Auth\" and open the given url in your browser.", "Invalid Shield API Key", MessageBoxButton.OK, MessageBoxImage.Error);

                    WriteLine("[WARN] Failed to start the browser.");
                    WriteLine("Manually open the following link in your browser:");
                    WriteLine(authorizationRequest);
                    WriteLine("From the url page authorizes access.");
                }

                WriteLine("Waiting for the request to be accepted...");

                // Waits for the OAuth authorization response.
                var context = await http.GetContextAsync();

                // Brings this app back to the foreground.
                this.Activate();

                // Sends an HTTP response to the browser.
                var response = context.Response;
                string responseString = string.Format("<html><head><meta http-equiv='refresh' content='10;url=https://bytehide.com'></head><body>Please return to the visual studio extension.</body></html>");
                var buffer = Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                var responseOutput = response.OutputStream;
                Task responseTask = responseOutput.WriteAsync(buffer, 0, buffer.Length).ContinueWith((task) =>
                {
                    responseOutput.Close();
                    http.Stop();
                    WriteLine("Request response received.");
                });

                var status = context.Request.QueryString.Get("status");

                // Checks for errors.
                if (status != null && status == "revoked")
                {
                    WriteLine("The request was not accepted. Cancelling.");
                    return;
                }

                if (context.Request.QueryString.Get("auth") == null
                    || context.Request.QueryString.Get("state") == null
                     || context.Request.QueryString.Get("endpoint") == null)
                {
                    WriteLine(string.Format("Malformed authorization response.: {0}.", context.Request.QueryString));
                    return;
                }

                // extracts the code
                var auth = context.Request.QueryString.Get("auth");
                var incoming_state = context.Request.QueryString.Get("state");

                // Compares the receieved state to the expected value, to ensure that
                // this app made the request which resulted in authorization.
                if (incoming_state != state)
                {
                    WriteLine(string.Format("Received request with invalid state ({0})", incoming_state));
                    return;
                }

                HttpClient httpClient = new HttpClient();
                HttpResponseMessage endpointResponse = await httpClient.GetAsync(string.Format("{0}&code={1}&scope[]={2}", context.Request.QueryString.Get("endpoint"), code_verifier, "shield"));
                
                string responseBody = await endpointResponse.Content.ReadAsStringAsync();

                if (!endpointResponse.IsSuccessStatusCode)
                {
                    WriteLine(string.Format("This application could not be authorized: ({0})", responseBody));
                    return;
                }

                var appResponse = JsonConvert.DeserializeObject<ApplicationResponse>(responseBody);

                var token = appResponse.token;


                var client = ShieldClient.CreateInstance(token);
                _viewModel.IsValidClient = true;
                ExtensionConfiguration.ApiToken = token;
                ShieldControl.SelectedIndex = 0;
               
                try
                {
                    var info = client.GetSession();
                    ExtensionConfiguration.Email = info.Email;
                    ExtensionConfiguration.Edition = info.Edition;
                } catch { }

                SaveExtensionConfiguration();

                UpdateUserInfo();

                WriteLine("Session started successfully.");

            }
            catch (Exception)
            {
                _viewModel.IsValidClient = false;
                ConnectButton.IsEnabled = true;
                ConnectButton.Content = "Sign in";
                System.Windows.MessageBox.Show("An error occurred while logging in or authorizing your account, contact support.","Invalid Auth",MessageBoxButton.OK,MessageBoxImage.Error);
            }
        }

        private void SaveExtensionConfiguration()
        => LocalStorage.Set(ExtensionConfigurationFile, ExtensionConfiguration);
        

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var removedItems = e.RemovedItems.OfType<ConfigurationViewModel.ProjectViewModel>();
            foreach (var item in removedItems)
                _viewModel.SelectedProjects.Remove(item);

            var addedItems = e.AddedItems.OfType<ConfigurationViewModel.ProjectViewModel>().Except(_viewModel.SelectedProjects);
            foreach (var item in addedItems)
                _viewModel.SelectedProjects.Add(item);
        }
    

        private void ListBox_Loaded(object sender, RoutedEventArgs e)
        {
            ((System.Windows.Controls.ListBox)sender).ScrollIntoView(_viewModel.SelectedProject);
        }

        private void EnableMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.Enable(true);
        }

        private void DisableMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.Enable(false);
        }

        private void AddCustomProtectionConfigMenuItem_Click(object sender, RoutedEventArgs e)
        {
            foreach (var viewModelSelectedProject in _viewModel.SelectedProjects)
            {
                viewModelSelectedProject.InheritFromProject = false;
                viewModelSelectedProject.ApplicationPreset = _viewModel.ProjectPresets.First(preset =>
                    preset.Name.ToLower().Equals(((System.Windows.Controls.MenuItem) sender).Header.ToString().ToLower()));
            }
        }

        private void OutputFilesComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            var comboBox = (System.Windows.Controls.ComboBox)sender;
            comboBox.Focus();

            var projectViewModel = _viewModel.SelectedProject;
            if (projectViewModel == null)
                return;

            var path = projectViewModel.OutputFullPath;

            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            {
                comboBox.ItemsSource = null;
                return;
            }

            var files = Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .OrderByDescending(p => p.StartsWith(projectViewModel.Name))
                .ThenBy(Path.GetFileNameWithoutExtension)
                .ToArray();

            comboBox.ItemsSource = files;
        }

        private void ApiKeyBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            //ConnectButton.Content = ExtensionConfiguration.ApiToken != ApiKeyBox.Password ? "Connect and save"  : "Retry connection";
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        { 
            _viewModel.Save();
            DialogResult = true;
            Close();
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void InheritConfigFromGlobal_Copy_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void ProtectionsPresetProject_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void GitHubButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/dotnetsafer/Shield.VSIX");
        }

        private void WebSiteButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://bytehide.com");
        }

        private void SignoutButton_Click(object sender, RoutedEventArgs e)
        {
            LocalStorage.Clear();
            _viewModel.IsValidClient = false;
        }

        private void DocumentationButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://docs.bytehide.com/platforms/dotnet/products/shield/vs-quick-start");
        }

        private void Generate_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start("https://docs.bytehide.com/platforms/dotnet/products/shield/vs-authentication");
      
        }

        private void ProtectionsPreset_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _viewModel.ShieldProjectEdition = (string)e.AddedItems[0];
        }

        private void ReadMore_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start("https://docs.bytehide.com/platforms/dotnet/products/shield");
        }
    }
}