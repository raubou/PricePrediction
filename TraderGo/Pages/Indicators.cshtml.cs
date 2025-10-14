using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using TMD.Database.Connect.Managers;
using TMDInvestments.Models;

namespace TraderGo.Pages
{
    public class IndicatorsModel : PageModelBase
    {
        //private readonly ILogger<PageModelBase> _logger;
        Manager _mgr;
        public IndicatorsModel(IOptions<ConnectionSettings> connectionSettings, IOptions<ConnectionSettings2> connectionSettings2, IOptions<MailSettings> mailSettings) //: base(connectionSettings, connectionSettings2, mailSettings)
        {
            //_logger = logger;
            _mgr = new Manager(connectionSettings?.Value?.Connection, "", null);
        }

        public override IActionResult Execute()
        {
            return null;
        }
    }
}
