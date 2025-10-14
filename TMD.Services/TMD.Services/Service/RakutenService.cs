using Microsoft.Extensions.Configuration;
using NewsAggregator.models.returnModel;
using System.Text;
using WikiNews2._0.helpers;
using TMDInvestments.Models;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using TMDInvestments.Models.Models.returnModel;
using System.Net;

namespace TMD.Services
{
    public sealed class RakutenService
    {
        public static Stopwatch timer;
        private static string? _baseUrl;
        //private static string? _baseToken;
        private static string? _clientId;
        private static string? _clientSecret;
        private static string? _userId;
        private static string? _password;
        private static string? _sid;
        private static readonly int maxPages = 3000;
        private static IConfiguration? linkShare;
        //private static IConfiguration? MongoDb;
        private static IConfiguration? connection;
        static HttpRequestMessage? request;
        static TokenModel? token;
        private static HttpClient? _client;
        //private MongodbConnect _connect;
        //private static Manager? manager;
        dynamic? error = null;
        private static ILogger? _log;
        //private static SqlConnection? _connection;
        //static string? conn = string.Empty;
        //static string? mongoConn = string.Empty;
        public RakutenService(ILogger log, HttpClient client, bool isAzureFunction)            
        {
            _log = log;
            _client = client;
            timer = new Stopwatch();
            if (!isAzureFunction)
            {
                IConfigurationBuilder builder = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json");
                IConfigurationRoot configuration = builder.Build();
                linkShare = configuration.GetSection("LinkShare");
                connection = configuration.GetSection("ConnectionStrings");
                //MongoDb = configuration.GetSection("Mondodb");
                if (linkShare != null)
                {
                    _baseUrl = linkShare.GetSection("baseUrl").Value;
                    //_baseToken = linkShare.GetSection("baseToken").Value;
                    _clientId = linkShare.GetSection("LinkShareClientId").Value;
                    _clientSecret = linkShare.GetSection("LinkShareClientSecret").Value;
                    _userId = linkShare.GetSection("userId").Value;
                    _password = linkShare.GetSection("password").Value;
                    _sid = linkShare.GetSection("LinkShareSid").Value;
                }
                //if (connection != null)
                //    conn = connection.GetSection("connection").Value;
                //if (mongoConn != null)
                //    mongoConn = MongoDb.GetSection("connection").Value;
                //manager = new Manager(connection.GetSection("connection").Value, MongoDb.GetSection("connection").Value, _log);
            }
            else
            {
                _baseUrl = Environment.GetEnvironmentVariable("LinkShareBaseUrl");
                //_baseToken = Environment.GetEnvironmentVariable("LinkShareBaseToken");
                _clientId = Environment.GetEnvironmentVariable("LinkShareClientId");
                _clientSecret = Environment.GetEnvironmentVariable("LinkShareClientSecret");
                _userId = Environment.GetEnvironmentVariable("LinkShareUserId");
                _password = Environment.GetEnvironmentVariable("LinkSharePassword");
                _sid = Environment.GetEnvironmentVariable("LinkShareSid");
                //conn = Environment.GetEnvironmentVariable("ConnectionString");
                //mongoConn = Environment.GetEnvironmentVariable("MondodbConnection");
                //manager = new Manager(Environment.GetEnvironmentVariable("ConnectionString"), Environment.GetEnvironmentVariable("MondodbConnection"), _log);
            }
            //_connection = new SqlConnection(conn);
            //_connection.Open();
            //manager = new Manager(_connection, mongoConn, _log);
        }
        static RakutenService()
        {
            //_log = log;
            //_client = client;
            timer = new Stopwatch();
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddFilter("Microsoft", LogLevel.Warning)
                    .AddFilter("System", LogLevel.Warning)
                    .AddFilter("LoggingConsoleApp.Program", LogLevel.Debug);
                    //.AddConsole();
            });
            _log = loggerFactory.CreateLogger<RakutenService>();
            _client = new HttpClient(new HttpClientHandler() { Proxy = null, UseProxy = false });
            _client.DefaultRequestVersion = HttpVersion.Version20;
            _client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
            _client.Timeout = TimeSpan.FromHours(2);
            try
            {
                IConfigurationBuilder builder = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json");
                IConfigurationRoot configuration = builder.Build();
                linkShare = configuration.GetSection("LinkShare");
                connection = configuration.GetSection("ConnectionStrings");
                //MongoDb = configuration.GetSection("Mondodb");
                if (linkShare != null)
                {
                    _baseUrl = linkShare.GetSection("baseUrl").Value;
                    //_baseToken = linkShare.GetSection("baseToken").Value;
                    _clientId = Environment.GetEnvironmentVariable("LinkShareClientId");
                    _clientSecret = Environment.GetEnvironmentVariable("LinkShareClientSecret");
                    _userId = linkShare.GetSection("userId").Value;
                    _password = linkShare.GetSection("password").Value;
                    _sid = linkShare.GetSection("LinkShareSid").Value;
                    //conn = connection.GetSection("connection").Value;
                    //mongoConn = MongoDb.GetSection("connection").Value;
                }
            }
            catch
            {
                _baseUrl = Environment.GetEnvironmentVariable("LinkShareBaseUrl");
                //_baseToken = Environment.GetEnvironmentVariable("LinkShareBaseToken");
                _clientId = Environment.GetEnvironmentVariable("LinkSHareClientId");
                _clientSecret = Environment.GetEnvironmentVariable("LinkSHareClientSecret");
                _userId = Environment.GetEnvironmentVariable("LinkShareUserId");
                _password = Environment.GetEnvironmentVariable("LinkSharePassword");
                _sid = Environment.GetEnvironmentVariable("LinkShareSid");
                //conn = Environment.GetEnvironmentVariable("ConnectionString");
                //mongoConn = Environment.GetEnvironmentVariable("MondodbConnection");
                //manager = new Manager(Environment.GetEnvironmentVariable("ConnectionString"), Environment.GetEnvironmentVariable("MondodbConnection"), _log);
            }
            if (string.IsNullOrEmpty(_baseUrl) || string.IsNullOrEmpty(_clientId))
            {
                _baseUrl = Environment.GetEnvironmentVariable("LinkShareBaseUrl");
                //_baseToken = Environment.GetEnvironmentVariable("LinkShareBaseToken");
                _clientId = Environment.GetEnvironmentVariable("LinkSHareClientId");
                _clientSecret = Environment.GetEnvironmentVariable("LinkSHareClientSecret");
                _userId = Environment.GetEnvironmentVariable("LinkShareUserId");
                _password = Environment.GetEnvironmentVariable("LinkSharePassword");
                _sid = Environment.GetEnvironmentVariable("LinkShareSid");
                //conn = Environment.GetEnvironmentVariable("ConnectionString");
                //mongoConn = Environment.GetEnvironmentVariable("MondodbConnection");
            }
            //_connection = new SqlConnection(conn);
            //manager = new Manager(_connection, mongoConn, _log);
        }

        public void GetProducts(MerchantItem merchant, List<Category> categories, ref RssModel rss)
        {
            try
            {
                timer.Start();
                //GetSettings(client);
                if (token == null || string.IsNullOrEmpty(token?.access_token))
                {
                    token = GetToken(_client, ref error);
                }
                //else
                if (token == null || string.IsNullOrEmpty(token?.access_token))
                {
                    //Console.WriteLine("Cannot get products for Index " + index + " Token not availiabe.");
                    _log.LogInformation("Cannot get products. Token not availiabe.");
                    return;
                }
                GetProductsIndexes(_log, ref rss, merchant, categories, ref error);
                timer.Stop();
                TimeSpan ts = timer.Elapsed;
                //Console.WriteLine(nameof(RakutenService) + " Processing Time - " + ts.Hours + ":" + ts.Minutes + ":" + ts.Seconds + ":" + ts.Milliseconds + ":");
                _log.LogInformation(nameof(RakutenService) + " Processing Time - " + ts.Hours + ":" + ts.Minutes + ":" + ts.Seconds + ":" + ts.Milliseconds + ":");
                
            }
            catch (Exception ex)
            {
                _log.LogError("An error has occured in your application" + ex.Source + " : " + ex.StackTrace + " : " + ex.Message);
                if (ex.InnerException != null)
                {
                    //Console.WriteLine(ex.InnerException?.Message);
                    _log.LogError(ex.InnerException?.Message);
                }
            }
            //System.Threading.Thread.Sleep(5000);
        }

        private void GetProductsIndexes(ILogger _log, ref RssModel rss, MerchantItem _merchant, List<Category> categories, ref dynamic error)
        {
            RakutenModel products;
            //do {
            //index += 1;
            int totalMatches;
            int totalPages;
            int minPages = 2;
            try
            {
                _log.LogInformation("Getting Products for merchant " + _merchant.name);
                products = GetRakutenProducts(1, token, _merchant, ref error);
                //products = GetRakutenProducts(index, token, ref error);
                totalMatches = Convert.ToInt32(products.TotalMatches);
                totalPages = (Convert.ToInt32(products.TotalPages) > maxPages) ? maxPages : Convert.ToInt32(products.TotalPages);
                if (products != null)
                {
                    //LoadProduct(products, _log, ref rss, categories, _merchant.merchantName);
                    LoadProduct(products, _log, ref rss, categories);
                }
                else
                {
                    //Console.WriteLine("Cannot retrieve Products for Index " + index);
                    _log.LogInformation("Cannot retrieve Products for merchant " + _merchant.name);
                }
                while(minPages <= totalPages)
                {
                    products = GetRakutenProducts(minPages, token, _merchant, ref error);
                    if (products != null)
                    {
                        //LoadProduct(products, _log, ref rss, categories, _merchant.merchantName);
                        LoadProduct(products, _log, ref rss, categories);
                    }
                    else
                    {
                        //Console.WriteLine("Cannot retrieve Products for Index " + index);
                        _log.LogInformation("Cannot retrieve Products for merchant " + _merchant.name);
                    }
                    minPages++;
                }

            }
            catch (Exception ex)
            {
                //Console.ForegroundColor = ConsoleColor.Red;
                //Console.WriteLine("An error has occured in your application" + ex.Source + " : " + ex.StackTrace + " : " + ex.Message);
                _log.LogError("An error has occured in your application" + ex.Source + " : " + ex.StackTrace + " : " + ex.Message);
                if (ex.InnerException != null)
                {
                    //Console.WriteLine(ex.InnerException?.Message);
                    _log.LogError(ex.InnerException?.Message);
                }
                //Console.BackgroundColor = ConsoleColor.Black;
                //Console.ForegroundColor = ConsoleColor.White;
                ////Console.Read();
            }
            //System.Threading.Thread.Sleep(5000);
            //} while (index < totalPages);
        }

        private RakutenModel GetRakutenProducts(int index, TokenModel token, MerchantItem merchant, ref dynamic error)
        //private RakutenModel GetRakutenProducts(int index, TokenModel token, ref dynamic error)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("Authorization", "Bearer " + token.access_token);
            HttpRequestMessage request = AuthenicationHelper.GetAuthenicatedRequest(HttpMethod.Get,_baseUrl + "productsearch/1.0?pagenumber=" + index + "&max=100&mid=" + merchant.mid, headers, "application/json", "");
            //HttpRequestMessage request = GetAuthenicatedRequest(HttpMethod.Get, "productsearch/1.0?pagenumber=" + index + "&max=100", headers, "application/json", "");
            RakutenModel results = APIProxy<RakutenModel>.Send(request, "application/json", ref error, _client);
            return results;
        }

        private static TokenModel GetToken(HttpClient client, ref dynamic error)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>();
            //headers.Add("Authorization", "Basic " + _baseToken);
            var textBytes = Encoding.UTF8.GetBytes(_clientId + ":" + _clientSecret);
            // after: 72 101 108 108 111 32 119 111 114 108 100 33 
            var base64String = Convert.ToBase64String(textBytes);
            headers.Add("Authorization", "Bearer " + base64String);
            //headers.Add("Content-Type", "application/x-www-form-urlencoded");
            string body = "grant_type=password&username=" + _userId + "&password=" + _password + "&scope=" + _sid;
            HttpRequestMessage request = AuthenicationHelper.GetAuthenicatedRequest(HttpMethod.Post, _baseUrl + "token", headers, "application/x-www-form-urlencoded", body);
            TokenModel result = APIProxy<TokenModel>.Send(request, "", ref error, client);
            return result;
        }
        //private void LoadProduct(RakutenModel products, ILog _log, ref RssModel rss, List<Category> categories, string merchantName)
        private void LoadProduct(RakutenModel products, ILogger _log, ref RssModel rss, List<Category> categories)
        {
            //DataAggregator da = new DataAggregator();
            foreach (var item in products.item)
            {
                try
                {
                    DateTime createOn;// = DateTime.Now;
                    DateTime.TryParse(item.createdon, out createOn);
                    if (createOn == DateTime.MinValue) { createOn = DateTime.Now; }
                    if (ImageHelper.IsImageUrl(item.imageurl.Replace("http:", "https:").Trim(), true, _client) && item.linkurl.Trim() != string.Empty)
                        rss.items.Add(new RssModelItems { title = item.productname.Replace("'", "").Replace("\"", "").Replace("&quot;", "").Replace(Convert.ToChar(34).ToString(), ""), link = item.linkurl, content = item.description[0].secondary.Replace("'", "").Replace("\"", "").Replace("&quot;", "").Replace(Convert.ToChar(34).ToString(), ""), source = "Rakuten", pubDate = createOn.ToString(), image = item.imageurl.Replace("http:", "https:").Trim(), price = (string.IsNullOrEmpty(item.saleprice)) ? "0" : item.saleprice, Advertiser = item.merchantname, Category = DataAggregator.SetCategory(item.productname.Replace("'", "").Replace("\"", "").Replace("&quot;", "").Replace(Convert.ToChar(34).ToString(), ""), categories), DateCreated = DateTime.Now });
                    //rss.items.Add(new RssModelItems { title = item.productname.Replace("'", "").Replace("\"", "").Replace("&quot;", "").Replace(Convert.ToChar(34).ToString(),""), link = item.linkurl, content = "<span style='color:green'>$" + item.price + "</span><br>" + item.merchantname + "<br />" + item.description[0].secondary.Replace("'", "").Replace("\"", "").Replace("&quot;", "").Replace(Convert.ToChar(34).ToString(), ""), source = "", pubDate = item.createdon.Replace("/", " "), image = item.imageurl.Replace("http:", "https:").Trim(), price = (string.IsNullOrEmpty(item.saleprice)) ? "0" : item.saleprice, Advertiser = item.merchantname, Category = DataAggregator.SetCategory(item.productname.Replace("'", "").Replace("\"", "").Replace("&quot;", "").Replace(Convert.ToChar(34).ToString(), ""), categories), DateCreated = DateTime.Now });
                    //rss.items.Add(new RssModelItems { title = item.productname.Replace("'", ""), link = item.linkurl, content = "<span style='color:green'>$" + item.price + "</span><br>" + item.merchantname + "<br />" + item.description[0].secondary, source = "", pubDate = item.createdon.Replace("/", " "), image = item.imageurl.Replace("http:", "https:").Trim(), price = (string.IsNullOrEmpty(item.saleprice)) ? "0" : item.saleprice, Advertiser = item.merchantname, Category = DataAggregator.SetCategory(item.productname.Replace("'", "")) });
                }
                catch (Exception ex)
                {
                    //Console.ForegroundColor = ConsoleColor.Red;
                    //Console.WriteLine("An error has occured in your application" + ex.Source + " : " + ex.StackTrace + " : " + ex.Message);
                    _log.LogError("An error has occured in your application" + ex.Source + " : " + ex.StackTrace + " : " + ex.Message);
                    if (ex.InnerException != null)
                    {
                        //Console.WriteLine(ex.InnerException?.Message);
                        _log.LogError(ex.InnerException?.Message);
                    }
                    //Console.BackgroundColor = ConsoleColor.Black;
                    //Console.ForegroundColor = ConsoleColor.White;
                    ////Console.Read();
                }

                //System.Threading.Thread.Sleep(1000);
            }
            if (rss.items.Count() <= 0)
            {
                //Console.WriteLine("No items added");
                _log.LogError("No items added");
            }
            else
            {
                //Console.WriteLine("Total Items, " + rss.items.Count());
                _log.LogInformation("Total Items for " + rss.items[0].Advertiser + ", " + rss.items.Count());
            }
        }

        public static MerchByAppStatusResponse LoadMerchants(ILogger _log, HttpClient client)
        {
            dynamic error = null;
            MerchByAppStatusResponse merchants = null;
            //Console.WriteLine("Getting Merchants.");
            _log.LogInformation("Getting Merchants.");
            token = GetToken(client, ref error);
            if (token != null)
            {
                Dictionary<string, string> headers = new Dictionary<string, string>();
                headers.Add("Authorization", "Bearer " + token.access_token);
                //request = AuthenicationHelper.GetAuthenicatedRequest(HttpMethod.Get, _baseUrl + "advertisersearch/1.0", headers, "application/json", "");
                //request = AuthenicationHelper.GetAuthenicatedRequest(HttpMethod.Get, _baseUrl + "v2/advertisers?page=0&limit=100&ships_to=US&deep_links=true&network=1", headers, "application/json", "");
                request = AuthenicationHelper.GetAuthenicatedRequest(HttpMethod.Get, _baseUrl + "linklocator/1.0/getMerchByAppStatus/approved", headers, "application/json", "");

                merchants = APIProxy<MerchByAppStatusResponse>.Send(request, "application/json", ref error, client);
            }
            else
            {
                //Console.WriteLine("Cannot get merchants. Token not availiabe.");
                _log.LogError("Cannot get merchants. Token not availiabe.");
                ////Console.Read();
                Environment.Exit(99);
            }
            return merchants;
        }

        //private void fixContent()
        //{
        //    var rss = _connect.Get("Rakuten");
        //    //foreach(var item in rss.items)
        //    for (int I = 0; I < rss.items.Count; I++)
        //    {
        //        rss.items[I].content = rss.items[I].content.Replace("<span style='color:green'>", "").Replace("</span>", " ").Replace("<br>", "").Replace("<br />", "");
        //        rss.items[I].DateCreated = DateTime.Now;
        //        //Console.WriteLine(rss.items[I].content);
        //    }
        //    rss.source = 5855; rss.title = "Rakuten";
        //    _connect.DeleteBySource("Rakuten");
        //    _connect.Save(rss);
        //}
        //private RakutenMerchantItems GetNextMerchant(ILog _log)
        //{
        //    //Random rnd = new Random();
        //    RakutenMerchantItems merchant = null;
        //    //if (merchants == null)
        //    //Console.WriteLine("Getting Next Merchant");
        //    _log.Info("Getting Next Merchant");
        //    if (_merchants != null && _merchants.midlist.merchant.Count >= 0)
        //    {
        //        merchant = _merchants.midlist.merchant.Where(x => !merchantId.Contains(x.mid)).FirstOrDefault();

        //        if(merchant != null && !string.IsNullOrEmpty(merchant.mid))
        //        {
        //            //Console.WriteLine("Merchant found " + merchant.merchantName);
        //            _log.Info("Merchant found " + merchant.merchantName);
        //            merchantId.Add(merchant.mid);
        //        }
        //        else
        //        {
        //            //Console.WriteLine("No other mernchant found.");
        //            _log.Info("No other mernchant found.");
        //            ////Console.ReadLine();
        //            Environment.Exit(999);
        //        }
        //    }

        //    return merchant;
        //}
    }
}
