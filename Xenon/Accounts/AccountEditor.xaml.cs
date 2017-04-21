using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Xenon.Accounts
{
    /// <summary>
    /// Interaction logic for AccountEditor.xaml
    /// </summary>
    public partial class AccountEditor : Window
    {
        public Account EditedAccount { get; set; }

        public AccountEditor(Account account = null)
        {
            InitializeComponent();

            if (account != null)
            {
                usernameTextBox.Text = account.Username;
                if (account.FriendlyName != null)
                    friendlyNameTextBox.Text = account.FriendlyName;

                passwordPasswordBox.Focus();
            }
            else
            {
                usernameTextBox.Focus();
            }

            EditedAccount = null;
        }

        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            string username = usernameTextBox.Text;
            string password = passwordPasswordBox.Password;

            if (String.IsNullOrEmpty(username) || String.IsNullOrEmpty(password))
            {
                MessageBox.Show("Username or Password cannot be empty!", "Xenon");
                return;
            }

            string friendlyName =
                String.IsNullOrEmpty(friendlyNameTextBox.Text)
                ? null
                : friendlyNameTextBox.Text;

            EditedAccount = new Account(username, password, friendlyName);
            Close();
        }
    }
}
