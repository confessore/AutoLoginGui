using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp
{
    public partial class MainWindow : Window
    {
        private const string WowPathFilePath = "wowpath.json";
        private const string AccountsFilePath = "accounts.json";

        public ObservableCollection<Account> Accounts { get; set; } = new ObservableCollection<Account>();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            LoadWowPath();
            LoadAccounts();

            WowPath.TextChanged += WowPath_TextChanged;
            Accounts.CollectionChanged += Accounts_CollectionChanged;
        }

        private int _launchDelayMs = 2500;

        private void DelaySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _launchDelayMs = (int)e.NewValue;
            DelayValueText.Text = _launchDelayMs.ToString();
        }


        private void LoadAccounts()
        {
            if (File.Exists(AccountsFilePath))
            {
                try
                {
                    string json = File.ReadAllText(AccountsFilePath);
                    var accounts = JsonSerializer.Deserialize<ObservableCollection<Account>>(json);
                    if (accounts != null)
                    {
                        foreach (var account in accounts)
                        {
                            Accounts.Add(account);
                        }
                        SortAccounts();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to load accounts: {ex.Message}");
                }
            }
        }

        private void SaveAccounts()
        {
            try
            {
                string json = JsonSerializer.Serialize(Accounts, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(AccountsFilePath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save accounts: {ex.Message}");
            }
        }

        private void Accounts_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            SaveAccounts();
        }

        private void AddAccount_Click(object sender, RoutedEventArgs e)
        {
            var username = UsernameTextBox.Text;
            var password = PasswordTextBox.Password;

            if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
            {
                Accounts.Add(new Account { Username = username, Password = password });
                SortAccounts(); // Add this line
                UsernameTextBox.Text = "";
                PasswordTextBox.Password = "";
            }
        }

        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Account account)
            {
                string wowPath = WowPath.Text;

                if (string.IsNullOrWhiteSpace(wowPath) || !File.Exists(wowPath))
                {
                    MessageBox.Show("Invalid WoW path. Please check the path and try again.");
                    return;
                }

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "AutoLogin.exe",
                        Arguments = $" \"{wowPath}\" {account.Username} {account.Password} {_launchDelayMs}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Account account)
            {
                var result = MessageBox.Show($"Are you sure you want to delete the account '{account.Username}'?",
                                             "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    Accounts.Remove(account); // This will trigger SaveAccounts() via CollectionChanged
                }
            }
        }

        private void WowPath_TextChanged(object sender, TextChangedEventArgs e)
        {
            SaveWowPath();
        }

        private void LoadWowPath()
        {
            if (File.Exists(WowPathFilePath))
            {
                try
                {
                    string json = File.ReadAllText(WowPathFilePath);
                    WowPath.Text = JsonSerializer.Deserialize<string>(json);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to load accounts: {ex.Message}");
                }
            }
        }

        private void SaveWowPath()
        {
            try
            {
                string json = JsonSerializer.Serialize(WowPath.Text, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(WowPathFilePath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save wow path: {ex.Message}");
            }
        }

        private void SortAccounts()
        {
            var sorted = new ObservableCollection<Account>(Accounts.OrderBy(a => a.Username, StringComparer.OrdinalIgnoreCase));
            Accounts.CollectionChanged -= Accounts_CollectionChanged;
            Accounts.Clear();
            foreach (var account in sorted)
                Accounts.Add(account);
            Accounts.CollectionChanged += Accounts_CollectionChanged;
        }

    }

    public class Account
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
