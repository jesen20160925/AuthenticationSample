using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AuthenticationCenter.Models;
using Microsoft.Extensions.Configuration;
using Authentication.Infrastructure;
using Authentication.Infrastructure.Cache;
using Microsoft.Extensions.Logging;

namespace AuthenticationCenter.Controllers
{
    public class AuthenticationController : Controller
    {
        private readonly IConfiguration _configuration;

        private readonly ICacheContext _cache;

        private readonly TokenContext _tokenContext;

        private readonly ILogger<AuthenticationController> _logger;

        public AuthenticationController(IConfiguration configuration,ICacheContext cache,TokenContext tokenContext,ILogger<AuthenticationController> logger)
        {
            _configuration = configuration;
            _cache = cache;
            _tokenContext = tokenContext;
            _logger = logger;
        }

        public IActionResult GetToken(string username, string password)
        {
            string accessToken = _cache.Get<string>(username);
            if (!(accessToken?.Length > 0))
            { 
                UserModel user = UserMock.FindUser(username, password);

                Dictionary<string, object> keyValuePairs = new Dictionary<string, object>();
                keyValuePairs.Add(nameof(user.Id), user.Id);
                keyValuePairs.Add(nameof(user.UserName), user.UserName);
                keyValuePairs.Add(nameof(user.Phone), user.Phone);
                keyValuePairs.Add(nameof(user.Email), user.Email);

                accessToken = _tokenContext.GetToken(keyValuePairs, 120);

                _cache.Set(user.UserName, accessToken, DateTime.Now.AddHours(2));
            }
            return Json(new { access_token =  accessToken });
        }

        public IActionResult ValidateToken(string accessToken)
        {
            if (_tokenContext.ValidateToken(accessToken))
            {
                return Json(new {
                    Code = 1,
                    Msg = "token is valid"
                });
            }
            else
            {
                return Json(new
                {
                    Code = 0,
                    Msg = "token is invalid"
                });
            }
        }
    }
}
