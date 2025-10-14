using Coinbase.AdvancedTrade.Models.Public;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Text.Json;
using TMD.Services;
using TMDInvestments.Models;

namespace TraderGo.Pages
{
    public class ArbitrageModel : PageModel
    {
        [BindProperty] public List<PublicProduct>? Products { get; set; }

        private readonly ILogger<IndexModel>? _logger;
        private readonly CoinBaseSettings? _settings;
        private readonly IHttpClientFactory? _factory;
        CoinBaseService services = null;
        public ArbitrageModel(ILogger<IndexModel> logger, IOptions<CoinBaseSettings> settings, IHttpClientFactory factory)
        {
            _logger = logger;
            _settings = settings.Value;
            _factory = factory;
            services = new CoinBaseService(_logger, _settings, _factory);
        }
        public void OnGet()
        {
            var results = services.GetProducts();
            var disCoinList = results.Select(x => x.DisplayName.Substring(0, x.DisplayName.IndexOf("-"))).Distinct();
            foreach (var disCoin in disCoinList) {
                var coins = results.Where(x => x.DisplayName.StartsWith(disCoin));
            }
        }
    }
}
