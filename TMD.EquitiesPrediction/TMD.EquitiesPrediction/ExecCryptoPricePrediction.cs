using Amazon.Runtime;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Threading.Tasks;
using TMD.Services;
using TMDInvestment.Models;
using TMDInvestments.Context;
using TMDInvestments.Models;
using TMDInvestments.Services;
using Coinbase.AdvancedTrade.Models;

namespace TMD.EquitiesPrediction
{
    public sealed class ExecCryptoPricePrediction
    {
        ILogger? _logger;
        CoinBaseSettings? _settings;        
        CoinBaseService services = null;
        TmdContext ctx = null;
        Dictionary<dynamic, TradeDirection> BuySellStockList;
        public ExecCryptoPricePrediction(ILogger logger, CoinBaseSettings settings, IHttpClientFactory httpClientFactory) {
            
            _logger = logger;
            _settings = settings;
            services = new CoinBaseService(logger, settings, httpClientFactory);
            ctx = new TmdContext();
        }
        public void Exec()
        {
            //do
            //{
                //var caca = services.GetPortfolios();
                //var popo = services.GetProducts();
                //var results = services.GetQuote("BTC-USDC");
                Run();
            //}
            //while (true);
        }
        private void Run()
        {
            //Get Account Info
            List<PortfolioBreakdown> account = services.GetPortfolios();
            _logger.LogInformation($"Get account info");

            //JsonElement securitiesAccount = account.GetProperty("securitiesAccount");
            //JsonElement initialBalances = securitiesAccount.GetProperty("initialBalances");
            decimal cash = account.Sum(x => Convert.ToDecimal(x.Portfolio_balances.total_Balance.value));
            decimal cryptoBalance = account.Sum(x => Convert.ToDecimal(x.Portfolio_balances.total_crypto_balance.value));
            //var totalAvailiableforLongTrade = (cash);
            var totalAvailiableforLongTrade = 1000m;

            //if (totalAvailiableforLongTrade < 100)
            //{
            //    _logger.LogError($"Cash and margin balance below threshold ${totalAvailiableforLongTrade}");
            //    return;
            //}

            _logger.LogInformation($"Cash Balance ${cash}. Crypto Balance ${cryptoBalance}. Total Availiable ${totalAvailiableforLongTrade}");
            //GetSettings
            var settings = ctx.RulesEngine.FirstOrDefault();
            _logger.LogInformation($"Get settings");

            //Get stock already in trade being tracked.
            List<CryptoTracker> cryptoTracker = ctx.CryptoTracker.Where(x => x.PriceSold == null && x.DateSold == null).ToList();
            var cryptoTrackerList = cryptoTracker.Select(y => y.Coin);
            _logger.LogInformation($"Get exiting stock tracker");

            //Get list of stocks to trade not in tracker
            var cryptoList = ctx.CryptoList.Where(x => !cryptoTrackerList.Contains(x.Coin)).ToList();
            _logger.LogInformation($"Get stock list to trade");

            //stock sold/brought to cover in the last 24 hrs
            var CryptoHold24Hr = ctx.CryptoTracker.ToList().Where(x => x.PriceSold != null && x.DateSold != null && (x.DateSold - DateTime.Now).Value.TotalHours > 24).ToList().Select(y => y.Coin);

            //Loop through and evaluate.
            int? shortCnt = 0;
            int? longCnt = 0;
            int? stockCnt = 0;

            _logger.LogInformation($"Iterate stock list, check indicators for buy/sell short");

            BuySellStockList = new Dictionary<dynamic, TradeDirection>();
            if (cryptoTracker.Count() < settings.MaxNumberofStockstoBuy)
                foreach (var crypto in cryptoList)
                {
                    //skip loop if stock amount equals max amount to own
                    if ((cryptoTracker.Count() + stockCnt) >= settings.MaxNumberofStockstoBuy)
                        break;

                    //Check it stock has been brought in the las 24 hrs
                    if (CryptoHold24Hr.Contains(crypto.Coin))
                    {
                        _logger.LogWarning($"Stock {crypto.Coin} - ({crypto.Coin} was purchased les than 24h ago, skipping");
                        continue;
                    }
                    _logger.LogInformation($"Checking {crypto.Coin} - ({crypto.Coin})");

                    var results = services.GetQuote(crypto.Coin);

                    if (Convert.ToDecimal(results["lastPrice"].ToString()) >= settings.MinStockPrice && !crypto.ExcludeFromAutomation)
                    {
                        shortCnt = 0;
                        longCnt = 0;
                        //Check over brough, over sold RSI
                        if (Convert.ToDecimal(results["RSI"].ToString()) >= 51) { shortCnt++; }
                        if (Convert.ToDecimal(results["RSI"].ToString()) <= 49) { longCnt++; }

                        //Check long SMA
                        if (Convert.ToDecimal(results["SMA"].ToString()) <= Convert.ToDecimal(results["lastPrice"].ToString())) { shortCnt++; }
                        if (Convert.ToDecimal(results["SMA"].ToString()) >= Convert.ToDecimal(results["lastPrice"].ToString())) { longCnt++; }

                        //Check long EMA
                        if (Convert.ToDecimal(results["EMA"].ToString()) <= Convert.ToDecimal(results["lastPrice"].ToString())) { shortCnt++; }
                        if (Convert.ToDecimal(results["EMA"].ToString()) >= Convert.ToDecimal(results["lastPrice"].ToString())) { longCnt++; }

                        //Check VMAP
                        if (Convert.ToDecimal(results["VWAP"].ToString()) <= Convert.ToDecimal(results["lastPrice"].ToString())) { shortCnt = shortCnt + 2; }
                        if (Convert.ToDecimal(results["VWAP"].ToString()) >= Convert.ToDecimal(results["lastPrice"].ToString())) { longCnt = longCnt + 2; }

                        //Check EmaNinePeriod
                        if (Convert.ToDecimal(results["EmaNinePeriod"].ToString()) <= Convert.ToDecimal(results["lastPrice"].ToString())) { shortCnt++; }
                        if (Convert.ToDecimal(results["EmaNinePeriod"].ToString()) >= Convert.ToDecimal(results["lastPrice"].ToString())) { longCnt++; }

                        //Check EmaTwelvePeriod
                        if (Convert.ToDecimal(results["EmaTwelvePeriod"].ToString()) <= Convert.ToDecimal(results["lastPrice"].ToString())) { shortCnt++; }
                        if (Convert.ToDecimal(results["EmaTwelvePeriod"].ToString()) >= Convert.ToDecimal(results["lastPrice"].ToString())) { longCnt++; }

                        //Check EmaTwentySixPeriod
                        if (Convert.ToDecimal(results["EmaTwentySixPeriod"].ToString()) <= Convert.ToDecimal(results["lastPrice"].ToString())) { shortCnt++; }
                        if (Convert.ToDecimal(results["EmaTwentySixPeriod"].ToString()) >= Convert.ToDecimal(results["lastPrice"].ToString())) { longCnt++; }

                        //Check MACD 9 Signal
                        if (Convert.ToDecimal(results["MACD9"].ToString()) >= Convert.ToDecimal(results["MACD"].ToString())) { shortCnt = shortCnt + 2; }
                        if (Convert.ToDecimal(results["MACD9"].ToString()) <= Convert.ToDecimal(results["MACD"].ToString())) { longCnt = longCnt + 2; }

                        //Check MACD
                        if (Convert.ToDecimal(results["MACD"].ToString()) <= 0) { shortCnt++; }
                        if (Convert.ToDecimal(results["MACD"].ToString()) >= 0) { longCnt++; }

                        if (longCnt >= 7)
                        {
                            _logger.LogInformation($"Buying {crypto.Coin} - ({crypto.Coin}). Long score: {longCnt}. Short score {shortCnt}");
                            BuySellStockList.Add(results, TradeDirection.buy);
                            //BuySellStock(null, results, TradeDirection.buy, services);
                            stockCnt++;
                        }
                        else
                        if (shortCnt >= 7 && (bool)settings.ShortEnabled)
                        {
                            _logger.LogInformation($"Shorting {crypto.Coin} - ({crypto.Coin}). Long score: {longCnt}. Short score {shortCnt}");
                            BuySellStockList.Add(results, TradeDirection.buy);
                            //BuySellStock(null, results, TradeDirection.sellShort, services);
                            stockCnt++;
                        }
                        else
                        {
                            _logger.LogInformation($"Not buying/selling short {crypto.Coin} - ({crypto.Coin}). Long score: {longCnt}. Short score {shortCnt}");
                        }
                    }
                }
            if(BuySellStockList.Count > 0)
                CalculatePurchaseAmount(totalAvailiableforLongTrade, ref BuySellStockList);
            //Re-Get stock already in trade being tracked.
            cryptoTracker = ctx.CryptoTracker.Where(x => x.PriceSold == null && x.DateSold == null).ToList();
            //Loop through current stock and evaluate
            _logger.LogInformation($"Checking current brought/shorted stock");
            foreach (var stock in cryptoTracker)
            {
                var st = stock;
                _logger.LogInformation($"Checking {stock.Coin} - ({stock.Name})");
                JsonObject results = services.GetQuote(stock.Coin);

                //just trade
                _logger.LogWarning($"Stock {stock.Coin} - ({stock.Name}) Time since traded hrs {Math.Round(((DateTime.Now - stock.DatePurchased.Value).TotalHours - 24), 2)}. Traded Price ${stock.PricePurchased} traded direction {Enum.GetName(typeof(TradeDirection), stock.TradeDirection)} stock current price {Convert.ToDecimal(results["lastPrice"].ToString())}");
                AnalyiseOwnedStock(st, results);
            }
        }
        bool BuySellStock(CryptoTracker cryptoTracker, dynamic order, TradeDirection direction, decimal perStock, CoinBaseService service)
        {
            string jsonOrder = CreateTradeObject(order, direction);
            service.SetOrder(jsonOrder, direction); //to be done
            //string symbol = order["Symbol"].ToString();
            //if direction is buy or sell short, stocktracker will be null.
            //StockTracker stocktracker = ctx.StockTracker.Where(x => x.Symbol == symbol && x.DateSold == null && x.PriceSold == null).FirstOrDefault();
            SaveStockTracker(cryptoTracker, direction, perStock, order, order);
            return true;
        }
        string CreateTradeObject(dynamic results, TradeDirection direction)
        {//to be done
            dynamic order = new ExpandoObject();
            return System.Text.Json.JsonSerializer.Serialize(order); //not done yet
        }
        bool SaveStockTracker(CryptoTracker cryptoTracker, TradeDirection direction, decimal perStock, dynamic results, dynamic order)
        {
            if (cryptoTracker != null && (direction == TradeDirection.sell || direction == TradeDirection.buyToCover))
            {
                cryptoTracker.PriceSold = Convert.ToDecimal(results["lastPrice"].ToString());
                cryptoTracker.SoldRsi = Convert.ToDecimal(results["RSI"].ToString());
                cryptoTracker.SoldSma200 = Convert.ToDecimal(results["SMA"].ToString());
                cryptoTracker.SoldEma200 = Convert.ToDecimal(results["EMA"].ToString());
                cryptoTracker.SoldVwap = Convert.ToDecimal(results["VWAP"].ToString());
                cryptoTracker.SoldEma9 = Convert.ToDecimal(results["EmaNinePeriod"].ToString());
                cryptoTracker.SoldEma12 = Convert.ToDecimal(results["EmaTwelvePeriod"].ToString());
                cryptoTracker.SoldEma26 = Convert.ToDecimal(results["EmaTwentySixPeriod"].ToString());
                cryptoTracker.SoldMacd = Convert.ToDecimal(results["MACD"].ToString());
                cryptoTracker.DateSold = DateTime.Now;
                cryptoTracker.DateUpdated = DateTime.Now;
                ctx.CryptoTracker.Attach(cryptoTracker);
                ctx.Entry(cryptoTracker).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
            }
            else
            {
                cryptoTracker = new CryptoTracker();
                cryptoTracker.Coin = results["Symbol"].ToString();
                cryptoTracker.Name = results["Name"].ToString();
                cryptoTracker.TradeDirection = (int)direction;
                cryptoTracker.PricePurchased = Convert.ToDecimal(results["lastPrice"].ToString());
                cryptoTracker.Amount = perStock;
                cryptoTracker.PurchasedRsi = Convert.ToDecimal(results["RSI"].ToString());
                cryptoTracker.PurchasedSma200 = Convert.ToDecimal(results["SMA"].ToString());
                cryptoTracker.PurchasedEma200 = Convert.ToDecimal(results["EMA"].ToString());
                cryptoTracker.PurchasedVwap = Convert.ToDecimal(results["VWAP"].ToString());
                cryptoTracker.PurchasedEma9 = Convert.ToDecimal(results["EmaNinePeriod"].ToString());
                cryptoTracker.PurchasedEma12 = Convert.ToDecimal(results["EmaTwelvePeriod"].ToString());
                cryptoTracker.PurchasedEma26 = Convert.ToDecimal(results["EmaTwentySixPeriod"].ToString());
                cryptoTracker.PurchasedMacd = Convert.ToDecimal(results["MACD"].ToString());
                cryptoTracker.DatePurchased = DateTime.Now;
                cryptoTracker.DateCreated = DateTime.Now;
                cryptoTracker.LastUpdatedBy = 1;
                ctx.CryptoTracker.Add(cryptoTracker);
                ctx.Entry(cryptoTracker).State = Microsoft.EntityFrameworkCore.EntityState.Added;
            }
            try
            {
                ctx.SaveChanges();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Source: {ex.Source} stack trace: {ex.StackTrace} inner exception {ex?.InnerException?.Message ?? ""} error: {ex.Message}");
            }
            
            return true;
        }
        bool UpdateLastPrice(decimal lastPrice, ref CryptoTracker cryptoTracker)
        {
            cryptoTracker.LastPrice = lastPrice;
            cryptoTracker.DateUpdated = DateTime.Now;
            ctx.Entry(cryptoTracker).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
            ctx.SaveChanges();
            return true;
        }

        void AnalyiseOwnedStock(CryptoTracker crypto, dynamic? results)
        {
            //calculate percent difference
            decimal? currentPrice = crypto.PricePurchased;
            decimal? lastPrice = Convert.ToDecimal(results["lastPrice"].ToString());
            decimal? percentDiff = ((Math.Abs((decimal)(currentPrice - lastPrice))) / ((currentPrice + lastPrice) / 2)) * 100;
            if (crypto.Amount < 0 && crypto.TradeDirection == (int)TradeDirection.sellShort) //selling short
            {
                if (crypto.LastPrice != null && crypto.LastPrice.Value > Convert.ToDecimal(results["lastPrice"].ToString()))
                {
                    //Good update higher price
                    _logger.LogInformation($"crypto {crypto.Coin} - ({crypto.Name}) Posting gains since shorting. Updating last Price.");
                    UpdateLastPrice(Convert.ToDecimal(results["lastPrice"].ToString()), ref crypto);
                }
                else if (crypto.LastPrice != null && crypto.LastPrice.Value < Convert.ToDecimal(results["lastPrice"].ToString()) && percentDiff > 5m)
                {
                    //if stock price fall by more than 5% of price
                    _logger.LogInformation($"stock {crypto.Coin} - ({crypto.Name}) has fallen by more then 5% since purchase. Buying to cover.");
                    BuySellStock(crypto, results, TradeDirection.buyToCover, 0, services);
                }
                else
                {
                    _logger.LogInformation($"stock {crypto.Coin} - ({crypto.Name}) still within 5% tolerance since purchase. Updating last Price.");
                    UpdateLastPrice(Convert.ToDecimal(results["lastPrice"].ToString()), ref crypto);
                }
            }
            else //Going Long
            {
                if (crypto.LastPrice != null && crypto.LastPrice.Value < Convert.ToDecimal(results["lastPrice"].ToString()))
                {
                    //Good update higher price
                    _logger.LogInformation($"stock {crypto.Coin} - ({crypto.Name}) Posting gains since purchase. Updating last Price.");
                    UpdateLastPrice(Convert.ToDecimal(results["lastPrice"].ToString()), ref crypto);
                }
                else if (crypto.LastPrice != null && crypto.LastPrice.Value > Convert.ToDecimal(results["lastPrice"].ToString()) && percentDiff > 5m)
                {
                    //if stock price fall by more than 5% of price
                    _logger.LogInformation($"stock {crypto.Coin} - ({crypto.Name}) has fallen by more then 5% since purchase. Buying to cover.");
                    BuySellStock(crypto, results, TradeDirection.sell, 0, services);
                }
                else
                {
                    _logger.LogInformation($"stock {crypto.Coin} - ({crypto.Name}) still within 5% tolerance since purchase. Updating last Price.");
                    UpdateLastPrice(Convert.ToDecimal(results["lastPrice"].ToString()), ref crypto);
                }

            }
        }
        void CalculatePurchaseAmount(decimal? availiabletoTrade, ref Dictionary<dynamic, TradeDirection> BuySellStockList)
        {
            decimal? perStock = availiabletoTrade / BuySellStockList.Count;
            foreach (var item in BuySellStockList)
            {
                var result = item.Key;
                var direction = item.Value;
                if (perStock >= 10)
                {
                    BuySellStock(null, result, direction, (decimal)perStock, services);
                }
            }
        }
    }
}
