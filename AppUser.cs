using System;
using System.Collections.Generic;
using System.Globalization;

namespace Iotbcdg.Model
{
    public class AppUser
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public List<string> Devices { get; set; } = new List<string>();
        public string Password { get; set; }
        public string Salt { get; set; }

        public AppUser(string Id, string Email, List<string> Devices, string Password, string Salt)
        {
            this.Id = Id;
            this.Email = Email;
            this.Devices = new List<string>(Devices);
            this.Password = Password;
            this.Salt = Salt;
        }
    }
}