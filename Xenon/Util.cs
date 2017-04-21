using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xenon
{
    static class Util
    {
        public static string EncodeB64(string plainText)
            => System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(plainText));

        public static string DecodeB64(string base64EncodedData)
            => System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(base64EncodedData));
    }
}
