using log4net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using TMD.Database.Connect.Managers;
using TMD.Services.Service;
using TMDInvestments.Models;
using TMDInvestments.Services;

namespace TraderGo.Pages
{
    public class AnalysisModel : PageModelBase
    {
        private readonly ILogger<AnalysisModel> _logger;
        TraderGoService _traderGoService;
        //HttpClient _httpClient;
        SchwabSettings _settings;
        IHttpClientFactory _factory;
        Manager _mgr;
        SchwabServices services;
        [BindProperty] public List<StockList>? StockLists { get; set; }
        [BindProperty] public StockList? StockList { get; set; } = new StockList();
        [BindProperty] public List<dynamic>? Analysis {  get; set; } = new List<dynamic>();
        [BindProperty] public int Total { get; set; }
        public AnalysisModel(IOptions<ConnectionSettings> connectionSettings, IOptions<ConnectionSettings2> connectionSettings2, IOptions<MailSettings> mailSettings, IHttpClientFactory factory, ILogger<AnalysisModel> logger, IOptions<SchwabSettings> settings) //: base(connectionSettings, connectionSettings2, mailSettings)
        {
            _logger = logger;
            _settings = settings.Value;
            _factory = factory;
            //_httpClient = _factory.CreateClient();
            _mgr = new Manager(connectionSettings?.Value?.Connection, "", null);
            _traderGoService = new TraderGoService(null, _logger, _factory, false);
            services = new SchwabServices(_logger, _settings, _factory);
        }
        public override IActionResult Execute()
        {
            StockLists = ctx.StockList.ToList();
            Total = StockLists.Count();
            foreach (var item in StockLists)
            {
                try
                {
                    JsonObject results = services.GetQuote(item.Symbol, false);
                    Analysis.Add(results);
                }
                catch (Exception ex)
                {

                }
            }
            return null;
        }
    }
}
