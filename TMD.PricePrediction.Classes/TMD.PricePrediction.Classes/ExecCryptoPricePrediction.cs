using Coinbase.AdvancedTrade.Models;
using Coinbase.AdvancedTrade.Models.Public;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using TMD.Services;
using TMDInvestments.Context;
using TMDInvestments.Models;
using TMDInvestments.Models.Enum;

namespace TMD.PricePrediction.Classes
{
    public sealed class ExecCryptoPricePrediction //: IDisposable
    {
        ILogger? _logger;
        CoinBaseSettings? _settings; 
        //TaAPISettings? _taapiSettings;
        CoinBaseService? coinBaseService = null;
        IHttpClientFactory? _httpClientFactory;
        //TaAPIService? taapiService = null;
        TmdContext? _ctx = null;
        List<BuySellCommodity>? BuySellCryptoList;
        RulesEngine? settings = null;
        IQueryable<CryptoTracker2>? cryptoTracker = null;
        List<PublicProduct>? products = null;
        //CryptoPriceCheck? cryptoPriceCheck = null;
        //Task? task1 = null;
        Helper? _helper = null;
        IDbContextFactory<TmdContext>? _contextFactory = null;
        List<PortfolioBreakdown>? portfolios = null;
        static readonly System.Threading.Lock _transactLock = new();
        //public ExecCryptoPricePrediction(ILogger logger, CoinBaseSettings settings, IHttpClientFactory httpClientFactory, Helper helper) {

        //    _logger = logger;
        //    _settings = settings;
        //    _helper = helper;
        //    coinBaseService = new CoinBaseService(logger, settings, _httpClientFactory);
        //    ctx = new TmdContext();
        //    //helper = new Helper();
        //}
        public ExecCryptoPricePrediction(ILogger logger, CoinBaseSettings settings, IHttpClientFactory httpClientFactory, IDbContextFactory<TmdContext> contextFactory)
        {

            _logger = logger;
            _settings = settings;
            _httpClientFactory = httpClientFactory;
            _contextFactory = contextFactory;
            
            //_ctx = ctx;
            //_taapiSettings = taAPISettings;
            //_helper = helper;
            _helper = new Helper(_logger, null, null, null, contextFactory);
            coinBaseService = new CoinBaseService(logger, settings, _httpClientFactory/*, _helper*/);
            //taapiService = new TaAPIService(_logger, _taapiSettings, _httpClientFactory);
            //ctx = new TmdContext();
            //helper = new Helper();
            //IConfigurationBuilder builder = new ConfigurationBuilder()
            //    .AddJsonFile("appsettings.json");
            //IConfigurationRoot config = builder.Build();
            //var ConnectionStrings = config.GetSection("ConnectionStrings").Get<ConnectionSettings>();
            //var services = new ServiceCollection();
            //var serviceProvider = services.BuildServiceProvider();
            var ctx = contextFactory.CreateDbContext();
            _ctx = ctx;
            //using (var scope = serviceProvider.CreateScope())
            //{
            //    var ctxFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TmdContext>>();
            //    var context = ctxFactory.CreateDbContext();
            //    _ctx = context;
            //}
        }
        public void Exec(CancellationToken token)
        {
            //do
            //{
                // 5. Regularly poll the cancellation request status
                if (token.IsCancellationRequested)
                {
                    Console.WriteLine("Cleaning up resources within the thread...");
                    // Perform thread-specific cleanup here
                    _ctx.Dispose();
                    _ctx = null;
                    //httpClientFactory = null;
                    return;
                }
                Run(token);
            //    System.Threading.Thread.Sleep(5000);
            //} while (true);
        }
        private void Run(CancellationToken stoppingToken)
        {
            
            decimal cash = 0m;
            decimal cryptoBalance = 0m;
            decimal totalAvailiableforLongTrade = 0m;

            //Get Products
            lock (_transactLock)
            {
                products = coinBaseService.GetProducts();
            }

            //GetSettings
            settings = _ctx.RulesEngine.FirstOrDefaultAsync().Result;
            Helper.Log(_logger, LogLevel.Information, $"Get settings");
            //_logger.LogInformation($"Get settings");

            //Clear Ophan wallet or crypto tracker
            //ClearOrphanWallets();
            //Helper.Log(_logger, LogLevel.Information, $"Clearing Ophan wallet or crypto tracker");
            //update order status for filled and cancelled orders
            //CheckOrderStatus();
            //_helper.Log(_logger, LogLevel.Information, $"Update order status for filled and cancelled orders");

            //Update Prices
            //Helper.Log(_logger, LogLevel.Information, $"Updating Latest Prices on Tracker");
            //UpdatePrices();
            //System.Threading.Thread.Sleep(1000);

            //Unload Aging Stocks
            Helper.Log(_logger, LogLevel.Information, $"Unload aging stock");
            //UnloadAgingStocks();

            //Get Portfolios Info
            Helper.Log(_logger, LogLevel.Information, $"Get account info");
            lock (_transactLock)
            {
                portfolios = coinBaseService.GetPortfolios();
            }                

            //Checked for queued crypto adn see if direction is the same
            Helper.Log(_logger, LogLevel.Information, $"Check for queued crypto");
            //CheckforQueuedCrypto(); 

            //Get Crypto already in trade being tracked.
            cryptoTracker = _ctx.CryptoTracker2.AsNoTracking().Where(x => x.PriceSold == null && x.DateSold == null && (x.BuyStatus.ToLower() == "filled" /*|| x.BuyStatus.ToLower() == "open" || x.BuyStatus.ToLower() == "pending"*/));//.ToList();
            var cryptoTrackerList = cryptoTracker.AsNoTracking().Select(y => y.Coin);
            Helper.Log(_logger, LogLevel.Information, $"Get exiting Crypto tracker");

            if(portfolios != null && portfolios.Count() > 0)
            {
                cash = portfolios.Sum(x => Convert.ToDecimal(x.Portfolio_balances.total_cash_equivalent_balance.value));
                cryptoBalance = portfolios.Sum(x => Convert.ToDecimal(x.Portfolio_balances.total_crypto_balance.value));
                totalAvailiableforLongTrade = Math.Truncate((cash * .99m)); //1% fee set aside
            }

            //var totalAvailiableforLongTrade = Math.Round(cash, 2);
            //totalAvailiableforLongTrade = 180m;

            Helper.Log(_logger, LogLevel.Information, $"Cash Balance ${cash}. Crypto Balance ${cryptoBalance}. Total Availiable ${totalAvailiableforLongTrade}");

            if (totalAvailiableforLongTrade > 2)
            {
                //Get list of Crypto to trade not in tracker
                //var cryptoList = _ctx.CryptoList.AsNoTracking().Where(x => (!cryptoTrackerList.Contains(x.Coin) && !cryptoTrackerList.Contains(x.Coin.Substring(0, (x.Coin.Length - 1)))) && !x.ExcludeFromAutomation).OrderBy(y => Guid.NewGuid());//.ToList();

                Helper.Log(_logger, LogLevel.Information, $"Get crypto list to trade");

                //Crypto sold/brought to cover in the last 2(4) hrs
                var CryptoHold24Hr = _ctx.CryptoTracker2.AsNoTracking().AsEnumerable().Where(x => x.PriceSold != null && x.DateSold != null && Math.Abs((x.DateSold - Helper.easternTime).Value.TotalHours) < 24).Select(y => new { Coin = y.Coin, DateSold = y.DateSold, DateUpdated = y.DateUpdated });//.ToList();

                //Loop through and evaluate.
                int? shortCnt = 0;
                int? longCnt = 0;
                int? cryptoCnt = 0;
                //int? rejectPriceCount = 0;

                Helper.Log(_logger, LogLevel.Information, $"Iterate Crypto list, check indicators for buy/sell short");

                BuySellCryptoList = new List<BuySellCommodity>();

                if (cryptoTracker.Count() < (int)settings.MaxNumberofCryptotoBuy)
                //foreach (var crypto in cryptoList)
                foreach (var crypto in Enum.GetNames<TMDInvestments.Models.Enum.CryptoList>())
                {
                    var cryptoEnum =Enum.Parse<TMDInvestments.Models.Enum.CryptoList>(crypto);
                    shortCnt = 0;
                    longCnt = 0;
                    //if (crypto?.QuoteRetry >= 50)
                    //    continue;
                    //Skip if status is not filled
                    var status = cryptoTracker.AsNoTracking().Where(x => x.Coin == crypto + "-USD").FirstOrDefault()?.BuyStatus?.ToLower();
                    if (status == "open" || status == "pending")
                        continue;


                    //Check it stock has been brought in the last 24 hrs
                    if (CryptoHold24Hr.Any(y => y.Coin == crypto + "-USD"))
                    {
                        //if stock was cancelled, check updated date
                        try
                        {
                            Helper.Log(_logger, LogLevel.Warning, $"Crypto {crypto + "-USD"} - ({cryptoEnum.GetDescription()}) purchased {Math.Round(Math.Abs((CryptoHold24Hr.Where(y => y.Coin == crypto + "-USD").Select(z => z.DateSold).FirstOrDefault() - Helper.easternTime).Value.TotalHours), 5)} hours ago was purchased less than 24h ago, skipping");
                        }
                        catch (Exception ex)
                        {
                            Helper.Log(_logger, LogLevel.Warning, $"Crypto {crypto + "-USD"} - ({cryptoEnum.GetDescription()}) cancelled {Math.Round(Math.Abs((CryptoHold24Hr.Where(y => y.Coin == crypto + "-USD").Select(z => z.DateUpdated).FirstOrDefault() - Helper.easternTime).Value.TotalHours), 5)} hours ago was purchased less than 24h ago, skipping");
                        }
                        continue;
                    }

                    //Check;
                    //CancelOnly = false
                    //IsDisabled = false
                    //LimitOnly = false
                    //PostOnly = false
                    //Status = online
                    //TradingDisabled = false
                    //ViewOnly = false
                    //if(crypto.Coin == "BTC-USD")
                    //{

                    //}
                    var product = products.Where(x => x.DisplayName == crypto + "-USD").FirstOrDefault();
                    if (product == null)
                    {
                        //Helper.Log(_logger, LogLevel.Error, $"Product - Error");
                        //var st = crypto;
                        //st.QuoteRetry = (st.QuoteRetry == null) ? 1 : st.QuoteRetry + 1;
                        //_ctx.CryptoList.Attach(st);
                        //_ctx.Entry(st).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                        //_ctx.SaveChanges();
                        //_ctx.Entry(st).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                        continue;
                    }
                    if (product.CancelOnly || product.IsDisabled || product.LimitOnly || product.PostOnly || product.TradingDisabled || product.ViewOnly || product.Status != "online")
                    {
                        Helper.Log(_logger, LogLevel.Warning, $"Crypto {crypto + "-USD"} - ({cryptoEnum.GetDescription()}) Does not meet the criteria for purchase at this time.");
                        continue;
                    }
                    //skip crypto under $1
                    //decimal? lowestPrice = .2m;
                    //if (Convert.ToDecimal(product.Price) <= lowestPrice)
                    //{
                    //    _helper.Log(_logger, LogLevel.Warning, $"Price of {crypto.Coin} - ({crypto.Name}) ${product.Price} too low below ${lowestPrice}, skipping");
                    //    rejectPriceCount++;
                    //    continue;
                    //}
                    Helper.Log(_logger, LogLevel.Information, $"Checking {crypto + "-USD"} - ({cryptoEnum.GetDescription()})");

                    dynamic results = null;
                    try
                    {
                        results = coinBaseService.GetQuoteAsync(crypto + "-USD").Result;
                    }
                    catch (Exception ex)
                    {
                        Helper.Log(_logger, LogLevel.Error, $"{ex.Message} - ({crypto + "-USD"}) - coinBaseService.GetQuote Error");
                        //var st = crypto;
                        //st.QuoteRetry = (st.QuoteRetry == null) ? 1 : st.QuoteRetry + 1;
                        //_ctx.CryptoList.Attach(st);
                        //_ctx.Entry(st).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                        //_ctx.SaveChanges();
                        //_ctx.Entry(st).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                        continue;
                    }
                    Helper.Log(_logger, LogLevel.Information, $"{crypto + "-USD"} - ({cryptoEnum.GetDescription()}) price {results?["lastPrice"]?.ToString()}");

                    if (results is JsonObject)
                    {
                        if (((JsonObject)results).TryGetPropertyValue("errors", out JsonNode errors))
                        {
                            //failed to get quote.
                            Helper.Log(_logger, LogLevel.Error, $"Failed to get quote for {crypto + "-USD"} - ({cryptoEnum.GetDescription()}) {errors.ToString()}");
                            //var st = crypto;
                            //st.QuoteRetry = (st.QuoteRetry == null) ? 1 : st.QuoteRetry + 1;
                            //_ctx.CryptoList.Attach(st);
                            //_ctx.Entry(st).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                            //_ctx.SaveChanges();
                            //_ctx.Entry(st).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                            continue;
                        }
                    }
                    else
                    {
                        if (((JsonElement)results).TryGetProperty("errors", out JsonElement errors))
                        {
                            //failed to get quote.
                            Helper.Log(_logger, LogLevel.Error, $"Failed to get quote for {crypto + "-USD"} - ({cryptoEnum.GetDescription()}) {errors.ToString()}");
                            //var st = crypto;
                            //st.QuoteRetry = (st.QuoteRetry == null) ? 1 : st.QuoteRetry + 1;
                            //_ctx.CryptoList.Attach(st);
                            //_ctx.Entry(st).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                            //_ctx.SaveChanges();
                            //_ctx.Entry(st).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                            continue;
                        }
                    }

                    //if (Convert.ToDecimal(results["volume"].ToString()) < 50000)
                    //{
                    //    _helper.Log(_logger, LogLevel.Information, $"Not buying/selling short {results["Symbol"].ToString()} - ({results["Symbol"].ToString()}). Volume fall below min of 50000");
                    //    continue;
                    //}
                    _helper?.CalculatePoints(results, ref shortCnt, ref longCnt, .08m, true);
                    //_helper?.CalculatePoints(results, ref longCnt, ref shortCnt, .75m, false);

                    //_logger.LogInformation($"{crypto.Coin} - ({crypto.Name}) Score. Long score: {longCnt}. Short score {shortCnt}");
                    Helper.Log(_logger, LogLevel.Information, $"{crypto + "-USD"} - ({cryptoEnum.GetDescription()}) Score. Long score: {longCnt}. Short score {shortCnt}");

                    if (longCnt >= settings.CryptoLongCount/*8*/)
                    {
                        Helper.Log(_logger, LogLevel.Information, $"Adding {crypto + "-USD"} - ({cryptoEnum.GetDescription()}) to potential long list.");
                        BuySellCryptoList.Add(new BuySellCommodity(longCnt, TradeDirection.BUY, results));
                        cryptoCnt++;
                    }
                    //else //Short not availiable in coinbase yet
                    //if (shortCnt >= settings.CryptoShortCount/*8*/ && (bool)settings.ShortEnabled)
                    //{
                    //    _logger.LogInformation($"Adding {crypto.Coin} - ({crypto.Name}) to potential short list.");
                    //    //BuySellCryptoList.Add(results, TradeDirection.SELL_SHORT);
                    //    BuySellCryptoList.Add(new BuySellCommodity(shortCnt, TradeDirection.SELL_SHORT, results));
                    //    cryptoCnt++;
                    //}
                    else
                    {
                        Helper.Log(_logger, LogLevel.Information, $"Not buying/selling short {crypto + "-USD"} - ({cryptoEnum.GetDescription()}). Long score: {longCnt}. Short score {shortCnt}");
                    }
                }

                //BuySellCryptoList = BuySellCryptoList/*.Where(x => Convert.ToDecimal(x.Results["RSIOneHour"].ToString()) <= 35)*/.OrderByDescending(x => x.Points).ThenBy(x => Convert.ToDecimal(x.Results["RSI"].ToString()))/*.ThenBy(x => Convert.ToDecimal(x.Results["RSI"].ToString()))*/.ToList();
                //BuySellCryptoList = BuySellCryptoList.Where(x => Convert.ToDecimal(x.Results?["RSI"]?.ToString()) <= 30 || Convert.ToDecimal(x.Results?["RSI"]?.ToString()) >= 70).ToList();
                //BuySellCryptoList = BuySellCryptoList.Where(x => Convert.ToDecimal(x.Results?["RSI"]?.ToString()) <= 5m).ToList();
                if (BuySellCryptoList.Count > 0)
                    CalculatePurchaseAmount2(totalAvailiableforLongTrade, products, cryptoTracker, ref BuySellCryptoList);
                else
                    Helper.Log(_logger, LogLevel.Warning, "No Crypto was found to trade matching setting, or max number of stock reached, or 24h hold");

            }
            else
            {
                Helper.Log(_logger, LogLevel.Warning, $"Cash and margin balance below threshold ${totalAvailiableforLongTrade}");
            }
        }
        //public void UnloadAgingStocks()
        //{
        //    var Accounts = services.GetAccounts();/*.Where(x => x.Currency != "USD")*///.ToList();
        //    var PublicProducts = services.GetProducts().OrderBy(x => x.DisplayName);//.ToList();
        //    // Get the TimeZoneInfo object for Eastern Standard Time
        //    // The identifier "Eastern Standard Time" is commonly used on Windows systems.
        //    // On Linux/macOS, you might need "America/New_York".
        //    TimeZoneInfo? easternTimeZone;
        //    try
        //    {
        //        easternTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
        //    }
        //    catch (TimeZoneNotFoundException)
        //    {
        //        // Fallback for non-Windows systems or if the ID is different
        //        easternTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
        //    }
        //    // Get the current UTC time
        //    DateTime utcNow = DateTime.UtcNow;

        //    // Convert the UTC time to Eastern Time
        //    DateTime easternTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, easternTimeZone);

        //    var agingcryptos = ctx?.CryptoTracker2?.ToList()?.Where(x => x.PriceSold == null && x.DateSold == null && x.BuyStatus == "FILLED"
        //    && Math.Abs((x.DateCreated - easternTime).Value.TotalDays) > 10).ToList();
        //    foreach (var crypto in agingcryptos)
        //    {
        //        var results = services.GetQuote(crypto.Coin);
        //        string[] coin = crypto.Coin.Split('-');
        //        var account = Accounts.Where(x => x.Currency == coin[0] && Convert.ToDecimal(x.AvailableBalance.Value) > 0m).FirstOrDefault();
        //        var publicProduct = PublicProducts.Where(x => x.DisplayName == account.Currency + "-USD").FirstOrDefault();
        //        var valueofNonTrackedAsset = Math.Round(Convert.ToDecimal(account.AvailableBalance.Value) * Convert.ToDecimal(publicProduct?.Price), 9);
        //        //Platform tradePlatform = (Platform)crypto.TradePlatform;
        //        TradeDirection currentTradeDirection = (TradeDirection)Enum.Parse(typeof(TradeDirection), crypto.TradeDirection.ToString(), true);
        //        TradeDirection tradeDirection = (currentTradeDirection == TradeDirection.BUY) ? TradeDirection.SELL : TradeDirection.BUY_TO_COVER;

        //        BuySellCrypto(crypto, results, tradeDirection, /*crypto.Amount ?? 0m*/valueofNonTrackedAsset, services, products, false/*, true*/);

        //    }
        //}
        //void UpdatePrices()
        //{
        //    //Re-Get crypto already in trade being tracked.
        //    cryptoTracker = _ctx.CryptoTracker2.AsNoTracking().Where(x => x.PriceSold == null && x.DateSold == null && x.BuyStatus == "Filled");//.ToList();
        //    //Loop through current crypto and evaluate
        //    Helper.Log(_logger, LogLevel.Information, $"Checking current brought/shorted crypto");
        //    foreach (var coin in cryptoTracker)
        //    {
        //        //if (coin.Status == "open" || coin.Status == "pending")
        //        //    continue;
        //        var st = coin;
        //        //var st = _ctx.CryptoTracker.Where(x => x.Coin == coin.Coin).FirstOrDefault();
        //        Helper.Log(_logger, LogLevel.Information, $"Checking {coin.Coin} - ({coin.Name})");
        //        JsonObject results = null;
        //        try
        //        {
        //            results = coinBaseService.GetQuoteAsync(coin.Coin).Result;
        //        }
        //        catch (Exception ex)
        //        {
        //            Helper.Log(_logger, LogLevel.Error, $"Crypto  {coin.Coin} check error {ex.Source} {ex.StackTrace} {ex?.InnerException} {ex.Message}.");
        //            continue;
        //        }                

        //        //just trade
        //        Helper.Log(_logger, LogLevel.Warning, $"Crypto {coin.Coin} - ({coin.Name}) Time since traded hrs {Math.Abs(Math.Round(((Helper.easternTime - coin.DatePurchased.Value).TotalHours - 24), 5))}. Traded Price ${coin.PricePurchased} traded direction {Enum.GetName(typeof(TradeDirection), coin.TradeDirection)} crypto current price ${Convert.ToDecimal(results["lastPrice"].ToString())}");
        //        //var accounts = services.GetAccounts().Where(x => Convert.ToDecimal(x.AvailableBalance.Value) > 0).ToList();                

        //        if (st != null)
        //            AnalyiseOwnedCrypto(st, results, products);
        //       System.Threading.Thread.Sleep(250);
        //    }
        //}
        //void QueueCryptotoBuy(dynamic order, TradeDirection direction, decimal amount)
        //{
        //    string symbol = order["Symbol"].ToString();
        //    var duplicateQueue = ctx?.CryptoTracker2.Where(x => x.Coin == symbol && x.BuyStatus == "QUEUED").FirstOrDefault();
        //    if (duplicateQueue != null)
        //        return;
        //    SaveCryptoTracker(null, direction, amount, order, null, "QUEUED");
        //}
        //void CheckforQueuedCrypto()
        //{
        //    int? longCnt = 0;
        //    int? shortCnt = 0;
        //    var queuedCrypto = ctx.CryptoTracker2.Where(x => x.BuyStatus == "QUEUED").ToList();
        //    foreach (var crypto in queuedCrypto)
        //    {
        //        JsonObject? results = services.GetQuote(crypto.Coin);
        //        helper.CalculatePoints(results, /*false, true,*/ ref shortCnt, ref longCnt);
        //        if (longCnt >= settings.CryptoLongCount /*|| shortCnt >= settings.CryptoShortCount*/)
        //        {
        //            BuySellCrypto(crypto, results,(TradeDirection)crypto.TradeDirection, crypto.Amount ?? 0m, services, products, false/*, true*/);
        //        }
        //        else
        //        {
        //            SaveCryptoTracker(crypto, (TradeDirection)crypto.TradeDirection, crypto.Amount ?? 0m, results, null, "CANCEL"/*, true*/);
        //        }
        //    }
        //}
        //bool BuySellCrypto(CryptoTracker2 cryptoTracker, dynamic results, TradeDirection direction, decimal amount, CoinBaseService service, List<PublicProduct> publicProducts, bool sellShort)
        //{
        //    string symbol = (results["Symbol"].ToString().Substring((results["Symbol"].ToString().Length - 1), 1) == "C") ? results["Symbol"].ToString().Substring(0, (results["Symbol"].ToString().Length - 1)) : results["Symbol"].ToString();
        //    PublicProduct? product = publicProducts.Where(x => x.DisplayName == symbol).FirstOrDefault();
        //    string precision = product.BaseIncrement.Substring((product.BaseIncrement.IndexOf(".") + 1), (product.BaseIncrement.Length - (product.BaseIncrement.IndexOf(".") + 1)));
        //    string[] currency = product.DisplayName.Split('-');
        //    var account = coinBaseService.GetAccounts().Where(x => x.Currency == currency[0] && Convert.ToDecimal(x.AvailableBalance.Value) > 0m).FirstOrDefault();
        //    decimal minSubtract = 0m;
        //    decimal baseSize = 0m;

        //    Coinbase.AdvancedTrade.Models.Order output = null;
        //    Coinbase.AdvancedTrade.Enums.OrderSide coinbaseDirection = (direction == TradeDirection.BUY || direction == TradeDirection.BUY_TO_COVER) ? Coinbase.AdvancedTrade.Enums.OrderSide.BUY : Coinbase.AdvancedTrade.Enums.OrderSide.SELL;
        //    switch (direction)
        //    {
        //        case TradeDirection.BUY:

        //            var perItem = amount.ToString();
        //            var amountSize = Convert.ToDecimal(perItem.Substring(0, ((perItem.Length - perItem.IndexOf(".")) - 1)));
        //            output = coinBaseService.SetOrder(product.DisplayName, coinbaseDirection, amountSize);
        //            break;
        //        case TradeDirection.SELL:

        //            switch (precision.Length)
        //            {
        //                case 1:
        //                    minSubtract = .1m;
        //                    break;
        //                case 2:
        //                    minSubtract = .01m;
        //                    break;
        //                case 3:
        //                    minSubtract = .001m;
        //                    break;
        //                case 4:
        //                    minSubtract = .0001m;
        //                    break;
        //                case 5:
        //                    minSubtract = .00001m;
        //                    break;
        //                case 6:
        //                    minSubtract = .000001m;
        //                    break;
        //                case 7:
        //                    minSubtract = .0000001m;
        //                    break;
        //                case 8 or 9:
        //                    minSubtract = .00000001m;
        //                    break;
        //                //case 9:
        //                //    minSubtract = .000000001m;
        //                //    break;
        //            }
        //            baseSize = decimal.Round(Convert.ToDecimal(account.AvailableBalance.Value), precision.Length) - minSubtract;                    
        //            output = coinBaseService.SetOrder(product.DisplayName, coinbaseDirection, baseSize);
        //            if(output == null)
        //            {
        //                baseSize = (int)Convert.ToDecimal(account.AvailableBalance.Value);
        //                output = coinBaseService.SetOrder(product.DisplayName, coinbaseDirection, baseSize);
        //            }
        //            break;
        //    }
        //    if (output != null)
        //    {
        //        Helper.Log(_logger, LogLevel.Information, $"Crypto trade results for {symbol}, {output.Status}");
        //    }
        //    else
        //    {
        //        Helper.Log(_logger, LogLevel.Error, $"Crypto trade results order {symbol} failed.");
        //    }

        //    if (output != null && (output.Status.ToLower() == "filled" || output.Status.ToLower() == "open" || output.Status.ToLower() == "pending"))
        //        SaveCryptoTracker(cryptoTracker, direction, amount, results, output, output.Status);
        //    //SaveCryptoTracker(cryptoTracker, direction, amount, order, output, (direction == TradeDirection.BUY) ? "FILLED" : null/*, IsUpdate*/);
        //    return true;
        //}
        bool SaveCryptoTracker(CryptoTracker2 cryptoTracker, TradeDirection direction, decimal currentBalance, dynamic results, Coinbase.AdvancedTrade.Models.Order order, string buyStatus)
        {
            bool isNew = false;
            if (cryptoTracker != null && (direction == TradeDirection.SELL || direction == TradeDirection.BUY_TO_COVER))
            {
                cryptoTracker.SoldRsi = Convert.ToDecimal(results["RSI"].ToString());
                cryptoTracker.SoldEma9 = Convert.ToDecimal(results["EMA9"].ToString());
                cryptoTracker.SoldEma12 = Convert.ToDecimal(results["EMA12"].ToString());
                cryptoTracker.SoldEma26 = Convert.ToDecimal(results["EMA26"].ToString());
                cryptoTracker.SoldEma3 = Convert.ToDecimal(results["EMA3"].ToString());
                cryptoTracker.SoldEma10 = Convert.ToDecimal(results["EMA10"].ToString());
                cryptoTracker.SoldEma16 = Convert.ToDecimal(results["EMA16"].ToString());
                cryptoTracker.SoldEma5 = Convert.ToDecimal(results["EMA5"].ToString());
                cryptoTracker.SoldEma8 = Convert.ToDecimal(results["EMA8"].ToString());
                cryptoTracker.SoldEma13 = Convert.ToDecimal(results["EMA13"].ToString());
                cryptoTracker.SoldSma9 = Convert.ToDecimal(results["SMA9"].ToString());
                cryptoTracker.SoldSma12 = Convert.ToDecimal(results["SMA12"].ToString());
                cryptoTracker.SoldSma26 = Convert.ToDecimal(results["SMA26"].ToString());
                cryptoTracker.SoldSma3 = Convert.ToDecimal(results["SMA3"].ToString());
                cryptoTracker.SoldSma10 = Convert.ToDecimal(results["SMA10"].ToString());
                cryptoTracker.SoldSma16 = Convert.ToDecimal(results["SMA16"].ToString());
                cryptoTracker.SoldSma5 = Convert.ToDecimal(results["SMA5"].ToString());
                cryptoTracker.SoldSma8 = Convert.ToDecimal(results["SMA8"].ToString());
                cryptoTracker.SoldSma13 = Convert.ToDecimal(results["SMA13"].ToString());
                cryptoTracker.SoldMacd = Decimal.Parse(results["MACD"].ToString(), NumberStyles.Float,
            CultureInfo.InvariantCulture);
                cryptoTracker.SoldMacdSignal = Decimal.Parse(results["MACDSignal"].ToString(), NumberStyles.Float,
            CultureInfo.InvariantCulture);
                cryptoTracker.SoldMacdHistogram = Decimal.Parse(results["MACDHistogram"].ToString(), NumberStyles.Float,
            CultureInfo.InvariantCulture);
                cryptoTracker.CurrentBalance = /*(direction == TradeDirection.SELL) ? cryptoTracker.CurrentBalance :*/ currentBalance;                
                cryptoTracker.SellOrderId = (order != null) ? Guid.Parse(order?.OrderId): null;
                cryptoTracker.SellStatus = (order != null) ? order.Status: null;
                cryptoTracker.DateSold = Helper.easternTime;
                cryptoTracker.DateUpdated = Helper.easternTime;
                cryptoTracker.PriceSold = Convert.ToDecimal(results["lastPrice"].ToString());
                cryptoTracker.LastPrice = Convert.ToDecimal(results["lastPrice"].ToString());
                _ctx?.CryptoTracker2.Attach(cryptoTracker);
                _ctx.Entry(cryptoTracker).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
            }
            else
            {
                if (cryptoTracker == null)
                {
                    cryptoTracker = new CryptoTracker2();
                    isNew = true;
                }                    
                cryptoTracker.Coin = results["Symbol"].ToString();
                cryptoTracker.BuyOrderId = (order != null) ? Guid.Parse((order?.OrderId ?? null)): null;
                //cryptoTracker.BuyStatus = (order != null) ? order.Status: buyStatus;
                cryptoTracker.Name = results["Name"].ToString();
                cryptoTracker.TradeDirection = (int)direction;
                cryptoTracker.PricePurchased = Convert.ToDecimal(results["lastPrice"].ToString());
                cryptoTracker.Amount = currentBalance;
                cryptoTracker.PurchasedRsi = Convert.ToDecimal(results["RSI"].ToString());
                cryptoTracker.PurchasedEma9 = Convert.ToDecimal(results["EMA9"].ToString());
                cryptoTracker.PurchasedEma12 = Convert.ToDecimal(results["EMA12"].ToString());
                cryptoTracker.PurchasedEma26 = Convert.ToDecimal(results["EMA26"].ToString());
                cryptoTracker.PurchasedEma3 = Convert.ToDecimal(results["EMA3"].ToString());
                cryptoTracker.PurchasedEma10 = Convert.ToDecimal(results["EMA10"].ToString());
                cryptoTracker.PurchasedEma16 = Convert.ToDecimal(results["EMA16"].ToString());
                cryptoTracker.PurchasedEma5 = Convert.ToDecimal(results["EMA5"].ToString());
                cryptoTracker.PurchasedEma8 = Convert.ToDecimal(results["EMA8"].ToString());
                cryptoTracker.PurchasedEma13 = Convert.ToDecimal(results["EMA13"].ToString());
                cryptoTracker.PurchasedSma9 = Convert.ToDecimal(results["SMA9"].ToString());
                cryptoTracker.PurchasedSma12 = Convert.ToDecimal(results["SMA12"].ToString());
                cryptoTracker.PurchasedSma26 = Convert.ToDecimal(results["SMA26"].ToString());
                cryptoTracker.PurchasedSma3 = Convert.ToDecimal(results["SMA3"].ToString());
                cryptoTracker.PurchasedSma10 = Convert.ToDecimal(results["SMA10"].ToString());
                cryptoTracker.PurchasedSma16 = Convert.ToDecimal(results["SMA16"].ToString());
                cryptoTracker.PurchasedSma5 = Convert.ToDecimal(results["SMA5"].ToString());
                cryptoTracker.PurchasedSma8 = Convert.ToDecimal(results["SMA8"].ToString());
                cryptoTracker.PurchasedSma13 = Convert.ToDecimal(results["SMA13"].ToString());
                cryptoTracker.PurchasedMacd = Decimal.Parse(results["MACD"].ToString(), NumberStyles.Float,
            CultureInfo.InvariantCulture);
                cryptoTracker.PurchasedMacdSignal = Decimal.Parse(results["MACDSignal"].ToString(), NumberStyles.Float,
            CultureInfo.InvariantCulture);
                cryptoTracker.PurchasedMacdHistogram = Decimal.Parse(results["MACDHistogram"].ToString(), NumberStyles.Float,
            CultureInfo.InvariantCulture);
                cryptoTracker.LastPrice = Convert.ToDecimal(results["lastPrice"].ToString());
                cryptoTracker.DatePurchased = Helper.easternTime;
                cryptoTracker.DateCreated = Helper.easternTime;
                cryptoTracker.LastUpdatedBy = 1;
                cryptoTracker.BuyStatus = (order != null) ? order.Status:(!string.IsNullOrEmpty(buyStatus)) ? buyStatus:null;
                if(isNew) {
                    _ctx.CryptoTracker2.Add(cryptoTracker);
                    _ctx.Entry(cryptoTracker).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                } else
                {
                    _ctx.CryptoTracker2.Attach(cryptoTracker);
                    _ctx.Entry(cryptoTracker).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                }
            }
            try
            {
                _ctx.SaveChanges();
                _ctx.Entry(cryptoTracker).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
            }
            catch (Exception ex)
            {
                Helper.Log(_logger, LogLevel.Error, $"Source: {ex.Source} stack trace: {ex.StackTrace} inner exception {ex?.InnerException?.Message ?? ""} error: {ex.Message}");
            }
            
            return true;
        }
        //bool UpdateLastPrice(decimal lastPrice, decimal Balance, ref CryptoTracker2 cryptoTracker)
        //{
        //    _ctx = new TmdContext();
        //    if ((cryptoTracker.TradeDirection == (int)TradeDirection.BUY && (lastPrice > cryptoTracker.LastPrice && lastPrice > cryptoTracker.PricePurchased)) || cryptoTracker.LastPrice is null)
        //    {
        //        cryptoTracker.LastPrice = lastPrice;
        //    }
        //    if ((cryptoTracker.TradeDirection == (int)TradeDirection.SELL_SHORT && (lastPrice < cryptoTracker.LastPrice && lastPrice < cryptoTracker.PricePurchased)) || cryptoTracker.LastPrice is null)
        //    {
        //        cryptoTracker.LastPrice = lastPrice;
        //    }
        //    cryptoTracker.CurrentBalance = Balance;
        //    cryptoTracker.DateUpdated = Helper.easternTime;
        //    try
        //    {
        //        _ctx.Entry(cryptoTracker).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
        //        System.Threading.Thread.Sleep(200);
        //        _ctx.SaveChanges();
        //        _ctx.Entry(cryptoTracker).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
        //    }
        //    catch (Exception ex)
        //    {
        //        Helper.Log(_logger, LogLevel.Error, $"Price Update Failed! {ex.Message}");
        //    }
        //    return true;
        //}

        //void AnalyiseOwnedCrypto(CryptoTracker2 crypto, JsonObject? results, List<PublicProduct> products)
        //{
        //    //calculate percent difference
        //    decimal percentDiffPricePurchased = 0m;
        //    decimal percentDiffLastPrice = 0m;
        //    decimal lastPrice = (crypto.LastPrice ?? 0);
        //    decimal pricePurchased = (crypto.PricePurchased ?? 0);
        //    decimal currentPrice = Convert.ToDecimal(results["lastPrice"].ToString());

        //    var product = products.Where(x => x.DisplayName == crypto.Coin).FirstOrDefault();
        //    var account = coinBaseService?.GetAccounts().Where(x => x.Currency == crypto.Coin.Split(new char[] { '-' })[0] && Convert.ToDecimal(x.AvailableBalance.Value) > 0m).FirstOrDefault();
        //    var currentBalance = Math.Round(Convert.ToDecimal(account?.AvailableBalance?.Value) * Convert.ToDecimal(product?.Price), 5);
        //    //var currentBalance = Math.Round(Convert.ToDecimal(account?.AvailableBalance?.Value), 5); //* Convert.ToDecimal(product?.Price), 5);
        //    //if (crypto.Coin == "POPCAT-USD")
        //    //{
        //    //    //bobo
        //    //}
        //    try
        //    {
        //        percentDiffPricePurchased = Math.Abs(((pricePurchased - currentPrice) / ((pricePurchased + currentPrice) / 2)) * 100);
        //        percentDiffLastPrice = Math.Abs(((lastPrice - currentPrice) / ((lastPrice + currentPrice) / 2)) * 100);
        //    }
        //    catch (Exception ex)
        //    {
        //        Helper.Log(_logger, LogLevel.Error, $"{ex.Message} at AnalyiseOwnedStock Percent Diff. Setting to zero.");
        //    }
        //    //Short
        //    //if (crypto.TradeDirection == (int)TradeDirection.SELL_SHORT) //selling short
        //    //{
        //    //    if (currentPrice > pricePurchased)
        //    //    {
        //    //        if (percentDiffPricePurchased > settings.StopLossPercentPurchasedCrypto)
        //    //        {
        //    //            //buy to cover
        //    //            _helper.Log(_logger, LogLevel.Information, $"crypto {crypto.Coin} - ({crypto.Name}) has fallen by more then about {Math.Round(percentDiffPricePurchased, 5)}% since price purchased. Buying to cover.");
        //    //            BuySellCrypto(crypto, results, TradeDirection.BUY_TO_COVER, Math.Round(Convert.ToDecimal(order.FilledSize), 5), services, products, false);
        //    //        }
        //    //        else
        //    //        {
        //    //            //Update Price
        //    //            _helper.Log(_logger, LogLevel.Information, $"crypto {crypto.Coin} - ({crypto.Name}) still within about {Math.Round(percentDiffPricePurchased, 5)}% tolerance since price purchased. Updating last Price.");
        //    //            UpdateLastPrice(currentPrice, balance, ref crypto);
        //    //        }
        //    //    }
        //    //    else
        //    //    if (currentPrice > lastPrice)
        //    //    {
        //    //        if (percentDiffLastPrice > settings.StopLossPercentLastPriceCrypto)
        //    //        {
        //    //            //buy to cover
        //    //            _helper.Log(_logger, LogLevel.Information, $"crypto {crypto.Coin} - ({crypto.Name}) has fallen by more then about {Math.Round(percentDiffLastPrice, 5)}% since last price. Buying to cover.");
        //    //            BuySellCrypto(crypto, results, TradeDirection.BUY_TO_COVER, Math.Round(Convert.ToDecimal(order.FilledSize), 5), services, products, false);
        //    //        }
        //    //        else
        //    //        {
        //    //            //Update Price
        //    //            //_helper.Log(_logger, LogLevel.Information, $"crypto {crypto.Coin} - ({crypto.Name}) still within about {Math.Round(percentDiffLastPrice, 5)}% tolerance since last price. Updating last Price.");
        //    //            //UpdateLastPrice(currentPrice, balance, ref crypto);
        //    //        }
        //    //    }
        //    //    else //if (currentPrice <= lastPrice)
        //    //    {
        //    //            //update price
        //    //            _helper.Log(_logger, LogLevel.Information, $"crypto {crypto.Coin} - ({crypto.Name}) still within about {Math.Round(percentDiffLastPrice, 5)}% tolerance since last price. Updating last Price.");
        //    //            UpdateLastPrice(currentPrice, balance, ref crypto);
        //    //    }
        //    //}
        //    //else //Going Long
        //    //{
        //        //if (currentPrice < pricePurchased)
        //        //{
        //        //    if (percentDiffPricePurchased > settings.StopLossPercentPurchasedCrypto)
        //        //    {
        //        //        //buy to cover
        //        //        Helper.Log(_logger, LogLevel.Information, $"crypto  {crypto.Coin}  - ( {crypto.Name}) has fallen by more then about {Math.Round(percentDiffPricePurchased, 5)}% since price purchased. Selling.");
        //        //        UpdateLastPrice(currentPrice, currentBalance, ref crypto);
        //        //        BuySellCrypto(crypto, results, TradeDirection.SELL, currentBalance, coinBaseService, products, false/*, false*/);
        //        //    }
        //        //    else
        //        //    {
        //        //        //Update Price
        //        //        Helper.Log(_logger, LogLevel.Information, $"crypto  {crypto.Coin}  - ( {crypto.Name}) still within about {Math.Round(percentDiffPricePurchased, 5)}% tolerance since price purchased. Updating last Price.");
        //        //        UpdateLastPrice(currentPrice, currentBalance, ref crypto);
        //        //    }
        //        //}
        //        //else
        //        //if (currentPrice < lastPrice && percentDiffLastPrice >= settings.StopLossPercentLastPriceCrypto && percentDiffPricePurchased >= 3.5m)
        //        if ((currentPrice < lastPrice) &&
        //        (pricePurchased < currentPrice) &&                
        //        (percentDiffPricePurchased >= 3.5m)) 
        //        //if (currentPrice < lastPrice && currentBalance > (crypto.Amount * 1.08m))
        //        {
        //            //if (percentDiffLastPrice > settings.StopLossPercentLastPriceCrypto)
        //            //{
        //                //sell
        //                Helper.Log(_logger, LogLevel.Information, $"crypto {crypto.Coin} - ({crypto.Name}) has fallen by more then about {Math.Round(percentDiffLastPrice, 5)}% since last Price. Selling.");
        //                UpdateLastPrice(currentPrice, currentBalance, ref crypto);
        //        _helper.BuySellCrypto(crypto, results, TradeDirection.SELL, currentBalance, coinBaseService, products, false/*, false*/);
        //            //}
        //            //else
        //            //{
        //                //Update Price
        //                //_helper.Log(_logger, LogLevel.Information, $"crypto {crypto.Coin} - ({crypto.Name}) still within about {Math.Round(percentDiffLastPrice, 5)}% tolerance since last Price. Updating last Price.");
        //                //UpdateLastPrice(currentPrice, balance, ref crypto);
        //            //}
        //        }
        //        else //if (currentPrice >= lastPrice)
        //        {
        //                //update price
        //                Helper.Log(_logger, LogLevel.Information, $"crypto {crypto.Coin} - ({crypto.Name}) still within about {Math.Round(percentDiffLastPrice, 5)}% tolerance since last price. Updating last Price.");
        //                UpdateLastPrice(currentPrice, currentBalance, ref crypto);
        //        }
        //    //}
        //}
       
        //void CalculatePurchaseAmount(decimal? availiabletoTrade, List<PublicProduct> products, IQueryable<CryptoTracker2> cryptoTracker, ref List<BuySellCommodity> BuySellCryptoList)
        //{
        //    //var alreadyQueuedStock = ctx?.CryptoTracker2?.Where(x => x.BuyStatus == "QUEUED").ToList();
        //    if (cryptoTracker.Count() >= settings.MaxNumberofCryptotoBuy)
        //        return;
        //    int cryptoCnt = 0;
        //    var totalCanBuy = (BuySellCryptoList.Count() > (settings.MaxNumberofCryptotoBuy - cryptoTracker.Count())) ? (settings.MaxNumberofCryptotoBuy - cryptoTracker.Count()) : BuySellCryptoList.Count();
        //    //decimal? perCrypto = (availiabletoTrade / BuySellCryptoList.Count()) > settings.MaxDollarAmountPerCrypto ? (availiabletoTrade < settings.MaxDollarAmountPerCrypto) ? availiabletoTrade : settings.MaxDollarAmountPerCrypto : (availiabletoTrade / BuySellCryptoList.Count());
        //    decimal? perCrypto = (availiabletoTrade / totalCanBuy) > settings.MaxDollarAmountPerCrypto ? (availiabletoTrade < settings.MaxDollarAmountPerCrypto) ? availiabletoTrade : settings.MaxDollarAmountPerCrypto : (availiabletoTrade / totalCanBuy);
        //    foreach (var item in BuySellCryptoList)
        //    {
        //        if ((cryptoTracker.Count() + cryptoCnt) >= settings.MaxNumberofCryptotoBuy)
        //            break;

        //        var result = item.Results;
        //        var direction = item.Direction;
        //        bool sellShort = (direction == TradeDirection.SELL_SHORT) ? true : false;
        //        if (perCrypto >= 5)
        //        {
        //            BuySellCrypto(null, result, direction,Math.Round((decimal)perCrypto, 5), services, products, sellShort);
        //            //if (!alreadyQueuedStock.Where(x => x.Coin == result["Symbol"].ToString()).Any())
        //            //    QueueCryptotoBuy(result, direction, Math.Round((decimal)perCrypto, 5));
        //            cryptoCnt++;
        //        }
        //    }
        //}

        void CalculatePurchaseAmount2(decimal? availiabletoTrade, List<PublicProduct> products, IQueryable<CryptoTracker2> cryptoTracker, ref List<BuySellCommodity> BuySellCryptoList)
        {
            if (cryptoTracker.Count() >= settings.MaxNumberofCryptotoBuy)
                return;
            int cryptoCnt = 0;
            var totalCanBuy = (BuySellCryptoList.Count() > (settings.MaxNumberofCryptotoBuy - cryptoTracker.Count())) ? (settings.MaxNumberofCryptotoBuy - cryptoTracker.Count()) : BuySellCryptoList.Count();
            //decimal? perCrypto = (availiabletoTrade / totalCanBuy) > settings.MaxDollarAmountPerCrypto ? (availiabletoTrade < settings.MaxDollarAmountPerCrypto) ? availiabletoTrade : settings.MaxDollarAmountPerCrypto : (availiabletoTrade / totalCanBuy);
            decimal? perCrypto = 3m;
            foreach (var item in BuySellCryptoList)
            {
                if ((cryptoTracker.Count() + cryptoCnt) >= settings.MaxNumberofCryptotoBuy)
                    break;

                var result = item.Results;
                var direction = item.Direction;
                //bool sellShort = (direction == TradeDirection.SELL_SHORT) ? true : false;
                //if (perCrypto >= 5)
                //{
                _helper.BuySellCrypto(null, result, direction, Math.Round((decimal)perCrypto, 5), coinBaseService, products/*, sellShort*/);
                    //if (!alreadyQueuedStock.Where(x => x.Coin == result["Symbol"].ToString()).Any())
                    //    QueueCryptotoBuy(result, direction, Math.Round((decimal)perCrypto, 5));
                    cryptoCnt++;
                //}
            }
        }
        //public void ClearOrphanWallets()
        //{
        //    CryptoTracker2? coin = null;
        //    Order output = null;
        //    //int lengthAfterDecimal = 0;
        //    Guid? buyOrderId = null;
        //    var Accounts = coinBaseService.GetAccounts();/*.Where(x => x.Currency != "USD")*///.ToList();
        //    Helper.Log(_logger, LogLevel.Information, $"Getting owned crypto list");
        //    var PublicProducts = coinBaseService.GetProducts().OrderBy(x => x.DisplayName);//.ToList();
        //    //check if there is a wallet with cash not in tracker
        //    foreach (var item in Accounts)
        //    {
        //        Helper.Log(_logger, LogLevel.Information, $"Iterating crypto list. Analyzing {item.Currency}");
        //        //if (item.Currency.Contains("ETH") /*|| item.Currency == "LTC"*/)
        //        //{
        //        //    //they do exist
        //        //}
        //        var publicProduct = PublicProducts.Where(x => x.DisplayName == item.Currency + "-USD").FirstOrDefault();
        //        var valueofNonTrackedAsset = Math.Round(Convert.ToDecimal(item.AvailableBalance.Value) * Convert.ToDecimal(publicProduct?.Price), 9);
        //        if (valueofNonTrackedAsset == .0m)
        //        {
        //            Helper.Log(_logger, LogLevel.Information, $"{item.Currency} no available balance.");
        //            continue;
        //        }
        //        if (valueofNonTrackedAsset >.0m && valueofNonTrackedAsset <= .80m)
        //        {
        //            Helper.Log(_logger, LogLevel.Warning, $"{item.Currency} too low.");
        //            continue;
        //        }else
        //        if (valueofNonTrackedAsset > .80m && valueofNonTrackedAsset <= 1.5m)
        //        {
        //            decimal baseSize = 0m;
        //            var precision = publicProduct?.BaseIncrement.Substring((publicProduct.BaseIncrement.IndexOf(".") + 1), (publicProduct.BaseIncrement.Length - (publicProduct.BaseIncrement.IndexOf(".") + 1)));
        //            var currency = publicProduct?.DisplayName.Split('-');
        //            var account = coinBaseService.GetAccounts().Where(x => x.Currency == currency[0]).FirstOrDefault();
        //            if (account?.AvailableBalance.Value.IndexOf(".") <= -1)
        //                baseSize = Convert.ToDecimal(account.AvailableBalance.Value);
        //            else
        //                baseSize = Convert.ToDecimal(account?.AvailableBalance.Value.Substring(0, (account.AvailableBalance.Value.IndexOf(".") + precision.Length)));
        //            if (baseSize <= .0m)
        //                continue;
        //            output = coinBaseService.SetOrder(publicProduct?.DisplayName, Coinbase.AdvancedTrade.Enums.OrderSide.SELL, baseSize);
        //            Helper.Log(_logger, LogLevel.Warning, $"{item.Currency} too low.");
        //            continue;
        //        }else
        //        if (valueofNonTrackedAsset > 1.5m)
        //        {
        //            coin = _ctx.CryptoTracker2.AsNoTracking().Where(x => x.Coin == item.Currency + "-USD" && x.PriceSold == null && x.DateSold == null && (x.BuyStatus.ToLower() == "open" || x.BuyStatus.ToLower() == "pending")).FirstOrDefault();
        //            Helper.Log(_logger, LogLevel.Information, $"{item.Currency} meets min amount checking if synced with db.");
        //        }

        //        //var coin = Cryptos.Where(x => x.Coin == item.Currency + "-USD").ToList();
        //        if (coin is not null)
        //        {
        //            Helper.Log(_logger, LogLevel.Information, $"{item.Currency} found in db pending");
        //            var crypto = coin; 
        //            if(crypto.BuyOrderId == null)
        //            {
        //                Helper.Log(_logger, LogLevel.Information, $"{item.Currency} BuyOrderId missing. Getting by last order.");
        //                //var results = services.GetOrders(crypto.Coin);
        //                //var lastSell = results.Where(x => x.Side == "BUY").OrderByDescending(y => Convert.ToDateTime(y.SequenceTimestamp)).FirstOrDefault();
        //                var lastSell = GetLastOrder(crypto.Coin);
        //                buyOrderId = Guid.Parse(lastSell?.OrderId);
        //                crypto.BuyOrderId = buyOrderId;
        //            }
        //            else
        //            {
        //                Helper.Log(_logger, LogLevel.Information, $"{item.Currency} BuyOrderId found.");
        //                buyOrderId = crypto.BuyOrderId;
        //            }

        //            Helper.Log(_logger, LogLevel.Information, $"{item.Currency} Getting latest order info and updating.");
        //            var buyOrder = coinBaseService.GetOrder((Guid)buyOrderId);
        //            //var results = coinBaseService.GetQuote(crypto.Coin);
        //            crypto.PricePurchased = Convert.ToDecimal(buyOrder.AverageFilledPrice);
        //            //crypto.LastPrice = Convert.ToDecimal(results["lastPrice"].ToString());
        //            crypto.CurrentBalance = valueofNonTrackedAsset;
        //            crypto.DateUpdated = Helper.easternTime;
        //            crypto.DatePurchased = item.UpdatedAt;
        //            crypto.BuyStatus = (buyOrder.Status ?? "FILLED");
        //            _ctx.CryptoTracker2.Attach(crypto);
        //            _ctx.Entry(crypto).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
        //            //_ctx.SaveChanges();
        //            //_ctx.Entry(crypto).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
        //            Helper.Log(_logger, LogLevel.Information, $"{item.Currency} Updating in db.");
        //        }
        //        else
        //        {
        //            Helper.Log(_logger, LogLevel.Information, $"{item.Currency} not found pending. Checking filled.");
        //            coin = _ctx.CryptoTracker2.AsNoTracking().Where(x => x.Coin == item.Currency + "-USD" && x.PriceSold == null && x.DateSold == null && x.BuyStatus.ToLower() == "filled").FirstOrDefault();
        //            if (coin is not null)
        //            {
        //                Helper.Log(_logger, LogLevel.Information, $"{item.Currency} found in db filled.");
        //                var crypto = coin;
        //                if (crypto.BuyOrderId == null)
        //                {
        //                    Helper.Log(_logger, LogLevel.Information, $"{item.Currency} BuyOrderId missing. Getting by last order.");
        //                    //var results2 = services.GetOrders(crypto.Coin);
        //                    //var lastSell = results2.Where(x => x.Side == "BUY").OrderByDescending(y => Convert.ToDateTime(y.SequenceTimestamp)).FirstOrDefault();
        //                    crypto.CurrentBalance = valueofNonTrackedAsset;
        //                    var lastSell = GetLastOrder(crypto.Coin);
        //                    buyOrderId = Guid.Parse(lastSell?.OrderId);
        //                    crypto.BuyOrderId = buyOrderId;
        //                    crypto.DatePurchased = item.UpdatedAt;
        //                    _ctx.CryptoTracker2.Attach(crypto);
        //                    _ctx.Entry(crypto).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
        //                    //_ctx.SaveChanges();
        //                    //_ctx.Entry(crypto).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
        //                }
        //                //continue; //already in tracker. Update pruchased date
        //                else {
        //                    crypto.CurrentBalance = valueofNonTrackedAsset;
        //                    crypto.DatePurchased = item.UpdatedAt;
        //                    _ctx.CryptoTracker2.Attach(crypto);
        //                    _ctx.Entry(crypto).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
        //                    //_ctx.SaveChanges();
        //                    //_ctx.Entry(crypto).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
        //                }
        //                Helper.Log(_logger, LogLevel.Information, $"{item.Currency} Updating in db.");
        //            }
        //            else
        //            {
        //                Helper.Log(_logger, LogLevel.Information, $"{item.Currency} Not found pending or filled. Creating new.");
        //                //create new crypto tracker
        //                coin = new CryptoTracker2();
        //                var results = coinBaseService.GetQuoteAsync(item.Currency + "-USD").Result;
        //                var lastSell = GetLastOrder(item.Currency + "-USD");
        //                buyOrderId = Guid.Parse(lastSell?.OrderId);
        //                coin.BuyOrderId = buyOrderId;
        //                coin.Coin = results["Symbol"].ToString();
        //                //coin.BuyOrderId = Guid.Parse(order.OrderId);
        //                coin.BuyStatus = "FILLED";
        //                coin.Name = results["Name"].ToString();
        //                coin.TradeDirection = 1;
        //                coin.PricePurchased = Convert.ToDecimal(results["lastPrice"].ToString());
        //                coin.Amount = valueofNonTrackedAsset;
        //                coin.PurchasedRsi = Convert.ToDecimal(results["RSI"].ToString());
        //                coin.PurchasedEma9 = Convert.ToDecimal(results["EMA9"].ToString());
        //                coin.PurchasedEma12 = Convert.ToDecimal(results["EMA12"].ToString());
        //                coin.PurchasedEma26 = Convert.ToDecimal(results["EMA26"].ToString());
        //                coin.PurchasedEma3 = Convert.ToDecimal(results["EMA3"].ToString());
        //                coin.PurchasedEma10 = Convert.ToDecimal(results["EMA10"].ToString());
        //                coin.PurchasedEma16 = Convert.ToDecimal(results["EMA16"].ToString());
        //                coin.PurchasedEma5 = Convert.ToDecimal(results["EMA5"].ToString());
        //                coin.PurchasedEma8 = Convert.ToDecimal(results["EMA8"].ToString());
        //                coin.PurchasedEma13 = Convert.ToDecimal(results["EMA13"].ToString());
        //                coin.PurchasedSma9 = Convert.ToDecimal(results["SMA9"].ToString());
        //                coin.PurchasedSma12 = Convert.ToDecimal(results["SMA12"].ToString());
        //                coin.PurchasedSma26 = Convert.ToDecimal(results["SMA26"].ToString());
        //                coin.PurchasedSma3 = Convert.ToDecimal(results["SMA3"].ToString());
        //                coin.PurchasedSma10 = Convert.ToDecimal(results["SMA10"].ToString());
        //                coin.PurchasedSma16 = Convert.ToDecimal(results["SMA16"].ToString());
        //                coin.PurchasedSma5 = Convert.ToDecimal(results["SMA5"].ToString());
        //                coin.PurchasedSma8 = Convert.ToDecimal(results["SMA8"].ToString());
        //                coin.PurchasedSma13 = Convert.ToDecimal(results["SMA13"].ToString());
        //                coin.PurchasedMacd = Convert.ToDecimal(results["MACD"].ToString());
        //                coin.PurchasedMacdSignal = Convert.ToDecimal(results["MACDSignal"].ToString());
        //                coin.PurchasedMacdHistogram = Convert.ToDecimal(results["MACDHistogram"].ToString());
        //                coin.LastPrice = Convert.ToDecimal(results["lastPrice"].ToString());
        //                coin.DatePurchased = item.UpdatedAt; ;
        //                coin.DateCreated = Helper.easternTime;
        //                coin.LastUpdatedBy = 1;
        //                _ctx.CryptoTracker2.Add(coin);
        //                _ctx.Entry(coin).State = Microsoft.EntityFrameworkCore.EntityState.Added;
        //                Helper.Log(_logger, LogLevel.Information, $"{item.Currency} Adding in db.");
        //            }
        //        }
        //        try
        //        {
        //            _ctx.SaveChanges();
        //            _ctx.Entry(coin).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
        //        }
        //        catch (Exception ex)
        //        {
        //            Helper.Log(_logger, LogLevel.Error, $"Source: {ex.Source} stack trace: {ex.StackTrace} inner exception {ex?.InnerException?.Message ?? ""} error: {ex.Message}");
        //        }

        //    }
        //    var Cryptos = _ctx.CryptoTracker2.AsNoTracking().Where(x => x.PriceSold == null && x.DateSold == null && x.BuyStatus == "FILLED");//.ToList();
        //    foreach(var crypto in Cryptos)
        //    {
        //        var currency = crypto.Coin.Split('-');
        //        var account = coinBaseService.GetAccounts().Where(x => x.Currency == currency[0]).FirstOrDefault();
        //        var publicProduct = coinBaseService.GetProducts().Where(x => x.DisplayName == crypto.Coin).FirstOrDefault();
        //        var valueofNonTrackedAsset = Math.Round(Convert.ToDecimal(account.AvailableBalance.Value) * Convert.ToDecimal(publicProduct?.Price), 9);
        //        //if (decimal.TryParse(account?.AvailableBalance.Value, out decimal bal) && bal <= 1.5m)
        //        if (/*valueofNonTrackedAsset > 0m &&*/ valueofNonTrackedAsset <= 1.5m)
        //        {
        //            var ct = crypto;
        //            var results = coinBaseService.GetQuoteAsync(ct.Coin).Result;
        //            ct.PriceSold = Convert.ToDecimal(results["lastPrice"].ToString());
        //            ct.SellOrderId = Guid.Empty;
        //            ct.SellStatus = "FILLED";
        //            ct.DateSold = Helper.easternTime;
        //            ct.DateUpdated = Helper.easternTime;
        //            _ctx.CryptoTracker2.Attach(ct);
        //            _ctx.Entry(ct).State = Microsoft.EntityFrameworkCore.EntityState.Deleted;
        //            _ctx.SaveChanges();
        //            _ctx.Entry(ct).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
        //        }
        //    }
        //}
        //private Fill GetLastOrder(string ProductId)
        //{
        //    var orders = coinBaseService.GetOrders(ProductId);
        //    var order = orders.Where(x => x.ProductId == ProductId).OrderByDescending(y => Convert.ToDateTime(y.SequenceTimestamp)).FirstOrDefault();
        //    return order;
        //}
        ~ExecCryptoPricePrediction()
        {
            coinBaseService = null;
            //ctx?.Dispose();
            //ctx = null;
            _helper = null;
        }
    }
}
