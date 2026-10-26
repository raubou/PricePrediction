using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;
using TMD.Services;
using TMDInvestments.Context;
using TMDInvestments.Models;

namespace TMD.PricePrediction
{
    public sealed class KalshiBalanceAndBuy
    {
        ILogger? _logger;
        IHttpClientFactory? httpClientFactory;
        KalshiService kalshiService = null;
        //Helper helper = null;
        RulesEngine _settings = null;
        TmdContext _ctx = null;
        dynamic positions = null;
        private readonly System.Threading.Lock _transactLock = new();
        public KalshiBalanceAndBuy(ILogger? logger, IHttpClientFactory? factory, IDbContextFactory<TmdContext> contextFactory)
        {
            _logger = logger;
            httpClientFactory = factory;
            _ctx = contextFactory.CreateDbContext();
            kalshiService = new KalshiService(_logger, httpClientFactory);
        }
        public void Exec(CancellationToken token)
        {
            do
            {
                // 5. Regularly poll the cancellation request status
                if (token.IsCancellationRequested)
                {
                    Console.WriteLine("Cleaning up resources within the thread...");
                    // Perform thread-specific cleanup here
                    _ctx.Dispose();
                    _ctx = null;
                    httpClientFactory = null;
                    break;
                }
                //List<dynamic> marketPositionTickers = new List<dynamic>();
                _logger.LogWarning($" ");
                _logger.LogWarning($"Fetching market positions and orders from Kalshi on thread loop...");
                _logger.LogWarning($" ");
                //var balance = kalshiService.SendRequest(HttpMethod.Get, "/trade-api/v2/portfolio/balance", "", true);
                lock (_transactLock)
                {
                    positions = kalshiService.SendRequest(HttpMethod.Get, "/trade-api/v2/portfolio/positions", "", true);
                }
                System.Threading.Thread.Sleep(250);
                //var orders = kalshiService.SendRequest(HttpMethod.Get, "/trade-api/v2/portfolio/orders", "", true);
                //var settlements = kalshiService.SendRequest(HttpMethod.Get, "/trade-api/v2/portfolio/settlements", "", true);
                try
                {
                    //var balanceBreakdown = balance.GetProperty("balance_breakdown");
                    //decimal balanceDollars = Convert.ToDecimal(balance.GetProperty("balance_dollars").GetString());
                    //if (balanceDollars <= 5)
                    //{
                    //    _logger?.LogWarning($"Low balance available: {balanceDollars}");
                    //    System.Threading.Thread.Sleep(100);
                    //    //return;
                    //}
                    //_logger.LogInformation($"Balance: {balanceDollars}");
                    var marketPositions = (IEnumerable<JsonElement>)positions.GetProperty("market_positions").EnumerateArray();
                    var eventsPositions = (IEnumerable<JsonElement>)positions.GetProperty("event_positions").EnumerateArray();
                    //var orderList = (IEnumerable<JsonElement>)orders.GetProperty("orders").EnumerateArray();
                    //foreach (var eventsPosition in eventsPositions)
                    //{
                    //    var gainsOrLoses = Convert.ToDecimal(eventsPosition.GetProperty("realized_pnl_dollars").GetString());
                    //}
                    foreach (var marketPosition in marketPositions)
                    {
                        var ticker = marketPosition.GetProperty("ticker").GetString();
                        var market = kalshiService.SendRequest(HttpMethod.Get, $"/trade-api/v2/markets/{ticker}", "", true);
                        System.Threading.Thread.Sleep(250);
                        var title = market.GetProperty("market").GetProperty("title").GetString();
                        var yes_bid_dollars = Convert.ToDecimal(market.GetProperty("market").GetProperty("yes_bid_dollars").GetString());
                        var no_bid_dollars = Convert.ToDecimal(market.GetProperty("market").GetProperty("no_bid_dollars").GetString());
                        var yes_ask_dollars = Convert.ToDecimal(market.GetProperty("market").GetProperty("yes_ask_dollars").GetString());
                        var no_ask_dollars = Convert.ToDecimal(market.GetProperty("market").GetProperty("no_ask_dollars").GetString());
                        var position_fp = Convert.ToDecimal(marketPosition.GetProperty("position_fp").GetString());
                        var market_exposure_dollars = Convert.ToDecimal(marketPosition.GetProperty("market_exposure_dollars").GetString());
                        var last_price_dollars = Convert.ToDecimal(market.GetProperty("market").GetProperty("last_price_dollars").GetString());
                        var fees_paid_dollars = Convert.ToDecimal(marketPosition.GetProperty("fees_paid_dollars").GetString());
                        //var order = orderList.Where(x => x.GetProperty("ticker").GetString() == ticker).FirstOrDefault();

                        //if (order.ValueKind == JsonValueKind.Null || order.ValueKind == JsonValueKind.Undefined)
                        //{
                        //    _logger.LogInformation($"Order cannot be found");
                        //    continue;
                        //}
                        var side = (position_fp > 0) ? "yes" : "no";// order.GetProperty("outcome_side").GetString();
                        //var yes_price_dollars = Convert.ToDecimal(order.GetProperty("yes_price_dollars").GetString());
                        //var no_price_dollars = Convert.ToDecimal(order.GetProperty("no_price_dollars").GetString());
                        //var fill_count_fp = Convert.ToDecimal(order.GetProperty("fill_count_fp").GetString());
                        //var client_order_id = order.GetProperty("client_order_id").GetString();
                        //decimal currentValue = 0m;
                        decimal sellPrice = 0m;
                        //decimal gainsOrLoses = 0m;

                        //decimal contractMarketValue = (Math.Abs(position_fp)/100) * last_price_dollars;
                        //decimal contractMarketValue = (Math.Abs(position_fp) / 100) * last_price_dollars;
                        //decimal contractMarketValue = Math.Abs(position_fp) * last_price_dollars;
                        decimal contractMarketValue = position_fp * last_price_dollars;
                        //decimal contractMarketValue = (Math.Abs(position_fp) / last_price_dollars);
                        //decimal currentMarketValue =  (market_exposure_dollars + fees_paid_dollars) - contractMarketValue;
                        decimal unrealizedPnL = 0m;//(market_exposure_dollars + fees_paid_dollars) - contractMarketValue;
                        //decimal unrealizedPnL = (market_exposure_dollars + fees_paid_dollars) - currentMarketValue;
                        //{ }
                        if (side == "yes")
                        {
                            unrealizedPnL = (market_exposure_dollars + fees_paid_dollars) - contractMarketValue;
                            ////    //currentValue = Math.Round(Math.Abs(position_fp) * yes_bid_dollars, 2);
                            //currentValue = Math.Round(Math.Abs(position_fp) * last_price_dollars, 5);
                            //if (currentValue != 0)
                            //    gainsOrLoses = currentValue - (market_exposure_dollars + fees_paid_dollars);
                            //sellPrice = yes_bid_dollars;
                        }
                        else if (side == "no")
                        {
                            unrealizedPnL = (market_exposure_dollars + fees_paid_dollars) - ((market_exposure_dollars + fees_paid_dollars) - contractMarketValue);
                            ////currentValue = Math.Round(Math.Abs(position_fp) * no_bid_dollars, 2);
                            ////currentValue = (Math.Round(Math.Abs(position_fp) * last_price_dollars, 5)) ;
                            //currentValue = (Math.Round(position_fp * last_price_dollars, 5));
                            //if (currentValue != 0)
                            //    gainsOrLoses = ((market_exposure_dollars + fees_paid_dollars) + currentValue);// - 1m;
                            ////gainsOrLoses = ((market_exposure_dollars + fees_paid_dollars) - currentValue) - (market_exposure_dollars + fees_paid_dollars);
                            ////gainsOrLoses = (market_exposure_dollars + fees_paid_dollars) - ((market_exposure_dollars + fees_paid_dollars) - currentValue);
                            //sellPrice = no_bid_dollars;
                        }
                        //if (currentValue != 0)
                        //    gainsOrLoses =((market_exposure_dollars + fees_paid_dollars) - currentValue) - (market_exposure_dollars + fees_paid_dollars);
                        //gainsOrLoses = currentValue - (market_exposure_dollars + fees_paid_dollars);
                        //_logger.LogInformation($"title: {title} position: ticker {ticker} contracts {marketPosition.GetProperty("position_fp").GetString()} cost {marketPosition.GetProperty("market_exposure_dollars").GetString()} gains or loses {gainsOrLoses}");
                        //if (/*gainsOrLoses >= .2m ||*/ gainsOrLoses <= -(.5m))
                        //{
                        //NameValueCollection orderbookYesDollarsDic = new NameValueCollection();
                        //NameValueCollection orderbookNoDollarsDic = new NameValueCollection();

                        //var orderBooks = kalshiService.SendRequest(HttpMethod.Get, $"/trade-api/v2/markets/{ticker}/orderbook", "", false);

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

                        //if (position_fp > 0)
                        //{
                        //    //currentValue = Math.Round(Math.Abs(position_fp) * yes_bid_dollars, 2);
                        //    //closingPrice = Math.Round(1m - bestYesBid, 2);
                        //}
                        //else if (position_fp < 0)
                        //{
                        //    //currentValue = Math.Round(Math.Abs(position_fp) * no_bid_dollars, 2);
                        //    //closingPrice = Math.Round(spread, 2);
                        //}
                        //continue;
                        if (unrealizedPnL <= -(.25m))
                        {
                        _logger.LogInformation($"Gains meet threshold: {unrealizedPnL} cashing out...");
                            //var order = orderList.Where(x => x.GetProperty("ticker").GetString() == ticker).FirstOrDefault();
                            //if (order.ValueKind == JsonValueKind.Null)
                            //{
                            //    _logger.LogInformation($"Order cannot be found");
                            //    continue;
                            //}
                            //var count = (Convert.ToDecimal(marketPosition.GetProperty("position_fp").GetString()) > 0) ? -Convert.ToDecimal(marketPosition.GetProperty("position_fp").GetString()) : Math.Abs(Convert.ToDecimal(marketPosition.GetProperty("position_fp").GetString()));
                            //var count = (Convert.ToDecimal(order.GetProperty("fill_count_fp").GetString()) > 0) ? -Convert.ToDecimal(order.GetProperty("fill_count_fp").GetString()) : Math.Abs(Convert.ToDecimal(marketPosition.GetProperty("fill_count_fp").GetString()));
                            //var side = (Convert.ToDecimal(marketPosition.GetProperty("position_fp").GetString()) > 0) ? "ask" : "bid";
                            //var position_cost = Convert.ToDecimal(marketPosition.GetProperty("position_cost").GetString());
                            //var fill_count_fp = order.GetProperty("fill_count_fp").GetString();
                            //sellPrice = (side == "yes") ? yes_bid_dollars : yes_bid_dollars - 1m;
                            sellPrice = market_exposure_dollars / Math.Abs(position_fp);
                            var reversSide = (side == "yes") ? "ask" : "bid";
                            //var reverseCurrent_ask = -(currentValue - 1m);// (side == "yes") ? 1m - Convert.ToDecimal(no_price_dollars) : 1m - Convert.ToDecimal(yes_price_dollars);
                            if (sellPrice <= 0)
                            {
                                sellPrice = 0.01m;
                            }
                            var body = "{" +
                                $"\"ticker\": \"{ticker}\"," +
                                $"\"side\": \"{reversSide}\"," +
                                //$"\"action\": \"buy\"," +
                                $"\"count\": \"-{(position_fp.ToString("F2"))}\"," +
                                $"\"price\": \"{sellPrice.ToString("F2")}\"," +
                                $"\"reduce_only\": true," +
                                $"\"time_in_force\": \"immediate_or_cancel\"," +
                                $"\"self_trade_prevention_type\": \"taker_at_cross\"," +
                                $"\"client_order_id\": \"{Guid.NewGuid()}\"" +  // Unique ID for deduplication
                            "}";

                            lock (_transactLock)
                            {
                                Helper.Log(_logger, LogLevel.Information, $"Locking Thread and Executing Trade for market {ticker} at price {sellPrice.ToString("F2")} for {-(position_fp)} contracts");
                                var results4 = kalshiService.SendRequest(HttpMethod.Post, $"/trade-api/v2/portfolio/events/orders", body, true);
                                System.Threading.Thread.Sleep(250);
                                //var results4 = kalshiService.SendRequest(HttpMethod.Post, $"/trade-api/v2/portfolio/events/orders/{order.GetProperty("order_id").GetString()}/decrease", body, true);
                                Helper.Log(_logger, LogLevel.Information, $"Exited Trade");
                                if (results4 is System.Dynamic.ExpandoObject && results4?.status == "Created")
                                {
                                    Helper.Log(_logger, LogLevel.Information, $"Order for market {ticker} {results4?.status}");
                                    Debug.WriteLine($"Order for market {ticker} {results4?.status}");

                                    var kalshiMarketPosition = _ctx.KalshiMarketPositions.FirstOrDefault(x => x.Ticker == ticker && x.IsReversed == false);
                                    if (kalshiMarketPosition != null && !kalshiMarketPosition.IsReversed)
                                    {
                                        //reverseCurrent_ask = (side == "yes") ? 1m - Convert.ToDecimal(no_ask_dollars) : Convert.ToDecimal(yes_ask_dollars);

                                        body = "{" +
                                            $"\"ticker\": \"{ticker}\"," +
                                            $"\"side\": \"{reversSide}\"," +
                                            $"\"action\": \"buy\"," +
                                            $"\"count\": \"-{(position_fp.ToString("F2"))}\"," +
                                            $"\"price\": \"{sellPrice}\"," +
                                            $"\"time_in_force\": \"good_till_canceled\"," +
                                            $"\"self_trade_prevention_type\": \"taker_at_cross\"," +
                                            $"\"client_order_id\": \"{Guid.NewGuid()}\"" +  // Unique ID for deduplication
                                        "}";
                                        var results5 = kalshiService.SendRequest(HttpMethod.Post, $"/trade-api/v2/portfolio/events/orders", body, true);
                                        System.Threading.Thread.Sleep(250);
                                        if (results5 is System.Dynamic.ExpandoObject && results5?.status == "Created")
                                        {
                                            Helper.Log(_logger, LogLevel.Information, $"Reversed Order for market {ticker} {results5?.status}");
                                            Debug.WriteLine($"Reversed Order for market {ticker} {results5?.status}");
                                            kalshiMarketPosition.IsReversed = true;
                                            _ctx.SaveChanges();
                                        }
                                        else
                                        {
                                            Helper.Log(_logger, LogLevel.Error, $"Reversed Order for market {ticker} failed: {results5?.ToString()}");
                                            Debug.WriteLine($"Reversed Order for market {ticker} failed: {results5?.ToString()}");
                                        }
                                    }
                                }
                                else
                                {
                                    Helper.Log(_logger, LogLevel.Error, $"Order for market {ticker} failed: {results4?.ToString()}");
                                    Debug.WriteLine($"Order for market {ticker} failed: {results4?.ToString()}");
                                }
                            }
                        }

                    }
                }
                catch (Exception ex)
                {
                    Helper.Log(_logger, LogLevel.Error, $"An error occurred while fetching balance or market positions: {ex.Message}");
                    System.Threading.Thread.Sleep(100);
                    //return;
                }
                Helper.Log(_logger, LogLevel.Information, $"End Thread Loop");
                System.Threading.Thread.Sleep(1000);
            } while (true);
            //return;
        }
    }
}
