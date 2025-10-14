using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using TMDInvestments.Models;

namespace TraderGo.Pages
{
    public class ContactUsModel : PageModelBase
    {
        //private readonly ILogger<PageModelBase> _logger;
        public ContactUsModel(IOptions<ConnectionSettings> connectionSettings, IOptions<ConnectionSettings2> connectionSettings2, IOptions<MailSettings> mailSettings) //: base(connectionSettings, connectionSettings2, mailSettings)
        {
            //_logger = logger;
        }

        public override IActionResult Execute()
        {
            return null;
        }
    }
}
