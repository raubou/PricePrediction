using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json.Nodes;
using TMD.PricePrediction;
using TMD.Services;
using TMDInvestments.Context;
using TMDInvestments.Models;
using TMDInvestments.Services;

namespace TraderGo.Pages
{
    public class ArbitrageCoinModel : PageModel
    {
        [BindProperty] public List<JsonObject>? movers { get; set; }

        private readonly ILogger<IndexModel>? _logger;
        private readonly CoinBaseSettings? _settings;
        private readonly IHttpClientFactory? _factory;
        CoinBaseService coinBaseService = null;
        Helper? _helper = null;
        public ArbitrageCoinModel(ILogger<IndexModel> logger, IOptions<CoinBaseSettings> settings, IHttpClientFactory factory, IDbContextFactory<TmdContext> contextFactory)
        {
            _logger = logger;
            _settings = settings.Value;
            _factory = factory;
            _helper = new Helper(_logger, null, null, null, contextFactory);
            //coinBaseService = new CoinBaseService(_logger, _settings, _factory, _helper, contextFactory);
            coinBaseService = new CoinBaseService(_logger, _settings, _helper, _factory, contextFactory);
        }
        public async Task OnGet()
        {
            movers = coinBaseService.GetArbitrage();
            
        }
    }
}
