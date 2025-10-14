using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using TMD.Database.Connect.Managers;
using TMDInvestments.Models;

namespace TraderGo.Pages
{
    public class TermsModel : PageModelBase
    {
        //private readonly ILogger<PageModelBase> _logger;
        Manager _mgr;
        [BindProperty]
        public List<dynamic>? IPOs { get; set; }
        public TermsModel(IOptions<ConnectionSettings> connectionSettings, IOptions<ConnectionSettings2> connectionSettings2, IOptions<MailSettings> mailSettings) //: base(connectionSettings, connectionSettings2, mailSettings)
        {
            //_logger = logger;
           // _mgr = new Manager(connectionSettings?.Value?.Connection, "", _logger);
        }

        public override IActionResult Execute()
        {
            //IPOs = _mgr.GetIPOs();
            return null;
        }
    }
}
