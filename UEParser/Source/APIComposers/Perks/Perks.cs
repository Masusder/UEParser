using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UEParser.Models;
using UEParser.Parser;
using UEParser.Utils;
using UEParser.ViewModels;

namespace UEParser.APIComposers;

public class Perks
{
    private static readonly Dictionary<string, Dictionary<string, List<LocalizationEntry>>> localizationData = [];

    public static async Task InitializePerksDB()
    {
        await Task.Run(() =>
        {
            Dictionary<string, Perk> parsedPerksDB = [];

            LogsWindowViewModel.Instance.AddLog($"Starting parsing process..", Logger.LogTags.Info, Logger.ELogExtraTag.Perks);

            ParsePerks(parsedPerksDB);

            LogsWindowViewModel.Instance.AddLog($"Parsed total of {parsedPerksDB.Count} items.", Logger.LogTags.Info, Logger.ELogExtraTag.Perks);

            ParseLocalizationAndSave(parsedPerksDB);
        });
    }

    public static void ParsePerks(Dictionary<string, Perk> parsedPerksDB)
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
                string role = StringUtils.StringSplitVE(roleRaw);

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

                localizationData.TryAdd(perkId, localizationModel);

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

                parsedPerksDB.Add(perkId, model);
            }
        }
    }

    private static void ParseLocalizationAndSave(Dictionary<string, Perk> parsedPerksDB)
    {
        LogsWindowViewModel.Instance.AddLog($"Starting localization process..", Logger.LogTags.Info, Logger.ELogExtraTag.Perks);

        string[] filePaths = Directory.GetFiles(Path.Combine(GlobalVariables.rootDir, "Dependencies", "Locres"), "*.json", SearchOption.TopDirectoryOnly);

        foreach (string filePath in filePaths)
        {
            string jsonString = File.ReadAllText(filePath);
            string fileName = Path.GetFileName(filePath);
            string langKey = StringUtils.LangSplit(fileName);

            Dictionary<string, string> languageKeys = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString) ?? throw new Exception($"Failed to load following locres file: {langKey}.");

            var objectString = JsonConvert.SerializeObject(parsedPerksDB);
            Dictionary<string, Perk> localizedPerksDB = JsonConvert.DeserializeObject<Dictionary<string, Perk>>(objectString) ?? [];

            Helpers.LocalizeDB(localizedPerksDB, localizationData, languageKeys, langKey);
            PerkUtils.FormatDescriptionTunables(localizedPerksDB, langKey);

            string outputPath = Path.Combine(GlobalVariables.pathToParsedData, GlobalVariables.versionWithBranch, langKey, "Perks.json");

            FileWriter.SaveParsedDB(localizedPerksDB, outputPath, "Perks");
        }
    }
}