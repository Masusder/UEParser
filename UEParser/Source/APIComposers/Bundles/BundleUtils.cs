﻿using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using UEParser.Models;
using UEParser.Utils;

namespace UEParser.APIComposers;

public class BundleUtils
{
    public static List<FullPrice> ParseFullPrice(JArray fullPriceArray)
    {
        List<FullPrice> fullPrices = [];
        foreach (JObject priceObject in fullPriceArray.Cast<JObject>())
        {
            string? currencyId = priceObject["currencyId"]?.ToString();
            int price = priceObject["price"]?.ToObject<int>() ?? 0;

            if (currencyId != null && price != 0)
            {
                FullPrice fullPrice = new()
                {
                    CurrencyId = currencyId,
                    Price = price
                };

                fullPrices.Add(fullPrice);
            }
        }

        return fullPrices;
    }

    public static ImageComposition? ParseImageComposition(JObject? imageCompositionObject)
    {
        if (imageCompositionObject == null) return null;

        int maxItemCount = imageCompositionObject.Value<int>("maxItemCount");
        bool overrideDefaults = imageCompositionObject.Value<bool>("overrideDefaults");
        string? typeRaw = imageCompositionObject.Value<string>("type");

        if (typeRaw == null) return null;

        string type = StringUtils.DoubleDotsSplit(typeRaw);

        ImageComposition imageComposition = new()
        {
            MaxItemCount = maxItemCount,
            OverrideDefaults = overrideDefaults,
            Type = type
        };

        return imageComposition;
    }

    public static List<ConsumptionRewards> ParseConsumptionRewards(JArray consumptionRewardArray)
    {
        List<ConsumptionRewards> consumptionRewardsList = [];

        foreach (JObject rewardObject in consumptionRewardArray.Cast<JObject>())
        {
            int amount = rewardObject["amount"]?.ToObject<int>() ?? 0;
            string? id = rewardObject["id"]?.ToString();
            string? type = rewardObject["type"]?.ToString();

            JObject? gameSpecificDataObject = rewardObject["gameSpecificData"] as JObject;
            bool hasPriorityForPackImageComposition = gameSpecificDataObject?["hasPriorityForPackImageComposition"]?.ToObject<bool>() ?? false;
            bool ignoreOwnership = gameSpecificDataObject?["ignoreOwnership"]?.ToObject<bool>() ?? false;
            bool includeInOwnership = gameSpecificDataObject?["includeInOwnership"]?.ToObject<bool>() ?? false;
            bool includeInPricing = gameSpecificDataObject?["includeInPricing"]?.ToObject<bool>() ?? false;
            string? gameSpecificDataType = gameSpecificDataObject?["type"]?.ToString();

            // Only create ConsumptionRewards object if all required properties are not null and have valid values
            if (amount != 0 && id != null && type != null && gameSpecificDataType != null)
            {
                ConsumptionRewards consumptionReward = new()
                {
                    Amount = amount,
                    Id = id,
                    Type = type,
                    GameSpecificData = new GameSpecificData
                    {
                        HasPriorityForPackImageComposition = hasPriorityForPackImageComposition,
                        IgnoreOwnership = ignoreOwnership,
                        IncludeInOwnership = includeInOwnership,
                        IncludeInPricing = includeInPricing,
                        Type = gameSpecificDataType
                    }
                };

                consumptionRewardsList.Add(consumptionReward);
            }

        }

        return consumptionRewardsList;
    }

    public static string? TransformImagePath_SpecialPacks(string input)
    {
        if (input == null) return null;

        string pattern = @"/Game/UI/UMGAssets/Icons/Banners/BundleBanners/(SpecialPack|ChapterBundles)/([^./]+)\.\w+";
        string transformedString = Regex.Replace(input, pattern, "/images/UI/Icons/Banners/BundleBanners/$1/$2.png");

        return transformedString;
    }

    public static Dictionary<string, int> CreateBundlesDictionary(dynamic catalogData)
    {
        Dictionary<string, int> bundlesDictionary = [];

        for (int i = 0; i < catalogData.Count; i++)
        {
            string id = catalogData[i].id;
            bundlesDictionary[id] = i;
        }

        return bundlesDictionary;
    }

    public static void PopulateLocalizationFromApi(Dictionary<string, Bundle> localizedBundlesDb, string langKey, Dictionary<string, int> catalogDictionary, dynamic catalogData)
    {
        foreach (var bundle in localizedBundlesDb)
        {
            int matchingIndex = catalogDictionary[bundle.Value.Id];

            var bundleTitle = catalogData[matchingIndex]["metaData"]["specialPackTitle"][langKey];

            bundle.Value.SpecialPackTitle = bundleTitle;
        }
    }
}