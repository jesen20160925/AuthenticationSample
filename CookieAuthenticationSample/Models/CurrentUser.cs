using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CookieAuthenticationSample.Models
{
    public class CurrentUser
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Account { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public DateTime LoginTime { get; set; }
    }

    public class UserMock
    {
        public static CurrentUser FindUser(string userName, string password)
        {
            return new CurrentUser()
            {
                Id = 123,
                Name = "admin",
                Account = "Administrator",
                Password = "123456",
                Email = "admin@qq.com",
                LoginTime = DateTime.Now,
                Role = userName.Equals("admin") ? "Admin" : "User"
            };
        }
    }
}
