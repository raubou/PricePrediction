using Coinbase.AdvancedTrade.Enums;
using Coinbase.AdvancedTrade.Models.Public;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Nodes;
using TMDInvestments.Models;
using TMDInvestments.Services;

namespace TMD.Services.Service
{
    public sealed class IndicesCalculatorV2//<, J, C, T> where S : class/*, new() where J : class*/, new() where C : class, new() where T : class, new()
    {
        ILogger? _log = null;
        SchwabSettings? _settings = null;
        CoinBaseSettings? _coinbaseSettings = null; 
        IHttpClientFactory? _factory = null;
        SchwabServices? services;
        CoinBaseService? coinbaseService;
        public IndicesCalculatorV2(ILogger log, SchwabSettings settings, IHttpClientFactory factory)
        {
            _log = log;
            _settings = settings;
            _factory = factory;            
        }
        public IndicesCalculatorV2(ILogger log, CoinBaseSettings coinbaseSettings, IHttpClientFactory factory)
        {
            _log = log;
            _coinbaseSettings = coinbaseSettings;
            _factory = factory;
        }
        public dynamic CalculateV2(string symbol, JsonElement quote)
        {
            services = new SchwabServices(_log, _settings, _factory);

            decimal currentPrice = 0m;
            decimal totalVolume = 0m;
            string exchange = string.Empty;
            dynamic? results = null;
            dynamic? ser = null;
            int? totalDaysBack = 0;
            List<TMDInvestment.Models.CandleItems> candle90 = new List<TMDInvestment.Models.CandleItems>();
            try
            {
                currentPrice = Convert.ToDecimal(quote.GetProperty(symbol.ToUpper()).GetProperty("quote").GetProperty("lastPrice").ToString());
                totalVolume = Convert.ToDecimal(quote.GetProperty(symbol.ToUpper()).GetProperty("quote").GetProperty("totalVolume").ToString());
                exchange = quote.GetProperty(symbol.ToUpper()).GetProperty("reference").GetProperty("exchangeName").ToString();

            }
            catch (Exception ex)
            {
                try
                {
                    currentPrice = Convert.ToDecimal(quote.GetProperty("quote").GetProperty("lastPrice").ToString());
                    totalVolume = Convert.ToDecimal(quote.GetProperty("quote").GetProperty("totalVolume").ToString());
                    exchange = quote.GetProperty("reference").GetProperty("exchangeName").ToString();
                }
                catch (Exception ex2)
                {
                    _log.LogError($"{ex2.Message}", ex2);
                    return null;
                }
            }

            //var results = services.GetPriceHistory(symbol, "month", "1", "daily", 1, DateTime.Now.AddDays(-90), DateTime.Now);//.ToList();
            DateTime previewDate = DateTime.Now;
            if (previewDate.DayOfWeek == DayOfWeek.Saturday)
                previewDate = previewDate.AddDays(-1).AddHours(-24);
            else if (previewDate.DayOfWeek == DayOfWeek.Sunday)
                previewDate = previewDate.AddDays(-2).AddHours(-24);
            else
                previewDate = previewDate.AddHours(-24);
            goto GetPriceHistory;

        GoBackOneDay:
            previewDate = previewDate.AddHours(-24);
            results = services.GetPriceHistory(symbol, "day", "1", "minute", 15,
                previewDate, DateTime.Now);
            ser = JsonSerializer.Serialize(results.GetProperty("candles"));
            candle90 = JsonSerializer.Deserialize<List<TMDInvestment.Models.CandleItems>>(ser);

        //var results = services.GetPriceHistory(symbol, "day", "1", "minute", 15, 
        //    new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0, 0), 
        //    new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 12, 23, 59, 59));
        GetPriceHistory:
            System.Threading.Thread.Sleep(25); //Sleep for 50 milliseconds to avoid throttling
            results = services.GetPriceHistory(symbol, "day", "1", "minute", 15,
                    previewDate, DateTime.Now);
            ser = JsonSerializer.Serialize(results.GetProperty("candles"));
            candle90 = JsonSerializer.Deserialize<List<TMDInvestment.Models.CandleItems>>(ser);

            if(candle90.Count < 26)
            {
                if(totalDaysBack> 5) //give up after 5 days
                {
                    goto GetCandles;
                }
                totalDaysBack++;
                goto GoBackOneDay;
            }

        GetCandles:
            //var test = candle90.OrderByDescending(x => x.datetime).ToList();
            decimal? SMA12 = (candle90.Count > 0) ? candle90.OrderByDescending(x => x.datetime).Take(12).Average(c => c.close) : 0m;
            decimal? SMA26 = (candle90.Count > 0) ? candle90.OrderByDescending(x => x.datetime).Take(26)?.Average(c => c.close) : 0m;
            decimal? SMA9 = (candle90.Count > 0) ? candle90.OrderByDescending(x => x.datetime).Take(9)?.Average(c => c.close) : 0m;
            decimal? SMA3 = (candle90.Count > 0) ? candle90.OrderByDescending(x => x.datetime).Take(3)?.Average(c => c.close) : 0m;
            decimal? SMA10 = (candle90.Count > 0) ? candle90.OrderByDescending(x => x.datetime).Take(10)?.Average(c => c.close) : 0m;
            decimal? SMA16 = (candle90.Count > 0) ? candle90.OrderByDescending(x => x.datetime).Take(16)?.Average(c => c.close) : 0m;
            decimal? SMA5 = (candle90.Count > 0) ? candle90.OrderByDescending(x => x.datetime).Take(5)?.Average(c => c.close) : 0m;
            decimal? SMA8 = (candle90.Count > 0) ? candle90.OrderByDescending(x => x.datetime).Take(8)?.Average(c => c.close) : 0m;
            decimal? SMA13 = (candle90.Count > 0) ? candle90.OrderByDescending(x => x.datetime).Take(13)?.Average(c => c.close) : 0m;

            decimal? smoothingFactor12 = (2m / (candle90.OrderByDescending(x => x.datetime).Take(12).ToList().Count + 1m));
            decimal? smoothingFactor26 = (2m / (candle90.OrderByDescending(x => x.datetime).Take(26).ToList().Count + 1m));            
            decimal? smoothingFactor9 = (2m / (candle90.OrderByDescending(x => x.datetime).Take(9).ToList().Count + 1m));
            decimal? smoothingFactor3 = (2m / (candle90.OrderByDescending(x => x.datetime).Take(3).ToList().Count + 1m));
            decimal? smoothingFactor10 = (2m / (candle90.OrderByDescending(x => x.datetime).Take(10).ToList().Count + 1m));
            decimal? smoothingFactor16 = (2m / (candle90.OrderByDescending(x => x.datetime).Take(16).ToList().Count + 1m));
            decimal? smoothingFactor5 = (2m / (candle90.OrderByDescending(x => x.datetime).Take(5).ToList().Count + 1m));
            decimal? smoothingFactor8 = (2m / (candle90.OrderByDescending(x => x.datetime).Take(8).ToList().Count + 1m));
            decimal? smoothingFactor13 = (2m / (candle90.OrderByDescending(x => x.datetime).Take(13).ToList().Count + 1m));

            decimal? EMA12 = (candle90.Count > 0) ? (candle90.OrderByDescending(x => x.datetime).ToList()[0].close * smoothingFactor12) + (SMA12 * (1 - smoothingFactor12)) : 0m;
            decimal? EMA26 = (candle90.Count > 0) ? (candle90.OrderByDescending(x => x.datetime).ToList()[0].close * smoothingFactor26) + (SMA26 * (1 - smoothingFactor26)) : 0m;
            decimal? EMA9 = (candle90.Count > 0) ? (candle90.OrderByDescending(x => x.datetime).ToList()[0].close * smoothingFactor9) + (SMA9 * (1 - smoothingFactor9)) : 0m;
            decimal? EMA3 = (candle90.Count > 0) ? (candle90.OrderByDescending(x => x.datetime).ToList()[0].close * smoothingFactor3) + (SMA3 * (1 - smoothingFactor3)) : 0m;
            decimal? EMA10 = (candle90.Count > 0) ? (candle90.OrderByDescending(x => x.datetime).ToList()[0].close * smoothingFactor10) + (SMA10 * (1 - smoothingFactor10)) : 0m;
            decimal? EMA16 = (candle90.Count > 0) ? (candle90.OrderByDescending(x => x.datetime).ToList()[0].close * smoothingFactor16) + (SMA16 * (1 - smoothingFactor16)) : 0m;
            decimal? EMA5 = (candle90.Count > 0) ? (candle90.OrderByDescending(x => x.datetime).ToList()[0].close * smoothingFactor5) + (SMA5 * (1 - smoothingFactor5)) : 0m;
            decimal? EMA8 = (candle90.Count > 0) ? (candle90.OrderByDescending(x => x.datetime).ToList()[0].close * smoothingFactor8) + (SMA8 * (1 - smoothingFactor8)) : 0m;
            decimal? EMA13 = (candle90.Count > 0) ? (candle90.OrderByDescending(x => x.datetime).ToList()[0].close * smoothingFactor13) + (SMA13 * (1 - smoothingFactor13)) : 0m;

            var closes = candle90.OrderBy(x => x.datetime)/*.Take(52)*/.Select(y => new MACDClosesDateTime { Price = (double)y.close, Datetime = DateTimeOffset.FromUnixTimeMilliseconds(y.datetime).DateTime }).ToList();
            var macdCalculator = new MacdCalculator();
            List<MacdResult> macdCalculatorResults = macdCalculator.CalculateMacd(closes);

            decimal? MACDLine = (decimal)macdCalculatorResults?.LastOrDefault()?.MacdLine;
            decimal? MACDSignal = (decimal)macdCalculatorResults?.LastOrDefault()?.SignalLine;
            decimal? MACDHistogram = (decimal)macdCalculatorResults?.LastOrDefault()?.Histogram;
            decimal? MACDLine2 = EMA3 - EMA10;
            decimal? MACDSignal2 = EMA3 - EMA16;
            decimal? MACDHistogram2 = MACDLine2 - MACDSignal2;
            decimal? MACDLine3 = EMA5 - EMA8;
            decimal? MACDSignal3 = EMA5 - EMA13;
            decimal? MACDHistogram3 = MACDLine3 - MACDSignal3;

            List<decimal?> RSIList = candle90.OrderByDescending(x => x.datetime).Take(15).Select(c => (decimal?)c.close).ToList();
            List<decimal?> gains = new List<decimal?>();
            List<decimal?> losses = new List<decimal?>();

            for (int i = 1; i <= RSIList.Count - 1; i++)
            {
                decimal? change = RSIList[i] - RSIList[i - 1];
                if (change > 0)
                    gains.Add(change);
                else
                    losses.Add(Math.Abs((decimal)change));
            }

            decimal? averageGain = gains.Sum() / 14;
            decimal? averageLoss = losses.Sum() / 14;

            decimal? rs = 0m;
            decimal? rsi = 0m;
            try
            {
                rs = averageGain / averageLoss;
                rsi = 100 - (100 / (1 + rs));
            }
            catch (Exception ex2)
            {
                _log.LogError($"{ex2.Message}", ex2);
            }

            //var wmaWeight = new List<decimal>();
            //decimal intCnt = 1;
            //foreach (var item in candle90) {
            //    decimal weight = intCnt / candle90.Count;
            //    wmaWeight.Add(weight);
            //    intCnt++;
            //}
            //var wma = candle90.OrderBy(x => x.datetime).Zip(wmaWeight, (c, d) => c.close * d).ToList();
            JsonObject output = new JsonObject();
            output.Add("Symbol", symbol);
            try
            {
                output.Add("Name", quote.GetProperty(symbol).GetProperty("reference").GetProperty("description").ToString());
            }
            catch (Exception)
            {
                try
                {
                    output.Add("Name", quote.GetProperty("reference").GetProperty("description").ToString());
                }
                catch (Exception ex2)
                {
                    _log.LogError($"{ex2.Message}", ex2);
                    return null;
                }
            }
            var resultsJson = JsonSerializer.Deserialize<JsonNode>(JsonSerializer.Serialize(quote));
            output.Add("lastPrice", currentPrice);
            output.Add("results", resultsJson);
            output.Add("RSI", rsi);
            output.Add("Exchange", exchange);
            //output.Add("RSIOneHour", RSIOneHour);
            output.Add("EMA26", EMA26);
            output.Add("EMA12", EMA12);
            output.Add("EMA9", EMA9);
            output.Add("EMA3", EMA3);
            output.Add("EMA10", EMA10);
            output.Add("EMA16", EMA16);
            output.Add("EMA5", EMA5);
            output.Add("EMA8", EMA8);
            output.Add("EMA13", EMA13);
            output.Add("SMA26", SMA26);
            output.Add("SMA12", SMA12);
            output.Add("SMA9", SMA9);
            output.Add("SMA3", SMA3);
            output.Add("SMA10", SMA10);
            output.Add("SMA16", SMA16);
            output.Add("SMA5", SMA5);
            output.Add("SMA8", SMA8);
            output.Add("SMA13", SMA13);
            //output.Add("WMA26", WMA26);
            //output.Add("WMA12", WMA12);
            //output.Add("WMA9", WMA9);
            //output.Add("WMA3", WMA3);
            //output.Add("WMA10", WMA10);
            //output.Add("WMA16", WMA16);
            //output.Add("WMA5", WMA5);
            //output.Add("WMA13", WMA13);
            output.Add("MACD", MACDLine);
            output.Add("MACDSignal", MACDSignal);
            output.Add("MACDHistogram", MACDHistogram);
            output.Add("MACD2", MACDLine);
            output.Add("MACDSignal2", MACDSignal2);
            output.Add("MACDHistogram2", MACDHistogram2);
            output.Add("MACD3", MACDLine3);
            output.Add("MACDSignal3", MACDSignal3);
            output.Add("MACDHistogram3", MACDHistogram3);
            //output.Add("VWAP", VWAP);
            output.Add("volume", totalVolume);
            //output.Add("SMACount", candle30.Count());
            return output;
        }

        public dynamic CalculateV2(PublicProduct product)
        {
            coinbaseService = new CoinBaseService(_log, _coinbaseSettings, _factory);

            decimal currentPrice = 0m;
            decimal totalVolume = 0m;
            List<Coinbase.AdvancedTrade.Models.Candle> candle90 = new List<Coinbase.AdvancedTrade.Models.Candle>();

            currentPrice = Convert.ToDecimal(product.Price);
            totalVolume = Convert.ToDecimal(product.Volume24h);

            //List<Coinbase.AdvancedTrade.Models.Candle> candle90 = coinbaseService.GetPriceHistory(product.ProductId, DateTime.Now.AddDays(-90), DateTime.Now, Granularity.ONE_DAY).OrderByDescending(x => x.StartDate).ToList();
            var previewDate = DateTime.Now.AddHours(-24);
            //List<Coinbase.AdvancedTrade.Models.Candle> candle90 = coinbaseService.GetPriceHistory(product.ProductId,
            //    new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0, 0), 
            //    new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 12, 23, 59, 59), 
            //    Granularity.FIFTEEN_MINUTE).OrderByDescending(x => x.StartDate).ToList();
            candle90 = coinbaseService.GetPriceHistory(product.ProductId,
                previewDate, DateTime.Now,
                Granularity.FIFTEEN_MINUTE).OrderByDescending(x => x.StartDate).ToList();

            //var test = candle90.OrderByDescending(x => x.StartDate).ToList();

            decimal? SMA12 = (candle90.Count > 0) ? candle90.OrderByDescending(x => x.StartDate).Take(12).Average(c => Convert.ToDecimal(c.Close)) : 0m;
            decimal? SMA26 = (candle90.Count > 0) ? candle90.OrderByDescending(x => x.StartDate).Take(26).Average(c => Convert.ToDecimal(c.Close)) : 0m;
            decimal? SMA9 = (candle90.Count > 0) ? candle90.OrderByDescending(x => x.StartDate).Take(9).Average(c => Convert.ToDecimal(c.Close)) : 0m;
            decimal? SMA3 = (candle90.Count > 0) ? candle90.OrderByDescending(x => x.StartDate).Take(3).Average(c => Convert.ToDecimal(c.Close)) : 0m;
            decimal? SMA10 = (candle90.Count > 0) ? candle90.OrderByDescending(x => x.StartDate).Take(10).Average(c => Convert.ToDecimal(c.Close)) : 0m;
            decimal? SMA16 = (candle90.Count > 0) ? candle90.OrderByDescending(x => x.StartDate).Take(16).Average(c => Convert.ToDecimal(c.Close)) : 0m;
            decimal? SMA5 = (candle90.Count > 0) ? candle90.OrderByDescending(x => x.StartDate).Take(5).Average(c => Convert.ToDecimal(c.Close)) : 0m;
            decimal? SMA8 = (candle90.Count > 0) ? candle90.OrderByDescending(x => x.StartDate).Take(8).Average(c => Convert.ToDecimal(c.Close)) : 0m;
            decimal? SMA13 = (candle90.Count > 0) ? candle90.OrderByDescending(x => x.StartDate).Take(13).Average(c => Convert.ToDecimal(c.Close)) : 0m;

            decimal? smoothingFactor12 = (2m / (candle90.OrderByDescending(x => x.StartDate).Take(12).ToList().Count + 1m));
            decimal? smoothingFactor26 = (2m / (candle90.OrderByDescending(x => x.StartDate).Take(26).ToList().Count + 1m));
            decimal? smoothingFactor9 = (2m / (candle90.OrderByDescending(x => x.StartDate).Take(9).ToList().Count + 1m));
            decimal? smoothingFactor3 = (2m / (candle90.OrderByDescending(x => x.StartDate).Take(3).ToList().Count + 1m));
            decimal? smoothingFactor10 = (2m / (candle90.OrderByDescending(x => x.StartDate).Take(10).ToList().Count + 1m));
            decimal? smoothingFactor16 = (2m / (candle90.OrderByDescending(x => x.StartDate).Take(16).ToList().Count + 1m));
            decimal? smoothingFactor5 = (2m / (candle90.OrderByDescending(x => x.StartDate).Take(5).ToList().Count + 1m));
            decimal? smoothingFactor8 = (2m / (candle90.OrderByDescending(x => x.StartDate).Take(8).ToList().Count + 1m));
            decimal? smoothingFactor13 = (2m / (candle90.OrderByDescending(x => x.StartDate).Take(13).ToList().Count + 1m));

            decimal? EMA12 = (candle90.Count > 0) ? (Convert.ToDecimal(candle90.OrderByDescending(x => x.StartDate).ToList()[0].Close) * smoothingFactor12) + (SMA12 * (1 - smoothingFactor12)) : 0m;
            decimal? EMA26 = (candle90.Count > 0) ? (Convert.ToDecimal(candle90.OrderByDescending(x => x.StartDate).ToList()[0].Close) * smoothingFactor26) + (SMA26 * (1 - smoothingFactor26)) : 0m;
            decimal? EMA9 = (candle90.Count > 0) ? (Convert.ToDecimal(candle90.OrderByDescending(x => x.StartDate).ToList()[0].Close) * smoothingFactor9) + (SMA9 * (1 - smoothingFactor9)) : 0m;
            decimal? EMA3 = (candle90.Count > 0) ? (Convert.ToDecimal(candle90.OrderByDescending(x => x.StartDate).ToList()[0].Close) * smoothingFactor3) + (SMA3 * (1 - smoothingFactor3)) : 0m;
            decimal? EMA10 = (candle90.Count > 0) ? (Convert.ToDecimal(candle90.OrderByDescending(x => x.StartDate).ToList()[0].Close) * smoothingFactor10) + (SMA10 * (1 - smoothingFactor10)) : 0m;
            decimal? EMA16 = (candle90.Count > 0) ? (Convert.ToDecimal(candle90.OrderByDescending(x => x.StartDate).ToList()[0].Close) * smoothingFactor16) + (SMA16 * (1 - smoothingFactor16)) : 0m;
            decimal? EMA5 = (candle90.Count > 0) ? (Convert.ToDecimal(candle90.OrderByDescending(x => x.StartDate).ToList()[0].Close) * smoothingFactor5) + (SMA5 * (1 - smoothingFactor5)) : 0m;
            decimal? EMA8 = (candle90.Count > 0) ? (Convert.ToDecimal(candle90.OrderByDescending(x => x.StartDate).ToList()[0].Close) * smoothingFactor8) + (SMA8 * (1 - smoothingFactor8)) : 0m;
            decimal? EMA13 = (candle90.Count > 0) ? (Convert.ToDecimal(candle90.OrderByDescending(x => x.StartDate).ToList()[0].Close) * smoothingFactor13) + (SMA13 * (1 - smoothingFactor13)) : 0m;

            var closes = candle90.OrderBy(x => x.StartDate)/*.Take(52)*/.Select(y => new MACDClosesDateTime { Price = Convert.ToDouble(y.Close), Datetime = y.StartDate }).ToList();
            var macdCalculator = new MacdCalculator();
            List<MacdResult> macdCalculatorResults = macdCalculator.CalculateMacd(closes);

            decimal? MACDLine = (decimal)macdCalculatorResults?.LastOrDefault()?.MacdLine;
            decimal? MACDSignal = (decimal)macdCalculatorResults?.LastOrDefault()?.SignalLine;
            decimal? MACDHistogram = (decimal)macdCalculatorResults?.LastOrDefault()?.Histogram;
            decimal? MACDLine2 = EMA3 - EMA10;
            decimal? MACDSignal2 = EMA3 - EMA16;
            decimal? MACDHistogram2 = MACDLine2 - MACDSignal2;
            decimal? MACDLine3 = EMA5 - EMA8;
            decimal? MACDSignal3 = EMA5 - EMA13;
            decimal? MACDHistogram3 = MACDLine3 - MACDSignal3;

            List<decimal?> RSIList = candle90.OrderByDescending(x => x.StartDate).Take(15).Select(c => (decimal?)Convert.ToDecimal(c.Close)).ToList();
            List<decimal?> gains = new List<decimal?>();
            List<decimal?> losses = new List<decimal?>();

            for (int i = 1; i <= RSIList.Count - 1; i++)
            {
                decimal? change = RSIList[i] - RSIList[i - 1];
                if (change > 0)
                    gains.Add(change);
                else
                    losses.Add(Math.Abs((decimal)change));
            }

            decimal? averageGain = gains.Sum() / 14;
            decimal? averageLoss = losses.Sum() / 14;

            decimal? rs = 0m;
            decimal? rsi = 0m;
            try
            {
                rs = averageGain / averageLoss;
                rsi = 100 - (100 / (1 + rs));
            }
            catch (Exception ex2)
            {
                _log.LogError($"{ex2.Message}", ex2);
            }

            JsonObject output = new JsonObject();
            output.Add("Symbol", product.ProductId);
            output.Add("Name", product.BaseName);
            output.Add("lastPrice", currentPrice);
            //output.Add("results", resultsJson);
            output.Add("RSI", rsi);
            //output.Add("RSIOneHour", RSIOneHour);
            output.Add("EMA26", EMA26);
            output.Add("EMA12", EMA12);
            output.Add("EMA9", EMA9);
            output.Add("EMA3", EMA3);
            output.Add("EMA10", EMA10);
            output.Add("EMA16", EMA16);
            output.Add("EMA5", EMA5);
            output.Add("EMA8", EMA8);
            output.Add("EMA13", EMA13);
            output.Add("SMA26", SMA26);
            output.Add("SMA12", SMA12);
            output.Add("SMA9", SMA9);
            output.Add("SMA3", SMA3);
            output.Add("SMA10", SMA10);
            output.Add("SMA16", SMA16);
            output.Add("SMA5", SMA5);
            output.Add("SMA8", SMA8);
            output.Add("SMA13", SMA13);
            //output.Add("WMA26", WMA26);
            //output.Add("WMA12", WMA12);
            //output.Add("WMA9", WMA9);
            //output.Add("WMA3", WMA3);
            //output.Add("WMA10", WMA10);
            //output.Add("WMA16", WMA16);
            //output.Add("WMA5", WMA5);
            //output.Add("WMA13", WMA13);
            output.Add("MACD", MACDLine);
            output.Add("MACDSignal", MACDSignal);
            output.Add("MACDHistogram", MACDHistogram);
            output.Add("MACD2", MACDLine);
            output.Add("MACDSignal2", MACDSignal2);
            output.Add("MACDHistogram2", MACDHistogram2);
            output.Add("MACD3", MACDLine3);
            output.Add("MACDSignal3", MACDSignal3);
            output.Add("MACDHistogram3", MACDHistogram3);
            //output.Add("VWAP", VWAP);
            output.Add("volume", totalVolume);
            //output.Add("SMACount", candle30.Count());
            return output;
        }
    }
}
