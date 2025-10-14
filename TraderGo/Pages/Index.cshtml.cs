using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using TMD.Database.Connect.Managers;
using TMD.Services.Service;
using TMDInvestments.Context;
using TMDInvestments.Models;
using TMDInvestments.Services;

namespace TraderGo.Pages
{
    public class IndexModel : PageModelBase
    {
        private readonly ILogger<IndexModel> _logger;
        HttpClient _httpClient;
        private readonly SchwabSettings? _settings;
        private readonly IHttpClientFactory? _factory;
        SchwabServices? services;
        [BindProperty]
        public dynamic? Markets { get; set; }
        public dynamic? Movers { get; set; }
        [BindProperty]
        public dynamic? GainersandLosers { get; set; }
        Manager _marketMgr;
        //Manager _NewsMgr;
        TraderGoService _traderGoService;
        public IndexModel(IOptions<ConnectionSettings> connectionSettings, IOptions<ConnectionSettings2> connectionSettings2, IOptions<MailSettings> mailSettings, IOptions<SchwabSettings> settings, IHttpClientFactory factory, ILogger<IndexModel> logger) //: base(connectionSettings,connectionSettings2, mailSettings)
        {
            _logger = logger;
            _httpClient = factory.CreateClient();
            _settings = settings.Value;
            _factory = factory;
            _marketMgr = new Manager(connectionSettings?.Value?.Connection, "",null);
            //_NewsMgr = new Manager(connectionSettings2?.Value?.Connection, "", _logger);
            _traderGoService = new TraderGoService(_logger, _httpClient, false);
            services = new SchwabServices(_logger, _settings, _factory);
        }

        public override IActionResult Execute()
        {
            //Markets = _marketMgr.GetMarkets();
            //GainersandLosers = _marketMgr.GetGainersandLosers();
            //Markets = JsonArray.Create(_traderGoService.GetMarkets());
            Markets = _traderGoService.GetMarkets();
            GainersandLosers = _traderGoService.GetGainersandLosers();
            //Movers = services.GetMovers("$SPX");
            TmdContext ctx = new TmdContext();
            var stockTracker = ctx.StockTracker.ToList();
            return null;
        }
    }
}