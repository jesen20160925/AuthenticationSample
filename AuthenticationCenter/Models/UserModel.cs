using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthenticationCenter.Models
{
    public class UserModel
    {
        public int Id { get; set; }

        public string UserName { get; set; }

        public string Password { get; set; }

        public string Phone { get; set; }

        public string Email { get; set; }
    }

    public class UserMock
    {
        public static UserModel FindUser(string username,string password)
        {
            return new UserModel()
            {
                Id = 1,
                UserName = "Jesen",
                Password = "123456",
                Phone = "18888888888",
                Email = "18888888@qq.com"
            };
        }
    }
}
