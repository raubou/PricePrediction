using Coinbase.AdvancedTrade.Models;
using Coinbase.AdvancedTrade.Models.Public;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using TMD.Services;
using TMD.Services.Service;
//using TMDInvestment.Models;
using TMDInvestments.Context;
using TMDInvestments.Models;


namespace TMD.PricePrediction.Classes
{
    public sealed class ExecCryptoPricePrediction //: IDisposable
    {
        ILogger? _logger;
        CoinBaseSettings? _settings; 
        TaAPISettings? _taapiSettings;
        CoinBaseService? services = null;
        IHttpClientFactory? _httpClientFactory;
        TaAPIService? taapiService = null;
        TmdContext? ctx = null;
        List<BuySellCommodity> BuySellCryptoList;
        RulesEngine? settings = null;
        List<CryptoTracker2> cryptoTracker = null;
        List<PublicProduct> products = null;
        Helper? helper = null;
        public ExecCryptoPricePrediction(ILogger logger, CoinBaseSettings settings, IHttpClientFactory httpClientFactory) {
            
            _logger = logger;
            _settings = settings;
            services = new CoinBaseService(logger, settings, _httpClientFactory);
            ctx = new TmdContext();
            helper = new Helper();
        }
        public ExecCryptoPricePrediction(ILogger logger, CoinBaseSettings settings, TaAPISettings taAPISettings, IHttpClientFactory httpClientFactory)
        {

            _logger = logger;
            _settings = settings;
            _taapiSettings = taAPISettings;
            services = new CoinBaseService(logger, settings, httpClientFactory);
            taapiService = new TaAPIService(_logger, _taapiSettings, _httpClientFactory);
            ctx = new TmdContext();
            helper = new Helper();
        }
        public void Exec()
        {
            Run();
        }
        private void Run()
        {
            //GetSettings
            settings = ctx.RulesEngine.FirstOrDefault();
            helper.Log(_logger, LogLevel.Information, $"Get settings");
            //_logger.LogInformation($"Get settings");

            //Clear Ophan wallet or crypto tracker
            ClearOrphanWallets();
            helper.Log(_logger, LogLevel.Information, $"Clearing Ophan wallet or crypto tracker");
            //update order status for filled and cancelled orders
            //CheckOrderStatus();
            //helper.Log(_logger, LogLevel.Information, $"Update order status for filled and cancelled orders");

            //Get Products
            products = services.GetProducts();

            //Update Prices
            helper.Log(_logger, LogLevel.Information, $"Updating Latest Prices on Tracker");
            UpdatePrices();
            //System.Threading.Thread.Sleep(1000);

            //Unload Aging Stocks
            helper.Log(_logger, LogLevel.Information, $"Unload aging stock");
            UnloadAgingStocks();

            //Get Account Info
            List<PortfolioBreakdown> portfolios = services.GetPortfolios();
            helper.Log(_logger, LogLevel.Information, $"Get account info");

            //Checked for queued crypto adn see if direction is the same
            helper.Log(_logger, LogLevel.Information, $"Check for queued crypto");
            CheckforQueuedCrypto(); 

            //Get Crypto already in trade being tracked.
            cryptoTracker = ctx.CryptoTracker2.Where(x => x.PriceSold == null && x.DateSold == null && (x.BuyStatus.ToLower() == "filled" /*|| x.BuyStatus.ToLower() == "open" || x.BuyStatus.ToLower() == "pending"*/)).ToList();
            var cryptoTrackerList = cryptoTracker.Select(y => y.Coin);
            helper.Log(_logger, LogLevel.Information, $"Get exiting Crypto tracker");

            decimal cash = portfolios.Sum(x => Convert.ToDecimal(x.Portfolio_balances.total_cash_equivalent_balance.value));
            decimal cryptoBalance = portfolios.Sum(x => Convert.ToDecimal(x.Portfolio_balances.total_crypto_balance.value));
            var totalAvailiableforLongTrade = Math.Round((cash * .89m), 5); //2% fee set aside
            //var totalAvailiableforLongTrade = Math.Round(cash, 2);
            //totalAvailiableforLongTrade = 180m;

            helper.Log(_logger, LogLevel.Information, $"Cash Balance ${cash}. Crypto Balance ${cryptoBalance}. Total Availiable ${totalAvailiableforLongTrade}");

            if (totalAvailiableforLongTrade > 30)
            {
                //Get list of Crypto to trade not in tracker
                var cryptoList = ctx.CryptoList.Where(x => (!cryptoTrackerList.Contains(x.Coin) && !cryptoTrackerList.Contains(x.Coin.Substring(0, (x.Coin.Length - 1)))) && !x.ExcludeFromAutomation).OrderBy(y => Guid.NewGuid()).ToList();
                //var cryptoList = ctx.CryptoList.Where(x => !x.ExcludeFromAutomation).ToList();
                helper.Log(_logger, LogLevel.Information, $"Get crypto list to trade");

                //Crypto sold/brought to cover in the last 2(4) hrs
                var CryptoHold24Hr = ctx.CryptoTracker2.ToList().Where(x => x.PriceSold != null && x.DateSold != null && Math.Abs((x.DateSold - helper.easternTime).Value.TotalHours) < 24).ToList().Select(y => new { Coin = y.Coin, DateSold = y.DateSold, DateUpdated = y.DateUpdated }).ToList();
                //var CryptoHold12Hr = ctx.CryptoTracker.ToList().Where(x => x.PriceSold != null && x.DateSold != null && Math.Abs((x.DateSold - DateTime.Now).Value.TotalHours) < 12).ToList().Select(y => new { Coin = y.Coin, DateSold = y.DateSold, DateUpdated = y.DateUpdated }).ToList();
                //var CryptoHold2Hr = ctx.CryptoTracker.ToList().Where(x => x.PriceSold != null && x.DateSold != null && Math.Abs((x.DateSold - DateTime.Now).Value.TotalHours) < 2).ToList().Select(y => new { Coin = y.Coin, DateSold = y.DateSold }).ToList();
                //var CryptoHold2Hr = ctx.CryptoTracker.ToList().Where(x => x.PriceSold != null && x.DateSold != null && Math.Abs((x.DateSold - DateTime.Now).Value.TotalHours) < 2).ToList().Select(y => new { Coin = y.Coin, DateSold = y.DateSold }).ToList();
                //Loop through and evaluate.
                int? shortCnt = 0;
                int? longCnt = 0;
                int? cryptoCnt = 0;
                int? rejectPriceCount = 0;

                helper.Log(_logger, LogLevel.Information, $"Iterate Crypto list, check indicators for buy/sell short");

                BuySellCryptoList = new List<BuySellCommodity>();

                if (cryptoTracker.Count < settings.MaxNumberofCryptotoBuy)
                foreach (var crypto in cryptoList)
                {
                    shortCnt = 0;
                    longCnt = 0;
                    if (crypto?.QuoteRetry >= 5)
                        continue;
                    //Skip if status is not filled
                    var status = cryptoTracker.Where(x => x.Coin == crypto.Coin).FirstOrDefault()?.BuyStatus?.ToLower();
                    if (status == "open" || status == "pending")
                        continue;


                    //Check it stock has been brought in the last 24 hrs
                    if (CryptoHold24Hr.Any(y => y.Coin == crypto.Coin))
                    {
                        //if stock was cancelled, check updated date
                        try
                        {
                            helper.Log(_logger, LogLevel.Warning, $"Crypto {crypto.Coin} - ({crypto.Name}) purchased {Math.Round(Math.Abs((CryptoHold24Hr.Where(y => y.Coin == crypto.Coin).Select(z => z.DateSold).FirstOrDefault() - helper.easternTime).Value.TotalHours), 5)} hours ago was purchased less than 24h ago, skipping");
                        }
                        catch (Exception ex)
                        {
                            helper.Log(_logger, LogLevel.Warning, $"Crypto {crypto.Coin} - ({crypto.Name}) cancelled {Math.Round(Math.Abs((CryptoHold24Hr.Where(y => y.Coin == crypto.Coin).Select(z => z.DateUpdated).FirstOrDefault() - helper.easternTime).Value.TotalHours), 5)} hours ago was purchased less than 24h ago, skipping");
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

                    var product = products.Where(x => x.DisplayName == crypto.Coin).FirstOrDefault();
                    if (product == null)
                    {
                        helper.Log(_logger, LogLevel.Error, $"Product - Error");
                        var st = crypto;
                        st.QuoteRetry = (st.QuoteRetry == null) ? 1 : st.QuoteRetry + 1;
                        ctx.CryptoList.Attach(st);
                        ctx.Entry(st).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                        ctx.SaveChanges();
                        ctx.Entry(st).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                        continue;
                    }
                    if (product.CancelOnly || product.IsDisabled || product.LimitOnly || product.PostOnly || product.TradingDisabled || product.ViewOnly || product.Status != "online")
                    {
                        helper.Log(_logger, LogLevel.Warning, $"Crypto {crypto.Coin} - ({crypto.Name}) Does not meet the criteria for purchase at this time.");
                        continue;
                    }
                    //skip crypto under $1
                    decimal? lowestPrice = .5m;
                        if (Convert.ToDecimal(product.Price) <= lowestPrice)
                    {
                        helper.Log(_logger, LogLevel.Warning, $"Price of {crypto.Coin} - ({crypto.Name}) ${product.Price} too low below ${lowestPrice}, skipping");
                        rejectPriceCount++;
                        continue;
                    }
                    helper.Log(_logger, LogLevel.Information, $"Checking {crypto.Coin} - ({crypto.Coin})");

                    dynamic results = null;
                    try
                    {
                        results = services.GetQuote(crypto.Coin);
                    }
                    catch (Exception ex)
                    {
                        helper.Log(_logger, LogLevel.Error, $"{ex.Message} - ({crypto.Coin}) - services.GetQuote Error");
                        var st = crypto;
                        st.QuoteRetry = (st.QuoteRetry == null) ? 1 : st.QuoteRetry + 1;
                        ctx.CryptoList.Attach(st);
                        ctx.Entry(st).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                        ctx.SaveChanges();
                        ctx.Entry(st).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                        continue;
                    }
                    helper.Log(_logger, LogLevel.Information, $"{crypto.Coin} - ({crypto.Coin}) price {results?["lastPrice"]?.ToString()}");

                    if (results is JsonObject)
                    {
                        if (((JsonObject)results).TryGetPropertyValue("errors", out JsonNode errors))
                        {
                            //failed to get quote.
                            helper.Log(_logger, LogLevel.Error, $"Failed to get quote for {crypto.Coin} - ({crypto.Name}) {errors.ToString()}");
                            var st = crypto;
                            st.QuoteRetry = (st.QuoteRetry == null) ? 1 : st.QuoteRetry + 1;
                            ctx.CryptoList.Attach(st);
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
                            helper.Log(_logger, LogLevel.Error, $"Failed to get quote for {crypto.Coin} - ({crypto.Name}) {errors.ToString()}");
                            var st = crypto;
                            st.QuoteRetry = (st.QuoteRetry == null) ? 1 : st.QuoteRetry + 1;
                            ctx.CryptoList.Attach(st);
                            ctx.Entry(st).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                            ctx.SaveChanges();
                            ctx.Entry(st).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                            continue;
                        }
                    }

                    //if (Convert.ToDecimal(results["volume"].ToString()) < 50000)
                    //{
                    //    helper.Log(_logger, LogLevel.Information, $"Not buying/selling short {results["Symbol"].ToString()} - ({results["Symbol"].ToString()}). Volume fall below min of 50000");
                    //    continue;
                    //}
                    helper.CalculatePoints(results, /*false, true,*/ ref shortCnt, ref longCnt);

                    //_logger.LogInformation($"{crypto.Coin} - ({crypto.Name}) Score. Long score: {longCnt}. Short score {shortCnt}");
                    helper.Log(_logger, LogLevel.Information, $"{crypto.Coin} - ({crypto.Name}) Score. Long score: {longCnt}. Short score {shortCnt}");

                    if (longCnt >= settings.CryptoLongCount/*8*/)
                    {
                        helper.Log(_logger, LogLevel.Information, $"Adding {crypto.Coin} - ({crypto.Name}) to potential long list.");
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
                        helper.Log(_logger, LogLevel.Information, $"Not buying/selling short {crypto.Coin} - ({crypto.Name}). Long score: {longCnt}. Short score {shortCnt}");
                    }
                }

                //BuySellCryptoList = BuySellCryptoList/*.Where(x => Convert.ToDecimal(x.Results["RSIOneHour"].ToString()) <= 35)*/.OrderByDescending(x => x.Points).ThenBy(x => Convert.ToDecimal(x.Results["RSI"].ToString()))/*.ThenBy(x => Convert.ToDecimal(x.Results["RSI"].ToString()))*/.ToList();
                //BuySellCryptoList = BuySellCryptoList.Where(x => Convert.ToDecimal(x.Results?["RSI"]?.ToString()) <= 30 || Convert.ToDecimal(x.Results?["RSI"]?.ToString()) >= 70).ToList();
                //BuySellCryptoList = BuySellCryptoList.Where(x => Convert.ToDecimal(x.Results?["RSI"]?.ToString()) <= 5m).ToList();
                if (BuySellCryptoList.Count > 0)
                    CalculatePurchaseAmount(totalAvailiableforLongTrade, products, cryptoTracker, ref BuySellCryptoList);
                else
                    helper.Log(_logger, LogLevel.Warning, "No Crypto was found to trade matching setting, or max number of stock reached, or 24h hold");

            }
            else
            {
                helper.Log(_logger, LogLevel.Error, $"Cash and margin balance below threshold ${totalAvailiableforLongTrade}");
            }
        }
        public void UnloadAgingStocks()
        {
            var Accounts = services.GetAccounts()/*.Where(x => x.Currency != "USD")*/.ToList();
            var PublicProducts = services.GetProducts().OrderBy(x => x.DisplayName).ToList();
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
            DateTime easternTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, easternTimeZone);

            var agingcryptos = ctx?.CryptoTracker2?.ToList()?.Where(x => x.PriceSold == null && x.DateSold == null && x.BuyStatus == "FILLED"
            && Math.Abs((x.DateCreated - easternTime).Value.TotalDays) > 10).ToList();
            foreach (var crypto in agingcryptos)
            {
                var results = services.GetQuote(crypto.Coin);
                string[] coin = crypto.Coin.Split('-');
                var account = Accounts.Where(x => x.Currency == coin[0] && Convert.ToDecimal(x.AvailableBalance.Value) > 0m).FirstOrDefault();
                var publicProduct = PublicProducts.Where(x => x.DisplayName == account.Currency + "-USD").FirstOrDefault();
                var valueofNonTrackedAsset = Math.Round(Convert.ToDecimal(account.AvailableBalance.Value) * Convert.ToDecimal(publicProduct?.Price), 9);
                //Platform tradePlatform = (Platform)crypto.TradePlatform;
                TradeDirection currentTradeDirection = (TradeDirection)Enum.Parse(typeof(TradeDirection), crypto.TradeDirection.ToString(), true);
                TradeDirection tradeDirection = (currentTradeDirection == TradeDirection.BUY) ? TradeDirection.SELL : TradeDirection.BUY_TO_COVER;
                //switch (tradePlatform)
                //{
                //    case Platform.CharlesSchwab:
                BuySellCrypto(crypto, results, tradeDirection, /*crypto.Amount ?? 0m*/valueofNonTrackedAsset, services, products, false/*, true*/);
                //        break;
                //    case Platform.ETrade:
                //        BuySellStock(stock, results, tradeDirection, stock.Quantity, true, services2);
                //        break;
                //}
            }
        }
        void UpdatePrices()
        {
            //Re-Get crypto already in trade being tracked.
            cryptoTracker = ctx.CryptoTracker2.Where(x => x.PriceSold == null && x.DateSold == null && x.BuyStatus == "Filled").ToList();
            //Loop through current crypto and evaluate
            helper.Log(_logger, LogLevel.Information, $"Checking current brought/shorted crypto");
            foreach (var coin in cryptoTracker)
            {
                //if (coin.Status == "open" || coin.Status == "pending")
                //    continue;
                var st = coin;
                //var st = ctx.CryptoTracker.Where(x => x.Coin == coin.Coin).FirstOrDefault();
                helper.Log(_logger, LogLevel.Information, $"Checking {coin.Coin} - ({coin.Name})");
                JsonObject results = null;
                try
                {
                    results = services.GetQuote(coin.Coin);
                }
                catch (Exception ex)
                {
                    helper.Log(_logger, LogLevel.Error, $"Crypto  {coin.Coin} check error {ex.Source} {ex.StackTrace} {ex?.InnerException} {ex.Message}.");
                    continue;
                }                

                //just trade
                helper.Log(_logger, LogLevel.Warning, $"Crypto {coin.Coin} - ({coin.Name}) Time since traded hrs {Math.Abs(Math.Round(((helper.easternTime - coin.DatePurchased.Value).TotalHours - 24), 5))}. Traded Price ${coin.PricePurchased} traded direction {Enum.GetName(typeof(TradeDirection), coin.TradeDirection)} crypto current price ${Convert.ToDecimal(results["lastPrice"].ToString())}");
                //var accounts = services.GetAccounts().Where(x => Convert.ToDecimal(x.AvailableBalance.Value) > 0).ToList();                

                if (st != null)
                    AnalyiseOwnedCrypto(st, results, products);
            }
        }
        void QueueCryptotoBuy(dynamic order, TradeDirection direction, decimal amount)
        {
            SaveCryptoTracker(null, direction, amount, order, null, "QUEUED");
        }
        void CheckforQueuedCrypto()
        {
            int? longCnt = 0;
            int? shortCnt = 0;
            var queuedCrypto = ctx.CryptoTracker2.Where(x => x.BuyStatus == "QUEUED").ToList();
             foreach (var crypto in queuedCrypto)
            {
                JsonObject? results = services.GetQuote(crypto.Coin);
                helper.CalculatePoints(results, /*false, true,*/ ref shortCnt, ref longCnt);
                if (longCnt >= settings.CryptoLongCount /*|| shortCnt >= settings.CryptoShortCount*/)
                {
                    BuySellCrypto(crypto, results,(TradeDirection)crypto.TradeDirection, crypto.Amount ?? 0m, services, products, false/*, true*/);
                }
                else
                {
                    SaveCryptoTracker(crypto, (TradeDirection)crypto.TradeDirection, crypto.Amount ?? 0m, results, null, "CANCEL"/*, true*/);
                }
            }
        }
        bool BuySellCrypto(CryptoTracker2 cryptoTracker, dynamic order, TradeDirection direction, decimal amount, CoinBaseService service, List<PublicProduct> publicProducts, bool sellShort)
        {
            string symbol = (order["Symbol"].ToString().Substring((order["Symbol"].ToString().Length - 1), 1) == "C") ? order["Symbol"].ToString().Substring(0, (order["Symbol"].ToString().Length - 1)) : order["Symbol"].ToString();
            PublicProduct? product = publicProducts.Where(x => x.DisplayName == symbol).FirstOrDefault();
            string precision = product.BaseIncrement.Substring((product.BaseIncrement.IndexOf(".") + 1), (product.BaseIncrement.Length - (product.BaseIncrement.IndexOf(".") + 1)));
            string[] currency = product.DisplayName.Split('-');
            var account = services.GetAccounts().Where(x => x.Currency == currency[0] && Convert.ToDecimal(x.AvailableBalance.Value) > 0m).FirstOrDefault();
            decimal minSubtract = 0m;
            //var valueofNonTrackedAsset = Math.Round(Convert.ToDecimal(account.AvailableBalance.Value) * Convert.ToDecimal(product?.Price), 2
            //var valueofNonTrackedAsset = Math.Round(Convert.ToDecimal(account.AvailableBalance.Value) * Convert.ToDecimal(product?.Price), 2);
            Coinbase.AdvancedTrade.Models.Order output = null;
            Coinbase.AdvancedTrade.Enums.OrderSide coinbaseDirection = (direction == TradeDirection.BUY || direction == TradeDirection.BUY_TO_COVER) ? Coinbase.AdvancedTrade.Enums.OrderSide.BUY : Coinbase.AdvancedTrade.Enums.OrderSide.SELL;
            switch (direction)
            {
                case TradeDirection.BUY:
                    //product = publicProducts.Where(x => x.DisplayName == symbol).FirstOrDefault();
                    ////precision = product.BaseIncrement.Substring((product.BaseIncrement.IndexOf(".") + 1), (product.BaseIncrement.Length - (product.BaseIncrement.IndexOf(".") + 1)));
                    //currency = product.DisplayName.Split('-');
                    var perItem = amount.ToString();
                    var amountSize = Convert.ToDecimal(perItem.Substring(0, ((perItem.Length - perItem.IndexOf(".")) - 1)));
                    output = services.SetOrder(product.DisplayName, coinbaseDirection, amountSize);
                    break;
                case TradeDirection.SELL:
                    //product = publicProducts.Where(x => x.DisplayName == symbol).FirstOrDefault();
                    //precision = product.BaseIncrement.Substring((product.BaseIncrement.IndexOf(".") + 1), (product.BaseIncrement.Length - (product.BaseIncrement.IndexOf(".") + 1)));
                    //currency = product.DisplayName.Split('-');
                    //var account = services.GetAccounts().Where(x => x.Currency == currency[0]).FirstOrDefault();
                    //var baseSize = (account.AvailableBalance.Value.IndexOf(".") == -1) ? Convert.ToDecimal(account.AvailableBalance.Value) : Convert.ToDecimal(account.AvailableBalance.Value.Substring(0, (account.AvailableBalance.Value.IndexOf(".") + precision.Length)));
                    switch (precision.Length)
                    {
                        case 1:
                            minSubtract = .1m;
                            break;
                        case 2:
                            minSubtract = .01m;
                            break;
                        case 3:
                            minSubtract = .001m;
                            break;
                        case 4:
                            minSubtract = .0001m;
                            break;
                        case 5:
                            minSubtract = .00001m;
                            break;
                        case 6:
                            minSubtract = .000001m;
                            break;
                        case 7:
                            minSubtract = .0000001m;
                            break;
                        case 8:
                            minSubtract = .00000001m;
                            break;
                        case 9:
                            minSubtract = .000000001m;
                            break;
                    }
                    var baseSize = decimal.Round(Convert.ToDecimal(account.AvailableBalance.Value), precision.Length) - minSubtract;
                    output = services.SetOrder(product.DisplayName, coinbaseDirection, baseSize);
                    break;
            }
            if (output != null)
            {
                helper.Log(_logger, LogLevel.Information, $"Crypto trade results for {symbol}, {output.Status}");
            }
            else
            {
                helper.Log(_logger, LogLevel.Error, $"Crypto trade results order {symbol} failed.");
            }

            if (output != null && (output.Status.ToLower() == "filled" || output.Status.ToLower() == "open" || output.Status.ToLower() == "pending"))
                SaveCryptoTracker(cryptoTracker, direction, amount, order, output, output.Status);
            //SaveCryptoTracker(cryptoTracker, direction, amount, order, output, (direction == TradeDirection.BUY) ? "FILLED" : null/*, IsUpdate*/);
            return true;
        }
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
                cryptoTracker.DateSold = helper.easternTime;
                cryptoTracker.DateUpdated = helper.easternTime;
                ctx.CryptoTracker2.Attach(cryptoTracker);
                ctx.Entry(cryptoTracker).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
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
                cryptoTracker.DatePurchased = helper.easternTime;
                cryptoTracker.DateCreated = helper.easternTime;
                cryptoTracker.LastUpdatedBy = 1;
                cryptoTracker.BuyStatus = (order != null) ? order.Status:(!string.IsNullOrEmpty(buyStatus)) ? buyStatus:null;
                if(isNew) {
                    ctx.CryptoTracker2.Add(cryptoTracker);
                    ctx.Entry(cryptoTracker).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                } else
                {
                    ctx.CryptoTracker2.Attach(cryptoTracker);
                    ctx.Entry(cryptoTracker).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                }
                    
                
            }
            try
            {
                ctx.SaveChanges();
                ctx.Entry(cryptoTracker).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
            }
            catch (Exception ex)
            {
                helper.Log(_logger, LogLevel.Error, $"Source: {ex.Source} stack trace: {ex.StackTrace} inner exception {ex?.InnerException?.Message ?? ""} error: {ex.Message}");
            }
            
            return true;
        }
        bool UpdateLastPrice(decimal lastPrice, decimal Balance, ref CryptoTracker2 cryptoTracker)
        {
            ctx = new TmdContext();
            if((cryptoTracker.TradeDirection == (int)TradeDirection.BUY && (lastPrice > cryptoTracker.LastPrice && lastPrice > cryptoTracker.PricePurchased)) || cryptoTracker.LastPrice is null)
            {
                cryptoTracker.LastPrice = lastPrice;
            }
            if ((cryptoTracker.TradeDirection == (int)TradeDirection.SELL_SHORT && (lastPrice < cryptoTracker.LastPrice && lastPrice < cryptoTracker.PricePurchased)) || cryptoTracker.LastPrice is null)
            {
                cryptoTracker.LastPrice = lastPrice;
            }
            cryptoTracker.CurrentBalance = Balance;
            cryptoTracker.DateUpdated = helper.easternTime;
            try
            {
                ctx.Entry(cryptoTracker).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                System.Threading.Thread.Sleep(200);
                ctx.SaveChanges();
                ctx.Entry(cryptoTracker).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
            }
            catch (Exception ex)
            {
                helper.Log(_logger, LogLevel.Error, $"Price Update Failed! {ex.Message}");
            }
            return true;
        }

        void AnalyiseOwnedCrypto(CryptoTracker2 crypto, JsonObject? results, List<PublicProduct> products)
        {
            //calculate percent difference
            decimal percentDiffPricePurchased = 0m;
            decimal percentDiffLastPrice = 0m;
            decimal lastPrice = (crypto.LastPrice ?? 0);
            decimal pricePurchased = (crypto.PricePurchased ?? 0);
            decimal currentPrice = Convert.ToDecimal(results["lastPrice"].ToString());
            //var order = services.GetOrder((Guid)crypto.BuyOrderId);
            var product = products.Where(x => x.DisplayName == crypto.Coin).FirstOrDefault();
            var account = services?.GetAccounts().Where(x => x.Currency == crypto.Coin.Split(new char[] { '-' })[0] && Convert.ToDecimal(x.AvailableBalance.Value) > 0m).FirstOrDefault();
            var currentBalance = Math.Round(Convert.ToDecimal(account?.AvailableBalance?.Value) * Convert.ToDecimal(product?.Price), 5);
            try
            {
                percentDiffPricePurchased = Math.Abs(((pricePurchased - currentPrice) / ((pricePurchased + currentPrice) / 2)) * 100);
                percentDiffLastPrice = Math.Abs(((lastPrice - currentPrice) / ((lastPrice + currentPrice) / 2)) * 100);
            }
            catch (Exception ex)
            {
                helper.Log(_logger, LogLevel.Error, $"{ex.Message} at AnalyiseOwnedStock Percent Diff. Setting to zero.");
            }
            //Short
            //if (crypto.TradeDirection == (int)TradeDirection.SELL_SHORT) //selling short
            //{
            //    if (currentPrice > pricePurchased)
            //    {
            //        if (percentDiffPricePurchased > settings.StopLossPercentPurchasedCrypto)
            //        {
            //            //buy to cover
            //            helper.Log(_logger, LogLevel.Information, $"crypto {crypto.Coin} - ({crypto.Name}) has fallen by more then about {Math.Round(percentDiffPricePurchased, 5)}% since price purchased. Buying to cover.");
            //            BuySellCrypto(crypto, results, TradeDirection.BUY_TO_COVER, Math.Round(Convert.ToDecimal(order.FilledSize), 5), services, products, false);
            //        }
            //        else
            //        {
            //            //Update Price
            //            helper.Log(_logger, LogLevel.Information, $"crypto {crypto.Coin} - ({crypto.Name}) still within about {Math.Round(percentDiffPricePurchased, 5)}% tolerance since price purchased. Updating last Price.");
            //            UpdateLastPrice(currentPrice, balance, ref crypto);
            //        }
            //    }
            //    else
            //    if (currentPrice > lastPrice)
            //    {
            //        if (percentDiffLastPrice > settings.StopLossPercentLastPriceCrypto)
            //        {
            //            //buy to cover
            //            helper.Log(_logger, LogLevel.Information, $"crypto {crypto.Coin} - ({crypto.Name}) has fallen by more then about {Math.Round(percentDiffLastPrice, 5)}% since last price. Buying to cover.");
            //            BuySellCrypto(crypto, results, TradeDirection.BUY_TO_COVER, Math.Round(Convert.ToDecimal(order.FilledSize), 5), services, products, false);
            //        }
            //        else
            //        {
            //            //Update Price
            //            //helper.Log(_logger, LogLevel.Information, $"crypto {crypto.Coin} - ({crypto.Name}) still within about {Math.Round(percentDiffLastPrice, 5)}% tolerance since last price. Updating last Price.");
            //            //UpdateLastPrice(currentPrice, balance, ref crypto);
            //        }
            //    }
            //    else //if (currentPrice <= lastPrice)
            //    {
            //            //update price
            //            helper.Log(_logger, LogLevel.Information, $"crypto {crypto.Coin} - ({crypto.Name}) still within about {Math.Round(percentDiffLastPrice, 5)}% tolerance since last price. Updating last Price.");
            //            UpdateLastPrice(currentPrice, balance, ref crypto);
            //    }
            //}
            //else //Going Long
            {
                if (currentPrice < pricePurchased)
                {
                    if (percentDiffPricePurchased > settings.StopLossPercentPurchasedCrypto)
                    {
                        //buy to cover
                        helper.Log(_logger, LogLevel.Information, $"crypto  {crypto.Coin}  - ( {crypto.Name}) has fallen by more then about {Math.Round(percentDiffPricePurchased, 5)}% since price purchased. Selling.");
                        UpdateLastPrice(currentPrice, currentBalance, ref crypto);
                        BuySellCrypto(crypto, results, TradeDirection.SELL, currentBalance, services, products, false/*, false*/);
                    }
                    else
                    {
                        //Update Price
                        helper.Log(_logger, LogLevel.Information, $"crypto  {crypto.Coin}  - ( {crypto.Name}) still within about {Math.Round(percentDiffPricePurchased, 5)}% tolerance since price purchased. Updating last Price.");
                        UpdateLastPrice(currentPrice, currentBalance, ref crypto);
                    }
                }
                else
                //if (currentPrice < lastPrice) // try to kill the loss by selling only if above amount balance
                if (currentPrice < lastPrice && currentBalance > (crypto.Amount * 1.08m))
                {
                    if (percentDiffLastPrice > settings.StopLossPercentLastPriceCrypto)
                    {
                        //sell
                        helper.Log(_logger, LogLevel.Information, $"crypto {crypto.Coin} - ({crypto.Name}) has fallen by more then about {Math.Round(percentDiffLastPrice, 5)}% since last Price. Selling.");
                        UpdateLastPrice(currentPrice, currentBalance, ref crypto);
                        BuySellCrypto(crypto, results, TradeDirection.SELL, currentBalance, services, products, false/*, false*/);
                    }
                    else
                    {
                        //Update Price
                        //helper.Log(_logger, LogLevel.Information, $"crypto {crypto.Coin} - ({crypto.Name}) still within about {Math.Round(percentDiffLastPrice, 5)}% tolerance since last Price. Updating last Price.");
                        //UpdateLastPrice(currentPrice, balance, ref crypto);
                    }
                }
                else //if (currentPrice >= lastPrice)
                {
                        //update price
                        helper.Log(_logger, LogLevel.Information, $"crypto {crypto.Coin} - ({crypto.Name}) still within about {Math.Round(percentDiffLastPrice, 5)}% tolerance since last price. Updating last Price.");
                        UpdateLastPrice(currentPrice, currentBalance, ref crypto);
                }
            }
        }
       
        void CalculatePurchaseAmount(decimal? availiabletoTrade, List<PublicProduct> products, List<CryptoTracker2> cryptoTracker, ref List<BuySellCommodity> BuySellCryptoList)
        {
            var alreadyQueuedStock = ctx?.CryptoTracker2?.Where(x => x.BuyStatus == "QUEUED").ToList();
            if (cryptoTracker.Count() >= settings.MaxNumberofCryptotoBuy)
                return;
            int cryptoCnt = 0;
            //decimal? perCrypto = (BuySellCryptoList.Count() < (settings.MaxNumberofCryptotoBuy - cryptoTracker.Count())) ? availiabletoTrade / BuySellCryptoList.Count() : availiabletoTrade / (settings.MaxNumberofCryptotoBuy - cryptoTracker.Count());//(availiabletoTrade / BuySellCryptoList.Count) > (availiabletoTrade * (settings.MaxPercentofAvailBalancePerOrder/100)) ? (availiabletoTrade * (settings.MaxPercentofAvailBalancePerOrder / 100)) : (availiabletoTrade / BuySellCryptoList.Count);
            //decimal? perCrypto = availiabletoTrade / Math.Abs((int)settings.MaxNumberofCryptotoBuy - cryptoTracker.Count());
            //decimal? perCrypto = ((availiabletoTrade / Math.Abs((int)settings.MaxNumberofCryptotoBuy - cryptoTracker.Count())) > settings.MaxDollarAmountPerCrypto) ? settings.MaxDollarAmountPerCrypto : (availiabletoTrade / Math.Abs((int)settings.MaxNumberofCryptotoBuy - cryptoTracker.Count()));
            //decimal? perCrypto = (availiabletoTrade / BuySellCryptoList.Count());
            decimal ? perCrypto = (availiabletoTrade / BuySellCryptoList.Count()) > settings.MaxDollarAmountPerCrypto ? (availiabletoTrade < settings.MaxDollarAmountPerCrypto) ? availiabletoTrade : settings.MaxDollarAmountPerCrypto : (availiabletoTrade / BuySellCryptoList.Count());
            foreach (var item in BuySellCryptoList)
            {
                if ((cryptoTracker.Count() + cryptoCnt) >= settings.MaxNumberofCryptotoBuy)
                    break;
                //var result = item.Key;
                //var direction = item.Value;
                var result = item.Results;
                var direction = item.Direction;
                bool sellShort = (direction == TradeDirection.SELL_SHORT) ? true : false;
                if (perCrypto >= 5)
                {
                    //BuySellCrypto(null, result, direction,Math.Round((decimal)perCrypto, 5), services, products, sellShort);
                    if (!alreadyQueuedStock.Where(x => x.Coin == result["Symbol"].ToString()).Any())
                        QueueCryptotoBuy(result, direction, Math.Round((decimal)perCrypto, 5));
                    cryptoCnt++;
                }
            }
        }

        public void ClearOrphanWallets()
        {
            CryptoTracker2? coin = null;
            Order output = null;
            int lengthAfterDecimal = 0;
            Guid? buyOrderId = null;
            var Accounts = services.GetAccounts()/*.Where(x => x.Currency != "USD")*/.ToList();
            helper.Log(_logger, LogLevel.Information, $"Getting owned crypto list");
            var PublicProducts = services.GetProducts().OrderBy(x => x.DisplayName).ToList();
            //var Cryptos = ctx.CryptoTracker.Where(x => x.PriceSold == null && x.DateSold == null && x.BuyStatus == "FILLED").ToList();
            //check if there is a wallet with cash not in tracker
            foreach (var item in Accounts)
            {
                helper.Log(_logger, LogLevel.Information, $"Iterating crypto list. Analyzing {item.Currency}");
                //if (item.Currency.Contains("ETH") /*|| item.Currency == "LTC"*/)
                //{
                //    //they do exist
                //}
                var publicProduct = PublicProducts.Where(x => x.DisplayName == item.Currency + "-USD").FirstOrDefault();
                var valueofNonTrackedAsset = Math.Round(Convert.ToDecimal(item.AvailableBalance.Value) * Convert.ToDecimal(publicProduct?.Price), 9);
                if (valueofNonTrackedAsset == .0m)
                {
                    helper.Log(_logger, LogLevel.Information, $"{item.Currency} no available balance.");
                    continue;
                }
                if (valueofNonTrackedAsset >.0m && valueofNonTrackedAsset <= .80m)
                {
                    helper.Log(_logger, LogLevel.Warning, $"{item.Currency} too low.");
                    continue;
                }else
                if (valueofNonTrackedAsset > .80m && valueofNonTrackedAsset <= 1.5m)
                {
                    decimal baseSize = 0m;
                    var precision = publicProduct?.BaseIncrement.Substring((publicProduct.BaseIncrement.IndexOf(".") + 1), (publicProduct.BaseIncrement.Length - (publicProduct.BaseIncrement.IndexOf(".") + 1)));
                    var currency = publicProduct?.DisplayName.Split('-');
                    var account = services.GetAccounts().Where(x => x.Currency == currency[0]).FirstOrDefault();
                    if (account?.AvailableBalance.Value.IndexOf(".") <= -1)
                        baseSize = Convert.ToDecimal(account.AvailableBalance.Value);
                    else
                        baseSize = Convert.ToDecimal(account?.AvailableBalance.Value.Substring(0, (account.AvailableBalance.Value.IndexOf(".") + precision.Length)));
                    if (baseSize <= .0m)
                        continue;
                    output = services.SetOrder(publicProduct?.DisplayName, Coinbase.AdvancedTrade.Enums.OrderSide.SELL, baseSize);
                    helper.Log(_logger, LogLevel.Warning, $"{item.Currency} too low.");
                    continue;
                }else
                if (valueofNonTrackedAsset > 1.5m)
                {
                    coin = ctx.CryptoTracker2.Where(x => x.Coin == item.Currency + "-USD" && x.PriceSold == null && x.DateSold == null && (x.BuyStatus.ToLower() == "open" || x.BuyStatus.ToLower() == "pending")).FirstOrDefault();
                    helper.Log(_logger, LogLevel.Information, $"{item.Currency} meets min amount checking if synced with db.");
                }

                //var coin = Cryptos.Where(x => x.Coin == item.Currency + "-USD").ToList();
                if (coin is not null)
                {
                    helper.Log(_logger, LogLevel.Information, $"{item.Currency} found in db pending");
                    var crypto = coin; 
                    if(crypto.BuyOrderId == null)
                    {
                        helper.Log(_logger, LogLevel.Information, $"{item.Currency} BuyOrderId missing. Getting by last order.");
                        //var results = services.GetOrders(crypto.Coin);
                        //var lastSell = results.Where(x => x.Side == "BUY").OrderByDescending(y => Convert.ToDateTime(y.SequenceTimestamp)).FirstOrDefault();
                        var lastSell = GetLastOrder(crypto.Coin);
                        buyOrderId = Guid.Parse(lastSell?.OrderId);
                        crypto.BuyOrderId = buyOrderId;
                    }
                    else
                    {
                        helper.Log(_logger, LogLevel.Information, $"{item.Currency} BuyOrderId found.");
                        buyOrderId = crypto.BuyOrderId;
                    }

                    helper.Log(_logger, LogLevel.Information, $"{item.Currency} Getting latest order info and updating.");
                    var buyOrder = services.GetOrder((Guid)buyOrderId);
                    //var results = services.GetQuote(crypto.Coin);
                    crypto.PricePurchased = Convert.ToDecimal(buyOrder.AverageFilledPrice);
                    //crypto.LastPrice = Convert.ToDecimal(results["lastPrice"].ToString());
                    crypto.CurrentBalance = valueofNonTrackedAsset;
                    crypto.DateUpdated = helper.easternTime;
                    crypto.DatePurchased = item.UpdatedAt;
                    crypto.BuyStatus = (buyOrder.Status ?? "FILLED");
                    ctx.CryptoTracker2.Attach(crypto);
                    ctx.Entry(crypto).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                    //ctx.SaveChanges();
                    //ctx.Entry(crypto).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                    helper.Log(_logger, LogLevel.Information, $"{item.Currency} Updating in db.");
                }
                else
                {
                    helper.Log(_logger, LogLevel.Information, $"{item.Currency} not found pending. CHecking filled.");
                    coin = ctx.CryptoTracker2.Where(x => x.Coin == item.Currency + "-USD" && x.PriceSold == null && x.DateSold == null && x.BuyStatus.ToLower() == "filled").FirstOrDefault();
                    if (coin is not null)
                    {
                        helper.Log(_logger, LogLevel.Information, $"{item.Currency} found in db filled.");
                        var crypto = coin;
                        if (crypto.BuyOrderId == null)
                        {
                            helper.Log(_logger, LogLevel.Information, $"{item.Currency} BuyOrderId missing. Getting by last order.");
                            //var results2 = services.GetOrders(crypto.Coin);
                            //var lastSell = results2.Where(x => x.Side == "BUY").OrderByDescending(y => Convert.ToDateTime(y.SequenceTimestamp)).FirstOrDefault();
                            crypto.CurrentBalance = valueofNonTrackedAsset;
                            var lastSell = GetLastOrder(crypto.Coin);
                            buyOrderId = Guid.Parse(lastSell?.OrderId);
                            crypto.BuyOrderId = buyOrderId;
                            crypto.DatePurchased = item.UpdatedAt;
                            ctx.CryptoTracker2.Attach(crypto);
                            ctx.Entry(crypto).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                            //ctx.SaveChanges();
                            //ctx.Entry(crypto).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                        }
                        //continue; //already in tracker. Update pruchased date
                        else {
                            crypto.CurrentBalance = valueofNonTrackedAsset;
                            crypto.DatePurchased = item.UpdatedAt;
                            ctx.CryptoTracker2.Attach(crypto);
                            ctx.Entry(crypto).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                            //ctx.SaveChanges();
                            //ctx.Entry(crypto).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                        }
                        helper.Log(_logger, LogLevel.Information, $"{item.Currency} Updating in db.");
                    }
                    else
                    {
                        helper.Log(_logger, LogLevel.Information, $"{item.Currency} Not found pending or filled. Creating new.");
                        //create new crypto tracker
                        coin = new CryptoTracker2();
                        var results = services.GetQuote(item.Currency + "-USD");
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
                        coin.DateCreated = helper.easternTime;
                        coin.LastUpdatedBy = 1;
                        ctx.CryptoTracker2.Add(coin);
                        ctx.Entry(coin).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                        helper.Log(_logger, LogLevel.Information, $"{item.Currency} Adding in db.");
                    }
                }
                try
                {
                    ctx.SaveChanges();
                    ctx.Entry(coin).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                }
                catch (Exception ex)
                {
                    helper.Log(_logger, LogLevel.Error, $"Source: {ex.Source} stack trace: {ex.StackTrace} inner exception {ex?.InnerException?.Message ?? ""} error: {ex.Message}");
                }

            }
            var Cryptos = ctx.CryptoTracker2.Where(x => x.PriceSold == null && x.DateSold == null && x.BuyStatus == "FILLED").ToList();
            foreach(var crypto in Cryptos)
            {
                var currency = crypto.Coin.Split('-');
                var account = services.GetAccounts().Where(x => x.Currency == currency[0]).FirstOrDefault();
                var publicProduct = PublicProducts.Where(x => x.DisplayName == crypto.Coin).FirstOrDefault();
                var valueofNonTrackedAsset = Math.Round(Convert.ToDecimal(account.AvailableBalance.Value) * Convert.ToDecimal(publicProduct?.Price), 9);
                //if (decimal.TryParse(account?.AvailableBalance.Value, out decimal bal) && bal <= 1.5m)
                if (valueofNonTrackedAsset > 0m && valueofNonTrackedAsset <= 1.5m)
                {
                    var ct = crypto;
                    var results = services.GetQuote(ct.Coin);
                    ct.PriceSold = Convert.ToDecimal(results["lastPrice"].ToString());
                    ct.SellOrderId = Guid.Empty;
                    ct.SellStatus = "FILLED";
                    ct.DateSold = helper?.easternTime;
                    ct.DateUpdated = helper?.easternTime;
                    ctx.CryptoTracker2.Attach(ct);
                    ctx.Entry(ct).State = Microsoft.EntityFrameworkCore.EntityState.Deleted;
                    ctx.SaveChanges();
                    ctx.Entry(ct).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                }
            }
        }
        private Fill GetLastOrder(string ProductId)
        {
            var orders = services.GetOrders(ProductId);
            var order = orders.Where(x => x.ProductId == ProductId).OrderByDescending(y => Convert.ToDateTime(y.SequenceTimestamp)).FirstOrDefault();
            return order;
        }
        ~ExecCryptoPricePrediction()
        {
            ctx?.Dispose();
            ctx = null;
            helper = null;
        }
    }
}
