using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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

            // Parse game specific data
            JObject? gameSpecificDataObject = rewardObject["gameSpecificData"] as JObject;
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

        // Define the regex pattern to match both formats
        string pattern = @"/Game/UI/UMGAssets/Icons/Banners/BundleBanners/(SpecialPack|ChapterBundles)/([^./]+)\.\w+";

        // Replace the matched pattern with the desired format
        string transformedString = Regex.Replace(input, pattern, "/images/UI/Icons/Banners/BundleBanners/$1/$2.png");

        return transformedString;
    }

    public static Dictionary<string, int> CreateBundlesDictionary(dynamic CatalogData)
    {
        Dictionary<string, int> bundlesDictionary = [];

        for (int i = 0; i < CatalogData.Count; i++)
        {
            string id = CatalogData[i].id;
            bundlesDictionary[id] = i;
        }

        return bundlesDictionary;
    }

    public static void PopulateLocalizationFromApi(Dictionary<string, Bundle> localizedBundlesDB, string langKey, Dictionary<string, int> catalogDictionary, dynamic CatalogData)
    {
        foreach (var bundle in localizedBundlesDB)
        {
            int matchingIndex = catalogDictionary[bundle.Value.Id];

            var bundleTitle = CatalogData[matchingIndex]["metaData"]["specialPackTitle"][langKey];

            bundle.Value.SpecialPackTitle = bundleTitle;
        }
    }
}