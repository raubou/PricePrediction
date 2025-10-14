using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using System.Net.Http;
using TMD.Database.Connect.Managers;
using TMD.Services.Service;
using TMDInvestments.Models;

namespace TraderGo.Pages
{
    public class IPOsandEarningsModel : PageModelBase
    {
        private readonly ILogger<IPOsandEarningsModel> _logger;
        TraderGoService _traderGoService;
        HttpClient _httpClient;
        Manager _mgr;
        [BindProperty]
        public dynamic? IPOs { get; set; }
        [BindProperty]
        public int Total { get; set; }
        public IPOsandEarningsModel(IOptions<ConnectionSettings> connectionSettings, IOptions<ConnectionSettings2> connectionSettings2, IOptions<MailSettings> mailSettings, IHttpClientFactory _factory, ILogger<IPOsandEarningsModel> logger, IOptions<FinnHub> finnHub, IOptions<Polygon> polycon, IOptions<SchwabSettings> settings) //: base(connectionSettings, connectionSettings2, mailSettings)
        {
            _logger = logger;
            _httpClient = _factory.CreateClient();
            _mgr = new Manager(connectionSettings?.Value?.Connection, "", null);
            _traderGoService = new TraderGoService(_logger, _httpClient, false, finnHub, polycon, _factory, settings); 
        }

        public override IActionResult Execute()
        {
            IPOs = _mgr.GetIPOs();
            Total = IPOs.Count;
            //IPOs = _traderGoService.GetIPOs();
            return null;
        }

        public IActionResult OnPost()
        {
            _traderGoService.LoadIPOs();
            Execute();
            return Page();
        }
    }
}

