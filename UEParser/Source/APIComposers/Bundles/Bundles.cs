using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using UEParser.Models;
using UEParser.Parser;
using UEParser.Utils;
using UEParser.ViewModels;

namespace UEParser.APIComposers;

public class Bundles
{
    private static readonly Dictionary<string, Dictionary<string, List<LocalizationEntry>>> LocalizationData = [];
    private static readonly dynamic CatalogData = FileUtils.LoadDynamicJson(Path.Combine(GlobalVariables.pathToKrakenApi, GlobalVariables.versionWithBranch, "catalog.json")) ?? throw new Exception("Failed to load catalog data.");
    private static readonly Dictionary<string, DLC> DlcsData = FileUtils.LoadJsonFileWithTypeCheck<Dictionary<string, DLC>>(Path.Combine(GlobalVariables.pathToParsedData, GlobalVariables.versionWithBranch, "en", "DLC.json")) ?? throw new Exception("Failed to load parsed DLC data.");
    private static readonly Dictionary<string, Character> CharactersData = FileUtils.LoadJsonFileWithTypeCheck<Dictionary<string, Character>>(Path.Combine(GlobalVariables.pathToParsedData, GlobalVariables.versionWithBranch, "en", "Characters.json")) ?? throw new Exception("Failed to load parsed Characters data.");

    public static async Task InitializeBundlesDB()
    {
        await Task.Run(() =>
        {
            Dictionary<string, Bundle> parsedBundlesDB = [];

            LogsWindowViewModel.Instance.AddLog($"[Bundles] Starting parsing process..", Logger.LogTags.Info);

            ParseBundles(parsedBundlesDB);

            LogsWindowViewModel.Instance.AddLog($"[Bundles] Parsed total of {parsedBundlesDB.Count} items.", Logger.LogTags.Info);

            ParseLocalizationAndSave(parsedBundlesDB);
        });
    }

    private static readonly string[] ignoreDlcs =
    [
        "80suitcase",
        "bloodstainedSack",
        "headCase"
    ];
    private static void ParseBundles(Dictionary<string, Bundle> parsedBundlesDB)
    {
        foreach (var item in CatalogData)
        {
            JArray typeArray = item["categories"];
            string prettyType = string.Join(" ", typeArray);

            if (prettyType == "specialPack")
            {
                string bundleId = item["id"];

                LogsWindowViewModel.Instance.AddLog($"[Bundles] Processing: {bundleId}", Logger.LogTags.Info);

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

                //JObject specialPackTitle = item["metaData"]["specialPackTitle"];

                JArray fullPriceArray = (JArray)item["metaData"]["fullPrice"];
                List<FullPrice> fullPrices = BundleUtils.ParseFullPrice(fullPriceArray);

                List<ConsumptionRewards> consumptionRewards = BundleUtils.ParseConsumptionRewards((JArray)item["consumptionReward"]);
                JArray segmentationTags = item["metaData"]["segmentationTags"];

                string imagePath = item["metaData"]["imagePath"];

                bool isChapterBundle = false;
                string[] dlcsToIgnore = ["80suitcase", "bloodstainedSack", "headCase"];
                if ((dlcId != null && !dlcsToIgnore.Contains(dlcId)) || isLicensedBundle)
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

                Bundle model = new()
                {
                    Id = bundleId,
                    SpecialPackTitle = "",
                    ImagePath = imagePath,
                    StartDate = startDate,
                    EndDate = endDate,
                    SortOrder = sortOrder,
                    IsChapterBundle = isChapterBundle,
                    IsLicensedBundle = isLicensedBundle,
                    MinNumberOfUnownedForPurchase = minNumberOfUnownedForPurchase,
                    DlcId = dlcId,
                    FullPrice = fullPrices,
                    Discount = discount,
                    ConsumptionRewards = consumptionRewards,
                    Consumable = isConsumable,
                    SegmentationTags = segmentationTags,
                    Type = prettyType
                };

                parsedBundlesDB.Add(bundleId, model);
            }
        }
    }

    private static void ParseLocalizationAndSave(Dictionary<string, Bundle> parsedBundlesDB)
    {
        LogsWindowViewModel.Instance.AddLog($"[Bundles] Starting localization process..", Logger.LogTags.Info);

        string[] filePaths = Directory.GetFiles(Path.Combine(GlobalVariables.rootDir, "Dependencies", "Locres"), "*.json", SearchOption.TopDirectoryOnly);

        var catalogDictionary = BundleUtils.CreateBundlesDictionary(CatalogData);

        foreach (string filePath in filePaths)
        {
            //string jsonString = File.ReadAllText(filePath);
            string fileName = Path.GetFileName(filePath);

            string langKey = StringUtils.LangSplit(fileName);

            //Dictionary<string, string> languageKeys = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString) ?? throw new Exception($"Failed to load following locres file: {langKey}.");

            var objectString = JsonConvert.SerializeObject(parsedBundlesDB);
            Dictionary<string, Bundle> localizedBundlesDB = JsonConvert.DeserializeObject<Dictionary<string, Bundle>>(objectString) ?? [];

            //Helpers.LocalizeDB(localizedBundlesDB, LocalizationData, languageKeys, langKey);

            BundleUtils.PopulateLocalizationFromApi(localizedBundlesDB, langKey, catalogDictionary, CatalogData);

            string outputPath = Path.Combine(GlobalVariables.pathToParsedData, GlobalVariables.versionWithBranch, langKey, "Bundles.json");

            FileWriter.SaveParsedDB(localizedBundlesDB, outputPath, "Bundles");
        }
    }
}