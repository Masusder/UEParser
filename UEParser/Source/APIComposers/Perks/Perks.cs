using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UEParser.Models;
using UEParser.Parser;
using UEParser.Utils;
using UEParser.ViewModels;

namespace UEParser.APIComposers;

public class Perks
{
    private static readonly Dictionary<string, Dictionary<string, List<LocalizationEntry>>> LocalizationData = [];

    public static async Task InitializePerksDb(CancellationToken token)
    {
        await Task.Run(() =>
        {
            Dictionary<string, Perk> parsedPerksDb = [];

            LogsWindowViewModel.Instance.AddLog($"Starting parsing process..", Logger.LogTags.Info, Logger.ELogExtraTag.Perks);

            ParsePerks(parsedPerksDb, token);

            LogsWindowViewModel.Instance.AddLog($"Parsed total of {parsedPerksDb.Count} items.", Logger.LogTags.Info, Logger.ELogExtraTag.Perks);

            ParseLocalizationAndSave(parsedPerksDb, token);
        }, token);
    }

    public static void ParsePerks(Dictionary<string, Perk> parsedPerksDb, CancellationToken token)
    {
        string[] filePaths = Helpers.FindFilePathsInExtractedAssetsCaseInsensitive("PerkDB.json");

        foreach (string filePath in filePaths)
        {
            string packagePath = StringUtils.StripExtractedAssetsDir(filePath);
            LogsWindowViewModel.Instance.AddLog($"Processing: {packagePath}", Logger.LogTags.Info, Logger.ELogExtraTag.Perks);

            var assetItems = FileUtils.LoadDynamicJson(filePath);
            if ((assetItems?[0]?["Rows"]) == null)
            {
                continue;
            }

            foreach (var item in assetItems[0]["Rows"])
            {
                token.ThrowIfCancellationRequested();

                string perkId = item.Name;

                JArray tagArray = item.Value["Tags"];
                string tag = string.Join(" ", tagArray);

                string? category = null;
                JArray categoryArray = item.Value["PerkCategory"];
                for (int i = 0; i < categoryArray.Count; i++)
                {
                    JToken categoryRaw = categoryArray[i];
                    category = StringUtils.DoubleDotsSplit(categoryRaw.ToString());
                }

                string roleRaw = item.Value["Role"];
                string role = StringUtils.StringSplitVe(roleRaw);

                List<LocalizationEntry> description = PerkUtils.ParsePerksDescription(item.Value);

                var tunables = PerkUtils.ArrangeTunables(item);

                // Stridor results in value length of two while only length 1 or 3 is accepted
                // This wouldn't be a problem if we used in-game rarity system, but we can't
                if (perkId == "Stridor")
                {
                    tunables = new List<List<string>>
                    {
                        new() {"25", "50", "50"},
                        new() {"0", "0", "25"}
                    };
                }

                string iconFilePathRaw = item.Value["UIData"]["IconFilePathList"][0];
                string iconFilePath = StringUtils.AddRootDirectory(iconFilePathRaw, "/images/");

                Dictionary<string, List<LocalizationEntry>> localizationModel = new()
                {
                    ["Name"] =
                    [
                        new()
                        {
                            Key = item.Value["UIData"]["DisplayName"]["Key"],
                            SourceString = item.Value["UIData"]["DisplayName"]["SourceString"],
                        }
                    ],
                    ["Description"] = description
                };

                LocalizationData.TryAdd(perkId, localizationModel);

                Perk model = new()
                {
                    Character = item.Value["AssociatedPlayerIndex"],
                    Categories = category,
                    Name = item.Value["UIData"]["DisplayName"]["Key"],
                    Description = "",
                    IconFilePathList = iconFilePath,
                    Tag = tag,
                    Role = role,
                    TeachableLevel = item.Value["TeachableOnBloodweblevel"],
                    Tunables = tunables,
                };

                parsedPerksDb.Add(perkId, model);
            }
        }
    }

    private static void ParseLocalizationAndSave(Dictionary<string, Perk> parsedPerksDb, CancellationToken token)
    {
        LogsWindowViewModel.Instance.AddLog($"Starting localization process..", Logger.LogTags.Info, Logger.ELogExtraTag.Perks);

        string[] filePaths = Directory.GetFiles(Path.Combine(GlobalVariables.RootDir, "Dependencies", "Locres"), "*.json", SearchOption.TopDirectoryOnly);

        foreach (string filePath in filePaths)
        {
            token.ThrowIfCancellationRequested();

            string jsonString = File.ReadAllText(filePath);
            string fileName = Path.GetFileName(filePath);
            string langKey = StringUtils.LangSplit(fileName);

            Dictionary<string, string> languageKeys = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString) ?? throw new Exception($"Failed to load following locres file: {langKey}.");

            var objectString = JsonConvert.SerializeObject(parsedPerksDb);
            Dictionary<string, Perk> localizedPerksDb = JsonConvert.DeserializeObject<Dictionary<string, Perk>>(objectString) ?? [];

            Helpers.LocalizeDb(localizedPerksDb, LocalizationData, languageKeys, langKey);
            PerkUtils.FormatDescriptionTunables(localizedPerksDb, langKey);

            string outputPath = Path.Combine(GlobalVariables.PathToParsedData, GlobalVariables.VersionWithBranch, langKey, "Perks.json");

            FileWriter.SaveParsedDb(localizedPerksDb, outputPath, Logger.ELogExtraTag.Perks);
        }
    }
}