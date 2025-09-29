using Newtonsoft.Json;
using System;
using System.IO;

namespace SimpleLogger
{
    public class AppSettings
    {
        public string LookupSource { get; set; } = "Callook.info";
        public string QrzUsername { get; set; } = string.Empty;
        public string QrzPassword { get; set; } = string.Empty;

        private static readonly string _appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SimpleLogger");
        private static readonly string _settingsFilePath = Path.Combine(_appDataPath, "settings.json");

        public static string DataDirectory => _appDataPath;

        public void Save()
        {
            Directory.CreateDirectory(_appDataPath);
            var json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(_settingsFilePath, json);
        }

        public static AppSettings Load()
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = File.ReadAllText(_settingsFilePath);
                return JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
            }
            return new AppSettings();
        }

        public static void ClearAllData()
        {
            // Delete the settings file
            if (File.Exists(_settingsFilePath))
            {
                File.Delete(_settingsFilePath);
            }

            // Delete the log file
            var logPath = Path.Combine(DataDirectory, "qso_log.json");
            if (File.Exists(logPath))
            {
                File.Delete(logPath);
            }
        }

        public void Clear()
        {
            LookupSource = "Callook.info";
            QrzUsername = string.Empty;
            QrzPassword = string.Empty;
        }
    }
}

