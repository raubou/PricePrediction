using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMD.Services
{
    public sealed class GeneralHelper
    {
        static dynamic? error = null;
        public static dynamic GetRequest(Dictionary<string, string> headers, string url, HttpClient? _client)
        {
            var request = AuthenicationHelper.GetAuthenicatedRequest(HttpMethod.Get, url, headers, "application/json", "");
            var response = APIProxy<dynamic>.Send(request, "application/json", ref error, _client);
            return response;
        }
    }
    public sealed class GeneralHelper<T>
    {
        static dynamic? error = null;

        public static T GetTypeRequest(Dictionary<string, string> headers, string url, HttpClient? _client)
        {
            var request = AuthenicationHelper.GetAuthenicatedRequest(HttpMethod.Get, url, headers, "application/json", "");
            var response = APIProxy<T>.Send(request, "application/json", ref error, _client);
            return response;
        }

        public static void AddColumnstoTable(string[] columns, ref DataTable dt)
        {
            foreach (string item in columns)
            {
                if (item == "marketchangepercent" || item == "marketprice" || item == "previousclose" || item == "marketchange" || item == "marketchangepercent"|| item == "epsActual" || item == "epsEstimate" || item == "revenueActual" || item == "revenueEstimate")
                    dt.Columns.Add(item, typeof(decimal));
                else if (item == "markettime" || item == "regularMarketTime")
                    dt.Columns.Add(item, typeof(DateTime));
                else if (item == "ImageData")
                    dt.Columns.Add(item, typeof(byte[]));
                else
                    dt.Columns.Add(item, typeof(string));
            }
        }
    }
}
