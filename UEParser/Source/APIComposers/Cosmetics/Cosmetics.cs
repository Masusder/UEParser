﻿using System;
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
using UEParser.Services;

namespace UEParser.APIComposers;

public class Cosmetics
{
    private static readonly Dictionary<string, Dictionary<string, List<LocalizationEntry>>> LocalizationData = [];

    public static async Task InitializeCosmeticsDb(CancellationToken token)
    {
        await Task.Run(() =>
        {
            DataInitializer.InitializeData([
                DataInitializer.DataToLoad.Catalog,
                DataInitializer.DataToLoad.Rifts,
                DataInitializer.DataToLoad.CatalogDictionary,
                DataInitializer.DataToLoad.Characters,
                DataInitializer.DataToLoad.CustomizationCategories
            ]);

            Dictionary<string, object> parsedCosmeticsDb = [];

            LogsWindowViewModel.Instance.AddLog("Starting parsing process..", Logger.LogTags.Info, Logger.ELogExtraTag.Cosmetics);

            parsedCosmeticsDb = ParseOutfits(parsedCosmeticsDb, token);
            parsedCosmeticsDb = ParseCustomizationItems(parsedCosmeticsDb, token);
            parsedCosmeticsDb = AssignAdditionalProperties(parsedCosmeticsDb);
            parsedCosmeticsDb = AppendStaticCurrencies(parsedCosmeticsDb);

            LogsWindowViewModel.Instance.AddLog($"Parsed total of {parsedCosmeticsDb.Count} items.", Logger.LogTags.Info, Logger.ELogExtraTag.Cosmetics);

            ParseLocalizationAndSave(parsedCosmeticsDb, token);
        }, token);
    }

    public static Dictionary<string, object> ParseOutfits(Dictionary<string, object> parsedCosmeticsDb, CancellationToken token)
    {
        string[] filePaths = Helpers.FindFilePathsInExtractedAssetsCaseInsensitive("OutfitDB.json");

        // K24_outfit_01 shouldn't exist and follows invalid format
        // Laurie_outfit_006 already exists under different ID
        // TR_outfit_011 isn't present in catalog
        // MT_outfit_022_CS already exists under different ID
        string[] outfitsToIgnore = ["K24_outfit_01", "Laurie_outfit_006", "TR_outfit_011", "MT_outfit_022_CS"];

        var CatalogData = DataInitializer.CatalogData;
        var CatalogDictionary = DataInitializer.CatalogDictionary;
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

                    parsedCosmeticsDb.TryAdd(cosmeticId, model);
                }
            }
        }

        return parsedCosmeticsDb;
    }

    private static readonly string[] CustomizationItemsToIgnore =
    [
        "NK_Torso01_Mods",
        "CM_Torso04_Mods",
        "DF_Torso04_Mods",
        "JP_Torso04_Mods",
        "MT_Torso04_Mods",
        "Default_Badge",
        "Default_Banner"
    ];
    private static Dictionary<string, object> ParseCustomizationItems(
        Dictionary<string, object> parsedCosmeticsDb,
        CancellationToken token)
    {
        string[] filePaths = Helpers.FindFilePathsInExtractedAssetsCaseInsensitive("CustomizationItemDB.json");

        var CatalogData = DataInitializer.CatalogData;
        var CatalogDictionary = DataInitializer.CatalogDictionary;
        var CustomizationCategories = DataInitializer.CustomizationCategories;
        var CharacterData = DataInitializer.CharacterData;
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

                if (CustomizationItemsToIgnore.Contains(cosmeticId)) continue;

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

                string categoryRaw = item.Value["Category"];
                string type = StringUtils.DoubleDotsSplit(categoryRaw);

                string category = type;
                if (characterIndex != -1 && CharacterData.TryGetValue(characterIndex.ToString(), out var character) && character?.CustomizationCategories?.Length > 0)
                {
                    foreach (var customizationCategory in character.CustomizationCategories)
                    {
                        if (CustomizationCategories.TryGetValue(customizationCategory, out var categoryType) && categoryType == type)
                        {
                            category = customizationCategory;
                            break;
                        }
                    }
                }

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
                if (!string.IsNullOrEmpty(secondaryIcon))
                {
                    secondaryIcon = StringUtils.AddRootDirectory(secondaryIcon, "/images/");
                }

                string roleString = item.Value["AssociatedRole"];
                string role = StringUtils.StringSplitVe(roleString);

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
                    Category = category,
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

                parsedCosmeticsDb.TryAdd(cosmeticId, model);
            }
        }

        return parsedCosmeticsDb;
    }

    private static Dictionary<string, object> AssignAdditionalProperties(Dictionary<string, object> parsedCosmeticsDb)
    {
        Dictionary<string, string> riftCosmeticsList = [];

        var RiftData = DataInitializer.RiftData;
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

        foreach (var item in parsedCosmeticsDb)
        {
            if (item.Value is Outfit cosmetic)
            {
                string matchingPieceId = cosmetic.OutfitItems[0].ToString();

                if (parsedCosmeticsDb[matchingPieceId] is CustomzatiomItem matchingPiece)
                {
                    cosmetic.Prefix = matchingPiece.Prefix;
                }

                if (riftCosmeticsList.TryGetValue(matchingPieceId, out string? value))
                {
                    string tomeId = value;
                    cosmetic.TomeId = tomeId;
                }
                else if (riftCosmeticsList.TryGetValue(item.Key, out string? tomeId))
                {
                    cosmetic.TomeId = tomeId;
                }
            }
            else if (riftCosmeticsList.TryGetValue(item.Key, out string? tomeId))
            {
                dynamic value = item.Value;
                value.TomeId = tomeId;
            }
        }

        return parsedCosmeticsDb;
    }

    // Currencies should be separated from cosmetics
    // but this makes it easier to populate Rifts data
    private static Dictionary<string, object> AppendStaticCurrencies(Dictionary<string, object> parsedCosmeticsDb)
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
            parsedCosmeticsDb.Add(id, currency);

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

        return parsedCosmeticsDb;
    }

    private static void ParseLocalizationAndSave(Dictionary<string, object> parsedCosmeticsDb, CancellationToken token)
    {
        LogsWindowViewModel.Instance.AddLog($"Starting localization process..", Logger.LogTags.Info, Logger.ELogExtraTag.Cosmetics);

        string[] filePaths = Directory.GetFiles(Path.Combine(GlobalVariables.RootDir, "Dependencies", "Locres"), "*.json", SearchOption.TopDirectoryOnly);

        foreach (string filePath in filePaths)
        {
            token.ThrowIfCancellationRequested();

            string jsonString = File.ReadAllText(filePath);
            string fileName = Path.GetFileName(filePath);
            string langKey = StringUtils.LangSplit(fileName);

            Dictionary<string, string> languageKeys = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString) ?? throw new Exception($"Failed to load following locres file: {langKey}.");

            var objectString = JsonConvert.SerializeObject(parsedCosmeticsDb);
            Dictionary<string, object> localizedCosmeticsDb = JsonConvert.DeserializeObject<Dictionary<string, object>>(objectString) ?? [];

            Helpers.LocalizeDb(localizedCosmeticsDb, LocalizationData, languageKeys, langKey);

            CosmeticUtils.AddAmountToCurrencyPacks(localizedCosmeticsDb);

            string outputPath = Path.Combine(GlobalVariables.PathToParsedData, GlobalVariables.VersionWithBranch, langKey, "Cosmetics.json");

            FileWriter.SaveParsedDb(localizedCosmeticsDb, outputPath, Logger.ELogExtraTag.Cosmetics);
        }
    }
}