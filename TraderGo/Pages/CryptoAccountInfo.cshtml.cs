using Coinbase.AdvancedTrade.Models;
using Coinbase.AdvancedTrade.Models.Public;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NewsAggregator.models.returnModel;
using System.Text.Json.Nodes;
using TMD.PricePrediction;
using TMD.Services;
using TMDInvestment.Models;
using TMDInvestments.Context;
using TMDInvestments.Models;

namespace TraderGo.Pages
{
    [Authorize]
    public class CryptoAccountInfoModel : PageModelBase
    {
        private readonly ILogger<CryptoAccountInfoModel> _logger;
        private readonly CoinBaseSettings _settings;
        private readonly IHttpClientFactory _factory;
        Helper _helper  = null;
        [BindProperty] public CoinBaseService coinBaseService { get; set; }
        [BindProperty] public List<Account>? Accounts { get; set; }
        [BindProperty] public List<PortfolioBreakdown>? Portfolios { get; set; }
        [BindProperty] public List<PublicProduct>? PublicProducts { get; set; }
        [BindProperty] public List<CryptoList> CryptoList { get; set; }
        [BindProperty] public List<CryptoTracker2> CryptoTracker { get; set; }
        public CryptoAccountInfoModel(IOptions<ConnectionSettings> connectionSettings, IOptions<ConnectionSettings2> connectionSettings2, IOptions<MailSettings> mailSettings, IHttpClientFactory factory, ILogger<CryptoAccountInfoModel> logger, IOptions<CoinBaseSettings> settings, IDbContextFactory<TmdContext> contextFactory)// : base(connectionSettings, connectionSettings2, mailSettings)
        {
            _logger = logger;
            _settings = settings.Value;
            _factory = factory;
            coinBaseService = new CoinBaseService(_logger, _settings, _factory/*, new Helper(new TMDInvestments.Context.TmdContext())*/);
            _helper = new Helper(_logger, null, null, null, contextFactory);
        }

        public override IActionResult Execute()
        {
            Accounts = coinBaseService.GetAccounts().ToList();
            PublicProducts = coinBaseService.GetProducts().OrderBy(x => x.DisplayName).ToList();
            Portfolios = coinBaseService.GetPortfolios();
            CryptoList = ctx.CryptoList.ToList();
            CryptoTracker = ctx.CryptoTracker2.Where(x => x.PriceSold == null && x.DateSold == null && (x.BuyStatus.ToLower() == "filled")).ToList();
            return Page();
        }
        public IActionResult OnPost()
        {
            var fromData = Request.Form["Currency"];
            var products = coinBaseService.GetProducts();
            //var coin = ctx.CryptoTracker2.Where(x => x.Coin == fromData + "-USD").FirstOrDefault();
            var coin = ctx.CryptoTracker2.Where(x => x.PriceSold == null && x.DateSold == null && (x.BuyStatus.ToLower() == "filled")).FirstOrDefault();
            coinBaseService = new CoinBaseService(_logger, _settings, _factory/*, new Helper(ctx)*/);
            var order = coinBaseService.GetOrder((Guid)coin.BuyOrderId);
            JsonObject results = coinBaseService.GetQuoteAsync(coin.Coin).Result;
            BuySellCrypto(coin, results, TradeDirection.SELL, Math.Round(Convert.ToDecimal(order.FilledSize), 5), coinBaseService, products, false);
            return Execute();
        }
        bool BuySellCrypto(CryptoTracker2 cryptoTracker, dynamic order, TradeDirection direction, decimal amount, CoinBaseService service, List<PublicProduct> publicProducts, bool sellShort)
        {
            string symbol = (order["Symbol"].ToString().Substring((order["Symbol"].ToString().Length - 1), 1) == "C") ? order["Symbol"].ToString().Substring(0, (order["Symbol"].ToString().Length - 1)) : order["Symbol"].ToString();
            PublicProduct? product = publicProducts.Where(x => x.DisplayName == symbol).FirstOrDefault();
            string precision = product.BaseIncrement.Substring((product.BaseIncrement.IndexOf(".") + 1), (product.BaseIncrement.Length - (product.BaseIncrement.IndexOf(".") + 1)));
            string[] currency = product.DisplayName.Split('-');
            var account = coinBaseService.GetAccounts().Where(x => x.Currency == currency[0]).FirstOrDefault();
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
                    output = coinBaseService.SetOrder(product.DisplayName, coinbaseDirection, amountSize);
                    break;
                case TradeDirection.SELL:
                    //product = publicProducts.Where(x => x.DisplayName == symbol).FirstOrDefault();
                    //precision = product.BaseIncrement.Substring((product.BaseIncrement.IndexOf(".") + 1), (product.BaseIncrement.Length - (product.BaseIncrement.IndexOf(".") + 1)));
                    //currency = product.DisplayName.Split('-');
                    //var account = services.GetAccounts().Where(x => x.Currency == currency[0]).FirstOrDefault();
                    //var baseSize = (account.AvailableBalance.Value.IndexOf(".") == -1) ? Convert.ToDecimal(account.AvailableBalance.Value) : Convert.ToDecimal(account.AvailableBalance.Value.Substring(0, (account.AvailableBalance.Value.IndexOf(".") + precision.Length)));
                    //output = coinBaseService.SetOrder(product.DisplayName, coinbaseDirection, baseSize);
                    var currentBalance = Math.Round(Convert.ToDecimal(account?.AvailableBalance?.Value) * Convert.ToDecimal(product?.Price), 5);
                    var results = coinBaseService.GetQuoteAsync(product.DisplayName).Result;
                    _helper.BuySellCrypto(cryptoTracker, results, TradeDirection.SELL, currentBalance, coinBaseService, publicProducts/*, false, false*/);
                    break;
            }
            //if (output != null)
            //{
            //    helper.Log(_logger, LogLevel.Information, $"Crypto trade results for {symbol}, {output.Status}");
            //}
            //else
            //{
            //    helper.Log(_logger, LogLevel.Error, $"Crypto trade results order {symbol} failed.");
            //}

            if (output != null && (output.Status.ToLower() == "filled" || output.Status.ToLower() == "open" || output.Status.ToLower() == "pending"))
                SaveCryptoTracker(cryptoTracker, direction, (direction == TradeDirection.SELL) ? Convert.ToDecimal(output.TotalValueAfterFees) : amount, order, output);
            return true;
        }
        bool SaveCryptoTracker(CryptoTracker2 cryptoTracker, TradeDirection direction, decimal currentBalance, dynamic results, Coinbase.AdvancedTrade.Models.Order order)
        {
            if (cryptoTracker != null && (direction == TradeDirection.SELL || direction == TradeDirection.BUY_TO_COVER))
            {
                cryptoTracker.PriceSold = Convert.ToDecimal(results["lastPrice"].ToString());
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
                cryptoTracker.SoldMacd = Convert.ToDecimal(results["MACD"].ToString());
                cryptoTracker.SoldMacdSignal = Convert.ToDecimal(results["MACDSignal"].ToString());
                cryptoTracker.SoldMacdHistogram = Convert.ToDecimal(results["MACDHistogram"].ToString());
                cryptoTracker.CurrentBalance = currentBalance;
                cryptoTracker.SellOrderId = Guid.Parse(order.OrderId);
                cryptoTracker.SellStatus = order.Status;
                cryptoTracker.DateSold = DateTime.Now;
                cryptoTracker.DateUpdated = DateTime.Now;
                ctx.CryptoTracker2.Attach(cryptoTracker);
                ctx.Entry(cryptoTracker).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
            }
            else
            {
                cryptoTracker = new CryptoTracker2();
                cryptoTracker.Coin = results["Symbol"].ToString();
                cryptoTracker.BuyOrderId = Guid.Parse(order.OrderId);
                cryptoTracker.BuyStatus = order.Status;
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
                cryptoTracker.PurchasedMacd = Convert.ToDecimal(results["MACD"].ToString());
                cryptoTracker.PurchasedMacdSignal = Convert.ToDecimal(results["MACDSignal"].ToString());
                cryptoTracker.PurchasedMacdHistogram = Convert.ToDecimal(results["MACDHistogram"].ToString());
                cryptoTracker.LastPrice = Convert.ToDecimal(results["lastPrice"].ToString());
                cryptoTracker.DatePurchased = DateTime.Now;
                cryptoTracker.DateCreated = DateTime.Now;
                cryptoTracker.LastUpdatedBy = 1;
                ctx.CryptoTracker2.Add(cryptoTracker);
                ctx.Entry(cryptoTracker).State = Microsoft.EntityFrameworkCore.EntityState.Added;
            }
            try
            {
                ctx.SaveChanges();
                ctx.Entry(cryptoTracker).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
            }
            catch (Exception ex)
            {
                //helper.Log(_logger, LogLevel.Error, $"Source: {ex.Source} stack trace: {ex.StackTrace} inner exception {ex?.InnerException?.Message ?? ""} error: {ex.Message}");
            }

            return true;
        }
    }
}
