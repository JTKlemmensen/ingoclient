using System;
using System.Collections.Generic;
using System.Text;

namespace IngoClient.Models
{
    public class User
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string DeviceName { get; set; }
        public string DeviceToken { get; set; }

        public string AccessToken { get; set; }
        public DateTime LastTimeTokenGenerated { get; set; }
        public int ExpiresIn { get; set; }
        public string RefreshToken { get; set; }

        public string GameToken { get; set; }
    }
}