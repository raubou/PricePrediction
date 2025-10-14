using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace TMDInvestment.Helpers
{
    public static class CoinBaseAuthenicator
    {
        public static string CB_ACCESS_KEY = string.Empty; //The api key as a string.
        public static string CB_ACCESS_SIGN = string.Empty; //The base64-encoded signature(see Signing a Message).
        public static string CB_ACCESS_TIMESTAMP = string.Empty; //A timestamp for your request.
        public static string CB_ACCESS_PASSPHRASE = string.Empty; //The passphrase you specified when creating the API key.
        public static string GenerateSignature(string timestamp, HttpMethod method, string url, string body, string appSecret)
        {
            return GetHMACInHex(appSecret, timestamp + method.ToString().ToUpper() + url + body);
        }

        private static string GetHMACInHex(string key, string data)
        {
            //var hmacKey = Encoding.UTF8.GetBytes(key);
            var hmacKey = Convert.FromBase64String(key);
            var dataBytes = Encoding.UTF8.GetBytes(data);

            using (var hmac = new HMACSHA256(hmacKey))
            {
                hmac.Initialize();                
                var sig = hmac.ComputeHash(dataBytes);
                //return sig.ByteArrayToHexString();
                //return ByteToHexString(sig);
                return Convert.ToBase64String(sig);
            }
        }

        //https://stackoverflow.com/questions/311165/how-do-you-convert-a-byte-array-to-a-hexadecimal-string-and-vice-versa/14333437#14333437
        //static string ByteToHexString(byte[] bytes)
        //{
        //    char[] c = new char[bytes.Length * 2];
        //    int b;
        //    for (int i = 0; i < bytes.Length; i++)
        //    {
        //        b = bytes[i] >> 4;
        //        c[i * 2] = (char)(87 + b + (((b - 10) >> 31) & -39));
        //        b = bytes[i] & 0xF;
        //        c[i * 2 + 1] = (char)(87 + b + (((b - 10) >> 31) & -39));
        //    }
        //    return new string(c);
        //}
    }
}
