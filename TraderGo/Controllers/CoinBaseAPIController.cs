using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Dynamic;
using TMD.Database.Connect.Managers;
using TMD.Services;
using TMD.Services.Service;
using TMDInvestments.Context;
using TMDInvestments.Models;
using TMDInvestments.Services;

namespace TraderGo.Controllers
{
    [EnableCors("TraderGo")]
    [Route("api/[controller]")]
    [ApiController]
    public class CoinBaseAPIController : ControllerBase
    {
        Manager? _mgr;
        Manager? _NewsMgr;
        private IOptions<ConnectionSettings> _connectionSettings;
        private IOptions<ConnectionSettings2> _connectionSettings2;
        private IOptions<FinnHub> _finnHub;
        private IOptions<Polygon> _polygon;
        dynamic error = new ExpandoObject();
        private readonly ILogger<TraderGoAPIController>? _logger;
        private readonly CoinBaseSettings? _settings;
        private readonly IHttpClientFactory? _factory;
        CoinBaseService? services;
        TraderGoService _traderGoService;
        HttpClient _client;
        TmdContext ctx = null;
        public CoinBaseAPIController(IOptions<ConnectionSettings> connectionSettings, IOptions<ConnectionSettings2> connectionSettings2, IOptions<FinnHub> finnHub, IOptions<Polygon> polycon, IHttpClientFactory? httpClientFactory, ILogger<TraderGoAPIController> logger, IOptions<CoinBaseSettings> settings)
        {
            _connectionSettings = connectionSettings;
            _connectionSettings2 = connectionSettings2;
            _finnHub = finnHub;
            _polygon = polycon;
            _logger = logger;
            _settings = settings.Value;
            _factory = httpClientFactory;
            //_client = _httpClientFactory.CreateClient();
            _client = httpClientFactory?.CreateClient();
            _traderGoService = new TraderGoService(_logger, _client, false);
        }
        [HttpGet, Route("GetQuote/{symbol}")]
        public IActionResult GetQuote(string symbol)
        {
            services = new CoinBaseService(_logger, _settings, _factory);
            var results = services.GetQuote(symbol);
            return Ok(results);
        }
        [HttpGet, Route("CryptoTracker/{coin}")]
        public IActionResult GeCryptfromCrypotTracker(string coin)
        {
            ctx = new TmdContext();
            dynamic results = ctx.CryptoList.Where(x => x.Coin == coin).FirstOrDefault();
            if (results != null)
            {
                return Ok(results);
            }
            else
            {
                results = new ExpandoObject();
                results.error = "Not in Crypto Tracker";
                return Ok(results);
            }
        }
        [HttpGet, Route("AddToCryptoTracker/{coin}/{name}")]
        public IActionResult AddToCryptoTracker(string coin, string name)
        {
            ctx = new TmdContext();
            CryptoList sl = new CryptoList()
            {
                Coin = coin,
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
                ctx.CryptoList.Attach(sl);
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
        [HttpGet, Route("DeleteFromCryptoTracker/{symbol}")]
        public IActionResult DeleteFromCryptoTracker(string coin)
        {
            ctx = new TmdContext();
            try
            {
                CryptoList sl = ctx.CryptoList.Where(x => x.Coin == coin).FirstOrDefault();
                ctx.CryptoList.Attach(sl);
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
    }
}
