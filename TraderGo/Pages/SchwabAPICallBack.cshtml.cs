using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using TMDInvestments.Services;
using TMDInvestments.Models;
using TMDInvestments.Context;

namespace TraderGo.Pages
{
    public class SchwabAPICallBackModel : PageModel
    {
        [BindProperty] public string? Message { get; set; }
        private readonly ILogger<IndexModel>? _logger;
        private readonly SchwabSettings? _settings;
        private readonly IHttpClientFactory? _factory;
        SchwabServices? services;
        //TmdContext? ctx = null;
        IDbContextFactory<TmdContext> _contextFactory;
        public SchwabAPICallBackModel(ILogger<IndexModel> logger, IOptions<SchwabSettings> settings, IHttpClientFactory factory, IDbContextFactory<TmdContext> contextFactory)
        {
            _logger = logger;
            _settings = settings.Value;
            _factory = factory;
            _contextFactory = contextFactory;
        }
        public async Task<IActionResult> OnGet()
        {
            bool results = false;
            var code = Uri.UnescapeDataString(Request.Query["code"].ToString());
            services = new SchwabServices(_logger, _settings, _factory, _contextFactory);
            services.FetchToken(code, ref results, false);
            if (results)
            {
                //System.Threading.Thread.Sleep(6000);
                //setMessages(error,message);
                return Redirect("./AccountInfo");
            }
            else
            {
                Message = "Error Retrieving token";
                return Page();
            }
        }
    }
}
