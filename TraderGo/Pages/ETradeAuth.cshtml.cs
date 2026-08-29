using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Options;
using TinyOAuth2;
using Microsoft.EntityFrameworkCore;
using TMD.Services.Service;
using TMDInvestments.Context;
using TMDInvestments.Models;

namespace TraderGo.Pages
{
    public class ETradeAuthModel : PageModel
    {
        [BindProperty] public string? Message { get; set; }
        private readonly ILogger<IndexModel>? _logger;
        private readonly ETradeSettings? _settings;
        private readonly IHttpClientFactory? _factory;
        ETradeService? services;
        RequestTokenInfo tokenInfo;
        public ETradeAuthModel(ILogger<IndexModel> logger, IOptions<ETradeSettings> settings, IHttpClientFactory factory, IDbContextFactory<TmdContext> contextFactory)
        {
            _logger = logger;
            _settings = settings.Value;
            _factory = factory;
            services = new ETradeService(_logger, _settings, _factory, contextFactory);
        }
        public async Task<IActionResult> OnGet()
        {
            //bool results = false;
            //var code = Uri.UnescapeDataString(Request.Query["oauth_token"].ToString());            
            var results = services.GetRequestToken();
            HttpContext.Session.SetString("RequestToken", results.RequestToken);
            HttpContext.Session.SetString("RequestTokenSecret", results.RequestTokenSecret);
            return Redirect(results.AuthorizationUrl);
        }
    }
}
