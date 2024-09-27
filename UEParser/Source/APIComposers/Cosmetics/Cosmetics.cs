using System;
using System.IO;
using System.Linq;
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

public class Cosmetics
{
    private static readonly Dictionary<string, Dictionary<string, List<LocalizationEntry>>> LocalizationData = [];
    private static readonly dynamic CatalogData = FileUtils.LoadDynamicJson(Path.Combine(GlobalVariables.pathToKraken, GlobalVariables.versionWithBranch, "CDN", "catalog.json")) ?? throw new Exception("Failed to load catalog data.");
    private static readonly Dictionary<string, Rift> RiftData = FileUtils.LoadJsonFileWithTypeCheck<Dictionary<string, Rift>>(Path.Combine(GlobalVariables.pathToParsedData, GlobalVariables.versionWithBranch, "en", "Rifts.json")) ?? throw new Exception("Failed to load rifts data.");
    private static readonly Dictionary<string, int> CatalogDictionary = CosmeticUtils.CreateCatalogDictionary(CatalogData);

    public static async Task InitializeCosmeticsDB(CancellationToken token)
    {
        await Task.Run(() =>
        {
            Dictionary<string, object> parsedCosmeticsDB = [];

            LogsWindowViewModel.Instance.AddLog("Starting parsing process..", Logger.LogTags.Info, Logger.ELogExtraTag.Cosmetics);

            parsedCosmeticsDB = ParseOutfits(parsedCosmeticsDB, token);
            parsedCosmeticsDB = ParseCustomizationItems(parsedCosmeticsDB, token);
            parsedCosmeticsDB = AssignRiftData(parsedCosmeticsDB);
            parsedCosmeticsDB = AppendStaticCurrencies(parsedCosmeticsDB);

            LogsWindowViewModel.Instance.AddLog($"Parsed total of {parsedCosmeticsDB.Count} items.", Logger.LogTags.Info, Logger.ELogExtraTag.Cosmetics);

            ParseLocalizationAndSave(parsedCosmeticsDB, token);
        }, token);
    }

    public static Dictionary<string, object> ParseOutfits(Dictionary<string, object> parsedCosmeticsDB, CancellationToken token)
    {
        string[] filePaths = Helpers.FindFilePathsInExtractedAssetsCaseInsensitive("OutfitDB.json");

        // K24_outfit_01 shouldn't exist and follows invalid format
        // Laurie_outfit_006 already exists under different ID
        // TR_outfit_011 isn't present in catalog
        // MT_outfit_022_CS already exists under different ID
        string[] outfitsToIgnore = ["K24_outfit_01", "Laurie_outfit_006", "TR_outfit_011", "MT_outfit_022_CS"];

        foreach (string filePath in filePaths)
        {
            string packagePath = StringUtils.StripExtractedAssetsDir(filePath);
            LogsWindowViewModel.Instance.AddLog($"Processing: {packagePath}", Logger.LogTags.Info, Logger.ELogExtraTag.Cosmetics);

            var assetItems = FileUtils.LoadDynamicJson(filePath);
            if ((assetItems?[0]?["Rows"]) == null)
            {
                continue;
            }

            foreach (var item in assetItems[0]["Rows"])
            {
                token.ThrowIfCancellationRequested();

                string cosmeticId = item.Name;

                if (outfitsToIgnore.Contains(cosmeticId)) continue;

                JArray cosmeticPieces = item.Value["OutfitItems"];

                string cosmeticIdLower = cosmeticId.ToLower();
                if (CatalogDictionary.TryGetValue(cosmeticIdLower, out int matchingIndex))
                {
                    string rarity = CosmeticUtils.FindRarityInCatalog(CatalogData, CatalogDictionary, matchingIndex);
                    List<Dictionary<string, int>> prices = CosmeticUtils.CalculateOutfitPrices(CatalogData, CatalogDictionary, cosmeticPieces);

                    string gameIconPath = item.Value["UIData"]["IconFilePathList"][0];
                    string iconPath = StringUtils.AddRootDirectory(gameIconPath, "/images/");

                    bool purchasable = CatalogData[matchingIndex]["purchasable"];

                    bool isLinked = CatalogData[matchingIndex]["metaData"]["unbreakable"];

                    string characterString = CatalogData[matchingIndex]["metaData"]["character"].ToString().ToLower();
                    int? characterIndex = CosmeticUtils.CharacterStringToIndex(characterString);

                    double discountPercentage = CatalogData[matchingIndex]["metaData"]["discountPercentage"];

                    DateTime releaseDate = CatalogData[matchingIndex]["metaData"]["releaseDate"];

                    List<LocalizationEntry> collectionName = CosmeticUtils.GetCollectionName(item);

                    string inclusionVersionRaw = item.Value["InclusionVersion"];
                    string inclusionVersion = CosmeticUtils.TrimInclusionVersion(inclusionVersionRaw);

                    string descriptionKey = CosmeticUtils.ParseCosmeticDescription(item.Value, "Key");

                    string eventId = CosmeticUtils.GrabEventIdForOutfit(CatalogData, CatalogDictionary, cosmeticPieces);

                    DateTime? limitedTimeEndDate = CosmeticUtils.GrabLimitedTimeEndDate(CatalogData, matchingIndex);

                    string customizedAudioStateCollection = item.Value["CustomizedAudioStateCollection"];

                    Dictionary<string, List<LocalizationEntry>> localizationModel = new()
                    {
                        ["CosmeticName"] =
                        [
                            new()
                            {
                                Key = item.Value["UIData"]["DisplayName"]["Key"],
                                SourceString = item.Value["UIData"]["DisplayName"]["SourceString"],

                            }
                        ],
                        ["Description"] =
                        [
                            new()
                            {
                                Key = descriptionKey,
                                SourceString = CosmeticUtils.ParseCosmeticDescription(item.Value, "SourceString"),

                            }
                        ],
                    };

                    // Add "CollectionName" only if collectionName is not null or empty
                    if (collectionName != null && collectionName.Count > 0)
                    {
                        localizationModel["CollectionName"] = collectionName;
                    }

                    LocalizationData.TryAdd(cosmeticId, localizationModel);

                    Outfit model = new()
                    {
                        CosmeticId = cosmeticId,
                        CosmeticName = "",
                        Description = "",
                        CollectionName = "",
                        IconFilePathList = iconPath,
                        EventId = eventId,
                        Type = "outfit",
                        Character = characterIndex,
                        Unbreakable = isLinked,
                        Purchasable = purchasable,
                        ReleaseDate = releaseDate,
                        LimitedTimeEndDate = limitedTimeEndDate,
                        Rarity = rarity,
                        OutfitItems = cosmeticPieces,
                        InclusionVersion = inclusionVersion,
                        CustomizedAudioStateCollection = customizedAudioStateCollection,
                        IsDiscounted = false,
                        DiscountPercentage = discountPercentage,
                        Prices = prices
                    };

                    parsedCosmeticsDB.TryAdd(cosmeticId, model);
                }
            }
        }

        return parsedCosmeticsDB;
    }

    private static readonly string[] customizationItemsToIgnore =
    [
        "NK_Torso01_Mods",
        "CM_Torso04_Mods",
        "DF_Torso04_Mods",
        "JP_Torso04_Mods",
        "MT_Torso04_Mods",
        "Default_Badge",
        "Default_Banner"
    ];
    private static Dictionary<string, object> ParseCustomizationItems(Dictionary<string, object> parsedCosmeticsDB, CancellationToken token)
    {
        string[] filePaths = Helpers.FindFilePathsInExtractedAssetsCaseInsensitive("CustomizationItemDB.json");

        foreach (string filePath in filePaths)
        {
            string packagePath = StringUtils.StripExtractedAssetsDir(filePath);
            LogsWindowViewModel.Instance.AddLog($"Processing: {packagePath}", Logger.LogTags.Info, Logger.ELogExtraTag.Cosmetics);

            var assetItems = FileUtils.LoadDynamicJson(filePath);
            if ((assetItems?[0]?["Rows"]) == null)
            {
                continue;
            }

            foreach (var item in assetItems[0]["Rows"])
            {
                token.ThrowIfCancellationRequested();

                string cosmeticId = item.Name;

                if (customizationItemsToIgnore.Contains(cosmeticId)) continue;

                string cosmeticIdLower = cosmeticId.ToLower();

                List<Dictionary<string, int>> prices = [];
                bool purchasable = false;
                DateTime releaseDate = new();
                string? eventId = null;
                DateTime? limitedTimeEndDate = null;
                if (CatalogDictionary.TryGetValue(cosmeticIdLower, out int matchingIndex))
                {
                    prices = CosmeticUtils.GrabCustomizationItemPrices(CatalogData[matchingIndex]["defaultCost"]);
                    purchasable = CatalogData[matchingIndex]["purchasable"];
                    releaseDate = CatalogData[matchingIndex]["metaData"]["releaseDate"];
                    eventId = CatalogData[matchingIndex]["metaData"]["eventID"];
                    limitedTimeEndDate = CosmeticUtils.GrabLimitedTimeEndDate(CatalogData, matchingIndex);
                }

                int characterIndex = item.Value["AssociatedCharacter"];

                string category = item.Value["Category"];
                string type = StringUtils.DoubleDotsSplit(category);

                List<LocalizationEntry> collectionName = CosmeticUtils.GetCollectionName(item);

                string gameIconPath = item.Value["UIData"]["IconFilePathList"][0];
                string iconPath = StringUtils.AddRootDirectory(gameIconPath, "/images/");

                string rarityString = item.Value["Rarity"];
                string rarity = StringUtils.DoubleDotsSplit(rarityString);

                string itemMeshPath = item.Value["ItemMesh"]["AssetPathName"];
                string modelDataPath = StringUtils.ModifyPath(itemMeshPath, "json", false, characterIndex);
                string fullModelDataPath = StringUtils.AddRootDirectory(modelDataPath, "/assets/");

                string[] cosmeticsWithoutModels = ["Badge", "Banner"];
                if (cosmeticsWithoutModels.Contains(type))
                {
                    fullModelDataPath = "None";
                }

                string secondaryIcon = item.Value["UIData"]["SecondaryIcon"];

                string roleString = item.Value["AssociatedRole"];
                string role = StringUtils.StringSplitVE(roleString);

                string inclusionVersionRaw = item.Value["InclusionVersion"];
                string inclusionVersion = CosmeticUtils.TrimInclusionVersion(inclusionVersionRaw);

                dynamic accessoriesData = item.Value["SocketAttachements"];
                dynamic materialsMap = item.Value["MaterialsMap"];
                dynamic texturesMap = item.Value["TexturesMap"];

                List<LocalizationEntry> searchTags = CosmeticUtils.ConstructSearchTags(item);

                if (modelDataPath != "None")
                {
                    ModelData.CreateModelData(modelDataPath, cosmeticId, characterIndex, type, accessoriesData, materialsMap, texturesMap);
                }
                else if (modelDataPath == "None")
                {
                    fullModelDataPath = "None";
                }

                string prefixModifier = item.Value["Prefix"];
                string prefix = StringUtils.DoubleDotsSplit(prefixModifier);

                string descriptionKey = CosmeticUtils.ParseCosmeticDescription(item.Value, "Key");

                bool isInStore = item.Value["IsInStore"];
                bool isEntitledByDefault = item.Value["IsEntitledByDefault"];

                Dictionary<string, List<LocalizationEntry>> localizationModel = new()
                {
                    ["CosmeticName"] =
                    [
                        new()
                        {
                            Key = item.Value["UIData"]["DisplayName"]["Key"],
                            SourceString = item.Value["UIData"]["DisplayName"]["SourceString"],
                        }
                    ],
                    ["Description"] =
                    [
                        new()
                        {
                            Key = descriptionKey,
                            SourceString = CosmeticUtils.ParseCosmeticDescription(item.Value, "SourceString"),
                        }
                    ],
                    ["SearchTags"] = searchTags
                };

                // Add "CollectionName" only if collectionName is not null or empty
                if (collectionName != null && collectionName.Count > 0)
                {
                    localizationModel["CollectionName"] = collectionName;
                }

                LocalizationData.TryAdd(cosmeticId, localizationModel);

                CustomzatiomItem model = new()
                {
                    CosmeticId = cosmeticId,
                    CosmeticName = "",
                    Description = "",
                    IconFilePathList = iconPath,
                    SearchTags = [],
                    SecondaryIcon = secondaryIcon,
                    ModelDataPath = fullModelDataPath,
                    CollectionName = "",
                    InclusionVersion = inclusionVersion,
                    EventId = eventId,
                    Role = role,
                    Type = type,
                    Character = characterIndex,
                    Rarity = rarity,
                    Prefix = prefix,
                    Purchasable = purchasable,
                    IsInStore = isInStore,
                    IsEntitledByDefault = isEntitledByDefault,
                    ReleaseDate = releaseDate,
                    LimitedTimeEndDate = limitedTimeEndDate,
                    Prices = prices
                };

                parsedCosmeticsDB.TryAdd(cosmeticId, model);
            }
        }

        return parsedCosmeticsDB;
    }

    private static Dictionary<string, object> AssignRiftData(Dictionary<string, object> parsedCosmeticsDB)
    {
        Dictionary<string, string> riftCosmeticsList = [];

        if (RiftData != null)
        {
            foreach (var rift in RiftData)
            {
                string tomeId = rift.Key;
                foreach (var tierInfo in rift.Value.TierInfo)
                {
                    if (tierInfo.Free != null)
                    {
                        foreach (var freeItem in tierInfo.Free)
                        {
                            if (freeItem.Type == "inventory")
                            {
                                string cosmeticId = freeItem.Id;
                                riftCosmeticsList[cosmeticId] = tomeId;
                            }
                        }
                    }

                    if (tierInfo.Premium != null)
                    {
                        foreach (var premiumItem in tierInfo.Premium)
                        {
                            if (premiumItem.Type == "inventory")
                            {
                                string cosmeticId = premiumItem.Id;
                                riftCosmeticsList[cosmeticId] = tomeId;
                            }
                        }
                    }
                }
            }
        }

        foreach (var item in parsedCosmeticsDB)
        {
            if (item.Value is Outfit riftCosmetic)
            {
                string matchingPieceId = riftCosmetic.OutfitItems[0].ToString();
                if (riftCosmeticsList.TryGetValue(matchingPieceId, out string? value))
                {
                    string tomeId = value;
                    riftCosmetic.TomeId = tomeId;
                }
            }
            else if (riftCosmeticsList.TryGetValue(item.Key, out string? tomeId))
            {
                dynamic value = item.Value;
                value.TomeId = tomeId;
            }
        }

        return parsedCosmeticsDB;
    }

    // Currencies should be separated from cosmetics
    // but this makes it easier to populate Rifts data
    private static Dictionary<string, object> AppendStaticCurrencies(Dictionary<string, object> parsedCosmeticsDB)
    {
        void AddCurrencyWithLocalization(string id, string nameKey, string iconFilePath)
        {
            Currency currency = new()
            {
                Type = "Currency",
                CosmeticId = id,
                CosmeticName = "",
                Description = "",
                IconFilePathList = iconFilePath,
                TomeId = null
            };
            parsedCosmeticsDB.Add(id, currency);

            Dictionary<string, List<LocalizationEntry>> localizationModel = new()
            {
                ["CosmeticName"] =
                [
                    new()
                    {
                        Key = nameKey,
                        SourceString = id
                    }
                ]
            };

            LocalizationData.TryAdd(id, localizationModel);
        }

        // Add the currencies using the helper method with localization
        AddCurrencyWithLocalization("cellsPack_25", "AuricCells", "/images/Currency/AuricCells_Icon.png");
        AddCurrencyWithLocalization("cellsPack_50", "AuricCells", "/images/Currency/AuricCells_Icon.png");
        AddCurrencyWithLocalization("cellsPack_75", "AuricCells", "/images/Currency/AuricCells_Icon.png");
        AddCurrencyWithLocalization("HalloweenEventCurrency", "CURRENCY_HalloweenEventCurrency_NAME", "/images/Currency/HalloweenEventCurrency_Icon.png");
        AddCurrencyWithLocalization("BonusBloodpoints", "4424FA7046950159B97A5395900A95B9", "/images/Currency/BloodpointsIcon.png");
        AddCurrencyWithLocalization("WinterEventCurrency", "CURRENCY_WinterEventCurrency_NAME", "/images/Currency/WinterEventCurrency_Icon.png");
        AddCurrencyWithLocalization("SpringEventCurrency", "CURRENCY_SpringEventCurrency_NAME", "/images/Currency/SpringEventCurrency_Icon.png");
        AddCurrencyWithLocalization("AnniversaryEventCurrency", "CURRENCY_AnniversaryEventCurrency_NAME", "/images/Currency/AnniversaryEventCurrency_Icon.png");
        AddCurrencyWithLocalization("Shards", "Shards", "/images/Currency/Shards_Icon.png");

        return parsedCosmeticsDB;
    }

    private static void ParseLocalizationAndSave(Dictionary<string, object> parsedCosmeticsDB, CancellationToken token)
    {
        LogsWindowViewModel.Instance.AddLog($"Starting localization process..", Logger.LogTags.Info, Logger.ELogExtraTag.Cosmetics);

        string[] filePaths = Directory.GetFiles(Path.Combine(GlobalVariables.rootDir, "Dependencies", "Locres"), "*.json", SearchOption.TopDirectoryOnly);

        foreach (string filePath in filePaths)
        {
            token.ThrowIfCancellationRequested();

            string jsonString = File.ReadAllText(filePath);
            string fileName = Path.GetFileName(filePath);
            string langKey = StringUtils.LangSplit(fileName);

            Dictionary<string, string> languageKeys = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString) ?? throw new Exception($"Failed to load following locres file: {langKey}.");

            var objectString = JsonConvert.SerializeObject(parsedCosmeticsDB);
            Dictionary<string, object> localizedCosmeticsDB = JsonConvert.DeserializeObject<Dictionary<string, object>>(objectString) ?? [];

            Helpers.LocalizeDB(localizedCosmeticsDB, LocalizationData, languageKeys, langKey);

            CosmeticUtils.AddAmountToCurrencyPacks(localizedCosmeticsDB);

            string outputPath = Path.Combine(GlobalVariables.pathToParsedData, GlobalVariables.versionWithBranch, langKey, "Cosmetics.json");

            FileWriter.SaveParsedDB(localizedCosmeticsDB, outputPath, Logger.ELogExtraTag.Cosmetics);
        }
    }
}