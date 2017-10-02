using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Net.Http;
using System.Net;
using Newtonsoft.Json.Linq;

namespace Xenon.Nexon
{
    public static class Auth
    {
        public const string CLIENT_ID = "7853644408";
        public const string SCOPE = "us.launcher.all";

        public static string DeviceId; // lets just make a random 64 string that we use everytime...

        public static HttpClient Client;
        public static CookieContainer ClientCookies = new CookieContainer();

        private static string accessToken;

        static Auth()
        {
            DeviceId = Properties.Settings.Default.nexonDeviceId;
            if (String.IsNullOrEmpty(DeviceId))
            {
                DeviceId = "";

                var random = new Random();
                for (int i = 0; i < 64; i++)
                    DeviceId += random.Next(16).ToString("X");

                DeviceId = DeviceId.ToLower();

                Properties.Settings.Default.nexonDeviceId = DeviceId;
                Properties.Settings.Default.Save();
            }

            Client = new HttpClient(new HttpClientHandler()
            {
                CookieContainer = ClientCookies,
                UseCookies = true
            });
        }

        public static string HashHexPassword(string password)
        {
            // hashes the password as sha-512 and hex as required by nexon
            // https://msdn.microsoft.com/en-us/library/s02tk69a(v=vs.110).aspx
            SHA512 shaM = new SHA512Managed();
            byte[] shaData = shaM.ComputeHash(Encoding.UTF8.GetBytes(password));

            var sb = new StringBuilder();
            for (int i = 0; i < shaData.Length; i++)
                sb.Append(shaData[i].ToString("x2")); // hex formatted

            return sb.ToString();
        }

        public static async Task Login(Accounts.Account account)
        {
            var req = new HttpRequestMessage()
            {
                RequestUri = new Uri("https://accounts.nexon.net/account/login/launcher"),
                Method = HttpMethod.Post,
                Content = new StringContent($"{{\"id\":\"{account.Username}\",\"password\":\"{account.Token}\",\"auto_login\":false,\"client_id\":\"{CLIENT_ID}\",\"scope\":\"{SCOPE}\",\"device_id\":\"{DeviceId}\"}}", Encoding.UTF8, "application/json")
            };
            req.Headers.Add("User-Agent", "NexonLauncher node-webkit/0.14.6 (Windows NT 10.0; WOW64) WebKit/537.36 (@c26c0312e940221c424c2730ef72be2c69ac1b67) nexon_client");
            req.Headers.Add("Origin", "chrome-extension://dobbaijafcbikgimjpakclacfgeagffm");

            HttpResponseMessage res = await Client.SendAsync(req);
            dynamic json = await ParseResponseJson(res);

            accessToken = json["access_token"];
            ClientCookies.Add(new Cookie("nxtk", accessToken, "/", ".nexon.net")); // this cookie is used in all future requests
        }

        public static async Task<dynamic> ParseResponseJson(HttpResponseMessage res)
        {
            // include err checking here
            dynamic json = JObject.Parse(await res.Content.ReadAsStringAsync());

            if (!res.IsSuccessStatusCode)
                throw new NexonError((string)json["message"], (string)json["code"]);

            if (json["error"] != null)
                throw new NexonError((string)json["error"]["code"], (string)json["error"]["message"]);

            return json;
        }

        public static void AddNexonLauncherData(HttpRequestMessage req)
        {
            req.Headers.Add("User-Agent", "NexonLauncher.nxl-17.03.02-275-220ecfb");
            req.Headers.Add("Authorization", "bearer " + Util.EncodeB64(accessToken)); // this + cookie is used for auth here.
        }
    }
}
