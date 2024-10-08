﻿using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using UEParser.Models;
using UEParser.Parser;
using UEParser.Utils;
using UEParser.ViewModels;

namespace UEParser.APIComposers;

public class SpecialEvents
{
    private static readonly Dictionary<string, Dictionary<string, List<LocalizationEntry>>> LocalizationData = [];

    public static async Task InitializeSpecialEventsDB(CancellationToken token)
    {
        await Task.Run(() =>
        {
            Dictionary<string, SpecialEvent> parsedSpecialEventsDB = [];

            LogsWindowViewModel.Instance.AddLog($"Starting parsing process..", Logger.LogTags.Info, Logger.ELogExtraTag.SpecialEvents);

            ParseSpecialEvents(parsedSpecialEventsDB, token);

            LogsWindowViewModel.Instance.AddLog($"Parsed total of {parsedSpecialEventsDB.Count} items.", Logger.LogTags.Info, Logger.ELogExtraTag.SpecialEvents);

            ParseLocalizationAndSave(parsedSpecialEventsDB, token);
        }, token);
    }

    private static readonly string[] ignoreEvents =
    [
        "Barrel2023",
        "Gnome2021",
        "EddieZodiac"
    ];
    private static void ParseSpecialEvents(Dictionary<string, SpecialEvent> parsedSpecialEventsDB, CancellationToken token)
    {
        string[] filePaths = Helpers.FindFilePathsInExtractedAssetsCaseInsensitive("SpecialEventsDB.json");

        foreach (string filePath in filePaths)
        {
            // Duplicated SpecialEvent..
            if (filePath.Contains(@"DeadByDaylight\Content\Data\Events\Bacon\SpecialEventsDB.json")) continue;

            string packagePath = StringUtils.StripExtractedAssetsDir(filePath);
            LogsWindowViewModel.Instance.AddLog($"Processing: {packagePath}", Logger.LogTags.Info, Logger.ELogExtraTag.SpecialEvents);

            var assetItems = FileUtils.LoadDynamicJson(filePath);

            if ((assetItems?[0]?["Rows"]) == null) continue;

            foreach (var item in assetItems[0]["Rows"])
            {
                token.ThrowIfCancellationRequested();

                string eventId = item.Value["EventId"];

                Dictionary<string, List<LocalizationEntry>> localizationModel = [];

                if (ignoreEvents.Contains(eventId)) continue;

                string nameKey = item.Value["EventName"]["Key"];
                string nameSourceString = item.Value["EventName"]["SourceString"];

                string descriptionKey = item.Value["EventEntryData"]["Description"]["Key"];
                string descriptionSourceString = item.Value["EventEntryData"]["Description"]["SourceString"];

                Helpers.AddLocalizationEntry(localizationModel, "Name", nameKey, nameSourceString);
                Helpers.AddLocalizationEntry(localizationModel, "Description", descriptionKey, descriptionSourceString);

                LocalizationData.TryAdd(eventId, localizationModel);

                SpecialEvent model = new()
                {
                    Name = "",
                    Description = "",
                    StoreItemIds = item.Value["EventEntryData"]["AdditionalStoreItemIds"]
                };

                parsedSpecialEventsDB.Add(eventId, model);
            }
        }
    }

    private static void ParseLocalizationAndSave(Dictionary<string, SpecialEvent> parsedSpecialEventsDB, CancellationToken token)
    {
        LogsWindowViewModel.Instance.AddLog($"Starting localization process..", Logger.LogTags.Info, Logger.ELogExtraTag.SpecialEvents);

        string[] filePaths = Directory.GetFiles(Path.Combine(GlobalVariables.rootDir, "Dependencies", "Locres"), "*.json", SearchOption.TopDirectoryOnly);

        foreach (string filePath in filePaths)
        {
            token.ThrowIfCancellationRequested();

            string jsonString = File.ReadAllText(filePath);
            string fileName = Path.GetFileName(filePath);
            string langKey = StringUtils.LangSplit(fileName);

            Dictionary<string, string> languageKeys = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString) ?? throw new Exception($"Failed to load following locres file: {langKey}.");

            var objectString = JsonConvert.SerializeObject(parsedSpecialEventsDB);
            Dictionary<string, SpecialEvent> localizedSpecialEventsDB = JsonConvert.DeserializeObject<Dictionary<string, SpecialEvent>>(objectString) ?? [];

            Helpers.LocalizeDB(localizedSpecialEventsDB, LocalizationData, languageKeys, langKey);

            string outputPath = Path.Combine(GlobalVariables.pathToParsedData, GlobalVariables.versionWithBranch, langKey, "SpecialEvents.json");

            FileWriter.SaveParsedDB(localizedSpecialEventsDB, outputPath, Logger.ELogExtraTag.SpecialEvents);
        }
    }
}