using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using TMDInvestments.Models;
using TMD.Database.Connect.Managers;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace TraderGo.Pages
{
    public class LoginModel : PageModelBase
    {
        private string RoleName = string.Empty;
        private string UserId = string.Empty;
        private Manager mgr;
        private MailSettings mail;
        public LoginModel(IOptions<ConnectionSettings> connectionSettings, IOptions<ConnectionSettings2> connectionSettings2, IOptions<MailSettings> mailSettings) //: base(connectionSettings, connectionSettings2, mailSettings)
        {
            mgr = new Manager(connectionSettings.Value.Connection, "", null);
            mail = mailSettings.Value;
        }

        public override IActionResult Execute()
        {
            if (Request.Query["logoff"] == "true")
            {
                //Logout();
                HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme).Wait();
                return Redirect("/");
            }
            if (User.Identity.IsAuthenticated)
            {
                return Redirect("/");
            }
            return null;
        }

        public async Task<IActionResult> OnPostAsync(LoginRegister loginRegister)
        {
            var returnUrl = Request.Query["ReturnUrl"].ToString();

            if ((loginRegister?.register?.UserName != null && loginRegister?.register?.Password != null) && (loginRegister?.register?.UserName.Trim() != string.Empty && loginRegister?.register?.Password.Trim() != string.Empty))
            {
                if (!CheckEmail(loginRegister.register.UserName))
                {
                    ModelState.AddModelError("register.UserName", "Email is invalid.");
                    return Page();
                }
                if (!loginRegister.register.AgreetoTerms)
                {
                    ModelState.AddModelError("register.AgreetoTerms", "You must agree to terms.");
                    return Page();
                }

                if (!mgr.Register(loginRegister.register.UserName, loginRegister.register.Password) && SignIn(loginRegister.register.UserName, "Users").Result)
                {
                    ModelState.AddModelError("register.UserName", "Invalid register attempt.");
                    return Page();
                }
            }
            else if ((loginRegister?.login?.UserName != null && loginRegister?.login?.Password != null) && (loginRegister?.login?.UserName.Trim() != string.Empty && loginRegister?.login?.Password.Trim() != string.Empty))
            {
                if (!CheckEmail(loginRegister.login.UserName))
                {
                    ModelState.AddModelError("login.UserName", "Email is invalid.");
                    return Page();
                }
                if (!mgr.Authenticate(loginRegister.login.UserName, loginRegister.login.Password, ref RoleName, ref UserId))
                {
                    ModelState.AddModelError("login.UserName", "Invalid login attempt.");
                    return Page();
                }
                if (!SignIn(loginRegister.login.UserId.ToString(), RoleName).Result)
                {
                    ModelState.AddModelError("login.UserName", "Invalid login attempt.");
                    return Page();
                }
            }
            else if ((loginRegister?.recover?.UserName != null) && (loginRegister?.recover?.UserName.Trim() != string.Empty))
            {
                string password = string.Empty;
                if (!mgr.RecoverPassword(loginRegister?.recover?.UserName, ref password))
                {
                    ModelState.AddModelError("recover.UserName", "Invalid User Name.");
                    return Page();
                }
                else
                {
                    string body = "<div style='width:100%;height:200px;margin:0px auto;float:none;overflow:hidden;color:black;border:1px solid #cacaca;border-radius:3px;padding:5px;'><div style='width:100%;height:40px;margin:0px auto;float:none;overflow:hidden;color:white;background-color:#6699cc;padding:5px;'><img src='http://www.tradergo.com//images/stocklogo.png' height='30' />Trader Go</div><br>Here's your Password :" + password + "</div>";
                    if (!mgr.SendEmail("TraderGo", mail.UserName, mail.Password, loginRegister?.recover?.UserName, loginRegister?.recover?.UserName, "Password Recovery", body, mail.Server, mail.Port))
                    {
                        return Page();
                    }
                }

            }

            if (returnUrl.Trim() == string.Empty)
                return Redirect("~/");
            else
                return Redirect("~" + returnUrl);
        }

        private async Task<bool> SignIn(string userName, string RoleName)
        {
            bool success = true;
            try
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, userName),
                    new Claim(ClaimTypes.Role, RoleName),
                    new Claim(ClaimTypes.NameIdentifier, UserId),
                };

                var claimsIdentity = new ClaimsIdentity(
                    claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity));
            }
            catch (Exception ex)
            {
                success = false;
            }

            return success;
        }
        private bool CheckEmail(string userName)
        {
            bool isValid = true;
            if (!Regex.IsMatch(userName,
                @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250)))
            {
                isValid = false;
            }
            return isValid;
        }
    }
}
