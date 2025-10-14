using log4net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NewsAggregator.helpers;
using NewsAggregator.models.returnModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TMDInvestments.PricePrediction.Helpers;
using TMD.Database.Connect;
using TMD.Database.Connect.Managers;
//using TMDInvestment.APIProxy;
using TMDInvestments.Models;
using WikiNews2._0.helpers;
//using WikiNewz.DataAggregator;

namespace TMD.Services
{
    public sealed class ImpactService
    {
        static public Stopwatch timer;
        static private string? _accountSID = string.Empty;
        static private string? _token = string.Empty;
        static private string? _baseUrl = string.Empty;
        static private string _actionVerb = string.Empty;
        static private IConfiguration? ImpactAPI;
        private static IConfiguration? MongoDb;
        private static IConfiguration? connection;
        static private HttpClient? _client;
        //private MongodbConnect _connect;
        private Manager? manager;
        static dynamic? error;
        private ILogger? _log;
        //private static SqlConnection? _connection;
        string? conn = string.Empty;
        string? mongoConn = string.Empty;
        public ImpactService(ILogger log, HttpClient client, bool isAzureFunction)
        {
            _log = log;
            _client = client;
            timer = new Stopwatch();
            if (!isAzureFunction)
            {
                IConfigurationBuilder builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json");
                IConfigurationRoot configuration = builder.Build();
                ImpactAPI = configuration.GetSection("Impact");
                MongoDb = configuration.GetSection("Mondodb");
                connection = configuration.GetSection("ConnectionStrings");
                //_connect = new MongodbConnect(MongoDb.GetSection("connection").Value, "Advertising");
                if (ImpactAPI != null)
                {
                    _baseUrl = ImpactAPI.GetSection("baseUrl").Value;
                    _accountSID = ImpactAPI.GetSection("accountSID").Value;
                    _token = ImpactAPI.GetSection("token").Value;
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
                _baseUrl = Environment.GetEnvironmentVariable("ImpactBaseUrl");
                _accountSID = Environment.GetEnvironmentVariable("ImpactAccountSID");
                _token = Environment.GetEnvironmentVariable("ImpactToken");
                conn = Environment.GetEnvironmentVariable("ConnectionString");
                mongoConn = Environment.GetEnvironmentVariable("MondodbConnection");
                //manager = new Manager(Environment.GetEnvironmentVariable("ConnectionString"), Environment.GetEnvironmentVariable("MondodbConnection"), _log);
            }
            //_connection = new SqlConnection(conn);
            //_connection.Open();
            manager = new Manager(conn, mongoConn, _log);

        }
        ~ImpactService()
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
        //        ImpactAPI = configuration.GetSection("Impact");
        //        MongoDb = configuration.GetSection("Mondodb");
        //        connection = configuration.GetSection("ConnectionStrings");
        //        //_connect = new MongodbConnect(MongoDb.GetSection("connection").Value, "Advertising");
        //        if (ImpactAPI != null)
        //        {
        //            _baseUrl = ImpactAPI.GetSection("baseUrl").Value;
        //            _accountSID = ImpactAPI.GetSection("accountSID").Value;
        //            _token = ImpactAPI.GetSection("token").Value;
        //        }
        //        manager = new Manager(connection.GetSection("connection").Value, MongoDb.GetSection("connection").Value, _log);
        //    }
        //    catch (Exception ex)
        //    {
        //        _baseUrl = Environment.GetEnvironmentVariable("ImpactBaseUrl");
        //        _accountSID = Environment.GetEnvironmentVariable("ImpactAccountSID");
        //        _token = Environment.GetEnvironmentVariable("ImpactToken");
        //        manager = new Manager(Environment.GetEnvironmentVariable("ConnectionString"), Environment.GetEnvironmentVariable("MondodbConnection"), _log);
        //    }            

        //    _client = client;
        //    timer = new Stopwatch();
        //}

        public void GetProducts(List<Category> categories)
        {
            //GetSettings(client);
            timer.Start();
            HttpRequestMessage request;
            RssModel rss;// = new RssModel() { title = "Impact", source = 4848};
            _actionVerb = "Ads";
            Dictionary<string, string> headers = new Dictionary<string, string>();
            ImpactAuthenicator.base64 = ImpactAuthenicator.GenerateSignature(_accountSID, _token);
            string url = _baseUrl + "Mediapartners/" + _accountSID + "/" + _actionVerb;
            headers.Add("Authorization", "Bearer " + ImpactAuthenicator.base64);
            //    HttpRequestMessage request = new HttpRequestMessage(method, _baseUrl + url) { Version = new Version(2, 0) };
            //    request.Headers.Add("Authorization", "Basic " + ImpactAuthenicator.base64);
            //     request.Content = new StringContent(jsonString, Encoding.UTF8, "application/json");
            request = AuthenicationHelper.GetAuthenicatedRequest(HttpMethod.Get, url, headers, "application/json", "");           
            var results = APIProxy<ImpactModel>.Send(request, "application/json", ref error, _client);
            if (Errors.HasErrors(error))
            {
                return;
            }
            rss = LoadProduct(categories, results, _log);            
            //rss.items.ToList().Shuffle<RssModelItems>();
            //_connect.DeleteBySource("Impact");
            //Console.WriteLine("Total Impact verified to save: " + rss.items.Count());
            _log.LogInformation("Total Impact verified to save: " + rss.items.Count());
            //_connect.Save(rss);
            //manager.DeleteAdvertisingBySource(null, "Impact");
            //manager.EditAdvertising(rss);
            manager.AddBulk("[queue].[Advertising]", rss.items);
            Thread.Sleep(1000);
            timer.Stop();
            TimeSpan ts = timer.Elapsed;
            //Console.WriteLine(nameof(ImpactService) + " Processing Time - " + ts.Hours + ":" + ts.Minutes + ":" + ts.Seconds + ":" + ts.Milliseconds + ":");
            _log.LogInformation(nameof(ImpactService) + " Processing Time - " + ts.Hours + ":" + ts.Minutes + ":" + ts.Seconds + ":" + ts.Milliseconds + ":");
        }

        private RssModel LoadProduct(List<Category> categories, ImpactModel products, ILogger _log)
        {
            RssModel rss = new RssModel() { title = "Impact", source = 478484, items = new List<RssModelItems>() };
            foreach (var item in products.ads)
            {
                var imageUrl = ImageHelper.AbstractImageURL(item.Code.Trim());
                if (ImageHelper.IsImageUrl(imageUrl.Replace("http:", "https:").Trim(), true, _client) && item.TrackingLink.Trim() != string.Empty)
                    rss.items.Add(new RssModelItems { title = item.Name.Replace("'", "").Replace("\"", "").Replace("&quot;", "").Replace(Convert.ToChar(34).ToString(), ""), link = item.TrackingLink, content = item.Description.Replace("'", "").Replace("\"", "").Replace("&quot;", "").Replace(Convert.ToChar(34).ToString(), ""), source = "Impact", pubDate = item.StartDate.ToString(), image = imageUrl.Replace("http:", "https:").Trim(), price = "0", Advertiser = item.AdvertiserName, Category = DataAggregator.SetCategory(item.Name.Replace("'", "").Replace("\"", "").Replace("&quot;", "").Replace(Convert.ToChar(34).ToString(), ""), categories), DateCreated = DateTime.Now });
            }
            rss.items = rss.items.GroupBy(x => x.title).Select(s => s.First()).ToList();
            if (rss.items.Count() <= 0)
            {
                //Console.WriteLine("Not items added");
                _log.LogInformation("Not items added");
            }                
            else
            {
                //Console.WriteLine("Total Items, for " + rss.title + " " + rss.items.Count());
                _log.LogInformation("Total Items, for " + rss.title + " " + rss.items.Count());
            }                
            return rss;
        }

        //private HttpRequestMessage GetAuthenicatedRequest(HttpMethod method, dynamic model, ILogger _log)
        //{
        //    JsonSerializerOptions options = new JsonSerializerOptions
        //    {
        //        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        //        AllowTrailingCommas = true,
        //        PropertyNameCaseInsensitive = true,
        //        //IgnoreNullValues = true,
        //        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        //        IgnoreReadOnlyProperties = true,
        //        MaxDepth = 128,
        //        WriteIndented = false,
        //        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        //        ReadCommentHandling = JsonCommentHandling.Disallow
        //    };

        //    string jsonString = string.Empty;
        //    try
        //    {
        //        if (model != null && ((JsonElement)model).GetType() != null)
        //            jsonString = JsonSerializer.Serialize(model, options);
        //    }
        //    catch (Exception ex)
        //    {
        //        //Console.ForegroundColor = ConsoleColor.Red;
        //        //Console.WriteLine("An error has occured in your application" + ex.Source + " : " + ex.StackTrace + " : " + ex.Message);

        //        if (ex.InnerException != null)
        //        {
        //            //Console.WriteLine(ex.InnerException);
        //            _log.LogError(ex.InnerException?.Message ?? "");
        //        }
        //        //Console.BackgroundColor = ConsoleColor.Black;
        //        //Console.ForegroundColor = ConsoleColor.White;
        //        ////Console.Read();
        //    }

        //    ImpactAuthenicator.base64 = ImpactAuthenicator.GenerateSignature(_accountSID, _token);
        //    string url = "Mediapartners/" + _accountSID + "/" + _actionVerb;
        //    HttpRequestMessage request = new HttpRequestMessage(method, _baseUrl + url) { Version = new Version(2, 0) };
        //    request.Headers.Add("Authorization", "Basic " + ImpactAuthenicator.base64);
        //     request.Content = new StringContent(jsonString, Encoding.UTF8, "application/json");
        //    return request;
        //}
    }
}
