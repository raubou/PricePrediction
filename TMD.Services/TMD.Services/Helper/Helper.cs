using log4net.Repository.Hierarchy;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using TMD.Services.Service;
using TMDInvestments.Context;
using TMDInvestments.Models;
using TMDInvestments.Services;

namespace TMD.PricePrediction
{
    public sealed class Helper
    {
        SchwabSettings? _settings = null;
        ETradeSettings? _settings2 = null;
        RulesEngine? rulesEngine = null;
        TraderGoService _traderGoService = null;
        private readonly ILogger? _logger = null;
        private readonly IHttpClientFactory? _factory = null;
        SchwabServices? services = null;
        ETradeService? services2 = null;
        TmdContext? ctx = null;
        Polygon? _polygon = null;
        DateTime runTime;// = easternTime;
        public DateTime easternTime;
        public Helper()
        {
            SetTimeZone();
        }
        public Helper(ILogger logger, SchwabSettings settings, Polygon polygon, IHttpClientFactory factory)
        {
            _logger = logger;
            _settings = settings;
            _factory = factory;
            _polygon = polygon;
            ctx = new TmdContext();
            services = new SchwabServices(logger, settings, _polygon, factory);
            SetTimeZone();
        }
        public Helper(ILogger logger, SchwabSettings settings, IHttpClientFactory factory)
        {
            _logger = logger;
            _settings = settings;
            _factory = factory;
            ctx = new TmdContext();
            rulesEngine = ctx.RulesEngine.FirstOrDefault();
            services = new SchwabServices(_logger, _settings, _factory);
            SetTimeZone();
        }
        public Helper(ILogger logger, SchwabSettings settings, ETradeSettings settings2, IHttpClientFactory factory)
        {
            _logger = logger;
            _settings = settings;
            _settings2 = settings2;
            _factory = factory;
            ctx = new TmdContext();
            rulesEngine = ctx.RulesEngine.FirstOrDefault();
            services = new SchwabServices(_logger, _settings, _factory);
            services2 = new ETradeService(_logger, _settings2, _factory);
            SetTimeZone();
        }
        public Helper(ILogger logger, SchwabSettings settings, ETradeSettings settings2, yfapiSettings yfapiSettings, IHttpClientFactory factory)
        {
            _logger = logger;
            _settings = settings;
            _settings2 = settings2;
            _factory = factory;
            ctx = new TmdContext();
            rulesEngine = ctx.RulesEngine.FirstOrDefault();
            services = new SchwabServices(_logger, _settings, _factory);
            services2 = new ETradeService(_logger, _settings2, _factory);
            _traderGoService = new TraderGoService(yfapiSettings, _logger, _factory, false);
            SetTimeZone();
        }
        public List<BuySellCommodity> GetQuoteList()
        {
            int? shortCnt = 0;
            int? longCnt = 0;
            int? stockCnt = 0;
            int? vixsandpLongCnt = 0;
            int? vixsandpShortCnt = 0;
            //int? SandPLong = 0;
            //int? SandPShort = 0;
            //int? DOWLong = 0;
            //int? DOWShort = 0;
            //int? NASDAQLong = 0;
            //int? NASDAQShort = 0;

            StockTracker2 stockTrackerInd = null;
            var BuySellStockList = new List<BuySellCommodity>();
            

            //Get stock already in trade being tracked.
            var stockTracker = ctx.StockTracker2.Where(x => x.PriceSold == null && x.DateSold == null && x.Status == "Filled"/* && x.TradePlatform == (int)Platform.CharlesSchwab*/).ToList();

            if(stockTracker.Where(x => x.TradePlatform == (int)Platform.CharlesSchwab).Count() >= (rulesEngine.MaxNumberofStockstoBuy / 2) &&
                stockTracker.Where(x => x.TradePlatform == (int)Platform.ETrade).Count() >= (rulesEngine.MaxNumberofStockstoBuy / 2))
            {
                Log(_logger, LogLevel.Warning, $"Stock count for Schwab and Etrade exceed max setting of {rulesEngine.MaxNumberofStockstoBuy}");
                return BuySellStockList;
            };
            var stockTrackerList = stockTracker.Select(y => y.Symbol);
            Log(_logger, LogLevel.Information, $"Get existing stock tracker");

            //Get list of stocks to trade not in tracker
            var stockList = ctx.StockList.Where(x => !stockTrackerList.Contains(x.Symbol) && !x.ExcludeFromAutomation).OrderBy(y => Guid.NewGuid()).ToList();
            Log(_logger, LogLevel.Information, $"Get stock list to trade");

            //stock sold/brought to cover in the last 24 hrs
            DateTime? minDate = DateTime.MinValue;
            var StockHold24Hr = ctx.StockTracker2.ToList().Where(x => x.PriceSold != null && x.DateSold != null && Math.Abs((x.DateSold - easternTime).Value.TotalHours) < 24).ToList().Select(y => new { Symbol = y.Symbol, DateSold = y.DateSold, DateUpdated = y.DateUpdated }).ToList();
            //Include cancelled item for a 24h hold
            StockHold24Hr.AddRange(ctx.StockTracker2.ToList().Where(x => x.Status == "Cancelled" && Math.Abs((x.DateUpdated - easternTime).Value.TotalHours) < 24).ToList().Select(y => new { Symbol = y.Symbol, DateSold = y.DateSold ?? minDate, DateUpdated = y.DateUpdated }).ToList());

            ///Test blank out to test stock quote list count
            //if (stockTracker.Where(x => x.TradePlatform == (int)Platform.CharlesSchwab).Count() >= (settings.MaxNumberofStockstoBuy / 2) &&
            //    stockTracker.Where(x => x.TradePlatform == (int)Platform.ETrade).Count() >= (settings.MaxNumberofStockstoBuy / 2))
            //{
            //    Log(_logger, LogLevel.Warning, $"Stock count for Schwab and Etrade exceed max setting of {settings.MaxNumberofStockstoBuy}");
            //    return new List<BuySellCommodity>();
            //}

            //S&P
            //var sandp = services.GetQuote("$SPX", true, false);
            //var vix = services.GetQuote("$VIX", true, false);

            ////DOW JONES
            //var dow = services.GetQuote("$DJI", true, false);
            //var vxd = services.GetQuote("$VXD", true, false);

            ////NASDAQ
            //var nasdaq = services.GetQuote("$NDX", true, false);
            //var vxn = services.GetQuote("$VXN", true, false);

            //if (!CheckMarketIndexes(vxn) || !CheckMarketIndexes(vxd) || !CheckMarketIndexes(vix) || !CheckMarketIndexes(sandp) || !CheckMarketIndexes(dow) || !CheckMarketIndexes(nasdaq))
            //{
            //    Log(_logger, LogLevel.Error, "Failed to get VIX or S&P or Dow or Nasdaq quote.");
            //    return null;
            //}
            //Log(_logger, LogLevel.Information, $"S&P: {sandp["lastPrice"].ToString()}. Dow: {dow["lastPrice"].ToString()}. Nasdaq: {nasdaq["lastPrice"].ToString()}");

            //Disabling Calculate VIX and S&P points
            //CheckDowNasdaqandSandP(vix, sandp, dow, vxd, nasdaq, vxn, settings?.StockLongCount, settings?.StockShortCount, _logger, ref SandPLong, ref SandPShort, ref DOWLong, ref DOWShort, ref NASDAQLong, ref NASDAQShort);
            //Log(_logger, LogLevel.Information, $"DOW Long: {DOWLong}. S&P Long: {SandPLong}. NASDAQ Long: {NASDAQLong}");
            //Log(_logger, LogLevel.Information, $"DOW Short: {DOWShort}. S&P Short: {SandPShort}. NASDAQ Short: {NASDAQShort}");

            //if (SandPLong == 1)
            //{
            //    vixsandpLongCnt = 1;
            //}
            //if (SandPShort == 1)
            //{
            //    vixsandpShortCnt = 1;
            //}
            //if (DOWLong > 0 && SandPLong > 0 && NASDAQLong > 0)
            //{
            //    vixsandpLongCnt = 1;
            //}
            //if (DOWShort > 0 && SandPShort > 0 && NASDAQShort > 0)
            //{
            //    vixsandpShortCnt = 1;
            //}

            if(runTime.Hour == 9 && (runTime.Minute >= 00 && runTime.Minute <= 30))
            {
                Log(_logger, LogLevel.Information, $"Getting movers from Yahoo");
                var GainersandLosers = _traderGoService.GetGainersandLosers(true);
                foreach (var gainer in GainersandLosers.Gainers)
                {
                    shortCnt = 0;
                    longCnt = 0;
                    if (gainer == null || string.IsNullOrEmpty(gainer["Symbol"].ToString()))
                        continue;
                    try
                    {
                        stockTrackerInd = stockTracker.Where(x => x.Symbol == gainer["Symbol"].ToString() && x.PriceSold == null && x.DateSold == null && x.Status == "Filled").FirstOrDefault();
                    }
                    catch (Exception ex)
                    {
                        Log(_logger, LogLevel.Error, $"Error in retrieving quote {ex.Message.ToString()} - {ex.StackTrace.ToString()}");
                        continue;
                    }

                    if (stockTrackerInd == null)
                    {
                        _logger.LogInformation($"Checking {gainer["Symbol"].ToString()} - ({gainer["Symbol"].ToString()})");
                        if (Convert.ToDecimal(gainer["volume"].ToString()) < 40000)
                        {
                            Log(_logger, LogLevel.Information, $"Not buying/selling short {gainer["Symbol"].ToString()} - ({gainer["Symbol"].ToString()}). Volume fall below min of 40000");
                            continue;
                        }
                        if (Convert.ToDecimal(gainer["lastPrice"].ToString()) >= rulesEngine.MinStockPrice && Convert.ToDecimal(gainer["lastPrice"].ToString()) <= 50m)
                        {
                            CalculatePoints(gainer, /*true, false,*/ ref shortCnt, ref longCnt);
                        }
                        else
                        {
                            Log(_logger, LogLevel.Information, $"Not buying/selling short {gainer["Symbol"].ToString()} - ({gainer["Symbol"].ToString()}). Stock fall above or below min stock price or is excluded");
                            continue;
                        }

                        Log(_logger, LogLevel.Information, $"{gainer["Symbol"].ToString()} - ({gainer["Name"].ToString()}) Score. Long score: {longCnt}. Short score {shortCnt}");

                        if ((longCnt + vixsandpLongCnt) >= rulesEngine.StockLongCount)
                        {
                            Log(_logger, LogLevel.Information, $"Adding {gainer["Symbol"].ToString()} - ({gainer["Name"].ToString()}) to potential long list.");
                            if (!BuySellStockList.Any(x => x.Results["Symbol"].ToString() == gainer["Symbol"].ToString()))
                            {
                                BuySellStockList.Add(new BuySellCommodity((longCnt + vixsandpLongCnt), TradeDirection.BUY, gainer));
                                stockCnt++;
                            }
                        }
                        else
                        if ((shortCnt + vixsandpShortCnt) >= rulesEngine.StockShortCount && (bool)rulesEngine.ShortEnabled)
                        {
                            Log(_logger, LogLevel.Information, $"Adding {gainer["Symbol"].ToString()} - ({gainer["Name"].ToString()}) to potential short list.");
                            if (!BuySellStockList.Any(x => x.Results["Symbol"].ToString() == gainer["Symbol"].ToString()))
                            {
                                BuySellStockList.Add(new BuySellCommodity((shortCnt + vixsandpShortCnt), TradeDirection.SELL_SHORT, gainer));
                                stockCnt++;
                            }
                        }
                        else
                        {
                            Log(_logger, LogLevel.Information, $"Not buying/selling short {gainer["Symbol"].ToString()} - ({gainer["Name"].ToString()}). Long score: {longCnt}. Short score {shortCnt}");
                        }
                    }
                }

                foreach (var loser in GainersandLosers.Losers)
                {
                    shortCnt = 0;
                    longCnt = 0;
                    if (loser == null || string.IsNullOrEmpty(loser["Symbol"].ToString()))
                        continue;
                    try
                    {
                        stockTrackerInd = stockTracker.Where(x => x.Symbol == loser["Symbol"].ToString() && x.PriceSold == null && x.DateSold == null && x.Status == "Filled").FirstOrDefault();
                    }
                    catch (Exception ex)
                    {
                        Log(_logger, LogLevel.Error, $"Error in retrieving quote {ex.Message.ToString()} - {ex.StackTrace.ToString()}");
                        continue;
                    }

                    if (stockTrackerInd == null)
                    {
                        _logger.LogInformation($"Checking {loser["Symbol"].ToString()} - ({loser["Symbol"].ToString()})");
                        if (Convert.ToDecimal(loser["volume"].ToString()) < 40000)
                        {
                            Log(_logger, LogLevel.Information, $"Not buying/selling short {loser["Symbol"].ToString()} - ({loser["Symbol"].ToString()}). Volume fall below min of 40000");
                            continue;
                        }
                        if (Convert.ToDecimal(loser["lastPrice"].ToString()) >= rulesEngine.MinStockPrice && Convert.ToDecimal(loser["lastPrice"].ToString()) <= 50m)
                        {
                            CalculatePoints(loser, /*true, false,*/ ref shortCnt, ref longCnt);
                        }
                        else
                        {
                            Log(_logger, LogLevel.Information, $"Not buying/selling short {loser["Symbol"].ToString()} - ({loser["Symbol"].ToString()}). Stock fall above or below min stock price or is excluded");
                            continue;
                        }

                        Log(_logger, LogLevel.Information, $"{loser["Symbol"].ToString()} - ({loser["Name"].ToString()}) Score. Long score: {longCnt}. Short score {shortCnt}");

                        if ((longCnt + vixsandpLongCnt) >= rulesEngine.StockLongCount)
                        {
                            Log(_logger, LogLevel.Information, $"Adding {loser["Symbol"].ToString()} - ({loser["Name"].ToString()}) to potential long list.");
                            if (!BuySellStockList.Any(x => x.Results["Symbol"].ToString() == loser["Symbol"].ToString()))
                            {
                                BuySellStockList.Add(new BuySellCommodity((longCnt + vixsandpLongCnt), TradeDirection.BUY, loser));
                                stockCnt++;
                            }
                        }
                        else
                        if ((shortCnt + vixsandpShortCnt) >= rulesEngine.StockShortCount && (bool)rulesEngine.ShortEnabled)
                        {
                            Log(_logger, LogLevel.Information, $"Adding {loser["Symbol"].ToString()} - ({loser["Name"].ToString()}) to potential short list.");
                            if (!BuySellStockList.Any(x => x.Results["Symbol"].ToString() == loser["Symbol"].ToString()))
                            {
                                BuySellStockList.Add(new BuySellCommodity((shortCnt + vixsandpShortCnt), TradeDirection.SELL_SHORT, loser));
                                stockCnt++;
                            }
                        }
                        else
                        {
                            Log(_logger, LogLevel.Information, $"Not buying/selling short {loser["Symbol"].ToString()} - ({loser["Name"].ToString()}). Long score: {longCnt}. Short score {shortCnt}");
                        }
                    }
                }
            }


            Log(_logger, LogLevel.Information, $"Getting movers from Schwab");           
            var movers = services.GetMovers();

            foreach(var mover in movers)
            {
                shortCnt = 0;
                longCnt = 0;
                if(mover == null || string.IsNullOrEmpty(mover["Symbol"].ToString()))
                    continue;
                try
                {
                    stockTrackerInd = stockTracker.Where(x => x.Symbol == mover["Symbol"].ToString() && x.PriceSold == null && x.DateSold == null && x.Status == "Filled").FirstOrDefault();
                }
                catch (Exception ex)
                {
                    Log(_logger, LogLevel.Error, $"Error in retrieving quote {ex.Message.ToString()} - {ex.StackTrace.ToString()}");
                    continue;
                }

                if (stockTrackerInd == null)
                {
                    _logger.LogInformation($"Checking {mover["Symbol"].ToString()} - ({mover["Symbol"].ToString()})");
                    if (Convert.ToDecimal(mover["volume"].ToString()) < 40000)
                    {
                        Log(_logger, LogLevel.Information, $"Not buying/selling short {mover["Symbol"].ToString()} - ({mover["Symbol"].ToString()}). Volume fall below min of 40000");
                        continue;
                    }
                    if (Convert.ToDecimal(mover["lastPrice"].ToString()) >= rulesEngine.MinStockPrice && Convert.ToDecimal(mover["lastPrice"].ToString()) <= 50m)
                    {
                        CalculatePoints(mover, /*true, false,*/ ref shortCnt, ref longCnt);
                    }
                    else
                    {
                        Log(_logger, LogLevel.Information, $"Not buying/selling short {mover["Symbol"].ToString()} - ({mover["Symbol"].ToString()}). Stock fall above or below min stock price or is excluded");
                        continue;
                    }

                    Log(_logger, LogLevel.Information, $"{mover["Symbol"].ToString()} - ({mover["Name"].ToString()}) Score. Long score: {longCnt}. Short score {shortCnt}");

                    if ((longCnt + vixsandpLongCnt) >= rulesEngine.StockLongCount)
                    {
                        Log(_logger, LogLevel.Information, $"Adding {mover["Symbol"].ToString()} - ({mover["Name"].ToString()}) to potential long list.");
                        if(!BuySellStockList.Any(x => x.Results["Symbol"].ToString() == mover["Symbol"].ToString()))
                        {
                            BuySellStockList.Add(new BuySellCommodity((longCnt + vixsandpLongCnt), TradeDirection.BUY, mover));
                            stockCnt++;
                        }

                    }
                    else
                    if ((shortCnt + vixsandpShortCnt) >= rulesEngine.StockShortCount && (bool)rulesEngine.ShortEnabled)
                    {
                        Log(_logger, LogLevel.Information, $"Adding {mover["Symbol"].ToString()} - ({mover["Name"].ToString()}) to potential short list.");
                        if (!BuySellStockList.Any(x => x.Results["Symbol"].ToString() == mover["Symbol"].ToString())) 
                        {
                            BuySellStockList.Add(new BuySellCommodity((shortCnt + vixsandpShortCnt), TradeDirection.SELL_SHORT, mover));
                            stockCnt++;
                        }
                    }
                    else
                    {
                        Log(_logger, LogLevel.Information, $"Not buying/selling short {mover["Symbol"].ToString()} - ({mover["Name"].ToString()}). Long score: {longCnt}. Short score {shortCnt}");
                    }
                }
            }
            foreach (var stock in stockList)
                {
                    shortCnt = 0;
                    longCnt = 0;
                    if (stock?.QuoteRetry >= 5)
                        continue;

                    //Check it stock has been brought in the last 24 hrs
                    if (StockHold24Hr.Any(y => y.Symbol == stock.Symbol))
                    {
                        //if stock was cancelled, check updated date
                        try
                        {
                            Log(_logger, LogLevel.Warning, $"Stock {stock.Symbol} - ({stock.Name}) purchased {Math.Round(Math.Abs((StockHold24Hr.Where(y => y.Symbol == stock.Symbol).Select(z => z.DateSold).FirstOrDefault() - easternTime).Value.TotalHours), 2)} hours ago was purchased less than 24h ago, skipping");
                        }
                        catch (Exception ex)
                        {
                            Log(_logger, LogLevel.Warning, $"Stock {stock.Symbol} - ({stock.Name}) cancelled {Math.Round(Math.Abs((StockHold24Hr.Where(y => y.Symbol == stock.Symbol).Select(z => z.DateUpdated).FirstOrDefault() - easternTime).Value.TotalHours), 2)} hours ago was purchased less than 24h ago, skipping");
                        }
                        continue;
                    }
                    _logger.LogInformation($"Checking {stock.Symbol} - ({stock.Name})");

                    dynamic? results = null;
                    try
                    {
                        results = services.GetQuote(stock.Symbol, true, false);
                    }
                    catch (Exception ex)
                    {
                        Log(_logger, LogLevel.Error, $"Error in retrieving quote for {stock.Symbol} - ({stock.Name}) {ex.Message.ToString()} - {ex.StackTrace.ToString()}");
                        var st = stock;
                        st.QuoteRetry = (st.QuoteRetry == null) ? 1 : st.QuoteRetry + 1;
                        ctx.StockList.Attach(st);
                        ctx.Entry(st).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                        ctx.SaveChanges();
                        ctx.Entry(st).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                        continue;
                    }
                    try
                    {
                        if (results is JsonObject)
                        {
                            if (((JsonObject)results).TryGetPropertyValue("errors", out JsonNode errors))
                            {
                                //failed to get quote.
                                Log(_logger, LogLevel.Error, $"Failed to get quote for {stock.Symbol} - ({stock.Name}) {errors.ToString()}");
                                var st = stock;
                                st.QuoteRetry = (st.QuoteRetry == null) ? 1 : st.QuoteRetry + 1;
                                ctx.StockList.Attach(st);
                                ctx.Entry(st).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                                ctx.SaveChanges();
                                ctx.Entry(st).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                                continue;
                            }
                        }
                        else
                        {
                            if (((JsonElement)results).TryGetProperty("errors", out JsonElement errors))
                            {
                                //failed to get quote.
                                Log(_logger, LogLevel.Error, $"Failed to get quote for {stock.Symbol} - ({stock.Name}) {errors.ToString()}");
                                var st = stock;
                                st.QuoteRetry = (st.QuoteRetry == null) ? 1 : st.QuoteRetry + 1;
                                ctx.StockList.Attach(st);
                                ctx.Entry(st).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                                ctx.SaveChanges();
                                ctx.Entry(st).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                                continue;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log(_logger, LogLevel.Error, $"Error in retrieving quote for {stock.Symbol} - ({stock.Name}) {ex.Message.ToString()} - {ex.StackTrace.ToString()}");
                        continue;
                    }


                    Log(_logger, LogLevel.Information, $"Exchange for {stock.Symbol} - ({results?["Exchange"]?.ToString()})");

                    if (Convert.ToDecimal(results["volume"].ToString()) < 40000)
                    {
                        Log(_logger, LogLevel.Information, $"Not buying/selling short {results["Symbol"].ToString()} - ({results["Symbol"].ToString()}). Volume fall below min of 40000");
                        continue;
                    }
                    if (Convert.ToDecimal(results["lastPrice"].ToString()) >= rulesEngine.MinStockPrice /*&& !stock.ExcludeFromAutomation*/)
                    {
                        CalculatePoints(results, /*true, false,*/ ref shortCnt, ref longCnt);
                    }
                    else
                    {
                        Log(_logger, LogLevel.Information, $"Not buying/selling short {results["Symbol"].ToString()} - ({results["Symbol"].ToString()}). Stock fall below min stock price or is excluded");
                        continue;
                    }

                    Log(_logger, LogLevel.Information, $"{stock.Symbol} - ({stock.Name}) Score. Long score: {longCnt}. Short score {shortCnt}");

                    if ((longCnt + vixsandpLongCnt) >= rulesEngine.StockLongCount)
                    {
                        Log(_logger, LogLevel.Information, $"Adding {stock.Symbol} - ({stock.Name}) to potential long list.");
                    if (!BuySellStockList.Any(x => x.Results["Symbol"].ToString() == results["Symbol"].ToString()))
                        BuySellStockList.Add(new BuySellCommodity((longCnt + vixsandpLongCnt), TradeDirection.BUY, results));
                        stockCnt++;
                    }
                    else
                    if ((shortCnt + vixsandpShortCnt) >= rulesEngine.StockShortCount && (bool)rulesEngine.ShortEnabled)
                    {
                        Log(_logger, LogLevel.Information, $"Adding {stock.Symbol} - ({stock.Name}) to potential short list.");
                    if (!BuySellStockList.Any(x => x.Results["Symbol"].ToString() == results["Symbol"].ToString()))
                        BuySellStockList.Add(new BuySellCommodity((shortCnt + vixsandpShortCnt), TradeDirection.SELL_SHORT, results));
                        stockCnt++;
                    }
                    else
                    {
                        Log(_logger, LogLevel.Information, $"Not buying/selling short {stock.Symbol} - ({stock.Name}). Long score: {longCnt}. Short score {shortCnt}");
                    }
                }
            //BuySellStockList = BuySellStockList.Where(x => Convert.ToDecimal(x.Results?["RSI"]?.ToString()) <= 30 || Convert.ToDecimal(x.Results?["RSI"]?.ToString()) >= 70).ToList();
            return BuySellStockList;
        }
        public bool CheckMarketIndexes(dynamic index)
        {
            bool areValid = true;
            Type type = index.GetType();
            if (index is JsonObject)
            {
                if (((JsonObject)index).TryGetPropertyValue("errors", out JsonNode errors))
                {
                    //failed to get quote.
                    Log(_logger, LogLevel.Error, $"Failed to get quote for {type.Name} {errors.ToString()}");
                    areValid = false;
                }
            }
            if (index is JsonElement)
            {
                if (((JsonElement)index).TryGetProperty("errors", out JsonElement errors))
                {
                    //failed to get quote.
                    Log(_logger, LogLevel.Error, $"Failed to get quote for {type.Name} {errors.ToString()}");
                    areValid = false;
                }
            }
            return areValid;
        }
        public void CalculatePoints(JsonObject? results/**, bool use31016, bool use5138,*/, ref int? shortCnt, ref int? longCnt)
        {
            shortCnt = 0;
            longCnt = 0;

            //if (!useOneHourRSI)
            //{
            if (Convert.ToDecimal(results["RSI"].ToString()) > 0)
            {
                if (Convert.ToDecimal(results["RSI"].ToString()) >= 80)
                {
                    shortCnt = shortCnt + 1;
                }
                if (Convert.ToDecimal(results["RSI"].ToString()) <= 20)
                {
                    longCnt = longCnt + 1;
                }
            }

            //}
            //else
            //{
            //    if (Convert.ToDecimal(results["RSIOneHour"].ToString()) > 0) {
            //        if (Convert.ToDecimal(results["RSIOneHour"].ToString()) >= 70)
            //        {
            //            shortCnt++;
            //        }
            //        if (Convert.ToDecimal(results["RSIOneHour"].ToString()) <= 30)
            //        {
            //            longCnt++;
            //        }
            //    }

            //}
            //Check SMA
            //if (Convert.ToDecimal(results["SMA30"].ToString()) >= Convert.ToDecimal(results["SMA26"].ToString())) { shortCnt++; }
            //if (Convert.ToDecimal(results["SMA30"].ToString()) <= Convert.ToDecimal(results["SMA26"].ToString())) { longCnt++; }

            //if (Convert.ToDecimal(results["SMA9"].ToString()) >= Convert.ToDecimal(results["SMA5"].ToString())) { shortCnt++; }
            //if (Convert.ToDecimal(results["SMA9"].ToString()) <= Convert.ToDecimal(results["SMA5"].ToString())) { longCnt++; }

            //Check EMA
            //if (Convert.ToDecimal(results["EMA30"].ToString()) >= Convert.ToDecimal(results["EMA26"].ToString())) { shortCnt++; }
            //if (Convert.ToDecimal(results["EMA30"].ToString()) <= Convert.ToDecimal(results["EMA26"].ToString())) { longCnt++; }

            //if (Convert.ToDecimal(results["EMA9"].ToString()) >= Convert.ToDecimal(results["EMA5"].ToString())) { shortCnt++; }
            //if (Convert.ToDecimal(results["EMA9"].ToString()) <= Convert.ToDecimal(results["EMA5"].ToString())) { longCnt++; }

            //Check WMA
            //if (Convert.ToDecimal(results["WMA30"].ToString()) >= Convert.ToDecimal(results["WMA26"].ToString())) { shortCnt++; }
            //if (Convert.ToDecimal(results["WMA30"].ToString()) <= Convert.ToDecimal(results["WMA26"].ToString())) { longCnt++; }

            //if (use31016)
            //{
            //// 3, 10, 16 STRATEGY
            //    if (Convert.ToDecimal(results["EMA3"].ToString()) > Convert.ToDecimal(results["EMA10"].ToString()))
            //    { longCnt = longCnt + 3; }
            //    if (Convert.ToDecimal(results["EMA3"].ToString()) > Convert.ToDecimal(results["EMA16"].ToString()) &&
            //        Convert.ToDecimal(results["EMA10"].ToString()) > Convert.ToDecimal(results["EMA16"].ToString()))
            //    { longCnt = longCnt + 3; }
            //    if (Convert.ToDecimal(results["SMA3"].ToString()) > Convert.ToDecimal(results["SMA10"].ToString()))
            //    { longCnt = longCnt + 3; }
            //    if (Convert.ToDecimal(results["SMA3"].ToString()) > Convert.ToDecimal(results["SMA16"].ToString()) &&
            //        Convert.ToDecimal(results["SMA10"].ToString()) > Convert.ToDecimal(results["SMA16"].ToString()))
            //    { longCnt = longCnt + 3; }

            //    if (Convert.ToDecimal(results["EMA3"].ToString()) < Convert.ToDecimal(results["EMA10"].ToString()))
            //    { shortCnt = shortCnt + 3; }
            //    if (Convert.ToDecimal(results["EMA3"].ToString()) < Convert.ToDecimal(results["EMA16"].ToString()) &&
            //        Convert.ToDecimal(results["EMA10"].ToString()) < Convert.ToDecimal(results["EMA16"].ToString()))
            //    { shortCnt = shortCnt + 3; }
            //    if (Convert.ToDecimal(results["SMA3"].ToString()) < Convert.ToDecimal(results["SMA10"].ToString()))
            //    { shortCnt = shortCnt + 3; }
            //    if (Convert.ToDecimal(results["SMA3"].ToString()) < Convert.ToDecimal(results["SMA16"].ToString()) &&
            //        Convert.ToDecimal(results["SMA10"].ToString()) < Convert.ToDecimal(results["SMA16"].ToString()))
            //    { shortCnt = shortCnt + 3; }
            ////}
            ////else if (use5138)
            ////{
            //    //// 5, 13, 13 STRATEGY
            //    if (Convert.ToDecimal(results["EMA5"].ToString()) > Convert.ToDecimal(results["EMA8"].ToString()))
            //    { longCnt = longCnt + 3; }
            //    if (Convert.ToDecimal(results["EMA5"].ToString()) > Convert.ToDecimal(results["EMA13"].ToString()) &&
            //        Convert.ToDecimal(results["EMA8"].ToString()) > Convert.ToDecimal(results["EMA13"].ToString()))
            //    { longCnt = longCnt + 3; }
            //    if (Convert.ToDecimal(results["EMA5"].ToString()) < Convert.ToDecimal(results["EMA8"].ToString()))
            //    { shortCnt = shortCnt + 3; }
            //    if (Convert.ToDecimal(results["EMA5"].ToString()) < Convert.ToDecimal(results["EMA13"].ToString()) &&
            //        Convert.ToDecimal(results["EMA8"].ToString()) < Convert.ToDecimal(results["EMA13"].ToString()))
            //    { shortCnt = shortCnt + 3; }
            //    if (Convert.ToDecimal(results["SMA5"].ToString()) > Convert.ToDecimal(results["SMA8"].ToString()))
            //    { longCnt = longCnt + 3; }
            //    if (Convert.ToDecimal(results["SMA5"].ToString()) > Convert.ToDecimal(results["SMA13"].ToString())  &&
            //        Convert.ToDecimal(results["SMA8"].ToString()) > Convert.ToDecimal(results["SMA13"].ToString()))
            //    { longCnt = longCnt + 3; }
            //    if (Convert.ToDecimal(results["SMA5"].ToString()) < Convert.ToDecimal(results["SMA8"].ToString()))
            //    { shortCnt = shortCnt + 3; }
            //    if (Convert.ToDecimal(results["SMA5"].ToString()) < Convert.ToDecimal(results["SMA13"].ToString()) &&
            //        Convert.ToDecimal(results["SMA8"].ToString()) < Convert.ToDecimal(results["SMA13"].ToString()))
            //    { shortCnt = shortCnt + 3; }
            ////}
            ////else
            ////{
            //    //// 9, 12, 26 STRATEGY
            if (Convert.ToDecimal(results["EMA9"].ToString()) > Convert.ToDecimal(results["EMA12"].ToString()))
            { longCnt = longCnt + 1; }
            if (Convert.ToDecimal(results["EMA12"].ToString()) > Convert.ToDecimal(results["EMA26"].ToString()))
            { longCnt = longCnt + 1; }
            if (Convert.ToDecimal(results["EMA9"].ToString()) < Convert.ToDecimal(results["EMA12"].ToString()))
            { shortCnt = shortCnt + 1; }
            if (Convert.ToDecimal(results["EMA12"].ToString()) < Convert.ToDecimal(results["EMA26"].ToString()))
            { shortCnt = shortCnt + 1; }
            if (Convert.ToDecimal(results["SMA9"].ToString()) > Convert.ToDecimal(results["SMA12"].ToString()))
            { longCnt = longCnt + 1; }
            if (Convert.ToDecimal(results["SMA12"].ToString()) > Convert.ToDecimal(results["SMA26"].ToString()))
            { longCnt = longCnt + 1; }
            if (Convert.ToDecimal(results["SMA9"].ToString()) < Convert.ToDecimal(results["SMA12"].ToString()))
            { shortCnt = shortCnt + 1; }
            if (Convert.ToDecimal(results["SMA12"].ToString()) < Convert.ToDecimal(results["SMA26"].ToString()))
            { shortCnt = shortCnt + 1; }
            //}



            //if (Convert.ToDecimal(results["WMA9"].ToString()) >= Convert.ToDecimal(results["WMA5"].ToString())) { shortCnt++; }
            //if (Convert.ToDecimal(results["WMA9"].ToString()) <= Convert.ToDecimal(results["WMA5"].ToString())) { longCnt++; }

            //if (Convert.ToDecimal(results["WMA3"].ToString()) >= Convert.ToDecimal(results["WMA10"].ToString())) { shortCnt++; }
            //if (Convert.ToDecimal(results["WMA3"].ToString()) <= Convert.ToDecimal(results["WMA10"].ToString())) { longCnt++; }
            //if (Convert.ToDecimal(results["WMA10"].ToString()) >= Convert.ToDecimal(results["WMA16"].ToString())) { shortCnt++; }
            //if (Convert.ToDecimal(results["WMA10"].ToString()) <= Convert.ToDecimal(results["WMA16"].ToString())) { longCnt++; }



            //////Check VMAP
            //if (useVWAP)
            //{
            //    if (Convert.ToDecimal(results["VWAP"].ToString()) <= Convert.ToDecimal(results["lastPrice"].ToString())) { shortCnt++; }
            //    if (Convert.ToDecimal(results["VWAP"].ToString()) >= Convert.ToDecimal(results["lastPrice"].ToString())) { longCnt++; }
            //}

            //Check MACD 9 Signal
            //if (MACDFocus) {
            //    if (Convert.ToDecimal(results["MACD12"].ToString()) >= Convert.ToDecimal(results["MACD9"].ToString())) { shortCnt = shortCnt + 2; }
            //    if (Convert.ToDecimal(results["MACD12"].ToString()) <= Convert.ToDecimal(results["MACD9"].ToString())) { longCnt = longCnt + 2; }
            //}
            //else
            //{
            //    if (Convert.ToDecimal(results["MACD12"].ToString()) >= Convert.ToDecimal(results["MACD9"].ToString())) { longCnt++; }
            //    if (Convert.ToDecimal(results["MACD12"].ToString()) <= Convert.ToDecimal(results["MACD9"].ToString())) { shortCnt++; }
            //}
            ////{
            //if (!MACDFocus)
            //{
            try
            {
                if (Convert.ToDecimal(results["MACD"].ToString()) > Convert.ToDecimal(results["MACDSignal"].ToString()) && (/*Convert.ToDecimal(results["MACDSignal"].ToString()) < 0 &&*/ Convert.ToDecimal(results["MACDHistogram"].ToString()) > 0))
                {
                    longCnt = longCnt + 1;
                }
                if (Convert.ToDecimal(results["MACD"].ToString()) < Convert.ToDecimal(results["MACDSignal"].ToString()) && (/*Convert.ToDecimal(results["MACDSignal"].ToString()) > 0 &&*/ Convert.ToDecimal(results["MACDHistogram"].ToString()) < 0))
                {
                    shortCnt = shortCnt + 1;
                }
            }
            catch (Exception ex)
            {
                Log(_logger, LogLevel.Error, $"Source: {ex.Source} stack trace: {ex.StackTrace} inner exception {ex?.InnerException?.Message ?? ""} error: {ex?.Message}");
            }

            //if (Convert.ToDecimal(results["MACD2"].ToString()) > Convert.ToDecimal(results["MACDSignal2"].ToString()) && (/*Convert.ToDecimal(results["MACDSignal"].ToString()) < 0 &&*/ Convert.ToDecimal(results["MACDHistogram2"].ToString()) > 0))
            //{
            //    longCnt = longCnt + 3;
            //}
            //if (Convert.ToDecimal(results["MACD2"].ToString()) < Convert.ToDecimal(results["MACDSignal2"].ToString()) && (/*Convert.ToDecimal(results["MACDSignal"].ToString()) > 0 &&*/ Convert.ToDecimal(results["MACDHistogram2"].ToString()) < 0))
            //{
            //    shortCnt = shortCnt + 3;
            //}
            //if (Convert.ToDecimal(results["MACD3"].ToString()) > Convert.ToDecimal(results["MACDSignal3"].ToString()) && (/*Convert.ToDecimal(results["MACDSignal"].ToString()) < 0 &&*/ Convert.ToDecimal(results["MACDHistogram3"].ToString()) > 0))
            //{
            //    longCnt = longCnt + 3;
            //}
            //if (Convert.ToDecimal(results["MACD3"].ToString()) < Convert.ToDecimal(results["MACDSignal3"].ToString()) && (/*Convert.ToDecimal(results["MACDSignal"].ToString()) > 0 &&*/ Convert.ToDecimal(results["MACDHistogram3"].ToString()) < 0))
            //{
            //    shortCnt = shortCnt + 3;
            //}
            //}
            //else
            //{
            //    if (Convert.ToDecimal(results["MACD2"].ToString()) >= Convert.ToDecimal(results["MACDSignal2"].ToString()) && (Convert.ToDecimal(results["MACDSignal2"].ToString()) < 0 && Convert.ToDecimal(results["MACD2"].ToString()) > 0))
            //    {
            //        longCnt = longCnt + 8;
            //    }
            //    if (Convert.ToDecimal(results["MACD2"].ToString()) <= Convert.ToDecimal(results["MACDSignal2"].ToString()) && (Convert.ToDecimal(results["MACDSignal2"].ToString()) > 0 && Convert.ToDecimal(results["MACD2"].ToString()) < 0))
            //    {
            //        shortCnt = shortCnt + 8;
            //    }
            //}


            //if (Convert.ToDecimal(results["MACD3"].ToString()) >= Convert.ToDecimal(results["MACDSignal3"].ToString()) && (Convert.ToDecimal(results["MACDSignal3"].ToString()) < 0 && Convert.ToDecimal(results["MACD3"].ToString()) > 0))
            //{
            //    longCnt = longCnt + 1;
            //}
            //if (Convert.ToDecimal(results["MACD3"].ToString()) <= Convert.ToDecimal(results["MACDSignal3"].ToString()) && (Convert.ToDecimal(results["MACDSignal3"].ToString()) > 0 && Convert.ToDecimal(results["MACD3"].ToString()) < 0))
            //{
            //    shortCnt = shortCnt + 1;
            //}


            if (Convert.ToDecimal(results["Upper Bound Price Diffrence"].ToString()) > Convert.ToDecimal(results["lastPrice"].ToString())) { longCnt++; }
            if (Convert.ToDecimal(results["Upper Bound Price Diffrence"].ToString()) < Convert.ToDecimal(results["lastPrice"].ToString())) { shortCnt++; }
            if (Convert.ToDecimal(results["Lower Bound Price Diffrence"].ToString()) > Convert.ToDecimal(results["lastPrice"].ToString())) { longCnt++; }
            if (Convert.ToDecimal(results["Lower Bound Price Diffrence"].ToString()) < Convert.ToDecimal(results["lastPrice"].ToString())) { shortCnt++; }
            if (Convert.ToDecimal(results["Forecast Bound Price Diffrence"].ToString()) > Convert.ToDecimal(results["lastPrice"].ToString())) { longCnt++; }
            if (Convert.ToDecimal(results["Forecast Bound Price Diffrence"].ToString()) < Convert.ToDecimal(results["lastPrice"].ToString())) { shortCnt++; }
            System.Threading.Thread.Sleep(150); //Sleep for 150 milliseconds to avoid throttling
        }

        //public void CheckDowNasdaqandSandP(JsonObject vix, JsonObject sandp, JsonObject dow, JsonObject vxd, JsonObject nasdaq, JsonObject vxn, int? longMax, int? shortMax, ILogger _logger, ref int? SandPLong, ref int? SandPShort, ref int? DOWLong, ref int? DOWShort, ref int? NASDAQLong, ref int? NASDAQShort)
        //{
        //    int? shortCnt = 0;
        //    int? longCnt = 0;
        //    //int? longTotal = 0;
        //    //int? shortTotal = 0;

        //    //_logger.LogInformation($"Checking VIX and S&P. VIX at {vix["lastPrice"].ToString()}. S&P at {sandp["lastPrice"].ToString()}.");

        //    CalculatePoints(vix, false, false, ref shortCnt, ref longCnt);

        //    _logger.LogInformation($"VIX Score. Long score: {longCnt}. Short score {shortCnt}");

        //    //Opposite for VIX
        //    if (longCnt >= (longMax)) { SandPShort++; }
        //    if (shortCnt >= (shortMax)) { SandPLong++; }
        //    shortCnt = 0;
        //    longCnt = 0;

        //    CalculatePoints(sandp, false, false, ref shortCnt, ref longCnt);

        //    _logger.LogInformation($"S&P Score. Long score: {longCnt}. Short score {shortCnt}");

        //    if (longCnt >= (longMax)) { SandPLong++; }
        //    if (shortCnt >= (shortMax)) { SandPShort++; }
        //    shortCnt = 0;
        //    longCnt = 0;

        //    CalculatePoints(vxd, false, false, ref shortCnt, ref longCnt);

        //    _logger.LogInformation($"VXD Score. Long score: {longCnt}. Short score {shortCnt}");

        //    //Opposite for VXD
        //    if (longCnt >= (longMax)) { DOWShort++; }
        //    if (shortCnt >= (shortMax)) { DOWLong++; }
        //    shortCnt = 0;
        //    longCnt = 0;

        //    CalculatePoints(dow, false, false, ref shortCnt, ref longCnt);

        //    _logger.LogInformation($"DOW Score. Long score: {longCnt}. Short score {shortCnt}");

        //    if (longCnt >= (longMax)) { DOWLong++; }
        //    if (shortCnt >= (shortMax)) { DOWShort++; }
        //    shortCnt = 0;
        //    longCnt = 0;

        //    CalculatePoints(vxn, false, false, ref shortCnt, ref longCnt);

        //    _logger.LogInformation($"VXN Score. Long score: {longCnt}. Short score {shortCnt}");

        //    //Opposite for VXN
        //    if (longCnt >= (longMax)) { NASDAQShort++; }
        //    if (shortCnt >= (shortMax)) { NASDAQLong++; }
        //    shortCnt = 0;
        //    longCnt = 0;

        //    CalculatePoints(nasdaq, false, false, ref shortCnt, ref longCnt);

        //    _logger.LogInformation($"NASDAQ Score. Long score: {longCnt}. Short score {shortCnt}");

        //    if (longCnt >= (longMax)) { NASDAQLong++; }
        //    if (shortCnt >= (shortMax)) { NASDAQShort++; }

        //    //vixsandpLongCnt = (longTotal > 0) ? 1 : 0;
        //    //vixsandpShortCnt = (shortTotal > 0) ? 1 : 0;

        //    //_logger.LogInformation($"Score. Long total {vixsandpLongCnt}. Short total {vixsandpShortCnt}.");
        //}
        public bool IsNotHolidayorWeekend()
        {
            bool run = true;
            //DateTime runTime = easternTime;
            TimeSpan startTime = new TimeSpan(9, 25, 0);
            TimeSpan endTime = new TimeSpan(15, 55, 0);

            //New Year Day
            if (runTime.Month == 1 && runTime.Day == 1)
                run = false;

            //Martin Luthor King Day 3rd Monday of January.
            if (runTime.Month == 1 && runTime.DayOfWeek == DayOfWeek.Monday && runTime.Day > 14 && runTime.Day <= 21)
                run = false;

            //Presidents Day 3rd Monday of February.
            if (runTime.Month == 2 && runTime.DayOfWeek == DayOfWeek.Monday && runTime.Day > 14 && runTime.Day <= 21)
                run = false;

            //Good Friday Easter Sunday
            DateTime goodFriday = new DateTime();
            DateTime easterSunday = new DateTime();
            EasterSunday(runTime.Year, ref goodFriday, ref easterSunday);
            if (runTime.Month == easterSunday.Month && (runTime.Day == easterSunday.Day && runTime.Day <= (easterSunday.Day - 2)))
                run = false;
            if (runTime.Month == goodFriday.Month && (runTime.Day == goodFriday.Day && runTime.Day <= (goodFriday.Day - 2)))
                run = false;

            //Memorial Day
            if (runTime.Month == 5 && runTime.DayOfWeek == DayOfWeek.Monday && runTime.Day > 21 && runTime.Day <= 31)
                run = false;

            //Juneteenth National Independence Day
            if (runTime.Month == 6 && runTime.Day == 19)
                run = false;

            //Independence Day
            if (runTime.Month == 7 && runTime.Day == 4)
                run = false;

            //Labor Day
            if (runTime.Month == 9 && runTime.DayOfWeek == DayOfWeek.Monday && runTime.Day > 1 && runTime.Day <= 7)
                run = false;

            //Thanksgiving Day
            if ((runTime.Month == 11 && runTime.DayOfWeek == DayOfWeek.Thursday && runTime.Day > 24 && runTime.Day <= 30) ||
                (runTime.Month == 11 && runTime.DayOfWeek == DayOfWeek.Friday && runTime.Day > 24 && runTime.Day <= 30 && runTime.Hour >= 1))
                run = false;

            //Christmas
            if (runTime.Month == 12 && runTime.Day == 25)
                run = false;

            //Christmas eve after 1pm
            if (runTime.Month == 12 && runTime.Day == 24 && runTime.TimeOfDay >= new TimeSpan(13, 00, 0))
                run = false;

            //After Hours, Saturday and Sunday
            if ((runTime.DayOfWeek == DayOfWeek.Saturday || runTime.DayOfWeek == DayOfWeek.Sunday) || (runTime.TimeOfDay >= endTime || runTime.TimeOfDay <= startTime))
                run = false;

            return run;
        }
        public void EasterSunday(int year, ref DateTime goodFriday, ref DateTime easterSunday)
        {
            int day = 0;
            int month = 0;

            int g = year % 19;
            int c = year / 100;
            int h = (c - (int)(c / 4) - (int)((8 * c + 13) / 25) + 19 * g + 15) % 30;
            int i = h - (int)(h / 28) * (1 - (int)(h / 28) * (int)(29 / (h + 1)) * (int)((21 - g) / 11));

            day = i - ((year + (int)(year / 4) + i + 2 - c + (int)(c / 4)) % 7) + 28;
            month = 3;

            if (day > 31)
            {
                month++;
                day -= 31;
            }

            DateTime ee = new DateTime(year, month, day);
            DateTime gf = ee.AddDays(-2);
            goodFriday = gf;
            easterSunday = ee;
        }
        public int CalculateBusinessDays(DateTime startDate, DateTime endDate)
        {
            var weekdays = 0;
            for (var currentDate = startDate; currentDate < endDate; currentDate = currentDate.AddDays(1))
            {
                if (IsWorkDay(currentDate))
                {
                    weekdays++;
                }
            }
            return weekdays;
        }
        private bool IsWorkDay(DateTime date)
        {
            return date.DayOfWeek is not DayOfWeek.Saturday
                and not DayOfWeek.Sunday;
        }
        public string CreateTradeObject(dynamic results, TradeDirection direction, int qty)
        {//to be done
            string order = "{" +
                "\"orderType\" : \"MARKET\"," +
                "\"session\" : \"NORMAL\"," +
                //"\"price\": \"72\"," +
                "\"duration\" : \"DAY\"," +
                "\"orderStrategyType\" : \"SINGLE\"," +
                "\"orderLegCollection\" : [{" +
                "\"instruction\" : \"" + Enum.GetName(typeof(TradeDirection), direction).ToUpper() + "\"," +
                    "\"quantity\" : " + qty + "," +
                    "\"instrument\" :{" +
                    "\"symbol\" : \"" + results["Symbol"].ToString() + "\"," +
                        "\"assetType\" : \"EQUITY\"" +
                    "}" +
                "}" +
                "]" +
            "}";
            return order;
        }

        public bool SaveStockTracker(StockTracker2 stockTracker, Platform tradePlatform, TradeDirection direction, int qty, bool IsReversed, dynamic results, string status, ILogger _logger)
        {
            bool isNew = false;
            if (stockTracker != null && (direction == TradeDirection.SELL || direction == TradeDirection.BUY_TO_COVER))
            {
                stockTracker.PriceSold = Convert.ToDecimal(results["lastPrice"].ToString());
                stockTracker.SoldRsi = Convert.ToDecimal(results["RSI"].ToString());
                stockTracker.SoldEma9 = Convert.ToDecimal(results["EMA9"].ToString());
                stockTracker.SoldEma12 = Convert.ToDecimal(results["EMA12"].ToString());
                stockTracker.SoldEma26 = Convert.ToDecimal(results["EMA26"].ToString());
                stockTracker.SoldEma3 = Convert.ToDecimal(results["EMA3"].ToString());
                stockTracker.SoldEma10 = Convert.ToDecimal(results["EMA10"].ToString());
                stockTracker.SoldEma16 = Convert.ToDecimal(results["EMA16"].ToString());
                stockTracker.SoldEma5 = Convert.ToDecimal(results["EMA5"].ToString());
                stockTracker.SoldEma8 = Convert.ToDecimal(results["EMA8"].ToString());
                stockTracker.SoldEma13 = Convert.ToDecimal(results["EMA13"].ToString());
                stockTracker.SoldSma9 = Convert.ToDecimal(results["SMA9"].ToString());
                stockTracker.SoldSma12 = Convert.ToDecimal(results["SMA12"].ToString());
                stockTracker.SoldSma26 = Convert.ToDecimal(results["SMA26"].ToString());
                stockTracker.SoldSma3 = Convert.ToDecimal(results["SMA3"].ToString());
                stockTracker.SoldSma10 = Convert.ToDecimal(results["SMA10"].ToString());
                stockTracker.SoldSma16 = Convert.ToDecimal(results["SMA16"].ToString());
                stockTracker.SoldSma5 = Convert.ToDecimal(results["SMA5"].ToString());
                stockTracker.SoldSma8 = Convert.ToDecimal(results["SMA8"].ToString());
                stockTracker.SoldSma13 = Convert.ToDecimal(results["SMA13"].ToString());
                stockTracker.SoldMacd = Decimal.Parse(results["MACD"].ToString(), NumberStyles.Float,
            CultureInfo.InvariantCulture);
                stockTracker.SoldMacdSignal = Decimal.Parse(results["MACDSignal"].ToString(), NumberStyles.Float,
            CultureInfo.InvariantCulture);
                stockTracker.SoldMacdHistogram = Decimal.Parse(results["MACDHistogram"].ToString(), NumberStyles.Float,
            CultureInfo.InvariantCulture);
                stockTracker.DateSold = easternTime;
                stockTracker.DateUpdated = easternTime;
                stockTracker.Status = "FILLED";// status;
                ctx.StockTracker2.Attach(stockTracker);
                ctx.Entry(stockTracker).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
            }
            else
            {
                if (stockTracker == null)
                {
                    stockTracker = new StockTracker2();
                    isNew = true;
                }
                //stockTracker = new StockTracker2();
                stockTracker.Symbol = results["Symbol"].ToString();
                stockTracker.Name = results["Name"].ToString();
                stockTracker.TradeDirection = (int)direction;
                stockTracker.TradePlatform = (int)tradePlatform;
                stockTracker.PricePurchased = Convert.ToDecimal(results["lastPrice"].ToString());
                stockTracker.IsReversed = IsReversed;
                stockTracker.Quantity = qty;
                stockTracker.PurchasedRsi = Convert.ToDecimal(results["RSI"].ToString());
                stockTracker.PurchasedEma9 = Convert.ToDecimal(results["EMA9"].ToString());
                stockTracker.PurchasedEma12 = Convert.ToDecimal(results["EMA12"].ToString());
                stockTracker.PurchasedEma26 = Convert.ToDecimal(results["EMA26"].ToString());
                stockTracker.PurchasedEma3 = Convert.ToDecimal(results["EMA3"].ToString());
                stockTracker.PurchasedEma10 = Convert.ToDecimal(results["EMA10"].ToString());
                stockTracker.PurchasedEma16 = Convert.ToDecimal(results["EMA16"].ToString());
                stockTracker.PurchasedEma5 = Convert.ToDecimal(results["EMA5"].ToString());
                stockTracker.PurchasedEma8 = Convert.ToDecimal(results["EMA8"].ToString());
                stockTracker.PurchasedEma13 = Convert.ToDecimal(results["EMA13"].ToString());
                stockTracker.PurchasedSma9 = Convert.ToDecimal(results["SMA9"].ToString());
                stockTracker.PurchasedSma12 = Convert.ToDecimal(results["SMA12"].ToString());
                stockTracker.PurchasedSma26 = Convert.ToDecimal(results["SMA26"].ToString());
                stockTracker.PurchasedSma3 = Convert.ToDecimal(results["SMA3"].ToString());
                stockTracker.PurchasedSma10 = Convert.ToDecimal(results["SMA10"].ToString());
                stockTracker.PurchasedSma16 = Convert.ToDecimal(results["SMA16"].ToString());
                stockTracker.PurchasedSma5 = Convert.ToDecimal(results["SMA5"].ToString());
                stockTracker.PurchasedSma8 = Convert.ToDecimal(results["SMA8"].ToString());
                stockTracker.PurchasedSma13 = Convert.ToDecimal(results["SMA13"].ToString());
                stockTracker.PurchasedMacd = Decimal.Parse(results["MACD"].ToString(),NumberStyles.Float,
            CultureInfo.InvariantCulture);
                stockTracker.PurchasedMacdSignal = Decimal.Parse(results["MACDSignal"].ToString(),NumberStyles.Float,
            CultureInfo.InvariantCulture);
                stockTracker.PurchasedMacdHistogram = Decimal.Parse(results["MACDHistogram"].ToString(), NumberStyles.Float,
            CultureInfo.InvariantCulture);
                stockTracker.LastPrice = Convert.ToDecimal(results["lastPrice"].ToString());
                stockTracker.DatePurchased = easternTime;
                stockTracker.DateCreated = easternTime;
                stockTracker.LastUpdatedBy = 1;
                stockTracker.Status = status;
                stockTracker.IsReversed = false;
                ctx.StockTracker2.Add(stockTracker);
                ctx.Entry(stockTracker).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                if (isNew)
                {
                    ctx.StockTracker2.Add(stockTracker);
                    ctx.Entry(stockTracker).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                }
                else
                {
                    ctx.StockTracker2.Attach(stockTracker);
                    ctx.Entry(stockTracker).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                }
            }
            try
            {
                ctx.SaveChanges();
                ctx.Entry(stockTracker).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
            }
            catch (Exception ex)
            {
                Log(_logger, LogLevel.Error, $"Source: {ex.Source} stack trace: {ex.StackTrace} inner exception {ex?.InnerException?.Message ?? ""} error: {ex.Message}");
            }
            return true;
        }
        public void CheckSchwabOrderStatus(ref decimal totalAvailiableforLongTrade)
        {
            Log(_logger, LogLevel.Information, $"Check Schwab Order Status");
            //decimal totalAvailiableforLongTrade = 0m;
            StockTracker2? stockTracker = null;
            int qty = 0;
            decimal price = 0m;
            TradeDirection direction;
            dynamic? quote = null;
            string sym = string.Empty;
            string desc = string.Empty;
            int authRetry = 0;
            //accountAuthRetry:
            //Get Account Info
            JsonElement account = new JsonElement();
            services.GetAccountInfo(ref account);
            Log(_logger, LogLevel.Information, $"Get account info for checking stock status and updating");

            JsonElement securitiesAccount = new JsonElement();
            JsonElement initialBalances = new JsonElement();
            try
            {
                securitiesAccount = account.GetProperty("securitiesAccount");
                initialBalances = securitiesAccount.GetProperty("initialBalances");
            }
            catch (Exception ex)
            {
                {
                    Log(_logger, LogLevel.Error, $"Authenication from schwab failed {authRetry} Retries. Exiting");
                    return;
                }

            }

            decimal cash = Convert.ToDecimal(initialBalances.GetProperty("availableFundsNonMarginableTrade").ToString());
            //decimal cash = Convert.ToDecimal(initialBalances.GetProperty("cashBalance").ToString());
            decimal margin = 0m;// Convert.ToDecimal(initialBalances.GetProperty("margin").ToString());
            totalAvailiableforLongTrade = Math.Round((cash + margin) * .9m, 2);
            //totalAvailiableforLongTrade = 0m;

            Log(_logger, LogLevel.Information, $"Cash Balance ${cash}. Margin Balance ${margin}. Total Availiable ${totalAvailiableforLongTrade}");

            foreach (var item in securitiesAccount.GetProperty("positions").EnumerateArray())
            {
                sym = item.GetProperty("instrument").GetProperty("symbol").ToString();
                //if (sym == "FDP" || sym == "MAN")
                //{
                //    //
                //}
                quote = services.GetQuote(sym, true, false);
                try
                {
                    if (!CheckMarketIndexes(quote))
                    {
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    Log(_logger, LogLevel.Information, $"Error for quote on {sym} - {ex.Message}");
                    continue;
                }
                try
                {
                    desc = quote?["Name"]?.ToString();
                }
                catch (Exception ex)
                {
                    Log(_logger, LogLevel.Information, $"Error for quote on {sym} - {ex.Message}");
                    continue;
                }

                Log(_logger, LogLevel.Information, $"Checking {sym} - ({desc}) for status Update");

                //check to see if more than one entry for the same stock, then delete duplicates
                stockTracker = null;
                var stockTrackerList = ctx.StockTracker2.Where(x => x.PriceSold == null && x.DateSold == null
                        && x.TradePlatform == (int)Platform.CharlesSchwab && /*(x.Status != "Cancelled" && x.Status != "Filled")*/
                        x.Status.Trim() == "FILLED"
                        /*&& (x.DateCreated >= easternTime.AddDays(-1) && x.DateCreated <= easternTime.AddDays(1))*/
                        && x.Symbol == sym
                        ).ToList();
                if (stockTrackerList.Count > 1)
                {
                    stockTracker = stockTrackerList.FirstOrDefault();
                    var removeList = stockTrackerList.Where(x => x.Id != stockTracker.Id).ToList();
                    foreach (var rem in removeList)
                    {
                        try
                        {
                            ctx.StockTracker2.Attach(rem);
                            ctx.Entry(rem).State = Microsoft.EntityFrameworkCore.EntityState.Deleted;
                            ctx.SaveChanges();
                            ctx.Entry(rem).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                        }
                        catch (Exception ex)
                        {
                            Log(_logger, LogLevel.Error, $"Error in removing duplicate {sym} - ({desc}) for status Update {ex.StackTrace} - {ex.Message}");
                        }
                    }
                }
                else
                {
                    stockTracker = stockTrackerList.FirstOrDefault();
                }

                if (stockTracker == null)
                {
                    stockTracker = ctx.StockTracker2.Where(x => x.PriceSold == null && x.DateSold == null
                    && x.TradePlatform == (int)Platform.CharlesSchwab && /*(x.Status != "Cancelled" && x.Status != "Filled")*/
                    x.Status.Trim() == "{\"$id\":\"1\",\"status\":\"Created\"}"
                    && (x.DateCreated >= easternTime.AddDays(-1) && x.DateCreated <= easternTime.AddDays(1))
                    && x.Symbol == sym
                    ).FirstOrDefault();
                }

                if (stockTracker == null)
                {
                    stockTracker = new StockTracker2();
                    stockTracker.Symbol = sym;
                    stockTracker.Name = desc;
                    stockTracker.PurchasedRsi = Convert.ToDecimal(quote?["RSI"].ToString());
                    stockTracker.PurchasedEma9 = Convert.ToDecimal(quote?["EMA9"].ToString());
                    stockTracker.PurchasedEma12 = Convert.ToDecimal(quote?["EMA12"].ToString());
                    stockTracker.PurchasedEma26 = Convert.ToDecimal(quote?["EMA26"].ToString());
                    stockTracker.PurchasedEma3 = Convert.ToDecimal(quote?["EMA3"].ToString());
                    stockTracker.PurchasedEma10 = Convert.ToDecimal(quote?["EMA10"].ToString());
                    stockTracker.PurchasedEma16 = Convert.ToDecimal(quote?["EMA16"].ToString());
                    stockTracker.PurchasedEma5 = Convert.ToDecimal(quote?["EMA5"].ToString());
                    stockTracker.PurchasedEma8 = Convert.ToDecimal(quote?["EMA8"].ToString());
                    stockTracker.PurchasedEma13 = Convert.ToDecimal(quote?["EMA13"].ToString());
                    stockTracker.PurchasedSma9 = Convert.ToDecimal(quote?["SMA9"].ToString());
                    stockTracker.PurchasedSma12 = Convert.ToDecimal(quote?["SMA12"].ToString());
                    stockTracker.PurchasedSma26 = Convert.ToDecimal(quote?["SMA26"].ToString());
                    stockTracker.PurchasedSma3 = Convert.ToDecimal(quote?["SMA3"].ToString());
                    stockTracker.PurchasedSma10 = Convert.ToDecimal(quote?["SMA10"].ToString());
                    stockTracker.PurchasedSma16 = Convert.ToDecimal(quote?["SMA16"].ToString());
                    stockTracker.PurchasedSma5 = Convert.ToDecimal(quote?["SMA5"].ToString());
                    stockTracker.PurchasedSma8 = Convert.ToDecimal(quote?["SMA8"].ToString());
                    stockTracker.PurchasedSma13 = Convert.ToDecimal(quote?["SMA13"].ToString());
                    stockTracker.PurchasedMacd = Decimal.Parse(quote?["MACD"].ToString(), NumberStyles.Float,
            CultureInfo.InvariantCulture);
                    stockTracker.PurchasedMacdSignal = Decimal.Parse(quote?["MACDSignal"].ToString(), NumberStyles.Float,
            CultureInfo.InvariantCulture);
                    stockTracker.PurchasedMacdHistogram = Decimal.Parse(quote?["MACDHistogram"].ToString(), NumberStyles.Float,
            CultureInfo.InvariantCulture);
                    stockTracker.DatePurchased = easternTime;
                    stockTracker.DateCreated = easternTime;
                    stockTracker.Status = "FILLED";
                    stockTracker.LastUpdatedBy = 1;
                    stockTracker.IsReversed = false;
                }
                var sp = stockTracker;
                price = Convert.ToDecimal(item.GetProperty("averagePrice").ToString());
                if (Convert.ToDecimal(item.GetProperty("shortQuantity").ToString()) > 0)
                {
                    qty = (int)Convert.ToDecimal(item.GetProperty("shortQuantity").ToString());
                    direction = TradeDirection.SELL_SHORT;
                }
                else
                {
                    qty = (int)Convert.ToDecimal(item.GetProperty("longQuantity").ToString());
                    direction = TradeDirection.BUY;
                }

                if (stockTracker == null)
                {
                    stockTracker = ctx.StockTracker2.Where(x => x.PriceSold == null && x.DateSold == null
                    && x.TradePlatform == (int)Platform.ETrade && /*(x.Status != "Cancelled" && x.Status != "Filled")*/
                    x.Status.Trim() == "Success"
                    && (x.DateCreated >= easternTime.AddDays(-1) && x.DateCreated <= easternTime.AddDays(1))
                    && x.Symbol == sym
                    ).FirstOrDefault();
                }
                try
                {
                    try
                    {
                        sp.LastPrice = Convert.ToDecimal(quote?["lastPrice"]?.ToString());
                    }
                    catch (Exception ex)
                    {
                        Log(_logger, LogLevel.Information, $"Error for quote on {sym} - {ex.Message}");
                        continue;
                    }
                    sp.Status = "FILLED";
                    sp.TradeDirection = (int)direction;
                    sp.TradePlatform = (int)Platform.CharlesSchwab;
                    sp.PricePurchased = price;
                    sp.Quantity = qty;
                    sp.DateUpdated = easternTime;
                    ctx.StockTracker2.Attach(sp);
                    ctx.Entry(sp).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                    ctx.SaveChanges();
                    ctx.Entry(sp).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                }
                catch (Exception ex)
                {
                    try
                    {
                        ctx.Entry(sp).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                        ctx.SaveChanges();
                        ctx.Entry(sp).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                    }
                    catch (Exception ex2)
                    {
                        Log(_logger, LogLevel.Error, $"Error in saving {sym} - ({desc}) for status Update {ex.StackTrace} - {ex.Message}");
                    }
                }
            }

            var stockTrkList = ctx.StockTracker2.Where(x => x.PriceSold == null && x.DateSold == null
                        && x.TradePlatform == (int)Platform.CharlesSchwab && 
                        x.Status.Trim() == "FILLED"
                        ).ToList();
            foreach (var stk in stockTrkList)
            {
                //if (stk.Symbol == "GNK")
                //{

                //}
                var pos = securitiesAccount.GetProperty("positions").EnumerateArray().Where(x => x.GetProperty("instrument").GetProperty("symbol").ToString() == stk.Symbol).FirstOrDefault();
                if (!(pos is JsonElement) || pos.ValueKind == JsonValueKind.Undefined)
                {
                    ctx.StockTracker2.Attach(stk);
                    ctx.Entry(stk).State = Microsoft.EntityFrameworkCore.EntityState.Deleted;
                    try
                    {
                        ctx.SaveChanges();
                        ctx.Entry(stk).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                    }
                    catch (Exception ex)
                    {
                        Log(_logger, LogLevel.Error, $"Error in updating sold status {stk.Symbol} - ({stk.Name}) for status Update {ex.StackTrace} - {ex.Message}");
                    }
                }
            }
        }
        public void CheckETradeOrderStatus(ref decimal totalAvailiableforLongTrade)
        {
            Log(_logger, LogLevel.Information, $"Check ETrade Order Status");
            StockTracker2? stockTracker = null;
            int qty = 0;
            decimal price = 0m;
            TradeDirection direction;
            dynamic? quote = null;
            string sym = string.Empty;
            string desc = string.Empty;
            XElement account;
            XElement securitiesAccount;
            List<XElement> positions = new List<XElement>();
            try
            {
                //account = services2.Portfolio();
                account = services2.Account();
                securitiesAccount = account.Element("Computed");
                Log(_logger, LogLevel.Information, $"CheckOrderStatus - Portfolio");
            }
            catch (Exception ex)
            {
                Log(_logger, LogLevel.Error, $"Error in CheckOrderStatus {ex.Message.ToString()} - {ex.StackTrace.ToString()}");
                return;
            }

            //var positions = securitiesAccount?.Element("AccountPortfolio")?.Elements("Position").ToList();
            //List<XElement> positions = services2?.Portfolio().Element("AccountPortfolio")?.Elements("Position").ToList();
            var portfolio = services2?.Portfolio();
            positions = portfolio?.Element("AccountPortfolio")?.Elements("Position").ToList();

            //decimal cash = Convert.ToDecimal(securitiesAccount.Element("cashBuyingPower").Value.ToString());
            decimal cash = Convert.ToDecimal(securitiesAccount.Element("cashAvailableForInvestment").Value.ToString());
            decimal margin = 0m;// Convert.ToDecimal(securitiesAccount.Element("marginBuyingPower").Value.ToString());
            //totalAvailiableforLongTrade = Math.Round((cash + margin), 2);
            totalAvailiableforLongTrade = Math.Round((cash + margin) * .9m, 2);
            //totalAvailiableforLongTrade = 200m;

            Log(_logger, LogLevel.Information, $"Cash Balance ${cash}. Margin Balance ${margin}. Total Availiable ${totalAvailiableforLongTrade}");

            foreach (var position in positions)
            {
                sym = position?.Element("Product")?.Element("symbol")?.Value;
                //if (sym == "LION")
                //{
                //    //int a = 1;
                //}
                //if (sym == "MSGE")
                //{
                //    //int a = 1;
                //}
                quote = services.GetQuote(sym, true, false);
                try
                {
                    if (!CheckMarketIndexes(quote))
                    {
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    Log(_logger, LogLevel.Information, $"Error for quote on {sym} - {ex.Message}");
                    continue;
                }
                try
                {
                    desc = quote?["Name"]?.ToString();
                    //continue;
                }
                catch (Exception ex)
                {
                    Log(_logger, LogLevel.Information, $"Error for quote on {sym} - {ex.Message}");
                    continue;
                }

                Log(_logger, LogLevel.Information, $"Checking {sym} - ({desc}) for status Update");

                //check to see if more than one entry for the same stock, then delete duplicates
                stockTracker = null;
                var stockTrackerList = ctx.StockTracker2.Where(x => x.PriceSold == null && x.DateSold == null
                        && x.TradePlatform == (int)Platform.ETrade && /*(x.Status != "Cancelled" && x.Status != "Filled")*/
                        x.Status.Trim() == "FILLED"
                        /*&& (x.DateCreated >= easternTime.AddDays(-1) && x.DateCreated <= easternTime.AddDays(1))*/
                        && x.Symbol == sym
                        ).ToList();
                if (stockTrackerList.Count > 1)
                {
                    stockTracker = stockTrackerList.FirstOrDefault();
                    var removeList = stockTrackerList.Where(x => x.Id != stockTracker.Id).ToList();
                    foreach (var rem in removeList)
                    {
                        try
                        {
                            ctx.StockTracker2.Attach(rem);
                            ctx.Entry(rem).State = Microsoft.EntityFrameworkCore.EntityState.Deleted;
                            ctx.SaveChanges();
                            ctx.Entry(rem).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                        }
                        catch (Exception ex)
                        {
                            Log(_logger, LogLevel.Error, $"Error in removing duplicate {sym} - ({desc}) for status Update {ex.StackTrace} - {ex.Message}");
                        }
                    }
                }
                else
                {
                    stockTracker = stockTrackerList.FirstOrDefault();
                }

                if (stockTracker == null)
                {
                    stockTracker = ctx.StockTracker2.Where(x => x.PriceSold == null && x.DateSold == null
                    && x.TradePlatform == (int)Platform.ETrade && /*(x.Status != "Cancelled" && x.Status != "Filled")*/
                    x.Status.Trim() == "Success"
                    && (x.DateCreated >= easternTime.AddDays(-1) && x.DateCreated <= easternTime.AddDays(1))
                    && x.Symbol == sym
                    ).FirstOrDefault();
                }

                if (stockTracker == null)
                {
                    try
                    {
                        stockTracker = new StockTracker2();
                        stockTracker.Symbol = sym;
                        stockTracker.Name = desc;
                        stockTracker.PurchasedRsi = Convert.ToDecimal(quote?["RSI"].ToString());
                        stockTracker.PurchasedEma9 = Convert.ToDecimal(quote?["EMA9"].ToString());
                        stockTracker.PurchasedEma12 = Convert.ToDecimal(quote?["EMA12"].ToString());
                        stockTracker.PurchasedEma26 = Convert.ToDecimal(quote?["EMA26"].ToString());
                        stockTracker.PurchasedEma3 = Convert.ToDecimal(quote?["EMA3"].ToString());
                        stockTracker.PurchasedEma10 = Convert.ToDecimal(quote?["EMA10"].ToString());
                        stockTracker.PurchasedEma16 = Convert.ToDecimal(quote?["EMA16"].ToString());
                        stockTracker.PurchasedEma5 = Convert.ToDecimal(quote?["EMA5"].ToString());
                        stockTracker.PurchasedEma8 = Convert.ToDecimal(quote?["EMA8"].ToString());
                        stockTracker.PurchasedEma13 = Convert.ToDecimal(quote?["EMA13"].ToString());
                        stockTracker.PurchasedSma9 = Convert.ToDecimal(quote?["SMA9"].ToString());
                        stockTracker.PurchasedSma12 = Convert.ToDecimal(quote?["SMA12"].ToString());
                        stockTracker.PurchasedSma26 = Convert.ToDecimal(quote?["SMA26"].ToString());
                        stockTracker.PurchasedSma3 = Convert.ToDecimal(quote?["SMA3"].ToString());
                        stockTracker.PurchasedSma10 = Convert.ToDecimal(quote?["SMA10"].ToString());
                        stockTracker.PurchasedSma16 = Convert.ToDecimal(quote?["SMA16"].ToString());
                        stockTracker.PurchasedSma5 = Convert.ToDecimal(quote?["SMA5"].ToString());
                        stockTracker.PurchasedSma8 = Convert.ToDecimal(quote?["SMA8"].ToString());
                        stockTracker.PurchasedSma13 = Convert.ToDecimal(quote?["SMA13"].ToString());
                        stockTracker.PurchasedMacd = Decimal.Parse(quote?["MACD"].ToString(), NumberStyles.Float,
            CultureInfo.InvariantCulture);
                        stockTracker.PurchasedMacdSignal = Decimal.Parse(quote?["MACDSignal"].ToString(), NumberStyles.Float,
            CultureInfo.InvariantCulture);
                        stockTracker.PurchasedMacdHistogram = Decimal.Parse(quote?["MACDHistogram"].ToString(), NumberStyles.Float,
            CultureInfo.InvariantCulture);
                        stockTracker.DatePurchased = easternTime;
                        stockTracker.DateCreated = easternTime;
                        stockTracker.Status = "FILLED";
                        stockTracker.LastUpdatedBy = 1;
                        stockTracker.IsReversed = false;
                    }
                    catch (Exception)
                    {
                        Log(_logger, LogLevel.Error, $"Cannot add stock info to stock tracker for {sym} for Etrade");
                        continue;
                    }

                }
                var sp = stockTracker;
                price = Convert.ToDecimal(position?.Element("pricePaid")?.Value);
                qty = (int)Convert.ToInt32(position?.Element("quantity").Value);
                direction = (qty > 0) ? TradeDirection.BUY : TradeDirection.SELL_SHORT;
                try
                {
                    try
                    {
                        sp.LastPrice = Convert.ToDecimal(quote?["lastPrice"]?.ToString());
                    }
                    catch (Exception ex)
                    {
                        Log(_logger, LogLevel.Information, $"Error for quote on {sym} - {ex.Message}");
                        continue;
                    }

                    sp.Status = "FILLED";
                    sp.TradeDirection = (int)direction;
                    sp.TradePlatform = (int)Platform.ETrade;
                    sp.PricePurchased = price;
                    sp.Quantity = qty;
                    sp.DateUpdated = easternTime;
                    ctx.StockTracker2.Attach(sp);
                    ctx.Entry(sp).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                    ctx.SaveChanges();
                    ctx.Entry(sp).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                }
                catch (Exception ex)
                {
                    try
                    {
                        ctx.Entry(sp).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                        ctx.SaveChanges();
                        ctx.Entry(sp).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                    }
                    catch (Exception ex2)
                    {
                        Log(_logger, LogLevel.Error, $"Error in saving {sym} - ({desc}) for status Update {ex.StackTrace} - {ex.Message}");
                    }
                }
            }
            var stockTrkList = ctx.StockTracker2.Where(x => x.PriceSold == null && x.DateSold == null
            && x.TradePlatform == (int)Platform.ETrade &&
            x.Status.Trim() == "FILLED"
            ).ToList();
            foreach (var stk in stockTrkList)
            {
                //if (stk.Symbol == "GNK")
                //{

                //}
                var pos = positions.Where(x => x.Element("Product")?.Element("symbol")?.Value.ToString() == stk.Symbol).FirstOrDefault();
                if (pos is null)
                {
                    ctx.StockTracker2.Attach(stk);
                    ctx.Entry(stk).State = Microsoft.EntityFrameworkCore.EntityState.Deleted;
                    try
                    {
                        ctx.SaveChanges();
                        ctx.Entry(stk).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                    }
                    catch (Exception ex)
                    {
                        Log(_logger, LogLevel.Error, $"Error in updating sold status {stk.Symbol} - ({stk.Name}) for status Update {ex.StackTrace} - {ex.Message}");
                    }
                }
            }
        }
        private void SetTimeZone()
        {
            // Get the TimeZoneInfo object for Eastern Standard Time
            // The identifier "Eastern Standard Time" is commonly used on Windows systems.
            // On Linux/macOS, you might need "America/New_York".
            TimeZoneInfo? easternTimeZone;
            try
            {
                easternTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                // Fallback for non-Windows systems or if the ID is different
                easternTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
            }
            // Get the current UTC time
            DateTime utcNow = DateTime.UtcNow;

            // Convert the UTC time to Eastern Time
            easternTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, easternTimeZone);
            runTime = easternTime;
        }
        public void UnloadAgingStocks()
        {
            var agingStock = ctx?.StockTracker2?.ToList()?.Where(x => x.PriceSold == null && x.DateSold == null && x.Status == "FILLED"
            && Math.Abs((x.DateCreated - easternTime).Value.TotalDays) > 5).ToList();
            foreach (var stock in agingStock)
            {
                var results = services.GetQuote(stock.Symbol);
                Platform tradePlatform = (Platform)stock.TradePlatform;
                TradeDirection currentTradeDirection = (TradeDirection)Enum.Parse(typeof(TradeDirection), stock.TradeDirection.ToString(), true);
                TradeDirection tradeDirection = (currentTradeDirection == TradeDirection.BUY) ? TradeDirection.SELL : TradeDirection.BUY_TO_COVER;
                switch (tradePlatform)
                {
                    case Platform.CharlesSchwab:
                        BuySellStock(stock, results, tradeDirection, stock.Quantity, true, services);
                        break;
                    case Platform.ETrade:
                        BuySellStock(stock, results, tradeDirection, stock.Quantity, true, services2);
                        break;
                }
            }
        }
        public void CalculatePurchaseAmount(decimal? availiabletoTrade, ref List<BuySellCommodity> BuySellStockList, RulesEngine settings, Platform platform)
        {
            //var stockTracker = ctx?.StockTracker?.Where(x => x.PriceSold == null && x.DateSold == null && x.Status.Trim() == "Filled" && x.TradePlatform == (int)Platform.ETrade).ToList();
            var stockTracker = ctx?.StockTracker2?.Where(x => x.PriceSold == null && x.DateSold == null && x.Status.Trim() == "Filled" && x.TradePlatform == (int)platform).ToList();
            var alreadyQueuedStock = ctx?.StockTracker2?.Where(x => x.Status == "QUEUED" && x.TradePlatform == (int)platform).ToList();
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
                    //BuySellStock(null, result, direction, qty, false, services);
                    if(!alreadyQueuedStock.Where(x => x.Symbol == result["Symbol"].ToString()).Any())
                        QueueStocktoBuy(result, platform, direction, qty);
                    stockCnt++;
                    BuySellStockList.Remove(item);
                }
            }
        }
        public bool BuySellStock(StockTracker2 stocktracker, dynamic order, TradeDirection direction, int qty, bool IsReversed, SchwabServices service)
        {
            //return false;
            string jsonOrder = CreateTradeObject(order, direction, qty);
            dynamic results = service.SetOrder(jsonOrder);
            //if direction is buy or sell short, stocktracker will be null.
            try
            {
                var options = new JsonSerializerOptions
                {
                    ReferenceHandler = ReferenceHandler.Preserve
                };
                string output = System.Text.Json.JsonSerializer.Serialize(results, options);
                if (output.Contains("message"))
                {
                    Log(_logger, LogLevel.Error, $"stock trade failed {order["Symbol"].ToString()} {output}");
                }
                else
                {
                    Log(_logger, LogLevel.Information, $"stock trade results {output}");
                    SaveStockTracker(stocktracker, Platform.CharlesSchwab, direction, qty, IsReversed, order, output, _logger/*, ref ctx*/);
                }
            }
            catch (Exception ex)
            {
                Log(_logger, LogLevel.Error, $"Error in buying stock - {ex.Message}");
            }
            return true;
        }
        public bool BuySellStock(StockTracker2 stocktracker, dynamic order, TradeDirection direction, int qty, bool IsReversed, ETradeService service)
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
                    Log(_logger, LogLevel.Error, $"Stock trade failed {order["Symbol"].ToString()} {output}");
                    return false;
                }
                Log(_logger, LogLevel.Information, $"Stock trade results {order["Symbol"].ToString()} Success");
                //SaveStockTracker(stocktracker, direction, qty, order, "Success");
                SaveStockTracker(stocktracker, Platform.ETrade, direction, qty, IsReversed, order, "Success", _logger/*, ref ctx*/);
                return true;
            }
            catch (Exception ex)
            {
                Log(_logger, LogLevel.Error, $"Error in buying stock - {ex.Message}");
            }
            return true;
        }
        void QueueStocktoBuy(dynamic order, Platform platform, TradeDirection direction, int qty)
        {
            SaveStockTracker(null, platform, direction, qty, false, order, "QUEUED" , _logger);
        }
        public void CheckforQueuedStock(RulesEngine settings, Platform platform)
        {
            int? longCnt = 0;
            int? shortCnt = 0;
            var queuedStock = ctx.StockTracker2.Where(x => x.Status == "QUEUED" && x.TradePlatform == (int)platform).ToList();
            foreach (var stock in queuedStock)
            {
                dynamic? results = services.GetQuote(stock.Symbol);
                CalculatePoints(results, /*false, true,*/ ref shortCnt, ref longCnt);
                if (longCnt >= settings.StockLongCount || (shortCnt >= settings.StockShortCount && (bool)settings.ShortEnabled))
                {
                    switch(platform)
                    {
                        case Platform.CharlesSchwab:
                            BuySellStock(stock, results, (TradeDirection)stock.TradeDirection, stock.Quantity, false, services/*, true*/);
                            break;
                        case Platform.ETrade:
                            BuySellStock(stock, results, (TradeDirection)stock.TradeDirection, stock.Quantity, false, services2/*, true*/);
                            break;
                    }
                    //BuySellStock(stock, results, (TradeDirection)stock.TradeDirection, stock.Quantity, false, services/*, true*/);
                }
                else
                {
                    SaveStockTracker(stock, platform, (TradeDirection)stock.TradeDirection, stock.Quantity, false, results, "CANCEL", _logger);
                }
            }
        }
        //public bool ReverseStock(StockTracker stock, JsonObject? results, dynamic services)
        //{
        //    //reverse stock
        //    Log(_logger, LogLevel.Information, $"Attempting Reversing stock {stock.Symbol} - ({stock.Name})");
        //    TradeDirection tradeDirection = (TradeDirection)stock.TradeDirection;
        //    int? shortCnt = 0; int? longCnt = 0;
        //    Log(_logger, LogLevel.Information, $"Checking {stock.Symbol} - ({stock.Name}) Points");
        //    CalculatePoints(results, false, false, ref shortCnt, ref longCnt);
        //    if ((longCnt) >= (rulesEngine.StockLongCount - 1) && tradeDirection == TradeDirection.SELL_SHORT)
        //    {
        //        BuySellStock(null, results, TradeDirection.BUY, stock.Quantity, true, services);
        //        Log(_logger, LogLevel.Information, $"Reversing stock {stock.Symbol} - ({stock.Name}). Long score: {longCnt}. Short score {shortCnt}.");
        //    }
        //    else
        //    if ((shortCnt >= rulesEngine.StockShortCount - 1) && tradeDirection == TradeDirection.BUY)
        //    {
        //        BuySellStock(null, results, TradeDirection.SELL_SHORT, stock.Quantity, true, services);
        //        Log(_logger, LogLevel.Information, $"Reversing stock {stock.Symbol} - ({stock.Name}). Long score: {longCnt}. Short score {shortCnt}.");
        //    }
        //    else
        //    {
        //        Log(_logger, LogLevel.Information, $"Not Reversing stock {stock.Symbol} - ({stock.Name}) as it does not meet the points criteria. Long score: {longCnt}. Short score {shortCnt}.");
        //    }
        //    return true;
        //}
        public void Log(ILogger? log, LogLevel logLevel, string message)
        {
            log.Log(logLevel, message);
            log.Log(LogLevel.Information, Environment.NewLine);
        }
        ~Helper()
        {
            ctx?.Dispose();
            ctx = null;
            services = null;
            services2 = null;
        }
    }
}
