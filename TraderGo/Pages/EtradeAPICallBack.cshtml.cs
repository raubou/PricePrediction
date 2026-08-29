//using MD.Database.Connect.Helper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using TinyOAuth2;
using TMD.Services;
using TMD.Services.Service;
using TMDInvestments.Context;
using TMDInvestments.Models;
using Microsoft.EntityFrameworkCore;

namespace TraderGo.Pages
{
    public class EtradeAPICallBackModel : PageModel
    {
        [BindProperty] public string? Message { get; set; }
        private readonly ILogger<IndexModel>? _logger;
        private readonly ETradeSettings? _settings;
        private readonly IHttpClientFactory? _factory;
        ETradeService? services;
        RequestTokenInfo tokenInfo;
        TmdContext ctx;
        AuthenicationHelper authenicationHelper = null;
        public EtradeAPICallBackModel(ILogger<IndexModel> logger, IOptions<ETradeSettings> settings, IHttpClientFactory factory, IDbContextFactory<TmdContext> contextFactory)
        {
            _logger = logger;
            _settings = settings.Value;
            _factory = factory;
            ctx = new TmdContext();
            authenicationHelper = new AuthenicationHelper();
            services = new ETradeService(_logger, _settings, _factory, contextFactory);
        }
        public async Task<IActionResult> OnGet()
        {
            //bool results = false;
            var code = Uri.UnescapeDataString(Request.Query["code"].ToString());
            if (string.IsNullOrEmpty(code)) {
                Message = "Error Retrieving token";
                return Page();
            }
            var RequestToken = HttpContext.Session.GetString("RequestToken");
            var RequestTokenSecret = HttpContext.Session.GetString("RequestTokenSecret");
            tokenInfo = new RequestTokenInfo() { RequestToken = Uri.UnescapeDataString(RequestToken),RequestTokenSecret = Uri.UnescapeDataString(RequestTokenSecret) };
            var accessToken = services.GetAuthorizationToken(tokenInfo, code).Result;
            if (accessToken.AccessToken != null)
            {
                //Token token = ctx.Token.Where(x => x.TokenType == TokenType.ETrade).FirstOrDefault();
                //token = (token == null) ? new Token() : token;
                //Microsoft.EntityFrameworkCore.EntityState state = (token.Id == null) ? Microsoft.EntityFrameworkCore.EntityState.Added : Microsoft.EntityFrameworkCore.EntityState.Modified;
                //authenicationHelper.AddToken(accessToken, state, TokenType.ETrade, ref token);
                authenicationHelper.AddToken(accessToken, TokenType.ETrade);
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
