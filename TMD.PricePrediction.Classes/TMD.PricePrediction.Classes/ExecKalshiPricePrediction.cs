using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;      // add if you use ILogger
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using TMD.Services;
using TMDInvestments.Context;
using TMDInvestments.Models;

namespace TMD.PricePrediction.Classes
{
    public sealed class ExecKalshiPricePrediction
    {
        ILogger? _logger;
        IHttpClientFactory? httpClientFactory;
        KalshiService? kalshiService = null;
        Helper? _helper = null;
        RulesEngine? _settings = null;
        TmdContext? _ctx = null;
        CoinBaseService? coinBaseService = null;
        public ExecKalshiPricePrediction(ILogger? logger, CoinBaseSettings coinBaseSettings, IHttpClientFactory? factory, Helper? helper, IDbContextFactory<TmdContext> contextFactory)
        {
            _logger = logger;
            httpClientFactory = factory;
            _helper = helper;
            var context = contextFactory.CreateDbContextAsync().Result;//)
                                                                       //{
            _ctx = context;
            //helper = new Helper();
            //ctx = new TmdContext();
            //IConfigurationBuilder builder = new ConfigurationBuilder()
            //    .AddJsonFile("appsettings.json");
            //IConfigurationRoot config = builder.Build();
            //var ConnectionStrings = config.GetSection("ConnectionStrings").Get<ConnectionSettings>();
            //var services = new ServiceCollection();
            //services.AddDbContextFactory<TmdContext>(options =>
            //    options.UseSqlServer(ConnectionStrings.Connection));
            ////services.AddTransient<PricePrediction>();
            //var serviceProvider = services.BuildServiceProvider();
            //using (var scope = serviceProvider.CreateScope())
            //{
            //    var ctxFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TmdContext>>();
            //    var context = ctxFactory.CreateDbContext();
            //    ctx = context;
            //}
            _settings = _ctx.RulesEngine.FirstOrDefault();
            kalshiService = new KalshiService(_logger, httpClientFactory);
            coinBaseService = new CoinBaseService(_logger, coinBaseSettings, httpClientFactory/*, _helper*/);
        }

        public void Exec(CancellationToken token)
        {
            //do {
            // 5. Regularly poll the cancellation request status
            if (token.IsCancellationRequested)
            {
                Helper.Log(_logger, LogLevel.Information, "Cleaning up resources within the thread...");
                // Perform thread-specific cleanup here
                //ctx.Dispose();
                //ctx = null;
                //httpClientFactory = null;
                return;
            }
            //List<dynamic> marketList = new List<dynamic>();
            //string[] title = { "Above/below", "15 Minute" };
            List<string> marketPositionTickers = new List<string>();
            try
            {
                Debug.WriteLine("Get balance and positions {0}", DateTime.Now);
                var balance = kalshiService.SendRequest(HttpMethod.Get, "/trade-api/v2/portfolio/balance", "", true);
                System.Threading.Thread.Sleep(250);
                var positions = kalshiService.SendRequest(HttpMethod.Get, "/trade-api/v2/portfolio/positions", "", true);
                System.Threading.Thread.Sleep(250);
                //var settlements = kalshiService.SendRequest(HttpMethod.Get, "/trade-api/v2/portfolio/settlements", "", true);
                //var fills = kalshiService.SendRequest(HttpMethod.Get, "/trade-api/v2/portfolio/fills", "", true);
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
                    var kalshiMarketPosition = _ctx.KalshiMarketPositions.FirstOrDefaultAsync(x => x.Ticker == marketPosition.GetProperty("ticker").GetString()).Result;
                    if (kalshiMarketPosition == null)
                    {
                        _ctx.KalshiMarketPositions.Add(new KalshiMarketPosition()
                        {
                            Ticker = marketPosition.GetProperty("ticker").GetString(),
                            fees_paid_dollars = Convert.ToDecimal(marketPosition.GetProperty("fees_paid_dollars").GetString()),
                            market_exposure_dollars = Convert.ToDecimal(marketPosition.GetProperty("market_exposure_dollars").GetString()),
                            position_fp = Convert.ToDecimal(marketPosition.GetProperty("position_fp").GetString()),
                            total_traded_dollars = Convert.ToDecimal(marketPosition.GetProperty("total_traded_dollars").GetString()),
                            IsClosed = false,
                            IsReversed = false,
                            Created = DateTime.UtcNow,
                            lastUpdated = null
                        });
                        _ctx.SaveChangesAsync();
                    }
                }
                Debug.WriteLine($"Getting series for Crypto");
                var results = kalshiService.SendRequest(HttpMethod.Get, "/trade-api/v2/series?category=Crypto", "", false);
                System.Threading.Thread.Sleep(250);
                var series = (IEnumerable<JsonElement>)results.GetProperty("series").EnumerateArray();//.GetProperty("category");
                                                                                                      //var seriesList = series.ToList(); 
                var categories = series.Where(x => x.GetProperty("category").GetString() == "Crypto" && (x.GetProperty("frequency").GetString() == "fifteen_min" /*|| x.GetProperty("frequency").GetString() == "hourly"*/) && (/*x.GetProperty("title").GetString().Contains("Above/below") ||*/ x.GetProperty("title").GetString().Contains("15 Minute"))).ToList();

                var tickerandTitle = categories.Select(x => new { SeriesTicker = x.GetProperty("ticker").GetString(), Title = x.GetProperty("title").GetString(), Frequency = x.GetProperty("frequency").GetString() }).ToList();
                //var tickerandTitle = series.Select(x => new { SeriesTicker = x.GetProperty("ticker").GetString(), Title = x.GetProperty("title").GetString() }).ToList();
                JsonElement eventsCursor = new JsonElement();
                JsonElement marketsCursor = new JsonElement();
                IEnumerable <JsonElement> events = null;
                Debug.WriteLine($"Getting events from series");
                foreach (var item in tickerandTitle)
                {
                EventsBySeries:
                    dynamic eventsBySeries = null;
                    //if(eventsCursor.ValueKind != JsonValueKind.Undefined && eventsCursor.TryGetProperty("cursor", out eventsCursor))
                    if (eventsCursor.ValueKind == JsonValueKind.String && !string.IsNullOrEmpty(eventsCursor.GetString()))
                        eventsBySeries = kalshiService.SendRequest(HttpMethod.Get, $"/trade-api/v2/events?series_ticker={item.SeriesTicker}&cursor={eventsCursor.GetString()}", "", false);
                    else
                        eventsBySeries = kalshiService.SendRequest(HttpMethod.Get, $"/trade-api/v2/events?series_ticker={item.SeriesTicker}", "", false);
                    System.Threading.Thread.Sleep(250);
                    try
                    {
                        //var getEvents = results2.GetProperty("events").EnumerateArray();
                        if (eventsBySeries is System.Dynamic.ExpandoObject)
                        {
                            Helper.Log(_logger, LogLevel.Warning, $"Events for series {item.SeriesTicker} is an ExpandoObject, skipping. {eventsBySeries.errors.Details}");
                            Debug.WriteLine($"Events for series {item.SeriesTicker} is an ExpandoObject, skipping. {eventsBySeries.errors.Details}");
                            //System.Threading.Thread.Sleep(250);
                            continue;
                        }
                        events = (IEnumerable<JsonElement>)eventsBySeries.GetProperty("events").EnumerateArray();
                        events = events.Where(x => x.GetProperty("strike_date").GetDateTime().Date == DateTime.Today).ToList();
                    }
                    catch (Exception ex)
                    {
                        Helper.Log(_logger, LogLevel.Error, $"An error occurred while fetching events for series {item.SeriesTicker}: {ex.Message}");
                        Debug.WriteLine($"An error occurred while fetching events for series {item.SeriesTicker}: {ex.Message}");
                        System.Threading.Thread.Sleep(100);
                        continue;
                    }

                    ((JsonElement)eventsBySeries).TryGetProperty("cursor", out eventsCursor);

                    var eventandTitle = events.Select(x => new { Strikedate = x.GetProperty("strike_date").GetString(), EventTicker = x.GetProperty("event_ticker").GetString(), Title = x.GetProperty("title").GetString(), SubTitle = x.GetProperty("sub_title").GetString() }).ToList();
                    IEnumerable<JsonElement> markets = null;
                    foreach (var item2 in eventandTitle)
                    {
                    MarketsByEvents:
                        dynamic marketsByEvents = null;
                        Debug.WriteLine($"Getting markets for event {item2.EventTicker}");
                        //if (marketsCursor.ValueKind != JsonValueKind.Undefined && marketsCursor.TryGetProperty("cursor", out marketsCursor))
                        if (marketsCursor.ValueKind == JsonValueKind.String && !string.IsNullOrEmpty(marketsCursor.GetString()))
                            marketsByEvents = kalshiService.SendRequest(HttpMethod.Get, $"/trade-api/v2/markets?event_ticker={item2.EventTicker}&cursor={marketsCursor.GetString()}", "", false);
                        else
                            marketsByEvents = kalshiService.SendRequest(HttpMethod.Get, $"/trade-api/v2/markets?event_ticker={item2.EventTicker}", "", false);
                        System.Threading.Thread.Sleep(250);
                        try
                        {
                            //var getMarkets = results3.GetProperty("markets").EnumerateArray();
                            if (marketsByEvents is System.Dynamic.ExpandoObject)
                            {
                                Helper.Log(_logger, LogLevel.Warning, $"Markets for event {item2.EventTicker} is an ExpandoObject, skipping. {marketsByEvents.errors.Details}");
                                Debug.WriteLine($"Markets for event {item2.EventTicker} is an ExpandoObject, skipping. {marketsByEvents.errors.Details}");
                                //System.Threading.Thread.Sleep(100);
                                continue;
                            }
                            markets = (IEnumerable<JsonElement>)marketsByEvents.GetProperty("markets").EnumerateArray();
                        }
                        catch (Exception ex)
                        {
                            Helper.Log(_logger, LogLevel.Error, $"An error occurred while fetching markets for event {item2.EventTicker}: {ex.Message}");
                            Debug.WriteLine($"An error occurred while fetching markets for event {item2.EventTicker}: {ex.Message}");
                            //System.Threading.Thread.Sleep(100);
                            continue;
                        }

                        ((JsonElement)marketsByEvents).TryGetProperty("cursor", out marketsCursor);

                        foreach (var item3 in markets)
                        {
                            //if (!item3.GetProperty("title").GetString().ToLower().Contains("price up") && 
                            //    !item3.GetProperty("title").GetString().ToLower().Contains("price down") && 
                            //    !item3.GetProperty("title").GetString().ToLower().Contains("price on") && 
                            //    !item3.GetProperty("title").GetString().ToLower().Contains("price at") &&
                            //    (item3.GetProperty("status").GetString() != "active" || item3.GetProperty("status").GetString() != "initialized"))
                            //{
                            //    Debug.WriteLine($"Market {item3.GetProperty("title").GetString()} does not match criteria");
                            //    System.Threading.Thread.Sleep(100);
                            //    continue;
                            //}
                            Helper.Log(_logger, LogLevel.Information, $"Processing market: {item3.GetProperty("title").GetString()}");
                            Debug.WriteLine($"Processing market: {item3.GetProperty("title").GetString()}");
                            dynamic? marketandTitle = markets.Where(x => x.GetProperty("ticker").GetString() == item3.GetProperty("ticker").GetString())                            
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

                            if (!marketandTitle?.Title.ToLower().Contains("price up") &&
                                !marketandTitle?.Title.ToLower().Contains("price down") &&
                                !marketandTitle?.Title.ToLower().Contains("price on") &&
                                !marketandTitle?.Title.ToLower().Contains("price at")
                               )
                            {
                                Debug.WriteLine($"Market {item3.GetProperty("title").GetString()} does not match criteria");
                                System.Threading.Thread.Sleep(100);
                                continue;
                            }
                            if(marketandTitle?.status != "active" && marketandTitle?.status != "initialized")
                            {
                                Debug.WriteLine($"Market {item3.GetProperty("title").GetString()} does not match criteria");
                                System.Threading.Thread.Sleep(100);
                                continue;
                            }
                            //if (marketandTitle.status != "active" && marketandTitle.status != "initialized") {
                            //    Task.Delay(250).Wait();
                            //    continue;
                            //}

                            if (marketPositionTickers.Contains(marketandTitle.MarketTicker) /*|| marketList.Count() > 5*/)
                            {
                                Helper.Log(_logger, LogLevel.Information, $"Not buying: {item3.GetProperty("title").GetString()} Already in the books or limit reached");
                                Debug.WriteLine($"Not buying: {item3.GetProperty("title").GetString()} Already in the books or limit reached");
                                //System.Threading.Thread.Sleep(100);
                                continue;
                            }

                            DateTime closeTime = DateTime.Parse(marketandTitle.CloseTime.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
                            //var diffCLoseandNow = Math.Abs((closeTime - DateTime.UtcNow).TotalMinutes);
                            //if (diffCLoseandNow < 10)
                            if (Math.Abs((closeTime - DateTime.UtcNow).TotalMinutes) <= 10)
                            {
                                Helper.Log(_logger, LogLevel.Warning, $"Not buying: {item3.GetProperty("title").GetString()} Closing in less than 10 minutes");
                                Debug.WriteLine($"Not buying: {item3.GetProperty("title").GetString()} Closing in less than 10 minutes");
                                //System.Threading.Thread.Sleep(100);
                                continue;
                            }

                            //NameValueCollection orderbookYesDollarsDic = new NameValueCollection();
                            //NameValueCollection orderbookNoDollarsDic = new NameValueCollection();
                            //Debug.WriteLine($"Getting orderbook for market: {marketandTitle.Title}");
                            //var orderBooks = kalshiService.SendRequest(HttpMethod.Get, $"/trade-api/v2/markets/{marketandTitle.MarketTicker}/orderbook", "", false);
                            //if (orderBooks is JsonElement)
                            //{

                                //DateTime closeTime = DateTime.Parse(marketandTitle.CloseTime.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
                                ////var diffCLoseandNow = Math.Abs((closeTime - DateTime.UtcNow).TotalMinutes);
                                ////if (diffCLoseandNow < 10)
                                //if (Math.Abs((closeTime - DateTime.UtcNow).TotalMinutes) <= 10)
                                //{
                                //    Helper.Log(_logger, LogLevel.Warning, $"Not buying: {item3.GetProperty("title").GetString()} Closing in less than 10 minutes");
                                //    Debug.WriteLine($"Not buying: {item3.GetProperty("title").GetString()} Closing in less than 10 minutes");
                                //    System.Threading.Thread.Sleep(100);
                                //    continue;
                                //}

                                //var orderbookNoDollars = (IEnumerable<JsonElement>)orderBooks.GetProperty("orderbook_fp").GetProperty("no_dollars").EnumerateArray();
                                //var orderbookYesDollars = (IEnumerable<JsonElement>)orderBooks.GetProperty("orderbook_fp").GetProperty("yes_dollars").EnumerateArray();
                                //var orderbookNoDollarsList = orderBooks.GetProperty("orderbook_fp").GetProperty("no_dollars").GetRawText();//.EnumerateArray();//.ToList();
                                //var orderbookYesDollarsList = orderBooks.GetProperty("orderbook_fp").GetProperty("yes_dollars").GetRawText();//.EnumerateArray();//.ToList();

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
                                    System.Threading.Thread.Sleep(250);
                                    Helper.Log(_logger, LogLevel.Information, $"Quote for {coin}: LastPrice:{quote["lastPrice"].ToString()} SMA5:{quote["SMA5"].ToString()} EMA5:{quote["EMA5"].ToString()} SMA9:{quote["SMA9"].ToString()} EMA9:{quote["EMA9"].ToString()} Forecast Bound Price Difference Average: {quote["Forecast Bound Price Difference Average"].ToString()} Target from market ticket {marketandTitle.yes_sub_title}");
                                    Debug.WriteLine($"Quote for {coin}: LastPrice:{quote["lastPrice"].ToString()} SMA5:{quote["SMA5"].ToString()} EMA5:{quote["EMA5"].ToString()} SMA9:{quote["SMA9"].ToString()} EMA9:{quote["EMA9"].ToString()} Forecast Bound Price Difference Average: {quote["Forecast Bound Price Difference Average"].ToString()} Target from market ticket {marketandTitle.yes_sub_title}");
                                }
                                catch (Exception)
                                {
                                    Helper.Log(_logger, LogLevel.Error, $"Not buying: {item3.GetProperty("title").GetString()} Cannot Get Quote");
                                    Debug.WriteLine($"Not buying: {item3.GetProperty("title").GetString()} Cannot Get Quote");
                                    //System.Threading.Thread.Sleep(100);
                                    continue;
                                }
                                if (quote == null || quote is not JsonObject)
                                {
                                    Helper.Log(_logger, LogLevel.Information, $"Not buying: {item3.GetProperty("title").GetString()} Cannot Get Quote");
                                    Debug.WriteLine($"Not buying: {item3.GetProperty("title").GetString()} Cannot Get Quote");
                                    //System.Threading.Thread.Sleep(100);
                                    continue;
                                }
                                Task.Delay(250).Wait();
                                int? shortCnt = 0; int? longCnt = 0;
                                Debug.WriteLine($"Calculating points for {coin}");
                                _helper?.CalculatePoints(quote, ref shortCnt, ref longCnt, .08m, true);
                                //_helper?.CalculatePoints(quote, ref longCnt, ref shortCnt, .75m, true);

                                Helper.Log(_logger, LogLevel.Information, $"Score for {coin} (Short: {shortCnt}, Long: {longCnt})");
                                Debug.WriteLine($"Score for {coin} (Short: {shortCnt}, Long: {longCnt})");

                                decimal current_ask = .99m;
                                //if (longCnt >= _settings.CryptoLongCount)
                                //    current_ask = Convert.ToDecimal(marketandTitle.yes_ask_dollars);
                                //else if (shortCnt >= _settings.CryptoShortCount /*|| shortCnt == 0*/)
                                //    current_ask = 1m - Convert.ToDecimal(marketandTitle.no_ask_dollars);
                                //else
                                //{
                                //    Helper.Log(_logger, LogLevel.Warning, $"Not buying: {item3.GetProperty("title").GetString()} Scores are 0");
                                //    Debug.WriteLine($"Should Not buying: {item3.GetProperty("title").GetString()} Scores are 0");
                                //    //System.Threading.Thread.Sleep(100);
                                //    //continue;
                                //}

                                //if(current_ask <= 0)
                                //{
                                //    current_ask = 0.01m;
                                //}
                                //decimal bet_amount = 1.00m;
                                int count = 0;
                                Helper.Log(_logger, LogLevel.Information, $"Time to trade.");
                                Debug.WriteLine($"Time to trade.");

                                try
                                {
                                //count = Convert.ToInt32(bet_amount / current_ask);
                                count = 1;
                                }
                                catch (Exception)
                                {
                                    Helper.Log(_logger, LogLevel.Error, $"Cannot calculate count for: {item3.GetProperty("title").GetString()} divide by zero error");
                                    Debug.WriteLine($"Cannot calculate count for: {item3.GetProperty("title").GetString()} divide by zero error");
                                    count = 1;
                                }

                                Helper.Log(_logger, LogLevel.Information, $"Market: {marketandTitle.Title}, Current Ask: {current_ask}, Count: {count}, Close Time: {marketandTitle.CloseTime.ToString()} - {closeTime}, Currnet Time {DateTime.UtcNow}, Minutes to Close: {Math.Abs((closeTime - DateTime.UtcNow).TotalMinutes)}");
                                Debug.WriteLine($"Market: {marketandTitle.Title}, Current Ask: {current_ask}, Count: {count}, Close Time: {marketandTitle.CloseTime.ToString()} - {closeTime}, Currnet Time {DateTime.UtcNow}, Minutes to Close: {Math.Abs((closeTime - DateTime.UtcNow).TotalMinutes)}");

                                if (longCnt >= _settings.CryptoLongCount/*8*/)
                                {
                                    //continue;
                                    Helper.Log(_logger, LogLevel.Information, $"Buying: {item3.GetProperty("title").GetString()} Going Long");
                                    Debug.WriteLine($"Buying: {item3.GetProperty("title").GetString()} Going Long");
                                    string body = "{" +
                                        $"\"ticker\": \"{ticker.Trim()}\"," +
                                        $"\"side\": \"bid\"," +
                                        $"\"action\": \"buy\"," +
                                        $"\"count\": \"{count}\"," +
                                        $"\"price\": \"{current_ask}\"," +
                                        $"\"time_in_force\": \"good_till_canceled\"," +
                                        $"\"self_trade_prevention_type\": \"taker_at_cross\"," +
                                        $"\"client_order_id\": \"{Guid.NewGuid()}\"" +  // Unique ID for deduplication
                                    "}";
                                    var results4 = kalshiService.SendRequest(HttpMethod.Post, $"/trade-api/v2/portfolio/events/orders", body, true);
                                    System.Threading.Thread.Sleep(250);
                                    if (results4 is System.Dynamic.ExpandoObject && results4?.status == "Created")
                                    {
                                        Helper.Log(_logger, LogLevel.Information, $"Order for market {ticker} {results4?.status}");
                                        Debug.WriteLine($"Order for market {ticker} {results4?.status}");
                                        //marketList.Add(marketandTitle);
                                    }
                                    else
                                    {
                                        Helper.Log(_logger, LogLevel.Error, $"Order for market {ticker} failed: {results4?.ToString()}");
                                        Debug.WriteLine($"Order for market {ticker} failed: {results4?.ToString()}");
                                    }
                                    //return;
                                }
                                else if (shortCnt >= _settings.CryptoShortCount/* || shortCnt == 0*/)
                                {
                                    Helper.Log(_logger, LogLevel.Information, $"Buying: {item3.GetProperty("title").GetString()} Going Short");
                                    Debug.WriteLine($"Buying: {item3.GetProperty("title").GetString()} Going Short");
                                    string body = "{" +
                                        $"\"ticker\": \"{ticker.Trim()}\"," +
                                        $"\"action\": \"buy\"," +
                                        $"\"side\": \"ask\"," +
                                        $"\"count\": \"{count}\"," +
                                        $"\"price\": \"{current_ask}\"," +
                                        $"\"time_in_force\": \"good_till_canceled\"," +
                                        $"\"self_trade_prevention_type\": \"taker_at_cross\"," +
                                        $"\"client_order_id\": \"{Guid.NewGuid()}\"" +  // Unique ID for deduplication
                                    "}";
                                    var results4 = kalshiService.SendRequest(HttpMethod.Post, $"/trade-api/v2/portfolio/events/orders", body, true);
                                    System.Threading.Thread.Sleep(250);
                                    if (results4 is System.Dynamic.ExpandoObject && results4?.status == "Created")
                                    {
                                        Helper.Log(_logger, LogLevel.Information, $"Order for market {ticker} {results4?.status}");
                                        Debug.WriteLine($"Order for market {ticker} {results4?.status}");
                                        //marketList.Add(marketandTitle);
                                    }
                                    else
                                    {
                                        Helper.Log(_logger, LogLevel.Error, $"Order for market {ticker} failed: {results4?.ToString()}");
                                        Debug.WriteLine($"Order for market {ticker} failed: {results4?.ToString()}");
                                    }
                                    //return;
                                }
                                else
                                {
                                    Helper.Log(_logger, LogLevel.Warning, $"Not buying: {item3.GetProperty("title").GetString()} No Conditions Met");
                                    Debug.WriteLine($"Not buying: {item3.GetProperty("title").GetString()} No Conditions Met");
                                }
                            //}
                        }
                        //if (marketsByEvents is object)
                        //{
                        //    if (!string.IsNullOrEmpty(marketsByEvents.GetType().GetProperty("cursor")?.GetValue(marketsByEvents, null)?.ToString()))
                        //    {
                        //        goto MarketsByEvents;
                        //    }
                        //}
                        //else
                        //{
                            //if (marketsByEvents.TryGetProperty("cursor", out marketsByEvents))
                            if (marketsCursor.ValueKind == JsonValueKind.String && !string.IsNullOrEmpty(marketsCursor.GetString()))
                            {
                                goto MarketsByEvents;
                            }
                        //}
                    }
                    System.Threading.Thread.Sleep(250);
                    //if(eventsCursor is object)
                    //{
                    //    if(!string.IsNullOrEmpty(eventsCursor.GetType().GetProperty("cursor")?.GetValue(eventsCursor, null)?.ToString()))
                    //    {
                    //        goto EventsBySeries;
                    //    }
                    //}
                    //else
                    //{
                    //if (eventsCursor.TryGetProperty("cursor", out eventsCursor))
                    if (eventsCursor.ValueKind == JsonValueKind.String && !string.IsNullOrEmpty(eventsCursor.GetString()))
                    {
                        goto EventsBySeries;
                    }
                    //}
                }
                //} while (true);
                System.Threading.Thread.Sleep(250);
            }
            catch (Exception ex)
            {
                Helper.Log(_logger, LogLevel.Error, $"An error occurred while fetching balance or market positions: {ex.StackTrace} {ex?.InnerException?.Message ?? ""} {ex?.Message}");
                System.Threading.Thread.Sleep(100);
                return;
            }
        }
        ~ExecKalshiPricePrediction()
        {
            //cts.Cancel();
            //ctx.Dispose();
            //ctx = null;
            httpClientFactory = null;
            //orderThread = null;
            //kalshiBalanceAndBuy = null;

        }
    }
}
