using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using UEParser.Models;
using UEParser.Parser;
using UEParser.Utils;
using UEParser.ViewModels;

namespace UEParser.APIComposers;

public class Journals
{
    private static readonly Dictionary<string, Dictionary<string, List<LocalizationEntry>>> LocalizationData = [];

    public static async Task InitializeJournalsDb(CancellationToken token)
    {
        await Task.Run(() =>
        {
            Dictionary<string, Journal> parsedJournalsDb = [];

            LogsWindowViewModel.Instance.AddLog($"Starting parsing process..", Logger.LogTags.Info, Logger.ELogExtraTag.Journals);

            ParseJournals(parsedJournalsDb, token);

            LogsWindowViewModel.Instance.AddLog($"Parsed total of {parsedJournalsDb.Count} items.", Logger.LogTags.Info, Logger.ELogExtraTag.Journals);

            ParseLocalizationAndSave(parsedJournalsDb, token);
        }, token);
    }

    private static void ParseJournals(Dictionary<string, Journal> parsedJournalsDb, CancellationToken token)
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
                token.ThrowIfCancellationRequested();

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

                parsedJournalsDb.Add(tomeIdTitleCase, model);
            }
        }
    }

    private static void ParseLocalizationAndSave(Dictionary<string, Journal> parsedJournalsDb, CancellationToken token)
    {
        LogsWindowViewModel.Instance.AddLog($"Starting localization process..", Logger.LogTags.Info, Logger.ELogExtraTag.Journals);

        string[] filePaths = Directory.GetFiles(Path.Combine(GlobalVariables.RootDir, "Dependencies", "Locres"), "*.json", SearchOption.TopDirectoryOnly);

        foreach (string filePath in filePaths)
        {
            token.ThrowIfCancellationRequested();

            string jsonString = File.ReadAllText(filePath);
            string fileName = Path.GetFileName(filePath);
            string langKey = StringUtils.LangSplit(fileName);

            Dictionary<string, string> languageKeys = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString) ?? throw new Exception($"Failed to load following locres file: {langKey}.");

            var objectString = JsonConvert.SerializeObject(parsedJournalsDb);
            Dictionary<string, Journal> localizedJournalsDb = JsonConvert.DeserializeObject<Dictionary<string, Journal>>(objectString) ?? [];

            Helpers.LocalizeDb(localizedJournalsDb, LocalizationData, languageKeys, langKey);

            JournalUtils.PopulateTomeNames(localizedJournalsDb, langKey);

            string outputPath = Path.Combine(GlobalVariables.PathToParsedData, GlobalVariables.VersionWithBranch, langKey, "Journals.json");

            FileWriter.SaveParsedDb(localizedJournalsDb, outputPath, Logger.ELogExtraTag.Journals);
        }
    }
}