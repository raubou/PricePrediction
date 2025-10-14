using System.Text.Json.Nodes;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TMDInvestments.Services;
using Coinbase.AdvancedTrade.Models.Public;
using TMDInvestments.Models;
using Coinbase.AdvancedTrade.Enums;
using TMD.Services.Service;
using System.Xml.Linq;

namespace TMD.Services
{
    public sealed class IndicesCalculator
    {
        //Generally, when the RSI indicator crosses 30 on the RSI chart,
        //it is a bullish sign and when it crosses 70, it is a bearish sign.
        //Put another way, one can interpret that RSI values of 70 or above
        //indicate that a security is becoming overbought or overvalued.
        //It may be primed for a trend reversal or corrective price pullback.
        //An RSI reading of 30 or below indicates an oversold or undervalued condition.
        //buy at oversold levels, bullish, above 50
        //selling opportunities when the price becomes overbought, bearish, below 50

        //To Do Bollinger Bands Calculation Formulas
        //* Middle Band = 20 - day simple moving average(SMA)
        //* Upper Band = 20 - day SMA + (20 - day standard deviation of price x 2) 
        //*Lower Band = 20 - day SMA - (20 - day standard deviation of price x 2)

        //Exponential Moving Average (EMA)
        //EMA = (K x(C - P)) +P
        //Where:
        //C = Current Price
        //P = Previous periods EMA(A SMA is used for the first periods calculations)
        //K = Exponential smoothing constant usually 2

        //12-20-2024
        //EMA = (Closing Price x Multiplier) + [Previous Day EMA x (1 - Multiplier)]

        //Use the EMA to determine trend direction, and trade in that direction.
        //When the EMA rises, you may want to consider buying when prices dip near or just below the EMA.
        //When the EMA falls, you may consider selling when prices rally towards or just above the EMA.

        //12/22/2024
        //future
        //To calculate a 5-period WMA where the most recent price has a weight of 5, the previous price has a weight of 4, and so on, the formula would look like this:
        //WMA = (P1 * 5 + P2 * 4 + P3 * 3 + P4 * 2 + P5 * 1) / (5 + 4 + 3 + 2 + 1)
        //Where P1 represents the most recent price, P2 the price from the period before that, and so on.

        //12/22/2024
        //future
        //Stochastic Oscillator Calculation
        //The stochastic oscillator is calculated using the following formulas:
        //%K = (Closing Price - Lowest Price in n Periods) / (Highest Price in n Periods - Lowest Price in n Periods) * 100.
        //A reading above 80 suggests overbought, while a reading below 20 suggests oversold.

        ILogger? _log = null;
        SchwabSettings? _settings = null;
        CoinBaseSettings? _coinbaseSettings = null;
        ETradeSettings? _eTradeSettings = null;
        IHttpClientFactory? _factory = null;
        SchwabServices? services;
        CoinBaseService? coinbaseService;
        ETradeService? _eTradeService;
        Polygon? _polygon;
        public IndicesCalculator(ILogger log, SchwabSettings settings, Polygon polygon, IHttpClientFactory factory) {
            _log = log;
            _settings = settings;
            _factory = factory;
            _polygon = polygon;
        }
        public IndicesCalculator(ILogger log, CoinBaseSettings coinbaseSettings, IHttpClientFactory factory)
        {
            _log = log;
            _coinbaseSettings = coinbaseSettings;
            _factory = factory;
        }
        public IndicesCalculator(ILogger log, ETradeSettings eTradeSettings, IHttpClientFactory factory)
        {
            _log = log;
            _eTradeSettings = eTradeSettings;
            _factory = factory;
        }

        public dynamic CalculateV2(PublicProduct product)
        {
            coinbaseService = new CoinBaseService(_log, _coinbaseSettings, _factory);
            var currentPrice = Convert.ToDecimal(product.Price);
            List<Coinbase.AdvancedTrade.Models.Candle> candle90 = coinbaseService.GetPriceHistory(product.ProductId, DateTime.Now.AddDays(-90), DateTime.Now, Granularity.ONE_DAY).OrderByDescending(x => x.StartDate).ToList();
            List<Coinbase.AdvancedTrade.Models.Candle> candleOneHour = coinbaseService.GetPriceHistory(product.ProductId, DateTime.Now.AddDays(-1), DateTime.Now, Granularity.ONE_HOUR).OrderByDescending(x => x.StartDate).ToList();
            //var candle60 = candle90.Take(60).ToList();
            var candle30 = candle90.Take(30).ToList();
            var candle26 = candle90.Take(26).ToList();
            var candle14 = candle90.Take(14).ToList();
            var candle12 = candle90.Take(12).ToList();
            var candle9 = candle90.Take(9).ToList();
            var candle3 = candle90.Take(3).ToList();
            var candle10 = candle90.Take(10).ToList();
            var candle16 = candle90.Take(16).ToList();
            var candle5 = candle90.Take(5).ToList();
            var candle13 = candle90.Take(13).ToList();
            

            var candle30LastFirst = candle30.OrderBy(x => x.StartDate).ToList();
            var candle26LastFirst = candle26.OrderBy(x => x.StartDate).ToList();
            var candle14LastFirst = candle14.OrderBy(x => x.StartDate).ToList();
            var candle12LastFirst = candle12.OrderBy(x => x.StartDate).ToList();
            var candle9LastFirst = candle9.OrderBy(x => x.StartDate).ToList();
            var candle3LastFirst = candle3.OrderBy(x => x.StartDate).ToList();
            var candle10LastFirst = candle10.OrderBy(x => x.StartDate).ToList();
            var candle16LastFirst = candle16.OrderBy(x => x.StartDate).ToList();
            var candle5LastFirst = candle5.OrderBy(x => x.StartDate).ToList();
            var candle13LastFirst = candle13.OrderBy(x => x.StartDate).ToList();
            var candleOneHourLastFirst = candleOneHour.OrderBy(x => x.StartDate).ToList();

            decimal WMA30 = 0m;
            decimal WMA26 = 0m;
            decimal WMA12 = 0m;
            decimal WMA9 = 0m;
            decimal WMA3 = 0m;
            decimal WMA10 = 0m;
            decimal WMA16 = 0m;
            decimal WMA5 = 0m;
            decimal WMA13 = 0m;

            decimal wmaPre30 = 0m;
            decimal wmaPre26 = 0m;
            decimal wmaPre12 = 0m;
            decimal wmaPre9 = 0m;
            decimal wmaPre3 = 0m;
            decimal wmaPre10 = 0m;
            decimal wmaPre16 = 0m;
            decimal wmaPre5 = 0m;
            decimal wmaPre13 = 0m;

            decimal N30 = 0m;
            decimal N26 = 0m;
            decimal N12 = 0m;
            decimal N9 = 0m;
            decimal N3 = 0m;
            decimal N10 = 0m;
            decimal N16 = 0m;
            decimal N5 = 0m;
            decimal N13 = 0m;

            decimal P30 = 0m;
            decimal P26 = 0m;
            decimal P12 = 0m;
            decimal P9 = 0m;
            decimal P3 = 0m;
            decimal P10 = 0m;
            decimal P16 = 0m;
            decimal P5 = 0m;
            decimal P13 = 0m;

            decimal SMA30 = 0m;
            decimal SMA26 = 0m;
            decimal SMA12 = 0m;
            decimal SMA9 = 0m;
            decimal SMA3 = 0m;
            decimal SMA10 = 0m;
            decimal SMA16 = 0m;
            decimal SMA5 = 0m;
            decimal SMA13 = 0m;

            decimal smaPre30 = 0m;
            decimal smaPre26 = 0m;
            decimal smaPre12 = 0m;
            decimal smaPre9 = 0m;
            decimal smaPre3 = 0m;
            decimal smaPre10 = 0m;
            decimal smaPre16 = 0m;
            decimal smaPre5 = 0m;
            decimal smaPre13 = 0m;

            decimal FC30 = candle30LastFirst.Count;
            decimal FC26 = candle26LastFirst.Count;
            decimal FC12 = candle12LastFirst.Count;
            decimal FC9 = candle9LastFirst.Count;
            decimal FC3 = candle3LastFirst.Count;
            decimal FC10 = candle10LastFirst.Count;
            decimal FC16 = candle16LastFirst.Count;
            decimal FC5 = candle5LastFirst.Count;
            decimal FC13 = candle13LastFirst.Count;

            decimal VWAP = 0;

            decimal EMA30 = 0m;
            decimal EMA26 = 0m;
            decimal EMA12 = 0m;
            decimal EMA9 = 0m;
            decimal EMA3 = 0m;
            decimal EMA10 = 0m;
            decimal EMA16 = 0m;
            decimal EMA5 = 0m;
            decimal EMA13 = 0m;

            decimal smoothingFactor30 = (2m / ((decimal)candle30LastFirst.Count + 1m));
            decimal smoothingFactor26 = (2m / ((decimal)candle26LastFirst.Count + 1m));
            decimal smoothingFactor12 = (2m / ((decimal)candle12LastFirst.Count + 1m));
            decimal smoothingFactor9 = (2m / ((decimal)candle9LastFirst.Count + 1m));
            decimal smoothingFactor3 = (2m / ((decimal)candle3LastFirst.Count + 1m));
            decimal smoothingFactor10 = (2m / ((decimal)candle10LastFirst.Count + 1m));
            decimal smoothingFactor16 = (2m / ((decimal)candle16LastFirst.Count + 1m));
            decimal smoothingFactor5 = (2m / ((decimal)candle5LastFirst.Count + 1m));
            decimal smoothingFactor13 = (2m / ((decimal)candle13LastFirst.Count + 1m));

            decimal MACD = 0m;
            decimal MACDSignal = 0m;
            decimal MACDHistogram = 0m;
            decimal MACDDD = 0m;
            decimal MACDSignalDD = 0m;
            decimal MACDHistogramDD = 0m;
            decimal MACDDD2 = 0m;
            decimal MACDSignalDD2 = 0m;
            decimal MACDHistogramDD2 = 0m;

            decimal RSI14 = 0m;
            decimal RSIOneHour = 0m;
            decimal close = 0m;
            decimal avgUp = 0m;
            decimal avgDown = 0m;
            decimal avgUpTotal = 0m;
            decimal avgDownTotal = 0m;
            decimal volume = 0m;

            for (int i = 0; i < candle30LastFirst.Count; i++)
            {
                N30 += (i + 1);
            }
            for (int i = 0; i < candle26LastFirst.Count; i++)
            {
                N26 += (i + 1);
            }
            for (int i = 0; i < candle12LastFirst.Count; i++)
            {
                N12 += (i + 1);
            }
            for (int i = 0; i < candle9LastFirst.Count; i++)
            {
                N9 += (i + 1);
            }
            for (int i = 0; i < candle3LastFirst.Count; i++)
            {
                N3 += (i + 1);
            }
            for (int i = 0; i < candle10LastFirst.Count; i++)
            {
                N10 += (i + 1);
            }
            for (int i = 0; i < candle16LastFirst.Count; i++)
            {
                N16 += (i + 1);
            }
            for (int i = 0; i < candle5LastFirst.Count; i++)
            {
                N5 += (i + 1);
            }
            for (int i = 0; i < candle13LastFirst.Count; i++)
            {
                N13 += (i + 1);
            }
            for (int i = 0; i < candle30LastFirst.Count; i++)
            {
                decimal wmmP30 = Convert.ToDecimal(candle30LastFirst[i].Close);
                P30 = (wmmP30 * (i + 1));
                wmaPre30 += P30;
                decimal smaP30 = Convert.ToDecimal(candle30LastFirst[i].Close);
                smaPre30 += smaP30;
            }
            for (int i = 0; i < candle26LastFirst.Count; i++)
            {
                decimal wmmP26 = Convert.ToDecimal(candle26LastFirst[i].Close);
                P26 = (wmmP26 * (i + 1));
                wmaPre26 += P26;
                decimal smaP26 = Convert.ToDecimal(candle26LastFirst[i].Close);
                smaPre26 += smaP26;
            }
            for (int i = 0; i < candle12LastFirst.Count; i++)
            {
                decimal wmmP12 = Convert.ToDecimal(candle12LastFirst[i].Close);
                P12 = (wmmP12 * (i + 1));
                wmaPre12 += P12;
                decimal smaP12 = Convert.ToDecimal(candle12LastFirst[i].Close);
                smaPre12 += smaP12;
            }
            for (int i = 0; i < candle9LastFirst.Count; i++)
            {
                decimal wmmP9 = Convert.ToDecimal(candle9LastFirst[i].Close);
                P9 = (wmmP9 * (i + 1));
                wmaPre9 += P9;
                decimal smaP9 = Convert.ToDecimal(candle9LastFirst[i].Close);
                smaPre9 += smaP9;
            }
            for (int i = 0; i < candle3LastFirst.Count; i++)
            {
                decimal wmmP3 = Convert.ToDecimal(candle3LastFirst[i].Close);
                P3 = (wmmP3 * (i + 1));
                wmaPre3 += P3;
                decimal smaP3 = Convert.ToDecimal(candle3LastFirst[i].Close);
                smaPre3 += smaP3;
            }
            for (int i = 0; i < candle10LastFirst.Count; i++)
            {
                decimal wmmP10 = Convert.ToDecimal(candle10LastFirst[i].Close);
                P10 = (wmmP10 * (i + 1));
                wmaPre10 += P10;
                decimal smaP10 = Convert.ToDecimal(candle10LastFirst[i].Close);
                smaPre10 += smaP10;
            }
            for (int i = 0; i < candle16LastFirst.Count; i++)
            {
                decimal wmmP16 = Convert.ToDecimal(candle16LastFirst[i].Close);
                P16 = (wmmP16 * (i + 1));
                wmaPre16 += P16;
                decimal smaP16 = Convert.ToDecimal(candle16LastFirst[i].Close);
                smaPre16 += smaP16;
            }
            for (int i = 0; i < candle5LastFirst.Count; i++)
            {
                decimal wmmP5 = Convert.ToDecimal(candle5LastFirst[i].Close);
                P5 = (wmmP5 * (i + 1));
                wmaPre5 += P5;
                decimal smaP5 = Convert.ToDecimal(candle5LastFirst[i].Close);
                smaPre5 += smaP5;
            }
            for (int i = 0; i < candle13LastFirst.Count; i++)
            {
                decimal wmmP13 = Convert.ToDecimal(candle13LastFirst[i].Close);
                P13 = (wmmP13 * (i + 1));
                wmaPre13 += P13;
                decimal smaP13 = Convert.ToDecimal(candle13LastFirst[i].Close);
                smaPre13 += smaP13;
            }
            foreach (var item in candle14LastFirst)
            {
                if (close > 0)
                    if (close > Convert.ToDecimal(item.Close))
                    {
                        //avgDown += Convert.ToDecimal(item.Close);
                        avgDown += Math.Abs(((close - Convert.ToDecimal(item.Close)) / ((close + Convert.ToDecimal(item.Close)) / 2)) * 100);
                        avgDownTotal++;
                    }
                    else
                    {
                        //avgUp += Convert.ToDecimal(item.Close);
                        avgUp += Math.Abs(((close - Convert.ToDecimal(item.Close)) / ((close + Convert.ToDecimal(item.Close)) / 2)) * 100);
                        avgUpTotal++;
                    }
                close = Convert.ToDecimal(item.Close);
            }
            try
            {
                avgDown = (avgDown / avgDownTotal);
                avgUp = (avgUp / avgUpTotal);
                RSI14 = 100 - 100 / (1 + (avgUp / avgDown));
            }
            catch (Exception ex)
            {
                _log.LogError($"{ex.Message}. Indicies Calculator RSI");
            }
            close = 0;
            avgUp = 0;
            avgDown = 0;
            avgUpTotal = 0;
            avgDownTotal = 0;
            foreach (var item in candleOneHourLastFirst)
            {
                if (close > 0)
                    if (close > Convert.ToDecimal(item.Close))
                    {
                        //avgDown += Convert.ToDecimal(item.Close);
                        avgDown += Math.Abs(((close - Convert.ToDecimal(item.Close)) / ((close + Convert.ToDecimal(item.Close)) / 2)) * 100);
                        avgDownTotal++;
                    }
                    else
                    {
                        //avgUp += Convert.ToDecimal(item.Close);
                        avgUp += Math.Abs(((close - Convert.ToDecimal(item.Close)) / ((close + Convert.ToDecimal(item.Close)) / 2)) * 100);
                        avgUpTotal++;
                    }
                close = Convert.ToDecimal(item.Close);
            }
            try
            {
                avgDown = (avgDown / avgDownTotal);
                avgUp = (avgUp / avgUpTotal);
                RSIOneHour = 100 - 100 / (1 + (avgUp / avgDown));
            }
            catch (Exception ex)
            {
                _log.LogError($"{ex.Message}. Indicies Calculator RSI");
            }
            try
            {
                WMA30 = (wmaPre30 / N30);
                WMA26 = (wmaPre26 / N26);
                WMA12 = (wmaPre12 / N12);
                WMA9 = (wmaPre9 / N9);
                WMA3 = (wmaPre3 / N3);
                WMA10 = (wmaPre10 / N10);
                WMA16 = (wmaPre16 / N16);
                WMA5 = (wmaPre5 / N5);
                WMA13 = (wmaPre13 / N13);

                SMA30 = (smaPre30 / FC30);
                SMA26 = (smaPre26 / FC26);
                SMA12 = (smaPre12 / FC12);
                SMA9 = (smaPre9 / FC9);
                SMA3 = (smaPre3 / FC3);
                SMA10 = (smaPre10 / FC10);
                SMA16 = (smaPre16 / FC16);
                SMA5 = (smaPre5 / FC5);
                SMA13 = (smaPre13 / FC13);
            }
            catch (Exception ex)
            {
                _log.LogError($"{ex.Message}. WMA/SMA RSI");
            }

            try
            {   
                VWAP = (Convert.ToDecimal(candle30[0].High) +
                Convert.ToDecimal(candle30[0].Low) +
                Convert.ToDecimal(candle30[0].Close)) / 3;
                volume = Convert.ToDecimal(product.Volume24h);
            }
            catch (Exception ex)
            {
                _log.LogError($"{ex.Message}. VWAP");
            }
            //EMA = (Today's Value * Multiplier) + Yesterday's EMA *(1 - Multiplier)
            //You can then use the SMA from the previous day as the first EMA for yesterday.
            try
            {
                EMA30 = (Convert.ToDecimal(candle30[0].Close) * smoothingFactor30) + (SMA30) * (1 - smoothingFactor30);
                EMA26 = (Convert.ToDecimal(candle26[0].Close) * smoothingFactor26) + (SMA26) * (1 - smoothingFactor26);
                EMA12 = (Convert.ToDecimal(candle12[0].Close) * smoothingFactor12) + (SMA12) * (1 - smoothingFactor12);
                EMA9 = (Convert.ToDecimal(candle9[0].Close) * smoothingFactor9) + (SMA9) * (1 - smoothingFactor9);
                EMA3 = (Convert.ToDecimal(candle3[0].Close) * smoothingFactor3) + (SMA3) * (1 - smoothingFactor3);
                EMA10 = (Convert.ToDecimal(candle10[0].Close) * smoothingFactor10) + (SMA10) * (1 - smoothingFactor10);
                EMA16 = (Convert.ToDecimal(candle16[0].Close) * smoothingFactor16) + (SMA16) * (1 - smoothingFactor16);
                EMA5 = (Convert.ToDecimal(candle5[0].Close) * smoothingFactor5) + (SMA5) * (1 - smoothingFactor5);
                EMA13 = (Convert.ToDecimal(candle13[0].Close) * smoothingFactor13) + (SMA13) * (1 - smoothingFactor13);
            }
            catch (Exception ex)
            {
                _log.LogError($"{ex.Message}. EMA");
            }

            MACD = EMA12 - EMA26;
            MACDSignal = EMA9 - EMA12;
            MACDHistogram = MACD - MACDSignal;
            MACDDD = EMA10 - EMA3;
            MACDSignalDD = EMA16 - EMA10;
            MACDHistogramDD = MACDDD - MACDSignalDD;
            MACDDD2 = EMA13 - EMA5;
            MACDSignalDD2 = EMA9 - EMA13;
            MACDHistogramDD2 = MACDDD2 - MACDSignalDD2;

            JsonObject output = new JsonObject();
            output.Add("Symbol", product.ProductId);
            output.Add("Name", product.BaseName);
            output.Add("lastPrice", currentPrice);
            output.Add("RSI", RSI14);
            output.Add("RSIOneHour", RSIOneHour);
            output.Add("SMA30", SMA30);
            output.Add("EMA30", EMA30);
            output.Add("WMA30", WMA30);
            output.Add("EMA26", EMA26);
            output.Add("EMA12", EMA12);
            output.Add("EMA9", EMA9);
            output.Add("EMA3", EMA3);
            output.Add("EMA10", EMA10);
            output.Add("EMA16", EMA16);
            output.Add("EMA5", EMA5);
            output.Add("EMA13", EMA13);
            output.Add("SMA26", SMA26);
            output.Add("SMA12", SMA12);
            output.Add("SMA9", SMA9);
            output.Add("SMA3", SMA3);
            output.Add("SMA10", SMA10);
            output.Add("SMA16", SMA16);
            output.Add("SMA5", SMA5);
            output.Add("SMA13", SMA13);
            output.Add("WMA26", WMA26);
            output.Add("WMA12", WMA12);
            output.Add("WMA9", WMA9);
            output.Add("WMA3", WMA3);
            output.Add("WMA10", WMA10);
            output.Add("WMA16", WMA16);
            output.Add("WMA5", WMA5);
            output.Add("WMA13", WMA13);
            output.Add("MACD", MACD);
            output.Add("MACDSignal", MACDSignal);
            output.Add("MACDHistogram", MACDHistogram);
            output.Add("MACDDD", MACDDD);
            output.Add("MACDSignalDD", MACDSignalDD);
            output.Add("MACDHistogramDD", MACDHistogramDD);
            output.Add("MACDDD2", MACDDD2);
            output.Add("MACDSignalDD2", MACDSignalDD2);
            output.Add("MACDHistogramDD2", MACDHistogramDD2);
            output.Add("VWAP", VWAP);
            output.Add("volume", volume);
            output.Add("SMACount", candle30.Count());
            return output;
        }
        public dynamic CalculateV2(string symbol, JsonElement quote)
        {
            services = new SchwabServices(_log, _settings, _polygon, _factory);
            decimal currentPrice = 0m;
            decimal totalVolume = 0m;

            try
            {
                currentPrice = Convert.ToDecimal(quote.GetProperty(symbol.ToUpper()).GetProperty("quote").GetProperty("lastPrice").ToString());
                totalVolume = Convert.ToDecimal(quote.GetProperty(symbol.ToUpper()).GetProperty("quote").GetProperty("totalVolume").ToString());

            }
            catch (Exception ex)
            {
                try
                {
                    currentPrice = Convert.ToDecimal(quote.GetProperty("quote").GetProperty("lastPrice").ToString());
                    totalVolume = Convert.ToDecimal(quote.GetProperty("quote").GetProperty("totalVolume").ToString());
                }
                catch (Exception ex2)
                {
                    _log.LogError($"{ex2.Message}", ex2);
                    return null;
                }
            }

            var results = services.GetPriceHistory(symbol, "month", "1", "daily", 1, DateTime.Now.AddDays(-90), DateTime.Now);//.ToList();
            //var resultsDay = services.GetPriceHistory(symbol, "day", "1", "minute", 30, DateTime.Now.AddDays(-90), DateTime.Now);//.ToList();
            //https://api.schwabapi.com/marketdata/v1/pricehistory?symbol=AAPL&periodType=day&period=10&frequencyType=minute&frequency=30
            var resultsDaily = services.GetPriceHistory(symbol, "day", "1", "minute", 30, null, null);
            List<TMDInvestment.Models.CandleItems> candle90 = new List<TMDInvestment.Models.CandleItems>();
            List<TMDInvestment.Models.CandleItems> candleDaily = new List<TMDInvestment.Models.CandleItems>();
            try
            {
                var ser = JsonSerializer.Serialize(results.GetProperty("candles"));
                var ser2 = JsonSerializer.Serialize(resultsDaily.GetProperty("candles"));
                candle90 = JsonSerializer.Deserialize<List<TMDInvestment.Models.CandleItems>>(ser);
                candleDaily = JsonSerializer.Deserialize<List<TMDInvestment.Models.CandleItems>>(ser2);
                candle90 = candle90.OrderByDescending(x => x.datetime).ToList();
                candleDaily = candleDaily.OrderByDescending(x => x.datetime).ToList();
            }
            catch (Exception ex)
            {
                _log.LogError($"{ex.Message}", ex);
            }
            //var candle60 = candle90.Take(60).ToList();
            var candle30 = candle90.Take(30).ToList();
            var candle26 = candle90.Take(26).ToList();
            var candle14 = candle90.Take(14).ToList();
            var candle12 = candle90.Take(12).ToList();
            var candle9 = candle90.Take(9).ToList();
            var candle3 = candle90.Take(3).ToList();
            var candle10 = candle90.Take(10).ToList();
            var candle16 = candle90.Take(16).ToList();
            var candle5 = candle90.Take(5).ToList();
            var candle13 = candle90.Take(13).ToList();

            var candle30LastFirst = candle30.OrderBy(x => x.datetime).ToList();
            var candle26LastFirst = candle26.OrderBy(x => x.datetime).ToList();
            var candle14LastFirst = candle14.OrderBy(x => x.datetime).ToList();
            var candle12LastFirst = candle12.OrderBy(x => x.datetime).ToList();
            var candle9LastFirst = candle9.OrderBy(x => x.datetime).ToList();
            var candle3LastFirst = candle3.OrderBy(x => x.datetime).ToList();
            var candle10LastFirst = candle10.OrderBy(x => x.datetime).ToList();
            var candle16LastFirst = candle16.OrderBy(x => x.datetime).ToList();
            var candle5LastFirst = candle5.OrderBy(x => x.datetime).ToList();
            var candle13LastFirst = candle13.OrderBy(x => x.datetime).ToList();
            var candleOneHourLastFirst = candleDaily.OrderBy(x => x.datetime).ToList();

            decimal WMA30 = 0m;
            decimal WMA26 = 0m;
            decimal WMA12 = 0m;
            decimal WMA9 = 0m;
            decimal WMA3 = 0m;
            decimal WMA10 = 0m;
            decimal WMA16 = 0m;
            decimal WMA5 = 0m;
            decimal WMA13 = 0m;

            decimal wmaPre30 = 0m;
            decimal wmaPre26 = 0m;
            decimal wmaPre12 = 0m;
            decimal wmaPre9 = 0m;
            decimal wmaPre3 = 0m;
            decimal wmaPre10 = 0m;
            decimal wmaPre16 = 0m;
            decimal wmaPre5 = 0m;
            decimal wmaPre13 = 0m;

            decimal N30 = 0m;
            decimal N26 = 0m;
            decimal N12 = 0m;
            decimal N9 = 0m;
            decimal N3 = 0m;
            decimal N10 = 0m;
            decimal N16 = 0m;
            decimal N5 = 0m;
            decimal N13 = 0m;

            decimal P30 = 0m;
            decimal P26 = 0m;
            decimal P12 = 0m;
            decimal P9 = 0m;
            decimal P3 = 0m;
            decimal P10 = 0m;
            decimal P16 = 0m;
            decimal P5 = 0m;
            decimal P13 = 0m;

            decimal SMA30 = 0m;
            decimal SMA26 = 0m;
            decimal SMA12 = 0m;
            decimal SMA9 = 0m;
            decimal SMA3 = 0m;
            decimal SMA10 = 0m;
            decimal SMA16 = 0m;
            decimal SMA5 = 0m;
            decimal SMA13 = 0m;

            decimal smaPre30 = 0m;
            decimal smaPre26 = 0m;
            decimal smaPre12 = 0m;
            decimal smaPre9 = 0m;
            decimal smaPre3 = 0m;
            decimal smaPre10 = 0m;
            decimal smaPre16 = 0m;
            decimal smaPre5 = 0m;
            decimal smaPre13 = 0m;

            decimal FC30 = candle30LastFirst.Count;
            decimal FC26 = candle26LastFirst.Count;
            decimal FC12 = candle12LastFirst.Count;
            decimal FC9 = candle9LastFirst.Count;
            decimal FC3 = candle3LastFirst.Count;
            decimal FC10 = candle10LastFirst.Count;
            decimal FC16 = candle16LastFirst.Count;
            decimal FC5 = candle5LastFirst.Count;
            decimal FC13 = candle13LastFirst.Count;

            decimal VWAP = 0;

            decimal EMA30 = 0m;
            decimal EMA26 = 0m;
            decimal EMA12 = 0m;
            decimal EMA9 = 0m;
            decimal EMA3 = 0m;
            decimal EMA10 = 0m;
            decimal EMA16 = 0m;
            decimal EMA5 = 0m;
            decimal EMA13 = 0m;

            decimal smoothingFactor30 = (2m / ((decimal)candle30LastFirst.Count + 1m));
            decimal smoothingFactor26 = (2m / ((decimal)candle26LastFirst.Count + 1m));
            decimal smoothingFactor12 = (2m / ((decimal)candle12LastFirst.Count + 1m));
            decimal smoothingFactor9 = (2m / ((decimal)candle9LastFirst.Count + 1m));
            decimal smoothingFactor3 = (2m / ((decimal)candle3LastFirst.Count + 1m));
            decimal smoothingFactor10 = (2m / ((decimal)candle10LastFirst.Count + 1m));
            decimal smoothingFactor16 = (2m / ((decimal)candle16LastFirst.Count + 1m));
            decimal smoothingFactor5 = (2m / ((decimal)candle5LastFirst.Count + 1m));
            decimal smoothingFactor13 = (2m / ((decimal)candle13LastFirst.Count + 1m));

            decimal MACD = 0m;
            decimal MACDSignal = 0m;
            decimal MACDHistogram = 0m;
            decimal MACDDD = 0m;
            decimal MACDSignalDD = 0m;
            decimal MACDHistogramDD = 0m;
            decimal MACDDD2 = 0m;
            decimal MACDSignalDD2 = 0m;
            decimal MACDHistogramDD2 = 0m;

            decimal RSI14 = 0m;
            decimal RSIOneHour = 0m;
            decimal close = 0m;
            decimal avgUp = 0m;
            decimal avgDown = 0m;
            decimal avgUpTotal = 0m;
            decimal avgDownTotal = 0m;
            decimal volume = 0m;

            for (int i = 0; i < candle30LastFirst.Count; i++)
            {
                N30 += (i + 1);
            }
            for (int i = 0; i < candle26LastFirst.Count; i++)
            {
                N26 += (i + 1);
            }
            for (int i = 0; i < candle12LastFirst.Count; i++)
            {
                N12 += (i + 1);
            }
            for (int i = 0; i < candle9LastFirst.Count; i++)
            {
                N9 += (i + 1);
            }
            for (int i = 0; i < candle3LastFirst.Count; i++)
            {
                N3 += (i + 1);
            }
            for (int i = 0; i < candle10LastFirst.Count; i++)
            {
                N10 += (i + 1);
            }
            for (int i = 0; i < candle16LastFirst.Count; i++)
            {
                N16 += (i + 1);
            }
            for (int i = 0; i < candle5LastFirst.Count; i++)
            {
                N5 += (i + 1);
            }
            for (int i = 0; i < candle13LastFirst.Count; i++)
            {
                N13 += (i + 1);
            }
            for (int i = 0; i < candle30LastFirst.Count; i++)
            {
                decimal wmmP30 = Convert.ToDecimal(candle30LastFirst[i].close);
                P30 = (wmmP30 * (i + 1));
                wmaPre30 += P30;
                decimal smaP30 = Convert.ToDecimal(candle30LastFirst[i].close);
                smaPre30 += smaP30;
            }
            for (int i = 0; i < candle26LastFirst.Count; i++)
            {
                decimal wmmP26 = Convert.ToDecimal(candle26LastFirst[i].close);
                P26 = (wmmP26 * (i + 1));
                wmaPre26 += P26;
                decimal smaP26 = Convert.ToDecimal(candle26LastFirst[i].close);
                smaPre26 += smaP26;
            }
            for (int i = 0; i < candle12LastFirst.Count; i++)
            {
                decimal wmmP12 = Convert.ToDecimal(candle12LastFirst[i].close);
                P12 = (wmmP12 * (i + 1));
                wmaPre12 += P12;
                decimal smaP12 = Convert.ToDecimal(candle12LastFirst[i].close);
                smaPre12 += smaP12;
            }
            for (int i = 0; i < candle9LastFirst.Count; i++)
            {
                decimal wmmP9 = Convert.ToDecimal(candle9LastFirst[i].close);
                P9 = (wmmP9 * (i + 1));
                wmaPre9 += P9;
                decimal smaP9 = Convert.ToDecimal(candle9LastFirst[i].close);
                smaPre9 += smaP9;
            }
            for (int i = 0; i < candle3LastFirst.Count; i++)
            {
                decimal wmmP3 = Convert.ToDecimal(candle3LastFirst[i].close);
                P3 = (wmmP3 * (i + 1));
                wmaPre3 += P3;
                decimal smaP3 = Convert.ToDecimal(candle3LastFirst[i].close);
                smaPre3 += smaP3;
            }
            for (int i = 0; i < candle10LastFirst.Count; i++)
            {
                decimal wmmP10 = Convert.ToDecimal(candle10LastFirst[i].close);
                P10 = (wmmP10 * (i + 1));
                wmaPre10 += P10;
                decimal smaP10 = Convert.ToDecimal(candle10LastFirst[i].close);
                smaPre10 += smaP10;
            }
            for (int i = 0; i < candle16LastFirst.Count; i++)
            {
                decimal wmmP16 = Convert.ToDecimal(candle16LastFirst[i].close);
                P16 = (wmmP16 * (i + 1));
                wmaPre16 += P16;
                decimal smaP16 = Convert.ToDecimal(candle16LastFirst[i].close);
                smaPre16 += smaP16;
            }
            for (int i = 0; i < candle5LastFirst.Count; i++)
            {
                decimal wmmP5 = Convert.ToDecimal(candle5LastFirst[i].close);
                P5 = (wmmP5 * (i + 1));
                wmaPre5 += P5;
                decimal smaP5 = Convert.ToDecimal(candle5LastFirst[i].close);
                smaPre5 += smaP5;
            }
            for (int i = 0; i < candle13LastFirst.Count; i++)
            {
                decimal wmmP13 = Convert.ToDecimal(candle13LastFirst[i].close);
                P13 = (wmmP13 * (i + 1));
                wmaPre13 += P13;
                decimal smaP13 = Convert.ToDecimal(candle13LastFirst[i].close);
                smaPre13 += smaP13;
            }
            foreach (var item in candle14LastFirst)
            {
                if (close > 0)
                    if (close > Convert.ToDecimal(item.close))
                    {
                        //avgDown += Convert.ToDecimal(item.close);
                        avgDown += Math.Abs(((close - Convert.ToDecimal(item.close)) / ((close + Convert.ToDecimal(item.close)) / 2)) * 100);
                        avgDownTotal++;
                    }
                    else
                    {
                        //avgUp += Convert.ToDecimal(item.close);
                        avgUp += Math.Abs(((close - Convert.ToDecimal(item.close)) / ((close + Convert.ToDecimal(item.close)) / 2)) * 100);
                        avgUpTotal++;
                    }
                close = Convert.ToDecimal(item.close);
            }
            try
            {
                avgDown = (avgDown / avgDownTotal);
                avgUp = (avgUp / avgUpTotal);
                RSI14 = 100 - 100 / (1 + (avgUp / avgDown));
            }
            catch (Exception ex)
            {
                _log.LogError($"{ex.Message}. Indicies Calculator RSI");
            }
            close = 0;
            avgUp = 0;
            avgDown = 0;
            avgUpTotal = 0;
            avgDownTotal = 0;
            foreach (var item in candleOneHourLastFirst)
            {
                if (close > 0)
                    if (close > Convert.ToDecimal(item.close))
                    {
                        //avgDown += Convert.ToDecimal(item.Close);
                        avgDown += Math.Abs(((close - Convert.ToDecimal(item.close)) / ((close + Convert.ToDecimal(item.close)) / 2)) * 100);
                        avgDownTotal++;
                    }
                    else
                    {
                        //avgUp += Convert.ToDecimal(item.Close);
                        avgUp += Math.Abs(((close - Convert.ToDecimal(item.close)) / ((close + Convert.ToDecimal(item.close)) / 2)) * 100);
                        avgUpTotal++;
                    }
                close = Convert.ToDecimal(item.close);
            }
            try
            {
                avgDown = (avgDown / avgDownTotal);
                avgUp = (avgUp / avgUpTotal);
                RSIOneHour = 100 - 100 / (1 + (avgUp / avgDown));
            }
            catch (Exception ex)
            {
                _log.LogError($"{ex.Message}. Indicies Calculator RSI");
            }
            try
            {
                WMA30 = (wmaPre30 / N30);
                WMA26 = (wmaPre26 / N26);
                WMA12 = (wmaPre12 / N12);
                WMA9 = (wmaPre9 / N9);
                WMA3 = (wmaPre3 / N3);
                WMA10 = (wmaPre10 / N10);
                WMA16 = (wmaPre16 / N16);
                WMA5 = (wmaPre5 / N5);
                WMA13 = (wmaPre13 / N13);

                SMA30 = (smaPre30 / FC30);
                SMA26 = (smaPre26 / FC26);
                SMA12 = (smaPre12 / FC12);
                SMA9 = (smaPre9 / FC9);
                SMA3 = (smaPre3 / FC3);
                SMA10 = (smaPre10 / FC10);
                SMA16 = (smaPre16 / FC16);
                SMA5 = (smaPre5 / FC5);
                SMA13 = (smaPre13 / FC13);
            }
            catch (Exception ex)
            {
                _log.LogError($"{ex.Message}. WMA/SMA RSI");
            }

            try
            {
                VWAP = (Convert.ToDecimal(quote.GetProperty(symbol.ToUpper()).GetProperty("quote").GetProperty("highPrice").ToString()) +
                Convert.ToDecimal(quote.GetProperty(symbol.ToUpper()).GetProperty("quote").GetProperty("lowPrice").ToString()) +
                Convert.ToDecimal(quote.GetProperty(symbol.ToUpper()).GetProperty("quote").GetProperty("closePrice").ToString())) / 3;


            }
            catch (Exception ex)
            {
                try
                {
                    VWAP = (Convert.ToDecimal(quote.GetProperty("quote").GetProperty("highPrice").ToString()) +
                    Convert.ToDecimal(quote.GetProperty("quote").GetProperty("lowPrice").ToString()) +
                    Convert.ToDecimal(quote.GetProperty("quote").GetProperty("closePrice").ToString())) / 3;
                }
                catch (Exception ex2)
                {
                    _log.LogError($"{ex2.Message}", ex2);
                    //return null;
                }
            }
            try
            {
                volume = Convert.ToInt64(quote.GetProperty(symbol.ToUpper()).GetProperty("quote").GetProperty("totalVolume").ToString());
            }
            catch (Exception ex)
            {
                try
                {
                    volume = Convert.ToInt64(quote.GetProperty("quote").GetProperty("totalVolume").ToString());
                }
                catch (Exception ex2)
                {
                    _log.LogError($"{ex2.Message}", ex2);
                    //return null
                }
            }
            //EMA = (Today's Value * Multiplier) + Yesterday's EMA *(1 - Multiplier)
            //You can then use the SMA from the previous day as the first EMA for yesterday.
            try
            {
                EMA30 = (Convert.ToDecimal(candle30[0].close) * smoothingFactor30) + (SMA30) * (1 - smoothingFactor30);
                EMA26 = (Convert.ToDecimal(candle26[0].close) * smoothingFactor26) + (SMA26) * (1 - smoothingFactor26);
                EMA12 = (Convert.ToDecimal(candle12[0].close) * smoothingFactor12) + (SMA12) * (1 - smoothingFactor12);
                EMA9 = (Convert.ToDecimal(candle9[0].close) * smoothingFactor9) + (SMA9) * (1 - smoothingFactor9);
                EMA3 = (Convert.ToDecimal(candle3[0].close) * smoothingFactor3) + (SMA3) * (1 - smoothingFactor3);
                EMA10 = (Convert.ToDecimal(candle10[0].close) * smoothingFactor10) + (SMA10) * (1 - smoothingFactor10);
                EMA16 = (Convert.ToDecimal(candle16[0].close) * smoothingFactor16) + (SMA16) * (1 - smoothingFactor16);
                EMA5 = (Convert.ToDecimal(candle5[0].close) * smoothingFactor5) + (SMA5) * (1 - smoothingFactor5);
                EMA13 = (Convert.ToDecimal(candle13[0].close) * smoothingFactor13) + (SMA13) * (1 - smoothingFactor13);
            }
            catch (Exception ex)
            {
                _log.LogError($"{ex.Message}. EMA");
            }

            MACD = EMA12 - EMA26;
            MACDSignal = EMA9 - EMA12;
            MACDHistogram = MACD - MACDSignal;
            MACDDD = EMA10 - EMA3;
            MACDSignalDD = EMA16 - EMA10;
            MACDHistogramDD = MACDDD - MACDSignalDD;
            MACDDD2 = EMA13 - EMA5;
            MACDSignalDD2 = EMA9 - EMA13;
            MACDHistogramDD2 = MACDDD2 - MACDSignalDD2;

            var resultsJson = JsonSerializer.Deserialize<JsonNode>(JsonSerializer.Serialize(quote));

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

            output.Add("lastPrice", currentPrice);
            output.Add("results", resultsJson);
            output.Add("RSI", RSI14);
            output.Add("RSIOneHour", RSIOneHour);
            output.Add("SMA30", SMA30);
            output.Add("EMA30", EMA30);
            output.Add("WMA30", WMA30);
            output.Add("EMA26", EMA26);
            output.Add("EMA12", EMA12);
            output.Add("EMA9", EMA9);
            output.Add("EMA3", EMA3);
            output.Add("EMA10", EMA10);
            output.Add("EMA16", EMA16);
            output.Add("EMA5", EMA5);
            output.Add("EMA13", EMA13);
            output.Add("SMA26", SMA26);
            output.Add("SMA12", SMA12);
            output.Add("SMA9", SMA9);
            output.Add("SMA3", SMA3);
            output.Add("SMA10", SMA10);
            output.Add("SMA16", SMA16);
            output.Add("SMA5", SMA5);
            output.Add("SMA13", SMA13);
            output.Add("WMA26", WMA26);
            output.Add("WMA12", WMA12);
            output.Add("WMA9", WMA9);
            output.Add("WMA3", WMA3);
            output.Add("WMA10", WMA10);
            output.Add("WMA16", WMA16);
            output.Add("WMA5", WMA5);
            output.Add("WMA13", WMA13);
            output.Add("MACD", MACD);
            output.Add("MACDSignal", MACDSignal);
            output.Add("MACDHistogram", MACDHistogram);
            output.Add("MACDDD", MACDDD);
            output.Add("MACDSignalDD", MACDSignalDD);
            output.Add("MACDHistogramDD", MACDHistogramDD);
            output.Add("MACDDD2", MACDDD2);
            output.Add("MACDSignalDD2", MACDSignalDD2);
            output.Add("MACDHistogramDD2", MACDHistogramDD2);
            output.Add("VWAP", VWAP);
            output.Add("volume", volume);
            output.Add("SMACount", candle30.Count());
            return output;
        }
        //public dynamic CalculateV2(string symbol, XElement quote)
        //{
        //    services = new SchwabServices(_log, _settings, _factory);
        //    decimal? currPrice = null;
        //    decimal currentPrice = 0m;
        //    symbol = (symbol.ToLower().Contains("vix") && !symbol.ToLower().Contains("$")) ? "$" + symbol : symbol;
        //    symbol = (symbol.ToLower().Contains("spx") && !symbol.ToLower().Contains("$")) ? "$" + symbol : symbol;
        //    try
        //    {
        //        currPrice = Convert.ToDecimal(quote?.Element("QuoteData")?.Element("All")?.Element("lastTrade")?.Value?.ToString());
        //        if (currPrice.Value <= 0)
        //        {
        //            return quote;
        //        }
        //        currentPrice = currPrice.Value;
        //    }
        //    catch (Exception)
        //    {
        //        var checkError = quote.Element("Messages").Value.ToString();
        //        if (checkError != null && !string.IsNullOrEmpty(checkError))
        //        {
        //            return quote;
        //        }
        //    }
        //    var results = services.GetPriceHistory(symbol, "month", "1", "daily", 1, DateTime.Now.AddDays(-90), DateTime.Now);//.ToList();
        //    //https://api.schwabapi.com/marketdata/v1/pricehistory?symbol=AAPL&periodType=day&period=10&frequencyType=minute&frequency=30
        //    var resultsDaily = services.GetPriceHistory(symbol, "day", "1", "minute", 30, null, null);
        //    List<TMDInvestment.Models.CandleItems> candle90 = new List<TMDInvestment.Models.CandleItems>();
        //    List<TMDInvestment.Models.CandleItems> candleDaily = new List<TMDInvestment.Models.CandleItems>();
        //    try
        //    {
        //        var ser = JsonSerializer.Serialize(results.GetProperty("candles"));
        //        var ser2 = JsonSerializer.Serialize(resultsDaily.GetProperty("candles"));
        //        candle90 = JsonSerializer.Deserialize<List<TMDInvestment.Models.CandleItems>>(ser);
        //        candleDaily = JsonSerializer.Deserialize<List<TMDInvestment.Models.CandleItems>>(ser2);
        //        candle90 = candle90.OrderByDescending(x => x.datetime).ToList();
        //        candleDaily = candleDaily.OrderByDescending(x => x.datetime).ToList();
        //    }
        //    catch (Exception ex)
        //    {
        //        _log.LogError($"{ex.Message}", ex);
        //    }
        //    //var candle60 = candle90.Take(60).ToList();
        //    var candle30 = candle90.Take(30).ToList();
        //    var candle26 = candle90.Take(26).ToList();
        //    var candle14 = candle90.Take(14).ToList();
        //    var candle12 = candle90.Take(12).ToList();
        //    var candle9 = candle90.Take(9).ToList();
        //    var candle3 = candle90.Take(3).ToList();
        //    var candle10 = candle90.Take(10).ToList();
        //    var candle16 = candle90.Take(16).ToList();
        //    var candle5 = candle90.Take(5).ToList();
        //    var candle13 = candle90.Take(13).ToList();

        //    var candle30LastFirst = candle30.OrderBy(x => x.datetime).ToList();
        //    var candle26LastFirst = candle26.OrderBy(x => x.datetime).ToList();
        //    var candle14LastFirst = candle14.OrderBy(x => x.datetime).ToList();
        //    var candle12LastFirst = candle12.OrderBy(x => x.datetime).ToList();
        //    var candle9LastFirst = candle9.OrderBy(x => x.datetime).ToList();
        //    var candle3LastFirst = candle3.OrderBy(x => x.datetime).ToList();
        //    var candle10LastFirst = candle10.OrderBy(x => x.datetime).ToList();
        //    var candle16LastFirst = candle16.OrderBy(x => x.datetime).ToList();
        //    var candle5LastFirst = candle5.OrderBy(x => x.datetime).ToList();
        //    var candle13LastFirst = candle13.OrderBy(x => x.datetime).ToList();
        //    var candleOneHourLastFirst = candleDaily.OrderBy(x => x.datetime).ToList();

        //    decimal WMA30 = 0m;
        //    decimal WMA26 = 0m;
        //    decimal WMA12 = 0m;
        //    decimal WMA9 = 0m;
        //    decimal WMA3 = 0m;
        //    decimal WMA10 = 0m;
        //    decimal WMA16 = 0m;
        //    decimal WMA5 = 0m;
        //    decimal WMA13 = 0m;

        //    decimal wmaPre30 = 0m;
        //    decimal wmaPre26 = 0m;
        //    decimal wmaPre12 = 0m;
        //    decimal wmaPre9 = 0m;
        //    decimal wmaPre3 = 0m;
        //    decimal wmaPre10 = 0m;
        //    decimal wmaPre16 = 0m;
        //    decimal wmaPre5 = 0m;
        //    decimal wmaPre13 = 0m;

        //    decimal N30 = 0m;
        //    decimal N26 = 0m;
        //    decimal N12 = 0m;
        //    decimal N9 = 0m;
        //    decimal N3 = 0m;
        //    decimal N10 = 0m;
        //    decimal N16 = 0m;
        //    decimal N5 = 0m;
        //    decimal N13 = 0m;

        //    decimal P30 = 0m;
        //    decimal P26 = 0m;
        //    decimal P12 = 0m;
        //    decimal P9 = 0m;
        //    decimal P3 = 0m;
        //    decimal P10 = 0m;
        //    decimal P16 = 0m;
        //    decimal P5 = 0m;
        //    decimal P13 = 0m;

        //    decimal SMA30 = 0m;
        //    decimal SMA26 = 0m;
        //    decimal SMA12 = 0m;
        //    decimal SMA9 = 0m;
        //    decimal SMA3 = 0m;
        //    decimal SMA10 = 0m;
        //    decimal SMA16 = 0m;
        //    decimal SMA5 = 0m;
        //    decimal SMA13 = 0m;

        //    decimal smaPre30 = 0m;
        //    decimal smaPre26 = 0m;
        //    decimal smaPre12 = 0m;
        //    decimal smaPre9 = 0m;
        //    decimal smaPre3 = 0m;
        //    decimal smaPre10 = 0m;
        //    decimal smaPre16 = 0m;
        //    decimal smaPre5 = 0m;
        //    decimal smaPre13 = 0m;

        //    decimal FC30 = candle30LastFirst.Count;
        //    decimal FC26 = candle26LastFirst.Count;
        //    decimal FC12 = candle12LastFirst.Count;
        //    decimal FC9 = candle9LastFirst.Count;
        //    decimal FC3 = candle3LastFirst.Count;
        //    decimal FC10 = candle10LastFirst.Count;
        //    decimal FC16 = candle16LastFirst.Count;
        //    decimal FC5 = candle5LastFirst.Count;
        //    decimal FC13 = candle13LastFirst.Count;

        //    decimal VWAP = 0;

        //    decimal EMA30 = 0m;
        //    decimal EMA26 = 0m;
        //    decimal EMA12 = 0m;
        //    decimal EMA9 = 0m;
        //    decimal EMA3 = 0m;
        //    decimal EMA10 = 0m;
        //    decimal EMA16 = 0m;
        //    decimal EMA5 = 0m;
        //    decimal EMA13 = 0m;

        //    decimal smoothingFactor30 = (2m / ((decimal)candle30LastFirst.Count + 1m));
        //    decimal smoothingFactor26 = (2m / ((decimal)candle26LastFirst.Count + 1m));
        //    decimal smoothingFactor12 = (2m / ((decimal)candle12LastFirst.Count + 1m));
        //    decimal smoothingFactor9 = (2m / ((decimal)candle9LastFirst.Count + 1m));
        //    decimal smoothingFactor3 = (2m / ((decimal)candle3LastFirst.Count + 1m));
        //    decimal smoothingFactor10 = (2m / ((decimal)candle10LastFirst.Count + 1m));
        //    decimal smoothingFactor16 = (2m / ((decimal)candle16LastFirst.Count + 1m));
        //    decimal smoothingFactor5 = (2m / ((decimal)candle5LastFirst.Count + 1m));
        //    decimal smoothingFactor13 = (2m / ((decimal)candle13LastFirst.Count + 1m));

        //    decimal MACD = 0m;
        //    decimal MACDSignal = 0m;
        //    decimal MACDHistogram = 0m;
        //    decimal MACDDD = 0m;
        //    decimal MACDSignalDD = 0m;
        //    decimal MACDHistogramDD = 0m;
        //    decimal MACDDD2 = 0m;
        //    decimal MACDSignalDD2 = 0m;
        //    decimal MACDHistogramDD2 = 0m;

        //    decimal RSI14 = 0m;
        //    decimal RSIOneHour = 0m;
        //    decimal close = 0m;
        //    decimal avgUp = 0m;
        //    decimal avgDown = 0m;
        //    decimal avgUpTotal = 0m;
        //    decimal avgDownTotal = 0m;
        //    decimal volume = 0m;

        //    for (int i = 0; i < candle30LastFirst.Count; i++)
        //    {
        //        N30 += (i + 1);
        //    }
        //    for (int i = 0; i < candle26LastFirst.Count; i++)
        //    {
        //        N26 += (i + 1);
        //    }
        //    for (int i = 0; i < candle12LastFirst.Count; i++)
        //    {
        //        N12 += (i + 1);
        //    }
        //    for (int i = 0; i < candle9LastFirst.Count; i++)
        //    {
        //        N9 += (i + 1);
        //    }
        //    for (int i = 0; i < candle3LastFirst.Count; i++)
        //    {
        //        N3 += (i + 1);
        //    }
        //    for (int i = 0; i < candle10LastFirst.Count; i++)
        //    {
        //        N10 += (i + 1);
        //    }
        //    for (int i = 0; i < candle16LastFirst.Count; i++)
        //    {
        //        N16 += (i + 1);
        //    }
        //    for (int i = 0; i < candle5LastFirst.Count; i++)
        //    {
        //        N5 += (i + 1);
        //    }
        //    for (int i = 0; i < candle13LastFirst.Count; i++)
        //    {
        //        N13 += (i + 1);
        //    }
        //    for (int i = 0; i < candle30LastFirst.Count; i++)
        //    {
        //        decimal wmmP30 = Convert.ToDecimal(candle30LastFirst[i].close);
        //        P30 = (wmmP30 * (i + 1));
        //        wmaPre30 += P30;
        //        decimal smaP30 = Convert.ToDecimal(candle30LastFirst[i].close);
        //        smaPre30 += smaP30;
        //    }
        //    for (int i = 0; i < candle26LastFirst.Count; i++)
        //    {
        //        decimal wmmP26 = Convert.ToDecimal(candle26LastFirst[i].close);
        //        P26 = (wmmP26 * (i + 1));
        //        wmaPre26 += P26;
        //        decimal smaP26 = Convert.ToDecimal(candle26LastFirst[i].close);
        //        smaPre26 += smaP26;
        //    }
        //    for (int i = 0; i < candle12LastFirst.Count; i++)
        //    {
        //        decimal wmmP12 = Convert.ToDecimal(candle12LastFirst[i].close);
        //        P12 = (wmmP12 * (i + 1));
        //        wmaPre12 += P12;
        //        decimal smaP12 = Convert.ToDecimal(candle12LastFirst[i].close);
        //        smaPre12 += smaP12;
        //    }
        //    for (int i = 0; i < candle9LastFirst.Count; i++)
        //    {
        //        decimal wmmP9 = Convert.ToDecimal(candle9LastFirst[i].close);
        //        P9 = (wmmP9 * (i + 1));
        //        wmaPre9 += P9;
        //        decimal smaP9 = Convert.ToDecimal(candle9LastFirst[i].close);
        //        smaPre9 += smaP9;
        //    }
        //    for (int i = 0; i < candle3LastFirst.Count; i++)
        //    {
        //        decimal wmmP3 = Convert.ToDecimal(candle3LastFirst[i].close);
        //        P3 = (wmmP3 * (i + 1));
        //        wmaPre3 += P3;
        //        decimal smaP3 = Convert.ToDecimal(candle3LastFirst[i].close);
        //        smaPre3 += smaP3;
        //    }
        //    for (int i = 0; i < candle10LastFirst.Count; i++)
        //    {
        //        decimal wmmP10 = Convert.ToDecimal(candle10LastFirst[i].close);
        //        P10 = (wmmP10 * (i + 1));
        //        wmaPre10 += P10;
        //        decimal smaP10 = Convert.ToDecimal(candle10LastFirst[i].close);
        //        smaPre10 += smaP10;
        //    }
        //    for (int i = 0; i < candle16LastFirst.Count; i++)
        //    {
        //        decimal wmmP16 = Convert.ToDecimal(candle16LastFirst[i].close);
        //        P16 = (wmmP16 * (i + 1));
        //        wmaPre16 += P16;
        //        decimal smaP16 = Convert.ToDecimal(candle16LastFirst[i].close);
        //        smaPre16 += smaP16;
        //    }
        //    for (int i = 0; i < candle5LastFirst.Count; i++)
        //    {
        //        decimal wmmP5 = Convert.ToDecimal(candle5LastFirst[i].close);
        //        P5 = (wmmP5 * (i + 1));
        //        wmaPre5 += P5;
        //        decimal smaP5 = Convert.ToDecimal(candle5LastFirst[i].close);
        //        smaPre5 += smaP5;
        //    }
        //    for (int i = 0; i < candle13LastFirst.Count; i++)
        //    {
        //        decimal wmmP13 = Convert.ToDecimal(candle13LastFirst[i].close);
        //        P13 = (wmmP13 * (i + 1));
        //        wmaPre13 += P13;
        //        decimal smaP13 = Convert.ToDecimal(candle13LastFirst[i].close);
        //        smaPre13 += smaP13;
        //    }
        //    foreach (var item in candle14LastFirst)
        //    {
        //        if (close > 0)
        //            if (close > Convert.ToDecimal(item.close))
        //            {
        //                //avgDown += Convert.ToDecimal(item.close);
        //                avgDown += Math.Abs(((close - Convert.ToDecimal(item.close)) / ((close + Convert.ToDecimal(item.close)) / 2)) * 100);
        //                avgDownTotal++;
        //            }
        //            else
        //            {
        //                //avgUp += Convert.ToDecimal(item.close);
        //                avgUp += Math.Abs(((close - Convert.ToDecimal(item.close)) / ((close + Convert.ToDecimal(item.close)) / 2)) * 100);
        //                avgUpTotal++;
        //            }
        //        close = Convert.ToDecimal(item.close);
        //    }
        //    try
        //    {
        //        avgDown = (avgDown / avgDownTotal);
        //        avgUp = (avgUp / avgUpTotal);
        //        RSI14 = 100 - 100 / (1 + (avgUp / avgDown));
        //    }
        //    catch (Exception ex)
        //    {
        //        _log.LogError($"{ex.Message}. Indicies Calculator RSI");
        //    }
        //    close = 0;
        //    avgUp = 0;
        //    avgDown = 0;
        //    avgUpTotal = 0;
        //    avgDownTotal = 0;
        //    foreach (var item in candleOneHourLastFirst)
        //    {
        //        if (close > 0)
        //            if (close > Convert.ToDecimal(item.close))
        //            {
        //                //avgDown += Convert.ToDecimal(item.Close);
        //                avgDown += Math.Abs(((close - Convert.ToDecimal(item.close)) / ((close + Convert.ToDecimal(item.close)) / 2)) * 100);
        //                avgDownTotal++;
        //            }
        //            else
        //            {
        //                //avgUp += Convert.ToDecimal(item.Close);
        //                avgUp += Math.Abs(((close - Convert.ToDecimal(item.close)) / ((close + Convert.ToDecimal(item.close)) / 2)) * 100);
        //                avgUpTotal++;
        //            }
        //        close = Convert.ToDecimal(item.close);
        //    }
        //    try
        //    {
        //        avgDown = (avgDown / avgDownTotal);
        //        avgUp = (avgUp / avgUpTotal);
        //        RSIOneHour = 100 - 100 / (1 + (avgUp / avgDown));
        //    }
        //    catch (Exception ex)
        //    {
        //        _log.LogError($"{ex.Message}. Indicies Calculator RSI");
        //    }
        //    try
        //    {
        //        WMA30 = (wmaPre30 / N30);
        //        WMA26 = (wmaPre26 / N26);
        //        WMA12 = (wmaPre12 / N12);
        //        WMA9 = (wmaPre9 / N9);
        //        WMA3 = (wmaPre3 / N3);
        //        WMA10 = (wmaPre10 / N10);
        //        WMA16 = (wmaPre16 / N16);
        //        WMA5 = (wmaPre5 / N5);
        //        WMA13 = (wmaPre13 / N13);

        //        SMA30 = (smaPre30 / FC30);
        //        SMA26 = (smaPre26 / FC26);
        //        SMA12 = (smaPre12 / FC12);
        //        SMA9 = (smaPre9 / FC9);
        //        SMA3 = (smaPre3 / FC3);
        //        SMA10 = (smaPre10 / FC10);
        //        SMA16 = (smaPre16 / FC16);
        //        SMA5 = (smaPre5 / FC5);
        //        SMA13 = (smaPre13 / FC13);
        //    }
        //    catch (Exception ex)
        //    {
        //        _log.LogError($"{ex.Message}. WMA/SMA RSI");
        //    }
        //    try
        //    {
        //        //int index = (candle30.Count() - 1);
        //        VWAP = (Convert.ToDecimal(quote?.Element("QuoteData")?.Element("All")?.Element("high")?.Value?.ToString()) +
        //        Convert.ToDecimal(quote?.Element("QuoteData")?.Element("All")?.Element("low")?.Value?.ToString()) +
        //        Convert.ToDecimal(quote?.Element("QuoteData")?.Element("All")?.Element("lastTrade")?.Value?.ToString())) / 3;
        //        volume = Convert.ToInt64(quote?.Element("QuoteData")?.Element("All")?.Element("totalVolume")?.Value?.ToString());
        //    }
        //    catch (Exception ex)
        //    {
        //        _log.LogError($"{ex.Message}. VWAP");
        //    }
        //    //EMA = (Today's Value * Multiplier) + Yesterday's EMA *(1 - Multiplier)
        //    //You can then use the SMA from the previous day as the first EMA for yesterday.
        //    try
        //    {
        //        EMA30 = (Convert.ToDecimal(candle30[0].close) * smoothingFactor30) + (SMA30) * (1 - smoothingFactor30);
        //        EMA26 = (Convert.ToDecimal(candle26[0].close) * smoothingFactor26) + (SMA26) * (1 - smoothingFactor26);
        //        EMA12 = (Convert.ToDecimal(candle12[0].close) * smoothingFactor12) + (SMA12) * (1 - smoothingFactor12);
        //        EMA9 = (Convert.ToDecimal(candle9[0].close) * smoothingFactor9) + (SMA9) * (1 - smoothingFactor9);
        //        EMA3 = (Convert.ToDecimal(candle3[0].close) * smoothingFactor3) + (SMA3) * (1 - smoothingFactor3);
        //        EMA10 = (Convert.ToDecimal(candle10[0].close) * smoothingFactor10) + (SMA10) * (1 - smoothingFactor10);
        //        EMA16 = (Convert.ToDecimal(candle16[0].close) * smoothingFactor16) + (SMA16) * (1 - smoothingFactor16);
        //        EMA5 = (Convert.ToDecimal(candle5[0].close) * smoothingFactor5) + (SMA5) * (1 - smoothingFactor5);
        //        EMA13 = (Convert.ToDecimal(candle13[0].close) * smoothingFactor13) + (SMA13) * (1 - smoothingFactor13);
        //    }
        //    catch (Exception ex)
        //    {
        //        _log.LogError($"{ex.Message}. EMA");
        //    }

        //    MACD = EMA12 - EMA26;
        //    MACDSignal = EMA9 - EMA12;
        //    MACDHistogram = MACD - MACDSignal;
        //    MACDDD = EMA10 - EMA3;
        //    MACDSignalDD = EMA16 - EMA10;
        //    MACDHistogramDD = MACDDD - MACDSignalDD;
        //    MACDDD2 = EMA13 - EMA5;
        //    MACDSignalDD2 = EMA9 - EMA13;
        //    MACDHistogramDD2 = MACDDD2 - MACDSignalDD2;

        //    JsonObject output = new JsonObject();
        //    output.Add("Symbol", symbol);
        //    output.Add("Name", quote.Element("QuoteData").Element("All").Element("companyName").Value.ToString());
        //    output.Add("lastPrice", currentPrice);
        //    output.Add("results", quote.Value.ToString());
        //    output.Add("RSI", RSI14);
        //    output.Add("RSIOneHour", RSIOneHour);
        //    output.Add("SMA30", SMA30);
        //    output.Add("EMA30", EMA30);
        //    output.Add("WMA30", WMA30);
        //    output.Add("EMA26", EMA26);
        //    output.Add("EMA12", EMA12);
        //    output.Add("EMA9", EMA9);
        //    output.Add("EMA3", EMA3);
        //    output.Add("EMA10", EMA10);
        //    output.Add("EMA16", EMA16);
        //    output.Add("EMA5", EMA5);
        //    output.Add("EMA13", EMA13);
        //    output.Add("SMA26", SMA26);
        //    output.Add("SMA12", SMA12);
        //    output.Add("SMA9", SMA9);
        //    output.Add("SMA3", SMA3);
        //    output.Add("SMA10", SMA10);
        //    output.Add("SMA16", SMA16);
        //    output.Add("SMA5", SMA5);
        //    output.Add("SMA13", SMA13);
        //    output.Add("WMA26", WMA26);
        //    output.Add("WMA12", WMA12);
        //    output.Add("WMA9", WMA9);
        //    output.Add("WMA3", WMA3);
        //    output.Add("WMA10", WMA10);
        //    output.Add("WMA16", WMA16);
        //    output.Add("WMA5", WMA5);
        //    output.Add("WMA13", WMA13);
        //    output.Add("MACD", MACD);
        //    output.Add("MACDSignal", MACDSignal);
        //    output.Add("MACDHistogram", MACDHistogram);
        //    output.Add("MACDDD", MACDDD);
        //    output.Add("MACDSignalDD", MACDSignalDD);
        //    output.Add("MACDHistogramDD", MACDHistogramDD);
        //    output.Add("MACDDD2", MACDDD2);
        //    output.Add("MACDSignalDD2", MACDSignalDD2);
        //    output.Add("MACDHistogramDD2", MACDHistogramDD2);
        //    output.Add("VWAP", VWAP);
        //    output.Add("volume", volume);
        //    output.Add("SMACount", candle30.Count());
        //    return output;
        //}

    }
}
