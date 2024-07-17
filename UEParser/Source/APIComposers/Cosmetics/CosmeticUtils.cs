using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UEParser.Utils;
using UEParser.Models;

namespace UEParser.APIComposers;

public class CosmeticUtils
{
    public static List<LocalizationEntry> ConstructSearchTags(dynamic item)
    {
        List<LocalizationEntry> searchTags = [];
        if (item.Value?["SearchTags"] is JArray searchTagsRaw)
        {
            foreach (JToken tag in searchTagsRaw)
            {
                string? tagKey = tag["Key"]?.ToString();
                string? tagSourceString = tag["SourceString"]?.ToString();

                if (tagKey != null && tagSourceString != null)
                {
                    LocalizationEntry entry = new()
                    {
                        Key = tagKey,
                        SourceString = tagSourceString
                    };

                    searchTags.Add(entry);
                }
            }
        }

        return searchTags;
    }

    public static List<LocalizationEntry> GetCollectionName(dynamic asset)
    {
        var localizationEntries = new List<LocalizationEntry>();
        // Check if "CollectionName" exists in the root of the asset
        if (asset.Value["CollectionName"] != null)
        {
            // Check if "LocalizedString" exists within "CollectionName"
            if (asset.Value["CollectionName"]["LocalizedString"] != null)
            {
                LocalizationEntry entry = new()
                {
                    Key = asset.Value["CollectionName"]["Key"].ToString(),
                    SourceString = asset.Value["CollectionName"]["SourceString"].ToString()
                };
                localizationEntries.Add(entry);

                return localizationEntries;
            }
        }

        // Check if "UIData" exists
        if (asset.Value["UIData"] != null && asset.Value["UIData"]["CollectionName"] != null)
        {
            // Check if "LocalizedString" exists within nested "CollectionName"
            if (asset.Value["UIData"]["CollectionName"]["LocalizedString"] != null)
            {
                LocalizationEntry entry = new()
                {
                    Key = asset.Value["UIData"]["CollectionName"]["Key"].ToString(),
                    SourceString = asset.Value["UIData"]["CollectionName"]["SourceString"].ToString()
                };
                localizationEntries.Add(entry);

                return localizationEntries;
            }
        }

        // If any part of the path is missing or null, return empty string
        return localizationEntries;
    }

    public static DateTime? GrabLimitedTimeEndDate(dynamic catalogData, int matchingIndex)
    {
        DateTime? limitedTimeEndDate = null;

        if (catalogData[matchingIndex]["metaData"] != null &&
            catalogData[matchingIndex]["metaData"]["limitedTimeEndDate"] != null)
        {
            string endDateStr = catalogData[matchingIndex]["metaData"]["limitedTimeEndDate"].ToString();

            if (!string.IsNullOrWhiteSpace(endDateStr) && DateTime.TryParse(endDateStr, out DateTime newEndDate))
            {
                limitedTimeEndDate = newEndDate;
            }
        }

        return limitedTimeEndDate;
    }

    public static Dictionary<string, int> CreateCatalogDictionary(dynamic catalogData)
    {
        Dictionary<string, int> catalogDictionary = [];

        int index = 0;
        foreach (var item in catalogData)
        {
            if (item.ContainsKey("id"))
            {
                catalogDictionary[item["id"].ToString().ToLower()] = index;
            }
            index++;
        }

        return catalogDictionary;
    }

    public static string FindRarityInCatalog(dynamic catalogData, Dictionary<string, int> catalogDictionary, int matchingIndex)
    {
        string rarity = "Common";
        if (catalogData[matchingIndex].ContainsKey("metaData") && catalogData[matchingIndex]["metaData"].ContainsKey("items") && catalogData[matchingIndex]["metaData"]["items"] != null)
        {
            string pieceId = catalogData[matchingIndex]["metaData"]["items"][0];

            if (catalogDictionary.TryGetValue(pieceId.ToLower(), out int matchingRarityIndex))
            {
                rarity = catalogData[matchingRarityIndex]["metaData"]["rarity"];
            }
        }

        return rarity;
    }

    public static string? GrabEventIdForOutfit(dynamic catalogData, Dictionary<string, int> catalogDictionary, JArray cosmeticPieces)
    {
        string? eventId = null;
        foreach (string? cosmeticPiece in cosmeticPieces.Select(v => (string?)v))
        {
            if (cosmeticPiece == null) continue;

            if (catalogDictionary.TryGetValue(cosmeticPiece.ToLower(), out int matchingPieceIndex))
            {
                JArray? defaultCosts = catalogData[matchingPieceIndex]["defaultCost"];
                eventId = catalogData[matchingPieceIndex]["metaData"]["eventID"];
            }
        }

        return eventId;
    }

    public static List<Dictionary<string, int>> CalculateOutfitPrices(dynamic catalogData, Dictionary<string, int> catalogDictionary, JArray cosmeticPieces)
    {
        Dictionary<string, int> calculatedPrices = [];

        foreach (string? cosmeticPiece in cosmeticPieces.Select(v => (string?)v))
        {
            if (cosmeticPiece != null)
            {
                if (catalogDictionary.TryGetValue(cosmeticPiece.ToLower(), out int matchingPieceIndex))
                {
                    JArray defaultCosts = catalogData[matchingPieceIndex]["defaultCost"];

                    if (defaultCosts != null)
                    {
                        foreach (var currency in defaultCosts)
                        {
                            string? currencyIdString = (string?)currency.SelectToken("currencyId");
                            JToken? priceToken = currency.SelectToken("price");
                            int priceInt = priceToken?.Value<int>() ?? 0;

                            if (currencyIdString == null) continue;

                            if (calculatedPrices.ContainsKey(currencyIdString))
                            {
                                calculatedPrices[currencyIdString] += priceInt;
                            }
                            else
                            {
                                calculatedPrices[currencyIdString] = priceInt;
                            }
                        }
                    }
                }
            }
        }

        List<Dictionary<string, int>> resultList = calculatedPrices
        .Select(kv => new Dictionary<string, int> { { kv.Key, kv.Value } })
        .ToList();

        return resultList;
    }

    public static List<Dictionary<string, int>> GrabCustomizationItemPrices(JArray defaultCosts)
    {
        Dictionary<string, int> calculatedPrices = [];

        if (defaultCosts != null)
        {
            foreach (var currency in defaultCosts)
            {
                string? currencyIdString = (string?)currency.SelectToken("currencyId");
                JToken? priceToken = currency.SelectToken("price");
                int priceInt = priceToken?.Value<int>() ?? 0;

                if (currencyIdString == null) continue;

                if (calculatedPrices.ContainsKey(currencyIdString))
                {
                    calculatedPrices[currencyIdString] += priceInt;
                }
                else
                {
                    calculatedPrices[currencyIdString] = priceInt;
                }
            }
        }

        List<Dictionary<string, int>> resultList = calculatedPrices
        .Select(kv => new Dictionary<string, int> { { kv.Key, kv.Value } })
        .ToList();

        return resultList;
    }

    public static int? CharacterStringToIndex(string characterString)
    {
        var charactersData = FileUtils.LoadDynamicJson(Path.Combine(GlobalVariables.rootDir, "Dependencies", "HelperComponents", "characterIds.json"));

        int? characterIndex = charactersData?[characterString];

        return characterIndex;
    }

    public static string TrimInclusionVersion(string inclusionVersion)
    {
        return inclusionVersion.TrimStart(' ').TrimEnd(' ');
    }

    // Deprecated cosmetic descriptions use both description and collection description combined
    // For our purpose we just use one description
    public static string ParseCosmeticDescription(dynamic data, string propertyName)
    {
        string? firstDescription = data?["UIData"]?["Description"]?[propertyName];
        string? secondDescription = data?["CollectionDescription"]?[propertyName];
        string? localizedDescription = data?["UIData"]?["Description"]?["LocalizedString"];

        if (firstDescription != null && localizedDescription == "\t") // Turkish localization is missing '\t' for some cosmetics therefore it needs to be checked
        {
            return secondDescription ?? throw new Exception("Not found cosmetic description.");
        }
        else
        {
            return firstDescription ?? secondDescription ?? throw new Exception("Not found cosmetic description.");
        }
    }
}