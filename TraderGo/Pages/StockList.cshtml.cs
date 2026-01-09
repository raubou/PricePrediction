using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using System.Net.Http;
using TMD.Database.Connect.Managers;
using TMD.Services.Service;
using TMDInvestments.Models;

namespace TraderGo.Pages
{
    public class StockListModel : PageModelBase
    {
        private readonly ILogger<StockListModel> _logger;
        TraderGoService _traderGoService;
        //HttpClient _httpClient;
        Manager _mgr;
        [BindProperty] public List<StockList>? StockLists { get; set; }
        [BindProperty] public StockList? StockList { get; set; } = new StockList();
        [BindProperty] public int Total {  get; set; }
        public StockListModel(IOptions<ConnectionSettings> connectionSettings, IOptions<ConnectionSettings2> connectionSettings2, IOptions<MailSettings> mailSettings, IHttpClientFactory _factory, ILogger<StockListModel> logger) //: base(connectionSettings, connectionSettings2, mailSettings)
        {
            _logger = logger;
            //_httpClient = _factory.CreateClient();
            _mgr = new Manager(connectionSettings?.Value?.Connection, "", null);
            _traderGoService = new TraderGoService(null, _logger, _factory, false);
        }
        public override IActionResult Execute()
        {
            StockLists = ctx.StockList.OrderBy(x => x.Symbol).ToList();
            Total = StockLists.Count();
            return null;
        }

        public IActionResult OnPost(StockList stockList)
        {
            if (stockList != null) {
                stockList.DateUpdated = DateTime.Now;
                ctx.StockList.Attach(stockList);
                ctx.Entry(stockList).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                ctx.SaveChanges();
            }
            StockLists = ctx.StockList.ToList();
            Total = StockLists.Count();
            return Redirect("~/StockList");
        }
    }
}
