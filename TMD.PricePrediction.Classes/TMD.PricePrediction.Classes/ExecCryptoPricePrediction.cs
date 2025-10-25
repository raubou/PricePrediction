using Coinbase.AdvancedTrade.Models;
using Coinbase.AdvancedTrade.Models.Public;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Nodes;
using TMD.Services;
using TMD.Services.Service;
using TMDInvestments.Context;
using TMDInvestments.Models;
using TMDInvestments.Models.Models;

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
        List<CryptoTracker> cryptoTracker = null;
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
            helper.Log(_logger, LogLevel.Information, $"Update order status for filled and cancelled orders");

            //Get Products
            products = services.GetProducts();

            //Update Prices
            helper.Log(_logger, LogLevel.Information, $"Updating Latest Prices on Tracker");
            UpdatePrices();
            //System.Threading.Thread.Sleep(1000);

            //Get Account Info
            List<PortfolioBreakdown> portfolios = services.GetPortfolios();
            helper.Log(_logger, LogLevel.Information, $"Get account info");

            //Get Crypto already in trade being tracked.
            cryptoTracker = ctx.CryptoTracker.Where(x => x.PriceSold == null && x.DateSold == null && (x.BuyStatus.ToLower() == "filled" /*|| x.BuyStatus.ToLower() == "open" || x.BuyStatus.ToLower() == "pending"*/)).ToList();
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
                var CryptoHold24Hr = ctx.CryptoTracker.ToList().Where(x => x.PriceSold != null && x.DateSold != null && Math.Abs((x.DateSold - helper.easternTime).Value.TotalHours) < 24).ToList().Select(y => new { Coin = y.Coin, DateSold = y.DateSold, DateUpdated = y.DateUpdated }).ToList();
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
                    if (Convert.ToDecimal(product.Price) <= .15m)
                    {
                        helper.Log(_logger, LogLevel.Warning, $"Price of {crypto.Coin} - ({crypto.Name}) ${product.Price} too low below $1, skipping");
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
                    helper.CalculatePoints(results, true, false, true, ref shortCnt, ref longCnt);

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
        void UpdatePrices()
        {
            //Re-Get crypto already in trade being tracked.
            cryptoTracker = ctx.CryptoTracker.Where(x => x.PriceSold == null && x.DateSold == null && x.BuyStatus == "Filled").ToList();
            //Loop through current crypto and evaluate
            helper.Log(_logger, LogLevel.Information, $"Checking current brought/shorted crypto");
            foreach (var coin in cryptoTracker)
            {
                //if (coin.Status == "open" || coin.Status == "pending")
                //    continue;
                var st = coin;
                //var st = ctx.CryptoTracker.Where(x => x.Coin == coin.Coin).FirstOrDefault();
                helper.Log(_logger, LogLevel.Information, $"Checking {coin.Coin} - ({coin.Name})");
                JsonObject results = services.GetQuote(coin.Coin);

                //just trade
                helper.Log(_logger, LogLevel.Warning, $"Crypto {coin.Coin} - ({coin.Name}) Time since traded hrs {Math.Abs(Math.Round(((helper.easternTime - coin.DatePurchased.Value).TotalHours - 24), 5))}. Traded Price ${coin.PricePurchased} traded direction {Enum.GetName(typeof(TradeDirection), coin.TradeDirection)} crypto current price ${Convert.ToDecimal(results["lastPrice"].ToString())}");
                //var accounts = services.GetAccounts().Where(x => Convert.ToDecimal(x.AvailableBalance.Value) > 0).ToList();                

                if (st != null)
                    AnalyiseOwnedCrypto(st, results, products);
            }
        }
        bool BuySellCrypto(CryptoTracker cryptoTracker, dynamic order, TradeDirection direction, decimal amount, CoinBaseService service, List<PublicProduct> publicProducts, bool sellShort)
        {
            string symbol = (order["Symbol"].ToString().Substring((order["Symbol"].ToString().Length - 1), 1) == "C") ? order["Symbol"].ToString().Substring(0, (order["Symbol"].ToString().Length - 1)) : order["Symbol"].ToString();
            PublicProduct? product = publicProducts.Where(x => x.DisplayName == symbol).FirstOrDefault();
            string precision = product.BaseIncrement.Substring((product.BaseIncrement.IndexOf(".") + 1), (product.BaseIncrement.Length - (product.BaseIncrement.IndexOf(".") + 1)));
            string[] currency = product.DisplayName.Split('-');
            var account = services.GetAccounts().Where(x => x.Currency == currency[0]).FirstOrDefault();
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
                SaveCryptoTracker(cryptoTracker, direction, amount, order, output);
                //SaveCryptoTracker(cryptoTracker, direction, (direction == TradeDirection.SELL) ? Convert.ToDecimal(output.TotalValueAfterFees) : amount, order, output);
            return true;
        }
        bool SaveCryptoTracker(CryptoTracker cryptoTracker, TradeDirection direction, decimal currentBalance, dynamic results, Coinbase.AdvancedTrade.Models.Order order)
        {
            if (cryptoTracker != null && (direction == TradeDirection.SELL || direction == TradeDirection.BUY_TO_COVER))
            {
                cryptoTracker.PriceSold = Convert.ToDecimal(results["lastPrice"].ToString());
                cryptoTracker.LastPrice = Convert.ToDecimal(results["lastPrice"].ToString());
                cryptoTracker.SoldRsi = Convert.ToDecimal(results["RSI"].ToString());
                //cryptoTracker.SoldSma200 = Convert.ToDecimal(results["SMA30"].ToString());
                //cryptoTracker.SoldEma200 = Convert.ToDecimal(results["EMA30"].ToString());
                //cryptoTracker.SoldVwap = Convert.ToDecimal(results["VWAP"].ToString());
                cryptoTracker.SoldEma9 = Convert.ToDecimal(results["EMA9"].ToString());
                cryptoTracker.SoldEma12 = Convert.ToDecimal(results["EMA12"].ToString());
                cryptoTracker.SoldEma26 = Convert.ToDecimal(results["EMA26"].ToString());
                cryptoTracker.SoldMacd = Convert.ToDecimal(results["MACD"].ToString());
                cryptoTracker.CurrentBalance = /*(direction == TradeDirection.SELL) ? cryptoTracker.CurrentBalance :*/ currentBalance;
                cryptoTracker.SellOrderId = Guid.Parse(order.OrderId);
                cryptoTracker.SellStatus = order.Status;
                cryptoTracker.DateSold = helper.easternTime;
                cryptoTracker.DateUpdated = helper.easternTime;
                ctx.CryptoTracker.Attach(cryptoTracker);
                ctx.Entry(cryptoTracker).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
            }
            else
            {
                cryptoTracker = new CryptoTracker();
                cryptoTracker.Coin = results["Symbol"].ToString();
                cryptoTracker.BuyOrderId = Guid.Parse(order.OrderId);
                cryptoTracker.BuyStatus = order.Status;
                cryptoTracker.Name = results["Name"].ToString();
                cryptoTracker.TradeDirection = (int)direction;
                cryptoTracker.PricePurchased = Convert.ToDecimal(results["lastPrice"].ToString());
                cryptoTracker.Amount = currentBalance;
                cryptoTracker.PurchasedRsi = Convert.ToDecimal(results["RSI"].ToString());
                //cryptoTracker.PurchasedSma200 = Convert.ToDecimal(results["SMA30"].ToString());
                //cryptoTracker.PurchasedEma200 = Convert.ToDecimal(results["EMA30"].ToString());
                //cryptoTracker.PurchasedVwap = Convert.ToDecimal(results["VWAP"].ToString());
                cryptoTracker.PurchasedEma9 = Convert.ToDecimal(results["EMA9"].ToString());
                cryptoTracker.PurchasedEma12 = Convert.ToDecimal(results["EMA12"].ToString());
                cryptoTracker.PurchasedEma26 = Convert.ToDecimal(results["EMA26"].ToString());
                cryptoTracker.PurchasedMacd = Convert.ToDecimal(results["MACD"].ToString());
                cryptoTracker.LastPrice = Convert.ToDecimal(results["lastPrice"].ToString());
                cryptoTracker.DatePurchased = helper.easternTime;
                cryptoTracker.DateCreated = helper.easternTime;
                cryptoTracker.LastUpdatedBy = 1;
                ctx.CryptoTracker.Add(cryptoTracker);
                ctx.Entry(cryptoTracker).State = Microsoft.EntityFrameworkCore.EntityState.Added;
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
        bool UpdateLastPrice(decimal lastPrice, decimal Balance, ref CryptoTracker cryptoTracker)
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

        void AnalyiseOwnedCrypto(CryptoTracker crypto, JsonObject? results, List<PublicProduct> products)
        {
            //calculate percent difference
            decimal percentDiffPricePurchased = 0m;
            decimal percentDiffLastPrice = 0m;
            decimal lastPrice = (crypto.LastPrice ?? 0);
            decimal pricePurchased = (crypto.PricePurchased ?? 0);
            decimal currentPrice = Convert.ToDecimal(results["lastPrice"].ToString());
            //var order = services.GetOrder((Guid)crypto.BuyOrderId);
            var product = products.Where(x => x.DisplayName == crypto.Coin).FirstOrDefault();
            var account = services?.GetAccounts().Where(x => x.Currency == crypto.Coin.Split(new char[] { '-' })[0]).FirstOrDefault();
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
            //    else if (currentPrice <= lastPrice)
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
                        BuySellCrypto(crypto, results, TradeDirection.SELL, currentBalance, services, products, false);
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
                if (currentPrice < lastPrice && currentBalance > (crypto.Amount * 1.075m))
                {
                    if (percentDiffLastPrice > settings.StopLossPercentLastPriceCrypto)
                    {
                        //sell
                        helper.Log(_logger, LogLevel.Information, $"crypto {crypto.Coin} - ({crypto.Name}) has fallen by more then about {Math.Round(percentDiffLastPrice, 5)}% since last Price. Selling.");
                        UpdateLastPrice(currentPrice, currentBalance, ref crypto);
                        BuySellCrypto(crypto, results, TradeDirection.SELL, currentBalance, services, products, false);
                    }
                    else
                    {
                        //Update Price
                        //helper.Log(_logger, LogLevel.Information, $"crypto {crypto.Coin} - ({crypto.Name}) still within about {Math.Round(percentDiffLastPrice, 5)}% tolerance since last Price. Updating last Price.");
                        //UpdateLastPrice(currentPrice, balance, ref crypto);
                    }
                }
                else if (currentPrice >= lastPrice)
                {
                        //update price
                        helper.Log(_logger, LogLevel.Information, $"crypto {crypto.Coin} - ({crypto.Name}) still within about {Math.Round(percentDiffLastPrice, 5)}% tolerance since last price. Updating last Price.");
                        UpdateLastPrice(currentPrice, currentBalance, ref crypto);
                }
            }
        }
       
        void CalculatePurchaseAmount(decimal? availiabletoTrade, List<PublicProduct> products, List<CryptoTracker> cryptoTracker, ref List<BuySellCommodity> BuySellCryptoList)
        {
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
                    BuySellCrypto(null, result, direction,Math.Round((decimal)perCrypto, 5), services, products, sellShort);
                    cryptoCnt++;
                }
            }
        }

        public void ClearOrphanWallets()
        {
            CryptoTracker? coin = null;
            Order output = null;
            int lengthAfterDecimal = 0;
            Guid? buyOrderId = null;
            var Accounts = services.GetAccounts()/*.Where(x => x.Currency != "USD")*/.ToList();
            var PublicProducts = services.GetProducts().OrderBy(x => x.DisplayName).ToList();
            //var Cryptos = ctx.CryptoTracker.Where(x => x.PriceSold == null && x.DateSold == null && x.BuyStatus == "FILLED").ToList();
            //check if there is a wallet with cash not in tracker
            foreach (var item in Accounts)
            {
                if (item.Currency == "BTC" || item.Currency == "LTC")
                {
                    //they do exist
                }
                var publicProduct = PublicProducts.Where(x => x.DisplayName == item.Currency + "-USD").FirstOrDefault();
                var valueofNonTrackedAsset = Math.Round(Convert.ToDecimal(item.AvailableBalance.Value) * Convert.ToDecimal(publicProduct?.Price), 5);
                if (valueofNonTrackedAsset <= .80m)
                {
                    continue;
                }
                if (valueofNonTrackedAsset > .80m && valueofNonTrackedAsset <= 1.5m)
                {
                    decimal baseSize = 0m;
                    var precision = publicProduct?.BaseIncrement.Substring((publicProduct.BaseIncrement.IndexOf(".") + 1), (publicProduct.BaseIncrement.Length - (publicProduct.BaseIncrement.IndexOf(".") + 1)));
                    var currency = publicProduct?.DisplayName.Split('-');
                    var account = services.GetAccounts().Where(x => x.Currency == currency[0]).FirstOrDefault();
                    if(account?.AvailableBalance.Value.IndexOf(".") <= -1)
                        baseSize = Convert.ToDecimal(account.AvailableBalance.Value);
                    else
                        baseSize = Convert.ToDecimal(account?.AvailableBalance.Value.Substring(0, (account.AvailableBalance.Value.IndexOf(".") + precision.Length)));
                    if(baseSize <= .0m)
                        continue;
                    output = services.SetOrder(publicProduct?.DisplayName, Coinbase.AdvancedTrade.Enums.OrderSide.SELL, baseSize);
                    continue;
                }
                if(valueofNonTrackedAsset > 1.5m)
                {
                    coin = ctx.CryptoTracker.Where(x => x.Coin == item.Currency + "-USD" && x.PriceSold == null && x.DateSold == null && (x.BuyStatus.ToLower() == "open" || x.BuyStatus.ToLower() == "pending")).FirstOrDefault();
                }

                //var coin = Cryptos.Where(x => x.Coin == item.Currency + "-USD").ToList();
                if (coin is not null)
                {
                    var crypto = coin; 
                    if(crypto.BuyOrderId == null)
                    {
                        //var results = services.GetOrders(crypto.Coin);
                        //var lastSell = results.Where(x => x.Side == "BUY").OrderByDescending(y => Convert.ToDateTime(y.SequenceTimestamp)).FirstOrDefault();
                        var lastSell = GetLastOrder(crypto.Coin);
                        buyOrderId = Guid.Parse(lastSell?.OrderId);
                        crypto.BuyOrderId = buyOrderId;
                    }
                    else
                    {
                        buyOrderId = crypto.BuyOrderId;
                    }


                     var buyOrder = services.GetOrder((Guid)buyOrderId);
                    //var results = services.GetQuote(crypto.Coin);
                    crypto.PricePurchased = Convert.ToDecimal(buyOrder.AverageFilledPrice);
                    //crypto.LastPrice = Convert.ToDecimal(results["lastPrice"].ToString());
                    crypto.CurrentBalance = valueofNonTrackedAsset;
                    crypto.DateUpdated = helper.easternTime;
                    crypto.BuyStatus = (buyOrder.Status ?? "FILLED");
                    ctx.CryptoTracker.Attach(crypto);
                    ctx.Entry(crypto).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                    ctx.SaveChanges();
                    ctx.Entry(crypto).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                }
                else
                {
                    coin = ctx.CryptoTracker.Where(x => x.Coin == item.Currency + "-USD" && x.PriceSold == null && x.DateSold == null && x.BuyStatus.ToLower() == "filled").FirstOrDefault();
                    if (coin is not null)
                    {
                        var crypto = coin;
                        if (crypto.BuyOrderId == null)
                        {
                            //var results2 = services.GetOrders(crypto.Coin);
                            //var lastSell = results2.Where(x => x.Side == "BUY").OrderByDescending(y => Convert.ToDateTime(y.SequenceTimestamp)).FirstOrDefault();
                            var lastSell = GetLastOrder(crypto.Coin);
                            buyOrderId = Guid.Parse(lastSell?.OrderId);
                            crypto.BuyOrderId = buyOrderId;
                            ctx.CryptoTracker.Attach(crypto);
                            ctx.Entry(crypto).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                            ctx.SaveChanges();
                            ctx.Entry(crypto).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                        }
                        //continue; //already in tracker
                    }
                    else
                    {
                        //create new crypto tracker
                        coin = new CryptoTracker();
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
                        coin.PurchasedMacd = Convert.ToDecimal(results["MACD"].ToString());
                        coin.LastPrice = Convert.ToDecimal(results["lastPrice"].ToString());
                        coin.DatePurchased = helper.easternTime;
                        coin.DateCreated = helper.easternTime;
                        coin.LastUpdatedBy = 1;
                        ctx.CryptoTracker.Add(coin);
                        ctx.Entry(coin).State = Microsoft.EntityFrameworkCore.EntityState.Added;
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
            var Cryptos = ctx.CryptoTracker.Where(x => x.PriceSold == null && x.DateSold == null && x.BuyStatus == "FILLED").ToList();
            foreach(var crypto in Cryptos)
            {
                var currency = crypto.Coin.Split('-');
                var account = services.GetAccounts().Where(x => x.Currency == currency[0]).FirstOrDefault();
                var publicProduct = PublicProducts.Where(x => x.DisplayName == crypto.Coin).FirstOrDefault();
                var valueofNonTrackedAsset = Math.Round(Convert.ToDecimal(account.AvailableBalance.Value) * Convert.ToDecimal(publicProduct?.Price), 5);
                //if (decimal.TryParse(account?.AvailableBalance.Value, out decimal bal) && bal <= 1.5m)
                if (valueofNonTrackedAsset <= 1.5m)
                {
                    var ct = crypto;
                    var results = services.GetQuote(ct.Coin);
                    ct.PriceSold = Convert.ToDecimal(results["lastPrice"].ToString());
                    ct.SellOrderId = Guid.Empty;
                    ct.SellStatus = "FILLED";
                    ct.DateSold = helper?.easternTime;
                    ct.DateUpdated = helper?.easternTime;
                    ctx.CryptoTracker.Attach(ct);
                    ctx.Entry(ct).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
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
