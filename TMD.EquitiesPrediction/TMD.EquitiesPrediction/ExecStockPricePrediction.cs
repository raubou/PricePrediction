using Microsoft.Extensions.Logging;
using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using TMDInvestments.Context;
using TMDInvestments.Models;
using TMDInvestments.Models.Models.Polygon;
using TMDInvestments.Services;

namespace TMD.EquitiesPrediction
{
    public sealed class ExecStockPricePrediction
    {
        ILogger? _logger;
        SchwabSettings? _settings;
        IHttpClientFactory? _httpClientFactory;
        SchwabServices services = null;
        TmdContext ctx = null;
        Dictionary<dynamic, TradeDirection> BuySellStockList;
        public ExecStockPricePrediction(ILogger logger, SchwabSettings settings, IHttpClientFactory httpClientFactory) { 
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _settings = settings;
            services = new SchwabServices(logger, settings, httpClientFactory);
            ctx = new TmdContext();
        }

        public void Exec()
        {
            //do
            //{
                DateTime runTime = DateTime.Now;
                TimeSpan startTime = new TimeSpan(9, 30, 0);
                TimeSpan endTime = new TimeSpan(16, 30, 0);

                if ((runTime.DayOfWeek != DayOfWeek.Saturday && runTime.DayOfWeek != DayOfWeek.Sunday && runTime.TimeOfDay >= startTime && runTime.TimeOfDay <= endTime))
                    Run();
                else
                {
                    _logger.LogWarning("Time is outside of trading window.");
                }
                
            //}
            //while (true);
        }

        private void Run()
        {
            //Get Account Info
            JsonElement account = services.GetAccountInfo();
            _logger.LogInformation($"Get account info");

            JsonElement securitiesAccount = account.GetProperty("securitiesAccount");
            JsonElement initialBalances = securitiesAccount.GetProperty("initialBalances");
            decimal cash = Convert.ToDecimal(initialBalances.GetProperty("cashBalance").ToString());
            decimal margin = Convert.ToDecimal(initialBalances.GetProperty("margin").ToString());
            var totalAvailiableforLongTrade = (cash + margin);

            if(totalAvailiableforLongTrade < 100)
            {
                _logger.LogError($"Cash and margin balance below threshold ${totalAvailiableforLongTrade}");
                return;
            }

            _logger.LogInformation($"Cash Balance ${cash}. Margin Balance ${margin}. Total Availiable ${totalAvailiableforLongTrade}");
            //GetSettings
            var settings = ctx.RulesEngine.FirstOrDefault();
            _logger.LogInformation($"Get settings");

            //Get stock already in trade being tracked.
            List<StockTracker> stockTracker = ctx.StockTracker.Where(x => x.PriceSold == null && x.DateSold == null).ToList();
            var stockTrackerList = stockTracker.Select(y => y.Symbol);
            _logger.LogInformation($"Get exiting stock tracker");

            //Get list of stocks to trade not in tracker
            var stockList = ctx.StockList.Where(x => !stockTrackerList.Contains(x.Symbol)).ToList();
            _logger.LogInformation($"Get stock list to trade");

            //stock sold/brought to cover in the last 24 hrs
            var StockHold24Hr = ctx.StockTracker.ToList().Where(x => x.PriceSold != null && (x.DateSold - DateTime.Now).Value.TotalHours > 24).ToList().Select(y => y.Symbol);

            //Loop through and evaluate.
            int? shortCnt = 0;
            int? longCnt = 0;
            int? stockCnt = 0;

            _logger.LogInformation($"Iterate stock list, check indicators for buy/sell short");

            BuySellStockList = new Dictionary<dynamic, TradeDirection>();
            if (stockTracker.Count() < settings.MaxNumberofStockstoBuy)
                foreach (var stock in stockList)
                {
                    //skip loop if stock amount equals max amount to own
                    if ((stockTracker.Count() + stockCnt) >= settings.MaxNumberofStockstoBuy)
                        break;

                    //Check it stock has been brought in the las 24 hrs
                    if (StockHold24Hr.Contains(stock.Symbol)){
                        _logger.LogWarning($"Stock {stock.Symbol} - ({stock.Name} was purchased les than 24h ago, skipping");
                        continue;
                    }
                    _logger.LogInformation($"Checking {stock.Symbol} - ({stock.Name})");

                    var results = services.GetQuote(stock.Symbol, false);

                    if (Convert.ToDecimal(results["lastPrice"].ToString()) >= settings.MinStockPrice && !stock.ExcludeFromAutomation)
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

                        if (longCnt >= 8)
                        {
                            _logger.LogInformation($"Buying {stock.Symbol} - ({stock.Name}). Long score: {longCnt}. Short score {shortCnt}");
                            BuySellStockList.Add(results, TradeDirection.buy);
                            //BuySellStock(null, results, TradeDirection.buy, services);
                            stockCnt++;
                        }else
                        if (shortCnt >= 8 && (bool)settings.ShortEnabled)
                        {
                            _logger.LogInformation($"Shorting {stock.Symbol} - ({stock.Name}). Long score: {longCnt}. Short score {shortCnt}");
                            BuySellStockList.Add(results, TradeDirection.sellShort);
                            //BuySellStock(null, results, TradeDirection.sellShort, services);
                            stockCnt++;
                        }
                        else
                        {
                            _logger.LogInformation($"Not buying/selling short {stock.Symbol} - ({stock.Name}). Long score: {longCnt}. Short score {shortCnt}");
                        }
                    }                    
                }
            if (BuySellStockList.Count > 0)
                CalculatePurchaseAmount(totalAvailiableforLongTrade, ref BuySellStockList);
            //Re-Get stock already in trade being tracked.
            stockTracker = ctx.StockTracker.Where(x => x.PriceSold == null && x.DateSold == null).ToList();
            //Loop through current stock and evaluate
            _logger.LogInformation($"Checking current brought/shorted stock");
            foreach (var stock in stockTracker)
            {
                var st = stock;
                _logger.LogInformation($"Checking {stock.Symbol} - ({stock.Name})");
                JsonObject results = services.GetQuote(stock.Symbol, false);
                if (!(bool)settings.DayTradeEnabled)
                {
                    if ((DateTime.Now - stock.DatePurchased.Value).TotalDays > 1)
                    {
                        //trade after 24 hrs
                        _logger.LogWarning($"Stock {stock.Symbol} - ({stock.Name}) Time since traded Past 24 hrs hrs {Math.Round(((DateTime.Now - stock.DatePurchased.Value).TotalHours - 24), 2)}. Traded Price ${stock.PricePurchased} traded direction {Enum.GetName(typeof(TradeDirection), stock.TradeDirection)} stock current price {Convert.ToDecimal(results["lastPrice"].ToString())}");
                        AnalyiseOwnedStock(st, results);
                    }
                    else
                    {
                        //not pass 24h period
                        _logger.LogWarning($"Stock {stock.Symbol} - ({stock.Name}) Cannot be transacted on before 24h period when day trade is disabled. Time til tradeable hrs {Math.Round((24 - (DateTime.Now - stock.DatePurchased.Value).TotalHours),2)}. Traded Price ${stock.PricePurchased} traded direction {Enum.GetName(typeof(TradeDirection), stock.TradeDirection)} stock current price {Convert.ToDecimal(results["lastPrice"].ToString())}");
                        UpdateLastPrice(Convert.ToDecimal(results["lastPrice"].ToString()), ref st);
                    }
                }
                else
                {
                    //just trade
                    _logger.LogWarning($"Stock {stock.Symbol} - ({stock.Name}) Time since traded hrs {Math.Round(((DateTime.Now - stock.DatePurchased.Value).TotalHours - 24), 2)}. Traded Price ${stock.PricePurchased} traded direction {Enum.GetName(typeof(TradeDirection), stock.TradeDirection)} stock current price {Convert.ToDecimal(results["lastPrice"].ToString())}");
                    AnalyiseOwnedStock(st, results);
                }
            }
        }
        bool BuySellStock(StockTracker stocktracker, dynamic order, TradeDirection direction, int qty, SchwabServices service)
        {
            string jsonOrder = CreateTradeObject(order, direction);
            service.SetOrder(jsonOrder, direction); //to be done
            //string symbol = order["Symbol"].ToString();
            //if direction is buy or sell short, stocktracker will be null.
            SaveStockTracker(stocktracker, direction, qty, order, order);
            return true;
        }
        string CreateTradeObject(dynamic results, TradeDirection direction) {//to be done
            dynamic order = new ExpandoObject();
            return System.Text.Json.JsonSerializer.Serialize(order); //not done yet
        }
        bool SaveStockTracker(StockTracker stockTracker, TradeDirection direction, int qty,dynamic results, dynamic order) { 
            if(stockTracker != null && (direction == TradeDirection.sell || direction == TradeDirection.buyToCover)) {
                stockTracker.PriceSold = Convert.ToDecimal(results["lastPrice"].ToString());
                stockTracker.SoldRsi = Convert.ToDecimal(results["RSI"].ToString());
                stockTracker.SoldSma200 = Convert.ToDecimal(results["SMA"].ToString());
                stockTracker.SoldEma200 = Convert.ToDecimal(results["EMA"].ToString());
                stockTracker.SoldVwap = Convert.ToDecimal(results["VWAP"].ToString());
                stockTracker.SoldEma9 = Convert.ToDecimal(results["EmaNinePeriod"].ToString());
                stockTracker.SoldEma12 = Convert.ToDecimal(results["EmaTwelvePeriod"].ToString());
                stockTracker.SoldEma26 = Convert.ToDecimal(results["EmaTwentySixPeriod"].ToString());
                stockTracker.SoldMacd = Convert.ToDecimal(results["MACD"].ToString());
                stockTracker.DateSold = DateTime.Now;
                stockTracker.DateUpdated = DateTime.Now;
                ctx.StockTracker.Attach(stockTracker);
                ctx.Entry(stockTracker).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
            }
            else
            {
                stockTracker = new StockTracker();
                stockTracker.Symbol = results["Symbol"].ToString();
                stockTracker.Name = results["Name"].ToString();
                stockTracker.TradeDirection = (int)direction;
                stockTracker.PricePurchased = Convert.ToDecimal(results["lastPrice"].ToString());
                stockTracker.Quantity = qty;
                stockTracker.PurchasedRsi = Convert.ToDecimal(results["RSI"].ToString());
                stockTracker.PurchasedSma200 = Convert.ToDecimal(results["SMA"].ToString());
                stockTracker.PurchasedEma200 = Convert.ToDecimal(results["EMA"].ToString());
                stockTracker.PurchasedVwap = Convert.ToDecimal(results["VWAP"].ToString());
                stockTracker.PurchasedEma9 = Convert.ToDecimal(results["EmaNinePeriod"].ToString());
                stockTracker.PurchasedEma12 = Convert.ToDecimal(results["EmaTwelvePeriod"].ToString());
                stockTracker.PurchasedEma26 = Convert.ToDecimal(results["EmaTwentySixPeriod"].ToString());
                stockTracker.PurchasedMacd = Convert.ToDecimal(results["MACD"].ToString());
                stockTracker.DatePurchased = DateTime.Now;
                stockTracker.DateCreated = DateTime.Now;
                stockTracker.LastUpdatedBy = 1;
                ctx.StockTracker.Add(stockTracker);
                ctx.Entry(stockTracker).State = Microsoft.EntityFrameworkCore.EntityState.Added;
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
        bool UpdateLastPrice(decimal lastPrice, ref StockTracker stockTracker)
        {
            stockTracker.LastPrice = lastPrice;
            stockTracker.DateUpdated = DateTime.Now;
            ctx.Entry(stockTracker).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
            ctx.SaveChanges();
            return true;
        }

        void AnalyiseOwnedStock(StockTracker stock, dynamic? results) {
            //calculate percent difference
            decimal? currentPrice = stock.PricePurchased;
            decimal? lastPrice = Convert.ToDecimal(results["lastPrice"].ToString());
            decimal? percentDiff = ((Math.Abs((decimal)(currentPrice - lastPrice))) / ((currentPrice + lastPrice) /2)) * 100;
            if (stock.Quantity < 0 && stock.TradeDirection == (int)TradeDirection.sellShort) //selling short
            {
                if (stock.LastPrice != null && stock.LastPrice.Value > Convert.ToDecimal(results["lastPrice"].ToString()))
                {
                    //Good update higher price
                    _logger.LogInformation($"stock {stock.Symbol} - ({stock.Name}) Posting gains since shorting. Updating last Price.");
                    UpdateLastPrice(Convert.ToDecimal(results["lastPrice"].ToString()), ref stock);
                }
                else if (stock.LastPrice != null && stock.LastPrice.Value < Convert.ToDecimal(results["lastPrice"].ToString()) && percentDiff > 5m)
                {
                    //if stock price fall by more than 5% of price
                    _logger.LogInformation($"stock {stock.Symbol} - ({stock.Name}) has fallen by more then 5% since purchase. Buying to cover.");
                    BuySellStock(stock, results, TradeDirection.buyToCover,0, services);
                }
                else
                {
                    _logger.LogInformation($"stock {stock.Symbol} - ({stock.Name}) still within 5% tolerance since purchase. Updating last Price.");
                    UpdateLastPrice(Convert.ToDecimal(results["lastPrice"].ToString()), ref stock);
                }
            }
            else //Going Long
            {
                if (stock.LastPrice != null && stock.LastPrice.Value < Convert.ToDecimal(results["lastPrice"].ToString()))
                {
                    //Good update higher price
                    _logger.LogInformation($"stock {stock.Symbol} - ({stock.Name}) Posting gains since purchase. Updating last Price.");
                    UpdateLastPrice(Convert.ToDecimal(results["lastPrice"].ToString()), ref stock);
                }
                else if (stock.LastPrice != null && stock.LastPrice.Value > Convert.ToDecimal(results["lastPrice"].ToString()) && percentDiff > 5m)
                {
                    //if stock price fall by more than 5% of price
                    _logger.LogInformation($"stock {stock.Symbol} - ({stock.Name}) has fallen by more then 5% since purchase. Buying to cover.");
                    BuySellStock(stock, results, TradeDirection.sell,0, services);
                }
                else
                {
                    _logger.LogInformation($"stock {stock.Symbol} - ({stock.Name}) still within 5% tolerance since purchase. Updating last Price.");
                    UpdateLastPrice(Convert.ToDecimal(results["lastPrice"].ToString()), ref stock);
                }

            }
        }

        void CalculatePurchaseAmount(decimal? availiabletoTrade, ref Dictionary<dynamic, TradeDirection> BuySellStockList)
        {
            decimal? perStock = availiabletoTrade / BuySellStockList.Count;
            foreach (var item in BuySellStockList) {
                var result = item.Key;
                var direction = item.Value;
                var lastPrice = Convert.ToDecimal(result["lastPrice"].ToString());
                var qty = Convert.ToInt32(perStock / lastPrice);
                if (qty >= 2) {
                    BuySellStock(null, result, direction, qty, services);
                }
            }
        }
    }
}
