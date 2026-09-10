using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PHelper
{
    public class AppConfig
    {
        public TargetMode DefaultMode { get; set; } = TargetMode.Balanced;
        public List<AppProfile> Profiles { get; set; } = new List<AppProfile>();
        public List<string> CustomScanFolders { get; set; } = new List<string>();
    }

    public static class ConfigManager
    {
        private static readonly string AppDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PHelper");
        private static readonly string ConfigPath = Path.Combine(AppDataFolder, "config.json");

        public static AppConfig LoadConfig()
        {
            if (!File.Exists(ConfigPath))
            {
                return new AppConfig();
            }

            try
            {
                var json = File.ReadAllText(ConfigPath);
                var options = new JsonSerializerOptions { Converters = { new JsonStringEnumConverter() } };
                
                try
                {
                    if (json.TrimStart().StartsWith("{"))
                    {
                        return JsonSerializer.Deserialize<AppConfig>(json, options) ?? new AppConfig();
                    }
                    else
                    {
                        var profiles = JsonSerializer.Deserialize<List<AppProfile>>(json, options) ?? new List<AppProfile>();
                        return new AppConfig { Profiles = profiles };
                    }
                }
                catch
                {
                    return new AppConfig();
                }
            }
            catch
            {
                return new AppConfig();
            }
        }

        public static void SaveConfig(AppConfig config)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true, Converters = { new JsonStringEnumConverter() } };
                var json = JsonSerializer.Serialize(config, options);
                Directory.CreateDirectory(AppDataFolder);
                File.WriteAllText(ConfigPath, json);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to save config: {ex.Message}");
            }
        }
    }
}
