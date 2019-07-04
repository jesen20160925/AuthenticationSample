using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Authentication.Infrastructure
{
    public class TokenContext
    {
        public string _securityKey; //秘钥
        public string _issuer; //发布者
        public string _audience; //受众

        private readonly ILogger<TokenContext> _logger;

        public TokenContext(IConfiguration configuration,ILogger<TokenContext> logger)
        {
            _securityKey = configuration["Jwt:Secret"];
            _issuer = configuration["Jwt:Issuer"];
            _audience = configuration["Jwt:Audience"];
            _logger = logger;
        }

        #region 自定义创建Jwt
        /// <summary>
        /// 自定义创建Json Web Token
        /// </summary>
        /// <param name="payLoad">Jwt 的payload部分</param>
        /// <param name="expiresMinute">有效分钟</param>
        /// <param name="header">Jwt 的header部分</param>
        /// <returns></returns>
        //public string GetToken(Dictionary<string, object> payLoad, int expiresMinute, Dictionary<string, object> header = null)
        //{
        //    if (header == null)
        //    {
        //        header = new Dictionary<string, object>(new List<KeyValuePair<string, object>>() {
        //            new KeyValuePair<string, object>("alg", "HS256"),
        //            new KeyValuePair<string, object>("typ", "JWT")
        //        });
        //    }
        //    //添加jwt可用时间
        //    var now = DateTime.UtcNow;
        //    payLoad["nbf"] = ToUnixEpochDate(now);//可用时间起始
        //    payLoad["exp"] = ToUnixEpochDate(now.Add(TimeSpan.FromMinutes(expiresMinute)));//可用时间结束

        //    var encodedHeader = Base64UrlEncoder.Encode(JsonConvert.SerializeObject(header));
        //    var encodedPayload = Base64UrlEncoder.Encode(JsonConvert.SerializeObject(payLoad));

        //    var hs256 = new HMACSHA256(Encoding.ASCII.GetBytes(_securityKey));
        //    var encodedSignature = Base64UrlEncoder.Encode(hs256.ComputeHash(Encoding.UTF8.GetBytes(string.Concat(encodedHeader, ".", encodedPayload))));

        //    var encodedJwt = string.Concat(encodedHeader, ".", encodedPayload, ".", encodedSignature);
        //    return encodedJwt;
        //} 
        #endregion

        #region 微软Jwt库创建
        /// <summary>
        /// 创建jwtToken,采用微软内部方法，默认使用HS256加密
        /// </summary>
        /// <param name="payLoad">Jwt 的payload部分</param>
        /// <param name="expiresMinute">有效分钟</param>
        /// <returns></returns>
        public string GetToken(Dictionary<string, object> payLoad, int expiresMinute)
        {
            var now = DateTime.UtcNow;

            // Specifically add the jti (random nonce), iat (issued timestamp), and sub (subject/user) claims.
            // You can add other claims here, if you want:
            var claims = new List<Claim>();
            foreach (var key in payLoad.Keys)
            {
                var tempClaim = new Claim(key, payLoad[key]?.ToString());
                claims.Add(tempClaim);
            }

            // Create the JWT and write it to a string
            var jwtSecurityToken = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                notBefore: now,
                expires: now.Add(TimeSpan.FromMinutes(expiresMinute)),
                signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_securityKey)), SecurityAlgorithms.HmacSha256));
            var accessToken = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);
            return accessToken;
        }

        #endregion

        #region 验证Token有效性
        /// <summary>
        /// 验证身份 验证签名的有效性,
        /// </summary>
        /// <param name="encodeJwt"></param>
        /// <param name="validatePayLoad">自定义各类验证； 是否包含那种申明，或者申明的值， </param>
        /// <returns></returns>
        public bool ValidateToken(string accessToken)
        {
            try
            {
                string[] tokenSplitArray = accessToken.Split('.');
                var header = JsonConvert.DeserializeObject<Dictionary<string, object>>(Base64UrlEncoder.Decode(tokenSplitArray[0]));
                var payLoad = JsonConvert.DeserializeObject<Dictionary<string, object>>(Base64UrlEncoder.Decode(tokenSplitArray[1]));
                var hs256 = new HMACSHA256(Encoding.ASCII.GetBytes(_securityKey));

                //验证签名是否正确
                if (!tokenSplitArray[2].Equals(Base64UrlEncoder.Encode(hs256.ComputeHash(Encoding.UTF8.GetBytes(string.Concat(tokenSplitArray[0], ".", tokenSplitArray[1]))))))
                {
                    return false;
                }

                //验证是否在有效期内
                long totalSeconds = ToUnixEpochDate(DateTime.UtcNow);
                if (!(totalSeconds >= long.Parse(payLoad["nbf"].ToString()) && totalSeconds < long.Parse(payLoad["exp"].ToString())))
                {
                    return false;
                }

                //验证发布者和受众是否一致，如果是在实际环境中，sso有多个系统需要登录，那么可以给每个系统分配一个audience，维护一个列表来校验
                if (!(payLoad["iss"].Equals(_issuer) && payLoad["aud"].Equals(_audience)))
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex.Message);
                return false;
            }
        }

        #endregion

        #region 获取Jwt中的有效信息payLoad
        /// <summary>
        /// 获取Jwt中的payLoad
        /// </summary>
        /// <param name="encodeJwt"></param>
        /// <returns></returns>
        public Dictionary<string, object> GetPayLoad(string encodeJwt)
        {
            var jwtArr = encodeJwt.Split('.');
            var payLoad = JsonConvert.DeserializeObject<Dictionary<string, object>>(Base64UrlEncoder.Decode(jwtArr[1]));
            return payLoad;
        }
        #endregion

        #region 将时间转换成1970到现在的秒数
        private long ToUnixEpochDate(DateTime date) =>
           (long)Math.Round((date.ToUniversalTime() - new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero)).TotalSeconds); 
        #endregion

    }
}

/*
    所谓的单点登录，就是用户访问其他系统的时候，检测到没有登录时于是请求认证中心，登录获取token返回，
    将其写入到缓存或者用户的localstorage，之后用户每次访问均需携带token请求，然后校验其token的有效性
*/