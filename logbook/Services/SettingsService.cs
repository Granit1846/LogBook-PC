using System;
using System.IO;
using Newtonsoft.Json;
using logbook.Models;

namespace logbook.Services
{
    public class SettingsService
    {
        private readonly string _filePath;

        public AppSettings Settings { get; private set; }

        public SettingsService()
        {
            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "LogbookApp");

            if (!Directory.Exists(appDataPath))
                Directory.CreateDirectory(appDataPath);

            _filePath = Path.Combine(appDataPath, "settings.json");
            Load();
        }

        private void Load()
        {
            if (File.Exists(_filePath))
            {
                try
                {
                    string json = File.ReadAllText(_filePath);
                    Settings = JsonConvert.DeserializeObject<AppSettings>(json)
                               ?? throw new JsonSerializationException("Empty settings data");
                }
                catch (JsonException ex)
                {
                    throw new InvalidDataException($"Cannot read the LogBook settings file: {_filePath}", ex);
                }
            }
            else
            {
                Settings = new AppSettings();
            }

            // По умолчанию — Documents\Logbook
            if (string.IsNullOrWhiteSpace(Settings.DataFolder))
            {
                Settings.DataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "Logbook");
            }

            // Если URL/ключ Supabase пусты — подставить значения по умолчанию
            if (string.IsNullOrWhiteSpace(Settings.SupabaseUrl))
            {
                Settings.SupabaseUrl = "https://zjagjauwmpuzcwzidhkn.supabase.co";
            }

            if (string.IsNullOrWhiteSpace(Settings.SupabaseKey))
            {
                Settings.SupabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InpqYWdqYXV3bXB1emN3emlkaGtuIiwicm9sZSI6ImFub24iLCJpYXQiOjE3OTA1MjUwNTYsImV4cCI6MjEwNjEwMTA1Nn0.VIHdNpnEZUBVO6HvNabQIREj45lenfT6WpgkUHGjnQ4";
            }
        }

        public void Save()
        {
            string json = JsonConvert.SerializeObject(Settings,
                Newtonsoft.Json.Formatting.Indented);
            AtomicFile.Write(_filePath, System.Text.Encoding.UTF8.GetBytes(json));
        }

        /// <summary>
        /// Возвращает путь к основному JSON.
        /// </summary>
        public string GetDataFilePath()
        {
            return Path.Combine(Settings.DataFolder, "work_data.json");
        }

        /// <summary>
        /// Возвращает путь к папке резервных копий.
        /// </summary>
        public string GetBackupFolder()
        {
            return Path.Combine(Settings.DataFolder, "backup");
        }

        /// <summary>
        /// Гарантирует, что папки существуют.
        /// </summary>
        public void EnsureFoldersExist()
        {
            if (!Directory.Exists(Settings.DataFolder))
                Directory.CreateDirectory(Settings.DataFolder);

            string backup = GetBackupFolder();
            if (!Directory.Exists(backup))
                Directory.CreateDirectory(backup);
        }
    }
}
