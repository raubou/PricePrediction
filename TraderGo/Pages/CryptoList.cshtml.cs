using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using TMD.Database.Connect.Managers;
using TMD.Services.Service;
using TMDInvestments.Models;

namespace TraderGo.Pages
{
    public class CryptoListModel : PageModelBase
    {
        private readonly ILogger<StockListModel> _logger;
        TraderGoService _traderGoService;
        HttpClient _httpClient;
        Manager _mgr;
        [BindProperty] public List<CryptoList>? CryptoLists { get; set; }
        [BindProperty] public CryptoList? CryptoList { get; set; } = new CryptoList();
        [BindProperty] public int Total { get; set; }
        public CryptoListModel(IOptions<ConnectionSettings> connectionSettings, IOptions<ConnectionSettings2> connectionSettings2, IOptions<MailSettings> mailSettings, IHttpClientFactory _factory, ILogger<StockListModel> logger) //: base(connectionSettings, connectionSettings2, mailSettings)
        {
            _logger = logger;
            _httpClient = _factory.CreateClient();
            _mgr = new Manager(connectionSettings?.Value?.Connection, "", null);
            _traderGoService = new TraderGoService(_logger, _httpClient, false);
        }
        public override IActionResult Execute()
        {
            CryptoLists = ctx.CryptoList.OrderBy(x => x.Coin).ToList();
            Total = CryptoLists.Count();
            return null;
        }

        public IActionResult OnPost(CryptoList CryptoList)
        {
            if (CryptoList != null)
            {
                CryptoList.DateUpdated = DateTime.Now;
                ctx.CryptoList.Attach(CryptoList);
                ctx.Entry(CryptoList).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                ctx.SaveChanges();
            }
            CryptoLists = ctx.CryptoList.ToList();
            Total = CryptoLists.Count();
            return Redirect("~/CryptoList");
        }
    }
}
