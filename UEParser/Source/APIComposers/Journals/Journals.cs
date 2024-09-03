using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UEParser.Models;
using UEParser.Parser;
using UEParser.Utils;
using UEParser.ViewModels;

namespace UEParser.APIComposers;

public class Journals
{
    private static readonly Dictionary<string, Dictionary<string, List<LocalizationEntry>>> LocalizationData = [];

    public static async Task InitializeJournalsDB()
    {
        await Task.Run(() =>
        {
            Dictionary<string, Journal> parsedJournalsDB = [];

            LogsWindowViewModel.Instance.AddLog($"Starting parsing process..", Logger.LogTags.Info, Logger.ELogExtraTag.Journals);

            ParseJournals(parsedJournalsDB);

            LogsWindowViewModel.Instance.AddLog($"Parsed total of {parsedJournalsDB.Count} items.", Logger.LogTags.Info, Logger.ELogExtraTag.Journals);

            ParseLocalizationAndSave(parsedJournalsDB);
        });
    }

    private static void ParseJournals(Dictionary<string, Journal> parsedJournalsDB)
    {
        string[] filePaths = Helpers.FindFilePathsInExtractedAssetsCaseInsensitive("ArchiveJournalDB.json");

        foreach (string filePath in filePaths)
        {
            string packagePath = StringUtils.StripExtractedAssetsDir(filePath);
            LogsWindowViewModel.Instance.AddLog($"Processing: {packagePath}", Logger.LogTags.Info, Logger.ELogExtraTag.Journals);

            var assetItems = FileUtils.LoadDynamicJson(filePath);

            if ((assetItems?[0]?["Rows"]) == null) continue;

            foreach (var item in assetItems[0]["Rows"])
            {
                string tomeId = item.Name;

                Dictionary<string, List<LocalizationEntry>> localizationModel = [];

                List<Vignette> vignettes = [];
                for (int vignetteIndex = 0; vignetteIndex < item.Value["Vignettes"].Count; vignetteIndex++)
                {
                    List<Entry> entries = [];
                    for (int entryIndex = 0; entryIndex < item.Value["Vignettes"][vignetteIndex]["Entries"].Count; entryIndex++)
                    {
                        string? rewardImageOutputPath = null;
                        string rewardImageAssetPathName = item.Value["Vignettes"][vignetteIndex]["Entries"][entryIndex]["RewardImage"]["AssetPathName"];
                        if (rewardImageAssetPathName != "None")
                        {
                            string[] pathComponents = rewardImageAssetPathName.Split('/');

                            int assetsIndex = Array.IndexOf(pathComponents, "Assets");

                            string directoryPath = string.Join("/", pathComponents, assetsIndex, pathComponents.Length - assetsIndex - 1);
                            string fileName = Path.GetFileNameWithoutExtension(pathComponents[^1]);

                            // Construct the desired output path
                            rewardImageOutputPath = $"/images/{directoryPath}/{fileName}.png";
                        }

                        RewardImage rewardImage = new()
                        {
                            AssetPathName = rewardImageOutputPath,
                            SubPathString = item.Value["Vignettes"][vignetteIndex]["Entries"][entryIndex]["RewardImage"]["SubPathString"]
                        };

                        Audio audio = new()
                        {
                            Path = null,
                            HasAudio = item.Value["Vignettes"][vignetteIndex]["Entries"][entryIndex]["HasAudio"]
                        };

                        string localizationTitleString = $"Vignettes.{vignetteIndex}.Entries.{entryIndex}.Title";
                        string localizationTextString = $"Vignettes.{vignetteIndex}.Entries.{entryIndex}.Text";

                        string titleKey = item.Value["Vignettes"][vignetteIndex]["Entries"][entryIndex]["Title"]["Key"];
                        string titleSourceString = item.Value["Vignettes"][vignetteIndex]["Entries"][entryIndex]["Title"]["SourceString"];

                        string textKey = item.Value["Vignettes"][vignetteIndex]["Entries"][entryIndex]["Text"]["Key"];
                        string textSourceString = item.Value["Vignettes"][vignetteIndex]["Entries"][entryIndex]["Text"]["SourceString"];

                        Helpers.AddLocalizationEntry(localizationModel, localizationTitleString, titleKey, titleSourceString);
                        Helpers.AddLocalizationEntry(localizationModel, localizationTextString, textKey, textSourceString);

                        Entry entryModel = new()
                        {
                            Title = "",
                            Text = "",
                            Audio = audio,
                            RewardImage = rewardImage
                        };

                        entries.Add(entryModel);
                    }

                    string localizationNameString = $"Vignettes.{vignetteIndex}.Name";
                    string localizationSubtitleString = $"Vignettes.{vignetteIndex}.SubTitle";

                    string nameKey = item.Value["Vignettes"][vignetteIndex]["Title"]["Key"];
                    string nameSourceString = item.Value["Vignettes"][vignetteIndex]["Title"]["SourceString"];

                    string subtitleKey = item.Value["Vignettes"][vignetteIndex]["Subtitle"]["Key"];
                    string subtitleSourceString = item.Value["Vignettes"][vignetteIndex]["Subtitle"]["SourceString"];

                    Helpers.AddLocalizationEntry(localizationModel, localizationNameString, nameKey, nameSourceString);
                    Helpers.AddLocalizationEntry(localizationModel, localizationSubtitleString, subtitleKey, subtitleSourceString);

                    Vignette vignetteModel = new()
                    {
                        VignetteId = item.Value["Vignettes"][vignetteIndex]["VignetteId"],
                        Name = "",
                        SubTitle = "",
                        Entries = entries
                    };

                    vignettes.Add(vignetteModel);
                }

                Journal model = new()
                {
                    TomeName = "",
                    Vignettes = vignettes
                };

                string tomeIdTitleCase = StringUtils.TomeToTitleCase(tomeId);

                LocalizationData.TryAdd(tomeIdTitleCase, localizationModel);

                parsedJournalsDB.Add(tomeIdTitleCase, model);
            }
        }
    }

    private static void ParseLocalizationAndSave(Dictionary<string, Journal> parsedJournalsDB)
    {
        LogsWindowViewModel.Instance.AddLog($"Starting localization process..", Logger.LogTags.Info, Logger.ELogExtraTag.Journals);

        string[] filePaths = Directory.GetFiles(Path.Combine(GlobalVariables.rootDir, "Dependencies", "Locres"), "*.json", SearchOption.TopDirectoryOnly);

        foreach (string filePath in filePaths)
        {
            string jsonString = File.ReadAllText(filePath);
            string fileName = Path.GetFileName(filePath);

            string langKey = StringUtils.LangSplit(fileName);

            Dictionary<string, string> languageKeys = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString) ?? throw new Exception($"Failed to load following locres file: {langKey}.");

            var objectString = JsonConvert.SerializeObject(parsedJournalsDB);
            Dictionary<string, Journal> localizedJournalsDB = JsonConvert.DeserializeObject<Dictionary<string, Journal>>(objectString) ?? [];

            Helpers.LocalizeDB(localizedJournalsDB, LocalizationData, languageKeys, langKey);

            JournalUtils.PopulateTomeNames(localizedJournalsDB, langKey);

            string outputPath = Path.Combine(GlobalVariables.pathToParsedData, GlobalVariables.versionWithBranch, langKey, "Journals.json");

            FileWriter.SaveParsedDB(localizedJournalsDB, outputPath, "Journals");
        }
    }
}