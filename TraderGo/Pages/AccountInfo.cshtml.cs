using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Linq;
using TMD.PricePrediction;
using TMD.Services.Service;
using TMDInvestment.Models;
using TMDInvestments.Context;
using TMDInvestments.Models;
using TMDInvestments.Services;
using Microsoft.EntityFrameworkCore;


namespace TraderGo.Pages
{
    [Authorize]
    public class AccountInfoModel : PageModelBase
    {
        [BindProperty] public SchwabAccountInfo? AccountInfo { get; set; }// = new SchwabAccountInfo();
        [BindProperty] public EtradeAccountInfo? AccountInfo2 { get; set; }// = new EtradeAccountInfo();
        [BindProperty] public EtradeAccountInfo2? AccountInfo3 { get; set; }// = new EtradeAccountInfo();
        //[BindProperty] public JsonElement? Orders { get; set; }
        [BindProperty] public string? clientSecret { get; set; }
        [BindProperty] public StockTracker2? stock {  get; set; }// = new StockTracker2();
        //[BindProperty] public List<Token>? tokens { get; set; }
        [BindProperty] public List<StockTracker2?> StockTracker { get; set; }// = new List<StockTracker2?>().AsQueryable();
        [BindProperty] public int stockCnt { get; set; } = 1;
        [BindProperty] public string? status { get; set; } = string.Empty;
        [BindProperty] public string? bold { get; set; } = string.Empty;
        int retryCnt = 0;
        //bool tokenRefreshed = false;
        private readonly ILogger<AccountInfoModel>? _logger;
        private readonly SchwabSettings? _settings;
        private readonly ETradeSettings? _settings2;
        private readonly IHttpClientFactory? _factory;
        public SchwabServices? schwabServices;
        public ETradeService? services2;
        Helper? helper = null;
        dynamic? results = null;
        //public AccountInfoModel(IOptions<ConnectionSettings>? connectionSettings, IOptions<ConnectionSettings2> connectionSettings2, IOptions<MailSettings> mailSettings) : base(connectionSettings, connectionSettings2, mailSettings) { }
        //public AccountInfoModel() { }
        public AccountInfoModel(ILogger<AccountInfoModel>? logger, IOptions<SchwabSettings>? settings, IOptions<ETradeSettings>? settings2, IHttpClientFactory factory, IOptions<ConnectionSettings>? connectionSettings, IOptions<ConnectionSettings2> connectionSettings2, IOptions<MailSettings> mailSettings, IDbContextFactory<TmdContext> contextFactory) //: base(connectionSettings, connectionSettings2, mailSettings)
        {
            _logger = logger;
            _settings = settings?.Value;
            _settings2 = settings2?.Value;
            _factory = factory;
            helper = new Helper(_logger, _settings, _settings2, null, contextFactory);
            schwabServices = new SchwabServices(_logger, _settings, _factory, contextFactory);
            services2 = new ETradeService(_logger, _settings2, _factory, contextFactory);
            //AccountInfo = new JsonElement();
            
        }
        public override IActionResult Execute()
        {
            clientSecret = _settings?.ApiKey;

            //tokens = ctx.Token.ToList();
            //JsonElement accountInfo = new JsonElement();
            //SchwabAccountInfo accountInfo = new SchwabAccountInfo();
            //helper = new Helper();
            //accountAuthRetry:
            //services.GetAccountInfo(ref accountInfo);
            //AccountInfo = accountInfo;
            //if(!((JsonElement)accountInfo).TryGetProperty("securitiesAccount", out JsonElement securitiesAccount) && retryCnt < 3)
            //{
            //    retryCnt += 1;
            //    goto accountAuthRetry;
            //}
            //try
            //{
            //AccountInfo2 = services2?.PortfolioModel();
            //}
            //catch (Exception)
            //{

            //}
            //StockTracker = ctx?.StockTracker2.Where(x => x.PriceSold == null && x.DateSold == null && (x.Status.ToLower() == "filled"))/*.ToList()*/;
            //tasks.Add(Task.Run(() => PerformCalculation(taskId)));
            Thread.CurrentThread.Name = "Main";
            Task taskA = new Task(() => { SchwabAccountInfo accountInfo = new SchwabAccountInfo(); schwabServices?.GetAccountInfo(ref accountInfo); AccountInfo = accountInfo; });
            Task taskB = new Task(() => AccountInfo2 = services2?.PortfolioModel());
            Task taskC = new Task(() => StockTracker = ctx?.StockTracker2.Where(x => x.PriceSold == null && x.DateSold == null && (x.Status.ToLower() == "filled"))?.ToList());
            Task taskD = new Task(() => AccountInfo3 = services2?.AccountModel());
            taskA.Start();
            taskB.Start();
            taskC.Start();
            taskD.Start();
            try
            {
                Task.WhenAll(taskA, taskB, taskC, taskD).Wait();
            }
            catch (Exception)
            {

                //throw;
            }
            
            return Page();
        }
        public IActionResult OnPost()
        {
            var platform = Convert.ToInt32(Request.Form["platform"].ToString());
            var symbol = Request.Form["symbol"].ToString();
            Platform tradePlatform = (Platform)platform;
            //var platform = Request.Form["platform"];
            //var products = coinBaseService.GetProducts();
            var stock = ctx.StockTracker2.Where(x => x.PriceSold == null && x.DateSold == null && x.Status == "FILLED" && x.Symbol == symbol && x.TradePlatform == (int)tradePlatform).FirstOrDefault();
            //var order = coinBaseService.GetOrder((Guid)coin.BuyOrderId);
            var results = schwabServices.GetQuoteAsync(symbol).Result;
            TradeDirection currentTradeDirection = (TradeDirection)Enum.Parse(typeof(TradeDirection), stock.TradeDirection.ToString(), true);
            TradeDirection tradeDirection = (currentTradeDirection == TradeDirection.BUY) ? TradeDirection.SELL : TradeDirection.BUY_TO_COVER;
            switch (platform)
            {
                case 1:
                    BuySellStock(stock, results, tradeDirection, stock.Quantity, true, schwabServices);
                    break;
                case 2:
                    BuySellStock(stock, results, tradeDirection, stock.Quantity, true, services2);
                    break;
            }

            return Execute();
        }
        bool BuySellStock(StockTracker2 stocktracker, dynamic order, TradeDirection direction, int? qty, bool IsReversed, SchwabServices service)
        {
            //return false;
            string? jsonOrder = helper?.CreateTradeObject(order, direction, qty);
            dynamic results = service.SetOrder(jsonOrder);
            //if direction is buy or sell short, stocktracker will be null.
            try
            {
                var options = new JsonSerializerOptions
                {
                    ReferenceHandler = ReferenceHandler.Preserve
                };
                string output = System.Text.Json.JsonSerializer.Serialize(results, options);
                if (output.Contains("message"))
                {
                    //helper.Log(_logger, LogLevel.Error, $"stock trade failed {order["Symbol"].ToString()} {output}");
                }
                else
                {
                    //helper.Log(_logger, LogLevel.Information, $"stock trade results {output}");
                    helper.SaveStockTracker(stocktracker, Platform.CharlesSchwab, direction, qty, IsReversed, order, output, _logger/*, ref ctx*/);
                }
            }
            catch (Exception ex)
            {
                //helper.Log(_logger, LogLevel.Error, $"Error in buying stock - {ex.Message}");
            }
            return true;
        }
        bool BuySellStock(StockTracker2 stocktracker, dynamic order, TradeDirection direction, int? qty, bool IsReversed, ETradeService service)
        {
            //return true;
            qty = Math.Abs((int)qty);
            dynamic results = service.SetOrder(order["Symbol"].ToString(), "MARKET", string.Empty, qty, direction);
            //if direction is buy or sell short, stocktracker will be null.
            //string output = System.Text.Json.JsonSerializer.Serialize(results);
            //((XElement)results).Element("code")?.Value?.ToString();
            try
            {
                var options = new JsonSerializerOptions
                {
                    ReferenceHandler = ReferenceHandler.Preserve
                };
                string output = System.Text.Json.JsonSerializer.Serialize(results, options);
                if (output.Contains("errors"))
                {
                    //helper.Log(_logger, LogLevel.Error, $"Stock trade failed {order["Symbol"].ToString()} {output}");
                    return false;
                }
                //helper.Log(_logger, LogLevel.Information, $"Stock trade results {order["Symbol"].ToString()} Success");
                //SaveStockTracker(stocktracker, direction, qty, order, "Success");
                helper.SaveStockTracker(stocktracker, Platform.ETrade, direction, qty, IsReversed, order, "Success", _logger/*, ref ctx*/);
                return true;
            }
            catch (Exception ex)
            {
                //helper.Log(_logger, LogLevel.Error, $"Error in buying stock - {ex.Message}");
            }
            return true;
        }
    }
}
