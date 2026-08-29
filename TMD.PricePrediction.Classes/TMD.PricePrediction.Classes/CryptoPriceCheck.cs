using Coinbase.AdvancedTrade.Models;
using Coinbase.AdvancedTrade.Models.Public;
using log4net.Repository.Hierarchy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NewsAggregator.models.returnModel;
using System.Text.Json.Nodes;
using TMD.Services;
using TMDInvestments.Context;
using TMDInvestments.Models;

namespace TMD.PricePrediction
{
    public sealed class CryptoPriceCheck
    {
        ILogger? _logger;
        CoinBaseSettings? _settings;
        //TaAPISettings? _taapiSettings;
        CoinBaseService? coinBaseService = null;
        IHttpClientFactory? _httpClientFactory;
        //TaAPIService? taapiService = null;
        TmdContext? _ctx = null;
        List<BuySellCommodity> BuySellCryptoList;
        //RulesEngine? settings = null;
        IQueryable<CryptoTracker2> cryptoTracker = null;
        List<PublicProduct> products = null;
        Helper? _helper = null;
        List<Account>? Accounts = null;
        static readonly System.Threading.Lock _transactLock = new();
        public CryptoPriceCheck(ILogger logger, CoinBaseSettings settings, IHttpClientFactory httpClientFactory, IDbContextFactory<TmdContext> contextFactory)
        {

            _logger = logger;
            _settings = settings;
            _httpClientFactory = httpClientFactory;
            //_products = products;
            _helper = new Helper(_logger, null, null, null, contextFactory);
            coinBaseService = new CoinBaseService(logger, settings, _httpClientFactory/*, _helper*/);
            //Accounts = coinBaseService?.GetAccounts();
            var ctx = contextFactory.CreateDbContext();
            _ctx = ctx;
        }

        public void Exec(CancellationToken token)
        {
            do
            {
                try
                {
                    if (token.IsCancellationRequested)
                    {
                        Console.WriteLine("Cleaning up resources within the thread...");
                        _ctx.Dispose();
                        _ctx = null;
                        break;
                    }
                    _logger.LogWarning($"Running CryptoPriceCheck on thread loop...");
                    lock (_transactLock)
                    {
                        Accounts = coinBaseService?.GetAccounts();
                    }
                    Run();
                }
                catch (Exception ex)
                {
                    Helper.Log(_logger, LogLevel.Error, $"Source: {ex.Source} stack trace: {ex.StackTrace} inner exception {ex?.InnerException?.Message ?? ""} error: {ex.Message}");
                }
            } while (true);

        }
        private void Run()
        {
            //Get Products
            lock (_transactLock)
            {
                products = coinBaseService.GetProducts();
            }            
            //Clear Ophan wallet or crypto tracker
            ClearOrphanWallets();
            Helper.Log(_logger, LogLevel.Information, $"Clearing Ophan wallet or crypto tracker");
            //update order status for filled and cancelled orders
            //CheckOrderStatus();
            //_helper.Log(_logger, LogLevel.Information, $"Update order status for filled and cancelled orders");

            //Get Products
            //products = coinBaseService.GetProducts();

            //Update Prices
            Helper.Log(_logger, LogLevel.Information, $"Updating Latest Prices on Tracker");
            UpdatePrices();
            System.Threading.Thread.Sleep(250);
        }
        public void ClearOrphanWallets()
        {
            CryptoTracker2? coin = null;
            Order output = null;
            //int lengthAfterDecimal = 0;
            Guid? buyOrderId = null;
            //var Accounts = coinBaseService.GetAccounts();/*.Where(x => x.Currency != "USD")*///.ToList();
            Helper.Log(_logger, LogLevel.Information, $"Getting owned crypto list");
            //var PublicProducts = coinBaseService.GetProducts().OrderBy(x => x.DisplayName);//.ToList();
            //check if there is a wallet with cash not in tracker
            foreach (var item in Accounts)
            {
                Helper.Log(_logger, LogLevel.Information, $"Iterating crypto list. Analyzing {item.Currency}");
                //if (item.Currency.Contains("ETH") /*|| item.Currency == "LTC"*/)
                //{
                //    //they do exist
                //}
                var publicProduct = products.Where(x => x.DisplayName == item.Currency + "-USD").FirstOrDefault();
                var valueofNonTrackedAsset = Math.Round(Convert.ToDecimal(item.AvailableBalance.Value) * Convert.ToDecimal(publicProduct?.Price), 9);
                if (valueofNonTrackedAsset == .0m)
                {
                    Helper.Log(_logger, LogLevel.Information, $"{item.Currency} no available balance.");
                    continue;
                }
                if (valueofNonTrackedAsset > .0m && valueofNonTrackedAsset <= .80m)
                {
                    Helper.Log(_logger, LogLevel.Warning, $"{item.Currency} too low.");
                    continue;
                }
                else
                    if (valueofNonTrackedAsset > .80m && valueofNonTrackedAsset <= 1.5m)
                    {
                        decimal baseSize = 0m;
                        var precision = publicProduct?.BaseIncrement.Substring((publicProduct.BaseIncrement.IndexOf(".") + 1), (publicProduct.BaseIncrement.Length - (publicProduct.BaseIncrement.IndexOf(".") + 1)));
                        var currency = publicProduct?.DisplayName.Split('-');
                        //var account = coinBaseService.GetAccounts().Where(x => x.Currency == currency[0]).FirstOrDefault();
                        var account = Accounts?.Where(x => x.Currency == currency[0])?.FirstOrDefault();
                        if (account?.AvailableBalance.Value.IndexOf(".") <= -1)
                            baseSize = Convert.ToDecimal(account.AvailableBalance.Value);
                        else
                            baseSize = Convert.ToDecimal(account?.AvailableBalance.Value.Substring(0, (account.AvailableBalance.Value.IndexOf(".") + precision.Length)));
                        if (baseSize <= .0m)
                            continue;
                        output = coinBaseService.SetOrder(publicProduct?.DisplayName, Coinbase.AdvancedTrade.Enums.OrderSide.SELL, baseSize);
                        Helper.Log(_logger, LogLevel.Warning, $"{item.Currency} too low.");
                        System.Threading.Thread.Sleep(250);
                        continue;
                    }
                    else
                        if (valueofNonTrackedAsset > 1.5m)
                        {
                            coin = _ctx.CryptoTracker2.AsNoTracking().Where(x => x.Coin == item.Currency + "-USD" && x.PriceSold == null && x.DateSold == null && (x.BuyStatus.ToLower() == "open" || x.BuyStatus.ToLower() == "pending")).FirstOrDefaultAsync().Result;
                            Helper.Log(_logger, LogLevel.Information, $"{item.Currency} meets min amount checking if synced with db.");
                        }

                //var coin = Cryptos.Where(x => x.Coin == item.Currency + "-USD").ToList();
                if (coin is not null)
                {
                    Helper.Log(_logger, LogLevel.Information, $"{item.Currency} found in db pending");
                    var crypto = coin;
                    if (crypto.BuyOrderId == null)
                    {
                        Helper.Log(_logger, LogLevel.Information, $"{item.Currency} BuyOrderId missing. Getting by last order.");
                        //var results = services.GetOrders(crypto.Coin);
                        //var lastSell = results.Where(x => x.Side == "BUY").OrderByDescending(y => Convert.ToDateTime(y.SequenceTimestamp)).FirstOrDefault();
                        var lastSell = GetLastOrder(crypto.Coin);
                        buyOrderId = Guid.Parse(lastSell?.OrderId);
                        crypto.BuyOrderId = buyOrderId;
                        System.Threading.Thread.Sleep(250);
                    }
                    else
                    {
                        Helper.Log(_logger, LogLevel.Information, $"{item.Currency} BuyOrderId found.");
                        buyOrderId = crypto.BuyOrderId;
                    }

                    Helper.Log(_logger, LogLevel.Information, $"{item.Currency} Getting latest order info and updating.");
                    var buyOrder = coinBaseService.GetOrder((Guid)buyOrderId);
                    //var results = coinBaseService.GetQuote(crypto.Coin);
                    crypto.PricePurchased = Convert.ToDecimal(buyOrder.AverageFilledPrice);
                    //crypto.LastPrice = Convert.ToDecimal(results["lastPrice"].ToString());
                    crypto.CurrentBalance = valueofNonTrackedAsset;
                    crypto.DateUpdated = Helper.easternTime;
                    crypto.DatePurchased = item.UpdatedAt;
                    crypto.BuyStatus = (buyOrder.Status ?? "FILLED");
                    _ctx.CryptoTracker2.Attach(crypto);
                    _ctx.Entry(crypto).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                    //_ctx.SaveChanges();
                    //_ctx.Entry(crypto).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                    Helper.Log(_logger, LogLevel.Information, $"{item.Currency} Updating in db.");
                    System.Threading.Thread.Sleep(250);
                }
                else
                {
                    Helper.Log(_logger, LogLevel.Information, $"{item.Currency} not found pending. Checking filled.");
                    coin = _ctx.CryptoTracker2.AsNoTracking().Where(x => x.Coin == item.Currency + "-USD" && x.PriceSold == null && x.DateSold == null && x.BuyStatus.ToLower() == "filled").FirstOrDefaultAsync().Result;
                    if (coin is not null)
                    {
                        Helper.Log(_logger, LogLevel.Information, $"{item.Currency} found in db filled.");
                        var crypto = coin;
                        if (crypto.BuyOrderId == null)
                        {
                            Helper.Log(_logger, LogLevel.Information, $"{item.Currency} BuyOrderId missing. Getting by last order.");
                            //var results2 = services.GetOrders(crypto.Coin);
                            //var lastSell = results2.Where(x => x.Side == "BUY").OrderByDescending(y => Convert.ToDateTime(y.SequenceTimestamp)).FirstOrDefault();
                            crypto.CurrentBalance = valueofNonTrackedAsset;
                            var lastSell = GetLastOrder(crypto.Coin);
                            buyOrderId = Guid.Parse(lastSell?.OrderId);
                            crypto.BuyOrderId = buyOrderId;
                            crypto.DatePurchased = item.UpdatedAt;
                            _ctx.CryptoTracker2.Attach(crypto);
                            _ctx.Entry(crypto).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                            //_ctx.SaveChanges();
                            //_ctx.Entry(crypto).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                        }
                        //continue; //already in tracker. Update pruchased date
                        else
                        {
                            crypto.CurrentBalance = valueofNonTrackedAsset;
                            crypto.DatePurchased = item.UpdatedAt;
                            _ctx.CryptoTracker2.Attach(crypto);
                            _ctx.Entry(crypto).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                            //_ctx.SaveChanges();
                            //_ctx.Entry(crypto).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                        }
                        Helper.Log(_logger, LogLevel.Information, $"{item.Currency} Updating in db.");
                        System.Threading.Thread.Sleep(250);
                    }
                    else
                    {
                        Helper.Log(_logger, LogLevel.Information, $"{item.Currency} Not found pending or filled. Creating new.");
                        //create new crypto tracker
                        coin = new CryptoTracker2();
                        var results = coinBaseService.GetQuoteAsync(item.Currency + "-USD").Result;
                        var lastSell = GetLastOrder(item.Currency + "-USD");
                        buyOrderId = Guid.Parse(lastSell?.OrderId);
                        coin.BuyOrderId = buyOrderId;
                        coin.Coin = results["Symbol"].ToString();
                        //coin.BuyOrderId = Guid.Parse(order.OrderId);
                        coin.BuyStatus = "FILLED";
                        coin.Name = results["Name"].ToString();
                        coin.TradeDirection = 1;
                        coin.PricePurchased = Convert.ToDecimal(results["lastPrice"].ToString());
                        coin.Amount = valueofNonTrackedAsset;
                        coin.PurchasedRsi = Convert.ToDecimal(results["RSI"].ToString());
                        coin.PurchasedEma9 = Convert.ToDecimal(results["EMA9"].ToString());
                        coin.PurchasedEma12 = Convert.ToDecimal(results["EMA12"].ToString());
                        coin.PurchasedEma26 = Convert.ToDecimal(results["EMA26"].ToString());
                        coin.PurchasedEma3 = Convert.ToDecimal(results["EMA3"].ToString());
                        coin.PurchasedEma10 = Convert.ToDecimal(results["EMA10"].ToString());
                        coin.PurchasedEma16 = Convert.ToDecimal(results["EMA16"].ToString());
                        coin.PurchasedEma5 = Convert.ToDecimal(results["EMA5"].ToString());
                        coin.PurchasedEma8 = Convert.ToDecimal(results["EMA8"].ToString());
                        coin.PurchasedEma13 = Convert.ToDecimal(results["EMA13"].ToString());
                        coin.PurchasedSma9 = Convert.ToDecimal(results["SMA9"].ToString());
                        coin.PurchasedSma12 = Convert.ToDecimal(results["SMA12"].ToString());
                        coin.PurchasedSma26 = Convert.ToDecimal(results["SMA26"].ToString());
                        coin.PurchasedSma3 = Convert.ToDecimal(results["SMA3"].ToString());
                        coin.PurchasedSma10 = Convert.ToDecimal(results["SMA10"].ToString());
                        coin.PurchasedSma16 = Convert.ToDecimal(results["SMA16"].ToString());
                        coin.PurchasedSma5 = Convert.ToDecimal(results["SMA5"].ToString());
                        coin.PurchasedSma8 = Convert.ToDecimal(results["SMA8"].ToString());
                        coin.PurchasedSma13 = Convert.ToDecimal(results["SMA13"].ToString());
                        coin.PurchasedMacd = Convert.ToDecimal(results["MACD"].ToString());
                        coin.PurchasedMacdSignal = Convert.ToDecimal(results["MACDSignal"].ToString());
                        coin.PurchasedMacdHistogram = Convert.ToDecimal(results["MACDHistogram"].ToString());
                        coin.LastPrice = Convert.ToDecimal(results["lastPrice"].ToString());
                        coin.DatePurchased = item.UpdatedAt; ;
                        coin.DateCreated = Helper.easternTime;
                        coin.LastUpdatedBy = 1;
                        _ctx.CryptoTracker2.Add(coin);
                        _ctx.Entry(coin).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                        Helper.Log(_logger, LogLevel.Information, $"{item.Currency} Adding in db.");
                    }
                }
                try
                {
                    _ctx.SaveChangesAsync().Wait();
                    _ctx.Entry(coin).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                }
                catch (Exception ex)
                {
                    Helper.Log(_logger, LogLevel.Error, $"Source: {ex.Source} stack trace: {ex.StackTrace} inner exception {ex?.InnerException?.Message ?? ""} error: {ex.Message}");
                }
                System.Threading.Thread.Sleep(250);
            }
            var Cryptos = _ctx.CryptoTracker2.AsNoTracking().Where(x => x.PriceSold == null && x.DateSold == null && x.BuyStatus == "FILLED");//.ToList();
            foreach (var crypto in Cryptos)
            {
                var currency = crypto.Coin.Split('-');
                var account = Accounts?.Where(x => x.Currency == currency[0])?.FirstOrDefault();
                //var publicProduct = coinBaseService.GetProducts().Where(x => x.DisplayName == crypto.Coin).FirstOrDefault();
                var publicProduct = products.Where(x => x.DisplayName == crypto.Coin).FirstOrDefault();
                var valueofNonTrackedAsset = Math.Round(Convert.ToDecimal(account?.AvailableBalance?.Value) * Convert.ToDecimal(publicProduct?.Price), 9);
                //if (decimal.TryParse(account?.AvailableBalance.Value, out decimal bal) && bal <= 1.5m)
                if (/*valueofNonTrackedAsset > 0m &&*/ valueofNonTrackedAsset <= 1.5m)
                {
                    var ct = crypto;
                    var results = coinBaseService.GetQuoteAsync(ct.Coin).Result;
                    ct.PriceSold = Convert.ToDecimal(results["lastPrice"].ToString());
                    ct.SellOrderId = Guid.Empty;
                    ct.SellStatus = "FILLED";
                    ct.DateSold = Helper.easternTime;
                    ct.DateUpdated = Helper.easternTime;
                    _ctx.CryptoTracker2.Attach(ct);
                    _ctx.Entry(ct).State = Microsoft.EntityFrameworkCore.EntityState.Deleted;
                    _ctx.SaveChangesAsync().Wait();
                    _ctx.Entry(ct).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                }
                System.Threading.Thread.Sleep(250);
            }
        }
        private Fill GetLastOrder(string ProductId)
        {
            var orders = coinBaseService.GetOrders(ProductId);
            var order = orders.Where(x => x.ProductId == ProductId).OrderByDescending(y => Convert.ToDateTime(y.SequenceTimestamp)).FirstOrDefault();
            return order;
        }
        void UpdatePrices()
        {
            //Re-Get crypto already in trade being tracked.
            cryptoTracker = _ctx.CryptoTracker2.AsNoTracking().Where(x => x.PriceSold == null && x.DateSold == null && x.BuyStatus == "Filled");//.ToList();
            //Loop through current crypto and evaluate
            Helper.Log(_logger, LogLevel.Information, $"Checking current brought/shorted crypto");
            foreach (var coin in cryptoTracker)
            {
                //if (coin.Status == "open" || coin.Status == "pending")
                //    continue;
                var st = coin;
                //var st = _ctx.CryptoTracker.Where(x => x.Coin == coin.Coin).FirstOrDefault();
                Helper.Log(_logger, LogLevel.Information, $"Checking {coin.Coin} - ({coin.Name})");
                JsonObject results = null;
                try
                {
                    System.Threading.Thread.Sleep(1000);
                    results = coinBaseService.GetQuoteAsync(coin.Coin).Result;                    
                }
                catch (Exception ex)
                {
                    Helper.Log(_logger, LogLevel.Error, $"Crypto check error. Failed to get {coin.Coin}. Trying again.");
                    try
                    {
                        //Try again
                        System.Threading.Thread.Sleep(1000);
                        results = coinBaseService.GetQuoteAsync(coin.Coin).Result;
                    }
                    catch (Exception ex2)
                    {
                        Helper.Log(_logger, LogLevel.Error, $"Crypto  {coin.Coin} check error {ex2?.Source} {ex2?.StackTrace} {ex2?.InnerException} {ex2?.Message}.");
                        continue;
                    }
                    
                }

                //just trade
                Helper.Log(_logger, LogLevel.Warning, $"Crypto {coin.Coin} - ({coin.Name}) Time since traded hrs {Math.Abs(Math.Round(((Helper.easternTime - coin.DatePurchased.Value).TotalHours - 24), 5))}. Traded Price ${coin.PricePurchased} traded direction {Enum.GetName(typeof(TradeDirection), coin.TradeDirection)} crypto current price ${Convert.ToDecimal(results["lastPrice"].ToString())}");
                //var accounts = services.GetAccounts().Where(x => Convert.ToDecimal(x.AvailableBalance.Value) > 0).ToList();                

                if (st != null)
                    AnalyiseOwnedCrypto(st, results, products);
                System.Threading.Thread.Sleep(250);
            }
        }
        void AnalyiseOwnedCrypto(CryptoTracker2 crypto, JsonObject? results, List<PublicProduct> products)
        {
            //calculate percent difference
            decimal percentDiffPricePurchased = 0m;
            decimal percentDiffLastPrice = 0m;
            decimal lastPrice = (crypto.LastPrice ?? 0);
            decimal pricePurchased = (crypto.PricePurchased ?? 0);
            decimal currentPrice = Convert.ToDecimal(results["lastPrice"].ToString());

            var product = products.Where(x => x.DisplayName == crypto.Coin).FirstOrDefault();
            var account = Accounts?.Where(x => x.Currency == crypto.Coin.Split(new char[] { '-' })[0] && Convert.ToDecimal(x.AvailableBalance.Value) > 0m)?.FirstOrDefault();
            var currentBalance = Math.Round(Convert.ToDecimal(account?.AvailableBalance?.Value) * Convert.ToDecimal(product?.Price), 5);
            //var currentBalance = Math.Round(Convert.ToDecimal(account?.AvailableBalance?.Value), 5); //* Convert.ToDecimal(product?.Price), 5);
            //if (crypto.Coin == "POPCAT-USD")
            //{
            //    //bobo
            //}
            try
            {
                percentDiffPricePurchased = Math.Abs(((pricePurchased - currentPrice) / ((pricePurchased + currentPrice) / 2)) * 100);
                percentDiffLastPrice = Math.Abs(((lastPrice - currentPrice) / ((lastPrice + currentPrice) / 2)) * 100);
            }
            catch (Exception ex)
            {
                Helper.Log(_logger, LogLevel.Error, $"{ex.Message} at AnalyiseOwnedStock Percent Diff. Setting to zero.");
            }
            //Short
            //if (crypto.TradeDirection == (int)TradeDirection.SELL_SHORT) //selling short
            //{
            //    if (currentPrice > pricePurchased)
            //    {
            //        if (percentDiffPricePurchased > settings.StopLossPercentPurchasedCrypto)
            //        {
            //            //buy to cover
            //            _helper.Log(_logger, LogLevel.Information, $"crypto {crypto.Coin} - ({crypto.Name}) has fallen by more then about {Math.Round(percentDiffPricePurchased, 5)}% since price purchased. Buying to cover.");
            //            BuySellCrypto(crypto, results, TradeDirection.BUY_TO_COVER, Math.Round(Convert.ToDecimal(order.FilledSize), 5), services, products, false);
            //        }
            //        else
            //        {
            //            //Update Price
            //            _helper.Log(_logger, LogLevel.Information, $"crypto {crypto.Coin} - ({crypto.Name}) still within about {Math.Round(percentDiffPricePurchased, 5)}% tolerance since price purchased. Updating last Price.");
            //            UpdateLastPrice(currentPrice, balance, ref crypto);
            //        }
            //    }
            //    else
            //    if (currentPrice > lastPrice)
            //    {
            //        if (percentDiffLastPrice > settings.StopLossPercentLastPriceCrypto)
            //        {
            //            //buy to cover
            //            _helper.Log(_logger, LogLevel.Information, $"crypto {crypto.Coin} - ({crypto.Name}) has fallen by more then about {Math.Round(percentDiffLastPrice, 5)}% since last price. Buying to cover.");
            //            BuySellCrypto(crypto, results, TradeDirection.BUY_TO_COVER, Math.Round(Convert.ToDecimal(order.FilledSize), 5), services, products, false);
            //        }
            //        else
            //        {
            //            //Update Price
            //            //_helper.Log(_logger, LogLevel.Information, $"crypto {crypto.Coin} - ({crypto.Name}) still within about {Math.Round(percentDiffLastPrice, 5)}% tolerance since last price. Updating last Price.");
            //            //UpdateLastPrice(currentPrice, balance, ref crypto);
            //        }
            //    }
            //    else //if (currentPrice <= lastPrice)
            //    {
            //            //update price
            //            _helper.Log(_logger, LogLevel.Information, $"crypto {crypto.Coin} - ({crypto.Name}) still within about {Math.Round(percentDiffLastPrice, 5)}% tolerance since last price. Updating last Price.");
            //            UpdateLastPrice(currentPrice, balance, ref crypto);
            //    }
            //}
            //else //Going Long
            //{
            //if (currentPrice < pricePurchased)
            //{
            //    if (percentDiffPricePurchased > settings.StopLossPercentPurchasedCrypto)
            //    {
            //        //buy to cover
            //        Helper.Log(_logger, LogLevel.Information, $"crypto  {crypto.Coin}  - ( {crypto.Name}) has fallen by more then about {Math.Round(percentDiffPricePurchased, 5)}% since price purchased. Selling.");
            //        UpdateLastPrice(currentPrice, currentBalance, ref crypto);
            //        BuySellCrypto(crypto, results, TradeDirection.SELL, currentBalance, coinBaseService, products, false/*, false*/);
            //    }
            //    else
            //    {
            //        //Update Price
            //        Helper.Log(_logger, LogLevel.Information, $"crypto  {crypto.Coin}  - ( {crypto.Name}) still within about {Math.Round(percentDiffPricePurchased, 5)}% tolerance since price purchased. Updating last Price.");
            //        UpdateLastPrice(currentPrice, currentBalance, ref crypto);
            //    }
            //}
            //else
            //if (currentPrice < lastPrice && percentDiffLastPrice >= settings.StopLossPercentLastPriceCrypto && percentDiffPricePurchased >= 3.5m)
            if ((currentPrice < lastPrice) &&
            (pricePurchased < currentPrice) &&
            (percentDiffPricePurchased >= 4m))
            //if (currentPrice < lastPrice && currentBalance > (crypto.Amount * 1.08m))
            {
                //if (percentDiffLastPrice > settings.StopLossPercentLastPriceCrypto)
                //{
                //sell
                Helper.Log(_logger, LogLevel.Information, $"crypto {crypto.Coin} - ({crypto.Name}) has fallen by more then about {Math.Round(percentDiffLastPrice, 5)}% since last Price. Selling.");
                UpdateLastPrice(currentPrice, currentBalance, ref crypto);
                _helper.BuySellCrypto(crypto, results, TradeDirection.SELL, currentBalance, coinBaseService, products/*, false, false*/);
                //}
                //else
                //{
                //Update Price
                //_helper.Log(_logger, LogLevel.Information, $"crypto {crypto.Coin} - ({crypto.Name}) still within about {Math.Round(percentDiffLastPrice, 5)}% tolerance since last Price. Updating last Price.");
                //UpdateLastPrice(currentPrice, balance, ref crypto);
                //}
            }
            else //if (currentPrice >= lastPrice)
            {
                //update price
                Helper.Log(_logger, LogLevel.Information, $"crypto {crypto.Coin} - ({crypto.Name}) still within about {Math.Round(percentDiffLastPrice, 5)}% tolerance since last price. Updating last Price.");
                UpdateLastPrice(currentPrice, currentBalance, ref crypto);
            }
            System.Threading.Thread.Sleep(250);
        }
        bool UpdateLastPrice(decimal lastPrice, decimal Balance, ref CryptoTracker2 cryptoTracker)
        {
            _ctx = new TmdContext();
            if ((cryptoTracker.TradeDirection == (int)TradeDirection.BUY && (lastPrice > cryptoTracker.LastPrice && lastPrice > cryptoTracker.PricePurchased)) || cryptoTracker.LastPrice is null)
            {
                cryptoTracker.LastPrice = lastPrice;
            }
            if ((cryptoTracker.TradeDirection == (int)TradeDirection.SELL_SHORT && (lastPrice < cryptoTracker.LastPrice && lastPrice < cryptoTracker.PricePurchased)) || cryptoTracker.LastPrice is null)
            {
                cryptoTracker.LastPrice = lastPrice;
            }
            cryptoTracker.CurrentBalance = Balance;
            cryptoTracker.DateUpdated = Helper.easternTime;
            try
            {
                _ctx.Entry(cryptoTracker).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                System.Threading.Thread.Sleep(200);
                _ctx.SaveChanges();
                _ctx.Entry(cryptoTracker).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
            }
            catch (Exception ex)
            {
                Helper.Log(_logger, LogLevel.Error, $"Price Update Failed! {ex.Message}");
            }
            return true;
        }

        ~CryptoPriceCheck() {
            coinBaseService = null;
            _helper = null;
        }
    }
}
