using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CookieAuthenticationSample.Models;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace CookieAuthenticationSample.Controllers
{
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    public class HomeController : Controller
	{
		public IActionResult Index()
		{
            string name = base.User?.Identity?.Name;

			return View();
		}

		public IActionResult Privacy()
		{
			return View();
		}

		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}

        [AllowAnonymous]
		[HttpGet]
		public IActionResult Login(string ReturnUrl = "")
		{
            ViewBag.ReturnUrl = ReturnUrl;
			return View();
		}

        [AllowAnonymous]
        [HttpPost]
		public IActionResult Login(string username,string password, string ReturnUrl = "")
		{
            CurrentUser user = UserMock.FindUser(username, password);//这里写自己的认证逻辑

            var claimIdentity = new ClaimsIdentity("Cookie");
            claimIdentity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
            claimIdentity.AddClaim(new Claim(ClaimTypes.Name, user.Name));
            claimIdentity.AddClaim(new Claim(ClaimTypes.Email, user.Email));
            claimIdentity.AddClaim(new Claim(ClaimTypes.Role, user.Role));
            var claimsPrincipal = new ClaimsPrincipal(claimIdentity);
            // 在Startup注册AddAuthentication时，指定了默认的Scheme，在这里便可以不再指定Scheme。
            base.HttpContext.SignInAsync(claimsPrincipal).Wait(); //SignInAsync 登入

            if (!string.IsNullOrEmpty(ReturnUrl)) return Redirect(ReturnUrl);
            return Redirect("~/Home/Index");
		}

        public ActionResult Logout()
        {
            base.HttpContext.SignOutAsync().Wait(); // SignOutAsync 注销
            return this.Redirect("~/Home/Login");
        }
    }
}
