using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UEParser.Utils;

namespace UEParser.APIComposers;

public class CosmeticUtils
{
    public static JArray ConstructSearchTags(dynamic item)
    {
        JArray searchTags = [];
        if (item.Value?["SearchTags"] is JArray searchTagsRaw)
        {
            foreach (JToken tag in searchTagsRaw)
            {
                if (tag["Key"] is JToken searchTagKey)
                {
                    searchTags.Add(searchTagKey);
                }
            }
        }

        return searchTags;
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

    public static string ParseCosmeticDescription(dynamic data, string propertyName)
    {
        string? firstDescription = data?["UIData"]?["Description"]?[propertyName];
        string? secondDescription = data?["CollectionDescription"]?[propertyName];
        string? localizedDescription = data?["UIData"]?["Description"]?["LocalizedString"];

        if (firstDescription != null && localizedDescription == "\t")
        {
            return secondDescription ?? throw new Exception("Not found cosmetic description.");
        }
        else
        {
            return firstDescription ?? secondDescription ?? throw new Exception("Not found cosmetic description.");
        }
    }
}