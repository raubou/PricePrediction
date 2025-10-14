using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Text.Json;
using TMD.Database.Connect.Managers;
using TMD.Services.Service;
using TMDInvestments.Models;
using TMDInvestments.Services;

namespace TraderGo.Pages
{
    public class StockTrackerModel : PageModelBase
    {
        private readonly ILogger<StockTrackerModel> _logger;
        TraderGoService _traderGoService;
        HttpClient _httpClient;
        SchwabSettings _settings;
        IHttpClientFactory _factory;
        Manager _mgr;
        SchwabServices services;
        [BindProperty] public List<StockTracker?> StockTracker {  get; set; } 
        [BindProperty] public int Total { get; set; }
        public StockTrackerModel(IOptions<ConnectionSettings> connectionSettings, IOptions<ConnectionSettings2> connectionSettings2, IOptions<MailSettings> mailSettings, IHttpClientFactory factory, ILogger<StockTrackerModel> logger, IOptions<SchwabSettings> settings) //: base(connectionSettings, connectionSettings2, mailSettings)
        {
            _logger = logger;
            _settings = settings.Value;
            _factory = factory;
            _httpClient = _factory.CreateClient();
            _mgr = new Manager(connectionSettings?.Value?.Connection, "", null);
            _traderGoService = new TraderGoService(_logger, _httpClient, false);
            services = new SchwabServices(_logger, _settings, _factory);
        }
        public override IActionResult Execute()
        {
            StockTracker = ctx.StockTracker.Where(x => x.Status == "Filled" && x.PriceSold == null).OrderByDescending(x => x.DateCreated).ToList();
            Total = StockTracker.Count;
            return Page();
        }
    }
}
