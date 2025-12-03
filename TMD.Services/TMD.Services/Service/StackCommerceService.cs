using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TMD.Database.Connect.Helper;

namespace TMD.Services.Service
{
    public sealed class StackCommerceService
    {
        private static IHttpClientFactory? _factory;
        dynamic? error = null;
        private static ILogger? _logger;
        private static HttpClientServices<dynamic>? _client;
        public StackCommerceService(ILogger logger, IHttpClientFactory factory) {
            _logger = logger;
            _factory = factory;
        }
        public dynamic? GetFeed()
        {
            //Dictionary<string, string> headers = new Dictionary<string, string>();
            //headers.Add("X-API-KEY", yfapi.yfapiKey.ToString());
            string url = "https://stacksocial.com/feed?aid=a-d6qvcjh3";
            _client = new HttpClientServices<dynamic>(_factory, _logger);
            var feed = _client.Get(url);
            return feed;
        }
    }
}
