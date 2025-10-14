using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using TMDInvestments.Context;
using TMDInvestments.Models;

namespace TraderGo
{
    public abstract class PageModelBase : PageModel
    {
        protected IOptions<ConnectionSettings>? _connectionSettings;
        public TmdContext ctx = null;
        //public ILogger _log;
        [BindProperty]
        public LoginRegister? loginRegister { get; set; }
        public PageModelBase() {
            ctx = new TmdContext();
        }

        //public PageModelBase(IOptions<ConnectionSettings> connectionSettings, IOptions<ConnectionSettings2> connectionSettings2, IOptions<MailSettings> mailSettings)
        //{
        //    _connectionSettings = connectionSettings;
        //    //_log = logger;
        //    ctx = new TmdContext();
        //}
        public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
        {
            ViewData["_connectionSettings"] = _connectionSettings;
            //ViewData["_mongDBConnection"] = _mongDBConnection;
            ViewData["RouteData"] = RouteData;
            //ViewData["Logger"] = _log;
            base.OnPageHandlerExecuting(context);
        }
        public void OnGet()
        {
            //ViewData["RouteData"] = RouteData;
            string schema = Request.Scheme.ToString();
            string host = Request.Host.ToString();
            string path = Request.Path.ToString();
            if (host.IndexOf("localhost") == -1)
            {
                if (schema.ToLower() == "http")
                {
                    Response.Redirect("https://" + host + path);
                }
            }
            Execute();
        }
        //public virtual void Execute()
        //{

        //}

        public virtual IActionResult Execute()
        {
            return null;
        }
    }

}
