using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using System.Text.Json;
using TMD.Database.Connect.Managers;
using TMD.Services.Service;
using TMDInvestments.Models;
using TMDInvestments.Services;

namespace TraderGo.Pages
{
    public class EarningsModel : PageModelBase
    {
        private readonly ILogger<EarningsModel> _logger;
        TraderGoService _traderGoService;
        HttpClient _httpClient;
        Manager _mgr;
        [BindProperty]
        public dynamic? Earnings { get; set; }
        [BindProperty]
        public int Total { get; set; }    
        [BindProperty]
        public SchwabServices schwabServices { get; set; }
        [BindProperty]
        public IEnumerable<StockList> stockList { get; set; }
        SchwabSettings _settings;
        private readonly IHttpClientFactory _factory;
        public EarningsModel(IOptions<ConnectionSettings> connectionSettings, IOptions<ConnectionSettings2> connectionSettings2, IOptions<MailSettings> mailSettings, IHttpClientFactory _factory, ILogger<EarningsModel> logger, IOptions<FinnHub> finnHub, IOptions<Polygon> polycon, IOptions<SchwabSettings> settings) //: base(connectionSettings, connectionSettings2, mailSettings)
        {
            _logger = logger;
            _httpClient = _factory.CreateClient();
            _mgr = new Manager(connectionSettings?.Value?.Connection, "", null);
            _traderGoService = new TraderGoService(_logger, _httpClient, false,finnHub,polycon,_factory, settings);
            _settings = settings.Value;
            _factory = _factory;
            schwabServices = new SchwabServices(_logger, _settings, _factory);
        }

        public override IActionResult Execute()
        { 
            Earnings = _mgr.GetEarnings().OrderBy(x => x.date)/*.ThenBy(y => y.name)*/.ToList();
            Total = Earnings.Count;
            stockList = ctx.StockList;
            return null;
        }

        public IActionResult OnPost()
        {
            _traderGoService.LoadEarnings();
            Execute();
            return Page();
        }
    }
}
