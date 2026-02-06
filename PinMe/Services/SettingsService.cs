using System;
using System.IO;
using System.Text.Json;

namespace Pinnie.Services
{
    public class AppSettings
    {
        // Default: Ctrl (2) + Win (8) = 10. Key T (0x54).
        public uint HotkeyModifiers { get; set; } = 10; 
        public uint HotkeyKey { get; set; } = 0x54; 
    }

    public class SettingsService
    {
        private readonly string _settingsPath;
        public AppSettings CurrentSettings { get; private set; }

        public SettingsService()
        {
            _settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
            Load();
        }

        public void Load()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    string json = File.ReadAllText(_settingsPath);
                    CurrentSettings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
                else
                {
                    CurrentSettings = new AppSettings();
                }
            }
            catch
            {
                CurrentSettings = new AppSettings(); // Fallback to defaults
            }
        }

        public void Save()
        {
            try
            {
                string json = JsonSerializer.Serialize(CurrentSettings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_settingsPath, json);
            }
            catch (Exception ex)
            {
                // Log failure?
                System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
            }
        }
    }
}
