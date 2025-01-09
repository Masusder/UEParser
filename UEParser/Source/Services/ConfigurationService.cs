using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UEParser.Models;

namespace UEParser.Services;

public class ConfigurationService
{
    private static readonly string AppDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "UEParser");
    private static readonly string ConfigFilePath = Path.Combine(AppDataFolder, "config.json");

    public static Configuration Config { get; private set; }

    static ConfigurationService()
    {
        Config = LoadConfiguration();
    }

    private static Configuration LoadConfiguration()
    {
        Configuration initializationConfig = new();

        if (!Directory.Exists(AppDataFolder)) Directory.CreateDirectory(AppDataFolder);

        if (File.Exists(ConfigFilePath))
        {
            var json = File.ReadAllText(ConfigFilePath);
            initializationConfig = JsonConvert.DeserializeObject<Configuration>(json, new JsonSerializerSettings
            {
                Converters = { new StringEnumConverter() } // Use StringEnumConverter for enum handling
            }) ?? initializationConfig;
        }

        return initializationConfig;
    }

    public static async Task SaveConfiguration()
    {
        if (!Directory.Exists(AppDataFolder)) Directory.CreateDirectory(AppDataFolder);

        var json = JsonConvert.SerializeObject(Config, Formatting.Indented, new JsonSerializerSettings
        {
            Converters = { new StringEnumConverter() } // Use StringEnumConverter for enum handling
        });

        await File.WriteAllTextAsync(ConfigFilePath, json);

        UpdateGlobalVariables();
    }

    private static void UpdateGlobalVariables()
    {
        GlobalVariables.VersionWithBranch = Helpers.ConstructVersionHeaderWithBranch();
        GlobalVariables.CompareVersionWithBranch = Helpers.ConstructVersionHeaderWithBranch(true);
    }
}