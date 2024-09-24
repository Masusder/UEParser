using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UEParser.ViewModels;
using Newtonsoft.Json;
using UEParser.Utils;
using UEParser.Services;
using UEParser.Parser;
using UEParser.Models;

namespace UEParser.APIComposers;

public class Rifts
{
    private static readonly Dictionary<string, Dictionary<string, List<LocalizationEntry>>> LocalizationData = [];
    private static readonly dynamic? ArchiveRewardData = FileUtils.LoadDynamicJson(Path.Combine(GlobalVariables.pathToKraken, GlobalVariables.versionWithBranch, "CDN", "ArchiveRewardData.json")) ?? throw new Exception("Failed to load archive reward data.");

    public static async Task InitializeRiftsDB()
    {
        await Task.Run(() =>
        {
            Dictionary<string, Rift> parsedRiftsDB = [];

            LogsWindowViewModel.Instance.AddLog($"Starting parsing process..", Logger.LogTags.Info, Logger.ELogExtraTag.Rifts);

            parsedRiftsDB = ParseRifts(parsedRiftsDB);

            LogsWindowViewModel.Instance.AddLog($"Parsed total of {parsedRiftsDB.Count} items.", Logger.LogTags.Info, Logger.ELogExtraTag.Rifts);

            ParseLocalizationAndSave(parsedRiftsDB);
        });
    }

    private static Dictionary<string, Rift> ParseRifts(Dictionary<string, Rift> parsedRiftsDB)
    {
        var config = ConfigurationService.Config;
        var eventTomesArray = config.Core.EventTomesList;

        string[] filePaths = Helpers.FindFilePathsInExtractedAssetsCaseInsensitive("ArchiveDB.json");

        foreach (string filePath in filePaths)
        {
            string packagePath = StringUtils.StripExtractedAssetsDir(filePath);
            LogsWindowViewModel.Instance.AddLog($"Processing: {packagePath}", Logger.LogTags.Info, Logger.ELogExtraTag.Rifts);

            var assetItems = FileUtils.LoadDynamicJson(filePath);

            if ((assetItems?[0]?["Rows"]) == null)
            {
                continue;
            }

            foreach (var item in assetItems[0]["Rows"])
            {
                string riftId = item.Name;
                bool exists = eventTomesArray?.Any(x => x == riftId) ?? true;
                if (!exists)
                {
                    string pathToRiftFile = Path.Combine(GlobalVariables.pathToKraken, GlobalVariables.versionWithBranch, "CDN", "Rifts", $"{riftId}.json");
                    if (!File.Exists(pathToRiftFile))
                    {
                        LogsWindowViewModel.Instance.AddLog($"Not found Rift data for '{riftId}'. Make sure to update API first or add any missing event tomes to config.", Logger.LogTags.Error);
                        LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
                        continue;
                    }

                    var riftData = FileUtils.LoadDynamicJson(pathToRiftFile);

                    string riftIdTitleCase = StringUtils.TomeToTitleCase(riftId);

                    Dictionary<string, List<LocalizationEntry>> localizationModel = new()
                    {
                        ["Name"] = [
                        new LocalizationEntry
                            {
                                Key = item.Value["Title"]["Key"],
                                SourceString = item.Value["Title"]["SourceString"]
                            }
                        ]
                    };

                    LocalizationData.TryAdd(riftIdTitleCase, localizationModel);

                    Rift model = new()
                    {
                        Name = item.Value["Title"]["Key"],
                        Requirement = riftData?.GetValue(riftId, StringComparison.OrdinalIgnoreCase)?["requirement"],
                        EndDate = ArchiveRewardData?.GetValue(riftId, StringComparison.OrdinalIgnoreCase)?["endDate"],
                        StartDate = ArchiveRewardData?.GetValue(riftId, StringComparison.OrdinalIgnoreCase)?["startDate"],
                        TierInfo = riftData?.GetValue(riftId, StringComparison.OrdinalIgnoreCase)?["tierInfo"]?.ToObject<List<TierInfo>>() ?? new List<TierInfo>()
                    };

                    parsedRiftsDB.Add(riftIdTitleCase, model);
                }
            }
        }

        return parsedRiftsDB;
    }

    private static void ParseLocalizationAndSave(Dictionary<string, Rift> parsedRiftsDB)
    {
        LogsWindowViewModel.Instance.AddLog($"Starting localization process..", Logger.LogTags.Info, Logger.ELogExtraTag.Rifts);

        string[] filePaths = Directory.GetFiles(Path.Combine(GlobalVariables.rootDir, "Dependencies", "Locres"), "*.json", SearchOption.TopDirectoryOnly);

        foreach (string filePath in filePaths)
        {
            string jsonString = File.ReadAllText(filePath);
            string fileName = Path.GetFileName(filePath);

            string langKey = StringUtils.LangSplit(fileName);

            Dictionary<string, string> languageKeys = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString) ?? throw new Exception($"Failed to load following locres file: {langKey}.");

            var objectString = JsonConvert.SerializeObject(parsedRiftsDB);
            Dictionary<string, Rift> localizedRiftsDB = JsonConvert.DeserializeObject<Dictionary<string, Rift>>(objectString) ?? [];

            Helpers.LocalizeDB(localizedRiftsDB, LocalizationData, languageKeys, langKey);

            string outputPath = Path.Combine(GlobalVariables.pathToParsedData, GlobalVariables.versionWithBranch, langKey, "Rifts.json");

            FileWriter.SaveParsedDB(localizedRiftsDB, outputPath, Logger.ELogExtraTag.Rifts);
        }
    }
}