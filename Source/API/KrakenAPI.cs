using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System;
using System.Linq;
using UEParser.CDNDecoder;
using UEParser.Services;
using UEParser.ViewModels;

namespace UEParser.Kraken;

public class KrakenAPI
{
    public static async Task UpdateKrakenApi()
    {
        var config = ConfigurationService.Config;

        LogsWindowViewModel.Instance.AddLog("Looking for latest Kraken API version.", Logger.LogTags.Info);

        string versionHeader = config.Core.VersionData.LatestVersionHeader;

        if (string.IsNullOrEmpty(versionHeader))
        {
            throw new InvalidOperationException("Latest Version Header in config is empty or null.. failed to check latest version.");
        }

        Dictionary<string, string> queryParams = [];
        queryParams["versionPattern"] = versionHeader;

        string versionContentUrl = ConstructApiUrl("contentVersion", queryParams);

        API.ApiResponse response = await API.FetchUrl(versionContentUrl);

        var responseData = JsonConvert.DeserializeObject<VersionData>(response.Data);

        string? customVersion = config.Core.ApiConfig.CustomVersion;
        if (config.Core.ApiConfig.CustomVersion != null)
        {
            LogsWindowViewModel.Instance.AddLog($"API Version has been overridden with custom value set in settings - '{customVersion}'. If you wish to use latest version you should remove value set in 'CustomVersion' property.", Logger.LogTags.Warning);
            await RetrieveData();

            return;
        }

        if (response.Success && responseData != null)
        {
            string latestSavedVersion = config.Core.ApiConfig.LatestVersion;

            // Check what's the latest version by its timestamp
            int maxTimestamp = 0;
            string? latestVersion = null;

            foreach (var version in responseData.AvailableVersions)
            {
                string versionValue = version.Value;
                int timestamp = int.Parse(versionValue.Split('-')[1]);
                if (timestamp > maxTimestamp)
                {
                    maxTimestamp = timestamp;
                    latestVersion = version.Key;
                }
            }

            if (string.IsNullOrEmpty(latestVersion))
            {
                throw new Exception("Kraken version check failed.");
            }

            config.Core.ApiConfig.LatestVersion = latestVersion;

            if (latestSavedVersion != latestVersion)
            {
                LogsWindowViewModel.Instance.AddLog($"Detected new version: '{latestVersion}'.", Logger.LogTags.Info);

                //await FetchCdnContent();
                //await FetchDynamicCdnContent(CDNOutputDirName.Tomes);
                //await FetchDynamicCdnContent(CDNOutputDirName.Rifts);

                //LogsWindowViewModel.Instance.AddLog("Creating game characters helper table from retrieved API.", Logger.LogTags.Info);
                //Helpers.CreateCharacterTable();

                //await ConfigurationService.SaveConfiguration();

                //LogsWindowViewModel.Instance.AddLog("Successfully retrieved Kraken API.", Logger.LogTags.Success);
                await RetrieveData();
            }
            else
            {
                LogsWindowViewModel.Instance.AddLog("No new version has been detected.", Logger.LogTags.Info);
            }
        }
        else
        {
            LogsWindowViewModel.Instance.AddLog($"Failed to fetch latest Kraken version: {response.ErrorMessage}", Logger.LogTags.Error);
        }
    }

    private static async Task RetrieveData()
    {
        await FetchCdnContent();
        await FetchDynamicCdnContent(CDNOutputDirName.Tomes);
        await FetchDynamicCdnContent(CDNOutputDirName.Rifts);

        LogsWindowViewModel.Instance.AddLog("Creating game characters helper table from retrieved API.", Logger.LogTags.Info);
        Helpers.CreateCharacterTable();

        await ConfigurationService.SaveConfiguration();

        LogsWindowViewModel.Instance.AddLog("Successfully retrieved Kraken API.", Logger.LogTags.Success);
    }

    private static string ConstructApiUrl(string endpoint, Dictionary<string, string>? queryParams = null)
    {
        var config = ConfigurationService.Config;

        string branch = config.Core.VersionData.Branch.ToString();
        string apiBaseUrl = config.Core.ApiConfig.ApiBaseUrl;

        // Branches live/ptb/qa use different base url, change it here
        string[] branchesToCheck = ["live", "ptb", "qa"];
        if (branchesToCheck.Contains(branch))
        {
            apiBaseUrl = config.Core.ApiConfig.SteamApiBaseUrl;
        }

        // Check if the endpoint exists in the configuration
        if (!config.Core.ApiConfig.ApiEndpoints.TryGetValue(endpoint, out string? apiEndpoint))
        {
            throw new ArgumentException($"Endpoint '{endpoint}' not found in configuration.");
        }

        string apiFullUrl = string.Format(apiBaseUrl, branch) + apiEndpoint;

        // Add query parameters if provided
        if (queryParams != null && queryParams.Count > 0)
        {
            if (apiFullUrl.Contains('?'))
            {
                apiFullUrl += '&';
            }
            else
            {
                apiFullUrl += '?';
            }

            foreach (var param in queryParams)
            {
                apiFullUrl += $"{param.Key}={param.Value}&";
            }

            apiFullUrl = apiFullUrl.TrimEnd('&');
        }

        return apiFullUrl;
    }

    private static string ConstructCdnUrl(string endpoint)
    {
        var config = ConfigurationService.Config;

        string branch = config.Core.VersionData.Branch.ToString();
        string apiBaseUrl = config.Core.ApiConfig.CdnBaseUrl;

        string cdnRoot = config.Global.BranchRoots[branch];

        string? customVersion = config.Core.ApiConfig.CustomVersion;

        string latestVersion;
        if (customVersion != null)
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

    private class VersionData
    {
        public required Dictionary<string, string> AvailableVersions { get; set; }
    }

    private static async Task FetchCdnContent()
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
            API.ApiResponse response = await API.FetchUrl(url);

            LogsWindowViewModel.Instance.AddLog($"Decrypting CDN: '{cdnEndpoint}'.", Logger.LogTags.Info);

            string decodedData = DbdDecryption.DecryptCDN(response.Data, branch);

            LogsWindowViewModel.Instance.AddLog($"Saved CDN: '{cdnEndpoint}'.", Logger.LogTags.Success);

            File.WriteAllText(outputPath, decodedData);
        }
    }

    private enum CDNOutputDirName
    {
        Tomes,
        Rifts
    }

    private static async Task FetchDynamicCdnContent(CDNOutputDirName outputDirName)
    {
        var config = ConfigurationService.Config;

        string versionWithBranch = Helpers.ConstructVersionHeaderWithBranch();

        string outputDirNameString = outputDirName.ToString();
        string outputDir = Path.Combine(GlobalVariables.rootDir, "Output", "API", versionWithBranch, outputDirNameString);
        Directory.CreateDirectory(outputDir);

        string branch = config.Core.VersionData.Branch.ToString();

        List<string> tomesList = config.Core.TomesList;
        List<string> eventTomesList = config.Core.EventTomesList;
        List<string> combinedTomesList = [.. tomesList, .. eventTomesList];

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

                API.ApiResponse response = await API.FetchUrl(url);

                LogsWindowViewModel.Instance.AddLog($"Decrypting CDN: '{outputDirNameString} {tomeId}'.", Logger.LogTags.Info);

                string decodedData = DbdDecryption.DecryptCDN(response.Data, branch);

                LogsWindowViewModel.Instance.AddLog($"Saved CDN: '{outputDirNameString} {tomeId}'.", Logger.LogTags.Success);

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

                API.ApiResponse response = await API.FetchUrl(url);

                LogsWindowViewModel.Instance.AddLog($"Decrypting CDN: '{outputDirNameString} {tomeId}'.", Logger.LogTags.Info);

                string decodedData = DbdDecryption.DecryptCDN(response.Data, branch);

                LogsWindowViewModel.Instance.AddLog($"Saved CDN: '{outputDirNameString} {tomeId}'.", Logger.LogTags.Success);

                File.WriteAllText(outputPath, decodedData);
            }
        }
    }
}