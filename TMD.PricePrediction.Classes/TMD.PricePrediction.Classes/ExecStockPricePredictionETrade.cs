using GraphQLParser;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
//using System.Data.Entity;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using TMD.Services.Service;
using TMDInvestments.Context;
using TMDInvestments.Models;
using TMDInvestments.Models.Models;
using TMDInvestments.Services;

namespace TMD.PricePrediction.Classes
{
    public sealed class ExecStockPricePredictionETrade
    {
        ILogger? _logger;
        ETradeSettings? _settings;
        SchwabSettings? _settings2;
        TaAPISettings? _taapiSettings;
        IHttpClientFactory? _httpClientFactory;
        ETradeService? eTradeService = null;
        SchwabServices? services2 = null;
        TmdContext? _ctx = null;
        RulesEngine? settings;
        List<StockTracker2> stockTracker = null;
        //TaAPIService? taapiService = null;
        ServiceProvider? _serviceProvider = null;
        Helper? _helper = null;
        IDbContextFactory<TmdContext> _contextFactory;
        public ExecStockPricePredictionETrade(ILogger logger, ETradeSettings settings, SchwabSettings settings2, IHttpClientFactory httpClientFactory, IDbContextFactory<TmdContext> contextFactory)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _settings = settings;
            _settings2 = settings2;
            _contextFactory = contextFactory;
            //_ctx = ctx;
            //_helper = helper;
            //ctx = new TmdContext();
            //IConfigurationBuilder builder = new ConfigurationBuilder()
            //    .AddJsonFile("appsettings.json");
            //IConfigurationRoot config = builder.Build();
            //var ConnectionStrings = config.GetSection("ConnectionStrings").Get<ConnectionSettings>();
            //var services = new ServiceCollection();
            //var serviceProvider = services.BuildServiceProvider();
            var ctx = contextFactory.CreateDbContext();
            _ctx = ctx;
            //using (var scope = serviceProvider.CreateScope())
            //{
            //    var ctxFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TmdContext>>();
            //    var context = ctxFactory.CreateDbContext();
            //    _ctx = context;
            //}
            _helper = new Helper(_logger, _settings2, _settings, null, contextFactory);
            eTradeService = new ETradeService(_logger, _settings, _httpClientFactory, contextFactory);
            services2 = new SchwabServices(_logger, _settings2, _httpClientFactory, _contextFactory);
        }
        //public ExecStockPricePredictionETrade(ILogger logger, ETradeService service, IHttpClientFactory httpClientFactory/*, Helper helper*/)
        //{
        //    _httpClientFactory = httpClientFactory;
        //    _logger = logger;
        //    //_settings = settings;
        //    this.services = service;
        //    //_helper = helper;
        //    //services = new ETradeService(_logger, _settings, _httpClientFactory, new Helper(ctx));
        //    ctx = new TmdContext();
        //    //helper = new Helper(_logger, _settings2, _settings, _httpClientFactory);
        //}
        //public ExecStockPricePredictionETrade(ILogger logger, ETradeSettings settings, SchwabSettings settings2, IHttpClientFactory httpClientFactory, Helper helper, ServiceProvider serviceProvider)
        //public ExecStockPricePredictionETrade(ILogger logger, ETradeService service, SchwabServices service2, IHttpClientFactory httpClientFactory/*, Helper helper*/, ServiceProvider serviceProvider)
        //{
        //    _httpClientFactory = httpClientFactory;
        //    _logger = logger;
        //    //_settings = settings;
        //    this.services = service;
        //    this.services2 = service2;
        //    //_helper = helper;
        //    //services = new ETradeService(_logger, _settings, _httpClientFactory, new Helper(ctx));
        //    //services2 = new SchwabServices(_logger, _settings2, _httpClientFactory);
        //    ctx = new TmdContext();
        //    //helper = new Helper(_logger, _settings2, _settings, _httpClientFactory);
        //}
        //public ExecStockPricePredictionETrade(ILogger logger, ETradeSettings settings, SchwabSettings settings2,TaAPISettings taAPISettings, IHttpClientFactory httpClientFactory, Helper helper)
        //public ExecStockPricePredictionETrade(ILogger logger, ETradeService service, SchwabServices service2, TaAPISettings taAPISettings, IHttpClientFactory httpClientFactory, Helper helper)
        //{
        //    _httpClientFactory = httpClientFactory;
        //    _logger = logger;
        //    //_settings = settings;
        //    //_settings2 = settings2;
        //    this.services = service;
        //    this.services2 = service2;
        //    _taapiSettings = taAPISettings;
        //    _helper = helper;
        //    //services = new ETradeService(_logger, _settings, _httpClientFactory, new Helper(ctx));
        //    //services2 = new SchwabServices(_logger, _settings2, _httpClientFactory);
        //    //taapiService = new TaAPIService(_logger, _taapiSettings, _httpClientFactory);
        //    ctx = new TmdContext();
        //    //helper = new Helper(_logger, _settings2, _settings, _httpClientFactory);
        //}
        public void Exec(decimal totalAvailiableforLongTrade, CancellationToken token, ref List<BuySellCommodity>? BuySellStockList)
        {
            Helper.Log(_logger, LogLevel.Information, $"Get account info for Charles Etrade");
            //do
            //{
                // 5. Regularly poll the cancellation request status
                if (token.IsCancellationRequested)
                {
                    Helper.Log(_logger, LogLevel.Information, "Cleaning up resources within the thread...");
                    // Perform thread-specific cleanup here
                    _ctx.Dispose();
                    _ctx = null;
                    //httpClientFactory = null;
                    return;
                }
                Run(totalAvailiableforLongTrade, ref BuySellStockList);
            //    System.Threading.Thread.Sleep(5000);
            //} while (true);            
        }

        private void Run(decimal totalAvailiableforLongTrade, ref List<BuySellCommodity>? BuySellStockList)
        {

            settings = _ctx.RulesEngine.FirstOrDefault();
            Helper.Log(_logger, LogLevel.Information, $"Get settings for ETrade");

            //Update Prices
            Helper.Log(_logger, LogLevel.Information, $"Updating Latest Prices on Tracker");
            UpdatePrices(_logger);

            //Checked for queued crypto adn see if direction is the same
            //Helper.Log(_logger, LogLevel.Information, $"Check for queued stock");
            //_helper.CheckforQueuedStock(settings, Platform.ETrade);

            if (totalAvailiableforLongTrade > 5 && (bool)settings.EtradeAnalysisTradeEnabled)
            {
                if (BuySellStockList.Count > 0)
                {
                    var stockTrackerBuy = _ctx.StockTracker2.AsNoTracking().Where(x => x.PriceSold == null && x.DateSold == null && x.Status == "Filled" && x.TradePlatform == (int)Platform.ETrade);//.ToList();
                    var numOfNeededStock = (int)((settings.MaxNumberofStockstoBuy / 2) - stockTrackerBuy.Count());
                    if (numOfNeededStock > 0)
                    {
                        List<BuySellCommodity> BuySellStockListLong = BuySellStockList.Where(x => x.Direction == TradeDirection.BUY).OrderByDescending(x => x.Points).ThenBy(x => Convert.ToDecimal(x.Results["RSI"].ToString()))/*.Take(numOfNeededStock)*/.ToList();
                        List<BuySellCommodity> BuySellStockListShort = new List<BuySellCommodity>();
                        //if ((numOfNeededStock - BuySellStockListLong.Count()) > 0)
                            BuySellStockListShort = BuySellStockList.Where(x => x.Direction == TradeDirection.SELL_SHORT).OrderByDescending(x => x.Points).ThenByDescending(x => Convert.ToDecimal(x.Results["RSI"].ToString()))/*.Take((numOfNeededStock - BuySellStockListLong.Count()))*/.ToList();
                        //CalculatePurchaseAmount(totalAvailiableforLongTrade, ref BuySellStockList);
                        List<BuySellCommodity> BuySellStockListTotal = new List<BuySellCommodity>();
                        BuySellStockListTotal.AddRange(BuySellStockListLong);
                        BuySellStockListTotal.AddRange(BuySellStockListShort);
                        BuySellStockList = BuySellStockListTotal;
                        //helper.CalculatePurchaseAmount(totalAvailiableforLongTrade, ref BuySellStockListTotal, settings, Platform.CharlesSchwab);
                        _helper.CalculatePurchaseAmount(totalAvailiableforLongTrade, ref BuySellStockList, settings, Platform.ETrade);
                    }
                }
                else
                    Helper.Log(_logger, LogLevel.Warning, "No Stock was found to trade matching setting or 24h hold");
            }
            else
            {
                Helper.Log(_logger, LogLevel.Warning, $"Cash and margin balance below threshold ${totalAvailiableforLongTrade}  or trading disabled.");
            }
        }
        void UpdatePrices(ILogger _logger)
        {
            dynamic results = null;
            //Re-Get stock already in trade being tracked.
            stockTracker = _ctx.StockTracker2.AsNoTracking().Where(x => x.PriceSold == null && x.DateSold == null && x.TradePlatform == (int)Platform.ETrade && x.Status.Trim() == "Filled").ToList();
            //Loop through current stock and evaluate
            Helper.Log(_logger, LogLevel.Information, $"Checking current brought/shorted stock");
            foreach (var stock in stockTracker)
            {
                var st = stock;
                //var st = ctx.StockTracker.Where(x => x.Symbol == stock.Symbol).FirstOrDefault();
                Helper.Log(_logger, LogLevel.Information, $"Checking {stock.Symbol} - ({stock.Name})");
                try
                {
                    results = services2.GetQuoteAsync(stock.Symbol).Result;
                    if (!_helper.CheckMarketIndexes(results))
                    {
                        Helper.Log(_logger, LogLevel.Error, $"Error in quote lookup Update Prices {results.GetProperty("errors").ToString()}");
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    Helper.Log(_logger, LogLevel.Error, $"Error in quote lookup Update Prices {ex.Message}");
                    continue;
                }
               
                if ((bool)settings.EtradeDayTradeEnabled || (Helper.easternTime - stock.DatePurchased.Value).TotalDays > 1)
                {
                    //if ((_helper.easternTime - stock.DatePurchased.Value).TotalDays > 1)
                    //{
                        //trade after 24 hrs
                        try
                        {
                            Helper.Log(_logger, LogLevel.Warning, $"Stock {stock.Symbol} - ({stock.Name}) Time since traded Past 24 hrs {Math.Round(((Helper.easternTime - stock.DatePurchased.Value).TotalHours - 24), 2)}. Traded Price ${stock.PricePurchased} traded direction {Enum.GetName(typeof(TradeDirection), stock.TradeDirection)} stock current price {Convert.ToDecimal(results["lastPrice"].ToString())}");
                        }
                        catch (Exception ex)
                        {
                            Helper.Log(_logger, LogLevel.Error, $"Error in quote lookup Update Prices {ex.Message} {results.ToString()}");
                            continue;
                        }
                        try
                        {
                            AnalyiseOwnedStock(st, results);
                        }
                        catch (Exception ex)
                        {
                            Helper.Log(_logger, LogLevel.Error, $"Error in AnalyiseOwnedStock {ex.Message}-{ex.StackTrace}-{st.ToString()}-{results.ToString()}");
                        }

                    }
                    else
                    {
                        //not pass 24h period
                        try
                        {
                            Helper.Log(_logger, LogLevel.Warning, $"Stock {stock.Symbol} - ({stock.Name}) Cannot be transacted on before 24h period when day trade is disabled. Time til tradeable hrs {Math.Round((24 - (Helper.easternTime - stock.DatePurchased.Value).TotalHours), 2)}. Traded Price ${stock.PricePurchased} traded direction {Enum.GetName(typeof(TradeDirection), stock.TradeDirection)} stock current price {Convert.ToDecimal(results["lastPrice"].ToString())}");
                        }
                        catch (Exception ex)
                        {
                            Helper.Log(_logger, LogLevel.Error, $"Error in quote lookup Update Prices {ex.Message} {results.ToString()}");
                            continue;
                        }
                        UpdateLastPrice(Convert.ToDecimal(results["lastPrice"].ToString()), ref st);
                    }
                //}
                //else
                //{
                //    //just trade
                //    _helper.Log(_logger, LogLevel.Warning, $"Stock {stock.Symbol} - ({stock.Name}) Time since traded hrs {Math.Abs(Math.Round(((_helper.easternTime - stock.DatePurchased.Value).TotalHours - 24), 2))}. Traded Price ${stock.PricePurchased} traded direction {Enum.GetName(typeof(TradeDirection), stock.TradeDirection)} stock current price ${Convert.ToDecimal(results["lastPrice"].ToString())}");
                //    AnalyiseOwnedStock(st, results);
                //}
            }
        }
        //bool BuySellStock(StockTracker stocktracker, dynamic order, TradeDirection direction, int qty, bool IsReversed, ETradeService service)
        //{
        //    //return true;
        //    qty = Math.Abs(qty);
        //    dynamic results = service.SetOrder(order["Symbol"].ToString(), qty, direction);
        //    //if direction is buy or sell short, stocktracker will be null.
        //    //string output = System.Text.Json.JsonSerializer.Serialize(results);
        //    //((XElement)results).Element("code")?.Value?.ToString();
        //    try
        //    {
        //        var options = new JsonSerializerOptions
        //        {
        //            ReferenceHandler = ReferenceHandler.Preserve
        //        };
        //        string output = System.Text.Json.JsonSerializer.Serialize(results, options);
        //        if (output.Contains("errors"))
        //        {
        //            helper.Log(_logger, LogLevel.Error, $"Stock trade failed {order["Symbol"].ToString()} {output}");
        //            return false;
        //        }
        //        helper.Log(_logger, LogLevel.Information, $"Stock trade results {order["Symbol"].ToString()} Success");
        //        //SaveStockTracker(stocktracker, direction, qty, order, "Success");
        //        helper.SaveStockTracker(stocktracker, Platform.ETrade, direction, qty, IsReversed, order, "Success", _logger, ref ctx);
        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        helper.Log(_logger, LogLevel.Error, $"Error in buying stock - {ex.Message}");
        //    }
        //    return true;
        //}

        //bool ReverseStock(StockTracker stock, JsonObject? results)
        //{
        //    //reverse stock
        //    helper.Log(_logger, LogLevel.Information, $"Attempting Reversing stock {stock.Symbol} - ({stock.Name})");
        //    TradeDirection tradeDirection = (TradeDirection)stock.TradeDirection;
        //    int? shortCnt = 0; int? longCnt = 0;
        //    helper.Log(_logger, LogLevel.Information, $"Checking {stock.Symbol} - ({stock.Name}) Points");
        //    helper.CalculatePoints(results, true, false,false, ref shortCnt, ref longCnt);
        //    if ((longCnt) >= (settings.StockLongCount - 1) && tradeDirection == TradeDirection.SELL_SHORT)
        //    {
        //        BuySellStock(null, results, TradeDirection.BUY, stock.Quantity, true, services);
        //        helper.Log(_logger, LogLevel.Information, $"Reversing stock {stock.Symbol} - ({stock.Name}). Long score: {longCnt}. Short score {shortCnt}.");
        //    }
        //    else
        //    if ((shortCnt >= settings.StockShortCount - 1) && tradeDirection == TradeDirection.BUY)
        //    {
        //        BuySellStock(null, results, TradeDirection.SELL_SHORT, stock.Quantity, true, services);
        //        helper.Log(_logger, LogLevel.Information, $"Reversing stock {stock.Symbol} - ({stock.Name}). Long score: {longCnt}. Short score {shortCnt}.");
        //    }
        //    else
        //    {
        //        helper.Log(_logger, LogLevel.Information, $"Not Reversing stock {stock.Symbol} - ({stock.Name}) as it does not meet the points criteria. Long score: {longCnt}. Short score {shortCnt}.");
        //    }
        //    return true;
        //}

        bool UpdateLastPrice(decimal lastPrice, ref StockTracker2 stockTracker)
        {
            //ctx = new TmdContext();
            if ((stockTracker.TradeDirection == (int)TradeDirection.BUY && (lastPrice > stockTracker.LastPrice && lastPrice > stockTracker.PricePurchased)) || stockTracker.LastPrice is null)
            {
                stockTracker.LastPrice = lastPrice;
            }
            if ((stockTracker.TradeDirection == (int)TradeDirection.SELL_SHORT && (lastPrice < stockTracker.LastPrice && lastPrice < stockTracker.PricePurchased)) || stockTracker.LastPrice is null)
            {
                stockTracker.LastPrice = lastPrice;
            }
            stockTracker.DateUpdated = Helper.easternTime;
            try
            {
                _ctx.Entry(stockTracker).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                System.Threading.Thread.Sleep(200);
                _ctx.SaveChanges();
                _ctx.Entry(stockTracker).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
            }
            catch (Exception ex)
            {
                Helper.Log(_logger, LogLevel.Error, $"Price Update Failed! {ex.Message}");
            }
            return true;
        }

        void AnalyiseOwnedStock(StockTracker2 stock, JsonObject? results)
        {
            //calculate percent difference
            decimal percentDiffPricePurchased = 0m;
            decimal percentDiffLastPrice = 0m;
            decimal lastPrice = (stock.LastPrice ?? 0);
            decimal pricePurchased = (stock.PricePurchased ?? 0);
            decimal currentPrice = Convert.ToDecimal(results["lastPrice"].ToString());
            try
            {
                percentDiffPricePurchased = Math.Abs(((pricePurchased - currentPrice) / ((pricePurchased + currentPrice) / 2)) * 100);
                percentDiffLastPrice = Math.Abs(((lastPrice - currentPrice) / ((lastPrice + currentPrice) / 2)) * 100);
            }
            catch (Exception ex)
            {
                Helper.Log(_logger, LogLevel.Error, $"{ex.Message} at AnalyiseOwnedStock Percent Diff. Setting to zero.");
            }

            if (stock.TradeDirection == (int)TradeDirection.SELL_SHORT) //selling short
            {
                //if (currentPrice > pricePurchased)
                //if (currentPrice > lastPrice && percentDiffLastPrice >= settings.StopLossPercentPurchasedStock && percentDiffPricePurchased >= 3.5m)
                //if ((currentPrice > lastPrice) && (pricePurchased > lastPrice) && (pricePurchased > currentPrice) && (percentDiffLastPrice >= settings.StopLossPercentLastPriceCrypto && percentDiffPricePurchased >= 3.5m))
                //if (((currentPrice > lastPrice) && (percentDiffLastPrice >= settings.StopLossPercentLastPriceStock)) ||
                //    ((currentPrice > pricePurchased) && (percentDiffPricePurchased >= settings.StopLossPercentPurchasedStock)) ||
                //    (pricePurchased > lastPrice) && (pricePurchased > currentPrice) && percentDiffPricePurchased >= 3.5m)
                if (((currentPrice > lastPrice) &&
                    (pricePurchased > currentPrice) &&
                    (percentDiffPricePurchased >= 3.5m)) ||
                    ((pricePurchased > currentPrice) && (percentDiffPricePurchased >= settings.StopLossPercentPurchasedStock))
                    )
                {
                    //if (percentDiffPricePurchased > settings.StopLossPercentPurchasedStock)
                    //{
                        //buy to cover
                        Helper.Log(_logger, LogLevel.Information, $"stock {stock.Symbol} - ({stock.Name}) has fallen by more then about {Math.Round(percentDiffPricePurchased, 4)}% since price purchased. Buying to cover.");
                        //_helper.BuySellStock(stock, results, TradeDirection.BUY_TO_COVER, stock.Quantity, false, eTradeService);
                        //Attempt to reverse stock
                        //if (stock.IsReversed != null && !(bool)stock.IsReversed)
                        //    ReverseStock(stock, results);
                    //}
                    //else
                    //{
                    //    //Update Price
                    //    Helper.Log(_logger, LogLevel.Information, $"stock {stock.Symbol} - ({stock.Name}) still within about {Math.Round(percentDiffPricePurchased, 4)}% tolerance since price purchased. Updating last Price.");
                    //    UpdateLastPrice(Convert.ToDecimal(results["lastPrice"].ToString()), ref stock);
                    //}
                }
                //else
                //if (currentPrice > lastPrice)
                //{
                //    if (percentDiffLastPrice > settings.StopLossPercentLastPriceStock)
                //    {
                //        //buy to cover
                //        Helper.Log(_logger, LogLevel.Information, $"stock {stock.Symbol} - ({stock.Name}) has fallen by more then about {Math.Round(percentDiffLastPrice, 4)}% since last price. Buying to cover.");
                //        UpdateLastPrice(Convert.ToDecimal(results["lastPrice"].ToString()), ref stock);
                //        _helper.BuySellStock(stock, results, TradeDirection.BUY_TO_COVER, stock.Quantity, false, eTradeService);
                //    }
                //    else
                //    {
                //        //Update Price
                //        //_helper.Log(_logger, LogLevel.Information, $"stock {stock.Symbol} - ({stock.Name}) still within about {Math.Round(percentDiffLastPrice, 4)}% tolerance since last price. Updating last Price.");
                //        //UpdateLastPrice(Convert.ToDecimal(results["lastPrice"].ToString()), ref stock);
                //    }
                //}
                //else if (currentPrice <= pricePurchased)
                else //if (currentPrice <= lastPrice)
                {
                    //update price
                    Helper.Log(_logger, LogLevel.Information, $"stock {stock.Symbol} - ({stock.Name}) still within about {Math.Round(percentDiffLastPrice, 4)}% tolerance since purchase. Updating last Price.");
                    UpdateLastPrice(Convert.ToDecimal(results["lastPrice"].ToString()), ref stock);
                }
            }
            else //Going Long
            {
                //if (currentPrice < pricePurchased)
                //if (currentPrice < lastPrice && percentDiffLastPrice >= settings.StopLossPercentPurchasedStock && percentDiffPricePurchased >= 3.5m)
                //if ((currentPrice < lastPrice) && (pricePurchased < lastPrice) && (pricePurchased < currentPrice) && (percentDiffLastPrice >= settings.StopLossPercentLastPriceCrypto && percentDiffPricePurchased >= 3.5m))
                //if (((currentPrice < lastPrice) && (percentDiffLastPrice >= settings.StopLossPercentLastPriceStock)) ||
                //    ((currentPrice < pricePurchased) && (percentDiffPricePurchased >= settings.StopLossPercentPurchasedStock)) ||
                //    (pricePurchased < lastPrice) && (pricePurchased < currentPrice) && percentDiffPricePurchased >= 3.5m)
                if (((currentPrice < lastPrice) &&
                    (pricePurchased < currentPrice) &&
                    (percentDiffPricePurchased >= 3.5m)) ||
                    ((pricePurchased < currentPrice) && (percentDiffPricePurchased >= settings.StopLossPercentPurchasedStock))
                    )
                {
                    //if (percentDiffPricePurchased > settings.StopLossPercentPurchasedStock)
                    //{
                    //buy to cover
                    Helper.Log(_logger, LogLevel.Information, $"stock {stock.Symbol} - ({stock.Name}) has fallen by more then about {Math.Round(percentDiffPricePurchased, 4)}% since price purchased. Buying to cover.");
                        UpdateLastPrice(Convert.ToDecimal(results["lastPrice"].ToString()), ref stock);
                        //_helper.BuySellStock(stock, results, TradeDirection.SELL, stock.Quantity, false, eTradeService);
                        //Attempt to reverse stock
                        //if (stock.IsReversed != null && !(bool)stock.IsReversed)
                        //    ReverseStock(stock, results);
                    //}
                    //else
                    //{
                        //Update Price
                        //Helper.Log(_logger, LogLevel.Information, $"stock {stock.Symbol} - ({stock.Name}) still within about {Math.Round(percentDiffPricePurchased, 4)}% tolerance since price purchased. Updating last Price.");
                        //UpdateLastPrice(Convert.ToDecimal(results["lastPrice"].ToString()), ref stock);
                    //}
                }
                //else
                //if (currentPrice < lastPrice)
                //{
                //    if (percentDiffLastPrice > settings.StopLossPercentLastPriceStock)
                //    {
                //        //sell
                //        Helper.Log(_logger, LogLevel.Information, $"stock {stock.Symbol} - ({stock.Name}) has fallen by more then about {Math.Round(percentDiffLastPrice, 4)}% since last price. Buying to cover.");
                //        UpdateLastPrice(Convert.ToDecimal(results["lastPrice"].ToString()), ref stock);
                //        _helper.BuySellStock(stock, results, TradeDirection.SELL, stock.Quantity, false, eTradeService);
                //    }
                //    else
                //    {
                //        //Update Price
                //        //_helper.Log(_logger, LogLevel.Information, $"stock {stock.Symbol} - ({stock.Name}) still within about {Math.Round(percentDiffLastPrice, 4)}% tolerance since last price. Updating last Price.");
                //        //UpdateLastPrice(Convert.ToDecimal(results["lastPrice"].ToString()), ref stock);
                //    }
                //}
                else //if (currentPrice >= lastPrice)
                {
                    //update price
                    Helper.Log(_logger, LogLevel.Information, $"stock {stock.Symbol} - ({stock.Name}) still within about {Math.Round(percentDiffLastPrice, 4)}% tolerance since purchase. Updating last Price.");
                    UpdateLastPrice(Convert.ToDecimal(results["lastPrice"].ToString()), ref stock);
                }
            }
        }
        //void CalculatePurchaseAmount(decimal? availiabletoTrade, ref List<BuySellCommodity> BuySellStockList)
        //{
        //    var stockTracker = ctx?.StockTracker?.Where(x => x.PriceSold == null && x.DateSold == null && x.Status.Trim() == "Filled" && x.TradePlatform == (int)Platform.ETrade).ToList();
        //    if (stockTracker?.Count() >= (settings?.MaxNumberofStockstoBuy / 2))
        //        return;
        //    int stockCnt = 0;
        //    //decimal? perStockSet = (BuySellStockList.Count() < (settings.MaxNumberofStockstoBuy - BuySellStockList.Count())) ? availiabletoTrade / BuySellStockList.Count() : availiabletoTrade / (settings.MaxNumberofStockstoBuy - stockTracker.Count());//(availiabletoTrade / BuySellStockList.Count) > (availiabletoTrade * (settings.MaxPercentofAvailBalancePerOrder / 100)) ? (availiabletoTrade * (settings.MaxPercentofAvailBalancePerOrder / 100)) : (availiabletoTrade / BuySellStockList.Count);
        //    if ((int)(settings.MaxNumberofStockstoBuy / 2) - stockTracker.Count() < 1)
        //        return;
        //    decimal? perStockSet = availiabletoTrade / Math.Abs((int)(settings.MaxNumberofStockstoBuy / 2) - stockTracker.Count());
        //    var stockList = new List<BuySellCommodity>(BuySellStockList);
        //    foreach (var item in stockList)
        //    {
        //        if ((stockTracker.Count() + stockCnt) >= settings.MaxNumberofStockstoBuy)
        //            break;
        //        //var result = item.Key;
        //        //var direction = item.Value;
        //        var result = item.Results;
        //        var direction = item.Direction;
        //        var lastPrice = Convert.ToDecimal(result?["lastPrice"]?.ToString());
        //        //var qty = Convert.ToInt32(perStockSet / lastPrice);
        //        var qty = Math.Abs((Convert.ToInt32(perStockSet / lastPrice) > settings.MaxNumberofSharesPerStock) ? (int)settings.MaxNumberofSharesPerStock : Convert.ToInt32(perStockSet / lastPrice));
        //        if (qty >= 2 && lastPrice >= 5m)
        //        {
        //            helper.BuySellStock(null, result, direction, qty, false, services);
        //            stockCnt++;
        //            BuySellStockList.Remove(item);
        //        }
        //    }
        //}
        ~ExecStockPricePredictionETrade()
        {
            _ctx?.Dispose();
            _ctx = null;
            _helper = null;
        }
    }
}
