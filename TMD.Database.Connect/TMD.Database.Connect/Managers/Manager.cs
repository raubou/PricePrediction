using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using TMDInvestments.Models;
using TMD.Database.Connect.Repositories;
using TMD.Database.Connect.MongoDB.entity;
using System.Data.SqlClient;
using System.Dynamic;
using MimeKit;
using MailKit.Net.Smtp;
using System.Net.Http;
using TMD.Database.Connect.Helper;
using System.Threading.Tasks;
using TMDInvestments.Models.Models;
using System.Text;
using MongoDB.Driver.Core.Operations;
using TMDInvestments.Models.Models.Polygon;
using TMDInvestments.Models.Models.FInhub;
using System.Linq;

namespace TMD.Database.Connect.Managers
{
    public sealed class Manager
    {
        private Repository _repository;
        //private IOptions<ConnectionSettings> _connectionSettings;
        private string _connectionSettings;
        //private static Dictionary<string, dynamic> param;
        private string _mongDBConnection;
        private static FinnHub _finnhub; 
        private static Polygon _polygon;
        private readonly HttpClient _client;
        //public Manager(IOptions<ConnectionSettings> connectionSettings, string mongDBConnection)
        ILogger? _log;
        private static SqlConnection _con;
        string _connectionString = string.Empty;
        //public Manager(string connectionString, FinnHub finnhub, Polygon polygon, HttpClient client, ILogger log)
        public Manager(ILogger log)
        {
            _log = log;
        }
        public Manager(string connectionString, FinnHub finnhub, Polygon polygon, IHttpClientFactory clientFactory, ILogger log)
        {
            _connectionString = connectionString;
            _finnhub = finnhub; _polygon = polygon;
            _log = log;
            _client = clientFactory.CreateClient();
        }
        //public Manager(FinnHub finnhub, Polygon polygon, HttpClient client)
        public Manager(string connectionString, FinnHub finnhub, Polygon polygon, IHttpClientFactory clientFactory)
        {
            _connectionString = connectionString; _finnhub = finnhub; _polygon = polygon; _client = clientFactory.CreateClient();
        }
        public Manager(string connectionString, string mongDBConnection, ILogger log)
        {
            _connectionString = connectionString;
            _mongDBConnection = mongDBConnection;
            _log = log;
        }
        //public Manager(string connectionString, string mongDBConnection, ILogger log, HttpClient client)
        public Manager(string connectionString, string mongDBConnection, ILogger log, IHttpClientFactory clientFactory)
        {
            _connectionString = connectionString;
            _mongDBConnection = mongDBConnection;
            _log = log;
            _client = clientFactory.CreateClient();
        }
        public RssModel GetNews(string source,bool getImageData, ref dynamic error)
        {
            RssModel rss = new RssModel() { title = "wikiNews", source = 232344, items = new List<RssModelItems>() };
            Dictionary<string, dynamic> param = new Dictionary<string, dynamic>();
            param.Add("source", source);
            if(getImageData)
                param.Add("GetImageData", true);
            _con = new SqlConnection(_connectionString); //if(_con.Open _con.Open();
            _repository = new Repository(_con, _log);
            try
            {
                var results = _repository.GetData("[dbo].[GetNews]", param);
                if (results != null && results.Tables["Results"] != null && results.Tables["Results"].Rows.Count > 0)
                    foreach (DataRow row in results.Tables["Results"].Rows)
                    {
                        rss.items.Add(new RssModelItems()
                        {
                            NewsId = Convert.ToString(row["id"].ToString()),
                            source = Convert.ToString(row["source"]),
                            _source = Convert.ToString(row["source"]),
                            title = Convert.ToString(row["title"]),
                            link = Convert.ToString(row["link"]),
                            price = Convert.ToString(row["price"]),
                            image = Convert.ToString(row["image"]),
                            ImageData = (row.Table.Columns.Contains("ImageData")) ? (byte[])row["ImageData"] : null,
                            pubDate = Convert.ToString(row["pubDate"]),
                            content = Convert.ToString(row["content"]),
                            Advertiser = Convert.ToString(row["Advertiser"]),
                            Category = Convert.ToString(row["Category"]),
                            Banner = Convert.ToString(row["Banner"]),
                            DateCreated = Convert.ToDateTime(row["DateCreated"]),
                        });
                    }
            }
            catch (Exception ex)
            {
                error = ex.Source + " " + ex.StackTrace + " " + ex?.InnerException?.Message + " " + ex.Message;
            }
            return rss;
        }

        public RssModelItems GetArticleById(string Id, ref dynamic error)
        {
            RssModelItems item = null;
            Dictionary<string, dynamic> param = new Dictionary<string, dynamic>();
            param.Add("Id", Id);
            _con = new SqlConnection(_connectionString); //if(_con.Open _con.Open();
            _repository = new Repository(_con, _log);
            try
            {
                var results = _repository.GetData("GetArticleById", param);
                if (results != null && results.Tables["Results"] != null && results.Tables["Results"].Rows.Count > 0)
                {
                    DataRow row = results.Tables["Results"].Rows[0];
                    item = new RssModelItems()
                    {
                        NewsId = Convert.ToString(row["id"].ToString()),
                        source = Convert.ToString(row["source"]),
                        _source = Convert.ToString(row["source"]),
                        title = Convert.ToString(row["title"]),
                        link = Convert.ToString(row["link"]),
                        price = Convert.ToString(row["price"]),
                        image = Convert.ToString(row["image"]),
                        ImageData = (row.Table.Columns.Contains("ImageData")) ? (byte[])row["ImageData"] : null,
                        pubDate = Convert.ToString(row["pubDate"]),
                        content = Convert.ToString(row["content"]),
                        Advertiser = Convert.ToString(row["Advertiser"]),
                        Category = Convert.ToString(row["Category"]),
                        Banner = Convert.ToString(row["Banner"]),
                        DateCreated = Convert.ToDateTime(row["DateCreated"]),
                    };
                }
            }
            catch (Exception ex)
            {
                error = ex.Source + " " + ex.StackTrace + " " + ex?.InnerException?.Message + " " + ex.Message;
            }
            return item;
        }
        
        public byte[] GetNewImage(string Id)
        {
            byte[] image = null; ;
            //RssModel rss = new RssModel() { title = "wikiNews", source = 232344, items = new List<RssModelItems>() };
            Dictionary<string, dynamic> param = new Dictionary<string, dynamic>();
            param.Add("Id", Id);
            _con = new SqlConnection(_connectionString); //if(_con.Open _con.Open();
            _repository = new Repository(_con, _log);
            var results = _repository.GetData("[dbo].[GetNewImage]", param);
            if (results != null && results.Tables["Results"] != null && results.Tables["Results"].Rows.Count > 0)
                image = (byte[])results.Tables["Results"].Rows[0]["ImageData"];
            return image;
        }

        public int? EditNews(RssModel rss)
        {
            //RssModel rss = new RssModel() { title = "wiliNews", source = 232344 };
            Dictionary<string, dynamic> param;// = new Dictionary<string, dynamic>();
            _con = new SqlConnection(_connectionString); _con.Open();
            int? results = 0;
            foreach (RssModelItems item in rss.items)
            {
                param = new Dictionary<string, dynamic>();
                param.Add("id", item.NewsId);
                param.Add("source", item.source);
                param.Add("title", item.title);
                param.Add("link", item.link);
                param.Add("price", item.price);
                param.Add("image", item.image);
                param.Add("pubDate", item.pubDate);
                param.Add("content", item.content);
                param.Add("Advertiser", item.Advertiser);
                param.Add("Category", item.Category);
                param.Add("Banner", item.Banner);
                param.Add("DateCreated", item.DateCreated);
                _repository = new Repository(_con, _log);
                results = Repository.PostData(_con, _log, "[dbo].[EditNews]", param);
            }
            return results;
        }

        public int? EditNews(RssModelItems item)
        {
            //RssModel rss = new RssModel() { title = "wiliNews", source = 232344 };
            Dictionary<string, dynamic> param = new Dictionary<string, dynamic>();
            int? results = 0;
            //foreach (RssModelItems item in rss.items)
            //{
            //param = new Dictionary<string, dynamic>();
            param.Add("id", item.NewsId);
            param.Add("source", item.source);
            param.Add("title", item.title);
            param.Add("link", item.link);
            param.Add("price", item.price);
            param.Add("image", item.image);
            param.Add("pubDate", item.pubDate);
            param.Add("content", item.content);
            param.Add("Advertiser", item.Advertiser);
            param.Add("Category", item.Category);
            param.Add("Banner", item.Banner);
            param.Add("DateCreated", item.DateCreated);
            _con = new SqlConnection(_connectionString); _con.Open();
            _repository = new Repository(_con, _log);
            results = Repository.PostData(_con, _log, "[dbo].[EditNews]", param);
            //}
            return results;
        }

        public int? DeleteNewsBySource(Guid? id, string source)
        {
            //bool success = false;
            Dictionary<string, dynamic> param = new Dictionary<string, dynamic>();
            if (id != null)
                param.Add("id", id);
            if (!string.IsNullOrEmpty(source))
                param.Add("source", source);
            _con = new SqlConnection(_connectionString); _con.Open();
            _repository = new Repository(_con, _log);
            var results = Repository.PostData(_con, _log, "[dbo].[DeleteNews]", param);
            return results;
        }

        public int? EditAdvertising(RssModel rss)
        {
            Dictionary<string, dynamic> param;
            _con = new SqlConnection(_connectionString); _con.Open();
            int? results = 0;
            foreach (RssModelItems item in rss.items)
            {
                DateTime pubDate = DateTime.MinValue;
                DateTime.TryParse(item.pubDate, out pubDate);
                if (pubDate == DateTime.MinValue)
                    pubDate = DateTime.Now;
                param = new Dictionary<string, dynamic>();
                param.Add("id", item.NewsId);
                param.Add("source", item.source);
                param.Add("title", item.title);
                param.Add("link", item.link);
                param.Add("price", item.price);
                param.Add("image", item.image);
                param.Add("pubDate", pubDate.ToString());
                param.Add("content", (item.content ?? ""));
                param.Add("Advertiser", item.Advertiser);
                param.Add("Category", item.Category);
                param.Add("Banner", item.Banner);
                param.Add("DateCreated", (item.DateCreated <= DateTime.MinValue) ? DateTime.Now : item.DateCreated);
                _repository = new Repository(_con, _log);
                results = Repository.PostData(_con, _log, "[dbo].[EditAdvertising]", param);
            }
            return results;
        }

        public int? DeleteAdvertisingBySource(string source)
        {
            //bool success = false;
            Dictionary<string, dynamic> param = new Dictionary<string, dynamic>();
            if (!string.IsNullOrEmpty(source))
                param.Add("source", source);
            _con = new SqlConnection(_connectionString); _con.Open();
            _repository = new Repository(_con, _log);
            var results = Repository.PostData(_con, _log, "[dbo].[DeleteAdvertisingBySource]", param);
            return results;
        }

        public int? TruncateAdvertiser()
        {
            _con = new SqlConnection(_connectionString); _con.Open();
            _repository = new Repository(_con, _log);
            var results = Repository.RunCommand(_con, _log, "[dbo].[TruncateAdvertiser]");
            return results;
        }

        public int? TruncateNews()
        {
            _con = new SqlConnection(_connectionString); _con.Open();
            _repository = new Repository(_con, _log);
            var results = Repository.RunCommand(_con, _log, "[dbo].[TruncateNews]");
            return results;
        }

        public int? TruncateMarkets()
        {
            _con = new SqlConnection(_connectionString); _con.Open();
            _repository = new Repository(_con, _log);
            var results = Repository.RunCommand(_con, _log, "[dbo].[TruncateStockMarketTables]");
            return results;
        }
        public int? TruncateEarnings()
        {
            _con = new SqlConnection(_connectionString); _con.Open();
            _repository = new Repository(_con, _log);
            var results = Repository.RunCommand(_con, _log, "[dbo].[TruncateEarnings]");
            return results;
        }
        public int? TruncateIPOS()
        {
            _con = new SqlConnection(_connectionString); _con.Open();
            _repository = new Repository(_con, _log);
            var results = Repository.RunCommand(_con, _log, "[dbo].[TruncateIPOS]");
            return results;
        }
        //public int? TruncateGroupDaily()
        //{
        //    _con = new SqlConnection(_connectionString); 
        //    _con.Open();
        //    _repository = new Repository(_con, _log);
        //    var results = Repository.RunCommand(_con, _log, "[dbo].[TruncateGroupDailyStock]");
        //    return results;
        //}

        public int? DeleteAdvertisingQueue()
        {
            _con = new SqlConnection(_connectionString); _con.Open();
            _repository = new Repository(_con, _log);
            var results = Repository.PostData(_con, _log, "[queue].[DeleteAdvertisingQueue]", null);
            return results;
        }

        public int? DeleteNewsQueue()
        {
            _con = new SqlConnection(_connectionString); _con.Open();
            _repository = new Repository(_con, _log);
            var results = Repository.PostData(_con, _log, "[queue].[DeleteNewsQueue]", null);
            return results;
        }

        public int? MoveAdvertisingQueue()
        {
            _con = new SqlConnection(_connectionString); _con.Open();
            _repository = new Repository(_con, _log);
            var results = Repository.PostData(_con, _log, "[dbo].[MoveAdvertisingQueue]", null);
            return results;
        }

        public int? MoveNewsQueue()
        {
            _con = new SqlConnection(_connectionString); _con.Open();
            _repository = new Repository(_con, _log);
            var results = Repository.PostData(_con, _log, "[dbo].[MoveNewsQueue]", null);
            return results;
        }

        //public int? QueueAdvertising(RssModel rss)
        //{
        //    Dictionary<string, dynamic> param;
        //    int? results = 0;
        //    Repository.BulkInserter("[queue].[Advertising]", _con, _log, rss.items);
        //    return results;
        //}

        //public int? QueueNews(RssModel rss)
        //{
        //    Dictionary<string, dynamic> param;
        //    int? results = 0;
        //    Repository.BulkInserter("[queue].[News]", _con, _log, rss.items);
        //    return results;
        //}
        public List<dynamic> GetMarkets()
        {
            List<dynamic> markets = new List<dynamic>();
            dynamic market;
            _con = new SqlConnection(_connectionString); _con.Open();
            _repository = new Repository(_con, _log);
            var results = _repository.GetData("[dbo].[GetMarkets]");
            if (results != null && results.Tables["Results"] != null && results.Tables["Results"].Rows.Count > 0)
                foreach (DataRow row in results.Tables["Results"].Rows)
                {
                    market = new ExpandoObject();
                    market.MarketId = row["MarketId"];
                    market.Exchange = row["Exchange"];
                    market.Name = row["Name"];
                    market.Symbol = row["Symbol"];
                    market.MarketPrice = row["MarketPrice"];
                    market.PreviousClose = row["PreviousClose"];
                    market.MarketChange = row["MarketChange"];
                    market.MarketChangePercent = row["MarketChangePercent"];
                    market.MarketTime = row["MarketTime"];
                    market.DateCreated = row["DateCreated"];
                    markets.Add(market);
                }
            return markets;
        }

        public List<dynamic> GetGainersandLosers()
        {
            List<dynamic> gainersandlosers = new List<dynamic>();
            dynamic gainerandloser;
            _con = new SqlConnection(_connectionString); _con.Open();
            _repository = new Repository(_con, _log);
            var results = _repository.GetData("[dbo].[GetGainersandLosers]");
            if (results != null && results.Tables["Results"] != null && results.Tables["Results"].Rows.Count > 0)
                foreach (DataRow row in results.Tables["Results"].Rows)
                {
                    gainerandloser = new ExpandoObject();
                    gainerandloser.GainersLosersId = row["GainersLosersId"];
                    gainerandloser.shortName = row["shortName"];
                    gainerandloser.symbol = row["symbol"];
                    gainerandloser.regularMarketChangePercent = row["regularMarketChangePercent"];
                    gainerandloser.regularMarketChange = row["regularMarketChange"];
                    gainerandloser.regularMarketTime = row["regularMarketTime"];
                    gainerandloser.regularMarketPrice = row["regularMarketPrice"];
                    //gainerandloser.MarketChangePercent = row["MarketChangePercent"];
                    //gainerandloser.MarketTime = row["MarketTime"];
                    //gainerandloser.DateCreated = row["DateCreated"];
                    //gainerandloser = new ExpandoObject();
                    //gainerandloser.MarketId = row["MarketId"];
                    //gainerandloser.Exchange = row["Exchange"];
                    //gainerandloser.Name = row["Name"];
                    //gainerandloser.Symbol = row["Symbol"];
                    //gainerandloser.MarketPrice = row["MarketPrice"];
                    //gainerandloser.PreviousClose = row["PreviousClose"];
                    //gainerandloser.MarketChange = row["MarketChange"];
                    //gainerandloser.MarketChangePercent = row["MarketChangePercent"];
                    //gainerandloser.MarketTime = row["MarketTime"];
                    //gainerandloser.DateCreated = row["DateCreated"];
                    gainersandlosers.Add(gainerandloser);
                }
            return gainersandlosers;
        }


        public List<dynamic> GetIPOs()
        {
            List<dynamic> IPOs = new List<dynamic>();
            dynamic IPO;
            _con = new SqlConnection(_connectionString); _con.Open();
            _repository = new Repository(_con, _log);
            var results = _repository.GetData("[dbo].[GetIPOs]");
            if (results != null && results.Tables["Results"] != null && results.Tables["Results"].Rows.Count > 0)
                foreach (DataRow row in results.Tables["Results"].Rows)
                {
                    IPO = new ExpandoObject();
                    IPO.symbol = row["Symbol"];
                    IPO.name = row["Name"];
                    IPO.exchange = row["Exchange"];
                    IPO.price = row["Price"];
                    IPO.status = row["Status"];
                    IPO.numberOfShares = row["NumberofShares"];
                    IPO.totalSharesValue = row["TotalSharesValue"];
                    IPO.dateExpected = row["DateExpected"];
                    IPO.datecreated = row["datecreated"];
                    IPOs.Add(IPO);
                }
            return IPOs;
        }

        public IEnumerable<DataRow> GetEarnings(ref int Total)
        {
            IEnumerable<DataRow> Earnings = null;// = new IQueryable<dynamic>();
            //dynamic Earning;
            _con = new SqlConnection(_connectionString); _con.Open();
            _repository = new Repository(_con, _log);
            var results = _repository.GetData("[dbo].[GetEarnings]");
            //if (results != null && results.Tables["Results"] != null && results.Tables["Results"].Rows.Count > 0)
            Total = results.Tables["Results"].Rows.Count;
            //Earnings = results.Tables["Results"].Rows.AsQueryable();//.Rows.GetEnumerator()
            Earnings = results.Tables["Results"].AsEnumerable();//.AsQueryable();
            //IEnumerable<DataRow> rows = results.Tables["Results"].AsEnumerable();
            //var caca = results.Tables["Results"].Rows.Cast<DataRow>().Select(dr => dr[0]);
            //foreach (DataRow row in results.Tables["Results"].Rows)
            //{
            //    Earning = new ExpandoObject();
            //    Earning.date = row["date"];
            //    Earning.epsActual = row["epsActual"];
            //    Earning.epsEstimate = row["epsEstimate"];
            //    Earning.hour = row["hour"];
            //    Earning.quarter = row["quarter"];
            //    Earning.revenueActual = row["revenueActual"];
            //    Earning.revenueEstimate = row["revenueEstimate"];
            //    Earning.symbol = row["symbol"];
            //    Earning.name = row["name"];
            //    Earning.year = row["year"];
            //    Earning.datecreated = row["datecreated"];
            //    Earning.LastEarningDate = row["LastEarningDate"];
            //    Earnings.Add(Earning);
            //}
            return Earnings;
        }

        public List<Banner> GetBannersAdmin()
        {
            List<Banner> banners = new List<Banner>();
            //MongodbConnect ads = new MongodbConnect(_mongDBConnection, "ActiveAds");
            _con = new SqlConnection(_connectionString); _con.Open();
            _repository = new Repository(_con, _log);
            var results = _repository.GetData("[dbo].[GetBannersAdmin]");
            if (results != null && results.Tables["Results"] != null && results.Tables["Results"].Rows.Count > 0)
                foreach (DataRow row in results.Tables["Results"].Rows)
                {
                    banners.Add(new Banner()
                    {
                        Id = Convert.ToInt64(row["Id"]),
                        Name = Convert.ToString(row["Name"]),
                        Description = Convert.ToString(row["Description"]),
                        Keywords = Convert.ToString(row["Keywords"]),
                        BannerId = (row["BannerId"] != DBNull.Value) ? Convert.ToInt32(row["BannerId"]) : null,
                        BackGroundColor = (row["BackGroundColor"] != DBNull.Value) ? Convert.ToString(row["BackGroundColor"]) : "",
                        Disabled = Convert.ToBoolean(row["Disabled"])
                        //rssModel = ads.GetAdsByBanner((row["Id"] != DBNull.Value) ? Convert.ToString(row["Id"]) : null)
                    });
                }
            return banners;
        }

        public List<Banner> GetBanners()
        {
            List<Banner> banners = new List<Banner>();
            //MongodbConnect ads = new MongodbConnect(_mongDBConnection, "ActiveAds");
            try
            {
                _con = new SqlConnection(_connectionString); //_con.Open();
                _repository = new Repository(_con, _log);
                var results = _repository.GetData("[dbo].[GetBanners]");
                if (results != null && results.Tables["Results"] != null && results.Tables["Results"].Rows.Count > 0)
                    foreach (DataRow row in results.Tables["Results"].Rows)
                    {
                        banners.Add(new Banner()
                        {
                            Id = Convert.ToInt64(row["Id"]),
                            Name = Convert.ToString(row["Name"]),
                            Description = Convert.ToString(row["Description"]),
                            Keywords = Convert.ToString(row["Keywords"]),
                            BannerId = (row["BannerId"] != DBNull.Value) ? Convert.ToInt32(row["BannerId"]) : null,
                            BackGroundColor = (row["BackGroundColor"] != DBNull.Value) ? Convert.ToString(row["BackGroundColor"]) : "",
                            Disabled = Convert.ToBoolean(row["Disabled"]),
                            rssModel = GetAdsByBanner((row["Id"] != DBNull.Value) ? Convert.ToString(row["Id"]) : null)
                            //rssModel = ads.GetAdsByBanner((row["Id"] != DBNull.Value) ? Convert.ToString(row["Id"]) : null)
                        });
                    }
            }
            catch (Exception ex)
            {
                _log?.LogError(ex.Source + " : " + ex.StackTrace + " : " + ex.InnerException?.Message + " : " + ex.Message);
            }
            return banners;
        }

        public List<Category> GetCategories()
        {
            List<Category> categories = new List<Category>();
            _con = new SqlConnection(_connectionString); _con.Open();
            _repository = new Repository(_con, _log);
            DataSet results = _repository.GetData("[dbo].[GetCategories]");
            if (results != null && results.Tables["Results"] != null && results.Tables["Results"].Rows.Count > 0)
                foreach (DataRow row in results.Tables["Results"].Rows)
                {
                    categories.Add(new Category()
                    {
                        Id = Convert.ToInt64(row["Id"]),
                        Name = Convert.ToString(row["Name"]),
                        Description = Convert.ToString(row["Description"]),
                        Keywords = Convert.ToString(row["Keywords"]),
                        SortOrder = (row["SortOrder"] != DBNull.Value) ? Convert.ToInt32(row["SortOrder"]) : null,
                        Disabled = Convert.ToBoolean(row["Disabled"])
                    });
                }
            return categories;
        }

        public bool EditeBanners(Banner banner)
        {
            bool success = true;
            Dictionary<string, dynamic> param = new Dictionary<string, dynamic>();
            try
            {
                param.Add("Id", banner.Id);
                param.Add("Name", banner.Name);
                param.Add("Description", banner.Description);
                param.Add("Keywords", banner.Keywords);
                param.Add("BannerId", banner.BannerId);
                param.Add("Disabled", banner.Disabled);
                param.Add("BackGroundColor", banner.BackGroundColor);
                _con = new SqlConnection(_connectionString); _con.Open();
                _repository = new Repository(_con, _log);
                int? results = Repository.PostData(_con, _log, "[dbo].[EditBanners]", param);
                if (results == null || results <= 0)
                    success = false;
            }
            catch (Exception ex)
            {
                success = false;
                _log?.LogError(ex.Source + " : " + ex.StackTrace + " : " + ex.InnerException?.Message + " : " + ex.Message);
            }
            return success;
        }

        public bool EditCategories(Category category)
        {
            bool success = true;
            Dictionary<string, dynamic> param = new Dictionary<string, dynamic>();
            try
            {
                param.Add("Id", category.Id);
                param.Add("Name", category.Name);
                param.Add("Description", category.Description);
                param.Add("Keywords", category.Keywords);
                param.Add("SortOrder", category.SortOrder);
                param.Add("Disabled", category.Disabled);
                _con = new SqlConnection(_connectionString); _con.Open();
                _repository = new Repository(_con, _log);
                int? results = Repository.PostData(_con, _log, "[dbo].[EditCategories]", param);
                if (results == null || results <= 0)
                    success = false;
            }
            catch (Exception ex)
            {
                success = false;
                _log?.LogError(ex.Source + " : " + ex.StackTrace + " : " + ex.InnerException?.Message + " : " + ex.Message);
            }
            return success;
        }

        public bool DeleteCategory(long Id)
        {
            bool success = true;
            Dictionary<string, dynamic> param = new Dictionary<string, dynamic>();
            try
            {
                param.Add("Id", Id);
                _con = new SqlConnection(_connectionString); _con.Open();
                _repository = new Repository(_con, _log);
                int? results = Repository.PostData(_con, _log, "[dbo].[DeleteCategory]", param);
                if (results == null || results <= 0)
                    success = false;
            }
            catch (Exception ex)
            {
                success = false;
                _log?.LogError(ex.Source + " : " + ex.StackTrace + " : " + ex.InnerException?.Message + " : " + ex.Message);
            }
            return success;
        }

        public bool DeleteBanner(long Id)
        {
            bool success = true;
            Dictionary<string, dynamic> param = new Dictionary<string, dynamic>();
            try
            {
                param.Add("Id", Id);
                _con = new SqlConnection(_connectionString); _con.Open();
                _repository = new Repository(_con, _log);
                int? results = Repository.PostData(_con, _log, "[dbo].[DeleteBanner]", param);
                if (results == null || results <= 0)
                    success = false;
            }
            catch (Exception ex)
            {
                success = false;
                _log?.LogError(ex.Source + " : " + ex.StackTrace + " : " + ex.InnerException?.Message + " : " + ex.Message);
            }
            return success;
        }

        public List<Users> GetUsers()
        {
            List<Users>? users = new List<Users>();
            try
            {
                _con = new SqlConnection(_connectionString); _con.Open();
                _repository = new Repository(_con, _log);
                var results = _repository.GetData("[dbo].[GetUsers]");

                if (results.Tables["Results"].Rows.Count <= 0)
                {
                    return users;
                }
                foreach (DataRow dr in results.Tables["Results"].Rows)
                {
                    users.Add(new Users { 
                        UserId = Convert.ToInt64(dr["UserId"]),
                        UserName = Convert.ToString(dr["UserName"]),
                        RoleId = Convert.ToInt64(dr["RoleId"]),
                        Password = (byte[])dr["Password"],
                        DateEntered = Convert.ToDateTime(dr["DateEntered"]),
                        DateUpdated = (dr["DateUpdated"] is DBNull)? null : Convert.ToDateTime(dr["DateUpdated"]),
                        Disabled = Convert.ToBoolean(dr["Disabled"])

                    });
                }
            }
            catch (Exception ex)
            {                
                _log?.LogError(ex.Source + " : " + ex.StackTrace + " : " + ex.InnerException?.Message + " : " + ex.Message);
            }
            return users;
        }

        public bool EditUser(Users user) {
            bool success = true;
            try
            {
                Dictionary<string, dynamic> param = new Dictionary<string, dynamic>();
                if (user?.UserId != null && user.UserId > 0)
                    param.Add("UserId", user.UserId);
                param.Add("UserName", user.UserName);
                param.Add("Password", user.Password);
                param.Add("RoleId", user.RoleId);
                param.Add("Disabled", user.Disabled);
                //param.Add("DateUpdated", user.DateUpdated);
                //param.Add("DateEntered", user.DateEntered);
                //param.Add("Output", null);
                _con = new SqlConnection(_connectionString); _con.Open();
                _repository = new Repository(_con, _log);
                Repository.PostData(_con, _log, "[dbo].[EditUsers]", param);
            }
            catch (Exception ex)
            {
                success = false;
                _log?.LogError(ex.Source + " : " + ex.StackTrace + " : " + ex.InnerException?.Message + " : " + ex.Message);
            }
            return success;
        }

        public bool Register(string userName, string password)
        {
            bool success = true;
            //string output = string.Empty;
            try
            {
                Dictionary<string, dynamic> param = new Dictionary<string, dynamic>();
                param.Add("userName", userName);
                param.Add("Password", System.Text.Encoding.UTF8.GetBytes(password));
                param.Add("RoleId", 1);
                //param.Add("Output", null);
                _con = new SqlConnection(_connectionString); _con.Open();
                _repository = new Repository(_con, _log);
                Repository.PostData(_con, _log, "[dbo].[EditUsers]", param);
            }
            catch (Exception ex)
            {
                success = false;
                _log?.LogError(ex.Source + " : " + ex.StackTrace + " : " + ex.InnerException?.Message + " : " + ex.Message);
            }
            return success;
        }
        public bool Authenticate(string userName, string password, ref string RoleName, ref string userId)
        {
            bool success = true;
            try
            {
                Dictionary<string, dynamic> param = new Dictionary<string, dynamic>();
                param.Add("UserName", userName);
                param.Add("Password", System.Text.Encoding.UTF8.GetBytes(password));
                _con = new SqlConnection(_connectionString); _con.Open();
                _repository = new Repository(_con, _log);
                var results = _repository.GetData("[dbo].[GetUser]", param);

                if (results.Tables["Results"].Rows.Count <= 0)
                {
                    success = false;
                    return success;
                }
                DataRow dr = results.Tables["Results"].Rows[0];
                if ((dr["UserName"] == DBNull.Value && dr["Password"] == DBNull.Value) && (dr["UserName"].ToString() == string.Empty && dr["Password"].ToString() == string.Empty))
                {
                    success = false;
                    return success;
                }
                else
                {
                    userId = dr["UserId"].ToString();
                    RoleName = dr["RoleName"].ToString();
                }
            }
            catch (Exception ex)
            {
                success = false;
                _log?.LogError(ex.Source + " : " + ex.StackTrace + " : " + ex.InnerException?.Message + " : " + ex.Message);
            }
            return success;
        }

        public bool RecoverPassword(string userName, ref string Password)
        {
            bool success = true;
            try
            {
                Dictionary<string, dynamic> param = new Dictionary<string, dynamic>();
                param.Add("UserName", userName);
                _con = new SqlConnection(_connectionString); _con.Open();
                _repository = new Repository(_con, _log);
                var results = _repository.GetData("[dbo].[RecoverPassword]", param);
                if (results.Tables["Results"].Rows.Count <= 0)
                {
                    success = false;
                    return success;
                }
                DataRow dr = results.Tables["Results"].Rows[0];
                if (dr["Password"] == DBNull.Value && dr["Password"].ToString() == string.Empty)
                {
                    success = false;
                    return success;
                }
                else
                {
                    Password = System.Text.Encoding.UTF8.GetString((byte[])dr["Password"]);
                }
            }
            catch (Exception ex)
            {
                success = false;
                _log?.LogError(ex.Source + " : " + ex.StackTrace + " : " + ex.InnerException?.Message + " : " + ex.Message);
            }
            return success;
        }

        public bool SaveClientInfo(Source source, ClientInfo clientInfo)
        {
            bool success = true;
            try
            {
                Dictionary<string, dynamic> param = new Dictionary<string, dynamic>();
                param.Add("Source", (int)source);
                param.Add("Page", clientInfo.Page);
                param.Add("TimeZone", clientInfo.Timezone);
                param.Add("System", clientInfo.System);
                param.Add("Browser", clientInfo.Browser);
                param.Add("Language", clientInfo.Language);
                param.Add("Latitude", clientInfo.Latitude);
                param.Add("Longitude", clientInfo.Longitude);
                param.Add("City", clientInfo.City);
                param.Add("State", clientInfo.State);
                param.Add("IP", clientInfo.IP);
                _con = new SqlConnection(_connectionString); _con.Open();
                _repository = new Repository(_con, _log);
                var results = Repository.PostData(_con, _log, "[dbo].[SaveClientInfo]", param);
            }
            catch (Exception ex)
            {
                success = false;
                _log?.LogError(ex.Source + " : " + ex.StackTrace + " : " + ex.InnerException?.Message + " : " + ex.Message);
            }
            return success;
        }
        public bool SaveContactUs(ContactUs contactUs)
        {
            bool success = true;
            try
            {
                Dictionary<string, dynamic> param = new Dictionary<string, dynamic>();
                param.Add("Source", 1);
                param.Add("Name", contactUs.Name);
                param.Add("Email", contactUs.Email);
                param.Add("Subject", contactUs.Subject);
                param.Add("Message", contactUs.Message);
                _con = new SqlConnection(_connectionString); _con.Open();
                _repository = new Repository(_con, _log);
                var results = Repository.PostData(_con, _log, "[dbo].[SaveContactUs]", param);
            }
            catch (Exception ex)
            {
                success = false;
                _log?.LogError(ex.Source + " : " + ex.StackTrace + " : " + ex.InnerException?.Message + " : " + ex.Message);
            }
            return success;
        }
        public bool SaveSubscribers(Subscriber subscriber)
        {
            bool success = true;
            try
            {
                Dictionary<string, dynamic> param = new Dictionary<string, dynamic>();
                param.Add("Source", 1);
                param.Add("email", subscriber.email);
                param.Add("TimeZone", subscriber.Timezone);
                param.Add("System", subscriber.System);
                param.Add("Browser", subscriber.Browser);
                param.Add("Language", subscriber.Language);
                param.Add("Latitude", subscriber.Latitude);
                param.Add("Longitude", subscriber.Longitude);
                param.Add("IP", subscriber.IP);
                _con = new SqlConnection(_connectionString); _con.Open();
                _repository = new Repository(_con, _log);
                var results = Repository.PostData(_con, _log, "[dbo].[SaveSubscribers]", param);
            }
            catch (Exception ex) { success = false; _log?.LogError(ex.Source + " : " + ex.StackTrace + " : " + ex.InnerException?.Message + " : " + ex.Message); }
            return success;
        }

        public bool SaveImpressionsAndClicks(SiteSource sitesource, ImpressionsAndClicks clicks)
        {
            bool success = true;
            try
            {
                Dictionary<string, dynamic> param = new Dictionary<string, dynamic>();
                param.Add("Source", (int)sitesource);
                param.Add("Platform", clicks.Platform);
                param.Add("Title", clicks.Title);
                param.Add("Advertisor", clicks.Advertisor);
                param.Add("Link", clicks.Link);
                param.Add("Impressions", clicks.Impressions);
                param.Add("Clicks", clicks.Clicks);
                _con = new SqlConnection(_connectionString); _con.Open();
                _repository = new Repository(_con, _log);
                var results = Repository.PostData(_con, _log, "[dbo].[SaveImpressionsAndClicks]", param);
            }
            catch (Exception ex) { success = false; _log?.LogError(ex.Source + " : " + ex.StackTrace + " : " + ex.InnerException?.Message + " : " + ex.Message); }
            return success;
        }
        public RssModelItems GetAdById(string Id)
        {
            RssModelItems item = new RssModelItems();
            try
            {
                Dictionary<string, dynamic> param = new Dictionary<string, dynamic>();
                param.Add("Id", Id);
                _con = new SqlConnection(_connectionString); _con.Open();
                _repository = new Repository(_con, _log);
                DataSet results = _repository.GetData("[dbo].[GetAdById]");
                if (results != null && results.Tables["Results"] != null && results.Tables["Results"].Rows.Count > 0)
                {
                    DataRow row = results.Tables["Results"].Rows[0];
                    item.NewsId = Convert.ToString(row["Id"]);
                    item.title = Convert.ToString(row["title"]);
                    item.source = Convert.ToString(row["source"]);
                    item.price = (row["price"] != DBNull.Value) ? Convert.ToString(row["price"]) : null;
                    item.link = Convert.ToString(row["link"]);
                    item.image = Convert.ToString(row["image"]);
                    item.pubDate = Convert.ToString(row["pubDate"]);
                    item.Advertiser = Convert.ToString(row["Advertiser"]);
                    item.Category = Convert.ToString(row["Category"]);
                }
            }
            catch (Exception ex)
            {
                _log?.LogError(ex.Source + " : " + ex.StackTrace + " : " + ex.InnerException?.Message + " : " + ex.Message);
                return item;
            }
            return item;
        }

        public RssModel GetAllActive()
        {
            RssModel rss = new RssModel() { source = 545544, title = "Active Ads", items = new List<RssModelItems>() };
            try
            {
                _con = new SqlConnection(_connectionString); _con.Open();
                _repository = new Repository(_con, _log);
                DataSet results = _repository.GetData("[dbo].[GetAllActiveAds]");
                if (results != null && results.Tables["Results"] != null && results.Tables["Results"].Rows.Count > 0)
                    foreach (DataRow row in results.Tables["Results"].Rows)
                    {
                        rss.items.Add(new RssModelItems()
                        {
                            NewsId = Convert.ToString(row["Id"]),
                            title = Convert.ToString(row["title"]),
                            source = Convert.ToString(row["source"]),
                            price = (row["price"] != DBNull.Value) ? Convert.ToString(row["price"]) : null,
                            link = Convert.ToString(row["link"]),
                            image = Convert.ToString(row["image"]),
                            pubDate = Convert.ToString(row["pubDate"]),
                            Advertiser = Convert.ToString(row["Advertiser"]),
                            Category = Convert.ToString(row["Category"]),
                            Banner = Convert.ToString(row["Banner"]),
                            IsInAdvertismentSpace = Convert.ToBoolean(row["IsInAdvertismentSpace"])
                        });
                    }
                return rss;
            }
            catch (Exception ex)
            {
                _log?.LogError(ex.Source + " : " + ex.StackTrace + " : " + ex.InnerException?.Message + " : " + ex.Message);
                return rss;
            }
        }
        public int? EditActiveAds(RssModel rss)
        {
            Dictionary<string, dynamic> param;
            int? results = 0;
            foreach (RssModelItems item in rss.items)
            {
                DateTime pubDate = DateTime.MinValue;
                DateTime.TryParse(item.pubDate, out pubDate);
                if (pubDate == DateTime.MinValue)
                    pubDate = DateTime.Now;
                param = new Dictionary<string, dynamic>();
                param.Add("id", item.NewsId);
                param.Add("source", item.source);
                param.Add("title", item.title);
                param.Add("link", item.link);
                param.Add("price", item.price);
                param.Add("image", item.image);
                param.Add("pubDate", pubDate.ToString());
                param.Add("content", item.content);
                param.Add("Advertiser", item.Advertiser);
                param.Add("Category", item.Category);
                param.Add("Banner", item.Banner);
                param.Add("DateCreated", (item.DateCreated <= DateTime.MinValue) ? DateTime.Now : item.DateCreated);
                _con = new SqlConnection(_connectionString); _con.Open();
                _repository = new Repository(_con, _log);
                results = Repository.PostData(_con, _log, "[dbo].[EditActiveAds]", param);
            }
            return results;
        }
        public int? EditActiveAds(RssModelItems item)
        {
            Dictionary<string, dynamic> param = new Dictionary<string, dynamic>();
            int? results = 0;
            //foreach (RssModelItems item in rss.items)
            //{
            DateTime pubDate = DateTime.MinValue;
            DateTime.TryParse(item.pubDate, out pubDate);
            if (pubDate == DateTime.MinValue)
                pubDate = DateTime.Now;
            param.Add("id", item.NewsId);
            param.Add("source", item.source);
            param.Add("title", item.title);
            param.Add("link", item.link);
            param.Add("price", item.price);
            param.Add("image", item.image);
            param.Add("pubDate", pubDate.ToString());
            param.Add("content", item.content ?? "");
            param.Add("Advertiser", item.Advertiser);
            param.Add("Category", item.Category);
            param.Add("Banner", item.Banner);
            //param.Add("DateCreated", (item.DateCreated <= DateTime.MinValue) ? DateTime.Now : item.DateCreated);
            param.Add("DateCreated", (item.NewsId != null) ? item.DateCreated : DateTime.Now);
            _con = new SqlConnection(_connectionString); _con.Open();
            _repository = new Repository(_con, _log);
            results = Repository.PostData(_con, _log, "[dbo].[EditActiveAds]", param);
            //}
            return results;
        }
        public int? DeleteActiveByIds(List<string> Ids)
        {
            Dictionary<string, dynamic> param;
            int? results = 0;
            if (Ids == null)
            {
                param = new Dictionary<string, dynamic>();
                param.Add("id", null);
                _con = new SqlConnection(_connectionString); _con.Open();
                _repository = new Repository(_con, _log);
                results = Repository.PostData(_con, _log, "[dbo].[DeleteActiveAds]", param);
                return results;
            }
            foreach (string Id in Ids)
            {
                param = new Dictionary<string, dynamic>();
                param.Add("id", Ids);
                _con = new SqlConnection(_connectionString); _con.Open();
                _repository = new Repository(_con, _log);
                results = Repository.PostData(_con, _log, "[dbo].[DeleteActiveAds]", param);
            }
            return results;
        }
        public RssModelItems FindQueudAdByTitleLinkandImage(RssModelItems input)
        {
            RssModelItems item = null;
            Dictionary<string, dynamic> param = new Dictionary<string, dynamic>();
            param.Add("title", input.title);
            param.Add("link", input.link);
            param.Add("image", input.image);
            _con = new SqlConnection(_connectionString); _con.Open();
            _repository = new Repository(_con, _log);
            var results = _repository.GetData("[dbo].[FindQueuedAd]", param);
            if (results != null && results.Tables["Results"] != null && results.Tables["Results"].Rows.Count > 0)
            {
                item = new RssModelItems();
                //results.Tables["Results"].Rows[0][""]
                item.NewsId = Convert.ToString(results.Tables["Results"].Rows[0]["Id"]);
                item.title = Convert.ToString(results.Tables["Results"].Rows[0]["title"]);
                item.source = Convert.ToString(results.Tables["Results"].Rows[0]["source"]);
                item.price = (results.Tables["Results"].Rows[0]["price"] != DBNull.Value) ? Convert.ToString(results.Tables["Results"].Rows[0]["price"]) : null;
                item.link = Convert.ToString(results.Tables["Results"].Rows[0]["link"]);
                item.image = Convert.ToString(results.Tables["Results"].Rows[0]["image"]);
                item.pubDate = Convert.ToString(results.Tables["Results"].Rows[0]["pubDate"]);
                item.Advertiser = Convert.ToString(results.Tables["Results"].Rows[0]["Advertiser"]);
                item.Category = Convert.ToString(results.Tables["Results"].Rows[0]["Category"]);
                item.Banner = Convert.ToString(results.Tables["Results"].Rows[0]["Banner"]);
            }
            return item;
        }
        public RssModelItems FindAdByTitleLinkandImage(RssModelItems input)
        {
            RssModelItems item = null;
            Dictionary<string, dynamic> param = new Dictionary<string, dynamic>();
            param.Add("title", input.title);
            param.Add("link", input.link);
            param.Add("image", input.image);
            _con = new SqlConnection(_connectionString); _con.Open();
            _repository = new Repository(_con, _log);
            var results = _repository.GetData("[dbo].[FindAd]", param);
            if (results != null && results.Tables["Results"] != null && results.Tables["Results"].Rows.Count > 0)
            {
                item = new RssModelItems();
                //results.Tables["Results"].Rows[0][""]
                item.NewsId = Convert.ToString(results.Tables["Results"].Rows[0]["Id"]);
                item.title = Convert.ToString(results.Tables["Results"].Rows[0]["title"]);
                item.source = Convert.ToString(results.Tables["Results"].Rows[0]["source"]);
                item.price = (results.Tables["Results"].Rows[0]["price"] != DBNull.Value) ? Convert.ToString(results.Tables["Results"].Rows[0]["price"]) : null;
                item.link = Convert.ToString(results.Tables["Results"].Rows[0]["link"]);
                item.image = Convert.ToString(results.Tables["Results"].Rows[0]["image"]);
                item.pubDate = Convert.ToString(results.Tables["Results"].Rows[0]["pubDate"]);
                item.Advertiser = Convert.ToString(results.Tables["Results"].Rows[0]["Advertiser"]);
                item.Category = Convert.ToString(results.Tables["Results"].Rows[0]["Category"]);
                item.Banner = Convert.ToString(results.Tables["Results"].Rows[0]["Banner"]);
            }
            return item;
        }
        public RssModelItems FindActiveAdByTitleLinkandImage(RssModelItems input)
        {
            RssModelItems item = null;
            Dictionary<string, dynamic> param = new Dictionary<string, dynamic>();
            param.Add("title", input.title);
            param.Add("link", input.link);
            param.Add("image", input.image);
            _con = new SqlConnection(_connectionString); _con.Open();
            _repository = new Repository(_con, _log);
            var results = _repository.GetData("[dbo].[FindActiveAd]", param);
            if (results != null && results.Tables["Results"] != null && results.Tables["Results"].Rows.Count > 0)
            {
                item = new RssModelItems();
                //results.Tables["Results"].Rows[0][""]
                item.NewsId = Convert.ToString(results.Tables["Results"].Rows[0]["Id"]);
                item.title = Convert.ToString(results.Tables["Results"].Rows[0]["title"]);
                item.source = Convert.ToString(results.Tables["Results"].Rows[0]["source"]);
                item.price = (results.Tables["Results"].Rows[0]["price"] != DBNull.Value) ? Convert.ToString(results.Tables["Results"].Rows[0]["price"]) : null;
                item.link = Convert.ToString(results.Tables["Results"].Rows[0]["link"]);
                item.image = Convert.ToString(results.Tables["Results"].Rows[0]["image"]);
                item.pubDate = Convert.ToString(results.Tables["Results"].Rows[0]["pubDate"]);
                item.Advertiser = Convert.ToString(results.Tables["Results"].Rows[0]["Advertiser"]);
                item.Category = Convert.ToString(results.Tables["Results"].Rows[0]["Category"]);
                item.Banner = Convert.ToString(results.Tables["Results"].Rows[0]["Banner"]);
            }
            return item;
        }
        public int? DeleteActiveAdsBySource(string advertiser, string source)
        {
            Dictionary<string, dynamic> param = new Dictionary<string, dynamic>();
            if (!string.IsNullOrEmpty(advertiser))
                param.Add("advertiser", advertiser);
            if (!string.IsNullOrEmpty(source))
                param.Add("source", source);
            _con = new SqlConnection(_connectionString); _con.Open();
            _repository = new Repository(_con, _log);
            var results = Repository.PostData(_con, _log, "[dbo].[DeleteActiveAds]", param);
            return results;
        }
        public int? DeleteActiveAdsByTitleLinkAndImage(string title, string link, string image)
        {
            Dictionary<string, dynamic> param = new Dictionary<string, dynamic>();
            param.Add("title", title);
            param.Add("link", link);
            param.Add("image", image);
            _con = new SqlConnection(_connectionString); _con.Open();
            _repository = new Repository(_con, _log);
            var results = Repository.PostData(_con, _log, "[dbo].[DeleteActiveAdByTitleLinkandImage]", param);
            return results;
        }

        public int? DeleteActiveAdNotInAdvertising()
        {
            _con = new SqlConnection(_connectionString); _con.Open();
            _repository = new Repository(_con, _log);
            var results = Repository.RunCommand(_con, _log, "[dbo].[DeleteActiveAdNotInAdvertising]");
            return results;
        }

        public int? DeleteItem(string title, string link, string NewsId)
        {
            Dictionary<string, dynamic> param = new Dictionary<string, dynamic>();
            param.Add("title", title);
            param.Add("link", link);
            param.Add("Id", NewsId);
            _repository = new Repository(_con, _log);
            var results = Repository.PostData(_con, _log, "[dbo].[DeleteAdvertising]", param);
            return results;
        }

        public int? DeleteByAdvertiser(string advertiser)
        {
            Dictionary<string, dynamic> param = new Dictionary<string, dynamic>();
            param.Add("advertiser", advertiser);
            _con = new SqlConnection(_connectionString); _con.Open();
            _repository = new Repository(_con, _log);
            var results = Repository.PostData(_con, _log, "[dbo].[DeleteAdvertising]", param);
            return results;
        }

        public RssModel GetAdsByBanner(string banner)
        {
            Dictionary<string, dynamic> param = new Dictionary<string, dynamic>();
            param.Add("BannerId", banner);
            RssModel rss = new RssModel() { source = 545544, title = "Active Ads", items = new List<RssModelItems>() };
            try
            {
                _con = new SqlConnection(_connectionString); _con.Open();
                _repository = new Repository(_con, _log);
                DataSet results = _repository.GetData("[dbo].[GetAdsByBanners]", param);
                if (results != null && results.Tables["Results"] != null && results.Tables["Results"].Rows.Count > 0)
                    foreach (DataRow row in results.Tables["Results"].Rows)
                    {
                        rss.items.Add(new RssModelItems()
                        {
                            NewsId = Convert.ToString(row["Id"]),
                            title = Convert.ToString(row["title"]),
                            source = Convert.ToString(row["source"]),
                            price = (row["price"] != DBNull.Value) ? Convert.ToString(row["price"]) : null,
                            link = Convert.ToString(row["link"]),
                            image = Convert.ToString(row["image"]),
                            pubDate = Convert.ToString(row["pubDate"]),
                            Advertiser = Convert.ToString(row["Advertiser"]),
                            Category = Convert.ToString(row["Category"]),
                            Banner = Convert.ToString(row["Banner"])
                        });
                    }
                return rss;
            }
            catch (Exception ex)
            { _log?.LogError(ex.Source + " : " + ex.StackTrace + " : " + ex.InnerException?.Message + " : " + ex.Message);
                return rss;
            }
        }

        public RssModel Search(string text, int page, int pagination, bool IsAdmin)
        {
            RssModel rss = new RssModel() { source = 343344, title = "Search Results", items = new List<RssModelItems>() };
            try
            {
                Dictionary<string, dynamic> param = new Dictionary<string, dynamic>();
                param.Add("SearchTxt", text);
                param.Add("Pagination", pagination);
                param.Add("Page", page);
                param.Add("IsAdmin", IsAdmin);
                //param.Add("TotalFound", 0);
                SqlParameter output = new SqlParameter("@TotalFound", 0) { DbType = DbType.Int32, Direction = ParameterDirection.Output };
                _con = new SqlConnection(_connectionString); _con.Open();
                _repository = new Repository(_con, _log);
                DataSet results = _repository.GetData("[dbo].[SearchAdvertising]", param, ref output);
                rss.totalFound = Convert.ToInt32(output.Value);
                if (results != null && results.Tables["Results"] != null && results.Tables["Results"].Rows.Count > 0)
                    foreach (DataRow row in results.Tables["Results"].Rows)
                    {
                        rss.items.Add(new RssModelItems()
                        {
                            NewsId = Convert.ToString(row["Id"]),
                            title = Convert.ToString(row["title"]),
                            source = Convert.ToString(row["source"]),
                            price = (row["price"] != DBNull.Value) ? Convert.ToString(row["price"]) : null,
                            link = Convert.ToString(row["link"]),
                            image = Convert.ToString(row["image"]),
                            pubDate = Convert.ToString(row["pubDate"]),
                            Advertiser = Convert.ToString(row["Advertiser"]),
                            Category = Convert.ToString(row["Category"]),
                            Banner = Convert.ToString(row["Banner"]),
                            DateCreated = Convert.ToDateTime(row["DateCreated"]),
                            IsActiveAd = (row["IsActiveAd"] == DBNull.Value) ? false : Convert.ToBoolean(row["IsActiveAd"]),
                        });
                    }
                return rss;
            }
            catch (Exception ex)
            {
                _log?.LogError(ex.Source + " : " + ex.StackTrace + " : " + ex.InnerException?.Message + " : " + ex.Message);
                return rss;
            }
        }

        public RssModel SearchNews(string text, int page, int pagination)
        {
            RssModel rss = new RssModel() { source = 343344, title = "Search Results", items = new List<RssModelItems>() };
            try
            {
                Dictionary<string, dynamic> param = new Dictionary<string, dynamic>();
                param.Add("SearchTxt", text);
                param.Add("Pagination", pagination);
                param.Add("Page", page);
                //param.Add("TotalFound", 0);
                SqlParameter output = new SqlParameter("@TotalFound", 0) { DbType = DbType.Int32, Direction = ParameterDirection.Output };
                _con = new SqlConnection(_connectionString); _con.Open();
                _repository = new Repository(_con, _log);
                DataSet results = _repository.GetData("[dbo].[SearchNews]", param, ref output);
                rss.totalFound = Convert.ToInt32(output.Value);
                if (results != null && results.Tables["Results"] != null && results.Tables["Results"].Rows.Count > 0)
                    foreach (DataRow row in results.Tables["Results"].Rows)
                    {
                        rss.items.Add(new RssModelItems()
                        {
                            NewsId = Convert.ToString(row["Id"]),
                            title = Convert.ToString(row["title"]),
                            source = Convert.ToString(row["source"]),
                            price = (row["price"] != DBNull.Value) ? Convert.ToString(row["price"]) : null,
                            link = Convert.ToString(row["link"]),
                            image = Convert.ToString(row["image"]),
                            pubDate = Convert.ToString(row["pubDate"]),
                            Advertiser = Convert.ToString(row["Advertiser"]),
                            Category = Convert.ToString(row["Category"]),
                            Banner = Convert.ToString(row["Banner"]),
                            DateCreated = Convert.ToDateTime(row["DateCreated"])
                        });
                    }
                return rss;
            }
            catch (Exception ex)
            {
                _log?.LogError(ex.Source + " : " + ex.StackTrace + " : " + ex.InnerException?.Message + " : " + ex.Message);
                return rss;
            }
        }

        public bool SaveSearch(string text, string userId)
        {
            bool success = true;
            try
            {
                Dictionary<string, dynamic> param = new Dictionary<string, dynamic>();
                //param.Add("Source", 1);
                param.Add("userId", userId);
                param.Add("SearchText", text);
                _con = new SqlConnection(_connectionString); _con.Open();
                _repository = new Repository(_con, _log);
                var results = Repository.PostData(_con, _log, "[dbo].[SaveSearchHistory]", param);
            }
            catch (Exception ex)
            {
                _log?.LogError(ex.Source + " : " + ex.StackTrace + " : " + ex.InnerException?.Message + " : " + ex.Message);
                success = false;
            }
            return success;
        }

        public int? AddBulk(string table, DataRow[] dataRows)
        {
            int? results = 0;
            _con = new SqlConnection(_connectionString);
            Repository.BulkInserter(table, _con, _log, dataRows);
            return results;
        }
        public int? AddBulk(string table, List<RssModelItems> items)
        {
            int? results = 0;
            _con = new SqlConnection(_connectionString);
            Repository.BulkInserter(table, _con, _log, items);
            return results;
        }

        public bool SendEmail(string senderName, string senderEmail, string senderPassword, string name, string[] emails, string subject, string body, string server, int port)
        {
            bool success = true;
            using (MimeMessage emailMessage = new MimeMessage())
            {
                MailboxAddress emailFrom = new MailboxAddress(senderName, senderEmail);
                emailMessage.From.Add(emailFrom);
                foreach(string email in emails)
                {
                    MailboxAddress emailTo = new MailboxAddress(name, email);
                    emailMessage.To.Add(emailTo);
                }

                //emailMessage.Cc.Add(new MailboxAddress("Cc Receiver", "cc@example.com"));
                //emailMessage.Bcc.Add(new MailboxAddress("Bcc Receiver", "bcc@example.com"));

                emailMessage.Subject = subject;

                BodyBuilder emailBodyBuilder = new BodyBuilder();
                emailBodyBuilder.HtmlBody = body;

                emailMessage.Body = emailBodyBuilder.ToMessageBody();
                //this is the SmtpClient from the Mailkit.Net.Smtp namespace, not the System.Net.Mail one
                using (SmtpClient mailClient = new SmtpClient())
                {
                    //mailClient.Connect(server, port, MailKit.Security.SecureSocketOptions.StartTls);
                    mailClient.Connect(server, port, true);
                    mailClient.Authenticate(senderEmail, senderPassword);
                    mailClient.Send(emailMessage);
                    mailClient.Disconnect(true);
                }
            }
            return success;
        }

        #region TraderGo
        //public static async Task<Results<Tickers>> Search(string symbol)
        //public Results<Tickers> Search(string symbol)
        public string Search(string symbol)
        {
            dynamic error = null;
            //Results<Tickers> results = new Results<Tickers>();
            try
            {
                return GeneralHelper<string>.GetStringRequest(new Dictionary<string, string>() { { "Authorization", "Bearer " + _polygon.PolygonToken } }, _polygon.Url + _polygon.Search.Replace("{symbol}", symbol), _client, ref error);
            }
            catch (Exception ex)
            {
                error = ex.Source + " " + ex.StackTrace + " " + ex?.InnerException?.Message + " " + ex.Message;
            }            
            return string.Empty;
        }

        //public static async Task<Quote> GetQuote(string symbol)
        //public Quote GetQuote(string symbol)
        public string GetQuote(string symbol)
        {
            dynamic error = null;
            //Quote results = new Quote();
            try
            {
                return GeneralHelper<string>.GetStringRequest(new Dictionary<string, string>() { { "X-Finnhub-Token", _finnhub.finnhubKey } }, _finnhub.URL + _finnhub.Quote.Replace("{symbol}", symbol), _client, ref error);
            }
            catch (Exception ex)
            {
                error = ex.Source + " " + ex.StackTrace + " " + ex?.InnerException?.Message + " " + ex.Message;
            }
            return string.Empty;
        }

        //public Result GetProfile(string symbol)
        public string GetProfile(string symbol)
        {
            dynamic error = null;
            //Result results = new Result();
            try
            {
                //results = GeneralHelper<dynamic>.GetTypeRequest(new Dictionary<string, string>() { { "X-Finnhub-Token", _finnhub.finnhubKey } }, _finnhub.URL + _finnhub.Profile.Replace("{symbol}", symbol), _client, ref error);
                return GeneralHelper<string>.GetStringRequest(new Dictionary<string, string>() { { "Authorization", "Bearer " + _polygon.PolygonToken } }, _polygon.Url + _polygon.Profile.Replace("{ticker}", symbol), _client, ref error);
            }
            catch (Exception ex)
            {
                error = ex.Source + " " + ex.StackTrace + " " + ex?.InnerException?.Message + " " + ex.Message;
            }
            return string.Empty;
        }

        //public Results<History> GetHistory(string symbol, string fromDate, string toDate)
        public string GetHistory(string symbol, string fromDate, string toDate)
        {
            dynamic error = null;
            //Results<History> results = new Results<History>();
            try
            {
                return GeneralHelper<string>.GetStringRequest(new Dictionary<string, string>() { { "Authorization", "Bearer " + _polygon.PolygonToken } }, _polygon.Url + _polygon.History.Replace("{symbol}", symbol).Replace("{from}", fromDate).Replace("{to}", toDate).Replace("{multiplier}", "1").Replace("{timespan}", "day"), _client, ref error);
            }
            catch (Exception ex)
            {
                error = ex.Source + " " + ex.StackTrace + " " + ex?.InnerException?.Message + " " + ex.Message;
            }
            return string.Empty;
        }
        //public List<News> GetCompanyNews(string symbol, string fromDate, string toDate)
        public string GetCompanyNews(string symbol, string fromDate, string toDate)
        {
            dynamic error = null;
            //List<News> results = new List<News>();
            try
            {
                return GeneralHelper<string>.GetStringRequest(new Dictionary<string, string>() { { "X-Finnhub-Token", _finnhub.finnhubKey } }, _finnhub.URL + _finnhub.News.Replace("{symbol}", symbol).Replace("{fromDate}", fromDate).Replace("{toDate}", toDate), _client, ref error);
            }
            catch (Exception ex)
            {
                error = ex.Source + " " + ex.StackTrace + " " + ex?.InnerException?.Message + " " + ex.Message;
            }
            return string.Empty;
        }
        public dynamic GetGroupDaily(string date, ref dynamic error)
        {
            dynamic results = new ExpandoObject();
            try
            {
                results = GeneralHelper<dynamic>.GetTypeRequest(new Dictionary<string, string>() { { "Authorization", "Bearer " + _polygon.PolygonToken } }, _polygon.Url + _polygon.GroupDaily.Replace("{date}", date), _client, ref error);
            }
            catch (Exception ex)
            {
                error = ex.Source + " " + ex.StackTrace + " " + ex?.InnerException?.Message + " " + ex.Message;
            }
            return results;
        }

        #endregion
        public dynamic GetStackSocialFeeds(ref dynamic error)
        {
            dynamic results = new ExpandoObject();
            try
            {
                results = GeneralHelper<dynamic>.GetTypeRequest(new Dictionary<string, string>(), "https://stacksocial.com/feed?aid=a-d6qvcjh3", _client, ref error);
            }
            catch (Exception ex)
            {
                error = ex.Source + " " + ex.StackTrace + " " + ex?.InnerException?.Message + " " + ex.Message;
            }
            return results;
        }
    }
    }
//}
