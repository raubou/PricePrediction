using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using TMD.PricePrediction;
using TMD.Services;
using TMD.Services.Service;
using TMDInvestments.Context;
using TMDInvestments.Models;

namespace PricePredictionTest
{
    [TestClass]
    public sealed class KalshiUnitTest
    {
        ILogger? _logger;
        RulesEngine _settings = null;
        TmdContext ctx = null;
        SchwabSettings? _settings2;
        CoinBaseSettings? coinBaseSettings;
        Helper _helper = null;
        ETradeService ETradeService = null;
        ETradeSettings? _settings3;
        CoinBaseService coinBaseService = null;
        IHttpClientFactory? httpClientFactory;
        KalshiService kalshiService = null;
        [TestInitialize]
        public void GetSettings()
        {
            ctx = new TmdContext();
            //var loggerFactory = LoggerFactory.Create(builder =>
            //{
            //    //builder.AddConsole();
            //    //builder.AddDebugOutput();
            //    builder.SetMinimumLevel(LogLevel.Trace);
            //}
            //);
            var builder = new ConfigurationBuilder();
            builder.SetBasePath(Directory.GetCurrentDirectory())
                   .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            IConfiguration config = builder.Build();
            _settings2 = config.GetSection("SchwabAPI").Get<SchwabSettings>();
            coinBaseSettings = config.GetSection("CoinBase").Get<CoinBaseSettings>();
            _settings3 = config.GetSection("ETradeAPI").Get<ETradeSettings>();
            //_logger = loggerFactory.CreateLogger<KalshiUnitTest>();
            var client = new ServiceCollection().AddHttpClient("TMD")
                .UseSocketsHttpHandler((handler, _) =>
                handler.PooledConnectionLifetime = TimeSpan.FromMinutes(7)) // Recreate connection every 2 minutes
                .SetHandlerLifetime(Timeout.InfiniteTimeSpan); // Disable rotation, as it is handled by PooledConnectionLifetime
            var serviceProvider = client.Services.BuildServiceProvider();
            httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
            _settings = ctx.RulesEngine.FirstOrDefault();
            _helper = new Helper(_logger, _settings2, _settings3, null, null);
            coinBaseService = new CoinBaseService(_logger, coinBaseSettings/*, _taApiSettings*/, httpClientFactory/*, helper*/);
            kalshiService = new KalshiService(_logger, httpClientFactory);
        }
        [TestMethod]
        public void TestKalshiSeries()
        {
            List<dynamic> marketList = new List<dynamic>();
            string[] title = { "Above/below", "15 Minute" };
            List<dynamic> marketPositionTickers = new List<dynamic>();
            try
            {
                Debug.WriteLine("Get balance and positions {0}", DateTime.Now);
                var balance = kalshiService.SendRequest(HttpMethod.Get, "/trade-api/v2/portfolio/balance", "", true);
                var positions = kalshiService.SendRequest(HttpMethod.Get, "/trade-api/v2/portfolio/positions", "", true);
                decimal balanceDollars = Convert.ToDecimal(balance.GetProperty("balance_dollars").GetString());
                if (balanceDollars <= 1)
                {
                    Helper.Log(_logger, LogLevel.Warning, $"Low balance available: {balanceDollars}");
                    Debug.WriteLine($"Low balance available: {balanceDollars}");
                    System.Threading.Thread.Sleep(100);
                    return;
                }
                Helper.Log(_logger, LogLevel.Information, $"Balance: {balanceDollars}");
                Debug.WriteLine($"Balance: {balanceDollars}");
                var marketPositions = (IEnumerable<JsonElement>)positions.GetProperty("market_positions").EnumerateArray();
                //var orderList = (IEnumerable<JsonElement>)orders.GetProperty("orders").EnumerateArray();
                foreach (var marketPosition in marketPositions)
                {
                    marketPositionTickers.Add(marketPosition.GetProperty("ticker").GetString());
                }
                Debug.WriteLine($"Getting series for Crypto");
                var results = kalshiService.SendRequest(HttpMethod.Get, "/trade-api/v2/series?category=Crypto", "", false);
                var series = (IEnumerable<JsonElement>)results.GetProperty("series").EnumerateArray();//.GetProperty("category");
                                                                                                      //var seriesList = series.ToList(); 
                var categories = series.Where(x => x.GetProperty("category").GetString() == "Crypto" && (x.GetProperty("frequency").GetString() == "fifteen_min" || x.GetProperty("frequency").GetString() == "hourly") && (x.GetProperty("title").GetString().Contains("Above/below") || x.GetProperty("title").GetString().Contains("15 Minute"))).ToList();

                var tickerandTitle = categories.Select(x => new { SeriesTicker = x.GetProperty("ticker").GetString(), Title = x.GetProperty("title").GetString(), Frequency = x.GetProperty("frequency").GetString() }).ToList();
                //var tickerandTitle = series.Select(x => new { SeriesTicker = x.GetProperty("ticker").GetString(), Title = x.GetProperty("title").GetString() }).ToList();
                IEnumerable<JsonElement> events = null;
                Debug.WriteLine($"Getting events from series");
                foreach (var item in tickerandTitle)
                {                    
                    var results2 = kalshiService.SendRequest(HttpMethod.Get, $"/trade-api/v2/events?series_ticker={item.SeriesTicker}", "", false);
                    try
                    {
                        //var getEvents = results2.GetProperty("events").EnumerateArray();
                        if (results2 is System.Dynamic.ExpandoObject)
                        {
                            Helper.Log(_logger, LogLevel.Warning, $"Events for series {item.SeriesTicker} is an ExpandoObject, skipping. {results2.errors.Details}");
                            Debug.WriteLine($"Events for series {item.SeriesTicker} is an ExpandoObject, skipping. {results2.errors.Details}");
                            System.Threading.Thread.Sleep(100);
                            continue;
                        }
                        events = (IEnumerable<JsonElement>)results2.GetProperty("events").EnumerateArray();
                        events = events.Where(x => x.GetProperty("strike_date").GetDateTime().Date == DateTime.Today).ToList();
                    }
                    catch (Exception ex)
                    {
                        Helper.Log(_logger, LogLevel.Warning, $"An error occurred while fetching events for series {item.SeriesTicker}: {ex.Message}");
                        Debug.WriteLine($"An error occurred while fetching events for series {item.SeriesTicker}: {ex.Message}");
                        System.Threading.Thread.Sleep(100);
                        continue;
                    }
                    var eventandTitle = events.Select(x => new { Strikedate = x.GetProperty("strike_date").GetString(), EventTicker = x.GetProperty("event_ticker").GetString(), Title = x.GetProperty("title").GetString(), SubTitle = x.GetProperty("sub_title").GetString() }).ToList();
                    IEnumerable<JsonElement> markets = null;
                    foreach (var item2 in eventandTitle)
                    {
                        Debug.WriteLine($"Getting markets for event {item2.EventTicker}");
                        var results3 = kalshiService.SendRequest(HttpMethod.Get, $"/trade-api/v2/markets?event_ticker={item2.EventTicker}", "", false);
                        try
                        {
                            //var getMarkets = results3.GetProperty("markets").EnumerateArray();
                            if (results3 is System.Dynamic.ExpandoObject)
                            {
                                Helper.Log(_logger, LogLevel.Warning, $"Markets for event {item2.EventTicker} is an ExpandoObject, skipping. {results3.errors.Details}");
                                Debug.WriteLine($"Markets for event {item2.EventTicker} is an ExpandoObject, skipping. {results3.errors.Details}");
                                System.Threading.Thread.Sleep(100);
                                continue;
                            }
                            markets = (IEnumerable<JsonElement>)results3.GetProperty("markets").EnumerateArray();
                        }
                        catch (Exception ex)
                        {
                            Helper.Log(_logger, LogLevel.Warning, $"An error occurred while fetching markets for event {item2.EventTicker}: {ex.Message}");
                            Debug.WriteLine($"An error occurred while fetching markets for event {item2.EventTicker}: {ex.Message}");
                            System.Threading.Thread.Sleep(100);
                            continue;
                        }

                        foreach (var item3 in markets)
                        {
                            if (!item3.GetProperty("title").GetString().ToLower().Contains("price up") && !item3.GetProperty("title").GetString().ToLower().Contains("price down") && !item3.GetProperty("title").GetString().ToLower().Contains("price on") && !item3.GetProperty("title").GetString().ToLower().Contains("price at"))
                            {
                                Debug.WriteLine($"Market {item3.GetProperty("title").GetString()} does not match criteria");
                                System.Threading.Thread.Sleep(100);
                                continue;
                            }
                            Helper.Log(_logger, LogLevel.Information, $"Processing market: {item3.GetProperty("title").GetString()}");
                            Debug.WriteLine($"Processing market: {item3.GetProperty("title").GetString()}");
                            var marketandTitle = markets.Where(x => x.GetProperty("title").GetString() == item3.GetProperty("title").GetString())
                                .Select(x => new
                                { /*floor_strike = x.GetProperty("floor_strike").GetString(),*/
                                    status = x.GetProperty("status").GetString(),
                                    CloseTime = x.GetProperty("close_time").GetString(),
                                    MarketTicker = x.GetProperty("ticker").GetString(),
                                    Title = x.GetProperty("title").GetString(),
                                    yes_ask_dollars = x.GetProperty("yes_ask_dollars").GetString(),
                                    yes_bid_dollars = x.GetProperty("yes_bid_dollars").GetString(),
                                    no_ask_dollars = x.GetProperty("no_ask_dollars").GetString(),
                                    no_bid_dollars = x.GetProperty("no_bid_dollars").GetString(),
                                    yes_sub_title = x.GetProperty("yes_sub_title").GetString(),
                                }).FirstOrDefault();
                            if (marketPositionTickers.Contains(marketandTitle.MarketTicker) || marketList.Count() > 5)
                            {
                                Helper.Log(_logger, LogLevel.Information, $"Not buying: {item3.GetProperty("title").GetString()} Already in the books or limit reached");
                                Debug.WriteLine($"Not buying: {item3.GetProperty("title").GetString()} Already in the books or limit reached");
                                System.Threading.Thread.Sleep(100);
                                continue;
                            }

                            NameValueCollection orderbookYesDollarsDic = new NameValueCollection();
                            NameValueCollection orderbookNoDollarsDic = new NameValueCollection();
                            Debug.WriteLine($"Getting orderbook for market: {marketandTitle.Title}");
                            var results5 = kalshiService.SendRequest(HttpMethod.Get, $"/trade-api/v2/markets/{marketandTitle.MarketTicker}/orderbook", "", false);
                            if (results5 is JsonElement)
                            {

                                DateTime closeTime = DateTime.Parse(marketandTitle.CloseTime.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
                                //var diffCLoseandNow = Math.Abs((closeTime - DateTime.UtcNow).TotalMinutes);
                                //if (diffCLoseandNow < 10)
                                if (Math.Abs((closeTime - DateTime.UtcNow).TotalMinutes) <= 10)
                                {
                                    Helper.Log(_logger, LogLevel.Information, $"Not buying: {item3.GetProperty("title").GetString()} Closing in less than 10 minutes");
                                    Debug.WriteLine($"Not buying: {item3.GetProperty("title").GetString()} Closing in less than 10 minutes");
                                    System.Threading.Thread.Sleep(100);
                                    continue;
                                }

                                //var rby = JsonSerializer.Deserialize<List<List<string>>>(orderbookYesDollarsList);
                                //foreach (var orderbookYes in rby)
                                //{
                                //    if (orderbookYes.Count >= 2)
                                //    {
                                //        var price = /*Convert.ToDecimal(*/orderbookYes[0];//);
                                //        var quantity = /*Convert.ToDecimal(*/orderbookYes[1];//);
                                //        orderbookYesDollarsDic.Add(quantity, price);
                                //    }
                                //}


                                //var rbn = JsonSerializer.Deserialize<List<List<string>>>(orderbookNoDollarsList);
                                //foreach (var orderbookNo in rbn)
                                //{
                                //    if (orderbookNo.Count >= 2)
                                //    {
                                //        var price = /*Convert.ToDecimal(*/orderbookNo[0];//);
                                //        var quantity = /*Convert.ToDecimal(*/orderbookNo[1];//);
                                //        orderbookNoDollarsDic.Add(quantity, price);
                                //    }
                                //}

                                //decimal bestYesBid = 0m;
                                //var bestNoBid = 0m;
                                //try
                                //{
                                //    bestYesBid = orderbookYesDollarsDic.AllKeys.SelectMany(key => orderbookYesDollarsDic.GetValues(key))
                                //        .Select(val => decimal.Parse(val))
                                //        .Max();
                                //    bestNoBid = orderbookNoDollarsDic.AllKeys.SelectMany(key => orderbookNoDollarsDic.GetValues(key))
                                //        .Select(val => decimal.Parse(val))
                                //        .Max();
                                //}
                                //catch (Exception)
                                //{
                                //    //helper.Log(_logger, LogLevel.Information, $"Not buying: {item3.GetProperty("title").GetString()} No Order Books No");
                                //    //System.Threading.Thread.Sleep(100);
                                //}

                                //var bestYesAsk = 1m - bestNoBid;
                                //var spread = bestYesAsk - bestYesBid;
                                //var midPoint = (bestYesAsk + bestYesBid) / 2m;

                                //decimal bet_amount = 1.00m;
                                //int count = 0;                                
                                ////marketList.Add(marketandTitle);
                                //Console.Write("Time to trade.");                                
                                string coin = string.Empty;
                                ////marketandTitle.Title.Trim().Replace(" price up in next 15 mins?", "") + "-USD";
                                switch (marketandTitle.Title.Trim().ToLower())
                                {
                                    case string s when s.Contains("bitcoin"):
                                        coin = "BTC-USD";
                                        break;
                                    case string s when s.Contains("ethereum"):
                                        coin = "ETH-USD";
                                        break;
                                    case string s when s.Contains("ripple"):
                                        coin = "XRP-USD";
                                        break;
                                    case string s when s.Contains("xrp"):
                                        coin = "XRP-USD";
                                        break;
                                    case string s when s.Contains("litecoin"):
                                        coin = "LTC-USD";
                                        break;
                                    case string s when s.Contains("cardano"):
                                        coin = "ADA-USD";
                                        break;
                                    case string s when s.Contains("polkadot"):
                                        coin = "DOT-USD";
                                        break;
                                    case string s when s.Contains("dogecoin"):
                                        coin = "DOGE-USD";
                                        break;
                                    case string s when s.Contains("binance"):
                                        coin = "BNB-USD";
                                        break;
                                    case string s when s.Contains("near"):
                                        coin = "NEAR-USD";
                                        break;
                                    case string s when s.Contains("zcash"):
                                        coin = "ZEC-USD";
                                        break;
                                    case string s when s.Contains("shiba"):
                                        coin = "SHIB-USD";
                                        break;
                                    case string s when s.Contains("solana"):
                                        coin = "SOL-USD";
                                        break;
                                    default:
                                        coin = marketandTitle.Title.Trim().Replace(" price up in next 15 mins?", "") + "-USD";
                                        break;
                                }
                                //var targetPrice = Convert.ToDecimal(marketandTitle.yes_sub_title.Trim().Replace("Target Price: $", ""));
                                var ticker = marketandTitle.MarketTicker.Trim();
                                JsonObject quote = null;
                                Debug.WriteLine($"Getting quote for {coin}");
                                try
                                {
                                    quote = coinBaseService.GetQuoteAsync(coin).Result;
                                    Helper.Log(_logger, LogLevel.Information, $"Quote for {coin}: LastPrice:{quote["lastPrice"].ToString()} SMA5:{quote["SMA5"].ToString()} EMA5:{quote["EMA5"].ToString()} SMA9:{quote["SMA9"].ToString()} EMA9:{quote["EMA9"].ToString()} Forecast Bound Price Difference Average: {quote["Forecast Bound Price Difference Average"].ToString()} Target from market ticket {marketandTitle.yes_sub_title}");
                                    Debug.WriteLine($"Quote for {coin}: LastPrice:{quote["lastPrice"].ToString()} SMA5:{quote["SMA5"].ToString()} EMA5:{quote["EMA5"].ToString()} SMA9:{quote["SMA9"].ToString()} EMA9:{quote["EMA9"].ToString()} Forecast Bound Price Difference Average: {quote["Forecast Bound Price Difference Average"].ToString()} Target from market ticket {marketandTitle.yes_sub_title}");
                                }
                                catch (Exception)
                                {
                                    Helper.Log(_logger, LogLevel.Information, $"Not buying: {item3.GetProperty("title").GetString()} Cannot Get Quote");
                                    Debug.WriteLine($"Not buying: {item3.GetProperty("title").GetString()} Cannot Get Quote");
                                    System.Threading.Thread.Sleep(100);
                                    continue;
                                }
                                if (quote == null || quote is not JsonObject)
                                {
                                    Helper.Log(_logger, LogLevel.Information, $"Not buying: {item3.GetProperty("title").GetString()} Cannot Get Quote");
                                    Debug.WriteLine($"Not buying: {item3.GetProperty("title").GetString()} Cannot Get Quote");
                                    System.Threading.Thread.Sleep(100);
                                    continue;
                                }
                                int? shortCnt = 0; int? longCnt = 0;
                                Debug.WriteLine($"Calculating points for {coin}");
                                _helper.CalculatePoints(quote, ref shortCnt, ref longCnt, .03m, true);

                                Helper.Log(_logger, LogLevel.Information, $"Score for {coin} (Short: {shortCnt}, Long: {longCnt})");
                                Debug.WriteLine($"Score for {coin} (Short: {shortCnt}, Long: {longCnt})");

                                decimal current_ask = 0m;
                                if (longCnt >= _settings.CryptoLongCount)
                                    current_ask = Convert.ToDecimal(marketandTitle.yes_ask_dollars);
                                else if (shortCnt >= _settings.CryptoShortCount)
                                    current_ask = Convert.ToDecimal(marketandTitle.no_ask_dollars);
                                else
                                {
                                    Helper.Log(_logger, LogLevel.Information, $"Not buying: {item3.GetProperty("title").GetString()} Scores are 0");
                                    Debug.WriteLine($"Not buying: {item3.GetProperty("title").GetString()} Scores are 0");
                                    System.Threading.Thread.Sleep(100);
                                    continue;
                                }

                                decimal bet_amount = 1.00m;
                                int count = 0;
                                Helper.Log(_logger, LogLevel.Information, $"Time to trade.");
                                Debug.WriteLine($"Time to trade.");

                                try
                                {
                                    count = Convert.ToInt32(bet_amount / current_ask);
                                }
                                catch (Exception)
                                {
                                    Helper.Log(_logger, LogLevel.Information, $"Cannot calculate count for: {item3.GetProperty("title").GetString()} divide by zero error");
                                    Debug.WriteLine($"Cannot calculate count for: {item3.GetProperty("title").GetString()} divide by zero error");
                                    count = 1;
                                }

                                Helper.Log(_logger, LogLevel.Information, $"Market: {marketandTitle.Title}, Current Ask: {current_ask}, Count: {count}, Close Time: {marketandTitle.CloseTime.ToString()} - {closeTime}, Currnet Time {DateTime.UtcNow}, Minutes to Close: {Math.Abs((closeTime - DateTime.UtcNow).TotalMinutes)}");
                                Debug.WriteLine($"Market: {marketandTitle.Title}, Current Ask: {current_ask}, Count: {count}, Close Time: {marketandTitle.CloseTime.ToString()} - {closeTime}, Currnet Time {DateTime.UtcNow}, Minutes to Close: {Math.Abs((closeTime - DateTime.UtcNow).TotalMinutes)}");

                                if (longCnt >= _settings.CryptoLongCount/*8*/)
                                {
                                    Helper.Log(_logger, LogLevel.Information, $"Buying: {item3.GetProperty("title").GetString()} Going Long");
                                    Debug.WriteLine($"Buying: {item3.GetProperty("title").GetString()} Going Long");
                                    string body = "{" +
                                        $"\"ticker\": \"{ticker}\"," +
                                        $"\"side\": \"bid\"," +
                                        $"\"count\": \"{count}\"," +
                                        //$"\"price\": \"{current_ask}\"," +
                                        $"\"price\": \".99\"," +
                                        $"\"time_in_force\": \"good_till_canceled\"," +
                                        $"\"self_trade_prevention_type\": \"taker_at_cross\"," +
                                        $"\"client_order_id\": \"{Guid.NewGuid()}\"" +  // Unique ID for deduplication
                                    "}";
                                    var results4 = kalshiService.SendRequest(HttpMethod.Post, $"/trade-api/v2/portfolio/events/orders", body, true);
                                    if (results4 is System.Dynamic.ExpandoObject && results4?.status == "Created")
                                    {
                                        Helper.Log(_logger, LogLevel.Information, $"Order for market {ticker} {results4?.status}");
                                        Debug.WriteLine($"Order for market {ticker} {results4?.status}");
                                        marketList.Add(marketandTitle);
                                    }
                                    else
                                    {
                                        Helper.Log(_logger, LogLevel.Information, $"Order for market {ticker} failed: {results4?.ToString()}");
                                        Debug.WriteLine($"Order for market {ticker} failed: {results4?.ToString()}");
                                    }
                                    //return;
                                }
                                else if (shortCnt >= _settings.CryptoShortCount)
                                {
                                    Helper.Log(_logger, LogLevel.Information, $"Buying: {item3.GetProperty("title").GetString()} Going Short");
                                    Debug.WriteLine($"Buying: {item3.GetProperty("title").GetString()} Going Short");
                                    string body = "{" +
                                        $"\"ticker\": \"{ticker}\"," +
                                        $"\"side\": \"bid\"," +
                                        $"\"count\": \"{count}\"," +
                                        //$"\"price\": \"{1 - current_ask}\"," +
                                        $"\"price\": \".01\"," +
                                        $"\"time_in_force\": \"good_till_canceled\"," +
                                        $"\"self_trade_prevention_type\": \"taker_at_cross\"," +
                                        $"\"client_order_id\": \"{Guid.NewGuid()}\"" +  // Unique ID for deduplication
                                    "}";
                                    var results4 = kalshiService.SendRequest(HttpMethod.Post, $"/trade-api/v2/portfolio/events/orders", body, true);
                                    if (results4 is System.Dynamic.ExpandoObject && results4?.status == "Created")
                                    {
                                        Helper.Log(_logger, LogLevel.Information, $"Order for market {ticker} {results4?.status}");
                                        Debug.WriteLine($"Order for market {ticker} {results4?.status}");
                                        marketList.Add(marketandTitle);
                                    }
                                    else
                                    {
                                        Helper.Log(_logger, LogLevel.Information, $"Order for market {ticker} failed: {results4?.ToString()}");
                                        Debug.WriteLine($"Order for market {ticker} failed: {results4?.ToString()}");
                                    }
                                    //return;
                                }
                                else
                                {
                                    Helper.Log(_logger, LogLevel.Information, $"Not buying: {item3.GetProperty("title").GetString()} No Conditions Met");
                                    Debug.WriteLine($"Not buying: {item3.GetProperty("title").GetString()} No Conditions Met");
                                }
                            }
                        }
                    }
                    System.Threading.Thread.Sleep(250);
                }
                //} while (true);
                System.Threading.Thread.Sleep(2500);
            }
            catch (Exception ex)
            {
                Helper.Log(_logger, LogLevel.Warning, $"An error occurred while fetching balance or market positions: {ex.StackTrace} {ex?.InnerException?.Message ?? ""} {ex?.Message}");
                System.Threading.Thread.Sleep(100);
                return;
            }
            Assert.IsTrue(true);
        }
        [TestMethod]
        public void TestKalshiBalance() {
            var cts = new CancellationTokenSource();
            CancellationToken token = cts.Token;
            var kalshiBalanceAndBuy = new KalshiBalanceAndBuy(_logger, httpClientFactory, null);
            kalshiBalanceAndBuy.Exec(token);
        }

    }
}
