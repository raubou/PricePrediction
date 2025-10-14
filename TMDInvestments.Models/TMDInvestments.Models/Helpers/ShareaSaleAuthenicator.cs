using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace WikiNews2._0.helpers
{
    public sealed class ShareaSaleAuthenicator
    {
        // these could be set as constants
        public static string apiToken = string.Empty;
        public static string apiSecret = string.Empty;
        public static string ut = string.Empty;

        //String to Hash: token:datetimeutc:bannerList:secret
        public static string GenerateSignature(string actionVerb)
        {
            SHA256Managed hasher = new SHA256Managed();

            byte[] hash = hasher.ComputeHash(Encoding.UTF8.GetBytes(apiToken + ':' + ut + ':' + actionVerb + ':' + apiSecret));

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }

            string authHeader = sb.ToString();
            return authHeader;
        }
    }
}
