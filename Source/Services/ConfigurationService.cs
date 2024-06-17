using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using UEParser.Models;

namespace UEParser.Services
{
    public class ConfigurationService
    {
        private const string ConfigFilePath = "config.json";
        private static readonly JsonSerializerOptions JsonSerializerOptions = new();

        public static Configuration? Config { get; private set; }

        static ConfigurationService()
        {
            LoadConfiguration();
        }

        private static void LoadConfiguration()
        {
            Configuration initializationConfig = new();

            if (File.Exists(ConfigFilePath))
            {
                var json = File.ReadAllText(ConfigFilePath);
                Config = JsonSerializer.Deserialize<Configuration>(json) ?? initializationConfig;
            }
            else
            {
                Config = initializationConfig;
            }
        }

        public static async Task SaveConfiguration()
        {
            var json = JsonSerializer.Serialize(Config, JsonSerializerOptions);
            await File.WriteAllTextAsync(ConfigFilePath, json);
        }
    }
}