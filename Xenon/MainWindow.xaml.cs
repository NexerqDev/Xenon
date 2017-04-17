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
using System.IO;
using System.Net.Http;
using System.Net;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Xenon
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static string AppDirectory = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "appdata");
        public static string MaplePath = Path.Combine(AppDirectory, "MapleStory.exe");

        private HttpClient httpClient;
        private CookieContainer cookieContainer = new CookieContainer();

        private string nexonToken;
        private string mapleLaunchToken;

        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            // for debugging - dont have to move to the folder or w/e
            AppDirectory = Microsoft.Win32.RegistryKey
                .OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, Microsoft.Win32.RegistryView.Registry32)
                .OpenSubKey(@"SOFTWARE\Wizet\MapleStory", false)
                .GetValue("ExecPath").ToString();
            MaplePath = Path.Combine(AppDirectory, "MapleStory.exe");
#endif

            //checkNexonLauncher();
            checkIfMapleExists();

            var httpHandler = new HttpClientHandler()
            {
                CookieContainer = cookieContainer,
                UseCookies = true
            };
            httpClient = new HttpClient(httpHandler);

            if (!String.IsNullOrEmpty(Properties.Settings.Default.username))
            {
                usernameTextBox.Text = Properties.Settings.Default.username;
                rememberUsernameCheckbox.IsChecked = true;
                passwordPasswordBox.Focus();
            }
            else
            {
                usernameTextBox.Focus();
            }
        }

        private void checkNexonLauncher()
        {
            if (Process.GetProcessesByName("nexon_runtime").Length < 1)
            {
                MessageBox.Show("The Nexon Launcher runtime does not seem to be running on your computer. Please ensure it is running and logged in to ANY Nexon account.", "Xenon", MessageBoxButton.OK, MessageBoxImage.Warning);
                Close();
            }
        }

        private void checkIfMapleExists()
        {
            if (!File.Exists(MaplePath))
            {
                MessageBox.Show("MapleStory.exe was not found. Are you sure you put Xenon into the MapleStory folder? Ensure it is NOT in the \"appdata\" folder that Nexon Launcher creates, but one step above that (by default it should be called \"maplestory\".", "Xenon", MessageBoxButton.OK, MessageBoxImage.Warning);
                Close();
            }
        }

        private bool supressErrorPop = false;
        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            usernameTextBox.IsEnabled = false;
            passwordPasswordBox.IsEnabled = false;
            playButton.IsEnabled = false;

#if !DEBUG
            try
            {
#endif
                statusLabel.Content = $"attempting login...";

                await performLogin(usernameTextBox.Text, passwordPasswordBox.Password);
                statusLabel.Content = $"logged in as {usernameTextBox.Text} - checking for updates...";

                await isMapleUpToDate();
                statusLabel.Content = $"maple seems to be up to date - getting launch data...";

                await getLaunchData();
                statusLabel.Content = $"got maple launch data - launching game!...";

                launchGame();
                statusLabel.Content = $"game launched! - thanks for using xenon!";

                if ((bool)rememberUsernameCheckbox.IsChecked)
                    Properties.Settings.Default.username = usernameTextBox.Text;
                else
                    Properties.Settings.Default.username = "";

                Properties.Settings.Default.Save();

                await Task.Delay(1500);
                Close();
#if !DEBUG
            }
            catch (Exception ex)
            {
                if (!supressErrorPop)
                    MessageBox.Show($"An error occurred...\n\n{ex.ToString()}", "Xenon", MessageBoxButton.OK, MessageBoxImage.Asterisk);

                supressErrorPop = false;
            }
            finally
            {
                statusLabel.Content = "";
                usernameTextBox.IsEnabled = true;
                passwordPasswordBox.IsEnabled = true;
                playButton.IsEnabled = true;
            }
#endif
        }

        private string encodeB64(string str) => Convert.ToBase64String(Encoding.UTF8.GetBytes(str));

        private async Task performLogin(string username, string password)
        {
            var req = new HttpRequestMessage()
            {
                RequestUri = new Uri("https://api.nexon.net/auth/login"),
                Method = HttpMethod.Post,
                Content = new StringContent($"{{\"allow_unverified\":true,\"user_id\":\"{username}\",\"user_pw\":\"{password}\"}}", Encoding.UTF8, "application/json")
            };
            req.Headers.Add("User-Agent", "NexonLauncher node-webkit/0.14.6 (Windows NT 10.0; WOW64) WebKit/537.36 (@c26c0312e940221c424c2730ef72be2c69ac1b67) nexon_client");
            req.Headers.Add("Authorization", "Basic " + encodeB64($"{username.Replace("@", "%40")}:{password}")); // nexon launcher does this, it uriencodes to %40 which is weird but meh

            HttpResponseMessage res = await httpClient.SendAsync(req);
            dynamic json = await parseResponseJson(res);

            nexonToken = json["token"];
            cookieContainer.Add(new Cookie("nxtk", nexonToken, "/", ".nexon.net")); // this cookie is used in all future requests
        }

        private Regex remoteVerRegex = new Regex(@"pub(\d+)_(\d+)_(\d+)\.manifest");
        private async Task isMapleUpToDate()
        {
            // minor: 184, build: 2, private: 0 ==> v184.2.0 maple.
            FileVersionInfo localVerData = FileVersionInfo.GetVersionInfo(MaplePath);
            string localVer = $"{localVerData.ProductMinorPart}.{localVerData.ProductBuildPart}.{localVerData.ProductPrivatePart}";

            // get remote version. if fail, well just prompt to continue or not.
            string remoteVer;
            try
            {
                var req = new HttpRequestMessage()
                {
                    RequestUri = new Uri("https://api.nexon.net/products/10100"),
                    Method = HttpMethod.Get
                };
                addNexonLauncherData(req); // bearer auth + UA

                HttpResponseMessage res = await httpClient.SendAsync(req);
                dynamic json = await parseResponseJson(res);

                // parse version
                string remoteData = json["product_details"]["branches"]["win32"]["public"];
                Match remoteVerData = remoteVerRegex.Match(remoteData);
                remoteVer = $"{remoteVerData.Groups[1].Value}.{remoteVerData.Groups[2].Value}.{remoteVerData.Groups[3].Value}";
            }
            catch (Exception e)
            {
                MessageBoxResult mbr = MessageBox.Show($"Couldn't get latest MapleStory version data. Error:\n\n{e.ToString()}\n\nContinue anyway?", "Xenon", MessageBoxButton.YesNo);
                if (mbr == MessageBoxResult.No)
                    returnNoError();

                return;
            }

            if (localVer != remoteVer)
            {
                MessageBox.Show($"Your copy of MapleStory does not seem to be up to date. (You have {localVer}, and the latest seems to be {remoteVer}. Please use Nexon Launcher to update MapleStory to the latest version before continuing.", "Xenon");
                returnNoError();
            }
        }

        private Regex errorRegex = new Regex(@"name=""ErrorCode"" value=""(\d+)""\/>.*name=""ErrorMessage"" value=""(.*?)""\/>");
        private Regex tokenRegex = new Regex(@"name=""NewPassport"" value=""(.*?)""\/>");
        private Regex authServerRegex = new Regex(@":auth(\d+):");
        private async Task getLaunchData()
        {
            // getting passport cookie to be able to get the launch token
            var req = new HttpRequestMessage()
            {
                RequestUri = new Uri("https://api.nexon.net/users/me/passport"),
                Method = HttpMethod.Get
            };
            addNexonLauncherData(req);

            HttpResponseMessage res = await httpClient.SendAsync(req);
            dynamic json = await parseResponseJson(res);
            string passportToken = json["passport"];


            // getting final launch token.
            // now it JUST uses cookies.
            string authServerNum = authServerRegex.Match(passportToken).Groups[1].Value;
            req = new HttpRequestMessage()
            {
                RequestUri = new Uri($"http://auth{authServerNum}.nexon.net/ajax/default.aspx?_vb=UpdateSession"),
                Method = HttpMethod.Post,
                Content = new StringContent("", Encoding.UTF8, "application/x-www-form-urlencoded")
            };
            req.Headers.Add("User-Agent", "Python-urllib/2.7"); // and now it uses some python launch? idfk nexon.
            req.Headers.Add("Cookie", "NPPv2=" + passportToken); // we will handle cookies here seperately, almost like how they use another python launch.

            using (var client = new HttpClient(new HttpClientHandler() { UseCookies = false }))
                res = await client.SendAsync(req);

            // ... and it uses xml, not json.
            string xml = await res.Content.ReadAsStringAsync();

            Match errorReg = errorRegex.Match(xml);
            if (errorReg.Success)
            {
                string errorCode = errorReg.Groups[1].Value;
                string errorMsg = errorReg.Groups[2].Value;

                if (errorCode != "0" || !String.IsNullOrEmpty(errorMsg))
                    throw new Exception($"Nexon launch token error: \n\n{errorMsg} [{errorCode}]");
            }

            Match tokenReg = tokenRegex.Match(xml);
            if (!tokenReg.Success)
                throw new Exception("Could not get launch token!");

            mapleLaunchToken = tokenReg.Groups[1].Value;
        }

        private void launchGame()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = MaplePath,
                Arguments = $"WebStart {mapleLaunchToken}",
                WorkingDirectory = AppDirectory
            };

            Process.Start(startInfo);
        }

        private async Task<dynamic> parseResponseJson(HttpResponseMessage res)
        {
            // include err checking here
            dynamic json = JObject.Parse(await res.Content.ReadAsStringAsync());
            if (json["error"] != null)
                throw new Exception($"Nexon error: \n\n{json["error"]["message"]} [{json["error"]["code"]}]");

            return json;
        }

        private void addNexonLauncherData(HttpRequestMessage req)
        {
            req.Headers.Add("User-Agent", "NexonLauncher.nxl-17.02.03-219-5e3143f");
            req.Headers.Add("Authorization", "bearer " + encodeB64(nexonToken)); // this + cookie is used for auth here.
        }

        private void returnNoError()
        {
            supressErrorPop = true;
            throw new Exception();
        }
    }
}
