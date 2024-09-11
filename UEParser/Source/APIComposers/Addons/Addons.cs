using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UEParser.Models;
using UEParser.Parser;
using UEParser.ViewModels;
using UEParser.Utils;

namespace UEParser.APIComposers;

public class Addons
{
    private static readonly Dictionary<string, Dictionary<string, List<LocalizationEntry>>> LocalizationData = [];

    public static async Task InitializeAddonsDB()
    {
        await Task.Run(() =>
        {
            Dictionary<string, Addon> parsedAddonsDB = [];

            LogsWindowViewModel.Instance.AddLog($"Starting parsing process..", Logger.LogTags.Info, Logger.ELogExtraTag.Addons);

            ParseAddons(parsedAddonsDB);

            LogsWindowViewModel.Instance.AddLog($"Parsed total of {parsedAddonsDB.Count} items.", Logger.LogTags.Info, Logger.ELogExtraTag.Addons);

            ParseLocalizationAndSave(parsedAddonsDB);
        });
    }

    private static readonly string[] ignoreAddons =
    [
        "Addon_Firecracker_BlackPowder",
        "Addon_Firecracker_BuckShot",
        "Addon_Firecracker_FlashPowder",
        "Addon_Firecracker_GunPowder",
        "Addon_Firecracker_LargePack",
        "Addon_Firecracker_LongFuse",
        "Addon_Firecracker_MagnesiumPowder",
        "Addon_Firecracker_MediumFuse",
        "Addon_GasBomb_19a",
        "Addon_GasBomb_20a",
        "Addon_GasBomb_20b"
    ];
    private static void ParseAddons(Dictionary<string, Addon> parsedAddonsDB)
    {
        string[] filePaths = Helpers.FindFilePathsInExtractedAssetsCaseInsensitive("ItemAddonDB.json");

        foreach (string filePath in filePaths)
        {
            string packagePath = StringUtils.StripExtractedAssetsDir(filePath);
            LogsWindowViewModel.Instance.AddLog($"Processing: {packagePath}", Logger.LogTags.Info, Logger.ELogExtraTag.Addons);

            var assetItems = FileUtils.LoadDynamicJson(filePath);

            if ((assetItems?[0]?["Rows"]) == null) continue;

            foreach (var item in assetItems[0]["Rows"])
            {
                string addonId = item.Name;

                if (ignoreAddons.Contains(addonId)) continue;

                string typeRaw = item.Value["Type"];
                string type = StringUtils.DoubleDotsSplit(typeRaw);

                string itemTypeRaw = item.Value["ItemType"];
                string itemType = StringUtils.DoubleDotsSplit(itemTypeRaw);

                string abilityRaw = item.Value["RequiredKillerAbility"];
                string ability = StringUtils.StringSplitVE(abilityRaw);

                string roleRaw = item.Value["Role"];
                string role = StringUtils.StringSplitVE(roleRaw);

                string rarityRaw = item.Value["Rarity"];
                string rarity = StringUtils.DoubleDotsSplit(rarityRaw);

                string iconPathRaw = item.Value["UIData"]["IconFilePathList"][0];
                string iconPath = StringUtils.AddRootDirectory(iconPathRaw, "/images/");

                Dictionary<string, List<LocalizationEntry>> localizationModel = new()
                {
                    ["Name"] = [
                    new LocalizationEntry
                        {
                            Key = item.Value["UIData"]["DisplayName"]["Key"],
                            SourceString = item.Value["UIData"]["DisplayName"]["SourceString"]
                        }
                    ],
                    ["Description"] = [
                    new LocalizationEntry
                        {
                            Key = item.Value["UIData"]["Description"]["Key"],
                            SourceString = item.Value["UIData"]["Description"]["SourceString"]
                        }
                    ]
                };

                LocalizationData.TryAdd(addonId, localizationModel);

                Addon model = new()
                {
                    Type = type,
                    ItemType = itemType,
                    ParentItem = item.Value["ParentItem"]["ItemIDs"],
                    KillerAbility = ability,
                    Name = "",
                    Description = "",
                    Role = role,
                    Rarity = rarity,
                    CanBeUsedAfterEvent = item.Value["CanUseAfterEventEnd"],
                    Bloodweb = item.Value["Bloodweb"],
                    Image = iconPath
                };

                parsedAddonsDB.Add(addonId, model);
            }
        }
    }

    private static void ParseLocalizationAndSave(Dictionary<string, Addon> parsedAddonsDB)
    {
        LogsWindowViewModel.Instance.AddLog($"Starting localization process..", Logger.LogTags.Info, Logger.ELogExtraTag.Addons);

        string[] filePaths = Directory.GetFiles(Path.Combine(GlobalVariables.rootDir, "Dependencies", "Locres"), "*.json", SearchOption.TopDirectoryOnly);

        foreach (string filePath in filePaths)
        {
            string jsonString = File.ReadAllText(filePath);
            string fileName = Path.GetFileName(filePath);

            string langKey = StringUtils.LangSplit(fileName);

            Dictionary<string, string> languageKeys = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString) ?? throw new Exception($"Failed to load following locres file: {langKey}.");

            var objectString = JsonConvert.SerializeObject(parsedAddonsDB);
            Dictionary<string, Addon> localizedAddonsDB = JsonConvert.DeserializeObject<Dictionary<string, Addon>>(objectString) ?? [];

            Helpers.LocalizeDB(localizedAddonsDB, LocalizationData, languageKeys, langKey);

            string outputPath = Path.Combine(GlobalVariables.pathToParsedData, GlobalVariables.versionWithBranch, langKey, "Addons.json");

            FileWriter.SaveParsedDB(localizedAddonsDB, outputPath, Logger.ELogExtraTag.Addons);
        }
    }
}