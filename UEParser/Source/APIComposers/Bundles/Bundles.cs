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

public class Bundles
{
    private static readonly Dictionary<string, Dictionary<string, List<LocalizationEntry>>> LocalizationData = [];
    private static readonly dynamic CatalogData = FileUtils.LoadDynamicJson(Path.Combine(GlobalVariables.PathToKraken, GlobalVariables.VersionWithBranch, "CDN", "catalog.json")) ?? throw new Exception("Failed to load catalog data.");
    private static readonly Dictionary<string, DLC> DlcsData = FileUtils.LoadJsonFileWithTypeCheck<Dictionary<string, DLC>>(Path.Combine(GlobalVariables.PathToParsedData, GlobalVariables.VersionWithBranch, "en", "DLC.json")) ?? throw new Exception("Failed to load parsed DLC data.");
    private static readonly Dictionary<string, Character> CharactersData = FileUtils.LoadJsonFileWithTypeCheck<Dictionary<string, Character>>(Path.Combine(GlobalVariables.PathToParsedData, GlobalVariables.VersionWithBranch, "en", "Characters.json")) ?? throw new Exception("Failed to load parsed Characters data.");

    public static async Task InitializeBundlesDb(CancellationToken token)
    {
        await Task.Run(() =>
        {
            Dictionary<string, Bundle> parsedBundlesDb = [];

            LogsWindowViewModel.Instance.AddLog($"Starting parsing process..", Logger.LogTags.Info, Logger.ELogExtraTag.Bundles);

            ParseBundles(parsedBundlesDb, token);

            LogsWindowViewModel.Instance.AddLog($"Parsed total of {parsedBundlesDb.Count} items.", Logger.LogTags.Info, Logger.ELogExtraTag.Bundles);

            ParseLocalizationAndSave(parsedBundlesDb, token);
        }, token);
    }

    private static readonly string[] IgnoreDlcs =
    [
        "80suitcase",
        "bloodstainedSack",
        "headCase"
    ];
    private static void ParseBundles(Dictionary<string, Bundle> parsedBundlesDb, CancellationToken token)
    {
        foreach (var item in CatalogData)
        {
            token.ThrowIfCancellationRequested();

            JArray typeArray = item["categories"];
            string prettyType = string.Join(" ", typeArray);

            if (prettyType == "specialPack")
            {
                string bundleId = item["id"];

                LogsWindowViewModel.Instance.AddLog($"Processing: {bundleId}", Logger.LogTags.Info, Logger.ELogExtraTag.Bundles);

                bool isLicensedBundle = false;
                if (bundleId.StartsWith("Licensor"))
                {
                    isLicensedBundle = true;
                }

                string dlcId = item["metaData"]["dlcId"];

                DateTime? startDate = item["metaData"]["startDate"];
                DateTime? endDate = item["metaData"]["endDate"];

                int sortOrder = item["metaData"]["sortOrder"];
                int minNumberOfUnownedForPurchase = item["metaData"]["minNumberOfUnownedForPurchase"];
                float discount = item["metaData"]["discount"];

                bool isConsumable = item["consumable"];
                bool purchasable = item["purchasable"];

                JArray fullPriceArray = (JArray)item["metaData"]["fullPrice"];
                List<FullPrice> fullPrices = BundleUtils.ParseFullPrice(fullPriceArray);

                List<ConsumptionRewards> consumptionRewards = BundleUtils.ParseConsumptionRewards((JArray)item["consumptionReward"]);
                JArray segmentationTags = item["metaData"]["segmentationTags"];

                string? imagePath = item["metaData"]["imagePath"];

                bool bundleContainsCharacterReward = consumptionRewards.Any(r => r.GameSpecificData.Type == "Character");

                if (!bundleContainsCharacterReward)
                {
                    isLicensedBundle = false;
                }

                bool isChapterBundle = false;
                string[] dlcsToIgnore = ["80suitcase", "bloodstainedSack", "headCase"];
                if ((dlcId != null && !dlcsToIgnore.Contains(dlcId)) && bundleContainsCharacterReward || isLicensedBundle)
                {
                    isChapterBundle = true;
                }

                if (!isLicensedBundle)
                {
                    imagePath = BundleUtils.TransformImagePath_SpecialPacks(imagePath);
                }
                else if (isLicensedBundle)
                {
                    for (int i = 0; i < consumptionRewards.Count; i++)
                    {
                        if (consumptionRewards[i].GameSpecificData.Type == "Character")
                        {
                            string characterId = consumptionRewards[i].Id;
                            Character? character = CharactersData.FirstOrDefault(c => c.Value.Id == characterId).Value;

                            if (character != null)
                            {
                                string licensorDlcId = character.DLC;
                                if (DlcsData.TryGetValue(licensorDlcId, out DLC? dlcValue))
                                {
                                    string dlcBanner = dlcValue.BannerImage;
                                    imagePath = dlcBanner;
                                }
                            }
                        }
                    }
                }

                JObject? imageCompositionObject = item["metaData"]["imageComposition"];
                var imageComposition = BundleUtils.ParseImageComposition(imageCompositionObject);

                Bundle model = new()
                {
                    Id = bundleId,
                    SpecialPackTitle = "",
                    ImagePath = imagePath,
                    StartDate = startDate,
                    EndDate = endDate,
                    SortOrder = sortOrder,
                    Purchasable = purchasable,
                    IsChapterBundle = isChapterBundle,
                    IsLicensedBundle = isLicensedBundle,
                    MinNumberOfUnownedForPurchase = minNumberOfUnownedForPurchase,
                    DlcId = dlcId,
                    FullPrice = fullPrices,
                    ImageComposition = imageComposition,
                    Discount = discount,
                    ConsumptionRewards = consumptionRewards,
                    Consumable = isConsumable,
                    SegmentationTags = segmentationTags,
                    Type = prettyType
                };

                parsedBundlesDb.Add(bundleId, model);
            }
        }
    }

    private static void ParseLocalizationAndSave(Dictionary<string, Bundle> parsedBundlesDb, CancellationToken token)
    {
        LogsWindowViewModel.Instance.AddLog($"Starting localization process..", Logger.LogTags.Info, Logger.ELogExtraTag.Bundles);

        string[] filePaths = Directory.GetFiles(Path.Combine(GlobalVariables.RootDir, "Dependencies", "Locres"), "*.json", SearchOption.TopDirectoryOnly);

        var catalogDictionary = BundleUtils.CreateBundlesDictionary(CatalogData);

        foreach (string filePath in filePaths)
        {
            token.ThrowIfCancellationRequested();

            string fileName = Path.GetFileName(filePath);
            string langKey = StringUtils.LangSplit(fileName);

            var objectString = JsonConvert.SerializeObject(parsedBundlesDb);
            Dictionary<string, Bundle> localizedBundlesDb = JsonConvert.DeserializeObject<Dictionary<string, Bundle>>(objectString) ?? [];

            BundleUtils.PopulateLocalizationFromApi(localizedBundlesDb, langKey, catalogDictionary, CatalogData);

            string outputPath = Path.Combine(GlobalVariables.PathToParsedData, GlobalVariables.VersionWithBranch, langKey, "Bundles.json");

            FileWriter.SaveParsedDb(localizedBundlesDb, outputPath, Logger.ELogExtraTag.Bundles);
        }
    }
}