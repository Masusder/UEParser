﻿using System;
using System.IO;
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

public class Collections
{
    private static readonly Dictionary<string, Dictionary<string, List<LocalizationEntry>>> LocalizationData = [];
    private static readonly dynamic CollectionsData = FileUtils.LoadDynamicJson(Path.Combine(GlobalVariables.pathToKraken, GlobalVariables.versionWithBranch, "CDN", "collections.json")) ?? throw new Exception("Failed to load collections data.");

    public static async Task InitializeCollectionsDB(CancellationToken token)
    {
        await Task.Run(() =>
        {
            Dictionary<string, Collection> parsedCollectionsDB = [];

            LogsWindowViewModel.Instance.AddLog($"Starting parsing process..", Logger.LogTags.Info, Logger.ELogExtraTag.Collections);

            ParseCollections(parsedCollectionsDB, token);

            LogsWindowViewModel.Instance.AddLog($"Parsed total of {parsedCollectionsDB.Count} items.", Logger.LogTags.Info, Logger.ELogExtraTag.Collections);

            ParseLocalizationAndSave(parsedCollectionsDB, token);
        }, token);
    }

    private static void ParseCollections(Dictionary<string, Collection> parsedCollectionsDB, CancellationToken token)
    {
        // CollectionsDB exists locally but shouldn't be used!
        //string[] filePaths = Helpers.FindFilePathsInExtractedAssetsCaseInsensitive("CollectionDB.json");

        foreach (var collection in CollectionsData.collections)
        {
            token.ThrowIfCancellationRequested();

            string collectionId = collection["collectionId"];

            LogsWindowViewModel.Instance.AddLog($"Processing: {collectionId}", Logger.LogTags.Info, Logger.ELogExtraTag.Collections);

            string heroImageRaw = collection["heroImage"]["path"];
            string heroImage = CollectionUtils.TransformImagePath(heroImageRaw);

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
            };

            parsedCollectionsDB.Add(collectionId, model);
        }

    }

    private static void ParseLocalizationAndSave(Dictionary<string, Collection> parsedCollectionsDB, CancellationToken token)
    {
        LogsWindowViewModel.Instance.AddLog($"Starting localization process..", Logger.LogTags.Info, Logger.ELogExtraTag.Collections);

        string[] filePaths = Directory.GetFiles(Path.Combine(GlobalVariables.rootDir, "Dependencies", "Locres"), "*.json", SearchOption.TopDirectoryOnly);

        var collectionsDictionary = CollectionUtils.CreateCollectionsDictionary(CollectionsData);

        foreach (string filePath in filePaths)
        {
            token.ThrowIfCancellationRequested();

            string fileName = Path.GetFileName(filePath);
            string langKey = StringUtils.LangSplit(fileName);

            var objectString = JsonConvert.SerializeObject(parsedCollectionsDB);
            Dictionary<string, Collection> localizedCollectionsDB = JsonConvert.DeserializeObject<Dictionary<string, Collection>>(objectString) ?? [];

            CollectionUtils.PopulateLocalizationFromApi(localizedCollectionsDB, langKey, collectionsDictionary, CollectionsData);

            string outputPath = Path.Combine(GlobalVariables.pathToParsedData, GlobalVariables.versionWithBranch, langKey, "Collections.json");

            FileWriter.SaveParsedDB(localizedCollectionsDB, outputPath, Logger.ELogExtraTag.Collections);
        }
    }
}