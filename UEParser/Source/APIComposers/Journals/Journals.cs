using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

            LogsWindowViewModel.Instance.AddLog($"[Journals] Starting parsing process..", Logger.LogTags.Info);

            ParseJournals(parsedJournalsDB);

            LogsWindowViewModel.Instance.AddLog($"[Journals] Parsed total of {parsedJournalsDB.Count} items.", Logger.LogTags.Info);

            ParseLocalizationAndSave(parsedJournalsDB);
        });
    }

    private static void ParseJournals(Dictionary<string, Journal> parsedJournalsDB)
    {
        string[] filePaths = Helpers.FindFilePathsInExtractedAssetsCaseInsensitive("ArchiveJournalDB.json");

        foreach (string filePath in filePaths)
        {
            string packagePath = StringUtils.StripExtractedAssetsDir(filePath);
            LogsWindowViewModel.Instance.AddLog($"[Journals] Processing: {packagePath}", Logger.LogTags.Info);

            var assetItems = FileUtils.LoadDynamicJson(filePath);

            if ((assetItems?[0]?["Rows"]) == null) continue;

            foreach (var item in assetItems[0]["Rows"])
            {
                //string journalId = item.Name;

                //List<Vignette> vignettes = [];
                //foreach (var vignette in item.Value["Vignettes"])
                //{
                //    List<Entry> entries = [];
                //    foreach (var entry in vignette["Entries"])
                //    {
                //        string? rewardImageOutputPath = null;
                //        string rewardImageAssetPathName = entry["RewardImage"]["AssetPathName"];
                //        if (rewardImageAssetPathName != "None")
                //        {
                //            string[] pathComponents = rewardImageAssetPathName.Split('/');

                //            int assetsIndex = Array.IndexOf(pathComponents, "Assets");

                //            string directoryPath = string.Join("/", pathComponents, assetsIndex, pathComponents.Length - assetsIndex - 1);
                //            string fileName = Path.GetFileNameWithoutExtension(pathComponents[pathComponents.Length - 1]);

                //            // construct the desired output path
                //            rewardImageOutputPath = $"/images/{directoryPath}/{fileName}.png";
                //        }

                //        RewardImage rewardImage = new()
                //        {
                //            AssetPathName = rewardImageOutputPath,
                //            SubPathString = entry["RewardImage"]["SubPathString"]
                //        };

                //        Audio audio = new()
                //        {
                //            Path = null,
                //            HasAudio = entry["HasAudio"]
                //        };

                //        Entry entryModel = new()
                //        {
                //            Title = entry["Title"]["Key"],
                //            Text = entry["Text"]["Key"],
                //            Audio = audio,
                //            RewardImage = rewardImage
                //        };

                //        entries.Add(entryModel);
                //    }

                //    Vignette vignetteModel = new()
                //    {
                //        VignetteId = vignette["VignetteId"],
                //        Name = vignette["Title"]["Key"],
                //        SubTitle = vignette["Subtitle"]["Key"],
                //        Entries = entries
                //    };

                //    vignettes.Add(vignetteModel);
                //}

                //Journal model = new()
                //{
                //    TomeName = null,
                //    Vignettes = vignettes
                //};

                //string fixedTomeId = StringUtils.TomeToTitleCase(item.Name);

                //parsedJournalsDB.Add(journalId, model);
            }
        }
    }

    private static void ParseLocalizationAndSave(Dictionary<string, Journal> parsedJournalsDB)
    {
        LogsWindowViewModel.Instance.AddLog($"[Journals] Starting localization process..", Logger.LogTags.Info);

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

            string outputPath = Path.Combine(GlobalVariables.rootDir, "Output", "ParsedData", GlobalVariables.versionWithBranch, langKey, "Journals.json");

            FileWriter.SaveParsedDB(localizedJournalsDB, outputPath, "Journals");
        }
    }
}