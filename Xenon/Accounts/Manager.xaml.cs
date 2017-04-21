using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// Interaction logic for Manager.xaml
    /// </summary>
    public partial class Manager : Window
    {
        private ObservableCollection<Account> accounts;

        private MainWindow _mw;
        private bool loaded = false;

        public Manager(MainWindow mw)
        {
            InitializeComponent();

            _mw = mw;

            List<Account> _accounts = JsonConvert.DeserializeObject<List<Account>>(Properties.Settings.Default.savedAccounts);
            accounts = new ObservableCollection<Account>(_accounts);
            listBox.ItemsSource = accounts;

            checkTutorialLabel();
            startupCheckBox.IsChecked = Properties.Settings.Default.showAccountManager;
            loaded = true;
        }

        private void startupCheckBox_Toggled(object sender, RoutedEventArgs e)
        {
            if (!loaded)
                return;

            Properties.Settings.Default.showAccountManager = (bool)startupCheckBox.IsChecked;
            Properties.Settings.Default.Save();
        }

        private void checkTutorialLabel()
        {
            tutorialLabel.Visibility =
                accounts.Count > 0
                    ? Visibility.Hidden
                    : Visibility.Visible;
        }

        private void addButton_Click(object sender, RoutedEventArgs e)
        {
            var editor = new AccountEditor();
            editor.ShowDialog();

            if (editor.EditedAccount != null)
            {
                accounts.Add(editor.EditedAccount);
                saveAccounts();
            }
        }

        private void removeButton_Click(object sender, RoutedEventArgs e)
        {
            if (listBox.SelectedItem == null)
            {
                MessageBox.Show("Please select an account!", "Xenon");
                return;
            }

            var account = (Account)listBox.SelectedItem;
            MessageBoxResult mbr = MessageBox.Show($"Are you sure you wish to remove the account '{account.Username}'?", "Xenon", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (mbr == MessageBoxResult.Yes)
            {
                accounts.Remove(account);
                saveAccounts();
            }
        }

        private void editButton_Click(object sender, RoutedEventArgs e)
        {
            if (listBox.SelectedItem == null)
            {
                MessageBox.Show("Please select an account!", "Xenon");
                return;
            }

            var account = accounts.FirstOrDefault(a => a == (Account)listBox.SelectedItem);
            var editor = new AccountEditor(account);
            editor.ShowDialog();

            if (editor.EditedAccount != null)
            {
                account.Username = editor.EditedAccount.Username;
                account.Password = editor.EditedAccount.Password;

                saveAccounts();
            }
        }

        private void saveAccounts()
        {
            Properties.Settings.Default.savedAccounts = JsonConvert.SerializeObject(accounts.ToList());
            Properties.Settings.Default.Save();

            checkTutorialLabel();
        }

        private void goButton_Click(object sender, RoutedEventArgs e)
        {
            if (listBox.SelectedItem == null)
            {
                MessageBox.Show("Please select an account!", "Xenon");
                return;
            }

            var account = (Account)listBox.SelectedItem;

            _mw.usernameTextBox.Text = account.Username;
            _mw.PerformLogin(account);
            Close();
        }
    }
}
