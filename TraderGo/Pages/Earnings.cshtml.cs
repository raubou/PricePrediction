using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Options;
using System.Data;
using System.Data.Entity;
using System.Text.Json;
using System.Text.Json.Nodes;
using TMD.Database.Connect.Managers;
using TMD.Services.Service;
using TMDInvestments.Models;
using Microsoft.EntityFrameworkCore;
using TMDInvestments.Context;

namespace TraderGo.Pages
{
    public class EarningsModel : PageModelBase
    {
        private readonly ILogger<EarningsModel>? _logger;
        TraderGoService? _traderGoService;
        HttpClient? _httpClient;
        Manager? _mgr;
        IDbContextFactory<TmdContext> _contextFactory;
        [BindProperty]
        public List<DataRow>? Earnings { get; set; }
        [BindProperty]
        public int? Total { get; set; }    
        //[BindProperty]
        //public SchwabServices? schwabServices { get; set; }
        [BindProperty]
        public List<StockList>? stockList { get; set; }
        SchwabSettings? _settings;
        private readonly IHttpClientFactory? _factory;
        [BindProperty]
        public int? rowCnt { get; set; } = 1;
        [BindProperty]
        public string? boldStyle { get; set; } = string.Empty;
        //[BindProperty]
        //public JsonObject? quote { get; set; } = null;
        public EarningsModel(IOptions<ConnectionSettings>? connectionSettings, IOptions<ConnectionSettings2>? connectionSettings2, IOptions<MailSettings>? mailSettings, IHttpClientFactory? _factory, ILogger<EarningsModel>? logger, IOptions<FinnHub>? finnHub, IOptions<Polygon>? polycon, IOptions<SchwabSettings>? settings, IDbContextFactory<TmdContext> contextFactory) //: base(connectionSettings, connectionSettings2, mailSettings)
        {
            _logger = logger;
            _httpClient = _factory?.CreateClient();
            _mgr = new Manager(connectionSettings?.Value?.Connection, "", null);
            _traderGoService = new TraderGoService(_logger, _httpClient, false,finnHub,polycon,_factory, settings, contextFactory);
            _settings = settings?.Value;
            _contextFactory = contextFactory;
            //_factory = _factory;
            //schwabServices = new SchwabServices(_logger, _settings, _factory);
        }

        public override IActionResult Execute()
        {
            int total = 0;
            Earnings = _mgr?.GetEarnings(ref total).ToList();//.ToListAsync();//.OrderBy(x => x.date)/*.ThenBy(y => y.name).ToList()*/;
            Total = total;
            //Total = Earnings.GetEnumerator();

            stockList = ctx?.StockList.ToList();
            return null;
        }

        public async Task<IActionResult> OnPost()
        {
            await _traderGoService?.LoadEarnings();
            Execute();
            return Page();
        }
    }
}
