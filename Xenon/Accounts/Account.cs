using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Xenon.Accounts
{
    public class Account : INotifyPropertyChanged
    {
        private string _username;
        private string _password;

        [JsonProperty("username")]
        public string Username
        {
            get { return _username; }
            set
            {
                _username = value;
                NotifyPropertyChanged();
            }
        }

        [JsonProperty("token")]
        public string Password
        {
            get { return _password; }
            set
            {
                _password = value;
                NotifyPropertyChanged();
            }
        }

        public Account(string username, string password, bool passwordAlreadyHashed = false)
        {
            this.Username = username;
            this.Password =
                passwordAlreadyHashed
                    ? password
                    : Nexon.Auth.HashHexPassword(password);
        }

        // notifyproperty stuff - https://msdn.microsoft.com/en-us/library/ms229614(v=vs.110).aspx
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
