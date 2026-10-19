using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Win32;

namespace DynamicIslandMedia.Models
{
    public class AppSettings
    {
        public const double DefaultBottomMargin = 7.6; // Exactly 1/5 (20%) of collapsed height (38px)

        public double BottomMargin { get; set; } = DefaultBottomMargin;
        public bool IsMasterEnabled { get; set; } = true;
        public bool AutoHideWhenIdle { get; set; } = false;
        public bool AutoExpandOnHover { get; set; } = true;
        public bool StartWithWindows { get; set; } = false;

        // Legacy support in case old settings.json has TopMargin
        [JsonPropertyName("TopMargin")]
        public double? LegacyTopMargin
        {
            get => null;
            set
            {
                if (value.HasValue && BottomMargin == DefaultBottomMargin)
                {
                    // keep DefaultBottomMargin as new standard
                }
            }
        }

        private static readonly string SettingsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DynamicIslandMedia"
        );
        private static readonly string SettingsFile = Path.Combine(SettingsDir, "settings.json");

        private const string StartupRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppRegistryName = "DynamicIslandMedia";

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsFile))
                {
                    var json = File.ReadAllText(SettingsFile);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    if (settings != null)
                    {
                        settings.SyncStartupRegistry();
                        return settings;
                    }
                }
            }
            catch
            {
                // Fallback to defaults
            }

            var defaultSettings = new AppSettings();
            defaultSettings.SyncStartupRegistry();
            return defaultSettings;
        }

        public void Save()
        {
            try
            {
                if (!Directory.Exists(SettingsDir))
                {
                    Directory.CreateDirectory(SettingsDir);
                }

                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFile, json);
                ApplyStartupRegistry(StartWithWindows);
            }
            catch
            {
                // Silently ignore settings write errors
            }
        }

        private void SyncStartupRegistry()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, false);
                StartWithWindows = key?.GetValue(AppRegistryName) != null;
            }
            catch
            {
                // Registry read fallback
            }
        }

        private static void ApplyStartupRegistry(bool enable)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, true);
                if (key == null) return;

                if (enable)
                {
                    var exePath = Environment.ProcessPath ?? System.IO.Path.Combine(AppContext.BaseDirectory, "DynamicIslandMedia.exe");
                    if (!string.IsNullOrEmpty(exePath))
                    {
                        key.SetValue(AppRegistryName, $"\"{exePath}\"");
                    }
                }
                else
                {
                    key.DeleteValue(AppRegistryName, false);
                }
            }
            catch
            {
                // Registry write fallback
            }
        }
    }
}
