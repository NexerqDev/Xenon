using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace Xenon.Nexon
{
    public static class Auth
    {
        public const string CLIENT_ID = "7853644408";
        public const string SCOPE = "us.launcher.all";

        public static string DeviceId = ""; // lets just make a random 64 string that we use everytime...

        static Auth()
        {
            DeviceId = Properties.Settings.Default.nexonDeviceId;

            if (String.IsNullOrEmpty(DeviceId))
            {
                DeviceId = "";

                var random = new Random();
                for (int i = 0; i < 64; i++)
                    DeviceId += random.Next(16).ToString("X");

                Properties.Settings.Default.nexonDeviceId = DeviceId;
                Properties.Settings.Default.Save();
            }
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
    }
}
