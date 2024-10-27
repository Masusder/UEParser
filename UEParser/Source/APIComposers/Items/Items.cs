using System;
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

public class Items
{
    private static readonly Dictionary<string, Dictionary<string, List<LocalizationEntry>>> LocalizationData = [];

    public static async Task InitializeItemsDb(CancellationToken token)
    {
        await Task.Run(() =>
        {
            Dictionary<string, Item> parsedItemsDb = [];

            LogsWindowViewModel.Instance.AddLog($"Starting parsing process..", Logger.LogTags.Info, Logger.ELogExtraTag.Items);

            ParseItems(parsedItemsDb, token);

            LogsWindowViewModel.Instance.AddLog($"Parsed total of {parsedItemsDb.Count} items.", Logger.LogTags.Info, Logger.ELogExtraTag.Items);

            ParseLocalizationAndSave(parsedItemsDb, token);
        }, token);
    }

    private static readonly string[] IgnoreItems =
    [
        "Item_Blighted_Serum",
        "Item_Camper_OnryoTape",
        "Item_Camper_K32Emp",
        "Item_Camper_K33Turret",
        "Father_Key_Card"
    ];
    private static void ParseItems(Dictionary<string, Item> parsedItemsDb, CancellationToken token)
    {
        string[] filePaths = Helpers.FindFilePathsInExtractedAssetsCaseInsensitive("ItemDB.json");

        foreach (string filePath in filePaths)
        {
            string packagePath = StringUtils.StripExtractedAssetsDir(filePath);
            LogsWindowViewModel.Instance.AddLog($"Processing: {packagePath}", Logger.LogTags.Info, Logger.ELogExtraTag.Items);

            var assetItems = FileUtils.LoadDynamicJson(filePath);

            if ((assetItems?[0]?["Rows"]) == null) continue;

            foreach (var item in assetItems[0]["Rows"])
            {
                token.ThrowIfCancellationRequested();

                string itemId = item.Name;

                if (IgnoreItems.Contains(itemId)) continue;

                string iconPathRaw = item.Value["UIData"]["IconFilePathList"][0];
                string iconPath = StringUtils.AddRootDirectory(iconPathRaw, "/images/");

                // Fix broken paths
                switch (itemId)
                {
                    case "Item_Camper_AnniversaryToolbox":
                        iconPath = "/images/UI/Icons/Items/Anniversary/iconItems_toolbox_anniversary2021.png";
                        break;
                    case "Item_Camper_Flashlight_Anniversary2022":
                        iconPath = "/images/UI/Icons/Items/Anniversary/iconItems_flashlight_anniversary2022.png";
                        break;
                    case "Item_Camper_Medkit_Anniversary2020":
                        iconPath = "/images/UI/Icons/Items/Anniversary/iconItems_medkit_anniversary2020.png";
                        break;
                    case "Item_Camper_Toolbox_Anniversary2022":
                        iconPath = "/images/UI/Icons/Items/Anniversary/iconItems_toolbox_anniversary2022.png";
                        break;
                    case "Item_Camper_Medkit_Anniversary2022":
                        iconPath = "/images/UI/Icons/Items/Anniversary/iconItems_medkit_anniversary2022.png";
                        break;
                }

                string abilityRaw = item.Value["RequiredKillerAbility"];
                string ability = StringUtils.StringSplitVe(abilityRaw);

                string roleRaw = item.Value["Role"];
                string role = StringUtils.StringSplitVe(roleRaw);

                string itemTypeRaw = item.Value["ItemType"];
                string itemType = StringUtils.DoubleDotsSplit(itemTypeRaw);

                string rarityRaw = item.Value["Rarity"];
                string rarity = StringUtils.DoubleDotsSplit(rarityRaw);

                // Every killer ability item should have 'Common' rarity
                switch (itemId)
                {
                    case "Item_Slasher_Kanobo":
                        rarity = "Common";
                        break;
                }

                string typeRaw = item.Value["Type"];
                string type = StringUtils.DoubleDotsSplit(typeRaw);

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

                LocalizationData.TryAdd(itemId, localizationModel);

                Item model = new()
                {
                    RequiredAbility = ability,
                    Role = role,
                    Rarity = rarity,
                    Type = type,
                    ItemType = itemType,
                    Name = "",
                    Description = "",
                    IconFilePathList = iconPath,
                    Inventory = item.Value["Inventory"],
                    Chest = item.Value["Chest"],
                    Bloodweb = item.Value["Bloodweb"],
                    IsBotSupported = item.Value["IsBotSupported"]
                };

                parsedItemsDb.Add(itemId, model);
            }
        }
    }

    private static void ParseLocalizationAndSave(Dictionary<string, Item> parsedItemsDb, CancellationToken token)
    {
        LogsWindowViewModel.Instance.AddLog($"Starting localization process..", Logger.LogTags.Info, Logger.ELogExtraTag.Items);

        string[] filePaths = Directory.GetFiles(Path.Combine(GlobalVariables.RootDir, "Dependencies", "Locres"), "*.json", SearchOption.TopDirectoryOnly);

        foreach (string filePath in filePaths)
        {
            token.ThrowIfCancellationRequested();

            string jsonString = File.ReadAllText(filePath);
            string fileName = Path.GetFileName(filePath);
            string langKey = StringUtils.LangSplit(fileName);

            Dictionary<string, string> languageKeys = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString) ?? throw new Exception($"Failed to load following locres file: {langKey}.");

            var objectString = JsonConvert.SerializeObject(parsedItemsDb);
            Dictionary<string, Item> localizedItemsDb = JsonConvert.DeserializeObject<Dictionary<string, Item>>(objectString) ?? [];

            Helpers.LocalizeDb(localizedItemsDb, LocalizationData, languageKeys, langKey);

            string outputPath = Path.Combine(GlobalVariables.PathToParsedData, GlobalVariables.VersionWithBranch, langKey, "Items.json");

            FileWriter.SaveParsedDb(localizedItemsDb, outputPath, Logger.ELogExtraTag.Items);
        }
    }
}