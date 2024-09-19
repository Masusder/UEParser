using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UEParser.Models;
using UEParser.ViewModels;
using UEParser.Utils;
using UEParser.Parser;

namespace UEParser.APIComposers;

internal class Maps
{
    private static readonly Dictionary<string, Dictionary<string, List<LocalizationEntry>>> LocalizationData = [];

    public static async Task InitializeMapsDB()
    {
        await Task.Run(() =>
        {
            Dictionary<string, Map> parsedMapsDB = [];

            LogsWindowViewModel.Instance.AddLog($"Starting parsing process..", Logger.LogTags.Info, Logger.ELogExtraTag.Maps);

            ParseMaps(parsedMapsDB);

            LogsWindowViewModel.Instance.AddLog($"Parsed total of {parsedMapsDB.Count} items.", Logger.LogTags.Info, Logger.ELogExtraTag.Maps);

            ParseLocalizationAndSave(parsedMapsDB);
        });
    }

    private static void ParseMaps(Dictionary<string, Map> parsedMapsDB)
    {
        // There's also limited gamemode maps, but we don't want these
        // If you for some reason need them comment out helper method
        string[] filePaths = [Path.Combine(GlobalVariables.pathToExtractedAssets, "DeadByDaylight", "Content", "Data", "ProceduralMaps.json")];
        //Helpers.FindFilePathsInExtractedAssetsCaseInsensitive("ProceduralMaps.json");

        foreach (string filePath in filePaths)
        {
            string packagePath = StringUtils.StripExtractedAssetsDir(filePath);
            LogsWindowViewModel.Instance.AddLog($"Processing: {packagePath}", Logger.LogTags.Info, Logger.ELogExtraTag.Maps);

            var assetItems = FileUtils.LoadDynamicJson(filePath);

            if ((assetItems?[0]?["Rows"]) == null) continue;

            foreach (var item in assetItems[0]["Rows"])
            {
                string mapId = item.Name;

                if (mapId == "Swp_Mound") continue; // Unfinished, unreleased map

                string thumbnailRaw = item.Value["ThumbnailPath"];
                string thumbnailPath = StringUtils.AddRootDirectory(thumbnailRaw, "/images/");

                Dictionary<string, List<LocalizationEntry>> localizationModel = new()
                {
                    ["Name"] = [
                    new LocalizationEntry
                        {
                            Key = item.Value["Name"]["Key"],
                            SourceString = item.Value["Name"]["SourceString"]
                        }
                    ],
                    ["Description"] = [
                    new LocalizationEntry
                        {
                            Key = item.Value["Description"]["Key"],
                            SourceString = item.Value["Description"]["SourceString"]
                        }
                    ],
                    ["Realm"] = [
                    new LocalizationEntry
                        {
                            Key = item.Value["ThemeName"]["Key"],
                            SourceString = item.Value["ThemeName"]["SourceString"]
                        }
                    ]
                };

                LocalizationData.TryAdd(mapId, localizationModel);

                Map model = new()
                {
                    Realm = "",
                    MapId = item.Value["MapId"],
                    Name = "",
                    Description = "",
                    HookMinDistance = item.Value["HookMinDistance"],
                    HookMinCount = item.Value["HookMinCount"],
                    HookMaxCount = item.Value["HookMaxCount"],
                    PalletsMinDistance = item.Value["BookShelvesMinDistance"],
                    PalletsMinCount = item.Value["BookShelvesMinCount"],
                    PalletsMaxCount = item.Value["BookShelvesMaxCount"],
                    DLC = item.Value["DlcIDString"],
                    Thumbnail = thumbnailPath
                };

                parsedMapsDB.Add(mapId, model);
            }
        }
    }

    private static void ParseLocalizationAndSave(Dictionary<string, Map> parsedMapsDB)
    {
        LogsWindowViewModel.Instance.AddLog($"Starting localization process..", Logger.LogTags.Info, Logger.ELogExtraTag.Maps);

        string[] filePaths = Directory.GetFiles(Path.Combine(GlobalVariables.rootDir, "Dependencies", "Locres"), "*.json", SearchOption.TopDirectoryOnly);

        foreach (string filePath in filePaths)
        {
            string jsonString = File.ReadAllText(filePath);
            string fileName = Path.GetFileName(filePath);

            string langKey = StringUtils.LangSplit(fileName);

            Dictionary<string, string> languageKeys = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString) ?? throw new Exception($"Failed to load following locres file: {langKey}.");

            var objectString = JsonConvert.SerializeObject(parsedMapsDB);
            Dictionary<string, Map> localizedMapsDB = JsonConvert.DeserializeObject<Dictionary<string, Map>>(objectString) ?? [];

            Helpers.LocalizeDB(localizedMapsDB, LocalizationData, languageKeys, langKey);

            string outputPath = Path.Combine(GlobalVariables.pathToParsedData, GlobalVariables.versionWithBranch, langKey, "Maps.json");

            FileWriter.SaveParsedDB(localizedMapsDB, outputPath, Logger.ELogExtraTag.Maps);
        }
    }
}