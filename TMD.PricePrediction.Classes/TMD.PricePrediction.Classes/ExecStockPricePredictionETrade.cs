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
            helper.Log(_logger, LogLevel.Information, $"Get account info for Charles Etrade");
            Run(totalAvailiableforLongTrade, ref BuySellStockList);
        }

        private void Run(decimal totalAvailiableforLongTrade, ref List<BuySellCommodity>? BuySellStockList)
        {

            settings = ctx.RulesEngine.FirstOrDefault();
            helper.Log(_logger, LogLevel.Information, $"Get settings for ETrade");

            //Update Prices
            helper.Log(_logger, LogLevel.Information, $"Updating Latest Prices on Tracker");
            UpdatePrices();

            if (totalAvailiableforLongTrade > 100 && (bool)settings.EtradeAnalysisTradeEnabled)
            {
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
                        //if (stock.IsReversed != null && !(bool)stock.IsReversed)
                        //    ReverseStock(stock, results);
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
                else //if (currentPrice <= lastPrice)
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
                        //if (stock.IsReversed != null && !(bool)stock.IsReversed)
                        //    ReverseStock(stock, results);
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
                else //if (currentPrice >= lastPrice)
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
        ~ExecStockPricePredictionETrade()
        {
            ctx?.Dispose();
            ctx = null;
            helper = null;
        }
    }
}
