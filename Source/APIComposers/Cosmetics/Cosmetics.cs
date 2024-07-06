using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UEParser.Parser;
using UEParser.Utils;
using UEParser.ViewModels;

namespace UEParser.APIComposers;

public class Cosmetics
{
    private static readonly Dictionary<string, Dictionary<string, Models.LocalizationEntry>> localizationData = [];
    private static readonly dynamic catalogData = FileUtils.LoadDynamicJson(Path.Combine(GlobalVariables.rootDir, "Output", "API", GlobalVariables.versionWithBranch, "catalog.json")) ?? throw new Exception("Failed to load catalog data.");
    private static readonly Dictionary<string, int> catalogDictionary = CosmeticUtils.CreateCatalogDictionary(catalogData);

    public static async Task InitializeCosmeticsDB()
    {
        await Task.Run(() =>
        {
            Dictionary<string, object> parsedCosmeticsDB = [];

            LogsWindowViewModel.Instance.AddLog($"[Cosmetics] Starting parsing process..", Logger.LogTags.Info);

            parsedCosmeticsDB = ParseOutfits(parsedCosmeticsDB);
            parsedCosmeticsDB = ParseCustomizationItems(parsedCosmeticsDB);

            ParseLocalizationAndSave(parsedCosmeticsDB);
        });
    }

    public static Dictionary<string, object> ParseOutfits(Dictionary<string, object> parsedCosmeticsDB)
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
            LogsWindowViewModel.Instance.AddLog($"[Cosmetics] Processing: {packagePath}", Logger.LogTags.Info);

            var assetItems = FileUtils.LoadDynamicJson(filePath);
            if ((assetItems?[0]?["Rows"]) == null)
            {
                continue;
            }

            foreach (var item in assetItems[0]["Rows"])
            {
                string cosmeticId = item.Name;

                if (outfitsToIgnore.Contains(cosmeticId)) continue;

                JArray cosmeticPieces = item.Value["OutfitItems"];

                string cosmeticIdLower = cosmeticId.ToLower();
                if (catalogDictionary.TryGetValue(cosmeticIdLower, out int matchingIndex))
                {
                    string rarity = CosmeticUtils.FindRarityInCatalog(catalogData, catalogDictionary, matchingIndex);
                    List<Dictionary<string, int>> prices = CosmeticUtils.CalculateOutfitPrices(catalogData, catalogDictionary, cosmeticPieces);

                    string gameIconPath = item.Value["UIData"]["IconFilePathList"][0];
                    string iconPath = StringUtils.AddRootDirectory(gameIconPath, "/images/");

                    bool purchasable = catalogData[matchingIndex]["purchasable"];

                    bool isLinked = catalogData[matchingIndex]["metaData"]["unbreakable"];

                    string characterString = catalogData[matchingIndex]["metaData"]["character"].ToString().ToLower();
                    int? characterIndex = CosmeticUtils.CharacterStringToIndex(characterString);

                    double discountPercentage = catalogData[matchingIndex]["metaData"]["discountPercentage"];

                    DateTime releaseDate = catalogData[matchingIndex]["metaData"]["releaseDate"];

                    string collectionName = StringUtils.GetCollectionName(item);

                    string inclusionVersionRaw = item.Value["InclusionVersion"];
                    string inclusionVersion = CosmeticUtils.TrimInclusionVersion(inclusionVersionRaw);

                    string descriptionKey = CosmeticUtils.ParseCosmeticDescription(item.Value, "Key");

                    string eventId = CosmeticUtils.GrabEventIdForOutfit(catalogData, catalogDictionary, cosmeticPieces);

                    DateTime? limitedTimeEndDate = CosmeticUtils.GrabLimitedTimeEndDate(catalogData, matchingIndex);

                    Dictionary<string, Models.LocalizationEntry> localizationModel = new()
                    {
                        ["CosmeticName"] = new Models.LocalizationEntry
                        {
                            Key = item.Value["UIData"]["DisplayName"]["Key"],
                            SourceString = item.Value["UIData"]["DisplayName"]["SourceString"]
                        },
                        ["Description"] = new Models.LocalizationEntry
                        {
                            Key = descriptionKey,
                            SourceString = CosmeticUtils.ParseCosmeticDescription(item.Value, "SourceString")
                        }
                    };

                    localizationData.TryAdd(cosmeticId, localizationModel);

                    Models.Outfit model = new()
                    {
                        CosmeticId = cosmeticId,
                        CosmeticName = item.Value["UIData"]["DisplayName"]["LocalizedString"],
                        Description = descriptionKey,
                        CollectionName = collectionName,
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

    private static Dictionary<string, object> ParseCustomizationItems(Dictionary<string, object> parsedCosmeticsDB)
    {
        string[] filePaths = Helpers.FindFilePathsInExtractedAssetsCaseInsensitive("CustomizationItemDB.json");

        foreach (string filePath in filePaths)
        {
            string packagePath = StringUtils.StripExtractedAssetsDir(filePath);
            LogsWindowViewModel.Instance.AddLog($"[Cosmetics] Processing: {packagePath}", Logger.LogTags.Info);

            var assetItems = FileUtils.LoadDynamicJson(filePath);
            if ((assetItems?[0]?["Rows"]) == null)
            {
                continue;
            }

            foreach (var item in assetItems[0]["Rows"])
            {
                string cosmeticId = item.Name;

                if (customizationItemsToIgnore.Contains(cosmeticId)) continue;

                string cosmeticIdLower = cosmeticId.ToLower();

                List<Dictionary<string, int>> prices = [];
                bool purchasable = false;
                DateTime releaseDate = new();
                string? eventId = null;
                DateTime? limitedTimeEndDate = null;
                if (catalogDictionary.TryGetValue(cosmeticIdLower, out int matchingIndex))
                {
                    prices = CosmeticUtils.GrabCustomizationItemPrices(catalogData[matchingIndex]["defaultCost"]);
                    purchasable = catalogData[matchingIndex]["purchasable"];
                    releaseDate = catalogData[matchingIndex]["metaData"]["releaseDate"];
                    eventId = catalogData[matchingIndex]["metaData"]["eventID"];
                    limitedTimeEndDate = CosmeticUtils.GrabLimitedTimeEndDate(catalogData, matchingIndex);
                }

                int characterIndex = item.Value["AssociatedCharacter"];

                string category = item.Value["Category"];
                string type = StringUtils.DoubleDotsSplit(category);

                string collectionName = StringUtils.GetCollectionName(item);

                string gameIconPath = item.Value["UIData"]["IconFilePathList"][0];
                string iconPath = StringUtils.AddRootDirectory(gameIconPath, "/images/");

                string rarityString = item.Value["Rarity"];
                string rarity = StringUtils.DoubleDotsSplit(rarityString);

                string itemMeshPath = item.Value["ItemMesh"]["AssetPathName"];
                string modelDataPath = StringUtils.ModifyPath(itemMeshPath, "json");
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

                JArray searchTags = CosmeticUtils.ConstructSearchTags(item);

                if (modelDataPath != "None")
                {
                    //MeshesData.CreateModelData(modelDataPath, cosmeticId, characterIndex, type, accessoriesData, materialsMap, texturesMap);
                }
                else if (modelDataPath == "None")
                {
                    fullModelDataPath = "None";
                }

                string prefixModifier = item.Value["Prefix"];
                string prefix = StringUtils.DoubleDotsSplit(prefixModifier);

                string descriptionKey = CosmeticUtils.ParseCosmeticDescription(item.Value, "Key");

                Dictionary<string, Models.LocalizationEntry> localizationModel = new()
                {
                    ["CosmeticName"] = new Models.LocalizationEntry
                    {
                        Key = item.Value["UIData"]["DisplayName"]["Key"],
                        SourceString = item.Value["UIData"]["DisplayName"]["SourceString"]
                    },
                    ["Description"] = new Models.LocalizationEntry
                    {
                        Key = descriptionKey,
                        SourceString = CosmeticUtils.ParseCosmeticDescription(item.Value, "SourceString")
                    }
                };

                localizationData.TryAdd(cosmeticId, localizationModel);

                Models.CustomzatiomItem model = new()
                {
                    CosmeticId = cosmeticId,
                    CosmeticName = item.Value["UIData"]["DisplayName"]["LocalizedString"],
                    Description = descriptionKey,
                    IconFilePathList = iconPath,
                    SearchTags = searchTags,
                    SecondaryIcon = secondaryIcon,
                    ModelDataPath = fullModelDataPath,
                    CollectionName = collectionName,
                    InclusionVersion = inclusionVersion,
                    EventId = eventId,
                    Role = role,
                    Type = type,
                    Character = characterIndex,
                    Rarity = rarity,
                    Prefix = prefix,
                    Purchasable = purchasable,
                    ReleaseDate = releaseDate,
                    LimitedTimeEndDate = limitedTimeEndDate,
                    Prices = prices
                };

                parsedCosmeticsDB.TryAdd(cosmeticId, model);
            }
        }

        return parsedCosmeticsDB;
    }

    private static readonly string[] itemsWithoutLocalization = [
        "C_Head01",
        "D_Head01",
        "J_Head01",
        "M_Head01",
        "S01_Head01",
        "DF_Head04",
        "D_Head02",
        "TR_Head03",
        "Default_Badge",
        "Default_Banner"
    ];

    private static void ParseLocalizationAndSave(Dictionary<string, object> parsedCosmeticsDB)
    {
        LogsWindowViewModel.Instance.AddLog($"[Cosmetics] Starting localization process..", Logger.LogTags.Info);

        string[] filePaths = Directory.GetFiles(Path.Combine(GlobalVariables.rootDir, "Dependencies", "Locres"), "*.json", SearchOption.TopDirectoryOnly);

        foreach (string filePath in filePaths)
        {
            string jsonString = File.ReadAllText(filePath);
            string fileName = Path.GetFileName(filePath);
            string langKey = StringUtils.LangSplit(fileName);

            Dictionary<string, string> languageKeys = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString) ?? throw new Exception($"Failed to load following locres file: {langKey}.");

            var objectString = JsonConvert.SerializeObject(parsedCosmeticsDB);
            Dictionary<string, object> localizedCosmeticsDB = JsonConvert.DeserializeObject<Dictionary<string, object>>(objectString) ?? [];

            foreach (var item in localizedCosmeticsDB)
            {
                string cosmeticId = item.Key;
                var localizationDataEntry = localizationData[cosmeticId];

                foreach (var entry in localizationDataEntry)
                {
                    try
                    {
                        string localizedString;
                        if (languageKeys.TryGetValue(entry.Value.Key, out string? langValue))
                        {
                            localizedString = langValue;
                        }
                        else
                        {
                            if (!itemsWithoutLocalization.Contains(cosmeticId))
                            {
                                LogsWindowViewModel.Instance.AddLog($"Missing localization string -> Property: '{entry.Key}', LangKey: '{langKey}', RowId: '{cosmeticId}', FallbackString: '{entry.Value.SourceString}'", Logger.LogTags.Warning);
                            }

                            localizedString = entry.Value.SourceString;
                        }

                        dynamic dynamicItem = item.Value;
                        dynamicItem[entry.Key] = localizedString;
                    }
                    catch (Exception ex)
                    {
                        LogsWindowViewModel.Instance.AddLog($"Missing localization string -> Property: '{entry.Key}', LangKey: '{langKey}', RowId: '{cosmeticId}', FallbackString: '{entry.Value.SourceString}' <- {ex}", Logger.LogTags.Warning);
                    }
                }
            }

            string outputPath = Path.Combine(GlobalVariables.rootDir, "Output", "ParsedData", GlobalVariables.versionWithBranch, langKey, "Cosmetics.json");

            FileWriter.SaveParsedDB(localizedCosmeticsDB, outputPath, "Cosmetics");
        }
    }
}