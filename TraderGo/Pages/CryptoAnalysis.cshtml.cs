using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using TMD.Database.Connect.Managers;
using TMD.Services;
using TMD.Services.Service;
using TMDInvestments.Models;
using Microsoft.EntityFrameworkCore;
using TMDInvestments.Context;

namespace TraderGo.Pages
{
    public class CryptoAnalysisModel : PageModelBase
    {
        private readonly ILogger<AnalysisModel> _logger;
        TraderGoService _traderGoService;
        //HttpClient _httpClient;
        CoinBaseSettings _settings;
        IHttpClientFactory _factory;
        Manager _mgr;
        CoinBaseService services;
        [BindProperty] public List<CryptoList>? CryptoLists { get; set; }
        [BindProperty] public CryptoList? CryptoList { get; set; } = new CryptoList();
        [BindProperty] public List<dynamic>? Analysis { get; set; } = new List<dynamic>();
        [BindProperty] public int Total { get; set; }
        public CryptoAnalysisModel(IOptions<ConnectionSettings> connectionSettings, IOptions<ConnectionSettings2> connectionSettings2, IOptions<MailSettings> mailSettings, IHttpClientFactory factory, ILogger<AnalysisModel> logger, IOptions<CoinBaseSettings> settings, IDbContextFactory<TmdContext> contextFactory) //: base(connectionSettings, connectionSettings2, mailSettings)
        {
            _logger = logger;
            _settings = settings.Value;
            _factory = factory;
            //_httpClient = _factory.CreateClient();
            _mgr = new Manager(connectionSettings?.Value?.Connection, "", null);
            _traderGoService = new TraderGoService(null, _logger, _factory, false, contextFactory);
            services = new CoinBaseService(_logger, _settings, _factory/*, new Helper(ctx)*/);
        }
        public override IActionResult Execute()
        {
            CryptoLists = ctx.CryptoList.ToList();
            Total = CryptoLists.Count();
            foreach (var item in CryptoLists)
            {
                var results = services.GetQuoteAsync(item.Coin).Result;
                Analysis.Add(results);
            }
            return null;
        }
    }
}
