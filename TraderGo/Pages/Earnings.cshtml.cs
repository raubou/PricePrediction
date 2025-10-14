using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using System.Text.Json;
using TMD.Database.Connect.Managers;
using TMD.Services.Service;
using TMDInvestments.Models;

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
        public EarningsModel(IOptions<ConnectionSettings> connectionSettings, IOptions<ConnectionSettings2> connectionSettings2, IOptions<MailSettings> mailSettings, IHttpClientFactory _factory, ILogger<EarningsModel> logger, IOptions<FinnHub> finnHub, IOptions<Polygon> polycon, IOptions<SchwabSettings> settings) //: base(connectionSettings, connectionSettings2, mailSettings)
        {
            _logger = logger;
            _httpClient = _factory.CreateClient();
            _mgr = new Manager(connectionSettings?.Value?.Connection, "", null);
            _traderGoService = new TraderGoService(_logger, _httpClient, false,finnHub,polycon,_factory, settings);
        }

        public override IActionResult Execute()
        { 
            Earnings = _mgr.GetEarnings().OrderBy(x => x.date)/*.ThenBy(y => y.name)*/.ToList();
            Total = Earnings.Count;
            //Earnings = _traderGoService.GetEarnings();
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
