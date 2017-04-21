using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace Xenon.Nexon
{
    static class Maple
    {
        public static string MaplePath = Path.Combine(Util.AppDirectory, "MapleStory.exe");

        public static bool GameExists
            => File.Exists(MaplePath);

        public static bool GameRunning
            => Process.GetProcessesByName("MapleStory").Length > 0;

        private static string launchToken;

        private static Regex remoteVerRegex = new Regex(@"pub(\d+)_(\d+)_(\d+)\.manifest");
        public static async Task CheckMapleUpToDate()
        {
            string localVer = getLocalMapleVersion();
            string remoteVer;

            // get remote version. if fail, well just prompt to continue or not.
            try
            {
                remoteVer = await getRemoteMapleVersion();
            }
            catch (Exception e)
            {
                MessageBoxResult mbr = MessageBox.Show($"Couldn't get latest MapleStory version data. Error:\n\n{e.ToString()}\n\nContinue anyway?", "Xenon", MessageBoxButton.YesNo);
                if (mbr == MessageBoxResult.No)
                    throw new SilentException();
                remoteVer = localVer;
            }

            if (localVer != remoteVer)
            {
                MessageBox.Show($"Your copy of MapleStory does not seem to be up to date. (You have {localVer}, and the latest seems to be {remoteVer}. Please use Nexon Launcher to update MapleStory to the latest version before continuing.", "Xenon");
                throw new SilentException();
            }
        }

        private static string getLocalMapleVersion()
        {
            // minor: 184, build: 2, private: 0 ==> v184.2.0 maple.
            FileVersionInfo localVerData = FileVersionInfo.GetVersionInfo(MaplePath);
            return $"{localVerData.ProductMinorPart}.{localVerData.ProductBuildPart}.{localVerData.ProductPrivatePart}";
        }

        private static async Task<string> getRemoteMapleVersion()
        {
            var req = new HttpRequestMessage()
            {
                RequestUri = new Uri("https://api.nexon.io/products/10100"),
                Method = HttpMethod.Get
            };
            Auth.AddNexonLauncherData(req); // bearer auth + UA

            HttpResponseMessage res = await Auth.Client.SendAsync(req);
            dynamic json = await Auth.ParseResponseJson(res);

            // parse version
            string remoteData = json["product_details"]["branches"]["win32"]["public"];
            Match remoteVerData = remoteVerRegex.Match(remoteData);

            return $"{remoteVerData.Groups[1].Value}.{remoteVerData.Groups[2].Value}.{remoteVerData.Groups[3].Value}";
        }

        public static async Task GetLaunchData()
        {
            // getting passport cookie to be able to get the launch token
            string passportToken = await getPassportToken();

            // getting final launch token.
            await getLaunchToken(passportToken);
        }

        private static async Task<string> getPassportToken()
        {
            var req = new HttpRequestMessage()
            {
                RequestUri = new Uri("https://api.nexon.io/users/me/passport"),
                Method = HttpMethod.Get
            };
            Auth.AddNexonLauncherData(req);

            HttpResponseMessage res = await Auth.Client.SendAsync(req);
            dynamic json = await Auth.ParseResponseJson(res);

            return json["passport"];
        }

        private static Regex errorRegex = new Regex(@"name=""ErrorCode"" value=""(\d+)""\/>.*name=""ErrorMessage"" value=""(.*?)""\/>");
        private static Regex tokenRegex = new Regex(@"name=""NewPassport"" value=""(.*?)""\/>");
        private static Regex authServerRegex = new Regex(@":auth(\d+):");
        private static async Task getLaunchToken(string passportToken)
        {
            // now it JUST uses cookies.
            string authServerNum = authServerRegex.Match(passportToken).Groups[1].Value;
            var req = new HttpRequestMessage()
            {
                RequestUri = new Uri($"http://auth{authServerNum}.nexon.net/ajax/default.aspx?_vb=UpdateSession"),
                Method = HttpMethod.Post,
                Content = new StringContent("", Encoding.UTF8, "application/x-www-form-urlencoded")
            };
            req.Headers.Add("User-Agent", "Python-urllib/2.7"); // and now it uses some python launch? idfk nexon.
            req.Headers.Add("Cookie", "NPPv2=" + passportToken);

            HttpResponseMessage res;
            using (var client = new HttpClient(new HttpClientHandler() { UseCookies = false }))
                res = await client.SendAsync(req); // we will handle cookies here seperately, almost like how they use another python launch.

            // ... and it uses xml, not json.
            string xml = await res.Content.ReadAsStringAsync();

            Match errorReg = errorRegex.Match(xml);
            if (errorReg.Success)
            {
                string errorCode = errorReg.Groups[1].Value;
                string errorMsg = errorReg.Groups[2].Value;

                if (errorCode != "0" || !String.IsNullOrEmpty(errorMsg))
                    throw new NexonError(errorCode, "Token error: " + errorMsg);
            }

            Match tokenReg = tokenRegex.Match(xml);
            if (!tokenReg.Success)
                throw new Exception("Could not get launch token!");

            launchToken = tokenReg.Groups[1].Value;
        }

        public static void launchGame()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = MaplePath,
                Arguments = $"WebStart {launchToken}",
                WorkingDirectory = Util.AppDirectory,
                UseShellExecute = !App.CompatFlag
            };

            Process.Start(startInfo);
        }
    }
}
