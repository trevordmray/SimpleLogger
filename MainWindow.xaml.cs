using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Xml.Linq;

namespace SimpleLogger
{
    public partial class MainWindow : Window
    {
        private AppSettings _appSettings;
        private QrzApiService _qrzApiService;
        private CallookApiService _callookApiService;
        private List<QSO> _qsoLog;

        public MainWindow()
        {
            InitializeComponent();
            _appSettings = AppSettings.Load();
            _qrzApiService = new QrzApiService();
            _callookApiService = new CallookApiService();
            _qsoLog = new List<QSO>();
            LoadQsoLog();
        }

        private void LoadQsoLog()
        {
            var logPath = Path.Combine(AppSettings.DataDirectory, "qso_log.json");
            if (File.Exists(logPath))
            {
                var json = File.ReadAllText(logPath);
                _qsoLog = JsonConvert.DeserializeObject<List<QSO>>(json) ?? new List<QSO>();
            }
            RefreshQsoGrid();
        }

        private void SaveQsoLog()
        {
            var logPath = Path.Combine(AppSettings.DataDirectory, "qso_log.json");
            var json = JsonConvert.SerializeObject(_qsoLog, Formatting.Indented);
            File.WriteAllText(logPath, json);
        }

        private void RefreshQsoGrid()
        {
            QsoDataGrid.ItemsSource = null;
            QsoDataGrid.ItemsSource = _qsoLog.OrderByDescending(q => q.Date).ThenByDescending(q => q.Time);
            QsoCountLabel.Content = $"QSOs Logged: {_qsoLog.Count}";
        }

        private async void LookupButton_Click(object sender, RoutedEventArgs e)
        {
            var callsign = CallsignTextBox.Text.Trim();
            if (string.IsNullOrEmpty(callsign)) return;

            LookupResultsGroup.Visibility = Visibility.Visible;
            DisplayLookupResults("Looking up...", null, null);

            if (_appSettings.LookupSource == "QRZ.com")
            {
                await HandleQrzLookup(callsign);
            }
            else
            {
                await HandleCallookLookup(callsign);
            }

            MyCallsignTextBox.Focus();
        }

        private async Task HandleQrzLookup(string callsign)
        {
            if (!_qrzApiService.IsLoggedIn)
            {
                var (success, errorMessage) = await _qrzApiService.LoginAsync(_appSettings.QrzUsername, _appSettings.QrzPassword);
                if (!success)
                {
                    MessageBox.Show($"Could not log in to QRZ.com. Reason: {errorMessage}", "QRZ Login Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                    DisplayLookupResults("QRZ.com login failed.", null, null);
                    return;
                }
            }

            XNamespace qrzNs = "http://xmldata.qrz.com";
            var qrzResult = await _qrzApiService.GetCallsignInfoAsync(callsign);

            var callsignElement = qrzResult.Descendants(qrzNs + "Callsign").FirstOrDefault();
            var sessionError = qrzResult.Descendants(qrzNs + "Error").FirstOrDefault()?.Value;

            if (callsignElement != null)
            {
                var city = callsignElement.Element(qrzNs + "addr2")?.Value;
                var state = callsignElement.Element(qrzNs + "state")?.Value;
                var country = callsignElement.Element(qrzNs + "country")?.Value;

                var locationParts = new[] { city, state, country };
                var cleanLocationParts = locationParts
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s.Trim().Trim(','));
                var location = string.Join(", ", cleanLocationParts);

                var sb = new StringBuilder();
                sb.AppendLine($"Callsign: {callsignElement.Element(qrzNs + "call")?.Value}");
                sb.AppendLine($"Name: {callsignElement.Element(qrzNs + "fname")?.Value} {callsignElement.Element(qrzNs + "name")?.Value}".Trim());
                sb.AppendLine($"Location: {location}");
                sb.AppendLine($"Grid: {callsignElement.Element(qrzNs + "grid")?.Value}");

                TheirNameTextBox.Text = $"{callsignElement.Element(qrzNs + "fname")?.Value} {callsignElement.Element(qrzNs + "name")?.Value}".Trim();
                TheirLocationTextBox.Text = location;
                TheirGridTextBox.Text = callsignElement.Element(qrzNs + "grid")?.Value;

                DisplayLookupResults(sb.ToString(), $"https://www.qrz.com/db/{callsign}", "View on QRZ.com");
            }
            else if (!string.IsNullOrEmpty(sessionError))
            {
                DisplayLookupResults($"QRZ.com lookup failed: {sessionError}", null, null);
            }
            else
            {
                DisplayLookupResults("Callsign not found in QRZ.com database.", null, null);
            }
        }

        private async Task HandleCallookLookup(string callsign)
        {
            var callookResult = await _callookApiService.GetCallsignInfoAsync(callsign);
            if (callookResult != null && callookResult["status"]?.ToString() == "VALID")
            {
                var locationString = callookResult["address"]?["line2"]?.ToString() ?? "";
                var location = $"{locationString}, United States";

                var sb = new StringBuilder();
                sb.AppendLine($"Callsign: {callookResult["current"]?["callsign"]?.ToString()}");
                sb.AppendLine($"Name: {callookResult["name"]?.ToString()}");
                sb.AppendLine($"Location: {location}");
                sb.AppendLine($"Grid: {callookResult["location"]?["gridsquare"]?.ToString()}");

                TheirNameTextBox.Text = callookResult["name"]?.ToString();
                TheirLocationTextBox.Text = location;
                TheirGridTextBox.Text = callookResult["location"]?["gridsquare"]?.ToString();

                DisplayLookupResults(sb.ToString(), callookResult["otherInfo"]?["ulsUrl"]?.ToString(), "View on ULS");
            }
            else
            {
                DisplayLookupResults("Callsign not found in Callook.info database.", null, null);
            }
        }

        private void DisplayLookupResults(string mainText, string? url, string? urlText)
        {
            LookupResultsTextBlock.Inlines.Clear();
            LookupResultsTextBlock.Inlines.Add(new Run(mainText.TrimEnd()));

            if (!string.IsNullOrEmpty(url) && !string.IsNullOrEmpty(urlText))
            {
                LookupResultsTextBlock.Inlines.Add(new LineBreak());

                var hyperlink = new Hyperlink(new Run(urlText))
                {
                    NavigateUri = new Uri(url)
                };
                hyperlink.RequestNavigate += Hyperlink_RequestNavigate;
                LookupResultsTextBlock.Inlines.Add(hyperlink);
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        private void LogQsoButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(MyCallsignTextBox.Text))
            {
                MessageBox.Show("My Callsign is a required field.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var qso = new QSO
            {
                Date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                Time = DateTime.UtcNow.ToString("HH:mm"),
                Callsign = CallsignTextBox.Text,
                TheirName = TheirNameTextBox.Text,
                FrequencyBand = FrequencyBandTextBox.Text,
                Mode = ModeComboBox.Text,
                RstSent = RstSentTextBox.Text,
                RstReceived = RstRcvdTextBox.Text,
                TheirLocation = TheirLocationTextBox.Text,
                TheirGrid = TheirGridTextBox.Text,
                MyCallsign = MyCallsignTextBox.Text,
                MyGrid = MyGridTextBox.Text,
                Notes = NotesTextBox.Text,
            };

            _qsoLog.Add(qso);
            SaveQsoLog();
            RefreshQsoGrid();
            ClearInputFields();
        }

        private void ClearInputFields()
        {
            CallsignTextBox.Clear();
            TheirNameTextBox.Clear();
            FrequencyBandTextBox.Clear();
            TheirGridTextBox.Clear();
            RstSentTextBox.Clear();
            RstRcvdTextBox.Clear();
            NotesTextBox.Clear();
            TheirLocationTextBox.Clear();
            LookupResultsTextBlock.Inlines.Clear();
            LookupResultsGroup.Visibility = Visibility.Collapsed;
        }

        private void ViewButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is QSO qso)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"Callsign: {qso.Callsign}");
                sb.AppendLine($"Date: {qso.Date}");
                sb.AppendLine($"Time: {qso.Time} UTC");
                sb.AppendLine($"Frequency/Band: {qso.FrequencyBand}");
                sb.AppendLine($"Mode: {qso.Mode}");
                sb.AppendLine($"RST Sent: {qso.RstSent}");
                sb.AppendLine($"RST Rcvd: {qso.RstReceived}");
                sb.AppendLine($"Location: {qso.TheirLocation}");
                sb.AppendLine($"Their Grid: {qso.TheirGrid}");
                sb.AppendLine($"My Callsign: {qso.MyCallsign}");
                sb.AppendLine($"My Grid: {qso.MyGrid}");
                sb.AppendLine($"Notes: {qso.Notes}");
                MessageBox.Show(sb.ToString(), "QSO Details");
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is QSO qso)
            {
                var editWindow = new EditQsoWindow(qso) { Owner = this };
                if (editWindow.ShowDialog() == true)
                {
                    SaveQsoLog();
                    RefreshQsoGrid();
                }
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is QSO qso)
            {
                if (MessageBox.Show($"Are you sure you want to delete the QSO with {qso.Callsign}?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    _qsoLog.Remove(qso);
                    SaveQsoLog();
                    RefreshQsoGrid();
                }
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow(_appSettings, _qrzApiService) { Owner = this };
            settingsWindow.ShowDialog();
            _appSettings.Save();
        }

        private void CreditsButton_Click(object sender, RoutedEventArgs e)
        {
            var creditsWindow = new CreditsWindow() { Owner = this };
            creditsWindow.ShowDialog();
        }

        private void ImportCsvButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog { Filter = "CSV Files (*.csv)|*.csv" };
            if (openFileDialog.ShowDialog() == true)
            {
                var newQsos = CsvHelper.Import(openFileDialog.FileName);
                var confirmWindow = new ImportConfirmWindow(newQsos.Count) { Owner = this };
                if (confirmWindow.ShowDialog() == true)
                {
                    if (confirmWindow.Merge)
                    {
                        _qsoLog.AddRange(newQsos);
                    }
                    else
                    {
                        _qsoLog = newQsos;
                    }
                    SaveQsoLog();
                    RefreshQsoGrid();
                }
            }
        }

        private void ExportCsvButton_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new SaveFileDialog { Filter = "CSV Files (*.csv)|*.csv" };
            if (saveFileDialog.ShowDialog() == true)
            {
                CsvHelper.Export(_qsoLog, saveFileDialog.FileName);
            }
        }

        private void ExportAdifButton_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new SaveFileDialog { Filter = "ADIF Files (*.adi)|*.adi" };
            if (saveFileDialog.ShowDialog() == true)
            {
                AdifHelper.Export(_qsoLog, saveFileDialog.FileName);
            }
        }

        private void ClearLogButton_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to clear the entire log? This cannot be undone.", "Confirm Clear Log", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                _qsoLog.Clear();
                SaveQsoLog();
                RefreshQsoGrid();
            }
        }

        private void CallsignTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                LookupButton_Click(sender, e);
            }
        }
    }
}

