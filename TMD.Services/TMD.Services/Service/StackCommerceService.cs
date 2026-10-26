using HtmlAgilityPack;
using ICSharpCode.SharpZipLib.GZip;
using Microsoft.Extensions.Logging;
using NewsAggregator.models.requestModel;
using Org.BouncyCastle.Utilities.Zlib;
using SharpCompress.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

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
        public XDocument? GetFeed()
        {
            Dictionary<string, object>? headers = new Dictionary<string, object>();
            //headers.Add("User-Agent", "BartergoraAPI");
            headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
            headers.Add("accept-encoding", "gzip,deflate,br,zstd");
            headers.Add("accept-language", "en-US,en;q=0.9");
            headers.Add("cache-control", "no-cache");
            headers.Add("Referer", "https://stacksocial.com/feed?aid=a-d6qvcjh3");
            //headers.Add("Cookie:", "aid=a-d6qvcjh3");
            headers.Add("Cookie:", Guid.NewGuid().ToString());
            //headers.Add("cache-control", "no-cache");
            headers.Add("pragma", "no-cache");
            headers.Add("Access-Control-Allow-Origin", "*");
            headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
            headers.Add("Access-Control-Allow-Headers", "Origin, X-Requested-With, Content-Type, Accept");
            string baseUrl = "https://stacksocial.com/";
            string url = "feed?aid=a-d6qvcjh3";
            _client = new HttpClientServices<dynamic>(_factory, _logger);
            var feed = _client.GetModel(baseUrl, url, null, headers);
            //var feed = _client.Get(url);
            return feed;
        }
        public SyndicationFeed? GetFileFeed()
        {
            // Create an XmlReader for the local file path
            SyndicationFeed returnItem = null;
            string filePath = "C:\\source\\Repo\\PricePrediction\\TMD.EquitiesPrediction\\TMD.EquitiesPrediction\\Stack Commerce Feed\\feed.txt";
            using (XmlReader reader = XmlReader.Create(filePath))
            {
                // Load the feed into a SyndicationFeed object
                SyndicationFeed feed = SyndicationFeed.Load(reader);
                returnItem = feed;

                // Access feed metadata
                Console.WriteLine($"Feed Title: {feed.Title.Text}");

                // Loop through the items in the RSS feed
                //foreach (SyndicationItem item in feed.Items)
                //{
                //    Console.WriteLine($"Title: {item.Title.Text}");
                //    Console.WriteLine($"Summary: {item.Summary?.Text}");

                //    var doc = new HtmlDocument();
                //    doc.LoadHtml(item.Summary?.Text); // or item.Content
                //                                      // Find the first <img> tag
                //    var imgNode = doc.DocumentNode.SelectSingleNode("//img");
                //    if (imgNode != null)
                //    {
                //        string imageUrl = imgNode.GetAttributeValue("src", "");
                //        // Process the imageUrl (e.g., download, display)
                //    }
                //    Console.WriteLine($"Link: {item.Links[0].Uri}");
                //    Console.WriteLine("----------------------------");
                //}
            }
            return returnItem;  
        }
    }
}
