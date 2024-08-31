using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using UEParser.Models;
using UEParser.Parser;
using UEParser.Utils;
using UEParser.ViewModels;

namespace UEParser.APIComposers;

public class Collections
{
    private static readonly Dictionary<string, Dictionary<string, List<LocalizationEntry>>> LocalizationData = [];
    private static readonly dynamic CollectionsData = FileUtils.LoadDynamicJson(Path.Combine(GlobalVariables.pathToKraken, GlobalVariables.versionWithBranch, "CDN", "collections.json")) ?? throw new Exception("Failed to load collections data.");

    public static async Task InitializeCollectionsDB()
    {
        await Task.Run(() =>
        {
            Dictionary<string, Collection> parsedCollectionsDB = [];

            LogsWindowViewModel.Instance.AddLog($"[Collections] Starting parsing process..", Logger.LogTags.Info);

            ParseCollections(parsedCollectionsDB);

            LogsWindowViewModel.Instance.AddLog($"[Collections] Parsed total of {parsedCollectionsDB.Count} items.", Logger.LogTags.Info);

            ParseLocalizationAndSave(parsedCollectionsDB);
        });
    }

    private static void ParseCollections(Dictionary<string, Collection> parsedCollectionsDB)
    {
        // CollectionsDB exists locally but shouldn't be used!
        //string[] filePaths = Helpers.FindFilePathsInExtractedAssetsCaseInsensitive("CollectionDB.json");

        foreach (var collection in CollectionsData.collections)
        {
            string collectionId = collection["collectionId"];

            LogsWindowViewModel.Instance.AddLog($"[Collections] Processing: {collectionId}", Logger.LogTags.Info);

            //JObject collectionTitle = collection["collectionTitle"];
            //JObject collectionSubtitle = collection["collectionSubtitle"];

            string heroImageRaw = collection["heroImage"]["path"];
            string heroImage = CollectionUtils.TransformImagePath(heroImageRaw);

            //long activeFromTicks = collection.Value["ActiveFrom"]["Ticks"];
            //DateTime activeFrom = new(activeFromTicks);

            //long activeUntilTicks = collection.Value["ActiveUntil"]["Ticks"];
            //DateTime activeUntil = new(activeUntilTicks);

            // Combine AssetPathNames into one array
            JArray additionalImages = [];
            foreach (var image in collection["additionalImages"])
            {
                string assetPathNameRaw = image.AssetPathName;

                if (assetPathNameRaw != "None")
                {
                    string assetPathName = CollectionUtils.TransformImagePath(assetPathNameRaw);
                    additionalImages.Add(assetPathName);
                }
            }

            string heroVideo = collection["heroVideo"]["path"];
            string inclusionVersion = collection["inclusionVersion"];
            JArray cosmeticItems = collection["items"];
            string sortOrder = collection["sortOrder"];
            DateTime updatedDate = collection["updatedDate"];
            DateTime? limitedAvailabilityStartDate = collection["limitedAvailabilityStartDate"];
            bool visibleBeforeStartDate = collection?["visibleBeforeStartDate"] ?? false;
            //int flags = collection.Value["Flags"];

            Collection model = new()
            {
                CollectionId = collectionId,
                AdditionalImages = additionalImages,
                CollectionTitle = "",
                CollectionSubtitle = "",
                HeroImage = heroImage,
                HeroVideo = heroVideo,
                InclusionVersion = inclusionVersion,
                Items = cosmeticItems,
                SortOrder = sortOrder,
                UpdatedDate = updatedDate,
                LimitedAvailabilityStartDate = limitedAvailabilityStartDate,
                VisibleBeforeStartDate = visibleBeforeStartDate
                // Flags = flags
            };

            parsedCollectionsDB.Add(collectionId, model);
        }

    }

    private static void ParseLocalizationAndSave(Dictionary<string, Collection> parsedCollectionsDB)
    {
        LogsWindowViewModel.Instance.AddLog($"[Collections] Starting localization process..", Logger.LogTags.Info);

        string[] filePaths = Directory.GetFiles(Path.Combine(GlobalVariables.rootDir, "Dependencies", "Locres"), "*.json", SearchOption.TopDirectoryOnly);

        var collectionsDictionary = CollectionUtils.CreateCollectionsDictionary(CollectionsData);

        foreach (string filePath in filePaths)
        {
            //string jsonString = File.ReadAllText(filePath);
            string fileName = Path.GetFileName(filePath);

            string langKey = StringUtils.LangSplit(fileName);

            //Dictionary<string, string> languageKeys = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString) ?? throw new Exception($"Failed to load following locres file: {langKey}.");

            var objectString = JsonConvert.SerializeObject(parsedCollectionsDB);
            Dictionary<string, Collection> localizedCollectionsDB = JsonConvert.DeserializeObject<Dictionary<string, Collection>>(objectString) ?? [];

            //Helpers.LocalizeDB(localizedCollectionsDB, LocalizationData, languageKeys, langKey);
            CollectionUtils.PopulateLocalizationFromApi(localizedCollectionsDB, langKey, collectionsDictionary, CollectionsData);

            string outputPath = Path.Combine(GlobalVariables.pathToParsedData, GlobalVariables.versionWithBranch, langKey, "Collections.json");

            FileWriter.SaveParsedDB(localizedCollectionsDB, outputPath, "Collections");
        }
    }
}