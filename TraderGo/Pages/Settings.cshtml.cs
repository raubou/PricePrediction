using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using System.Net.Http;
using TMD.Database.Connect.Managers;
using TMD.Services.Service;
using TMDInvestments.Models;

namespace TraderGo.Pages
{
    [Authorize]
    public class SettingsModel : PageModelBase
    {
        private readonly ILogger<SettingsModel> _logger;
        TraderGoService _traderGoService;
        HttpClient _httpClient;
        Manager _mgr;
        [BindProperty] public RulesEngine? RulesEngines { get; set; }
        [BindProperty] public RulesEngine? RulesEngine { get; set; }
        public SettingsModel(IOptions<ConnectionSettings> connectionSettings, IOptions<ConnectionSettings2> connectionSettings2, IOptions<MailSettings> mailSettings, IHttpClientFactory _factory, ILogger<SettingsModel> logger, IOptions<FinnHub> finnHub, IOptions<Polygon> polycon, IOptions<SchwabSettings> settings) //: base(connectionSettings, connectionSettings2, mailSettings)
        {
            _logger = logger;
            _httpClient = _factory.CreateClient();
            _mgr = new Manager(connectionSettings?.Value?.Connection, "", null);
            _traderGoService = new TraderGoService(_logger, _httpClient, false, finnHub, polycon, _factory, settings);
        }

        public override IActionResult Execute()
        {
            RulesEngine = ctx.RulesEngine.FirstOrDefault();
            if (RulesEngine == null) { RulesEngine = new RulesEngine(); }
            return null;
        }
        public IActionResult OnPost()
        {
            if(RulesEngines != null)
            {
                RulesEngines.DateCreated = (RulesEngines.DateCreated == null || RulesEngines.DateCreated == DateTime.MinValue) ? DateTime.Now : RulesEngines.DateCreated;
                RulesEngines.DateUpdated = DateTime.Now;
                if (RulesEngines.Id == null || RulesEngines.Id <= 0) {
                    //RulesEngines.Id = 0;
                    ctx.RulesEngine.Add(RulesEngines);
                    ctx.Entry(RulesEngines).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                } else {
                    ctx.RulesEngine.Attach(RulesEngines);
                    ctx.Entry(RulesEngines).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                }
                ctx.SaveChanges();
            }
            return Redirect("~/Settings");
        }
    }
}
