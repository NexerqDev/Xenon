using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xenon
{
    static class Util
    {
        public static string AppDirectory = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "appdata");

        public static string EncodeB64(string plainText)
            => System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(plainText));

        public static string DecodeB64(string base64EncodedData)
            => System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(base64EncodedData));

#if DEBUG
        static Util()
        {
            // for debugging - dont have to move to the folder or w/e
            AppDirectory = Microsoft.Win32.RegistryKey
                .OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, Microsoft.Win32.RegistryView.Registry32)
                .OpenSubKey(@"SOFTWARE\Wizet\MapleStory", false)
                .GetValue("ExecPath").ToString();
        }
#endif
    }
}
