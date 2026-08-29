using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Options;
using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Serialization;
using TMD.Database.Connect.Managers;
using TMD.Services.Service;
using TMDInvestments.Context;
using TMDInvestments.Models;
using TMDInvestments.Services;


namespace TraderGo.Controllers
{
    [EnableCors("TraderGo")]
    [Route("api/[controller]")]
    [ApiController]
    public sealed class TraderGoAPIController : ControllerBase
    {
        Manager? _mgr;
        Manager? _NewsMgr;
        private IOptions<ConnectionSettings> _connectionSettings;
        private IOptions<ConnectionSettings2> _connectionSettings2;
        private IOptions<FinnHub> _finnHub;
        private IOptions<Polygon> _polygon;
        dynamic error = new ExpandoObject();
        private readonly ILogger<TraderGoAPIController>? _logger;
        private readonly SchwabSettings? _settings;
        private readonly IHttpClientFactory? _factory;
        SchwabServices? services;
        TraderGoService _traderGoService;
        HttpClient _client;
        TmdContext ctx = null;
        IDbContextFactory<TmdContext> _contextFactory;


        public TraderGoAPIController(IOptions<ConnectionSettings> connectionSettings, IOptions<ConnectionSettings2> connectionSettings2, IOptions<FinnHub> finnHub, IOptions<Polygon> polycon, IHttpClientFactory? httpClientFactory, ILogger<TraderGoAPIController> logger, IOptions<SchwabSettings> settings, IDbContextFactory<TmdContext> contextFactory)
        {
            _connectionSettings = connectionSettings;
            _connectionSettings2 = connectionSettings2;
            _finnHub = finnHub;
            _polygon = polycon;
            _logger = logger;
            _settings = settings.Value;
            _factory = httpClientFactory;
            _contextFactory = contextFactory;
            //_client = _httpClientFactory.CreateClient();
            //_client = httpClientFactory?.CreateClient();
            _traderGoService = new TraderGoService(null, _logger, httpClientFactory, false, contextFactory);
        }
        [HttpGet("Init")]
        public string Init()
        {
            return "OK";
        }
        [HttpGet("GetByCategory/{category}/{start}/{length}")]
        public IActionResult GetByCategory(string category, int start, int length)
        {
            RssModel rssAll;
            //SyncIOFeature();
            _NewsMgr = new Manager(_connectionSettings2?.Value.Connection, "", null);
            rssAll = _NewsMgr.GetNews(category, false, ref error);
            if (rssAll.items.Count() > 0)
                return Ok(rssAll);
            else
                return BadRequest(error);
        }

        [HttpGet("Search/{symbol}")]
        public string Search(string symbol)
        {
            //SyncIOFeature();
            _mgr = new Manager("", _finnHub.Value, _polygon.Value, _factory);
            var results = _mgr.Search(symbol);
            //if (!string.IsNullOrEmpty(results?.GetProperty("results")?.ToString()))
            if(results != string.Empty)
                return results;
            else
            {
                error.error = "error";
                return BadRequest(error).ToString();
            }
                
        }

        [HttpGet("SearchQuote/{symbol}/{fromDate}/{toDate}")]
        public string SearchQuote(string symbol, string fromDate, string toDate)
        {
            //SyncIOFeature();
            //dynamic results = new ExpandoObject();
            //dynamic quote = null;
            //dynamic profile = null;
            //dynamic ticker = null;
            //dynamic data = null;
            _mgr = new Manager("", _finnHub.Value, _polygon.Value, _factory);
            //await Task.Run(() =>
            //{
            /*quote*/
            var results = _mgr.GetQuote(symbol);
            //    profile = _mgr.GetProfile(symbol).Result;
            //    ticker = _mgr.GetHistory(symbol, fromDate, toDate).Result;
            //    data = _mgr.GetCompanyNews(symbol, fromDate, toDate).Result;

            //});
            //results.quote = quote;
            //results.profile = profile;
            //results.ticker = ticker;
            //results.data = data;
            //if (!string.IsNullOrEmpty(results?.GetProperty("c")?.ToString()))
            //if(results?.c > 0)
            if (results != string.Empty)
                return results;
            else
            {
                error.error = "error";
                return BadRequest(error).ToString();
            }
        }
        [HttpGet("GetProfile/{symbol}")]
        public string GetProfile(string symbol)
        {
            //SyncIOFeature();
            _mgr = new Manager("", _finnHub.Value, _polygon.Value, _factory);
            var results = _mgr.GetProfile(symbol);
            //if (!string.IsNullOrEmpty(results?.GetProperty("results")?.GetProperty("ticker")?.ToString()))
            if (results != string.Empty)
                return results;
            else
            {
                error.error = "error";
                return BadRequest(error).ToString();
            }
        }

        [HttpGet("GetHistory/{symbol}/{fromDate}/{toDate}")]
        public string GetHistory(string symbol, string fromDate, string toDate)
        {
            //SyncIOFeature();
            _mgr = new Manager("", _finnHub.Value, _polygon.Value, _factory);
            var results = _mgr.GetHistory(symbol, fromDate, toDate);
            //if (!string.IsNullOrEmpty(results?.GetProperty("ticker")?.ToString()))
            if (results != string.Empty)
                return results;
            else
            {
                error.error = "error";
                return BadRequest(error).ToString();
            }
        }
        [HttpGet("GetNews")]
        public IActionResult GetNews()
        {
            //SyncIOFeature();
            //_mgr = new Manager(_finnHub.Value, _polygon.Value, _factory);
            var results = _traderGoService.GetNews();
            //if (!string.IsNullOrEmpty(results?.GetProperty("ticker")?.ToString()))
            if (results is object)
                return Ok(results);
            else
                return BadRequest(error);
        }

        [HttpGet("GetCompanyNews/{symbol}/{fromDate}/{toDate}")]
        public string GetCompanyNews(string symbol, string fromDate, string toDate)
        {
            //SyncIOFeature();
            _mgr = new Manager("", _finnHub.Value, _polygon.Value, _factory);
            var results = _mgr.GetCompanyNews(symbol, fromDate, toDate);
            //if (!string.IsNullOrEmpty(results?[0].GetProperty("category")?.ToString()))
            if(results is object) 
                return results;
            else
            {
                error.error = "error";
                return BadRequest(error).ToString();
            }
        }

        [HttpGet("GetGroupDaily/{date}")]
        public IActionResult GetGroupDaily(string date)
        {
            //SyncIOFeature();
            _mgr = new Manager("", _finnHub.Value, _polygon.Value, _factory);
            var results = _mgr.GetGroupDaily(date, ref error);
            if (!string.IsNullOrEmpty(results.GetProperty("results")?.ToString()))
                return Ok(results);
            else
                return BadRequest(error);
        }

        [HttpGet("GetArticleById/{Id}")]
        public IActionResult GetArticleById(string Id)
        {
            //SyncIOFeature();
            RssModelItems item;
            _NewsMgr = new Manager(_connectionSettings2?.Value.Connection, "", null);
            item = _NewsMgr.GetArticleById(Id, ref error);
            //rssAll.items = (List<RssModelItems>)rssAll.items.Skip(start).Take(length).ToList();
            if (item != null)
                return Ok(item);
            else
                return BadRequest(error);
        }

        [HttpGet, Route("SearchSchwab/{name}")]
        public IActionResult SearchSchwab(string name)
        {
            services = new SchwabServices(_logger, _settings, _factory, _contextFactory);
            var results = services.Search(name.ToUpper());
            return Ok(results);
        }
        [HttpGet, Route("GetQuote/{symbol}")]
        public async Task<IActionResult> GetQuote(string symbol)
        {
            services = new SchwabServices(_logger, _settings, _factory, _contextFactory);
            var results = await services.GetQuoteAsync(symbol, true);
           //System.Threading.Thread.Sleep(250);
            return Ok(results);
        }
        [HttpGet, Route("GetPriceHistory/{symbol}/{periodType}/{period}/{frequencyType}/{frequency}/{startDate}/{endDate}")]
        public IActionResult GetPriceHistory(string symbol, string periodType, string period, string frequencyType, int frequency, DateTime startDate, DateTime endDate)
        {
            services = new SchwabServices(_logger, _settings, _factory, _contextFactory);
            var results = services.GetPriceHistory(symbol, periodType, period, frequencyType, frequency, startDate, endDate);
            //var elfenreponce = System.Text.Json.JsonSerializer.Deserialize(results);
            return Ok(results);
        }
        [HttpGet, Route("GetPriceHistory/{symbol}/{periodType}/{period}/{frequencyType}/{frequency}")]
        public IActionResult GetPriceHistory(string symbol, string periodType, string period, string frequencyType, int frequency)
        {
            services = new SchwabServices(_logger, _settings, _factory, _contextFactory);
            var results = services.GetPriceHistory(symbol, periodType, period, frequencyType, frequency, null, null);
            //var elfenreponce = System.Text.Json.JsonSerializer.Deserialize(results);
            return Ok(results);
        }
        [HttpGet, Route("StockTracker/{symbol}")]
        public IActionResult GetStockfromStockTracker(string symbol) {
            ctx = new TmdContext();
            dynamic results = ctx.StockList.Where(x => x.Symbol == symbol).FirstOrDefault();
            if (results != null)
            {
                return Ok(results);
            }
            else { 
                results = new ExpandoObject();
                results.error = "Not in Stock Tracker";
                return Ok(results);
            }
        }
        [HttpGet, Route("AddToStockTracker/{symbol}/{name}")]
        public IActionResult AddToStockTracker(string symbol, string name)
        {
            ctx = new TmdContext();
            StockList sl = new StockList()
            {
                Symbol = symbol,
                Name = name,
                UseRsi = true,
                UseSma200 = true,
                UseEma200 = true,
                UseVwap = true,
                UseEma9 = true,
                UseEma12 = true,
                UseEma26 = true,
                UseMacd = true,
                ExcludeFromAutomation = false,
                DateCreated = DateTime.Now,
                LastUpdatedBy = 1                
            };
            try
            {
                ctx.StockList.Attach(sl);
                ctx.Entry(sl).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                ctx.SaveChanges();
                return Ok();
            }
            catch (Exception)
            {
                dynamic results = new ExpandoObject();
                results.error = "Not in Stock Tracker";
                return BadRequest(results);
            }
        }
        [HttpGet, Route("DeleteFromStockTracker/{symbol}")]
        public IActionResult DeleteFromStockTracker(string symbol)
        {
            ctx = new TmdContext();
            try
            {
                StockList sl = ctx.StockList.Where(x => x.Symbol == symbol).FirstOrDefault();
                ctx.StockList.Attach(sl);
                ctx.Entry(sl).State = Microsoft.EntityFrameworkCore.EntityState.Deleted;
                ctx.SaveChanges();
                return Ok();
            }
            catch (Exception)
            {
                dynamic results = new ExpandoObject();
                results.error = "Not in Stock Tracker";
                return BadRequest(results);
            }
        }
        //[HttpGet]
        //public IActionResult BuYSellStock(StockTracker stocktracker, dynamic order, Platform tradePlatfrom, TradeDirection direction, int qty)
        //{
        //    //return false;
        //    string jsonOrder = Helper.CreateTradeObject(order, direction, qty);
        //    dynamic results = services.SetOrder(jsonOrder);
        //    //if direction is buy or sell short, stocktracker will be null.
        //    try
        //    {
        //        var options = new JsonSerializerOptions
        //        {
        //            ReferenceHandler = ReferenceHandler.Preserve
        //        };
        //        string output = System.Text.Json.JsonSerializer.Serialize(results, options);
        //        if (output.Contains("message"))
        //        {
        //            //Helper.Log(_logger, LogLevel.Error, $"stock trade failed {order["Symbol"].ToString()} {output}");
        //        }
        //        else
        //        {
        //            //Helper.Log(_logger, LogLevel.Information, $"stock trade results {output}");
        //            Helper.SaveStockTracker(stocktracker,Platform.CharlesSchwab, direction, qty, order, output, _logger, ref ctx);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        //Helper.Log(_logger, LogLevel.Error, $"Error in buying stock - {ex.Message}");
        //        return BadRequest(ex);
        //    }
        //    return Ok();
        //}
    }
}
