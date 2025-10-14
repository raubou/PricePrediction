using Microsoft.Extensions.Logging;
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
        ETradeService? services = null;
        SchwabServices? services2 = null;
        TmdContext? ctx = null;
        RulesEngine? settings;
        List<StockTracker> stockTracker = null;
        TaAPIService? taapiService = null;
        //decimal totalAvailiableforLongTrade = 0m;
        Helper? helper = null;
        public ExecStockPricePredictionETrade(ILogger logger, ETradeSettings settings, IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _settings = settings;
            services = new ETradeService(_logger, _settings, _httpClientFactory);
            ctx = new TmdContext();
            helper = new Helper(_logger, _settings2, _settings, _httpClientFactory);
        }
        public ExecStockPricePredictionETrade(ILogger logger, ETradeSettings settings, SchwabSettings settings2, IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _settings = settings;
            _settings2 = settings2;
            services = new ETradeService(_logger, _settings, _httpClientFactory);
            services2 = new SchwabServices(_logger, _settings2, _httpClientFactory);
            ctx = new TmdContext();
            helper = new Helper(_logger, _settings2, _settings, _httpClientFactory);
        }
        public ExecStockPricePredictionETrade(ILogger logger, ETradeSettings settings, SchwabSettings settings2,TaAPISettings taAPISettings, IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _settings = settings;
            _settings2 = settings2;
            _taapiSettings = taAPISettings;
            services = new ETradeService(_logger, _settings, _httpClientFactory);
            services2 = new SchwabServices(_logger, _settings2, _httpClientFactory);
            taapiService = new TaAPIService(_logger, _taapiSettings, _httpClientFactory);
            ctx = new TmdContext();
            helper = new Helper(_logger, _settings2, _settings, _httpClientFactory);
        }
        public void Exec(decimal totalAvailiableforLongTrade, ref List<BuySellCommodity>? BuySellStockList)
        {
        //    int authRetry = 0;
        //accountAuthRetry:
        //    //Get Account Info
        //    XElement account = null;
        //    try
        //    {
        //        helper.Log(_logger, LogLevel.Information, $"Get account info for ETrade");
        //        account = services.Account();
        //    }
        //    catch (Exception ex)
        //    {
        //        if (authRetry < 3)
        //        {
        //            helper.Log(_logger, LogLevel.Error, "Authenication from ETrade failed. Retrying");
        //            authRetry++;
        //            goto accountAuthRetry;
        //        }
        //        else
        //        {
        //            helper.Log(_logger, LogLevel.Error, $"Authenication from schwab failed {authRetry} Retries. Exiting");
        //            return;
        //        }
        //    }

        //    //helper.Log(_logger, LogLevel.Information, $"Get account info for ETrade");

        //    XElement securitiesAccount = null;
        //    try
        //    {
        //        securitiesAccount = account.Element("Computed");
        //    }
        //    catch (Exception ex)
        //    {
        //        if (authRetry < 3)
        //        {
        //            helper.Log(_logger, LogLevel.Error, "Authenication from ETrade failed. Retrying");
        //            authRetry++;
        //            goto accountAuthRetry;
        //        }
        //        else
        //        {
        //            helper.Log(_logger, LogLevel.Error, $"Authenication from schwab failed {authRetry} Retries. Exiting");
        //            return;
        //        }
        //    }

            //decimal cash = Convert.ToDecimal(securitiesAccount.Element("cashBuyingPower").Value.ToString());
            //decimal margin = Convert.ToDecimal(securitiesAccount.Element("marginBuyingPower").Value.ToString());
            ////totalAvailiableforLongTrade = Math.Round((cash + margin), 2);
            //totalAvailiableforLongTrade = Math.Round((cash + margin) * .9m, 2);
            ////totalAvailiableforLongTrade = 200m;

            //helper.Log(_logger, LogLevel.Information, $"Cash Balance ${cash}. Margin Balance ${margin}. Total Availiable ${totalAvailiableforLongTrade}");

            //if (helper.IsNotHolidayorWeekend())
                Run(totalAvailiableforLongTrade, ref BuySellStockList);
            //else
            //    helper.Log(_logger, LogLevel.Warning, "Time is outside of trading window.");
        }
      
        private void Run(decimal totalAvailiableforLongTrade, ref List<BuySellCommodity>? BuySellStockList)
        {
            //int authRetry = 0;
            //GetSettings
            settings = ctx.RulesEngine.FirstOrDefault();
            helper.Log(_logger, LogLevel.Information, $"Get settings for ETrade");

            //helper.Log(_logger, LogLevel.Information, $"Check for Check Order Status");
            //CheckOrderStatus();

            //Check trades not tracked.
            //helper.Log(_logger, LogLevel.Information, $"Check for orphan trades");
            //GetOrphanETrades();

            //Update Prices
            helper.Log(_logger, LogLevel.Information, $"Updating Latest Prices on Tracker");
            UpdatePrices();

            if (totalAvailiableforLongTrade > 100 && (bool)settings.EtradeAnalysisTradeEnabled)
            {
                //Get stock already in trade being tracked.
                //stockTracker = ctx.StockTracker.Where(x => x.PriceSold == null && x.DateSold == null && x.Status.Trim() == "Filled" /*&& x.TradePlatform == (int)Platform.ETrade*/).ToList();
                //var stockTrackerList = stockTracker.Select(y => y.Symbol);
                //helper.Log(_logger, LogLevel.Information, $"Get exiting stock tracker");

                ////Get list of stocks to trade not in tracker
                //var stockList = ctx.StockList.Where(x => !stockTrackerList.Contains(x.Symbol) && !x.ExcludeFromAutomation).OrderBy(y => Guid.NewGuid()).ToList();
                //helper.Log(_logger, LogLevel.Information, $"Get stock list to trade");

                ////stock sold/brought to cover in the last 24 hrs
                //DateTime? minDate = DateTime.MinValue;
                //var StockHold24Hr = ctx.StockTracker.ToList().Where(x => x.PriceSold != null && x.DateSold != null && Math.Abs((x.DateSold - DateTime.Now).Value.TotalHours) < 24).ToList().Select(y => new { Symbol = y.Symbol, DateSold = y.DateSold, DateUpdated = y.DateUpdated }).ToList();
                ////Include cancelled item for a 24h hold
                //StockHold24Hr.AddRange(ctx.StockTracker.ToList().Where(x => x.Status == "Cancelled" && Math.Abs((x.DateUpdated - DateTime.Now).Value.TotalHours) < 24).ToList().Select(y => new { Symbol = y.Symbol, DateSold = y.DateSold ?? minDate, DateUpdated = y.DateUpdated }).ToList());
                ////Loop through and evaluate.
                //int? shortCnt = 0;
                //int? longCnt = 0;
                //int? stockCnt = 0;
                //int? vixsandpLongCnt = 0;
                //int? vixsandpShortCnt = 0;

                //var vix = services.Quote("VIX");
                //var sandp = services.Quote("SPX");
                //helper.CheckVixandSandP(vix, sandp, (settings?.StockLongCount - 1), (settings?.StockShortCount - 1), _logger, ref vixsandpLongCnt, ref vixsandpShortCnt);

                //helper.Log(_logger, LogLevel.Information, $"Iterate stock list, check indicators for buy/sell short");

                //BuySellStockList = new List<BuySellCommodity>();

                //var stocktrackerPlatform = ctx.StockTracker.Where(x => x.PriceSold == null && x.DateSold == null && x.Status == "Filled" && x.TradePlatform == (int)Platform.ETrade).ToList();
                //if (stocktrackerPlatform.Count < settings.MaxNumberofStockstoBuy)
                //foreach (var stock in stockList)
                //{
                //    if (stock?.QuoteRetry >= 5)
                //        continue;
                //    ////skip loop if stock amount equals max amount to own
                //    //if ((stockTracker.Count() + stockCnt) >= settings.MaxNumberofStockstoBuy)
                //    //    break;

                //    //Check it stock has been brought in the las 24 hrs
                //    if (StockHold24Hr.Any(y => y.Symbol == stock.Symbol))
                //    {
                //        //if stock was cancelled, check updated date
                //        try
                //        {
                //            helper.Log(_logger, LogLevel.Warning, $"Stock {stock.Symbol} - ({stock.Name}) purchased {Math.Round(Math.Abs((StockHold24Hr.Where(y => y.Symbol == stock.Symbol).Select(z => z.DateSold).FirstOrDefault() - DateTime.Now).Value.TotalHours), 2)} hours ago was purchased less than 24h ago, skipping");
                //        }
                //        catch (Exception ex)
                //        {
                //            helper.Log(_logger, LogLevel.Warning, $"Stock {stock.Symbol} - ({stock.Name}) cancelled {Math.Round(Math.Abs((StockHold24Hr.Where(y => y.Symbol == stock.Symbol).Select(z => z.DateUpdated).FirstOrDefault() - DateTime.Now).Value.TotalHours), 2)} hours ago was purchased less than 24h ago, skipping");
                //        }
                //        continue;
                //    }
                //    helper.Log(_logger, LogLevel.Information, $"Checking {stock.Symbol} - ({stock.Name})");

                //    dynamic results = null;
                //    try
                //    {
                //        results = services.Quote(stock.Symbol);
                //    }
                //    catch (Exception ex)
                //    {
                //        helper.Log(_logger, LogLevel.Error, $"Error in retrieving quote for {stock.Symbol} - ({stock.Name}) {ex.Message.ToString()} - {ex.StackTrace.ToString()}");
                //        var st = stock;
                //        st.QuoteRetry = (st.QuoteRetry == null) ? 1 : st.QuoteRetry + 1;
                //        ctx.StockList.Attach(st);
                //        ctx.Entry(st).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                //        ctx.SaveChanges();
                //        ctx.Entry(st).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                //        continue;
                //    }

                //    if (results is JsonObject)
                //    {
                //        if (((JsonObject)results).TryGetPropertyValue("errors", out JsonNode errors))
                //        {
                //            //failed to get quote.
                //            helper.Log(_logger, LogLevel.Error, $"Failed to get quote for {stock.Symbol} - ({stock.Name}) {errors.ToString()}");
                //            var st = stock;
                //            st.QuoteRetry = (st.QuoteRetry == null) ? 1 : st.QuoteRetry + 1;
                //            ctx.StockList.Attach(st);
                //            ctx.Entry(st).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                //            ctx.SaveChanges();
                //            ctx.Entry(st).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                //            continue;
                //        }
                //    }
                //    else if(results is JsonElement)
                //    {
                //        if (((JsonElement)results).TryGetProperty("errors", out JsonElement errors))
                //        {
                //            //failed to get quote.
                //            helper.Log(_logger, LogLevel.Error, $"Failed to get quote for {stock.Symbol} - ({stock.Name}) {errors.ToString()}");
                //            var st = stock;
                //            st.QuoteRetry = (st.QuoteRetry == null) ? 1 : st.QuoteRetry + 1;
                //            ctx.StockList.Attach(st);
                //            ctx.Entry(st).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                //            ctx.SaveChanges();
                //            ctx.Entry(st).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                //            continue;
                //        }
                //    }
                //    else if(results is XElement)
                //    {
                //        if (((XElement)results).Element("Messages")?.Element("Message")?.Value != null)
                //        {
                //            //failed to get quote.
                //            helper.Log(_logger, LogLevel.Error, $"Failed to get quote for {stock.Symbol} - ({stock.Name}) {((XElement)results).Element("Messages")?.Element("Message")?.Value?.ToString()}");
                //            var st = stock;
                //            st.QuoteRetry = (st.QuoteRetry == null) ? 1 : st.QuoteRetry + 1;
                //            ctx.StockList.Attach(st);
                //            ctx.Entry(st).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                //            ctx.SaveChanges();
                //            ctx.Entry(st).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                //            continue;
                //        }
                //    }

                //    if (Convert.ToDecimal(results["volume"].ToString()) < 50000)
                //    {
                //        helper.Log(_logger, LogLevel.Information, $"Not buying/selling short {results["Symbol"].ToString()} - ({results["Symbol"].ToString()}). Volume fall below min of 50000");
                //        continue;
                //    }

                //    if (Convert.ToDecimal(results["lastPrice"].ToString()) >= settings.MinStockPrice /*&& !stock.ExcludeFromAutomation*/)
                //    {
                //        helper.CalculatePoints(results, true, false, true, ref shortCnt, ref longCnt);
                //    }
                //    else
                //    {
                //        helper.Log(_logger, LogLevel.Information, $"Not buying/selling short {results["Symbol"].ToString()} - ({results["Symbol"].ToString()}). Stock fall below min stock price or is excluded");
                //    }

                //    helper.Log(_logger, LogLevel.Information, $"{stock.Symbol} - ({stock.Name}) Score. Long score: {longCnt}. Short score {shortCnt}");

                //    if ((longCnt + vixsandpLongCnt) >= settings.StockLongCount/*9*/)
                //    {
                //        helper.Log(_logger, LogLevel.Information, $"Adding {stock.Symbol} - ({stock.Name}) to potential long list.");
                //        BuySellStockList.Add(new BuySellCommodity((longCnt + vixsandpLongCnt), TradeDirection.BUY, results));
                //        stockCnt++;
                //    }
                //    else
                //    if ((shortCnt + vixsandpShortCnt) >= settings.StockShortCount/*9 */&& (bool)settings.ShortEnabled)
                //    {
                //        helper.Log(_logger, LogLevel.Information, $"Adding {stock.Symbol} - ({stock.Name}) to potential short list.");
                //        BuySellStockList.Add(new BuySellCommodity((shortCnt + vixsandpShortCnt), TradeDirection.SELL_SHORT, results));
                //        stockCnt++;
                //    }
                //    else
                //    {
                //        helper.Log(_logger, LogLevel.Information, $"Not buying/selling short {stock.Symbol} - ({stock.Name}). Long score: {longCnt}. Short score {shortCnt}");
                //    }
                //}
                if (BuySellStockList.Count > 0)
                {
                    var stockTrackerBuy = ctx.StockTracker.Where(x => x.PriceSold == null && x.DateSold == null && x.Status == "Filled" && x.TradePlatform == (int)Platform.ETrade).ToList();
                    var numOfNeededStock = (int)((settings.MaxNumberofStockstoBuy / 2) - stockTrackerBuy.Count);
                    if (numOfNeededStock > 0)
                    {
                        List<BuySellCommodity> BuySellStockListLong = BuySellStockList.Where(x => x.Direction == TradeDirection.BUY).OrderByDescending(x => x.Points).ThenBy(x => Convert.ToDecimal(x.Results["RSI"].ToString())).Take(numOfNeededStock).ToList();
                        List<BuySellCommodity> BuySellStockListShort = new List<BuySellCommodity>();
                        if ((numOfNeededStock - BuySellStockListLong.Count()) > 0)
                            BuySellStockListShort = BuySellStockList.Where(x => x.Direction == TradeDirection.SELL_SHORT).OrderByDescending(x => x.Points).ThenByDescending(x => Convert.ToDecimal(x.Results["RSI"].ToString())).Take((numOfNeededStock - BuySellStockListLong.Count())).ToList();
                        //CalculatePurchaseAmount(totalAvailiableforLongTrade, ref BuySellStockList);
                        List<BuySellCommodity> BuySellStockListTotal = new List<BuySellCommodity>();
                        BuySellStockListTotal.AddRange(BuySellStockListLong);
                        BuySellStockListTotal.AddRange(BuySellStockListShort);
                        CalculatePurchaseAmount(totalAvailiableforLongTrade, ref BuySellStockListTotal);
                    }
                }
                else
                    helper.Log(_logger, LogLevel.Warning, "No Stock was found to trade matching setting or 24h hold");
            }
            else
            {
                helper.Log(_logger, LogLevel.Error, $"Cash and margin balance below threshold ${totalAvailiableforLongTrade}  or trading disabled.");
            }
        }
        void UpdatePrices()
        {
            dynamic results = null;
            //Re-Get stock already in trade being tracked.
            stockTracker = ctx.StockTracker.Where(x => x.PriceSold == null && x.DateSold == null && x.TradePlatform == (int)Platform.ETrade && x.Status.Trim() == "Filled").ToList();
            //Loop through current stock and evaluate
            helper.Log(_logger, LogLevel.Information, $"Checking current brought/shorted stock");
            foreach (var stock in stockTracker)
            {
                var st = stock;
                //var st = ctx.StockTracker.Where(x => x.Symbol == stock.Symbol).FirstOrDefault();
                helper.Log(_logger, LogLevel.Information, $"Checking {stock.Symbol} - ({stock.Name})");
                try
                {
                    results = services2.GetQuote(stock.Symbol);
                    if (!helper.CheckMarketIndexes(results))
                    {
                        helper.Log(_logger, LogLevel.Error, $"Error in quote lookup Update Prices {results.GetProperty("errors").ToString()}");
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    helper.Log(_logger, LogLevel.Error, $"Error in quote lookup Update Prices {ex.Message}");
                    continue;
                }
               
                if (!(bool)settings.EtradeDayTradeEnabled)
                {
                    if ((helper.easternTime - stock.DatePurchased.Value).TotalDays > 1)
                    {
                        //trade after 24 hrs
                        try
                        {
                            helper.Log(_logger, LogLevel.Warning, $"Stock {stock.Symbol} - ({stock.Name}) Time since traded Past 24 hrs {Math.Round(((helper.easternTime - stock.DatePurchased.Value).TotalHours - 24), 2)}. Traded Price ${stock.PricePurchased} traded direction {Enum.GetName(typeof(TradeDirection), stock.TradeDirection)} stock current price {Convert.ToDecimal(results["lastPrice"].ToString())}");
                        }
                        catch (Exception ex)
                        {
                            helper.Log(_logger, LogLevel.Error, $"Error in quote lookup Update Prices {ex.Message} {results.ToString()}");
                            continue;
                        }
                        try
                        {
                            AnalyiseOwnedStock(st, results);
                        }
                        catch (Exception ex)
                        {
                            helper.Log(_logger, LogLevel.Error, $"Error in AnalyiseOwnedStock {ex.Message}-{ex.StackTrace}-{st.ToString()}-{results.ToString()}");
                        }

                    }
                    else
                    {
                        //not pass 24h period
                        try
                        {
                            helper.Log(_logger, LogLevel.Warning, $"Stock {stock.Symbol} - ({stock.Name}) Cannot be transacted on before 24h period when day trade is disabled. Time til tradeable hrs {Math.Round((24 - (helper.easternTime - stock.DatePurchased.Value).TotalHours), 2)}. Traded Price ${stock.PricePurchased} traded direction {Enum.GetName(typeof(TradeDirection), stock.TradeDirection)} stock current price {Convert.ToDecimal(results["lastPrice"].ToString())}");
                        }
                        catch (Exception ex)
                        {
                            helper.Log(_logger, LogLevel.Error, $"Error in quote lookup Update Prices {ex.Message} {results.ToString()}");
                            continue;
                        }
                        UpdateLastPrice(Convert.ToDecimal(results["lastPrice"].ToString()), ref st);
                    }
                }
                else
                {
                    //just trade
                    helper.Log(_logger, LogLevel.Warning, $"Stock {stock.Symbol} - ({stock.Name}) Time since traded hrs {Math.Abs(Math.Round(((helper.easternTime - stock.DatePurchased.Value).TotalHours - 24), 2))}. Traded Price ${stock.PricePurchased} traded direction {Enum.GetName(typeof(TradeDirection), stock.TradeDirection)} stock current price ${Convert.ToDecimal(results["lastPrice"].ToString())}");
                    AnalyiseOwnedStock(st, results);
                }
            }
        }
        bool BuySellStock(StockTracker stocktracker, dynamic order, TradeDirection direction, int qty, bool IsReversed, ETradeService service)
        {
            //return true;
            qty = Math.Abs(qty);
            dynamic results = service.SetOrder(order["Symbol"].ToString(), qty, direction);
            //if direction is buy or sell short, stocktracker will be null.
            //string output = System.Text.Json.JsonSerializer.Serialize(results);
            //((XElement)results).Element("code")?.Value?.ToString();
            try
            {
                var options = new JsonSerializerOptions
                {
                    ReferenceHandler = ReferenceHandler.Preserve
                };
                string output = System.Text.Json.JsonSerializer.Serialize(results, options);
                if (output.Contains("errors"))
                {
                    helper.Log(_logger, LogLevel.Error, $"Stock trade failed {order["Symbol"].ToString()} {output}");
                    return false;
                }
                helper.Log(_logger, LogLevel.Information, $"Stock trade results {order["Symbol"].ToString()} Success");
                //SaveStockTracker(stocktracker, direction, qty, order, "Success");
                helper.SaveStockTracker(stocktracker, Platform.ETrade, direction, qty, IsReversed, order, "Success", _logger, ref ctx);
                return true;
            }
            catch (Exception ex)
            {
                helper.Log(_logger, LogLevel.Error, $"Error in buying stock - {ex.Message}");
            }
            return true;
        }

        bool ReverseStock(StockTracker stock, JsonObject? results)
        {
            //reverse stock
            helper.Log(_logger, LogLevel.Information, $"Attempting Reversing stock {stock.Symbol} - ({stock.Name})");
            TradeDirection tradeDirection = (TradeDirection)stock.TradeDirection;
            int? shortCnt = 0; int? longCnt = 0;
            helper.Log(_logger, LogLevel.Information, $"Checking {stock.Symbol} - ({stock.Name}) Points");
            helper.CalculatePoints(results, true, false,false, ref shortCnt, ref longCnt);
            if ((longCnt) >= (settings.StockLongCount - 1) && tradeDirection == TradeDirection.SELL_SHORT)
            {
                BuySellStock(null, results, TradeDirection.BUY, stock.Quantity, true, services);
                helper.Log(_logger, LogLevel.Information, $"Reversing stock {stock.Symbol} - ({stock.Name}). Long score: {longCnt}. Short score {shortCnt}.");
            }
            else
            if ((shortCnt >= settings.StockShortCount - 1) && tradeDirection == TradeDirection.BUY)
            {
                BuySellStock(null, results, TradeDirection.SELL_SHORT, stock.Quantity, true, services);
                helper.Log(_logger, LogLevel.Information, $"Reversing stock {stock.Symbol} - ({stock.Name}). Long score: {longCnt}. Short score {shortCnt}.");
            }
            else
            {
                helper.Log(_logger, LogLevel.Information, $"Not Reversing stock {stock.Symbol} - ({stock.Name}) as it does not meet the points criteria. Long score: {longCnt}. Short score {shortCnt}.");
            }
            return true;
        }

        bool UpdateLastPrice(decimal lastPrice, ref StockTracker stockTracker)
        {
            ctx = new TmdContext();
            if ((stockTracker.TradeDirection == (int)TradeDirection.BUY && (lastPrice > stockTracker.LastPrice && lastPrice > stockTracker.PricePurchased)) || stockTracker.LastPrice is null)
            {
                stockTracker.LastPrice = lastPrice;
            }
            if ((stockTracker.TradeDirection == (int)TradeDirection.SELL_SHORT && (lastPrice < stockTracker.LastPrice && lastPrice < stockTracker.PricePurchased)) || stockTracker.LastPrice is null)
            {
                stockTracker.LastPrice = lastPrice;
            }
            stockTracker.DateUpdated = helper.easternTime;
            try
            {
                ctx.Entry(stockTracker).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                System.Threading.Thread.Sleep(200);
                ctx.SaveChanges();
                ctx.Entry(stockTracker).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
            }
            catch (Exception ex)
            {
                helper.Log(_logger, LogLevel.Error, $"Price Update Failed! {ex.Message}");
            }
            return true;
        }

        void AnalyiseOwnedStock(StockTracker stock, JsonObject? results)
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
                helper.Log(_logger, LogLevel.Error, $"{ex.Message} at AnalyiseOwnedStock Percent Diff. Setting to zero.");
            }

            if (stock.TradeDirection == (int)TradeDirection.SELL_SHORT) //selling short
            {
                if (currentPrice > pricePurchased)
                {
                    if (percentDiffPricePurchased > settings.StopLossPercentPurchasedStock)
                    {
                        //buy to cover
                        helper.Log(_logger, LogLevel.Information, $"stock {stock.Symbol} - ({stock.Name}) has fallen by more then about {Math.Round(percentDiffPricePurchased, 4)}% since price purchased. Buying to cover.");
                        BuySellStock(stock, results, TradeDirection.BUY_TO_COVER, stock.Quantity, false, services);
                        //Attempt to reverse stock
                        if (stock.IsReversed != null && !(bool)stock.IsReversed)
                            ReverseStock(stock, results);
                    }
                    else
                    {
                        //Update Price
                        helper.Log(_logger, LogLevel.Information, $"stock {stock.Symbol} - ({stock.Name}) still within about {Math.Round(percentDiffPricePurchased, 4)}% tolerance since price purchased. Updating last Price.");
                        UpdateLastPrice(Convert.ToDecimal(results["lastPrice"].ToString()), ref stock);
                    }
                }
                else
                if (currentPrice > lastPrice)
                {
                    if (percentDiffLastPrice > settings.StopLossPercentLastPriceStock)
                    {
                        //buy to cover
                        helper.Log(_logger, LogLevel.Information, $"stock {stock.Symbol} - ({stock.Name}) has fallen by more then about {Math.Round(percentDiffLastPrice, 4)}% since last price. Buying to cover.");
                        UpdateLastPrice(Convert.ToDecimal(results["lastPrice"].ToString()), ref stock);
                        BuySellStock(stock, results, TradeDirection.BUY_TO_COVER, stock.Quantity, false, services);
                    }
                    else
                    {
                        //Update Price
                        //helper.Log(_logger, LogLevel.Information, $"stock {stock.Symbol} - ({stock.Name}) still within about {Math.Round(percentDiffLastPrice, 4)}% tolerance since last price. Updating last Price.");
                        //UpdateLastPrice(Convert.ToDecimal(results["lastPrice"].ToString()), ref stock);
                    }
                }
                //else if (currentPrice <= pricePurchased)
                else if (currentPrice <= lastPrice)
                {
                    //update price
                    helper.Log(_logger, LogLevel.Information, $"stock {stock.Symbol} - ({stock.Name}) still within about {Math.Round(percentDiffLastPrice, 4)}% tolerance since purchase. Updating last Price.");
                    UpdateLastPrice(Convert.ToDecimal(results["lastPrice"].ToString()), ref stock);
                }
            }
            else //Going Long
            {
                if (currentPrice < pricePurchased)
                {
                    if (percentDiffPricePurchased > settings.StopLossPercentPurchasedStock)
                    {
                        //buy to cover
                        helper.Log(_logger, LogLevel.Information, $"stock {stock.Symbol} - ({stock.Name}) has fallen by more then about {Math.Round(percentDiffPricePurchased, 4)}% since price purchased. Buying to cover.");
                        UpdateLastPrice(Convert.ToDecimal(results["lastPrice"].ToString()), ref stock);
                        BuySellStock(stock, results, TradeDirection.SELL, stock.Quantity, false, services);
                        //Attempt to reverse stock
                        if (stock.IsReversed != null && !(bool)stock.IsReversed)
                            ReverseStock(stock, results);
                    }
                    else
                    {
                        //Update Price
                        helper.Log(_logger, LogLevel.Information, $"stock {stock.Symbol} - ({stock.Name}) still within about {Math.Round(percentDiffPricePurchased, 4)}% tolerance since price purchased. Updating last Price.");
                        UpdateLastPrice(Convert.ToDecimal(results["lastPrice"].ToString()), ref stock);
                    }
                }
                else
                if (currentPrice < lastPrice)
                {
                    if (percentDiffLastPrice > settings.StopLossPercentLastPriceStock)
                    {
                        //sell
                        helper.Log(_logger, LogLevel.Information, $"stock {stock.Symbol} - ({stock.Name}) has fallen by more then about {Math.Round(percentDiffLastPrice, 4)}% since last price. Buying to cover.");
                        UpdateLastPrice(Convert.ToDecimal(results["lastPrice"].ToString()), ref stock);
                        BuySellStock(stock, results, TradeDirection.SELL, stock.Quantity, false, services);
                    }
                    else
                    {
                        //Update Price
                        //helper.Log(_logger, LogLevel.Information, $"stock {stock.Symbol} - ({stock.Name}) still within about {Math.Round(percentDiffLastPrice, 4)}% tolerance since last price. Updating last Price.");
                        //UpdateLastPrice(Convert.ToDecimal(results["lastPrice"].ToString()), ref stock);
                    }
                }
                else if (currentPrice >= lastPrice)
                {
                    //update price
                    helper.Log(_logger, LogLevel.Information, $"stock {stock.Symbol} - ({stock.Name}) still within about {Math.Round(percentDiffLastPrice, 4)}% tolerance since purchase. Updating last Price.");
                    UpdateLastPrice(Convert.ToDecimal(results["lastPrice"].ToString()), ref stock);
                }
            }
        }
        void CalculatePurchaseAmount(decimal? availiabletoTrade, ref List<BuySellCommodity> BuySellStockList)
        {
            var stockTracker = ctx?.StockTracker?.Where(x => x.PriceSold == null && x.DateSold == null && x.Status.Trim() == "Filled" && x.TradePlatform == (int)Platform.ETrade).ToList();
            if (stockTracker?.Count() >= (settings?.MaxNumberofStockstoBuy / 2))
                return;
            int stockCnt = 0;
            //decimal? perStockSet = (BuySellStockList.Count() < (settings.MaxNumberofStockstoBuy - BuySellStockList.Count())) ? availiabletoTrade / BuySellStockList.Count() : availiabletoTrade / (settings.MaxNumberofStockstoBuy - stockTracker.Count());//(availiabletoTrade / BuySellStockList.Count) > (availiabletoTrade * (settings.MaxPercentofAvailBalancePerOrder / 100)) ? (availiabletoTrade * (settings.MaxPercentofAvailBalancePerOrder / 100)) : (availiabletoTrade / BuySellStockList.Count);
            if ((int)(settings.MaxNumberofStockstoBuy / 2) - stockTracker.Count() < 1)
                return;
            decimal? perStockSet = availiabletoTrade / Math.Abs((int)(settings.MaxNumberofStockstoBuy / 2) - stockTracker.Count());
            var stockList = new List<BuySellCommodity>(BuySellStockList);
            foreach (var item in stockList)
            {
                if ((stockTracker.Count() + stockCnt) >= settings.MaxNumberofStockstoBuy)
                    break;
                //var result = item.Key;
                //var direction = item.Value;
                var result = item.Results;
                var direction = item.Direction;
                var lastPrice = Convert.ToDecimal(result?["lastPrice"]?.ToString());
                //var qty = Convert.ToInt32(perStockSet / lastPrice);
                var qty = Math.Abs((Convert.ToInt32(perStockSet / lastPrice) > settings.MaxNumberofSharesPerStock) ? (int)settings.MaxNumberofSharesPerStock : Convert.ToInt32(perStockSet / lastPrice));
                if (qty >= 2 && lastPrice >= 5m)
                {
                    BuySellStock(null, result, direction, qty, false, services);
                    stockCnt++;
                    BuySellStockList.Remove(item);
                }
            }
        }
        //void CheckOrderStatus()
        //{
        //    StockTracker? stockTracker = null;
        //    int qty = 0;
        //    decimal price = 0m;
        //    TradeDirection direction;
        //    dynamic quote = null;
        //    string sym = string.Empty;
        //    string desc = string.Empty;
        //    XElement results;

        //    try
        //    {
        //        results = services.Portfolio();
        //        helper.Log(_logger, LogLevel.Information, $"CheckOrderStatus - Portfolio");
        //    }
        //    catch (Exception ex)
        //    {
        //        helper.Log(_logger, LogLevel.Error, $"Error in CheckOrderStatus {ex.Message.ToString()} - {ex.StackTrace.ToString()}");
        //        return;
        //    }

        //    var positions = results?.Element("AccountPortfolio")?.Elements("Position").ToList();

        //    foreach (var position in positions)
        //    {
        //        sym = position?.Element("Product")?.Element("symbol")?.Value;
        //        quote = services2.GetQuote(sym, true, false);
        //        try
        //        {
        //            if (!helper.CheckMarketIndexes(quote))
        //            {
        //                continue;
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            helper.Log(_logger, LogLevel.Information, $"Error for quote on {sym} - {ex.Message}");
        //            continue;
        //        }
        //        desc = quote["Name"].ToString();
        //        helper.Log(_logger, LogLevel.Information, $"Checking {sym} - ({desc}) for status Update");
        //        //symbolList.Add(item.GetProperty("instrument").GetProperty("symbol").ToString());
        //        stockTracker = ctx.StockTracker.Where(x => x.PriceSold == null && x.DateSold == null
        //            && x.TradePlatform == (int)Platform.ETrade && /*(x.Status != "Cancelled" && x.Status != "Filled")*/
        //            x.Status.Trim() == "Success"
        //            && (x.DateCreated >= DateTime.Now.AddDays(-1) && x.DateUpdated <= DateTime.Now.AddDays(-1))
        //            && x.Symbol == sym
        //            ).FirstOrDefault();
        //        if (stockTracker == null)
        //            stockTracker = ctx.StockTracker.Where(x => x.PriceSold == null && x.DateSold == null
        //            && x.TradePlatform == (int)Platform.ETrade && /*(x.Status != "Cancelled" && x.Status != "Filled")*/
        //            x.Status.Trim() == "FILLED"
        //            /*&& (x.DateCreated >= DateTime.Now.AddDays(-1) && x.DateUpdated <= DateTime.Now.AddDays(-1))*/
        //            && x.Symbol == sym
        //            ).FirstOrDefault();
        //        if (stockTracker == null)
        //        {
        //            stockTracker = new StockTracker();
        //            stockTracker.Symbol = sym;
        //            stockTracker.Name = desc;
        //            stockTracker.PurchasedRsi = Convert.ToDecimal(quote["RSI"].ToString());
        //            stockTracker.PurchasedEma9 = Convert.ToDecimal(quote["EMA9"].ToString());
        //            stockTracker.PurchasedEma12 = Convert.ToDecimal(quote["EMA12"].ToString());
        //            stockTracker.PurchasedEma26 = Convert.ToDecimal(quote["EMA26"].ToString());
        //            stockTracker.PurchasedMacd = Convert.ToDecimal(quote["MACD"].ToString());
        //            stockTracker.DatePurchased = DateTime.Now;
        //            stockTracker.DateCreated = DateTime.Now;
        //            stockTracker.Status = "FILLED";
        //            stockTracker.LastUpdatedBy = 1;
        //        }
        //        var sp = stockTracker;
        //        price = Convert.ToDecimal(position?.Element("pricePaid")?.Value);
        //        qty = (int)Convert.ToInt32(position?.Element("quantity").Value);
        //        direction = (qty > 0) ? TradeDirection.BUY : TradeDirection.SELL_SHORT;
        //        try
        //        {
        //            sp.LastPrice = Convert.ToDecimal(quote["lastPrice"].ToString());
        //            sp.Status = "FILLED";
        //            sp.TradeDirection = (int)direction;
        //            sp.TradePlatform = (int)Platform.ETrade;
        //            sp.PricePurchased = price;
        //            sp.Quantity = qty;
        //            sp.DateUpdated = DateTime.Now;
        //            ctx.StockTracker.Attach(sp);
        //            ctx.Entry(sp).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
        //            ctx.SaveChanges();
        //            ctx.Entry(sp).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
        //        }
        //        catch (Exception ex)
        //        {
        //            try
        //            {
        //                ctx.Entry(sp).State = Microsoft.EntityFrameworkCore.EntityState.Added;
        //                ctx.SaveChanges();
        //                ctx.Entry(sp).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
        //            }
        //            catch (Exception ex2)
        //            {
        //                helper.Log(_logger, LogLevel.Error, $"Error in saving {sym} - ({desc}) for status Update {ex.StackTrace} - {ex.Message}");
        //            }
        //        }
        //    }
        //}
        //public void GetOrphanETrades()
        //{
        //    XElement results;

        //    try
        //    {
        //        results = services.Portfolio();
        //        helper.Log(_logger, LogLevel.Information,$"GetOrphanETrades - Portfolio");
        //    }
        //    catch (Exception ex)
        //    {
        //        helper.Log(_logger, LogLevel.Error, $"Error in GetOrphanETrades {ex.Message.ToString()} - {ex.StackTrace.ToString()}");
        //        return;
        //    }
        //    var stockTrackerList = ctx?.StockTracker?.Where(x => x.PriceSold == null && x.DateSold == null && x.TradePlatform == (int)Platform.ETrade && x.Status == "Filled").ToList();
        //    helper.Log(_logger, LogLevel.Information, $"GetOrphanETrades - Loading Stock Track List");
        //    var positions = results?.Element("AccountPortfolio")?.Elements("Position").ToList();
        //    foreach (var position in positions)
        //    {
        //        if(long.TryParse(position?.Element("Product")?.Element("symbol")?.Value, out long sym))
        //            continue; //skip if no quantity
        //        var symbol = position?.Element("Product")?.Element("symbol")?.Value;
        //        helper.Log(_logger, LogLevel.Information, $"GetOrphanETrades - Checking {symbol} from Etrade Positions");
        //        var pricePaid = Convert.ToDecimal(position?.Element("pricePaid")?.Value);
        //        var lastPrice = Convert.ToDecimal(position?.Element("Quick")?.Element("lastTrade")?.Value);
        //        var qty = Convert.ToInt32(position?.Element("quantity").Value);
        //        if (pricePaid > 0 && lastPrice > 0)
        //        {
        //            helper.Log(_logger, LogLevel.Information, $"GetOrphanETrades - Checking {symbol} from stock tracker list");
        //            var stockTracker = stockTrackerList?.Where(x => x.PriceSold == null && x.DateSold == null && x.TradePlatform == (int)Platform.ETrade && x.Status != "Cancelled" && x.Symbol == symbol.ToUpper()).FirstOrDefault();
        //            if (stockTracker == null)
        //            {
        //                helper.Log(_logger, LogLevel.Information, $"GetOrphanETrades - Stock {symbol} not found from stock tracker list, adding.");
        //                try
        //                {
        //                    var quote = services2.GetQuote(symbol);
        //                    if (!helper.CheckMarketIndexes(quote))  
        //                    {
        //                        continue;
        //                    }
        //                    stockTracker = new StockTracker();
        //                    stockTracker.Symbol = quote["Symbol"].ToString();
        //                    stockTracker.Name = quote["Name"].ToString();
        //                    stockTracker.TradeDirection = (qty > 0) ? (int)TradeDirection.BUY : (int)TradeDirection.SELL_SHORT;//(int)direction;
        //                    stockTracker.TradePlatform = (int)Platform.ETrade;
        //                    stockTracker.PricePurchased = pricePaid;
        //                    stockTracker.LastPrice = lastPrice;
        //                    stockTracker.Quantity = Math.Abs(qty);
        //                    stockTracker.PurchasedRsi = Convert.ToDecimal(quote["RSI"].ToString());
        //                    //stockTracker.PurchasedSma200 = Convert.ToDecimal(quote["SMA30"].ToString());
        //                    //stockTracker.PurchasedEma200 = Convert.ToDecimal(quote["EMA30"].ToString());
        //                    //stockTracker.PurchasedVwap = Convert.ToDecimal(quote["VWAP"].ToString());
        //                    stockTracker.PurchasedEma9 = Convert.ToDecimal(quote["EMA9"].ToString());
        //                    stockTracker.PurchasedEma12 = Convert.ToDecimal(quote["EMA12"].ToString());
        //                    stockTracker.PurchasedEma26 = Convert.ToDecimal(quote["EMA26"].ToString());
        //                    stockTracker.PurchasedMacd = Convert.ToDecimal(quote["MACD"].ToString());
        //                    stockTracker.DatePurchased = DateTime.Now;
        //                    stockTracker.DateCreated = DateTime.Now;
        //                    stockTracker.LastUpdatedBy = 1;
        //                    stockTracker.Status = "Filled";
        //                    stockTracker.IsReversed = false;
        //                    ctx?.StockTracker?.Add(stockTracker);
        //                    ctx.Entry(stockTracker).State = Microsoft.EntityFrameworkCore.EntityState.Added;
        //                    ctx.SaveChanges();
        //                    ctx.Entry(stockTracker).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
        //                }
        //                catch (Exception ex)
        //                {
        //                    helper.Log(_logger, LogLevel.Error, $"Error in updating quote for Get  OrphanETrades {symbol} {ex.Message.ToString()} - {ex.StackTrace.ToString()}");
        //                }
        //            }
        //            else if(stockTracker != null)
        //            {
        //                helper.Log(_logger, LogLevel.Information, $"GetOrphanETrades - Stock {symbol} found from stock tracker list, updating.");
        //                //check proper qty 
        //                if ((Math.Abs(qty) != stockTracker.Quantity) || pricePaid != stockTracker.PricePurchased)
        //                {
        //                    stockTracker.Quantity = Math.Abs(qty);
        //                    stockTracker.PricePurchased = pricePaid;
        //                    stockTracker.Status = "FILLED";
        //                    ctx?.StockTracker?.Attach(stockTracker);
        //                    ctx.Entry(stockTracker).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
        //                    ctx.SaveChanges();
        //                    ctx.Entry(stockTracker).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
        //                }
        //            }
        //        }
        //    }
        //    helper.Log(_logger, LogLevel.Information, $"GetOrphanETrades - Checking sotck track list to positions");
        //    foreach (var stock in stockTrackerList)
        //    {
        //        helper.Log(_logger, LogLevel.Information, $"GetOrphanETrades - Checking {stock.Symbol} in stock tracking list to positions.");
        //        var position = positions.Where(x => x?.Element("Product")?.Element("symbol")?.Value == stock.Symbol.ToUpper()).FirstOrDefault();
        //        if(position == null)
        //        {
        //            helper.Log(_logger, LogLevel.Information, $"GetOrphanETrades - position {stock.Symbol} not found, adding.");
        //            var st = stock;
        //            st.PriceSold = .01m;
        //            st.Status = "FILLED";
        //            st.DateSold = DateTime.Now;
        //            ctx.StockTracker.Attach(st);
        //            ctx.StockTracker.Entry(st).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
        //            ctx.SaveChanges();
        //            ctx.StockTracker.Entry(st).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
        //        }
        //    }
        //}
        ~ExecStockPricePredictionETrade()
        {
            ctx?.Dispose();
            ctx = null;
            helper = null;
        }
    }
}
