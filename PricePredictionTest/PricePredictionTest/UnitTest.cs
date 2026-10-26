using Bartergora2._0.Models;
using Coinbase.AdvancedTrade.Enums;
using Coinbase.AdvancedTrade.Models;
using Coinbase.AdvancedTrade.Models.Public;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using MongoDB.Driver;
using NewsAggregator.models.returnModel;
using System.Diagnostics;
using System.Globalization;
using System.ServiceModel.Syndication;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using TMD.Database.Connect.Managers;
using TMD.PricePrediction;
using TMD.Services;
using TMD.Services.Service;
using TMDInvestments.Context;
using TMDInvestments.Models;
using TMDInvestments.Models.Models.Commerce;
using TMDInvestments.Models.Models.Polygon;
using TMDInvestments.Services;
using static Microsoft.ML.Data.SchemaDefinition;
using static MongoDB.Driver.WriteConcern;


namespace PricePredictionTest
{
    [TestClass]
    public class UnitTest
    {
        ILogger? _logger;
        CoinBaseSettings? _settings;
        CoinBaseService services = null;
        TmdContext ctx = null;
        BartergoraDBContext ctx2 = null;
        SchwabServices SchwabService = null;
        SchwabSettings? _settings2;
        TaAPISettings? _taApiSettings;
        IHttpClientFactory? httpClientFactory;
        ETradeService ETradeService = null;
        ETradeSettings? _settings3;
        Polygon? _polygon = null;
        yfapiSettings? yfapiSettings = null;
        StackCommerceService? stackCommerceService = null;
        RakutenService? rakutenService = null;
        CommissionJunctionService? commissionJunctionService = null;
        TraderGoService _traderGoService = null;
        Manager manager = null;
        MailSettings mailSettings = null;
        MLPricePredict ml = null;
        BartergoraDataLoadService bartergoraDataLoadService = null;
        DataMuseService dataMuseService = null;
        //KalshiService kalshiService = null;
        Helper helper = null;
        RulesEngine settings = null;
        public UnitTest()
        {
        }
        [TestInitialize]
        public void GetSettings()
        {
            ctx = new TmdContext();
            ctx2 = new BartergoraDBContext();
            var builder = new ConfigurationBuilder();
            builder.SetBasePath(Directory.GetCurrentDirectory())
                   .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            IConfiguration config = builder.Build();

            var ConnectionStrings = config.GetSection("ConnectionStrings").Get<ConnectionSettings>();
            mailSettings = config.GetSection("MailSettings").Get<MailSettings>();
            var _finnHub = config.GetSection("finnhub").Get<FinnHub>();
            //var Polygon = config.GetSection("polygon").Get<Polygon>();
            _settings2 = config.GetSection("SchwabAPI").Get<SchwabSettings>();
            _settings = config.GetSection("CoinBase").Get<CoinBaseSettings>();
            _settings3 = config.GetSection("ETradeAPI").Get<ETradeSettings>();
            _polygon = config.GetSection("polygon").Get<Polygon>();
            _taApiSettings = config.GetSection("TaApi").Get<TaAPISettings>();
            yfapiSettings = config.GetSection("yfapi").Get<yfapiSettings>();
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                //builder.AddConsole();
                //builder.AddDebugOutput();
                builder.SetMinimumLevel(LogLevel.Trace);
            }
            );
            
            _logger = loggerFactory.CreateLogger<UnitTest>();
            var client = new ServiceCollection().AddHttpClient("TMD")
                .UseSocketsHttpHandler((handler, _) =>
                handler.PooledConnectionLifetime = TimeSpan.FromMinutes(7)) // Recreate connection every 2 minutes
                .SetHandlerLifetime(Timeout.InfiniteTimeSpan); // Disable rotation, as it is handled by PooledConnectionLifetime

            client.Services.AddDbContextFactory<TmdContext>(options =>
            options.UseSqlServer(ConnectionStrings.Connection));
            var serviceProvider = client.Services.BuildServiceProvider();
            httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
            var ctxFactory = serviceProvider.GetRequiredService<IDbContextFactory<TmdContext>>();
            helper = new Helper(_logger, _settings2, _settings3, null, ctxFactory);
            services = new CoinBaseService(_logger, _settings, helper, httpClientFactory, ctxFactory);
            SchwabService = new SchwabServices(_logger, _settings2, httpClientFactory, helper, ctxFactory);
            ETradeService = new ETradeService(_logger, _settings3, httpClientFactory, ctxFactory);
            stackCommerceService = new StackCommerceService(_logger, httpClientFactory);
            rakutenService = new RakutenService(_logger, httpClientFactory, false);
            //_traderGoService = new TraderGoService(yfapiSettings, _logger, httpClientFactory, false, ctxFactory);
            _traderGoService = new TraderGoService(yfapiSettings, _logger, SchwabService, helper, httpClientFactory, false, ctxFactory);
            commissionJunctionService = new CommissionJunctionService(_logger, httpClientFactory, false);
            //manager = new Manager(_logger);
            manager = new Manager("", _finnHub, _polygon, httpClientFactory, _logger);
            ml = new MLPricePredict();
            bartergoraDataLoadService = new BartergoraDataLoadService(_logger, httpClientFactory);
            dataMuseService = new DataMuseService(_logger, httpClientFactory);
            //kalshiService = new KalshiService(_logger, httpClientFactory);            
            settings = ctx.RulesEngine.FirstOrDefault();
        }
        [TestMethod]
        public void SendMail()
        {
            manager.SendEmail(mailSettings.SenderName, mailSettings.UserName, mailSettings.Password, "Polycard, Richard", new string[] { "raubou@yahoo.com", "raubou@gmail.com" }, "Test Email from TMD Investments", "This is a test email from the TMD Investments application.", mailSettings.Server, mailSettings.Port);
        }
        [TestMethod]
        public void TestCoinBaseQuote()
        {
            int? longCnt = 0; int? shortCnt = 0;
            var results = services.GetQuoteAsync("DOGE-USD").Result;
            var emaToVwapDiff = helper.CalculatePoints(results, ref shortCnt, ref longCnt, .03m, true);
            Assert.IsNotNull(results);
        }
        [TestMethod]
        public void TestCoinBaseQuoteFromCryptoList()
        {
            int? longCnt = 0; int? shortCnt = 0;
            //var cryptoList = Enum.GetValues(typeof(TMDInvestments.Models.Enum.CryptoList)).Cast<TMDInvestments.Models.Enum.CryptoList>().ToList();
            var cryptoListLower = new List<JsonObject>();
            var cryptoListHigher = new List<JsonObject>();
            foreach (var crypto in TMDInvestments.Models.Enum.CryptoList.GetValues(typeof(TMDInvestments.Models.Enum.CryptoList)).Cast<TMDInvestments.Models.Enum.CryptoList>())
            {
                var results = services.GetQuoteAsync($"{crypto}-USD").Result;
                double.TryParse(results?["lastopChange"]?.ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double lastopChange);
                double.TryParse(results?["lastopChangePercent"]?.ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double lastopChangePercent);
                double.TryParse(results?["PercentHigher"]?.ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double percentHigher);
                double.TryParse(results?["PercentLower"]?.ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double percentLower);
                var emaToVwapDiff = helper.CalculatePoints(results, ref shortCnt, ref longCnt, .03m, true);
                if((lastopChange < 0) && (lastopChangePercent < 0) && (shortCnt >= 3) && (percentLower > percentHigher))
                {
                    cryptoListLower.Add(results);
                }
                else if ((lastopChange > 0) && (lastopChangePercent > 0) && (longCnt >= 3) && (percentLower < percentHigher))
                {
                    cryptoListHigher.Add(results);
                }
                Assert.IsNotNull(results);
            }
        }
        [TestMethod]
        public void TestCoinBaseLimitOrder()
        {
            string coin = "INV-USD";
            decimal availiable = 36m;
            //string baseSize = string.Empty; 
            var results = services.GetQuoteAsync(coin).Result;
            decimal baseSize = Math.Round(availiable / Convert.ToDecimal(results["lastPrice"].ToString()), 3);
            decimal amount = Math.Round(Convert.ToDecimal(results["lastPrice"].ToString()) * baseSize, 2);
            var output = services.SetOrderLimit(coin, Coinbase.AdvancedTrade.Enums.OrderSide.BUY, baseSize.ToString(), amount);
            Assert.IsNotNull(results);
        }
        [TestMethod]
        public void TestCoinBaseBuyOrder()
        {
            var product = services.GetProducts().Where(x => x.DisplayName == "00-USD").FirstOrDefault();
            if (product.CancelOnly || product.IsDisabled || product.LimitOnly || product.PostOnly || product.TradingDisabled || product.ViewOnly || product.Status != "online")
            {
                return;
            }
            var perItem = "15.74";
            var amountSize = Convert.ToDecimal(perItem.Substring(0, ((perItem.Length - perItem.IndexOf(".")) - 1)));
            var results = services.SetOrder(product.DisplayName, Coinbase.AdvancedTrade.Enums.OrderSide.BUY, amountSize);
            Assert.IsNotNull(results);
        }
        [TestMethod]
        public void TestCoinBaseSellOrder()
        {
            string symbol = "USDT-USD";
            PublicProduct? product = services.GetProducts().Where(x => x.DisplayName == symbol).FirstOrDefault();
            string precision = product.BaseIncrement.Substring((product.BaseIncrement.IndexOf(".") + 1), (product.BaseIncrement.Length - (product.BaseIncrement.IndexOf(".") + 1)));
            string[] currency = product.DisplayName.Split('-');
            var account = services.GetAccounts().Where(x => x.Currency == currency[0] && Convert.ToDecimal(x.AvailableBalance.Value) > 0m).FirstOrDefault();
            decimal minSubtract = 0m;
            decimal baseSize = 0m;
            Order output = null;
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
                case 8 or 9:
                    minSubtract = .00000001m;
                    break;
                    //case 9:
                    //    minSubtract = .000000001m;
                    //    break;
            }
            baseSize = decimal.Round(Convert.ToDecimal(account.AvailableBalance.Value), precision.Length) - minSubtract;
            output = services.SetOrder(product.DisplayName, Coinbase.AdvancedTrade.Enums.OrderSide.SELL, baseSize);
            if (output == null)
            {
                baseSize = (int)Convert.ToDecimal(account.AvailableBalance.Value);
                output = services.SetOrder(product.DisplayName, Coinbase.AdvancedTrade.Enums.OrderSide.SELL, baseSize);
            }
            if (output != null)
            {
                Helper.Log(_logger, LogLevel.Information, $"Crypto trade results for {symbol}, {output.Status}");
            }
            else
            {
                Helper.Log(_logger, LogLevel.Error, $"Crypto trade results order {symbol} failed.");
            }

            //if (output != null && (output.Status.ToLower() == "filled" || output.Status.ToLower() == "open" || output.Status.ToLower() == "pending"))
            //    SaveCryptoTracker(cryptoTracker, direction, amount, results, output, output.Status);
            Assert.IsNotNull(output);
        }
        [TestMethod]
        public void TestCoinBaseGetPortfolios()
        {
            var results = services.GetPortfolios();
            Assert.IsNotNull(results);
        }
        [TestMethod]
        public void TestCoinBaseGetProducts()
        {
            var results = services.GetProducts();
            //var nonSpots = results.Where(x => x.ProductType.ToLower() != "spot").ToList();
            var result2 = results.Where(x => x.DisplayName == "00-USD").ToList();
            var result3 = results.Where(x => x.DisplayName.Contains("BTC")).ToList();
            var disCoinList = results.Select(x => x.DisplayName.Substring(0, x.DisplayName.IndexOf("-"))).Distinct();
            foreach (var disCoin in disCoinList)
            {
                var coins = results.Where(x => x.DisplayName.StartsWith(disCoin));
            }
            Assert.IsNotNull(results);
        }
        [TestMethod]
        public void TestCoinBaseGetAccount()
        {
            var results = services.GetAccounts().Where(x => Convert.ToDecimal(x.AvailableBalance.Value) > 0).OrderByDescending(y => Convert.ToDecimal(y.AvailableBalance.Value)).ToList();
            Assert.IsNotNull(results);
        }
        [TestMethod]
        public void TestCoinBaseGetFills()
        {
            var results = services.GetOrders("EUL-USD");
            var lastSell = results.Where(x => x.Side == "BUY").OrderByDescending(y => Convert.ToDateTime(y.SequenceTimestamp)).FirstOrDefault();
            Assert.IsNotNull(results);
        }
        [TestMethod]
        public void TestCoinBaseGetorder()
        {
            var results = services.GetOrder(Guid.Parse("12144b33-9c7d-4981-8b99-8948ecb16ee1"));
            Assert.IsNotNull(results);
        }
        [TestMethod]
        public void TestCoinBaseOwnedProducts()
        {
            var account = services.GetAccounts().Where(x => x.Currency == "ETH").ToList();
            Assert.IsNotNull(account);
        }
        [TestMethod]
        public void TestCoinBaseProducts()
        {
            var product = services.GetProducts().Where(x => x.DisplayName == "ETH-USD").ToList();
            Assert.IsNotNull(product);
        }
        [TestMethod]
        public void TestCoinBaseMovers()
        {
            var results = services.GetArbitrage();
            Assert.IsNotNull(results);
        }
        [TestMethod]
        public void TestSchwabGetMovers()
        {
            var results = SchwabService.GetMoversAsync().Result;
            Assert.IsNotNull(results);
        }
        public void TestSchwabOrder()
        {
            //string order = "";
            string order = "{" +
                "\"orderType\" : \"MARKET\"," +
                "\"session\" : \"NORMAL\"," +
                //"\"price\": \"72\"," +
                "\"duration\" : \"DAY\"," +
                "\"orderStrategyType\" : \"SINGLE\"," +
                "\"orderLegCollection\" : [{" +
                "\"instruction\" : \"BUY\"," +
                    "\"quantity\" : 2," +
                    "\"instrument\" :{" +
                    "\"symbol\" : \"UAN\"," +
                        "\"assetType\" : \"EQUITY\"" +
                    "}" +
                "}" +
                "]" +
            "}";
            var results = SchwabService.SetOrder(order);
            Assert.IsNotNull(results);
        }
        [TestMethod]
        public void TestSchwabGetOrder()
        {
            string enterTime = string.Empty;
            DateTime dateTime = DateTime.MinValue;
            string symbol = string.Empty;
            float? quantity = 0;
            string? instruction = string.Empty;
            float? price = 0;
            string? status = string.Empty;
            Orders orderFirst = null;
            var results = SchwabService.GetOrders(DateTime.Now.AddDays(-21).ToString("O"), DateTime.Now.ToString("O"));
            IEnumerable<Orders>? orders = System.Text.Json.JsonSerializer.Deserialize<IEnumerable<Orders>>(results);
            //Orders order = orders.Where(x => x?.orderLegCollection[0]?.instrument?.symbol == "CWAN").FirstOrDefault();
            foreach (var order in orders)
            {
                foreach (var orderColl in order.orderLegCollection)
                {
                    if (orderColl.instrument.symbol == "CWAN")
                    {
                        orderFirst = order;
                    }
                }
            }
            foreach (var item in orders)
            {
                if (item?.status == "REJECTED")
                    continue;
                try
                {
                    enterTime = item.enteredTime;
                    dateTime = DateTime.ParseExact(enterTime, "yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture);
                    symbol = item?.orderLegCollection[0]?.instrument?.symbol;
                    quantity = item?.orderLegCollection[0]?.quantity;
                    instruction = item?.orderLegCollection[0]?.instruction;
                    price = item?.orderActivityCollection[0]?.executionLegs[0]?.price;
                    status = item?.status;
                }
                catch (Exception EX)
                {
                    continue;
                }

                StockTracker2? stock = ctx?.StockTracker2?.Where(x => x.Symbol == symbol &&
                (x.DatePurchased >= DateTime.Parse(dateTime.ToShortDateString()) && x.DatePurchased <= DateTime.Parse(dateTime.ToShortDateString()).AddDays(1) ||
                x.DateUpdated >= DateTime.Parse(dateTime.ToShortDateString()) && x.DateUpdated <= DateTime.Parse(dateTime.ToShortDateString()).AddDays(1)) &&
                x.TradePlatform == (int)Platform.CharlesSchwab).FirstOrDefault();
                if (stock != null)
                {
                    try
                    {
                        switch (instruction)
                        {
                            case "BUY":
                            case "SELL_SHORT":
                                stock.PricePurchased = (decimal)price;
                                stock.DatePurchased = dateTime;
                                stock.Quantity = (int)quantity;
                                stock.Status = status;
                                break;
                            case "SELL":
                            case "BUY_TO_COVER":
                                //quote = SchwabService.GetQuote(symbol, true, false);
                                stock.PriceSold = (decimal)price;
                                stock.DateSold = dateTime;
                                stock.DateUpdated = dateTime;
                                stock.Quantity = (int)quantity;
                                stock.Status = status;
                                break;
                        }
                        ctx.Entry(stock).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                        ctx.SaveChanges();
                        ctx.Entry(stock).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                    }
                    catch (Exception ex)
                    {

                    }
                }

            }
            Assert.IsNotNull(results);
        }
        [TestMethod]
        public void GetScHwabAccountInfo()
        {
            ///string order = "";
            SchwabAccountInfo results = new SchwabAccountInfo();
            SchwabService.GetAccountInfo(ref results);
            Console.WriteLine(results.ToString());
            Assert.IsNotNull(results);
        }
        [TestMethod]
        public void TestCryptoTrackingMethod()
        {
            Coinbase.AdvancedTrade.Models.Order output = null;
            var Accounts = services.GetAccounts()/*.Where(x => Convert.ToDecimal(x.AvailableBalance.Value) > 0)*/.ToList();
            //var mrpopo = Accounts.Where(x => x.Currency == "BTC").FirstOrDefault();
            var PublicProducts = services.GetProducts().OrderBy(x => x.DisplayName).ToList();
            //var Cryptos = ctx.CryptoTracker.Where(x => x.PriceSold == null && x.Status != "OPEN").ToList();
            var Cryptos = ctx.CryptoTracker2.Where(x => x.PriceSold == null).ToList();
            //check if there is a wallet with cash not in tracker
            foreach (var item in Accounts)
            {
                var publicProduct = PublicProducts.Where(x => x.DisplayName == item.Currency + "-USD").FirstOrDefault();
                var coin = Cryptos.Where(x => x.Coin == item.Currency + "-USD").FirstOrDefault();
                if (coin == null)
                {
                    var valueofNonTrackedAsset = Math.Round(Convert.ToDecimal(item.AvailableBalance.Value) * Convert.ToDecimal(publicProduct?.Price), 2);
                    if (valueofNonTrackedAsset > 1)
                    {
                        //try to sell non track items over a dollar
                        //var orders = services.GetOrders(item.Currency + "-USD").Where(x => x.Side == "BUY").OrderByDescending(y => DateTime.Parse(y.SequenceTimestamp)).ToList();
                        //var results = services.GetQuote(item.Currency + "-USD");
                        int decIndex = item.AvailableBalance.Value.IndexOf(".");
                        decimal amountSize = Convert.ToDecimal(item.AvailableBalance.Value.Substring(0, (decIndex + 4)));
                        output = services.SetOrder(publicProduct.DisplayName, Coinbase.AdvancedTrade.Enums.OrderSide.SELL, amountSize);
                    }
                }
            }
            //checks if there is a tracker not in a wallet with cash
            foreach (var item in Cryptos)
            {
                var currency = item.Coin.Split('-');
                var publicProduct = PublicProducts.Where(x => x.DisplayName == item.Coin).FirstOrDefault();
                var account = Accounts.Where(x => x.Currency == currency[0].ToString().ToUpper().Trim() && (Convert.ToDecimal(x.AvailableBalance.Value) * Convert.ToDecimal(publicProduct?.Price)) > 1).FirstOrDefault();
                if (account == null)
                {
                    //delete the orphan tracked item.
                    //var results = services.GetQuote(item.Coin);
                    var sp = item;
                    sp.PriceSold = 0;
                    sp.DateSold = DateTime.Now;
                    sp.DateUpdated = DateTime.Now;
                    ctx.CryptoTracker2.Attach(sp);
                    ctx.Entry(sp).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                    ctx.SaveChanges();
                }
                //else{
                //    var results = services.GetQuote(item.Coin);
                //}
                //var orders = services.GetOrders(item.Coin).OrderByDescending(y => DateTime.Parse(y.SequenceTimestamp)).ToList();
            }
            Assert.IsTrue(true);
        }
        [TestMethod]
        public void TestSchwabCheckOrderStatus()
        {
            int authRetry = 0;
        accountAuthRetry:
            //Get Account Info
            SchwabAccountInfo account = new SchwabAccountInfo();
            SchwabService.GetAccountInfo(ref account);
            _logger.LogInformation($"Get account info for checking stock status and updating");

            //JsonElement securitiesAccount = new JsonElement();
            //JsonElement initialBalances = new JsonElement();
            //try
            //{
            //    securitiesAccount = account.GetProperty("securitiesAccount");
            //    initialBalances = securitiesAccount.GetProperty("initialBalances");
            //}
            //catch (Exception ex)
            //{
            //    if (authRetry < 3)
            //    {
            //        _helper.Log(_logger, LogLevel.Error, "Authenication from schwab failed. Retrying");
            //        authRetry++;
            //        goto accountAuthRetry;
            //    }
            //    else
            //    {
            //        _helper.Log(_logger, LogLevel.Error, $"Authenication from schwab failed {authRetry} Retries. Exiting");
            //        return;
            //    }

            //}
            ////List<StockTracker> stockTracker = ctx.StockTracker.Where(x => x.PriceSold == null && x.DateSold == null && (x.Status != "Cancelled" && x.Status != "Filled")).ToList();
            //List<StockTracker> stockTracker = ctx.StockTracker.Where(x => x.PriceSold != null && x.DateSold != null && (x.Status != "Cancelled" && x.Status != "Filled")).ToList();
            //List<string> symbolList = new List<string>();
            ////if (stockTracker.Count > 0)
            //{
            //    foreach (var item in securitiesAccount.GetProperty("positions").EnumerateArray())
            //    {
            //        symbolList.Add(item.GetProperty("instrument").GetProperty("symbol").ToString());
            //    }
            //    foreach (var item in stockTracker)
            //    {
            //        var sp = item;
            //        _logger.LogInformation($"Checking {sp.Symbol} - ({sp.Name}) for status Update");
            //        //if (symbolList.Contains(sp.Symbol))
            //        //{
            //        //    _logger.LogInformation($"Setting {sp.Symbol} - ({sp.Name}) to FIlled");
            //            sp.Status = "Filled";
            //        //}
            //        //else
            //        //{
            //        //    _logger.LogInformation($"Setting {sp.Symbol} - ({sp.Name}) to Cancelled");
            //        //    sp.Status = "Cancelled";
            //        //}
            //        sp.DateUpdated = DateTime.Now;
            //        ctx.StockTracker.Attach(sp);
            //        ctx.Entry(sp).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
            //        ctx.SaveChanges();
            //    }
            //}

        }
        [TestMethod]
        public void GetPriceHistoryCoinTest()
        {
            string[] productIds = new string[5] { "BTC-USD", "ETH-USD", "LTC-USD", "BCH-USD", "DASH-USD" };
            foreach (var productId in productIds)
            {
                var quote = services.GetQuoteAsync(productId).Result;
                var currentPrice = Convert.ToDecimal(quote["lastPrice"].ToString());
                var candle90 = services.GetPriceHistoryAsync(productId, DateTime.Now.AddDays(-90), DateTime.Now, Granularity.ONE_DAY).Result.OrderByDescending(x => x.StartDate).ToList();
                var candle60 = candle90.Take(60).ToList();
                var candle30 = candle90.Take(30).ToList();
                var candle26 = candle90.Take(26).ToList();
                var candle12 = candle90.Take(12).ToList();
                var candle9 = candle90.Take(9).ToList();
                var lowestPrice90 = candle90.Min(x => Convert.ToDecimal(x.Close));
                var highestPrice90 = candle90.Max(x => Convert.ToDecimal(x.Close));
                var lowestPrice60 = candle60.Min(x => Convert.ToDecimal(x.Close));
                var highestPrice60 = candle60.Max(x => Convert.ToDecimal(x.Close));
                var lowestPrice30 = candle30.Min(x => Convert.ToDecimal(x.Close));
                var highestPrice30 = candle30.Max(x => Convert.ToDecimal(x.Close));
                var lowestPrice26 = candle26.Min(x => Convert.ToDecimal(x.Close));
                var highestPrice26 = candle26.Max(x => Convert.ToDecimal(x.Close));
                decimal percentDiffLowest90 = Math.Abs(((currentPrice - lowestPrice90) / ((currentPrice + lowestPrice90) / 2)) * 100);
                decimal percentDiffHighest90 = Math.Abs(((currentPrice - highestPrice90) / ((currentPrice + highestPrice90) / 2)) * 100);
                decimal percentDiffLowest60 = Math.Abs(((currentPrice - lowestPrice60) / ((currentPrice + lowestPrice60) / 2)) * 100);
                decimal percentDiffHighest60 = Math.Abs(((currentPrice - highestPrice60) / ((currentPrice + highestPrice60) / 2)) * 100);
                decimal percentDiffLowest30 = Math.Abs(((currentPrice - lowestPrice30) / ((currentPrice + lowestPrice30) / 2)) * 100);
                decimal percentDiffHighest30 = Math.Abs(((currentPrice - highestPrice30) / ((currentPrice + highestPrice30) / 2)) * 100);
                decimal percentDiffLowest26 = Math.Abs(((currentPrice - lowestPrice26) / ((currentPrice + lowestPrice26) / 2)) * 100);
                decimal percentDiffHighest26 = Math.Abs(((currentPrice - highestPrice26) / ((currentPrice + highestPrice26) / 2)) * 100);
                var candle30LastFirst = candle30.OrderBy(x => x.StartDate).ToList();
                var candle26LastFirst = candle26.OrderBy(x => x.StartDate).ToList();
                var candle12LastFirst = candle12.OrderBy(x => x.StartDate).ToList();
                var candle9LastFirst = candle9.OrderBy(x => x.StartDate).ToList();

                decimal WMA30 = 0m;
                decimal WMA26 = 0m;
                decimal WMA12 = 0m;
                decimal WMA9 = 0m;

                decimal wmaPre30 = 0m;
                decimal wmaPre26 = 0m;
                decimal wmaPre12 = 0m;
                decimal wmaPre9 = 0m;
                ;
                decimal N30 = 0m;
                decimal N26 = 0m;
                decimal N12 = 0m;
                decimal N9 = 0m;

                decimal P30 = 0m;
                decimal P26 = 0m;
                decimal P12 = 0m;
                decimal P9 = 0m;

                decimal SMA30 = 0m;
                decimal SMA26 = 0m;
                decimal SMA12 = 0m;
                decimal SMA9 = 0m;

                decimal smaPre30 = 0m;
                decimal smaPre26 = 0m;
                decimal smaPre12 = 0m;
                decimal smaPre9 = 0m;

                decimal FC30 = candle30LastFirst.Count;
                decimal FC26 = candle26LastFirst.Count;
                decimal FC12 = candle12LastFirst.Count;
                decimal FC9 = candle9LastFirst.Count;

                decimal VWAP30 = 0;

                decimal EMA30 = 0m;
                decimal EMA26 = 0m;
                decimal EMA12 = 0m;
                decimal EMA9 = 0m;

                decimal smoothingFactor30 = (2m / ((decimal)candle30LastFirst.Count + 1m));
                decimal smoothingFactor26 = (2m / ((decimal)candle26LastFirst.Count + 1m));
                decimal smoothingFactor12 = (2m / ((decimal)candle12LastFirst.Count + 1m));
                decimal smoothingFactor9 = (2m / ((decimal)candle9LastFirst.Count + 1m));

                decimal MACD9 = 0m;
                decimal MACD12 = 0m;

                decimal RSI12 = 0m;
                decimal close = 0m;
                decimal avgUp = 0m;
                decimal avgDown = 0m;
                decimal avgUpTotal = 0m;
                decimal avgDownTotal = 0m;

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
                foreach (var item in candle12LastFirst)
                {
                    if (close > 0 && close > Convert.ToDecimal(item.Close))
                    {
                        avgDown += Convert.ToDecimal(item.Close);
                        avgDownTotal++;
                    }
                    else
                    {
                        avgUp += Convert.ToDecimal(item.Close);
                        avgUpTotal++;
                    }
                    close = Convert.ToDecimal(item.Close);
                }
                RSI12 = 100 - 100 / (1 + (avgUp / avgDown));

                WMA30 = (wmaPre30 / N30);
                WMA26 = (wmaPre26 / N26);
                WMA12 = (wmaPre12 / N12);
                WMA9 = (wmaPre9 / N9);
                SMA30 = (smaPre30 / FC30);
                SMA26 = (smaPre26 / FC26);
                SMA12 = (smaPre12 / FC12);
                SMA9 = (smaPre9 / FC9);
                VWAP30 = (((Convert.ToDecimal(candle30[1].Low) + Convert.ToDecimal(candle30[1].High) + Convert.ToDecimal(candle30[1].Close)) / 3) * Convert.ToDecimal(candle30[1].Volume)) / (Convert.ToDecimal(candle30[1].Volume));
                //EMA = (Today's Value * Multiplier) + Yesterday's EMA *(1 - Multiplier)
                //You can then use the SMA from the previous day as the first EMA for yesterday.
                EMA30 = (Convert.ToDecimal(candle30[0].Close) * smoothingFactor30) + (SMA30) * (1 - smoothingFactor30);
                EMA26 = (Convert.ToDecimal(candle26[0].Close) * smoothingFactor26) + (SMA26) * (1 - smoothingFactor26);
                EMA12 = (Convert.ToDecimal(candle12[0].Close) * smoothingFactor12) + (SMA12) * (1 - smoothingFactor12);
                EMA9 = (Convert.ToDecimal(candle9[0].Close) * smoothingFactor9) + (SMA9) * (1 - smoothingFactor9);
                //MACD = 12 - Period EMA − 26 - Period EMA
                MACD9 = EMA9 - EMA12;
                MACD12 = EMA12 - EMA26;
                Console.WriteLine($"MACD9 - {MACD9} MACD12 - {MACD12}");
            }
            Assert.IsTrue(true);
        }
        [TestMethod]
        public void GetPriceHistorySchwabTest2()
        {//Add $VIX and $SPX to the equation higher vix history and lower s&p history for short point, vise versa for long.
            string[] symbols = { "$VIX", "$SPX", "IBM", "NFLX", "AMZN", "GOOGL", "META", "MSFT" };
            foreach (string symbol in symbols)
            {
                var quote = SchwabService.GetQuoteAsync(symbol, true, true).Result;
                //var currentPrice = Convert.ToDecimal(quote.GetProperty(symbol).GetProperty("quote").GetProperty("lastPrice").ToString());
                var currentPrice = Convert.ToDecimal(quote?[symbol]?["quote"]?["lastPrice"]?.ToString());
                var results = SchwabService.GetPriceHistory(symbol, "month", "1", "daily", 1, DateTime.Now.AddDays(-90), DateTime.Now).Result;//.ToList();
                List<TMDInvestments.Models.Candles> candle90 = JsonSerializer.Deserialize<List<TMDInvestments.Models.Candles>>(JsonSerializer.Serialize(results.GetProperty("candles")));
                var candle60 = candle90.Take(60).ToList();
                var candle30 = candle90.Take(30).ToList();
                var candle26 = candle90.Take(26).ToList();
                var candle12 = candle90.Take(12).ToList();
                var candle9 = candle90.Take(9).ToList();
                var lowestPrice90 = candle90.Min(x => Convert.ToDecimal(x.close));
                var highestPrice90 = candle90.Max(x => Convert.ToDecimal(x.close));
                var lowestPrice60 = candle60.Min(x => Convert.ToDecimal(x.close));
                var highestPrice60 = candle60.Max(x => Convert.ToDecimal(x.close));
                var lowestPrice30 = candle30.Min(x => Convert.ToDecimal(x.close));
                var highestPrice30 = candle30.Max(x => Convert.ToDecimal(x.close));
                var lowestPrice26 = candle26.Min(x => Convert.ToDecimal(x.close));
                var highestPrice26 = candle26.Max(x => Convert.ToDecimal(x.close));
                decimal percentDiffLowest90 = Math.Abs(((currentPrice - lowestPrice90) / ((currentPrice + lowestPrice90) / 2)) * 100);
                decimal percentDiffHighest90 = Math.Abs(((currentPrice - highestPrice90) / ((currentPrice + highestPrice90) / 2)) * 100);
                decimal percentDiffLowest60 = Math.Abs(((currentPrice - lowestPrice60) / ((currentPrice + lowestPrice60) / 2)) * 100);
                decimal percentDiffHighest60 = Math.Abs(((currentPrice - highestPrice60) / ((currentPrice + highestPrice60) / 2)) * 100);
                decimal percentDiffLowest30 = Math.Abs(((currentPrice - lowestPrice30) / ((currentPrice + lowestPrice30) / 2)) * 100);
                decimal percentDiffHighest30 = Math.Abs(((currentPrice - highestPrice30) / ((currentPrice + highestPrice30) / 2)) * 100);
                decimal percentDiffLowest26 = Math.Abs(((currentPrice - lowestPrice26) / ((currentPrice + lowestPrice26) / 2)) * 100);
                decimal percentDiffHighest26 = Math.Abs(((currentPrice - highestPrice26) / ((currentPrice + highestPrice26) / 2)) * 100);
                var candle30LastFirst = candle30.OrderBy(x => x.datetime).ToList();
                var candle26LastFirst = candle26.OrderBy(x => x.datetime).ToList();
                var candle12LastFirst = candle12.OrderBy(x => x.datetime).ToList();
                var candle9LastFirst = candle9.OrderBy(x => x.datetime).ToList();

                decimal WMA30 = 0m;
                decimal WMA26 = 0m;
                decimal WMA12 = 0m;
                decimal WMA9 = 0m;

                decimal wmaPre30 = 0m;
                decimal wmaPre26 = 0m;
                decimal wmaPre12 = 0m;
                decimal wmaPre9 = 0m;
                ;
                decimal N30 = 0m;
                decimal N26 = 0m;
                decimal N12 = 0m;
                decimal N9 = 0m;

                decimal P30 = 0m;
                decimal P26 = 0m;
                decimal P12 = 0m;
                decimal P9 = 0m;

                decimal SMA30 = 0m;
                decimal SMA26 = 0m;
                decimal SMA12 = 0m;
                decimal SMA9 = 0m;

                decimal smaPre30 = 0m;
                decimal smaPre26 = 0m;
                decimal smaPre12 = 0m;
                decimal smaPre9 = 0m;

                decimal FC30 = candle30LastFirst.Count;
                decimal FC26 = candle26LastFirst.Count;
                decimal FC12 = candle12LastFirst.Count;
                decimal FC9 = candle9LastFirst.Count;

                decimal VWAP30 = 0;

                decimal EMA30 = 0m;
                decimal EMA26 = 0m;
                decimal EMA12 = 0m;
                decimal EMA9 = 0m;

                decimal smoothingFactor30 = (2m / ((decimal)candle30LastFirst.Count + 1m));
                decimal smoothingFactor26 = (2m / ((decimal)candle26LastFirst.Count + 1m));
                decimal smoothingFactor12 = (2m / ((decimal)candle12LastFirst.Count + 1m));
                decimal smoothingFactor9 = (2m / ((decimal)candle9LastFirst.Count + 1m));

                decimal MACD9 = 0m;
                decimal MACD12 = 0m;

                decimal RSI12 = 0m;
                decimal close = 0m;
                decimal avgUp = 0m;
                decimal avgDown = 0m;
                decimal avgUpTotal = 0m;
                decimal avgDownTotal = 0m;

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
                foreach (var item in candle12LastFirst)
                {
                    if (close > 0 && close > Convert.ToDecimal(item.close))
                    {
                        avgDown += Convert.ToDecimal(item.close);
                        avgDownTotal++;
                    }
                    else
                    {
                        avgUp += Convert.ToDecimal(item.close);
                        avgUpTotal++;
                    }
                    close = Convert.ToDecimal(item.close);
                }
                RSI12 = 100 - 100 / (1 + (avgUp / avgDown));

                WMA30 = (wmaPre30 / N30);
                WMA26 = (wmaPre26 / N26);
                WMA12 = (wmaPre12 / N12);
                WMA9 = (wmaPre9 / N9);
                SMA30 = (smaPre30 / FC30);
                SMA26 = (smaPre26 / FC26);
                SMA12 = (smaPre12 / FC12);
                SMA9 = (smaPre9 / FC9);
                if (Convert.ToDecimal(candle30[1].volume) > 0)
                    VWAP30 = (((Convert.ToDecimal(candle30[1].low) + Convert.ToDecimal(candle30[1].high) + Convert.ToDecimal(candle30[1].close)) / 3) * Convert.ToDecimal(candle30[1].volume)) / (Convert.ToDecimal(candle30[1].volume));
                //EMA = (Today's Value * Multiplier) + Yesterday's EMA *(1 - Multiplier)
                //You can then use the SMA from the previous day as the first EMA for yesterday.
                EMA30 = (Convert.ToDecimal(candle30[0].close) * smoothingFactor30) + (SMA30) * (1 - smoothingFactor30);
                EMA26 = (Convert.ToDecimal(candle26[0].close) * smoothingFactor26) + (SMA26) * (1 - smoothingFactor26);
                EMA12 = (Convert.ToDecimal(candle12[0].close) * smoothingFactor12) + (SMA12) * (1 - smoothingFactor12);
                EMA9 = (Convert.ToDecimal(candle9[0].close) * smoothingFactor9) + (SMA9) * (1 - smoothingFactor9);
                //MACD = 12 - Period EMA − 26 - Period EMA
                MACD9 = EMA9 - EMA12;
                MACD12 = EMA12 - EMA26;
                Console.WriteLine($"MACD9 - {MACD9} MACD12 - {MACD12}");
            }
            Assert.IsTrue(true);
        }
        [TestMethod]
        public void GetMACD()
        {
            var results = SchwabService.GetPriceHistory("AMZN", "day", "1", "minute", 15, DateTime.Now.AddDays(-1), DateTime.Now).Result;//.ToList();
            var ser = JsonSerializer.Serialize(results.GetProperty("candles"));
            List<TMDInvestment.Models.CandleItems> candle90 = JsonSerializer.Deserialize<List<TMDInvestment.Models.CandleItems>>(ser);
            //var closes = new List<double> { 10, 12, 11, 13, 15, 14, 16, 18, 17, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30 };
            //var closes = candle90.OrderBy(x => x.datetime).Take(52).Select(x => Convert.ToDouble(x.close)).ToList();
            var closes = candle90.OrderBy(x => x.datetime)/*.Take(52)*/.Select(y => new MACDClosesDateTime { Price = y.close, Datetime = DateTimeOffset.FromUnixTimeMilliseconds(y.datetime).DateTime }).ToList();
            //var calculator = new MacdCalculator();
            //var (macd, signal, histogram) = calculator.CalculateMacd(closes);
            var macdCalculator = new MacdCalculator();
            List<MacdResult> macdCalculatorResults = macdCalculator.CalculateMacd(closes);
        }
        [TestMethod]
        public void GetSchwabQuote()
        {
            var quote = SchwabService.GetQuoteAsync("SMCI", true, false).Result;
            Assert.IsNotNull(quote);
        }
        [TestMethod]
        public void GetTaApiIndicators()
        {
            TaAPIService taApiService = new TaAPIService(_logger, _taApiSettings, httpClientFactory);
            var results = taApiService.GetTechnicalAnalysis("AAPL", "5m", "stocks", "stocks");
            Assert.IsTrue(true);
        }
        //[TestMethod]
        //public void GetSchwabGenericQuote()
        //{
        //    IndicesCalculatorV2<SchwabSettings,JsonElement,CandleItems,SchwabServices> testService = null;
        //    testService = new IndicesCalculatorV2<SchwabSettings, JsonElement, CandleItems, SchwabServices>(_logger, _settings2, httpClientFactory);
        //    var quote = SchwabService.GetQuote("ACON", true, true);
        //    var results = testService.CalculateV2("ACON", quote);
        //    Assert.IsNotNull(quote);
        //}
        [TestMethod]
        public void GetSchwabQuotes()
        {
            string[] quotes = { "CAR", "glue", "fcel", "domo", "ee", "dgly", "dfh" };
            JsonObject[] quote = SchwabService.GetQuotes(string.Join(",", quotes), true, false);
            quote = quote.Where(item => item != null).ToArray();
            foreach (var item in quote)
            {
                //var quoteData = ((JsonElement)quote).GetProperty(item.ToUpper());
                var quoteData = item["RSI"];
            }
            Assert.IsNotNull(quote);
        }
        [TestMethod]
        public void GetSchwabQuotesFromList()
        {
            string[] quotes = ctx.StockList.Select(x => x.Symbol).ToArray();
            JsonObject[] quote = SchwabService.GetQuotes(string.Join(",", quotes), true, false);
            quote = quote.Where(item => item != null).ToArray();
            foreach (var item in quote)
            {
                //var quoteData = ((JsonElement)quote).GetProperty(item.ToUpper());
                //var quoteData = item["RSI"];
            }
            Assert.IsNotNull(quote);
        }
        [TestMethod]
        public void GetSchwabQuoteHistory()
        {
            var results = SchwabService.GetPriceHistory("AAPL", "month", "1", "daily", 1, DateTime.Now.AddDays(-90), DateTime.Now);
            Assert.IsNotNull(results);
        }
        [TestMethod]
        public void GetETradeKey()
        {
            //var results = ETradeService.GetToken();
            dynamic error = null;
            //var results = ETradeService.GetRequestToken(ref error);
            var results = ETradeService.GetRequestToken();
            //Console.WriteLine(results.GetProperty("errors").ToString(), null);
            Assert.IsTrue(true);
        }
        [TestMethod]
        public void RefreshSchwabToken()
        {
            bool success = false;
            SchwabService.FetchToken("", ref success, true);
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void GetETradeQuote()
        {
            //var results = ETradeService.Quote("VIX");
            //var results2 = ETradeService.Quote("SPX");
            var results3 = ETradeService.Quote("RGC");
            Assert.IsTrue(true);
        }
        [TestMethod]
        public void GetETradeAccount()
        {
            var results = ETradeService.Account();
            Assert.IsTrue(true);
        }
        [TestMethod]
        public void GetETradeAccountModel()
        {
            var results = ETradeService.AccountModel();
            Assert.IsTrue(true);
        }
        [TestMethod]
        public void GetETradelookup()
        {
            var results = ETradeService.Search("Amazon");
            Assert.IsTrue(true);
        }
        [TestMethod]
        public void GetETradePortfolio()
        {
            XElement results = ETradeService.Portfolio();
            EtradeAccountInfo results2 = ETradeService.PortfolioModel();
            Assert.IsTrue(true);
        }
        [TestMethod]
        public void GetOrphanETrades()
        {
            XElement results = ETradeService.Portfolio();
            //((XElement)accounts).Elements("Accounts").ToList()[0].Element("Account").Element("accountIdKey").Value;
            var positions = results?.Element("AccountPortfolio")?.Elements("Position").ToList();
            //var stockTracker = ctx?.StockTracker?.Where(x => x.PriceSold == null && x.DateSold == null && x.TradePlatform == (int)Platform.ETrade && x.Status != "Cancelled").ToList();
            //foreach(var stock in stockTracker)
            //{
            //    var products = positions?.Where(x => x?.Element("Product")?.Element("symbol")?.Value == stock.Symbol).FirstOrDefault();
            //}
            foreach (var position in positions)
            {
                var symbol = position?.Element("Product")?.Element("symbol")?.Value;
                var pricePaid = Convert.ToDecimal(position?.Element("pricePaid")?.Value);
                var lastPrice = Convert.ToDecimal(position?.Element("Quick")?.Element("lastTrade")?.Value);
                var qty = Convert.ToInt32(position?.Element("quantity").Value);
                if (pricePaid > 0 && lastPrice > 1)
                {
                    var stockTracker = ctx?.StockTracker2?.Where(x => x.PriceSold == null && x.DateSold == null && x.TradePlatform == (int)Platform.ETrade && x.Status != "Cancelled" && x.Symbol == symbol.ToUpper()).FirstOrDefault();
                    if (stockTracker == null)
                    {
                        var quote = ETradeService.Quote(symbol);
                        stockTracker = new StockTracker2();
                        stockTracker.Symbol = quote["Symbol"].ToString();
                        stockTracker.Name = quote["Name"].ToString();
                        stockTracker.TradeDirection = (qty > 0) ? (int)TradeDirection.BUY : (int)TradeDirection.SELL_SHORT;//(int)direction;
                        stockTracker.TradePlatform = (int)Platform.ETrade;
                        stockTracker.PricePurchased = Convert.ToDecimal(quote["lastPrice"].ToString());
                        stockTracker.Quantity = qty;
                        stockTracker.PurchasedRsi = Convert.ToDecimal(quote["RSI"].ToString());
                        //stockTracker.PurchasedSma200 = Convert.ToDecimal(quote["SMA30"].ToString());
                        //stockTracker.PurchasedEma200 = Convert.ToDecimal(quote["EMA30"].ToString());
                        stockTracker.PurchasedVwap = Convert.ToDecimal(quote["VWAP"].ToString());
                        stockTracker.PurchasedEma9 = Convert.ToDecimal(quote["EMA9"].ToString());
                        stockTracker.PurchasedEma12 = Convert.ToDecimal(quote["EMA12"].ToString());
                        stockTracker.PurchasedEma26 = Convert.ToDecimal(quote["EMA26"].ToString());
                        stockTracker.PurchasedMacd = Convert.ToDecimal(quote["MACD12"].ToString());
                        stockTracker.DatePurchased = DateTime.Now;
                        stockTracker.DateCreated = DateTime.Now;
                        stockTracker.LastUpdatedBy = 1;
                        stockTracker.Status = "Filled";
                        ctx.StockTracker2.Add(stockTracker);
                        ctx.Entry(stockTracker).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                        ctx.SaveChanges();
                        ctx.Entry(stockTracker).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                    }
                }
            }
            Assert.IsTrue(true);
        }
        [TestMethod]
        public void PlaceETradeOrder()
        {
            var order = SchwabService.GetQuoteAsync("TRCY", true, false).Result;
            var results = ETradeService.SetOrder(order["Symbol"].ToString(), "MARKET", string.Empty, 1, TradeDirection.BUY);
            var options = new JsonSerializerOptions
            {
                ReferenceHandler = ReferenceHandler.Preserve
            };
            string output = System.Text.Json.JsonSerializer.Serialize(results, options);
            if (output.Contains("errors"))
            {
                //Log(_logger, LogLevel.Error, $"Stock trade failed {order["Symbol"].ToString()} {output}");
                //Try limit order
                string limitPrice = order["lastPrice"].ToString();
                results = ETradeService.SetOrder(order["Symbol"].ToString(), "LIMIT", limitPrice, 1, TradeDirection.BUY);
                if (output.Contains("errors"))
                {
                    //Log(_logger, LogLevel.Error, $"Stock trade failed {order["Symbol"].ToString()} {output}");
                    //return false;
                }
            }
            //Log(_logger, LogLevel.Information, $"Stock trade results {order["Symbol"].ToString()} Success");
            Assert.IsTrue(true);
        }
        [TestMethod]
        public void GetStackCommerceFeed()
        {
            Bartergora2._0.Models.ProductsToSubCategories productsToSubCategory = null;
            Bartergora2._0.Models.Advertiser advertiser = null;
            Bartergora2._0.Models.Product products = null;
            Bartergora2._0.Models.ProductImage productImage = null;
            decimal price = 0m;
            string imageUrl = string.Empty;

            IEnumerable<CategoriesWithDescription> categories = null;

            SyndicationFeed feed = stackCommerceService.GetFileFeed();

            categories = ctx2.Database.SqlQueryRaw<CategoriesWithDescription>($"EXEC [tmd].[GetCategoriesWithDescription]");

            //List<ModelInput> categoryResults = new List<ModelInput>();
            //foreach (var item in categories)
            //{
            //    categoryResults.Add(new ModelInput { SubCategoryId = item.SubCategoryId, SubCategoryName = item.SubCategoryName/*, Description = item.SubCategoryDefinition*/ });
            //}
            List<ProductInput> categoryResults = new List<ProductInput>();
            foreach (var item in categories)
            {
                categoryResults.Add(new ProductInput { /*Category = item.CategoryName, Description = item.CategoryDescription,*/ SubCategoryId = item.SubCategoryId, SubCategoryName = item.SubCategoryName/*, SubCategoryDefinition = item.SubCategoryDefinition */ });
            }
            var predEngine = ml.InitTextCategoryPredict4(categoryResults);

            ml.predictCategory(predEngine, categoryResults, ref feed);

            foreach (SyndicationItem item in feed.Items)
            {
                Console.WriteLine($"Title: {item.Title.Text}");
                Console.WriteLine($"Summary: {item.Summary?.Text}");

                //int priceStartIndex = item.Title?.Text.IndexOf("$") ?? -1;
                //int titleLength = item.Title.Text.Length;
                //try
                //{
                    //price = Convert.ToDecimal(item.Title.Text.Substring(priceStartIndex + 1, titleLength - (priceStartIndex + 1)));
                    //string digitsOnly = new string(item.Title.Text.Where(char.IsDigit).ToArray());
                    bool abstractDigit = false; 
                    string digitsOnly = ""; 
                    foreach (char c in item.Title.Text)
                    {
                        if(abstractDigit == true && char.IsDigit(c))
                        {
                            digitsOnly += c;
                        }
                        else
                        {
                            abstractDigit = false;
                        }
                        if (c == '$')
                        {
                            abstractDigit = true;
                            digitsOnly = "";
                        }
                    }
                    price = Convert.ToDecimal(digitsOnly);
                //}
                //catch (Exception ex)
                //{
                //    continue;
                //}

                var doc = new HtmlDocument();
                doc.LoadHtml(item.Summary?.Text); // or item.Content
                                                  // Find the first <img> tag
                var imgNode = doc.DocumentNode.SelectSingleNode("//img");
                if (imgNode != null)
                {
                    imageUrl = imgNode.GetAttributeValue("src", "");
                    // Process the imageUrl (e.g., download, display)
                }
                Console.WriteLine($"Link: {item.Links[0].Uri}");
                Console.WriteLine("----------------------------");

                if(!Uri.TryCreate(imageUrl, UriKind.Absolute, out Uri uriResult)
                    || !(uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
                    continue;

                advertiser = ctx2.Advertisers.Where(x => x.AdvertiserName.ToLower().Trim() == "Stack Commerce" && x.AdvertiserKey == 999).FirstOrDefault();
                if (advertiser == null)
                {
                    advertiser = new Advertiser() { AdvertiserName = "Stack Commerce", AdvertiserKey = 999, DateCreated = DateTime.Now, Deleted = false };
                    ctx2.Advertisers.Add(advertiser);
                    ctx2.Entry(advertiser).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                    ctx2.SaveChanges();
                    ctx2.Entry(advertiser).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                }
                products = ctx2.Products.Where(x => x.Source == "Stack Commerce" && x.ProductName == item.Title.Text && x.Description == item.Summary.Text).FirstOrDefault();
                if (products == null)
                {
                    products = new Bartergora2._0.Models.Product()
                    {
                        Source = "Stack Commerce",
                        AdvertiserId = advertiser.AdvertiserId,
                        ProductName = item.Title.Text,
                        Description = item.Summary?.Text,
                        Price = price,
                        SalePrice = 0,
                        SKU = "0",
                        UPC = "0",
                        linkURL = item.Links[0].Uri.ToString(),
                        DateCreated = DateTime.Now,
                        Deleted = false
                    };
                    ctx2.Products.Add(products);
                    ctx2.Entry(products).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                    ctx2.SaveChanges();
                    ctx2.Entry(products).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                }
                productImage = ctx2.ProductImages.Where(x => x.ProductId == products.ProductId && x.ImageUrl == item.Links[0].Uri.ToString()).FirstOrDefault();
                if (productImage == null)
                {
                    productImage = new ProductImage() { ProductId = products.ProductId, ImageUrl = item.Links[0].Uri.ToString(), DateCreated = DateTime.Now, Deleted = false };
                    ctx2.ProductImages.Add(productImage);
                    ctx2.Entry(productImage).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                    ctx2.SaveChanges();
                    ctx2.Entry(productImage).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                }
                productsToSubCategory = ctx2.ProductsToSubCategories.Where(x => x.SubCategoryId == Convert.ToInt32(item.Categories[0].Label) && x.ProductId == products.ProductId).FirstOrDefault();
                if (productsToSubCategory == null)
                {
                    productsToSubCategory = new ProductsToSubCategories() { SubCategoryId = Convert.ToInt32(item.Categories[0].Label), ProductId = products.ProductId, DateCreated = DateTime.Now, Deleted = false };
                    ctx2.ProductsToSubCategories.Add(productsToSubCategory);
                    ctx2.Entry(productsToSubCategory).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                    ctx2.SaveChanges();
                    ctx2.Entry(productsToSubCategory).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                }
            }
            Assert.IsTrue(true);
        }
        [TestMethod]
        public void TestRakutenLoadMerchants()
        {
            dynamic error = null;
            var merchants = RakutenService.LoadMerchants(_logger);
            RakutenModel products = null;
            List<RakutenModel> productsList = new List<RakutenModel>();
            //Bartergora2._0.Models.Category category = null;
            //Bartergora2._0.Models.SubCategory subCategory = null;   
            int pageCount = 1;
            IEnumerable<CategoriesWithDescription> categories = null;
            //categories = ctx2.Set<GetCategoriesWithDescription>().FromSql($"EXEC [tmd].[GetCategoriesWithDescription]").ToList();
            categories = ctx2.Database.SqlQueryRaw<CategoriesWithDescription>($"EXEC [tmd].[GetCategoriesWithDescription]");

            //List<ModelInput> categoryResults = new List<ModelInput>();
            //foreach (var item in categories)
            //{
            //    categoryResults.Add(new ModelInput { SubCategoryId = item.SubCategoryId, SubCategoryName = item.SubCategoryName/*, Description = item.SubCategoryDefinition*/ });
            //}
            List<ProductInput> categoryResults = new List<ProductInput>();
            foreach (var item in categories)
            {
                categoryResults.Add(new ProductInput { /*Category = item.CategoryName, Description = item.CategoryDescription,*/ SubCategoryId = item.SubCategoryId, SubCategoryName = item.SubCategoryName/*, SubCategoryDefinition = item.SubCategoryDefinition */ });
            }
            var predEngine = ml.InitTextCategoryPredict4(categoryResults);
            foreach (var merchant in merchants.Items)
            {
               products = rakutenService.GetRakutenProducts(1,/*token,*/ merchant, ref error);
               var totalMatches = Convert.ToInt32(products.TotalMatches);
               var totalPages = /*(Convert.ToInt32(products.TotalPages) > maxPages) ? maxPages :*/ Convert.ToInt32(products.TotalPages);
                if(totalPages > 0)
                {
                    LoadRakutenCategories(predEngine,categoryResults, products.items);
                    productsList.Add(products);
                    for (int i = 2; i < totalPages; i++)
                    {
                        products = rakutenService.GetRakutenProducts(i, /*token,*/ merchant, ref error);
                        if(products == null || products.items == null || products.items.Count == 0)
                        {
                            continue;
                        }
                        LoadRakutenCategories(predEngine, categoryResults, products.items);
                        productsList.Add(products);
                        System.Threading.Thread.Sleep(1000);
                    }
                }
                System.Threading.Thread.Sleep(1000);
            }
            Assert.IsTrue(true);
        }
        //private void LoadRakutenCategories(PredictionEngine<ModelInput, ModelOutput> predEngine, List<ModelInput> categoryResults, IEnumerable<RakutenModelItems> items)
        private void LoadRakutenCategories(PredictionEngine<ProductInput, ProductPrediction> predEngine, List<ProductInput> categoryResults, IEnumerable<RakutenModelItems> items)
        {
            //Bartergora2._0.Models.Category category = null;
            //Bartergora2._0.Models.SubCategory subCategory = null;
            //Bartergora2._0.Models.SubCategoriesToCategories subCategoriesToCategory = null;
            Bartergora2._0.Models.ProductsToSubCategories productsToSubCategory = null;
            Bartergora2._0.Models.Advertiser advertiser = null;
            Bartergora2._0.Models.Product products = null;
            Bartergora2._0.Models.ProductImage productImage = null;

            //IEnumerable<GetCategoriesWithDescription> categories = null;
            ////categories = ctx2.Set<GetCategoriesWithDescription>().FromSql($"EXEC [tmd].[GetCategoriesWithDescription]").ToList();
            //categories = ctx2.Database.SqlQueryRaw<GetCategoriesWithDescription>($"EXEC [tmd].[GetCategoriesWithDescription]");

            //List<ModelInput> categoryResults = new List<ModelInput>();
            //foreach(var item in categories)
            //{
            //    categoryResults.Add(new ModelInput { SubCategoryId = item.SubCategoryId, SubCategoryName = item.SubCategoryName, Description = item.SubCategoryDefinition });
            //}
            //var predEngine = ml.InitTextCategoryPredict(categoryResults);

            ml.predictCategory(predEngine, categoryResults, ref items);
            ///ml.SetCategory(categoryResults, ref items);

            foreach (var item in items)
            {
                if (!Uri.TryCreate(item.imageurl, UriKind.Absolute, out Uri uriResult)
                    || !(uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
                    continue;
                advertiser = ctx2.Advertisers.Where(x => x.AdvertiserName.ToLower().Trim() == item.merchantname.ToLower().Trim() && x.AdvertiserKey == Convert.ToInt32(item.mid)).FirstOrDefault();
                if (advertiser == null)
                {
                    advertiser = new Advertiser() { AdvertiserName = item.merchantname, AdvertiserKey = Convert.ToInt32(item.mid), DateCreated = DateTime.Now, Deleted = false };
                    ctx2.Advertisers.Add(advertiser);
                    ctx2.Entry(advertiser).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                    ctx2.SaveChanges();
                    ctx2.Entry(advertiser).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                }
                products = ctx2.Products.Where(x => x.AdvertiserId == advertiser.AdvertiserId && x.ProductName == item.productname && x.Description == item.description[0].secondary).FirstOrDefault();
                if (products == null)
                {
                    products = new Bartergora2._0.Models.Product()
                    {
                        Source = "Rakuten",
                        AdvertiserId = advertiser.AdvertiserId,
                        ProductName = item.productname,
                        Description = item.description[0]._short,
                        Price = Convert.ToDecimal(item.price),
                        SalePrice = Convert.ToDecimal(item.saleprice),
                        SKU = item.sku,
                        UPC = item.upccode,
                        linkURL = item.linkurl,
                        DateCreated = DateTime.Now,
                        Deleted = false
                    };
                    ctx2.Products.Add(products);
                    ctx2.Entry(products).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                    ctx2.SaveChanges();
                    ctx2.Entry(products).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                }
                productImage = ctx2.ProductImages.Where(x => x.ProductId == products.ProductId && x.ImageUrl == item.imageurl).FirstOrDefault();
                if (productImage == null)
                {
                    productImage = new ProductImage() { ProductId = products.ProductId, ImageUrl = item.imageurl, DateCreated = DateTime.Now, Deleted = false };
                    ctx2.ProductImages.Add(productImage);
                    ctx2.Entry(productImage).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                    ctx2.SaveChanges();
                    ctx2.Entry(productImage).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                }
                productsToSubCategory = ctx2.ProductsToSubCategories.Where(x => x.SubCategoryId == item.SubCategoryId && x.ProductId == products.ProductId).FirstOrDefault();
                if (productsToSubCategory == null)
                {
                    productsToSubCategory = new ProductsToSubCategories() { SubCategoryId = item.SubCategoryId, ProductId = products.ProductId, DateCreated = DateTime.Now, Deleted = false };
                    ctx2.ProductsToSubCategories.Add(productsToSubCategory);
                    ctx2.Entry(productsToSubCategory).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                    ctx2.SaveChanges();
                    ctx2.Entry(productsToSubCategory).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                }

            }
        }
        [TestMethod]
        public void TestCjProducts()
        {
            Bartergora2._0.Models.Advertiser advertiser = null;
            Bartergora2._0.Models.Product products = null;
            Bartergora2._0.Models.ProductImage productImage = null;
            Bartergora2._0.Models.ProductsToSubCategories ProductsToSubCategories = null;
            var response = commissionJunctionService.GetProductsGraphQL();

            IEnumerable<CategoriesWithDescription> categories = null;
            //categories = ctx2.Set<GetCategoriesWithDescription>().FromSql($"EXEC [tmd].[GetCategoriesWithDescription]").ToList();
            categories = ctx2.Database.SqlQueryRaw<CategoriesWithDescription>("EXEC [tmd].[GetCategoriesWithDescription]");

            //List<ModelInput> categoryResults = new List<ModelInput>();
            //foreach (var item in categories)
            //{
            //    categoryResults.Add(new ModelInput { SubCategoryId = item.SubCategoryId, SubCategoryName = item.SubCategoryName /*, Description = item.SubCategoryDefinition*/ });
            //}
            List<ProductInput> categoryResults = new List<ProductInput>();
            foreach (var item in categories)
            {
                categoryResults.Add(new ProductInput { /*Category = item.CategoryName, Description = item.CategoryDescription,*/ SubCategoryId = item.SubCategoryId, SubCategoryName = item.SubCategoryName/*, SubCategoryDefinition = item.SubCategoryDefinition */ });
            }
            var predEngine = ml.InitTextCategoryPredict4(categoryResults);

            ml.predictCategory(predEngine, categoryResults, ref response);

            foreach (var item in response.products.ResultList)
            {
                if (!Uri.TryCreate(item.linkCode.imageUrl, UriKind.Absolute, out Uri uriResult)
                    || !(uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
                    continue;
                advertiser = ctx2.Advertisers.Where(x => x.AdvertiserName.ToLower().Trim() == item.advertiserName.ToLower().Trim() && x.AdvertiserKey == Convert.ToInt32(item.advertiserId)).FirstOrDefault();
                if (advertiser == null)
                {
                    advertiser = new Advertiser() { AdvertiserName = item.advertiserName, AdvertiserKey = Convert.ToInt32(item.advertiserId), DateCreated = DateTime.Now, Deleted = false };
                    ctx2.Advertisers.Add(advertiser);
                    ctx2.Entry(advertiser).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                    ctx2.SaveChanges();
                    ctx2.Entry(advertiser).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                }
                products = ctx2.Products.Where(x => x.AdvertiserId == advertiser.AdvertiserId && x.ProductName == item.title && x.Description == item.description).FirstOrDefault();
                if (products == null)
                {
                    products = new Bartergora2._0.Models.Product()
                    {
                        Source = "Commission Junction",
                        AdvertiserId = advertiser.AdvertiserId,
                        ProductName = item.title,
                        Description = item.description,
                        Price = Convert.ToDecimal(item?.price?.amount ?? "0"),
                        SalePrice = Convert.ToDecimal(item?.salePrice?.amount ?? "0"),
                        //SKU = item.sku,
                        //UPC = item.upccode,
                        linkURL = item.linkCode.clickUrl,
                        DateCreated = DateTime.Now,
                        Deleted = false
                    };
                    ctx2.Products.Add(products);
                    ctx2.Entry(products).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                    ctx2.SaveChanges();
                    ctx2.Entry(products).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                }
                productImage = ctx2.ProductImages.Where(x => x.ProductId == products.ProductId && x.ImageUrl == item.linkCode.imageUrl).FirstOrDefault();
                if (productImage == null)
                {
                    productImage = new ProductImage() { ProductId = products.ProductId, ImageUrl = item.linkCode.imageUrl, DateCreated = DateTime.Now, Deleted = false };
                    ctx2.ProductImages.Add(productImage);
                    ctx2.Entry(productImage).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                    ctx2.SaveChanges();
                    ctx2.Entry(productImage).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                }
                ProductsToSubCategories = ctx2.ProductsToSubCategories.Where(x => x.SubCategoryId == item.SubCategoryId && x.ProductId == products.ProductId).FirstOrDefault();
                if (ProductsToSubCategories == null)
                {
                    ProductsToSubCategories = new ProductsToSubCategories() { SubCategoryId = item.SubCategoryId, ProductId = products.ProductId, DateCreated = DateTime.Now, Deleted = false };
                    ctx2.ProductsToSubCategories.Add(ProductsToSubCategories);
                    ctx2.Entry(ProductsToSubCategories).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                    ctx2.SaveChanges();
                    ctx2.Entry(ProductsToSubCategories).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                }
            }
            Assert.IsTrue(true);
        }
        [TestMethod]
        public void TestBartagoraDataLoad()
        {
            bartergoraDataLoadService.LoadData();
            Assert.IsTrue(true);
        }
        [TestMethod]
        public void LoadData()
        {
            // Implement data loading logic here
            IEnumerable<CategoriesWithDescription> categories = null;

            ctx2.Database.ExecuteSqlRaw("EXEC [TMD].[TruncateAllTables]");

            categories = ctx2.Database.SqlQueryRaw<CategoriesWithDescription>("EXEC [tmd].[GetCategoriesWithDescription]");
            List<ProductInput> categoryResults = new List<ProductInput>();
            foreach (var item in categories)
            {
                categoryResults.Add(new ProductInput { /*Category = item.CategoryName, Description = item.CategoryDescription,*/ SubCategoryId = item.SubCategoryId, SubCategoryName = item.SubCategoryName/*, SubCategoryDefinition = item.SubCategoryDefinition */ });
            }
            var predEngine = ml.InitTextCategoryPredict4(categoryResults);

            //LoadCJData(predEngine, categoryResults);
            ////LoadStackCommerceData(predEngine, categoryResults);
            //LoadRakutenData(predEngine, categoryResults);
        }
        [TestMethod]
        public void TestDataLoadCategories()
        {
            var categoryId = ctx2.Categories.Where(x => x.CategoryName == "electronics").Select(x => x.CategoryId).Distinct().ToList();
            var subCategoryId = ctx2.SubCategoriesToCategories.Where(x => categoryId.Any(id => id == x.CategoryId)).Select(x => x.SubCategoryId).Distinct().ToList();
            var subCategories = ctx2.SubCategories.Where(y => subCategoryId.Contains(y.SubCategoryId)).Select(x => new { /*x.SubCategoryId,*/ x.SubCategoryDefinition }).Distinct().ToList();
            Assert.IsTrue(true);
        }
        [TestMethod]
        public void TestGetSynonyms()
        {
            var results = dataMuseService.GetSynonyms("headphone");
            Assert.IsTrue(true);
        }
        [TestMethod]
        public void TestWordAssociation()
        {
            //string word = string.Empty;
            int maxCount = 0;
            string response = string.Empty;
            //var products = ctx2.Products;
            var categories = ctx2.Database.SqlQueryRaw<CategoriesWithDescription>("EXEC [tmd].[GetCategoriesWithDescription]");
            foreach (var item in categories)
            {
                response = string.Empty;
                maxCount = 0;
                var preliminaryWord = item.SubCategoryName.Trim().Split(' ');
                if (item.SubCategoryName.Trim() == "Hairdressing & Cosmetology")
                {
                    Console.WriteLine("test");
                }
                if (preliminaryWord[0] == "hairdressing")
                {
                    Console.WriteLine("test");
                }
                if (preliminaryWord[0].Substring(preliminaryWord[0].Length - 2, 2) == "es")
                {
                    preliminaryWord[0] = preliminaryWord[0].Substring(0, preliminaryWord[0].Length - 2);
                    Console.WriteLine("test");
                }
                if (preliminaryWord[0].Substring(preliminaryWord[0].Length - 1, 1) == "s")
                {
                    preliminaryWord[0] = preliminaryWord[0].Substring(0, preliminaryWord[0].Length - 1);
                    Console.WriteLine("test");
                }
                if (preliminaryWord[0].Substring(preliminaryWord[0].Length - 3, 3) == "ing")
                {
                    preliminaryWord[0] = preliminaryWord[0].Substring(0, preliminaryWord[0].Length - 3);
                    Console.WriteLine("test");
                }
                if (preliminaryWord[0].Substring(preliminaryWord[0].Length - 2, 2) == "s,")
                {
                    preliminaryWord[0] = preliminaryWord[0].Substring(0, preliminaryWord[0].Length - 2);
                    Console.WriteLine("test");
                }
                //var resultsString = dataMuseService.GetSynonyms(preliminaryWord[0]/*.Replace("Accessories", "").Replace("&", "").Trim()*/);
                var resultsString = dataMuseService.GetSynonyms(item.SubCategoryName.Trim());
                if(string.IsNullOrEmpty(resultsString))
                {
                    continue;
                }
                var results = JsonSerializer.Deserialize<List<WordResult>>(resultsString).OrderByDescending(x => x.score);
                foreach (var synonym in results)
                {
                    response += synonym.word + ", ";
                    //maxCount++;
                    //if (maxCount > 1)
                    //{
                    //    break;
                    //}
                }
                var categoryMatch = ctx2.SubCategories.Where(x => x.SubCategoryName == item.SubCategoryName).FirstOrDefault();
                if (categoryMatch != null)
                {
                    categoryMatch.KeyWords = response;
                    ctx2.Attach(categoryMatch);
                    ctx2.Entry(categoryMatch).State = EntityState.Modified;
                    ctx2.SaveChanges();
                    ctx2.Entry(categoryMatch).State = EntityState.Detached;
                }

            }

            Assert.IsTrue(true);
        }
        [TestMethod]
        public void TestYahooMovers()
        {
            var GainersandLosers = _traderGoService.GetGainersandLosers(true);
            Assert.IsTrue(true);
        }
        [TestMethod]
        public void GetPriceHistoryTest()
        {
            string symbol = "MU";
            DateTime startDate = DateTime.Now.AddDays(-45);
            DateTime endDate = DateTime.Now;
            float percentHigher = 0f;
            float percentLower = 0f;
            int higherCount = 0;
            int lowerCount = 0;
            //JsonObject quote = SchwabService.GetQuoteAsync(symbol, true, false).Result;
            //var results = SchwabService.GetPriceHistory(symbol, "day", "2", "minute", 15,
            //    DateTime.Now.AddDays(-30), DateTime.Now);
            var movers = SchwabService.GetMoversAsync().Result;
            foreach (var mover in movers)
            {
                if(mover == null) { continue; }
                var results = SchwabService.GetPriceHistory(mover["Symbol"].ToString(), "month", "1", "daily", 1, startDate, endDate).Result;
                var ser = JsonSerializer.Serialize(results.GetProperty("candles"));
                var candles90 = JsonSerializer.Deserialize<List<TMDInvestment.Models.CandleItems>>(ser).OrderByDescending(x => x.datetime).ToList();
                var candleFirst = candles90.FirstOrDefault();
                mover.Add("lastopChange", candleFirst?.opChange ?? 0f);
                mover.Add("lastopChangePercent", candleFirst?.opPercent ?? 0f);
                mover.Add("lastOpen", candleFirst?.open ?? 0f);
                mover.Add("lastClose", candleFirst?.close ?? 0f);
                foreach (var candle in candles90)
                {
                    if (candle.opChange > 0)
                    {
                        higherCount++;

                    }
                    else if (candle.opChange < 0)
                    {
                        lowerCount++;
                    }
                }
                percentHigher = ((float)higherCount / (float)candles90.Count) * 100f;
                percentLower = ((float)lowerCount / (float)candles90.Count) * 100f;
                mover.Add("PercentHigher", percentHigher);
                mover.Add("PercentLower", percentLower);
            }
            

            //var lastCandle90 = candle90.OrderByDescending(x => x.datetime);
            Assert.IsTrue(true);
        }
        [TestMethod]
        public void TestTextCategoryTrainer()
        {
            IEnumerable<ModelInput> categories = null;
            //// Initialize MLContext
            //var mlContext = new MLContext();
            categories = ctx2.Set<ModelInput>().FromSql($"EXEC [tmd].[GetCategoriesWithDescription]").ToList();
            //// Load your data into a strongly-typed collection
            //var reviews = new[]
            //{
            //    new { Text = "This is a bad steak", Sentiment = "Negative" },
            //    new { Text = "I really like this restaurant", Sentiment = "Positive" },
            //    new { Text = "The food was okay, but the service was slow", Sentiment = "Negative" },
            //    new { Text = "Amazing experience, definitely coming back!", Sentiment = "Positive" }
            //};
            //var reviewsDV = mlContext.Data.LoadFromEnumerable(reviews);

            //// Define your training pipeline
            //var pipeline = mlContext.Transforms.Conversion.MapValueToKey("Label", "Sentiment")
            //    .Append(mlContext.MulticlassClassification.Trainers.TextClassification(/*numberOfClasses: 2,*/ sentence1ColumnName: "Text"))
            //    .Append(mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel")); // Map the predicted key back to a string label

            //// Train the model (Fit the pipeline to the data)
            //var model = pipeline.Fit(reviewsDV);

            //// Create a prediction engine for making predictions
            //var predEngine = mlContext.Model.CreatePredictionEngine<SentimentData, SentimentPrediction>(model);

            //// Test the trained model with new data
            //var input = new SentimentData { Text = "The meal was fantastic!" };
            //var result = predEngine.Predict(input);

            //Console.WriteLine($"Text: {input.Text}");
            //Console.WriteLine($"Prediction: {result.PredictedLabel}");

            //ml.TestTextCategoryTrainer(categories);
        }

        // Define the data classes
        //class SentimentData
        //{
        //    public string Text { get; set; }
        //    public string Sentiment { get; set; }
        //}

        //class SentimentPrediction
        //{
        //    // ML.NET requires a property with this specific name for the prediction output
        //    public string PredictedLabel { get; set; }
        //    public float[] Score { get; set; } // Confidence scores for each class
        //}
    }
}