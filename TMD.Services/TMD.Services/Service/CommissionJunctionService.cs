using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using log4net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Dynamic;
using System.Text;
using System.Xml.Linq;
using TMDInvestments.PricePrediction.Helpers;
using TMD.Database.Connect.Managers;
using TMDInvestments.Models;
using WikiNews2._0.helpers;

namespace TMD.Services
{
    public sealed class CommissionJunctionService
    {
        public static Stopwatch timer;
        private static string? _affiliateid = string.Empty;
        private static string? _websiteId = string.Empty;
        private static string? _baseUrl = string.Empty;
        private static string? _baseGraphUrl = string.Empty;
        private static string? _query = string.Empty;
        private static string? _token = string.Empty;
        private static string? _merchantUrl = string.Empty;
        private static IConfiguration? CommissionJunctionAPI;
        private static IConfiguration? MongoDb;
        private static IConfiguration? connection;
        private static HttpClient? _client;
        //private MongodbConnect _connect;
        private Manager? manager;
        private ILogger? _log;
        //private static SqlConnection? _connection;
        string? conn = string.Empty;
        string? mongoConn = string.Empty;
        public CommissionJunctionService(ILogger log, HttpClient client, bool isAzureFunction)
        {
            _log = log;
            _client = client;
            timer = new Stopwatch();
            if (!isAzureFunction)
            {
                IConfigurationBuilder builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json");
                IConfigurationRoot configuration = builder.Build();
                CommissionJunctionAPI = configuration.GetSection("CommissionJunction");
                MongoDb = configuration.GetSection("Mondodb");
                connection = configuration.GetSection("ConnectionStrings");
                if (CommissionJunctionAPI != null)
                {
                    _baseUrl = CommissionJunctionAPI.GetSection("baseUrl").Value;
                    _baseGraphUrl = CommissionJunctionAPI.GetSection("baseGraphUrl").Value;
                    _token = CommissionJunctionAPI.GetSection("token").Value;
                    _affiliateid = CommissionJunctionAPI.GetSection("affiliateId").Value;
                    _websiteId = CommissionJunctionAPI.GetSection("websiteId").Value;
                    _merchantUrl = CommissionJunctionAPI.GetSection("merchantUrl").Value;
                    //conn = connection.GetSection("connection").Value;
                    //mongoConn = MongoDb.GetSection("connection").Value;
                }
                if (connection != null)
                    conn = connection.GetSection("connection").Value;
                if (mongoConn != null)
                    mongoConn = MongoDb.GetSection("connection").Value;
                //manager = new Manager(connection.GetSection("connection").Value, MongoDb.GetSection("connection").Value, _log);
            }
            else
            {
                _baseUrl = Environment.GetEnvironmentVariable("CommissionJunctionBaseUrl");
                _baseGraphUrl = Environment.GetEnvironmentVariable("CommissionJunctionBaseGraphUrl");
                _token = Environment.GetEnvironmentVariable("CommissionJunctionToken");
                _affiliateid = Environment.GetEnvironmentVariable("CommissionJunctionAffiliateId");
                _websiteId = Environment.GetEnvironmentVariable("CommissionJunctionWebsiteId");
                _merchantUrl = Environment.GetEnvironmentVariable("CommissionJunctionMerchantUrl");
                conn = Environment.GetEnvironmentVariable("ConnectionString");
                mongoConn = Environment.GetEnvironmentVariable("MondodbConnection");
                //manager = new Manager(Environment.GetEnvironmentVariable("ConnectionString"), Environment.GetEnvironmentVariable("MondodbConnection"), _log);
            }
            //_connection = new SqlConnection(conn);
            //_connection.Open();
            manager = new Manager(conn, mongoConn, _log);
        }
        ~CommissionJunctionService()
        {
            //if (_connection.State == ConnectionState.Open)
            //{
            //    _connection.Close(); _connection.Dispose(); SqlConnection.ClearPool(_connection);
            //}
        }
        //private void GetSettings(HttpClient client)
        //{
        //    try
        //    {
        //        IConfigurationBuilder builder = new ConfigurationBuilder()
        //        .AddJsonFile("appsettings.json");
        //        IConfigurationRoot configuration = builder.Build();
        //        CommissionJunctionAPI = configuration.GetSection("CommissionJunction");
        //        MongoDb = configuration.GetSection("Mondodb");
        //        connection = configuration.GetSection("ConnectionStrings");
        //        if (CommissionJunctionAPI != null)
        //        {
        //            _baseUrl = CommissionJunctionAPI.GetSection("baseUrl").Value;
        //            _baseGraphUrl = CommissionJunctionAPI.GetSection("baseGraphUrl").Value;
        //            _token = CommissionJunctionAPI.GetSection("token").Value;
        //            _affiliateid = CommissionJunctionAPI.GetSection("affiliateId").Value;
        //            _websiteId = CommissionJunctionAPI.GetSection("websiteId").Value;
        //            _merchantUrl = CommissionJunctionAPI.GetSection("merchantUrl").Value;
        //        }
        //        manager = new Manager(connection.GetSection("connection").Value, MongoDb.GetSection("connection").Value, _log);
        //    }
        //    catch (Exception ex)
        //    {
        //        _baseUrl = Environment.GetEnvironmentVariable("CommissionJunctionBaseUrl");
        //        _baseGraphUrl = Environment.GetEnvironmentVariable("CommissionJunctionBaseGraphUrl");
        //        _token = Environment.GetEnvironmentVariable("CommissionJunctionToken");
        //        _affiliateid = Environment.GetEnvironmentVariable("CommissionJunctionAffiliateId");
        //        _websiteId = Environment.GetEnvironmentVariable("CommissionJunctionWebsiteId");
        //        _merchantUrl = Environment.GetEnvironmentVariable("CommissionJunctionMerchantUrl");
        //        manager = new Manager(Environment.GetEnvironmentVariable("ConnectionString"), Environment.GetEnvironmentVariable("MondodbConnection"), _log);
        //    }

        //    //_connect = new MongodbConnect(MongoDb.GetSection("connection").Value, "Advertising");



        //    _client = client;
        //}

        public void GetProductsGraphQL(List<Category> categories)
        {
            timer.Start();
            //GetSettings(client);
            RssModel rss;// = new RssModel() { title = "CJ", source = 4848};
            dynamic results;
            var request = new GraphQLRequest
            {
                Query = @"{products(companyId:" + _affiliateid + ",partnerStatus:JOINED,limit:10000){resultList{advertiserId,advertiserName,catalogId,id,title,description,price{amount,currency}linkCode(pid:" + _websiteId +"){clickUrl,imageUrl,html{LinkType:image}}}}}"
            };

            var graphQLClient = new GraphQLHttpClient(_baseGraphUrl, new NewtonsoftJsonSerializer());
            graphQLClient.HttpClient.DefaultRequestHeaders.Add("Authorization", "bearer " + _token);
            results = graphQLClient.SendQueryAsync<dynamic>(request).Result;

            //Console.WriteLine("Total Products for CJ" + results.Data.products.resultList.Count);
            _log.LogInformation($"Total Products for CJ {results.Data.products.resultList.Count}");
            rss = LoadGraphProduct(categories, results.Data, _log);
            //rss.items.Shuffle<RssModelItems>();
            //_connect.DeleteBySource("Commision Junction");
            //Console.WriteLine("Total Commision Junction verified to save: " + rss.items.Count());
            _log.LogInformation("Total Commision Junction verified to save: " + rss.items.Count());
            //_connect.Save(rss);
            //manager.DeleteAdvertisingBySource(null, "Commision Junction");
            //manager.EditAdvertising(rss);
            manager.AddBulk("[queue].[Advertising]", rss.items);
            Thread.Sleep(1000);
            timer.Stop();
            TimeSpan ts = timer.Elapsed;
            //Console.WriteLine(nameof(CommissionJunctionService) + " Processing Time - " + ts.Hours + ":" + ts.Minutes + ":" + ts.Seconds + ":" + ts.Milliseconds + ":");
            _log.LogInformation(nameof(CommissionJunctionService) + " Processing Time - " + ts.Hours + ":" + ts.Minutes + ":" + ts.Seconds + ":" + ts.Milliseconds + ":");
        }

        public dynamic GetPromos(ILog _log, ref dynamic error)
        {
            HttpRequestMessage request;
            dynamic results = new ExpandoObject();
            Dictionary<string, string> headers = new Dictionary<string, string>();
            string url = _baseUrl + "website-id=" + _websiteId + "&link-type=banner&advertiser-ids=joined&records-per-page=100";
            headers.Add("Authorization", "Bearer " + _token);
            request = AuthenicationHelper.GetAuthenicatedRequest(HttpMethod.Get, url, headers, "", "");
            results = APIProxy<ExpandoObject>.Send(request, "", ref error, _client);
            if (Errors.HasErrors(error))
            {
                return error;
            }
            results = LoadProduct(results.doc, _log);
            return results;
        }

        private RssModel LoadProduct(dynamic results, ILog _log)
        {
            RssModel rss = new RssModel();
            rss.source = 99;
            rss.title = "Commision Junction";
            foreach (var item in (((XDocument)results).Descendants("links").Elements()))
            {
                var title = item.Descendants("advertiser-name").FirstOrDefault().Value + " Sales";
                var advertiser = item.Descendants("advertiser-Name").FirstOrDefault().Value;
                var image = ImageHelper.AbstractImageURL(item.Descendants("link-code-html").FirstOrDefault().Value);
                var link = item.Descendants("clickUrl").FirstOrDefault().Value;
                if (ImageHelper.IsImageUrl(image.Replace("http:", "https:").Trim(), true, _client) && link.Trim() != string.Empty)
                    rss.items.Add(new RssModelItems { title = title, link = link, content = "", source = "Commision Junction", pubDate = DateTime.Now.ToString(), image = image.Replace("http:", "https:").Trim(), price = "",Advertiser = advertiser, DateCreated = DateTime.Now });
            }
            if (rss.items.Count() <= 0)
            {
                //Console.WriteLine("Not items added");
                _log.Info("Not items added");
            }                
            else
            {
                //Console.WriteLine("Total Items, for " + rss.title + " " + rss.items.Count());
                _log.Info("Total Items, for " + rss.title + " " + rss.items.Count());
            }                
            return rss;
        }

        private RssModel LoadGraphProduct(List<Category> categories, dynamic results, ILogger _log)
        {
            RssModel rss = new RssModel() {items = new List<RssModelItems>() };
            rss.source = 102;
            rss.title = "Commision Junction";
            //List<RssModelItems> resultList = ((JArray)results.products.resultList).Where(y => ImageHelper.IsImageUrl(((string)y["linkCode"]["imageUrl"]).Replace("http:", "https:").Trim(), true, _client).Result && ((string)y["linkCode"]["clickUrl"]).Trim() != string.Empty).Select(x => new RssModelItems
            //{        
            //    title = (string)x["title"],
            //    content = (string)x["description"],
            //    price =(string.IsNullOrEmpty((string)x["price"]["amount"])) ? "0" : (string)x["price"]["amount"],
            //    image = ((string)x["linkCode"]["imageUrl"]).Replace("http:", "https:").Trim(),
            //    link = (string)x["linkCode"]["clickUrl"],
            //    Advertiser = (string)x["advertiserName"]
            //}).ToList();
            List<RssModelItems> resultList = ((JArray)results.products.resultList).Select(x => new RssModelItems
            {
                title = (string)x["title"].ToString().Replace("'", "").Replace("\"", "").Replace("&quot;", "").Replace(Convert.ToChar(34).ToString(), ""),
                content = (string)x["description"].ToString().Replace("'", "").Replace("\"", "").Replace("&quot;", "").Replace(Convert.ToChar(34).ToString(), ""),
                price = (string.IsNullOrEmpty((string)x["price"]["amount"])) ? "0" : (string)x["price"]["amount"],
                image = ((string)x["linkCode"]["imageUrl"]).Replace("http:", "https:").Trim(),
                link = (string)x["linkCode"]["clickUrl"],
                Advertiser = (string)x["advertiserName"],
                Category = DataAggregator.SetCategory((string)x["title"].ToString().Replace("'", "").Replace("\"", "").Replace("&quot;", "").Replace(Convert.ToChar(34).ToString(), ""), categories),
                source = "Commision Junction",
                DateCreated = DateTime.Now,
                pubDate = DateTime.Now.ToString()
            }).ToList();
            //foreach (var item in resultList)
            //{
            //    try
            //    {
            //        if (ImageHelper.IsImageUrl(item.image.Replace("http:", "https:").Trim(), true, _client) && item.link.Trim() != string.Empty)
            //            rss.items.Add(new RssModelItems { title = item.title.Replace("'", ""), link = item.link, content = "<span style='color:green'>$" + item.price + "</span><br>" + item.Advertiser + "<br />" + item.content, source = "", pubDate = item.pubDate.Replace("/", " "), image = item.image.Replace("http:", "https:").Trim(), price = item.price, Advertiser = item.Advertiser, Category = item.Advertiser.Replace("'", "") });
            //        //rss.items.Add(new RssModelItems { title = item.productname.Replace("'", ""), link = item.linkurl, content = "<span style='color:green'>$" + item.price + "</span><br>" + item.merchantname + "<br />" + item.description[0].secondary, source = "", pubDate = item.createdon.Replace("/", " "), image = item.imageurl.Replace("http:", "https:").Trim(), price = (string.IsNullOrEmpty(item.saleprice)) ? "0" : item.saleprice, Advertiser = item.merchantname, Category = DataAggregator.SetCategory(item.productname.Replace("'", "")) });
            //    }
            //    catch (Exception ex)
            //    {
            //        var innerExp = (ex.InnerException != null) ? ex.InnerException.ToString() : "";
            //        //Console.WriteLine("An Error has occured in your application.");
            //        _log.Error("An Error has occured in your application.");
            //        //Console.WriteLine(ex.Source + " " + ex.StackTrace + " " + innerExp);
            //        _log.Error(ex.Source + " " + ex.StackTrace + " " + innerExp);
            //    }
            //}
            rss.items = resultList;
            if (rss.items.Count() <= 0)
            {
                //Console.WriteLine("Not items added");
                _log.LogInformation("Not items added");
            }
            else
            {
                //Console.WriteLine("Total Items, for CJ " + rss.items.Count());
                _log.LogInformation("Total Items, for CJ " + rss.items.Count());
            }
            return rss;
        }

        //private HttpRequestMessage GetAuthenicatedRequest(HttpMethod method, string url, Dictionary<string, string> header, string contentType, string body)
        //{
        //    HttpRequestMessage request = new HttpRequestMessage(method, url) { Version = new Version(2, 0) };
        //    foreach (var item in header)
        //    {
        //        request.Headers.Add(item.Key, item.Value);
        //    }
        //    request.Content = new StringContent(body,Encoding.UTF8);
        //    return request;
        //}
    }
}
