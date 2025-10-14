using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TMDInvestments.Models;

namespace TMD.Services.Service
{
    public sealed class TaAPIService
    {
        private readonly ILogger? _logger;
        private readonly TaAPISettings? _settings;
        private readonly IHttpClientFactory? _factory;
        HttpClientServices<dynamic>? client = null;
        public TaAPIService(ILogger logger, TaAPISettings settings, IHttpClientFactory? factory)
        {
            _logger = logger;
            _settings = settings;
            _factory = factory;
        }
        public dynamic GetTechnicalAnalysis(string symbol, string interval, string type, string exchange)
        {
            JsonElement? results = null;
            string payload = "{" +
                "\"secret\": \"" + _settings?.Key + "\"," +
                "\"construct\": {" +
                    "\"exchange\": \"" + exchange + "\"," +
                    "\"symbol\": \"" + symbol + "\"," +
                    "\"interval\": \"" + interval + "\"," +
                    "\"type\": \"" + type + "\"," +
                    "\"indicators\": [" +
                        "{" +
                            "\"indicator\": \"price\"" +
                        "}," +
                        "{" +
                            "\"indicator\": \"rsi\"" +
                        "}," +
                        "{" +
                            "\"indicator\": \"sma\"" +
                        "}," +
                        "{" +
                            "\"indicator\": \"ema\"" +
                        "}," +
                        "{" +
                            "\"indicator\": \"wma\"" +
                        "}," +
                        "{" +
                            "\"indicator\": \"vwap\"" +
                        "}," +
                        "{" +
                            "\"id\": \"my_custom_id\"," +
                            "\"indicator\": \"macd\"," +
                            "\"backtrack\": 0" +
                        "}" +
                    "]" +
                "}" +
            "}";
            try
            {
                client = new HttpClientServices<dynamic>(_factory, _settings, _logger);
                results = client.Post("bulk",payload,null, new Dictionary<string, object>(), "application/json");
                JsonElement value = new JsonElement();
                if (results.Value.ValueKind == JsonValueKind.Object && !results.Value.TryGetProperty("errors", out value))
                {
                    return results;
                }
                else
                {
                    _logger?.LogError($"Error: {value}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Source: {ex.Source} stack trace: {ex.StackTrace} inner exception {ex?.InnerException?.Message ?? ""} error: {ex.Message}");
                return null;
            }
        }
    }
}
