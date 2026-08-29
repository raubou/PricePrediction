using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using TMDInvestments.Models;
using TMDInvestments.Models.Models.FInhub;
using TMDInvestments.Models.Models.Polygon;

namespace TMD.Services.Service
{
    public sealed class PolygonService
    {
        private readonly ILogger? _logger;
        private Polygon? _settings;
        private readonly IHttpClientFactory? _factory;
        Dictionary<string, object>? headers;
        HttpClientServices<Indicators> client = null;
        public PolygonService(ILogger logger, Polygon settings, IHttpClientFactory factory)
        {
            _logger = logger;
            _settings = settings;
            _factory = factory;
        }

        public JsonObject GetIndicators(string symbol, DateTime timestamp, string timespan, int? rsiWindow, int? short_window, int? long_window, int? signal_window, dynamic results)
        {
            JsonObject output = new JsonObject();
            //SMA
            Indicators sma = GetIndicator(symbol, "sma", timestamp.ToString("yyyy-MM-dd"), timespan, short_window, null, null);
            var date = DateTimeOffset.FromUnixTimeMilliseconds(sma.results.values[0].timestamp).DateTime;
            output.Add($"SMA{short_window}", sma.results.values[0].value);
            System.Threading.Thread.Sleep(1000); // To avoid hitting API rate limits
            sma = GetIndicator(symbol, "sma", timestamp.ToString("yyyy-MM-dd"), timespan, long_window, null, null);
            output.Add($"SMA{long_window}", sma.results.values[0].value);
            System.Threading.Thread.Sleep(1000); // To avoid hitting API rate limits
            sma = GetIndicator(symbol, "sma", timestamp.ToString("yyyy-MM-dd"), timespan, signal_window, null, null);
            output.Add($"SMA{signal_window}", sma.results.values[0].value);
            System.Threading.Thread.Sleep(1000); // To avoid hitting API rate limits
            //EMA
            Indicators ema = GetIndicator(symbol, "ema", timestamp.ToString("yyyy-MM-dd"), timespan, short_window, null, null);
            output.Add($"EMA{short_window}", ema.results.values[0].value);
            System.Threading.Thread.Sleep(1000); // To avoid hitting API rate limits
            ema = GetIndicator(symbol, "ema", timestamp.ToString("yyyy-MM-dd"), timespan, long_window, null, null);
            output.Add($"EMA{long_window}", ema.results.values[0].value);
            System.Threading.Thread.Sleep(1000); // To avoid hitting API rate limits
            ema = GetIndicator(symbol, "ema", timestamp.ToString("yyyy-MM-dd"), timespan, signal_window, null, null);
            output.Add($"EMA{signal_window}", ema.results.values[0].value);
            System.Threading.Thread.Sleep(1000); // To avoid hitting API rate limits
            //MACD
            var macd = GetIndicator(symbol, "macd", timestamp.ToString("yyyy-MM-dd"), timespan, short_window, long_window, signal_window);
            output.Add("MACD", sma.results.values[0].value);
            output.Add("MACDSignal", sma.results.values[0].signal);
            output.Add("MACDHistogram", sma.results.values[0].histogram);
            System.Threading.Thread.Sleep(1000); // To avoid hitting API rate limits
            //RSI
            var rsi = GetIndicator(symbol, "rsi", timestamp.ToString("yyyy-MM-dd"), timespan, rsiWindow, null, null);
            output.Add("RSI", sma.results.values[0].value);
            try
            {
                output.Add("volume", Convert.ToInt64(results.GetProperty(symbol.ToUpper()).GetProperty("quote").GetProperty("totalVolume").ToString()));
                output.Add("lastPrice", Convert.ToDecimal(results.GetProperty("quote").GetProperty("lastPrice").ToString()));
            }
            catch (Exception ex)
            {
                try
                {
                    output.Add("volume", Convert.ToDecimal(results.Volume24h));
                    output.Add("lastPrice", Convert.ToDecimal(results.Price));
                }
                catch (Exception ex2)
                {
                    try
                    {
                        output.Add("volume", Convert.ToInt64(results?.Element("QuoteData")?.Element("All")?.Element("totalVolume")?.Value?.ToString()));
                        output.Add("lastPrice", Convert.ToDecimal(results?.Element("QuoteData")?.Element("All")?.Element("lastTrade")?.Value?.ToString()));
                    }
                    catch (Exception ex3)
                    {

                    }
                }
            }

            return output;
        }

        private Indicators GetIndicator(string symbol, string indicator, string timestamp, string timespan, int? short_window, int? long_window, int? signal_window)
        {
            headers = new Dictionary<string, object>();
            Indicators indicators = new Indicators();
            headers.Add("accept", "*/*");
            client = new HttpClientServices<Indicators>(_factory, _settings/*, _logger*/);
            indicators = client.Get($"{_settings.Indicators.Replace("{symbol}", symbol.ToString()).Replace("{indicator}", indicator.ToString()).Replace("{timestamp}", timestamp.ToString()).Replace("{timespan}", timespan.ToString()).Replace("{window}", short_window.ToString()).Replace("{short_window}", short_window.ToString()).Replace("{long_window}", long_window.ToString()).Replace("{signal_window}", signal_window.ToString())}", $"Bearer {_settings.PolygonToken}", headers).Result;
            return indicators;
        }
    }
}
