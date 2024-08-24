using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UEParser.Services;
using UEParser.ViewModels;
using UEParser.Network.Kraken.API;

namespace UEParser.Network.Kraken.CDN;

public partial class KrakenCDN
{
    private static string ConstructCdnUrl(string endpoint)
    {
        var config = ConfigurationService.Config;

        string branch = config.Core.VersionData.Branch.ToString();
        string apiBaseUrl = config.Core.ApiConfig.CdnBaseUrl;

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

        string cdnFullUrl = string.Format(apiBaseUrl, branch) + contentSegment + latestVersion + endpoint;

        return cdnFullUrl;
    }

    public static async Task FetchCdnContent()
    {
        var config = ConfigurationService.Config;

        string versionWithBranch = Helpers.ConstructVersionHeaderWithBranch();

        string outputDir = Path.Combine(GlobalVariables.rootDir, "Output", "API", versionWithBranch);
        Directory.CreateDirectory(outputDir);

        string branch = config.Core.VersionData.Branch.ToString();

        Dictionary<string, string> cdnEndpoints = config.Core.ApiConfig.CdnEndpoints;

        foreach (string cdnEndpoint in cdnEndpoints.Keys)
        {
            LogsWindowViewModel.Instance.AddLog($"Fetching CDN: '{cdnEndpoint}'.", Logger.LogTags.Info);

            string outputPath = Path.Combine(outputDir, cdnEndpoint + ".json");
            string url = ConstructCdnUrl(cdnEndpoints[cdnEndpoint]);
            Network.API.ApiResponse response = await Network.API.FetchUrl(url);

            LogsWindowViewModel.Instance.AddLog($"Decrypting CDN: '{cdnEndpoint}'.", Logger.LogTags.Info);

            string decodedData = DbdDecryption.DecryptCDN(response.Data, branch);

            LogsWindowViewModel.Instance.AddLog($"Saved CDN: '{cdnEndpoint}'.", Logger.LogTags.Info);

            File.WriteAllText(outputPath, decodedData);
        }
    }

    public enum CDNOutputDirName
    {
        Tomes,
        Rifts
    }
    public static async Task FetchDynamicCdnContent(CDNOutputDirName outputDirName)
    {
        var config = ConfigurationService.Config;

        string versionWithBranch = Helpers.ConstructVersionHeaderWithBranch();

        string outputDirNameString = outputDirName.ToString();
        string outputDir = Path.Combine(GlobalVariables.rootDir, "Output", "API", versionWithBranch, outputDirNameString);
        Directory.CreateDirectory(outputDir);

        string branch = config.Core.VersionData.Branch.ToString();

        HashSet<string> tomesList = config.Core.TomesList;
        HashSet<string> eventTomesList = config.Core.EventTomesList;
        HashSet<string> combinedTomesList = [.. tomesList, .. eventTomesList];

        string cdnEndpoint = config.Core.ApiConfig.DynamicCdnEndpoints[outputDirNameString];

        if (CDNOutputDirName.Tomes == outputDirName)
        {
            foreach (string tomeId in combinedTomesList)
            {
                string outputPath = Path.Combine(outputDir, tomeId + ".json");

                if (File.Exists(outputPath))
                {
                    continue;
                }

                string tomeCdnEndpoint = string.Format(cdnEndpoint, tomeId);
                string url = ConstructCdnUrl(tomeCdnEndpoint);

                LogsWindowViewModel.Instance.AddLog($"Fetching CDN: '{outputDirNameString} {tomeId}'", Logger.LogTags.Info);

                Network.API.ApiResponse response = await Network.API.FetchUrl(url);

                LogsWindowViewModel.Instance.AddLog($"Decrypting CDN: '{outputDirNameString} {tomeId}'.", Logger.LogTags.Info);

                string decodedData = DbdDecryption.DecryptCDN(response.Data, branch);

                LogsWindowViewModel.Instance.AddLog($"Saved CDN: '{outputDirNameString} {tomeId}'.", Logger.LogTags.Info);

                File.WriteAllText(outputPath, decodedData);
            }
        }
        else if (CDNOutputDirName.Rifts == outputDirName)
        {
            foreach (string tomeId in tomesList)
            {
                string outputPath = Path.Combine(outputDir, tomeId + ".json");

                if (File.Exists(outputPath))
                {
                    continue;
                }

                string tomeCdnEndpoint = string.Format(cdnEndpoint, tomeId);
                string url = ConstructCdnUrl(tomeCdnEndpoint);

                LogsWindowViewModel.Instance.AddLog($"Fetching CDN: '{outputDirNameString} {tomeId}'", Logger.LogTags.Info);

                Network.API.ApiResponse response = await Network.API.FetchUrl(url);

                LogsWindowViewModel.Instance.AddLog($"Decrypting CDN: '{outputDirNameString} {tomeId}'.", Logger.LogTags.Info);

                string decodedData = DbdDecryption.DecryptCDN(response.Data, branch);

                LogsWindowViewModel.Instance.AddLog($"Saved CDN: '{outputDirNameString} {tomeId}'.", Logger.LogTags.Info);

                File.WriteAllText(outputPath, decodedData);
            }
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
        string s3AccessKey = config.Core.ApiConfig.S3AccessKeys[krakenApiVersion];

        string cdnBaseUrlWithBranch = string.Format(cdnBaseUrl, branch);
        string cdnFullUrl = cdnBaseUrlWithBranch + Path.Combine(contentSegment, latestVersion, s3AccessKey, uri).Replace("\\", "/");

        return cdnFullUrl;
    }
}