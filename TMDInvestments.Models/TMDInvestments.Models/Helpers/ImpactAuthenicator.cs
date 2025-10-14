using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WikiNews2._0.helpers
{
    public sealed class ImpactAuthenicator
    {
        public static string base64 = string.Empty;
        public static string GenerateSignature(string accountSID, string token)
        {
            string hash = Convert.ToBase64String(Encoding.UTF8.GetBytes(accountSID + ':' + token));

            return hash;
        }
    }
}
