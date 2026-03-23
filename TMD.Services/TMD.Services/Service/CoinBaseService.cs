using Coinbase.AdvancedTrade;
using Coinbase.AdvancedTrade.Enums;
using Coinbase.AdvancedTrade.Models;
using Coinbase.AdvancedTrade.Models.Public;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using TMD.Services.Service;
using TMDInvestment.Helpers;
using TMDInvestment.Models;
using TMDInvestments.Context;
using TMDInvestments.Models;
using TMDInvestments.Models.Models.Polygon;


namespace TMD.Services
{
    public sealed class CoinBaseService
    {
        private readonly ILogger? _logger;
        private readonly CoinBaseSettings? _settings;
        private readonly IHttpClientFactory? _factory;
        private TaAPISettings? _taapiSettings;
        TmdContext? ctx = null;
        private CoinbaseClient _coinbaseClient = null!;
        private CoinbasePublicClient _coinbasePublicClient = null!;
        TaAPIService? taAPIService = null;
        public CoinBaseService() {
            _coinbasePublicClient = new CoinbasePublicClient();
        }
        public CoinBaseService(ILogger logger, CoinBaseSettings settings, IHttpClientFactory? factory)
        {
            _logger = logger;
            _settings = settings;
            _factory = factory;
            // Setup for OAuth2 authenticated client
            //var oAuth2Token = Environment.GetEnvironmentVariable("COINBASE_OAUTH2_TOKEN", EnvironmentVariableTarget.User)
            // ?? throw new InvalidOperationException("COINBASE_OAUTH2_TOKEN not found");

            //_coinbaseOAuthClient = new CoinbaseOauth2Client()
            _coinbaseClient = new CoinbaseClient(settings.Name, settings.PrivateKey);
            _coinbasePublicClient = new CoinbasePublicClient();
            //_factory = factory;

            //_coinbaseClient.Products.GetProductCandlesAsync();
            //_coinbaseAuthenticator = new CoinbaseAuthenticator(settings.Name, settings.PrivateKey);
            //_coinbaseAuthenticator.

            //Account account = new Account();
            //account.
        }
        public CoinBaseService(ILogger logger, CoinBaseSettings settings, TaAPISettings taAPISettings, IHttpClientFactory? factory)
        {
            _logger = logger;
            _settings = settings;
            _factory = factory;
            _taapiSettings = taAPISettings;
            _coinbaseClient = new CoinbaseClient(settings.Name, settings.PrivateKey);
            _coinbasePublicClient = new CoinbasePublicClient();
            taAPIService = new TaAPIService(_logger, _taapiSettings, _factory);
        }

        public List<Account> GetAccounts()
        {
            return _coinbaseClient.Accounts.ListAccountsAsync().Result;
            
        }
        public Coinbase.AdvancedTrade.Models.Order GetOrder(Guid orderId)
        {
            return _coinbaseClient.Orders.GetOrderAsync(orderId.ToString()).Result;
        }
        public List<Coinbase.AdvancedTrade.Models.Fill> GetOrders(string producdId)
        {
            return _coinbaseClient.Orders.ListFillsAsync(null, producdId,null,null).Result;
        }
        public List<PublicProduct> GetProducts()
        {
            return _coinbasePublicClient.Public.ListPublicProductsAsync().Result;
        }
        public dynamic GetQuote(string productId)
        {

            IndicesCalculatorV2 indicesCalculator = new IndicesCalculatorV2(_logger, _settings, _factory/*, false, true*/);
            var product = _coinbasePublicClient.Public.GetPublicProductAsync(productId).Result;
            var results = indicesCalculator.CalculateV2(product);
            return results;
        }
        public List<PortfolioBreakdown> GetPortfolios()
        {
            List<PortfolioBreakdown> portfolios = new List<PortfolioBreakdown>();
            var Portfolios = _coinbaseClient.Portfolio.ListPortfolioAsync().Result;
            foreach (var portfolio in Portfolios) {
                portfolios.Add(_coinbaseClient.Portfolio.GetPortfolioAsync(portfolio.uuid).Result);
            }
            return portfolios;
        }
        public List<Coinbase.AdvancedTrade.Models.Public.PublicCandle> GetPriceHistory(string productId, DateTime? startDate, DateTime? endDate, Granularity granularity) 
        {
            var start = new DateTimeOffset(startDate.Value.ToUniversalTime()).ToUnixTimeSeconds()/*.ToString()*/;
            var end = new DateTimeOffset(endDate.Value.ToUniversalTime()).ToUnixTimeSeconds()/*.ToString()*/;
            //return _coinbaseClient.Products.GetProductCandlesAsync(productId, start, end, granularity).Result;
            return _coinbasePublicClient.Public.GetPublicProductCandlesAsync(productId, start, end, granularity).Result;
        }

        public Coinbase.AdvancedTrade.Models.Order SetOrder(string productId, OrderSide side, decimal amount) 
        {
            try
            {
                Coinbase.AdvancedTrade.Models.Order results = _coinbaseClient.Orders.CreateMarketOrderAsync(productId, side, amount.ToString(), true).Result;
                if (!string.IsNullOrEmpty(results.Status))
                    return results;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Buy Crypto Error Source: {ex.Source} stack trace: {ex.StackTrace} inner exception {ex?.InnerException?.Message ?? ""} error: {ex.Message}");
                return null;
            }

            return null;
        }
        public Coinbase.AdvancedTrade.Models.Order SetOrderLimit(string productId, OrderSide side, string baseSize, decimal? amount)
        {
            try
            {
                Coinbase.AdvancedTrade.Models.Order results = _coinbaseClient.Orders.CreateLimitOrderGTCAsync(productId, side, baseSize, amount.ToString(), false, true).Result;
                if (!string.IsNullOrEmpty(results.Status))
                    return results;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Buy Crypto Error Source: {ex.Source} stack trace: {ex.StackTrace} inner exception {ex?.InnerException?.Message ?? ""} error: {ex.Message}");
                return null;
            }

            return null;
        }
    }
}
