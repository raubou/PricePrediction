using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Text.Json.Nodes;
using TMD.Database.Connect.Managers;
using TMD.Services.Service;
using TMDInvestments.Models;
using TMDInvestments.Services;
using TMDInvestments.Context;
using Microsoft.EntityFrameworkCore;

namespace TraderGo.Pages
{
    public class StockAnalysisModel : PageModelBase
    {
        [BindProperty]
        public string Search {  get; set; } = string.Empty;
        public JsonObject[] results { get; set; } = new JsonObject[0];
        private readonly ILogger<SettingsModel> _logger;
        SchwabServices _service;
        SchwabSettings _settings;
        HttpClient _httpClient;
        [BindProperty] public int Total { get; set; }
        public StockAnalysisModel(IOptions<ConnectionSettings> connectionSettings, IOptions<ConnectionSettings2> connectionSettings2, IOptions<MailSettings> mailSettings, IHttpClientFactory _factory, ILogger<SettingsModel> logger, IOptions<FinnHub> finnHub, IOptions<Polygon> polycon, IOptions<SchwabSettings> settings, IDbContextFactory<TmdContext> contextFactory) //: base(connectionSettings, connectionSettings2, mailSettings)
        {
            _logger = logger;
            //_httpClient = _factory.CreateClient();
            //_mgr = new Manager(connectionSettings?.Value?.Connection, "", null);
            _settings = settings.Value;
            _service = new SchwabServices(_logger, _settings, _factory, contextFactory);
        }

        public override IActionResult Execute()
        {
            return Page();
        }
        public IActionResult OnPost() {
            if(string.IsNullOrEmpty(Search))
                return Page();
            string[] quotes = Search.Split(',');
            JsonObject[] quote = _service.GetQuotes(string.Join(",", quotes), true, false);
            quote = quote.Where(item => item != null).ToArray();
            Total = quote.Count();
            results = quote;
            //foreach (var item in quote)
            //{
            //    //var quoteData = ((JsonElement)quote).GetProperty(item.ToUpper());
            //    var quoteData = item["RSI"];
            //}
            return Page();
        }
    }
}
