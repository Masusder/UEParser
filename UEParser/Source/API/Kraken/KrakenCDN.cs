using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using UEParser.Services;
using UEParser.ViewModels;
using UEParser.Network.Kraken.API;

namespace UEParser.Network.Kraken.CDN;

public class KrakenCDN
{
    private static string ConstructCdnUrl(string endpoint, string latestVersion)
    {
        var config = ConfigurationService.Config;

        string branch = config.Core.VersionData.Branch.ToString();
        string apiBaseUrl = config.Core.ApiConfig.CdnBaseUrl;

        string cdnRoot = config.Global.BranchRoots[branch];

        string? customVersion = config.Core.ApiConfig.CustomVersion;
        if (!string.IsNullOrEmpty(customVersion))
        {
            latestVersion = customVersion;
        }

        string contentSegmentWithoutRoot = config.Core.ApiConfig.CdnContentSegment;
        string contentSegment = string.Format(contentSegmentWithoutRoot, cdnRoot);

        string cdnFullUrl = string.Format(apiBaseUrl, branch) + contentSegment + latestVersion + endpoint;

        return cdnFullUrl;
    }

    public static async Task FetchGeneralCdnContent(string latestVersion)
    {
        var config = ConfigurationService.Config;

        string versionWithBranch = Helpers.ConstructVersionHeaderWithBranch();

        string outputDir = Path.Combine(GlobalVariables.PathToKraken, versionWithBranch, "CDN");
        Directory.CreateDirectory(outputDir);

        string branch = config.Core.VersionData.Branch.ToString();

        Dictionary<string, string> cdnEndpoints = config.Core.ApiConfig.CdnEndpoints;

        foreach (string cdnEndpoint in cdnEndpoints.Keys)
        {
            LogsWindowViewModel.Instance.AddLog($"Fetching, decrypting and saving CDN: '{cdnEndpoint}'.", Logger.LogTags.Info);

            string outputPath = Path.Combine(outputDir, cdnEndpoint + ".json");
            string url = ConstructCdnUrl(cdnEndpoints[cdnEndpoint], latestVersion);

            NetAPI.ApiResponse response = await NetAPI.FetchUrl(url);
            string decodedData = DbdDecryption.DecryptCDN(response.Data, branch);

            File.WriteAllText(outputPath, decodedData);
        }
    }

    public enum CDNOutputDirName
    {
        Tomes,
        Rifts
    }

    public static async Task FetchArchivesCdnContent(CDNOutputDirName outputDirName, string latestVersion)
    {
        var config = ConfigurationService.Config;

        string versionWithBranch = Helpers.ConstructVersionHeaderWithBranch();

        string outputDirNameString = outputDirName.ToString();
        string outputDir = Path.Combine(GlobalVariables.PathToKraken, versionWithBranch, "CDN", outputDirNameString);
        Directory.CreateDirectory(outputDir);

        string branch = config.Core.VersionData.Branch.ToString();

        HashSet<string> tomesList = config.Core.TomesList;
        HashSet<string> eventTomesList = config.Core.EventTomesList;
        HashSet<string> combinedTomesList = [.. tomesList, .. eventTomesList];

        string cdnEndpoint = config.Core.ApiConfig.DynamicCdnEndpoints[outputDirNameString];

        IEnumerable<string> listToProcess = outputDirName == CDNOutputDirName.Tomes ? combinedTomesList : tomesList;

        await ProcessCdnItems(listToProcess, outputDir, outputDirNameString, cdnEndpoint, latestVersion, branch);
    }

    private static async Task ProcessCdnItems(IEnumerable<string> items, string outputDir, string outputDirNameString, string cdnEndpoint, string latestVersion, string branch)
    {
        foreach (string itemId in items)
        {
            string outputPath = Path.Combine(outputDir, itemId + ".json");

            if (File.Exists(outputPath))
            {
                continue;
            }

            string itemCdnEndpoint = string.Format(cdnEndpoint, itemId);
            string url = ConstructCdnUrl(itemCdnEndpoint, latestVersion);

            LogsWindowViewModel.Instance.AddLog($"Fetching, decrypting and saving CDN: '{outputDirNameString} {itemId}'", Logger.LogTags.Info);

            NetAPI.ApiResponse response = await NetAPI.FetchUrl(url);
            string decodedData = DbdDecryption.DecryptCDN(response.Data, branch);

            File.WriteAllText(outputPath, decodedData);
        }
    }

    public static string ConstructDynamicAssetCdnUrl(string uri)
    {
        var config = ConfigurationService.Config;

        string branch = config.Core.VersionData.Branch.ToString();
        string cdnBaseUrl = config.Core.ApiConfig.CdnBaseUrl;

        string cdnRoot = config.Global.BranchRoots[branch];

        string? customVersion = config.Core.ApiConfig.CustomVersion;

        string latestVersion;
        if (!string.IsNullOrEmpty(customVersion))
        {
            latestVersion = customVersion;
        }
        else
        {
            latestVersion = config.Core.ApiConfig.LatestVersion;
        }

        string contentSegmentWithoutRoot = config.Core.ApiConfig.CdnContentSegment;
        string contentSegment = string.Format(contentSegmentWithoutRoot, cdnRoot);

        // Use Kraken API version instead of one configured locally!
        string krakenApiVersion = KrakenAPI.DeconstructKrakenApiVersion(latestVersion);
        config.Core.ApiConfig.S3AccessKeys.TryGetValue(krakenApiVersion, out string? s3AccessKey);

        if (string.IsNullOrEmpty(s3AccessKey)) throw new Exception("S3 Access Key was not present.");

        string cdnBaseUrlWithBranch = string.Format(cdnBaseUrl, branch);
        string cdnFullUrl = cdnBaseUrlWithBranch + Path.Combine(contentSegment, latestVersion, s3AccessKey, uri).Replace("\\", "/");

        return cdnFullUrl;
    }
}