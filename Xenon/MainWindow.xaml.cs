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
using System.Windows.Navigation;

namespace Xenon
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            checkIfMapleExists();

            if (!String.IsNullOrEmpty(Properties.Settings.Default.username))
            {
                usernameTextBox.Text = Properties.Settings.Default.username;
                rememberUsernameCheckbox.IsChecked = true;
                passwordPasswordBox.Focus();
            }
            else
            {
                usernameTextBox.Focus();
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (Properties.Settings.Default.showAccountManager)
                (new Accounts.Manager(this)).ShowDialog();
        }

        private void checkIfMapleExists()
        {
            if (!Nexon.Maple.GameExists())
            {
                MessageBox.Show("MapleStory.exe was not found. Are you sure you put Xenon into the MapleStory folder? Ensure it is NOT in the \"appdata\" folder that Nexon Launcher creates, but one step above that (by default it should be called \"maplestory\".", "Xenon", MessageBoxButton.OK, MessageBoxImage.Warning);
                Close();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
            => PerformLogin();

        public async void PerformLogin(Accounts.Account _account = null)
        {
            usernameTextBox.IsEnabled = false;
            passwordPasswordBox.IsEnabled = false;
            playButton.IsEnabled = false;

            Accounts.Account account =
                (_account != null)
                    ? _account
                    : new Accounts.Account(usernameTextBox.Text, passwordPasswordBox.Password);

            try
            {
                statusLabel.Content = $"attempting login...";

                await Nexon.Auth.Login(account);
                statusLabel.Content = $"logged in as {account.Username} - checking for updates...";

                await Nexon.Maple.CheckMapleUpToDate();
                statusLabel.Content = $"maple seems to be up to date - getting launch data...";

                await Nexon.Maple.GetLaunchData();
                statusLabel.Content = $"got maple launch data - launching game!...";

                Nexon.Maple.launchGame();
                statusLabel.Content = $"game launched! - thanks for using xenon!";

                if ((bool)rememberUsernameCheckbox.IsChecked)
                    Properties.Settings.Default.username = account.Username;
                else
                    Properties.Settings.Default.username = "";

                Properties.Settings.Default.Save();

                await Task.Delay(1500);
                Close();
            }
            catch (Nexon.NexonError ex) { MessageBox.Show($"Nexon error: \n\n{ex.Message}", "Xenon", MessageBoxButton.OK, MessageBoxImage.Exclamation); }
            catch (SilentException) { /* meh */ }
#if !DEBUG
            catch (Exception ex) { MessageBox.Show($"An error occurred...\n\n{ex.ToString()}", "Xenon", MessageBoxButton.OK, MessageBoxImage.Exclamation); }
#endif
            finally
            {
                statusLabel.Content = "";
                usernameTextBox.IsEnabled = true;
                passwordPasswordBox.IsEnabled = true;
                playButton.IsEnabled = true;
            }
        }

        private void accountsManagerButton_Click(object sender, RoutedEventArgs e)
            => (new Accounts.Manager(this)).ShowDialog();
    }
}
