using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Dynamic;
using TMD.Database.Connect.Managers;
using TMD.Database.Connect.Helper;
using System.Text.Json;
using TMDInvestments.Models;
using Microsoft.Extensions.Options;
using TMDInvestments.Services;
using Newtonsoft.Json.Linq;

namespace TMD.Services.Service
{
    public sealed class TraderGoService
    {
        SchwabSettings _settings;
        private IOptions<FinnHub> _finnHub;
        private IOptions<Polygon> _polygon;
        private readonly IHttpClientFactory? _factory;
        dynamic yfapi = new ExpandoObject();
        //dynamic polygon = new ExpandoObject();
        public static dynamic finnhub = new ExpandoObject();
        private static HttpClient? _client;
        dynamic? error = null;
        private static ILogger? _log;
        //private Manager? NewsManager;
        private Manager? MarketsManager;
        //string? conn = string.Empty;
        string? conn2 = string.Empty;
        private static IConfiguration? connection;
        //List<DataRow>? dataRows;
        DataTable? dt;
        static TraderGoService()
        {
            finnhub.GeneralNews = "/news?category=general";
            finnhub.finnhubKey = "cjbc5i9r01qn9c1l9ncgcjbc5i9r01qn9c1l9nd0";
            finnhub.URL = "https://finnhub.io/api/v1";
        }
        public TraderGoService(ILogger log, HttpClient client, bool isAzureFunction)
        {
            yfapi.URL = "https://yfapi.net";
            yfapi.MarketSummary = "/v6/finance/quote/marketSummary?lang=en&region=US";
            //yfapi.Trending = "/v1/finance/trending/US";
            yfapi.GainersLosers = "/ws/screeners/v1/finance/screener/predefined/saved?count=30&scrIds={direction}";
            yfapi.yfapiKey = "dJpHrhx6GG9aLvvqoSL0l8QliXlxQbjt55I0YLPY";

            //polygon.URL = "https://api.polygon.io";
            //polygon.GainersLosers = "/v2/snapshot/locale/us/markets/stocks/{direction}?include_otc=false";
            //polygon.polygonToken = "nZ6BWDvn_sGspFnsHKfdGBOpGnTI43RD";

            finnhub.URL = "https://finnhub.io/api/v1";
            finnhub.IPOCalendar = "/calendar/ipo?from={from}&to={to}";
            finnhub.Earnings = "/calendar/earnings?from={from}&to={to}";
            finnhub.finnhubKey = "cjbc5i9r01qn9c1l9ncgcjbc5i9r01qn9c1l9nd0";

            _log = log;
            _client = client;
            if (!isAzureFunction)
            {
                IConfigurationBuilder builder = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json");
                IConfigurationRoot configuration = builder.Build();
                connection = configuration.GetSection("ConnectionStrings");
                conn2 = connection.GetSection("connection").Value;
            }
            else
            {
                //conn = Environment.GetEnvironmentVariable("ConnectionString");
                conn2 = Environment.GetEnvironmentVariable("ConnectionString2");
            }
            //NewsManager = new Manager(conn, "", _log);
            MarketsManager = new Manager(conn2, "", _log);
        }
        public TraderGoService(ILogger log, HttpClient client, bool isAzureFunction,IOptions<FinnHub> finnHub, IOptions<Polygon> polycon, IHttpClientFactory? httpClientFactory, IOptions<SchwabSettings> settings)
        {
            _log = log;
            _client = client;
            _finnHub = finnHub;
            _polygon = polycon;
            _factory = httpClientFactory;
            _settings = settings.Value;
            if (!isAzureFunction)
            {
                IConfigurationBuilder builder = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json");
                IConfigurationRoot configuration = builder.Build();
                connection = configuration.GetSection("ConnectionStrings");
                conn2 = connection.GetSection("connection").Value;
            }
            else
            {
                //conn = Environment.GetEnvironmentVariable("ConnectionString");
                conn2 = Environment.GetEnvironmentVariable("ConnectionString2");
            }
        }
        public dynamic? GetMarkets()
        {
            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("X-API-KEY", yfapi.yfapiKey.ToString());
            var marketSummary = GeneralHelper.GetRequest(headers, yfapi.URL + yfapi.MarketSummary.ToString(), _client, error);
            if (marketSummary is JsonElement)
                return marketSummary.GetProperty("marketSummaryResponse").GetProperty("result")?.EnumerateArray();
            else 
                return null;
        }
        public dynamic GetGainersandLosers()
        {
            dynamic GainersandLosers = new ExpandoObject();
            GainersandLosers.Gainers = new ExpandoObject();
            GainersandLosers.Losers = new ExpandoObject();
            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("X-API-KEY", yfapi.yfapiKey.ToString());
            var losers = GeneralHelper.GetRequest(headers, yfapi.URL.ToString() + yfapi.GainersLosers.ToString().Replace("{direction}", "day_losers"), _client, error);
            var gainers = GeneralHelper.GetRequest(headers, yfapi.URL.ToString() + yfapi.GainersLosers.ToString().Replace("{direction}", "day_gainers"), _client, error);
            if (gainers is JsonElement)
            GainersandLosers.Gainers = gainers.GetProperty("finance").GetProperty("result")[0].GetProperty("quotes")?.EnumerateArray();
            if (losers is JsonElement)
            GainersandLosers.Losers = losers.GetProperty("finance").GetProperty("result")[0].GetProperty("quotes")?.EnumerateArray();
            return GainersandLosers;
        }
        DateTime fromDate = DateTime.Now;
        DateTime toDate = DateTime.Now.AddDays(35);
        public dynamic GetIPOs()
        {
            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers = new Dictionary<string, string>();
            headers.Add("X-Finnhub-Token", finnhub.finnhubKey.ToString());
            var ipos = GeneralHelper.GetRequest(headers, finnhub.URL.ToString() + finnhub.IPOCalendar.ToString().Replace("{from}", fromDate.Year + "-" + fromDate.Month + "-" + fromDate.Day).Replace("{to}", toDate.Year + "-" + toDate.Month + "-" + toDate.Day), _client, error);
            return ipos.GetProperty("ipoCalendar")?.EnumerateArray();
        }
        public dynamic GetEarnings()
        {
            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers = new Dictionary<string, string>();
            headers.Add("X-Finnhub-Token", finnhub.finnhubKey.ToString());
            _log.LogInformation("Getting Earnings Calender");
            var earnings = GeneralHelper.GetRequest(headers, finnhub.URL.ToString() + finnhub.Earnings.ToString().Replace("{from}", fromDate.Year + "-" + fromDate.Month + "-" + fromDate.Day).Replace("{to}", toDate.Year + "-" + toDate.Month + "-" + toDate.Day), _client, error);
            return earnings.GetProperty("earningsCalendar")?.EnumerateArray();
        }

        public dynamic GetNews()
        {
            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("X-Finnhub-Token", TraderGoService.finnhub.finnhubKey.ToString());
            var news = GeneralHelper.GetRequest(headers, TraderGoService.finnhub.URL + TraderGoService.finnhub.GeneralNews, _client, error);
            return news?.EnumerateArray();
        }
        public void StartRequests()
        {
            _log.LogInformation("Truncate stock Info");
            MarketsManager.TruncateMarkets();
            Dictionary<string, string> headers;// = new Dictionary<string, string>();
            headers = new Dictionary<string, string>();
            _log.LogInformation("Getting Market Info");
            headers.Add("X-API-KEY", yfapi.yfapiKey.ToString());
            var marketSummary = GeneralHelper.GetRequest(headers, yfapi.URL + yfapi.MarketSummary.ToString(), _client, error);
            var marketresponse = marketSummary.GetProperty("marketSummaryResponse").GetProperty("result")?.EnumerateArray();
            dt = new DataTable();
            GeneralHelper.AddColumnstoTable(new string[] { "Exchange", "Name", "Symbol", "MarketPrice", "PreviousClose", "MarketChange", "MarketChangePercent", "MarketTime" }, ref dt);
            foreach (var item in marketresponse)
            {            
                try
                {
                    DataRow dr = dt.NewRow();
                    dr["Exchange"] = item.GetProperty("exchange").ToString();
                    dr["Name"] = item.GetProperty("shortName").ToString();
                    dr["Symbol"] = item.GetProperty("symbol").ToString();
                    dr["MarketPrice"] = Convert.ToDecimal(item.GetProperty("regularMarketPrice").GetProperty("raw").ToString());
                    dr["PreviousClose"] = Convert.ToDecimal(item.GetProperty("regularMarketPreviousClose").GetProperty("raw").ToString());
                    dr["MarketChange"] = Convert.ToDecimal(item.GetProperty("regularMarketChange").GetProperty("fmt").ToString());
                    dr["MarketChangePercent"] = Convert.ToDecimal(item.GetProperty("regularMarketChangePercent").GetProperty("raw").ToString());
                    var offset = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(item.GetProperty("regularMarketTime").GetProperty("raw").ToString()));
                    dr["MarketTime"] = Convert.ToDateTime(offset.LocalDateTime.ToString());
                    dt.Rows.Add(dr);
                }
                catch (Exception ex)
                {
                    _log.LogError(ex.Source + " : " + ex.StackTrace + " : " + ex.InnerException?.Message + " : " + ex.Message);
                }
            }

            MarketsManager.AddBulk("[dbo].[Markets]", dt.Select());

            //var trends = GetRequest(headers, yfapi.URL + yfapi.Trending.ToString());
            //headers = new Dictionary<string, string>();
            //headers.Add("Authorization", "Bearer " + polygon.polygonToken.ToString());
            _log.LogInformation("Getting Gainers and Losers");
            var gainers = GeneralHelper.GetRequest(headers, yfapi.URL.ToString() + yfapi.GainersLosers.ToString().Replace("{direction}", "day_losers"), _client, error);
            var losers = GeneralHelper.GetRequest(headers, yfapi.URL.ToString() + yfapi.GainersLosers.ToString().Replace("{direction}", "day_gainers"), _client, error);
            dt = new DataTable();
            GeneralHelper.AddColumnstoTable(new string[] {"regularMarketChangePercent",
            //"averageAnalystRating",
            "regularMarketChange",
            "regularMarketTime",
            "regularMarketPrice",
            "regularMarketDayHigh",
            "regularMarketDayLow",
            "regularMarketVolume",
            "regularMarketPreviousClose",
            "shortName",
            "regularMarketOpen",
            "fiftyTwoWeekLowChange",
            "fiftyTwoWeekLowChangePercent",
            "fiftyTwoWeekRange",
            "fiftyTwoWeekHighChange",
            "fiftyTwoWeekHighChangePercent",
            "fiftyTwoWeekLow",
            "fiftyTwoWeekHigh",
            //"earningsTimestamp",
            "marketCap",
            "symbol",
            "direction"}, ref dt);
            var gainersArray = gainers.GetProperty("finance").GetProperty("result")?.EnumerateArray();
            foreach (var gainer in gainersArray)
            {
                foreach (var item in gainer.GetProperty("quotes")?.EnumerateArray())
                {
                    DataRow dr = dt.NewRow();
                    try
                    {
                        dr["regularMarketChangePercent"] = Convert.ToDecimal(item.GetProperty("regularMarketChangePercent").ToString());
                        //dr["averageAnalystRating"] = item.GetProperty("averageAnalystRating").ToString();
                        dr["regularMarketChange"] = Convert.ToDecimal(item.GetProperty("regularMarketChange").ToString());
                        //dr["regularMarketTime"] = item.GetProperty("regularMarketTime").ToString();
                        var offset = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(item.GetProperty("regularMarketTime").ToString()));
                        dr["regularMarketTime"] = offset.LocalDateTime.ToString();
                        dr["regularMarketPrice"] = Convert.ToDecimal(item.GetProperty("regularMarketPrice").ToString());
                        dr["regularMarketDayHigh"] = Convert.ToDecimal(item.GetProperty("regularMarketDayHigh").ToString());
                        dr["regularMarketDayLow"] = Convert.ToDecimal(item.GetProperty("regularMarketDayLow").ToString());
                        dr["regularMarketVolume"] = Convert.ToInt64(item.GetProperty("regularMarketVolume").ToString());
                        dr["regularMarketPreviousClose"] = Convert.ToDecimal(item.GetProperty("regularMarketPreviousClose").ToString());
                        dr["shortName"] = item.GetProperty("shortName").ToString();
                        dr["regularMarketOpen"] = Convert.ToDecimal(item.GetProperty("regularMarketOpen").ToString());
                        dr["fiftyTwoWeekLowChange"] = Convert.ToDecimal(item.GetProperty("fiftyTwoWeekLowChange").ToString());
                        dr["fiftyTwoWeekLowChangePercent"] = Convert.ToDecimal(item.GetProperty("fiftyTwoWeekLowChangePercent").ToString());
                        dr["fiftyTwoWeekRange"] = Convert.ToString(item.GetProperty("fiftyTwoWeekRange").ToString());
                        dr["fiftyTwoWeekHighChange"] = Convert.ToDecimal(item.GetProperty("fiftyTwoWeekHighChange").ToString());
                        dr["fiftyTwoWeekHighChangePercent"] = Convert.ToDecimal(item.GetProperty("fiftyTwoWeekHighChangePercent").ToString());
                        dr["fiftyTwoWeekLow"] = Convert.ToDecimal(item.GetProperty("fiftyTwoWeekLow").ToString());
                        dr["fiftyTwoWeekHigh"] = Convert.ToDecimal(item.GetProperty("fiftyTwoWeekHigh").ToString());
                        //dr["earningsTimestamp"] = item.GetProperty("earningsTimestamp").ToString();
                        dr["marketCap"] = Convert.ToDecimal(item.GetProperty("marketCap").ToString());
                        dr["symbol"] = item.GetProperty("symbol").ToString();
                        dr["direction"] = "losers";
                        dt.Rows.Add(dr);
                    }
                    catch (Exception ex)
                    {
                        _log.LogError(ex.Source + " : " + ex.StackTrace + " : " + ex.InnerException?.Message + " : " + ex.Message);
                    }
                }
            }

            var losersArray = losers.GetProperty("finance").GetProperty("result")?.EnumerateArray();
            foreach (var loser in losersArray)
            {
                foreach (var item in loser.GetProperty("quotes")?.EnumerateArray())
                {
                    DataRow dr = dt.NewRow();
                    try
                    {
                        dr["regularMarketChangePercent"] = Convert.ToDecimal(item.GetProperty("regularMarketChangePercent").ToString());
                        //dr["averageAnalystRating"] = item.GetProperty("averageAnalystRating").ToString();
                        dr["regularMarketChange"] = Convert.ToDecimal(item.GetProperty("regularMarketChange").ToString());
                        //dr["regularMarketTime"] = item.GetProperty("regularMarketTime").ToString();
                        var offset = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(item.GetProperty("regularMarketTime").ToString()));
                        dr["regularMarketTime"] = offset.LocalDateTime.ToString();
                        dr["regularMarketPrice"] = Convert.ToDecimal(item.GetProperty("regularMarketPrice").ToString());
                        dr["regularMarketDayHigh"] = Convert.ToDecimal(item.GetProperty("regularMarketDayHigh").ToString());
                        dr["regularMarketDayLow"] = Convert.ToDecimal(item.GetProperty("regularMarketDayLow").ToString());
                        dr["regularMarketVolume"] = Convert.ToInt64(item.GetProperty("regularMarketVolume").ToString());
                        dr["regularMarketPreviousClose"] = Convert.ToDecimal(item.GetProperty("regularMarketPreviousClose").ToString());
                        dr["shortName"] = item.GetProperty("shortName").ToString();
                        dr["regularMarketOpen"] = Convert.ToDecimal(item.GetProperty("regularMarketOpen").ToString());
                        dr["fiftyTwoWeekLowChange"] = Convert.ToDecimal(item.GetProperty("fiftyTwoWeekLowChange").ToString());
                        dr["fiftyTwoWeekLowChangePercent"] = Convert.ToDecimal(item.GetProperty("fiftyTwoWeekLowChangePercent").ToString());
                        dr["fiftyTwoWeekRange"] = Convert.ToString(item.GetProperty("fiftyTwoWeekRange").ToString());
                        dr["fiftyTwoWeekHighChange"] = Convert.ToDecimal(item.GetProperty("fiftyTwoWeekHighChange").ToString());
                        dr["fiftyTwoWeekHighChangePercent"] = Convert.ToDecimal(item.GetProperty("fiftyTwoWeekHighChangePercent").ToString());
                        dr["fiftyTwoWeekLow"] = Convert.ToDecimal(item.GetProperty("fiftyTwoWeekLow").ToString());
                        dr["fiftyTwoWeekHigh"] = Convert.ToDecimal(item.GetProperty("fiftyTwoWeekHigh").ToString());
                        //dr["earningsTimestamp"] = item.GetProperty("earningsTimestamp").ToString();
                        dr["marketCap"] = Convert.ToDecimal(item.GetProperty("marketCap").ToString());
                        dr["symbol"] = item.GetProperty("symbol").ToString();
                        dr["direction"] = "losers";
                        dt.Rows.Add(dr);
                    }
                    catch (Exception ex)
                    {
                        _log.LogError(ex.Source + " : " + ex.StackTrace + " : " + ex.InnerException?.Message + " : " + ex.Message);
                    }
                }
            }
            MarketsManager.AddBulk("[dbo].[GainersLosers]", dt.Select());

            //_log.LogInformation("Getting IPO Calender");
            //DateTime fromDate = DateTime.Now;
            //DateTime toDate = DateTime.Now.AddDays(35);
            //headers = new Dictionary<string, string>();
            //headers.Add("X-Finnhub-Token", finnhub.finnhubKey.ToString());
            //var ipos = GeneralHelper.GetRequest(headers, finnhub.URL.ToString() + finnhub.IPOCalendar.ToString().Replace("{from}", fromDate.Year + "-" + fromDate.Month + "-" + fromDate.Day).Replace("{to}", toDate.Year + "-" + toDate.Month + "-" + toDate.Day), _client, error);
            //dt = new DataTable();
            //GeneralHelper.AddColumnstoTable(new string[] { "Symbol", "Name", "Exchange", "Price", "Status", "NumberofShares", "TotalSharesValue", "DateExpected" }, ref dt);
            //foreach (var item in ipos.GetProperty("ipoCalendar")?.EnumerateArray())
            //{
            //    DataRow dr = dt.NewRow();
            //    try
            //    {
            //        dr["Symbol"] = item.GetProperty("symbol").ToString();
            //        dr["Name"] = item.GetProperty("name").ToString();
            //        dr["Exchange"] = item.GetProperty("exchange").ToString();
            //        dr["Price"] = item.GetProperty("price").ToString();
            //        dr["Status"] = item.GetProperty("status").ToString();
            //        dr["NumberofShares"] = Convert.ToInt64(item.GetProperty("numberOfShares").ToString());
            //        dr["TotalSharesValue"] = Convert.ToInt64(item.GetProperty("totalSharesValue").ToString());
            //        dr["DateExpected"] = Convert.ToDateTime(item.GetProperty("date").ToString());
            //        dt.Rows.Add(dr);
            //    }
            //    catch (Exception ex)
            //    {
            //        _log.LogError(ex.Source + " : " + ex.StackTrace + " : " + ex.InnerException?.Message + " : " + ex.Message);
            //    }
            //}
            //MarketsManager.AddBulk("[dbo].[IPOs]", dt.Select());

            //headers = new Dictionary<string, string>();
            //headers.Add("X-Finnhub-Token", finnhub.finnhubKey.ToString());
            //_log.LogInformation("Getting Earnings Calender");
            //var earnings = GeneralHelper.GetRequest(headers, finnhub.URL.ToString() + finnhub.Earnings.ToString().Replace("{from}", fromDate.Year + "-" + fromDate.Month + "-" + fromDate.Day).Replace("{to}", toDate.Year + "-" + toDate.Month + "-" + toDate.Day), _client, error);
            //dt = new DataTable();

            //GeneralHelper.AddColumnstoTable(new string[] {"date", "epsActual", "epsEstimate", "hour", "quarter", "revenueActual", "revenueEstimate", "symbol", "year" }, ref dt);

            //foreach(var item in earnings.GetProperty("earningsCalendar")?.EnumerateArray())
            //{
            //    DataRow dr = dt.NewRow();
            //    try
            //    {
            //        dr["date"] = item.GetProperty("date").ToString();
            //        dr["epsActual"] = (string.IsNullOrEmpty(item.GetProperty("epsActual").ToString())) ? 0 : Convert.ToDecimal(item.GetProperty("epsActual").ToString());
            //        dr["epsEstimate"] = (string.IsNullOrEmpty(item.GetProperty("epsEstimate").ToString())) ? 0 : Convert.ToDecimal(item.GetProperty("epsEstimate").ToString());
            //        dr["hour"] = item.GetProperty("hour").ToString();
            //        dr["quarter"] = item.GetProperty("quarter").ToString();
            //        dr["revenueActual"] = (string.IsNullOrEmpty(item.GetProperty("revenueActual").ToString())) ? 0 : Convert.ToDecimal(item.GetProperty("revenueActual").ToString());
            //        dr["revenueEstimate"] = (string.IsNullOrEmpty(item.GetProperty("revenueEstimate").ToString())) ? 0 : Convert.ToDecimal(item.GetProperty("revenueEstimate").ToString());
            //        dr["symbol"] = item.GetProperty("symbol").ToString();
            //        dr["year"] = item.GetProperty("year").ToString();
            //        dt.Rows.Add(dr);
            //    }
            //    catch (Exception ex)
            //    {
            //        _log.LogError(ex.Source + " : " + ex.StackTrace + " : " + ex.InnerException?.Message + " : " + ex.Message);
            //    }
            //}
            //MarketsManager.AddBulk("[dbo].[Earnings]", dt.Select());
        }

        public void LoadEarnings()
        {
            List<string> addedSymbols = new List<string>();
            //dynamic? jsonQuote = null;
            MarketsManager = new Manager(conn2, _finnHub.Value, _polygon.Value, _factory);
            SchwabServices services = new SchwabServices(_log, _settings, _polygon.Value, _factory);
            MarketsManager.TruncateEarnings();
            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("X-Finnhub-Token", finnhub.finnhubKey.ToString());
            _log.LogInformation("Getting Earnings Calender");
            var earnings = GeneralHelper.GetRequest(headers, finnhub.URL.ToString() + finnhub.Earnings.ToString().Replace("{from}", fromDate.Year + "-" + fromDate.Month + "-" + fromDate.Day).Replace("{to}", toDate.Year + "-" + toDate.Month + "-" + toDate.Day), _client, error);
            dt = new DataTable();

            GeneralHelper.AddColumnstoTable(new string[] { "date", "epsActual", "epsEstimate", "hour", "quarter", "revenueActual", "revenueEstimate", "symbol", "Name", "LastEarningDate", "year" }, ref dt);

            foreach (var item in earnings.GetProperty("earningsCalendar")?.EnumerateArray())
            {
                if (addedSymbols.Contains(item.GetProperty("symbol").ToString()))
                    continue;
                DataRow dr = dt.NewRow();
                try
                {
                    addedSymbols.Add(item.GetProperty("symbol").ToString());
                    dr["date"] =  (DateTime.TryParse(item.GetProperty("date").ToString(), out DateTime value2)) ?DateTime.Parse(item.GetProperty("date").ToString()) : DateTime.Parse("1/1/1753 12:00:00 AM").ToString();
                    dr["epsActual"] = (string.IsNullOrEmpty(item.GetProperty("epsActual").ToString())) ? 0 : Convert.ToDecimal(item.GetProperty("epsActual").ToString());
                    dr["epsEstimate"] = (string.IsNullOrEmpty(item.GetProperty("epsEstimate").ToString())) ? 0 : Convert.ToDecimal(item.GetProperty("epsEstimate").ToString());
                    dr["hour"] = item.GetProperty("hour").ToString();
                    dr["quarter"] = item.GetProperty("quarter").ToString();
                    dr["revenueActual"] = (string.IsNullOrEmpty(item.GetProperty("revenueActual").ToString())) ? 0 : Convert.ToDecimal(item.GetProperty("revenueActual").ToString());
                    dr["revenueEstimate"] = (string.IsNullOrEmpty(item.GetProperty("revenueEstimate").ToString())) ? 0 : Convert.ToDecimal(item.GetProperty("revenueEstimate").ToString());
                    dr["symbol"] = item.GetProperty("symbol").ToString();
                    try
                    {
                        //jsonQuote = System.Text.Json.JsonSerializer.Serialize(services.GetQuote(item.GetProperty("symbol").ToString(), true));
                        var quote = services.GetQuote(item.GetProperty("symbol").ToString(), true);
                        if (quote != null)
                        {
                            //JObject quote = JObject.Parse(jsonQuote);
                            //dr["Name"] = quote[item?.GetProperty("symbol")?.ToString()]?.reference?.description;
                            dr["Name"] = quote["Name"]?.ToString();
                            dr["LastEarningDate"] = (DateTime.TryParse(quote[item?.GetProperty("symbol")?.ToString()]?.fundamental?.lastEarningsDate?.ToString(), out DateTime value)) ? DateTime.Parse(quote[item?.GetProperty("symbol")?.ToString()]?.fundamental?.lastEarningsDate?.ToString())?.ToShortDateString() : DateTime.Parse("1/1/1753 12:00:00 AM").ToShortDateString();
                            dr["year"] = item.GetProperty("year").ToString();
                            dt.Rows.Add(dr);
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.LogError(ex.Source + " : " + ex.StackTrace + " : " + ex.InnerException?.Message + " : " + ex.Message);
                    }


                }
                catch (Exception ex)
                {
                    _log.LogError(ex.Source + " : " + ex.StackTrace + " : " + ex.InnerException?.Message + " : " + ex.Message);
                }
            }
            MarketsManager.AddBulk("[dbo].[Earning]", dt.Select());
        }

        public void LoadIPOs()
        {
            List<string> addedSymbols = new List<string>();
            MarketsManager = new Manager(conn2, _finnHub.Value, _polygon.Value, _factory);            
            MarketsManager.TruncateIPOS();
            _log.LogInformation("Getting IPO Calender");
            DateTime fromDate = DateTime.Now;
            DateTime toDate = DateTime.Now.AddDays(35);
            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("X-Finnhub-Token", finnhub.finnhubKey.ToString());
            var ipos = GeneralHelper.GetRequest(headers, finnhub.URL.ToString() + finnhub.IPOCalendar.ToString().Replace("{from}", fromDate.Year + "-" + fromDate.Month + "-" + fromDate.Day).Replace("{to}", toDate.Year + "-" + toDate.Month + "-" + toDate.Day), _client, error);
            dt = new DataTable();
            GeneralHelper.AddColumnstoTable(new string[] { "Symbol", "Name", "Exchange", "Price", "Status", "NumberofShares", "TotalSharesValue", "DateExpected" }, ref dt);
            foreach (var item in ipos.GetProperty("ipoCalendar")?.EnumerateArray())
            {
                if (addedSymbols.Contains(item.GetProperty("symbol").ToString()))
                    continue;
                DataRow dr = dt.NewRow();
                try
                {
                    addedSymbols.Add(item.GetProperty("symbol").ToString());
                    dr["Symbol"] = item.GetProperty("symbol").ToString();
                    dr["Name"] = item.GetProperty("name").ToString();
                    dr["Exchange"] = item.GetProperty("exchange").ToString();
                    dr["Price"] = item.GetProperty("price").ToString();
                    dr["Status"] = item.GetProperty("status").ToString();
                    dr["NumberofShares"] = Convert.ToInt64(item.GetProperty("numberOfShares").ToString());
                    dr["TotalSharesValue"] = Convert.ToInt64(item.GetProperty("totalSharesValue").ToString());
                    dr["DateExpected"] = Convert.ToDateTime(item.GetProperty("date").ToString());
                    dt.Rows.Add(dr);
                }
                catch (Exception ex)
                {
                    _log.LogError(ex.Source + " : " + ex.StackTrace + " : " + ex.InnerException?.Message + " : " + ex.Message);
                }
            }
            MarketsManager.AddBulk("[dbo].[IPO]", dt.Select());
        }
    }
}
