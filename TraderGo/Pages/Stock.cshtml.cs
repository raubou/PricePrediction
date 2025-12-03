using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TraderGo.Pages
{
    [Authorize]
    public class StockModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
