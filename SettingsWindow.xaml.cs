using System;
using System.Windows;
using System.Windows.Controls;

namespace SimpleLogger
{
    public partial class SettingsWindow : Window
    {
        private readonly AppSettings _settings;
        private readonly QrzApiService _qrzApiService;

        public SettingsWindow(AppSettings settings, QrzApiService qrzApiService)
        {
            InitializeComponent();
            _settings = settings;
            _qrzApiService = qrzApiService;
            LoadSettings();
        }

        private void LoadSettings()
        {
            SourceComboBox.Text = _settings.LookupSource;
            UsernameTextBox.Text = _settings.QrzUsername;
            PasswordBox.Password = _settings.QrzPassword;
            UpdateQrzGroupVisibility();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (SourceComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                // Safely handle potential null value for Content
                _settings.LookupSource = selectedItem.Content?.ToString() ?? "Callook.info";
            }
            _settings.QrzUsername = UsernameTextBox.Text;
            _settings.QrzPassword = PasswordBox.Password;
            _settings.Save();
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void SourceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateQrzGroupVisibility();
        }

        private void UpdateQrzGroupVisibility()
        {
            if (QrzCredentialsGroup == null) return;
            var selectedSource = (SourceComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            QrzCredentialsGroup.Visibility = selectedSource == "QRZ.com" ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void TestConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            StatusTextBlock.Text = "Testing...";
            var (success, message) = await _qrzApiService.LoginAsync(UsernameTextBox.Text, PasswordBox.Password);
            if (success)
            {
                StatusTextBlock.Text = "Login successful! Session key obtained.";
            }
            else
            {
                StatusTextBlock.Text = $"Login failed. Reason: {message}";
            }
        }

        private void ClearDataButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to clear all program data?\n\nThis will delete your log and all saved settings. This action cannot be undone.",
                "Confirm Clear Data",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                // Delete files from disk
                AppSettings.ClearAllData();

                // Clear the in-memory settings object
                _settings.Clear();

                // Refresh the UI to show the cleared state
                LoadSettings();

                // Inform user and shut down
                MessageBox.Show("All program data has been cleared. The application will now close.", "Data Cleared", MessageBoxButton.OK, MessageBoxImage.Information);
                Application.Current.Shutdown();
            }
        }
    }
}

