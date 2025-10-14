using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using TMD.Services.Service;
using TMDInvestments.Context;
using TMDInvestments.Models;
using TMDInvestments.Models.Models;
using TMDInvestments.Services;

namespace TMD.PricePrediction
{
    public sealed class Helper
    {
        SchwabSettings? _settings = null;
        ETradeSettings? _settings2 = null;
        private readonly ILogger? _logger = null;
        private readonly IHttpClientFactory? _factory = null;
        SchwabServices? services = null;
        ETradeService? services2 = null;
        TmdContext? ctx = null;
        Polygon? _polygon = null;
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
            services = new SchwabServices(_logger, _settings, _factory);
            services2 = new ETradeService(_logger, _settings2, _factory);
            SetTimeZone();
        }
        public List<BuySellCommodity> GetQuoteList()
        {
            int? shortCnt = 0;
            int? longCnt = 0;
            int? stockCnt = 0;
            int? vixsandpLongCnt = 0;
            int? vixsandpShortCnt = 0;
            int? SandPLong = 0;
            int? SandPShort = 0;
            int? DOWLong = 0;
            int? DOWShort = 0;
            int? NASDAQLong = 0;
            int? NASDAQShort = 0;

            var BuySellStockList = new List<BuySellCommodity>();
            RulesEngine settings = ctx.RulesEngine.FirstOrDefault();

            //Get stock already in trade being tracked.
            var stockTracker = ctx.StockTracker.Where(x => x.PriceSold == null && x.DateSold == null && x.Status == "Filled"/* && x.TradePlatform == (int)Platform.CharlesSchwab*/).ToList();
            var stockTrackerList = stockTracker.Select(y => y.Symbol);
            Log(_logger, LogLevel.Information, $"Get exiting stock tracker");

            //Get list of stocks to trade not in tracker
            var stockList = ctx.StockList.Where(x => !stockTrackerList.Contains(x.Symbol) && !x.ExcludeFromAutomation).OrderBy(y => Guid.NewGuid()).ToList();
            Log(_logger, LogLevel.Information, $"Get stock list to trade");

            //stock sold/brought to cover in the last 24 hrs
            DateTime? minDate = DateTime.MinValue;
            var StockHold24Hr = /*new List<StockTracker>();//*/ ctx.StockTracker.ToList().Where(x => x.PriceSold != null && x.DateSold != null && Math.Abs((x.DateSold - easternTime).Value.TotalHours) < 24).ToList().Select(y => new { Symbol = y.Symbol, DateSold = y.DateSold, DateUpdated = y.DateUpdated }).ToList();
            //Include cancelled item for a 24h hold
            StockHold24Hr.AddRange(ctx.StockTracker.ToList().Where(x => x.Status == "Cancelled" && Math.Abs((x.DateUpdated - easternTime).Value.TotalHours) < 24).ToList().Select(y => new { Symbol = y.Symbol, DateSold = y.DateSold ?? minDate, DateUpdated = y.DateUpdated }).ToList());

            ///Test blank out to test stock quote list count
            //if (stockTracker.Where(x => x.TradePlatform == (int)Platform.CharlesSchwab).Count() >= (settings.MaxNumberofStockstoBuy / 2) &&
            //    stockTracker.Where(x => x.TradePlatform == (int)Platform.ETrade).Count() >= (settings.MaxNumberofStockstoBuy / 2))
            //{
            //    Log(_logger, LogLevel.Warning, $"Stock count for Schwab and Etrade exceed max setting of {settings.MaxNumberofStockstoBuy}");
            //    return new List<BuySellCommodity>();
            //}

            //S&P
            var sandp = services.GetQuote("$SPX", true, false);
            var vix = services.GetQuote("$VIX", true, false);

            //DOW JONES
            var dow = services.GetQuote("$DJI", true, false);
            var vxd = services.GetQuote("$VXD", true, false);

            //NASDAQ
            var nasdaq = services.GetQuote("$NDX", true, false);
            var vxn = services.GetQuote("$VXN", true, false);

            if (!CheckMarketIndexes(vxn) || !CheckMarketIndexes(vxd) || !CheckMarketIndexes(vix) || !CheckMarketIndexes(sandp) || !CheckMarketIndexes(dow) || !CheckMarketIndexes(nasdaq))
            {
                Log(_logger, LogLevel.Error, "Failed to get VIX or S&P or Dow or Nasdaq quote.");
                return null;
            }
            Log(_logger, LogLevel.Information, $"S&P: {sandp["lastPrice"].ToString()}. Dow: {dow["lastPrice"].ToString()}. Nasdaq: {nasdaq["lastPrice"].ToString()}");

            //CheckDowNasdaqandSandP(vix, sandp, dow, vxd, nasdaq, vxn, (settings?.StockLongCount - 1), (settings?.StockShortCount - 1), _logger, ref vixsandpLongCnt, ref vixsandpShortCnt);
            CheckDowNasdaqandSandP(vix, sandp, dow, vxd, nasdaq, vxn, settings?.StockLongCount, settings?.StockShortCount, _logger, ref SandPLong, ref SandPShort, ref DOWLong, ref DOWShort, ref NASDAQLong, ref NASDAQShort);
            Log(_logger, LogLevel.Information, $"DOW Long: {DOWLong}. S&P Long: {SandPLong}. NASDAQ Long: {NASDAQLong}");
            Log(_logger, LogLevel.Information, $"DOW Short: {DOWShort}. S&P Short: {SandPShort}. NASDAQ Short: {NASDAQShort}");

            if (SandPLong == 1)
            {
                vixsandpLongCnt = 1;
            }
            if (SandPShort == 1)
            {
                vixsandpShortCnt = 1;
            }
            //if (DOWLong > 0 && SandPLong > 0 && NASDAQLong > 0)
            //{
            //    vixsandpLongCnt = 1;
            //}
            //if (DOWShort > 0 && SandPShort > 0 && NASDAQShort > 0)
            //{
            //    vixsandpShortCnt = 1;
            //}

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
                    if (Convert.ToDecimal(results["lastPrice"].ToString()) >= settings.MinStockPrice /*&& !stock.ExcludeFromAutomation*/)
                    {
                        CalculatePoints(results, true, false, false, ref shortCnt, ref longCnt);
                    }
                    else
                    {
                        Log(_logger, LogLevel.Information, $"Not buying/selling short {results["Symbol"].ToString()} - ({results["Symbol"].ToString()}). Stock fall below min stock price or is excluded");
                    }

                    Log(_logger, LogLevel.Information, $"{stock.Symbol} - ({stock.Name}) Score. Long score: {longCnt}. Short score {shortCnt}");

                    if ((longCnt + vixsandpLongCnt) >= settings.StockLongCount)
                    {
                        Log(_logger, LogLevel.Information, $"Adding {stock.Symbol} - ({stock.Name}) to potential long list.");
                        BuySellStockList.Add(new BuySellCommodity((longCnt + vixsandpLongCnt), TradeDirection.BUY, results));
                        stockCnt++;
                    }
                    else
                    if ((shortCnt + vixsandpShortCnt) >= settings.StockShortCount && (bool)settings.ShortEnabled)
                    {
                        Log(_logger, LogLevel.Information, $"Adding {stock.Symbol} - ({stock.Name}) to potential short list.");
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
        public void CalculatePoints(JsonObject? results, bool useVWAP, bool useOneHourRSI, bool use31016, ref int? shortCnt, ref int? longCnt)
        {
            shortCnt = 0;
            longCnt = 0;

            //if (!useOneHourRSI)
            //{
            if (Convert.ToDecimal(results["RSI"].ToString()) > 0)
            {
                if (Convert.ToDecimal(results["RSI"].ToString()) >= 80)
                {
                    shortCnt = shortCnt + 2;
                }
                if (Convert.ToDecimal(results["RSI"].ToString()) <= 20)
                {
                    longCnt = longCnt + 2;
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

            if (use31016)
            {
                //// 3, 10, 16 STRATEGY
                if (Convert.ToDecimal(results["EMA3"].ToString()) > Convert.ToDecimal(results["EMA10"].ToString()))
                { longCnt = longCnt + 3; }
                if (Convert.ToDecimal(results["EMA3"].ToString()) > Convert.ToDecimal(results["EMA16"].ToString()) &&
                    Convert.ToDecimal(results["EMA10"].ToString()) > Convert.ToDecimal(results["EMA16"].ToString()))
                { longCnt = longCnt + 3; }
                if (Convert.ToDecimal(results["SMA3"].ToString()) > Convert.ToDecimal(results["SMA10"].ToString()))
                { longCnt = longCnt + 3; }
                if (Convert.ToDecimal(results["SMA3"].ToString()) > Convert.ToDecimal(results["SMA16"].ToString()) &&
                    Convert.ToDecimal(results["SMA10"].ToString()) > Convert.ToDecimal(results["SMA16"].ToString()))
                { longCnt = longCnt + 3; }

                if (Convert.ToDecimal(results["EMA3"].ToString()) < Convert.ToDecimal(results["EMA10"].ToString()))
                { shortCnt = shortCnt + 3; }
                if (Convert.ToDecimal(results["EMA3"].ToString()) < Convert.ToDecimal(results["EMA16"].ToString()) &&
                    Convert.ToDecimal(results["EMA10"].ToString()) < Convert.ToDecimal(results["EMA16"].ToString()))
                { shortCnt = shortCnt + 3; }
                if (Convert.ToDecimal(results["SMA3"].ToString()) < Convert.ToDecimal(results["SMA10"].ToString()))
                { shortCnt = shortCnt + 3; }
                if (Convert.ToDecimal(results["SMA3"].ToString()) < Convert.ToDecimal(results["SMA16"].ToString()) &&
                    Convert.ToDecimal(results["SMA10"].ToString()) < Convert.ToDecimal(results["SMA16"].ToString()))
                { shortCnt = shortCnt + 3; }
            }
            else
            {
                //// 5, 8, 13 STRATEGY
                if (Convert.ToDecimal(results["EMA5"].ToString()) > Convert.ToDecimal(results["EMA8"].ToString()))
                { longCnt = longCnt + 3; }
                if (Convert.ToDecimal(results["EMA8"].ToString()) > Convert.ToDecimal(results["EMA13"].ToString()))
                { longCnt = longCnt + 3; }
                //if (Convert.ToDecimal(results["lastPrice"].ToString()) > Convert.ToDecimal(results["EMA5"].ToString()) &&
                //    Convert.ToDecimal(results["lastPrice"].ToString()) > Convert.ToDecimal(results["EMA8"].ToString()) &&
                //    Convert.ToDecimal(results["lastPrice"].ToString()) > Convert.ToDecimal(results["EMA13"].ToString()))
                //{ longCnt = longCnt + 2; }
                if (Convert.ToDecimal(results["EMA5"].ToString()) < Convert.ToDecimal(results["EMA8"].ToString()))
                { shortCnt = shortCnt + 3; }
                if (Convert.ToDecimal(results["EMA8"].ToString()) < Convert.ToDecimal(results["EMA13"].ToString()))
                { shortCnt = shortCnt + 3; }
                //if (Convert.ToDecimal(results["lastPrice"].ToString()) < Convert.ToDecimal(results["EMA5"].ToString()) &&
                //    Convert.ToDecimal(results["lastPrice"].ToString()) < Convert.ToDecimal(results["EMA8"].ToString()) &&
                //    Convert.ToDecimal(results["lastPrice"].ToString()) < Convert.ToDecimal(results["EMA13"].ToString()))
                //{ shortCnt = shortCnt + 2; }

                if (Convert.ToDecimal(results["SMA5"].ToString()) > Convert.ToDecimal(results["SMA8"].ToString()))
                { longCnt = longCnt + 3; }
                if (Convert.ToDecimal(results["SMA8"].ToString()) > Convert.ToDecimal(results["SMA13"].ToString()))
                { longCnt = longCnt + 3; }
                //if (Convert.ToDecimal(results["lastPrice"].ToString()) > Convert.ToDecimal(results["SMA5"].ToString()) &&
                //    Convert.ToDecimal(results["lastPrice"].ToString()) > Convert.ToDecimal(results["SMA8"].ToString()) &&
                //    Convert.ToDecimal(results["lastPrice"].ToString()) > Convert.ToDecimal(results["SMA13"].ToString()))
                //{ longCnt = longCnt + 2; }
                if (Convert.ToDecimal(results["SMA5"].ToString()) < Convert.ToDecimal(results["SMA8"].ToString()))
                { shortCnt = shortCnt + 3; }
                if (Convert.ToDecimal(results["SMA8"].ToString()) < Convert.ToDecimal(results["SMA13"].ToString()))
                { shortCnt = shortCnt + 3; }
                //if (Convert.ToDecimal(results["lastPrice"].ToString()) < Convert.ToDecimal(results["SMA5"].ToString()) &&
                //    Convert.ToDecimal(results["lastPrice"].ToString()) < Convert.ToDecimal(results["SMA8"].ToString()) &&
                //    Convert.ToDecimal(results["lastPrice"].ToString()) < Convert.ToDecimal(results["SMA13"].ToString()))
                //{ shortCnt = shortCnt + 2; }
            }

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
            //if (Convert.ToDecimal(results["MACD"].ToString()) >= Convert.ToDecimal(results["MACDSignal"].ToString()) && (Convert.ToDecimal(results["MACDSignal"].ToString()) < 0 && Convert.ToDecimal(results["MACD"].ToString()) > 0))
            //    {
            //        longCnt = longCnt + 8;
            //    }
            //    if (Convert.ToDecimal(results["MACD"].ToString()) <= Convert.ToDecimal(results["MACDSignal"].ToString()) && (Convert.ToDecimal(results["MACDSignal"].ToString()) > 0 && Convert.ToDecimal(results["MACD"].ToString()) < 0))
            //    {
            //        shortCnt = shortCnt + 8;
            //    }
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


            //if (Convert.ToDecimal(results["MACDHistogram"].ToString()) > 0m) { longCnt++; }
            //if (Convert.ToDecimal(results["MACDHistogram"].ToString()) < 0m) { shortCnt++; }
            System.Threading.Thread.Sleep(150); //Sleep for 150 milliseconds to avoid throttling
        }
        //public void CheckDowNasdaqandSandP(JsonObject vix, JsonObject sandp, JsonObject dow, JsonObject vxd, JsonObject nasdaq, JsonObject vxn, int? longMax, int? shortMax, ILogger _logger, ref int? vixsandpLongCnt, ref int? vixsandpShortCnt)
        public void CheckDowNasdaqandSandP(JsonObject vix, JsonObject sandp, JsonObject dow, JsonObject vxd, JsonObject nasdaq, JsonObject vxn, int? longMax, int? shortMax, ILogger _logger, ref int? SandPLong, ref int? SandPShort, ref int? DOWLong, ref int? DOWShort, ref int? NASDAQLong, ref int? NASDAQShort)
        {
            int? shortCnt = 0;
            int? longCnt = 0;
            //int? longTotal = 0;
            //int? shortTotal = 0;

            //_logger.LogInformation($"Checking VIX and S&P. VIX at {vix["lastPrice"].ToString()}. S&P at {sandp["lastPrice"].ToString()}.");

            CalculatePoints(vix, true, false, false, ref shortCnt, ref longCnt);

            _logger.LogInformation($"VIX Score. Long score: {longCnt}. Short score {shortCnt}");

            //Opposite for VIX
            if (longCnt >= (longMax)) { SandPShort++; }
            if (shortCnt >= (shortMax)) { SandPLong++; }
            shortCnt = 0;
            longCnt = 0;

            CalculatePoints(sandp, true, false, false, ref shortCnt, ref longCnt);

            _logger.LogInformation($"S&P Score. Long score: {longCnt}. Short score {shortCnt}");

            if (longCnt >= (longMax)) { SandPLong++; }
            if (shortCnt >= (shortMax)) { SandPShort++; }
            shortCnt = 0;
            longCnt = 0;

            CalculatePoints(vxd, true, false, false, ref shortCnt, ref longCnt);

            _logger.LogInformation($"VXD Score. Long score: {longCnt}. Short score {shortCnt}");

            //Opposite for VXD
            if (longCnt >= (longMax)) { DOWShort++; }
            if (shortCnt >= (shortMax)) { DOWLong++; }
            shortCnt = 0;
            longCnt = 0;

            CalculatePoints(dow, true, false, false, ref shortCnt, ref longCnt);

            _logger.LogInformation($"DOW Score. Long score: {longCnt}. Short score {shortCnt}");

            if (longCnt >= (longMax)) { DOWLong++; }
            if (shortCnt >= (shortMax)) { DOWShort++; }
            shortCnt = 0;
            longCnt = 0;

            CalculatePoints(vxn, true, false, false, ref shortCnt, ref longCnt);

            _logger.LogInformation($"VXN Score. Long score: {longCnt}. Short score {shortCnt}");

            //Opposite for VXN
            if (longCnt >= (longMax)) { NASDAQShort++; }
            if (shortCnt >= (shortMax)) { NASDAQLong++; }
            shortCnt = 0;
            longCnt = 0;

            CalculatePoints(nasdaq, true, false, false, ref shortCnt, ref longCnt);

            _logger.LogInformation($"NASDAQ Score. Long score: {longCnt}. Short score {shortCnt}");

            if (longCnt >= (longMax)) { NASDAQLong++; }
            if (shortCnt >= (shortMax)) { NASDAQShort++; }

            //vixsandpLongCnt = (longTotal > 0) ? 1 : 0;
            //vixsandpShortCnt = (shortTotal > 0) ? 1 : 0;

            //_logger.LogInformation($"Score. Long total {vixsandpLongCnt}. Short total {vixsandpShortCnt}.");
        }
        public bool IsNotHolidayorWeekend()
        {
            bool run = true;
            DateTime runTime = easternTime;
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
            if (runTime.Month == 11 && runTime.DayOfWeek != DayOfWeek.Thursday && runTime.Day > 24 && runTime.Day <= 30)
                run = false;

            //Christmas
            if (runTime.Month == 12 && runTime.Day == 25)
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

        public bool SaveStockTracker(StockTracker stockTracker, Platform tradePlatform, TradeDirection direction, int qty, bool IsReversed, dynamic results, string status, ILogger _logger, ref TmdContext ctx)
        {
            if (stockTracker != null && (direction == TradeDirection.SELL || direction == TradeDirection.BUY_TO_COVER))
            {
                stockTracker.PriceSold = Convert.ToDecimal(results["lastPrice"].ToString());
                stockTracker.SoldRsi = Convert.ToDecimal(results["RSI"].ToString());
                //stockTracker.SoldSma200 = Convert.ToDecimal(results["SMA30"].ToString());
                //stockTracker.SoldEma200 = Convert.ToDecimal(results["EMA30"].ToString());
                //stockTracker.SoldVwap = Convert.ToDecimal(results["VWAP"].ToString());
                stockTracker.SoldEma9 = Convert.ToDecimal(results["EMA9"].ToString());
                stockTracker.SoldEma12 = Convert.ToDecimal(results["EMA12"].ToString());
                stockTracker.SoldEma26 = Convert.ToDecimal(results["EMA26"].ToString());
                stockTracker.SoldMacd = Convert.ToDecimal(results["MACD"].ToString());
                stockTracker.DateSold = easternTime;
                stockTracker.DateUpdated = easternTime;
                stockTracker.Status = "FILLED";// status;
                ctx.StockTracker.Attach(stockTracker);
                ctx.Entry(stockTracker).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
            }
            else
            {
                stockTracker = new StockTracker();
                stockTracker.Symbol = results["Symbol"].ToString();
                stockTracker.Name = results["Name"].ToString();
                stockTracker.TradeDirection = (int)direction;
                stockTracker.TradePlatform = (int)tradePlatform;
                stockTracker.PricePurchased = Convert.ToDecimal(results["lastPrice"].ToString());
                stockTracker.IsReversed = IsReversed;
                stockTracker.Quantity = qty;
                stockTracker.PurchasedRsi = Convert.ToDecimal(results["RSI"].ToString());
                //stockTracker.PurchasedSma200 = Convert.ToDecimal(results["SMA30"].ToString());
                //stockTracker.PurchasedEma200 = Convert.ToDecimal(results["EMA30"].ToString());
                //stockTracker.PurchasedVwap = Convert.ToDecimal(results["VWAP"].ToString());
                stockTracker.PurchasedEma9 = Convert.ToDecimal(results["EMA9"].ToString());
                stockTracker.PurchasedEma12 = Convert.ToDecimal(results["EMA12"].ToString());
                stockTracker.PurchasedEma26 = Convert.ToDecimal(results["EMA26"].ToString());
                stockTracker.PurchasedMacd = Convert.ToDecimal(results["MACD"].ToString());
                stockTracker.LastPrice = Convert.ToDecimal(results["lastPrice"].ToString());
                stockTracker.DatePurchased = easternTime;
                stockTracker.DateCreated = easternTime;
                stockTracker.LastUpdatedBy = 1;
                stockTracker.Status = status;
                stockTracker.IsReversed = false;
                ctx.StockTracker.Add(stockTracker);
                ctx.Entry(stockTracker).State = Microsoft.EntityFrameworkCore.EntityState.Added;
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
            StockTracker? stockTracker = null;
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

            decimal cash = Convert.ToDecimal(initialBalances.GetProperty("cashBalance").ToString());
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
                var stockTrackerList = ctx.StockTracker.Where(x => x.PriceSold == null && x.DateSold == null
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
                            ctx.StockTracker.Attach(rem);
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
                    stockTracker = ctx.StockTracker.Where(x => x.PriceSold == null && x.DateSold == null
                    && x.TradePlatform == (int)Platform.CharlesSchwab && /*(x.Status != "Cancelled" && x.Status != "Filled")*/
                    x.Status.Trim() == "{\"$id\":\"1\",\"status\":\"Created\"}"
                    && (x.DateCreated >= easternTime.AddDays(-1) && x.DateCreated <= easternTime.AddDays(1))
                    && x.Symbol == sym
                    ).FirstOrDefault();
                }

                if (stockTracker == null)
                {
                    stockTracker = new StockTracker();
                    stockTracker.Symbol = sym;
                    stockTracker.Name = desc;
                    stockTracker.PurchasedRsi = Convert.ToDecimal(quote?["RSI"]?.ToString());
                    stockTracker.PurchasedEma9 = Convert.ToDecimal(quote?["EMA9"]?.ToString());
                    stockTracker.PurchasedEma12 = Convert.ToDecimal(quote?["EMA12"]?.ToString());
                    stockTracker.PurchasedEma26 = Convert.ToDecimal(quote?["EMA26"]?.ToString());
                    stockTracker.PurchasedMacd = Convert.ToDecimal(quote?["MACD"]?.ToString());
                    stockTracker.DatePurchased = easternTime;
                    stockTracker.DateCreated = easternTime;
                    stockTracker.Status = "FILLED";
                    stockTracker.LastUpdatedBy = 1;
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
                    stockTracker = ctx.StockTracker.Where(x => x.PriceSold == null && x.DateSold == null
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
                    ctx.StockTracker.Attach(sp);
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

            var stockTrkList = ctx.StockTracker.Where(x => x.PriceSold == null && x.DateSold == null
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
                    ctx.StockTracker.Attach(stk);
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
            StockTracker? stockTracker = null;
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

            decimal cash = Convert.ToDecimal(securitiesAccount.Element("cashBuyingPower").Value.ToString());
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
                var stockTrackerList = ctx.StockTracker.Where(x => x.PriceSold == null && x.DateSold == null
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
                            ctx.StockTracker.Attach(rem);
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
                    stockTracker = ctx.StockTracker.Where(x => x.PriceSold == null && x.DateSold == null
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
                        stockTracker = new StockTracker();
                        stockTracker.Symbol = sym;
                        stockTracker.Name = desc;
                        stockTracker.PurchasedRsi = Convert.ToDecimal(quote?["RSI"]?.ToString());
                        stockTracker.PurchasedEma9 = Convert.ToDecimal(quote?["EMA9"]?.ToString());
                        stockTracker.PurchasedEma12 = Convert.ToDecimal(quote?["EMA12"]?.ToString());
                        stockTracker.PurchasedEma26 = Convert.ToDecimal(quote?["EMA26"]?.ToString());
                        stockTracker.PurchasedMacd = Convert.ToDecimal(quote?["MACD"]?.ToString());
                        stockTracker.DatePurchased = easternTime;
                        stockTracker.DateCreated = easternTime;
                        stockTracker.Status = "FILLED";
                        stockTracker.LastUpdatedBy = 1;
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
                    ctx.StockTracker.Attach(sp);
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
            var stockTrkList = ctx.StockTracker.Where(x => x.PriceSold == null && x.DateSold == null
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
                    ctx.StockTracker.Attach(stk);
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
        }
        public void UnloadAgingStocks()
        {
            var agingStock = ctx?.StockTracker?.ToList()?.Where(x => x.PriceSold == null && x.DateSold == null && x.Status == "FILLED"
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
        bool BuySellStock(StockTracker stocktracker, dynamic order, TradeDirection direction, int qty, bool IsReversed, SchwabServices service)
        {
            //return false;
            string? jsonOrder = CreateTradeObject(order, direction, qty);
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
                    //helper.Log(_logger, LogLevel.Error, $"stock trade failed {order["Symbol"].ToString()} {output}");
                }
                else
                {
                    //helper.Log(_logger, LogLevel.Information, $"stock trade results {output}");
                    SaveStockTracker(stocktracker, Platform.CharlesSchwab, direction, qty, IsReversed, order, output, _logger, ref ctx);
                }
            }
            catch (Exception ex)
            {
                //helper.Log(_logger, LogLevel.Error, $"Error in buying stock - {ex.Message}");
            }
            return true;
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
                    //helper.Log(_logger, LogLevel.Error, $"Stock trade failed {order["Symbol"].ToString()} {output}");
                    return false;
                }
                //helper.Log(_logger, LogLevel.Information, $"Stock trade results {order["Symbol"].ToString()} Success");
                //SaveStockTracker(stocktracker, direction, qty, order, "Success");
                SaveStockTracker(stocktracker, Platform.ETrade, direction, qty, IsReversed, order, "Success", _logger, ref ctx);
                return true;
            }
            catch (Exception ex)
            {
                //helper.Log(_logger, LogLevel.Error, $"Error in buying stock - {ex.Message}");
            }
            return true;
        }
        public void Log(ILogger? log, LogLevel logLevel, string message)
        {
            log.Log(logLevel, message);
            log.Log(LogLevel.Information, Environment.NewLine);
        }
    }
}
