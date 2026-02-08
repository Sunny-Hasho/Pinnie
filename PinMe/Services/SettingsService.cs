using System;
using System.IO;
using System.Text.Json;

namespace Pinnie.Services
{
    public class AppSettings
    {
        // Hotkey settings
        // Default: Ctrl (2) + Win (8) = 10. Key T (0x54).
        public uint HotkeyModifiers { get; set; } = 10; 
        public uint HotkeyKey { get; set; } = 0x54; 
        
        // Visual settings
        public bool ShowPetIcon { get; set; } = true;
        public bool ShowBorder { get; set; } = true;
        public int BorderThickness { get; set; } = 2; // Default: Small (2px)
        public int BorderRadius { get; set; } = 8; // Default: 8px
        public string BorderColor { get; set; } = "#FFFFFF"; // Default: White (as hex string)
        
        // Pet icon settings
        public string? PetIconPath { get; set; } // Will be set to default capy.gif path if null
        public int PetIconSize { get; set; } = 50; // Default: Medium (50px)
        public string PetIconPosition { get; set; } = "Center"; // Default: Center
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
                    
                    // Ensure defaults for null values (backward compatibility)
                    if (string.IsNullOrEmpty(CurrentSettings.BorderColor))
                        CurrentSettings.BorderColor = "#FFFFFF";
                    if (string.IsNullOrEmpty(CurrentSettings.PetIconPosition))
                        CurrentSettings.PetIconPosition = "Center";
                }
                else
                {
                    CurrentSettings = new AppSettings();
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to load settings: {ex.Message}");
                CurrentSettings = new AppSettings(); // Fallback to defaults
            }
        }

        public void Save()
        {
            try
            {
                string json = JsonSerializer.Serialize(CurrentSettings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_settingsPath, json);
                Logger.Log("Settings saved successfully");
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to save settings: {ex.Message}");
            }
        }
    }
}
