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
        private string _token;
        private string _friendlyName = null;

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
        public string Token
        {
            get { return _token; }
            set
            {
                _token = value;
                NotifyPropertyChanged();
            }
        }

        [JsonProperty("friendly_name", NullValueHandling = NullValueHandling.Ignore)]
        public string FriendlyName
        {
            get { return _friendlyName; }
            set
            {
                _friendlyName = value;
                NotifyPropertyChanged();
            }
        }

        // used for WPF display
        [JsonIgnore]
        public string DisplayName
            => (_friendlyName != null) ? _friendlyName : _username;

        public Account() { }
        public Account(string username, string passwordOrToken, string friendlyName = null, bool isToken = false)
        {
            this.Username = username;
            this.Token =
                isToken
                    ? passwordOrToken
                    : Nexon.Auth.HashHexPassword(passwordOrToken);

            this.FriendlyName = friendlyName;
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
