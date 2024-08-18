using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System;
using System.Linq;
using UEParser.Services;
using UEParser.ViewModels;
using UEParser.Utils;
using UEParser.Models.KrakenCDN;
using System.Text.RegularExpressions;
using UEParser.AssetRegistry;

namespace UEParser.Kraken;

public partial class KrakenAPI
{
    public static async Task UpdateKrakenApi()
    {
        var config = ConfigurationService.Config;

        LogsWindowViewModel.Instance.AddLog("Looking for latest Kraken API version.", Logger.LogTags.Info);

        string? versionHeader = config.Core.VersionData.LatestVersionHeader;

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
        if (!string.IsNullOrEmpty(config.Core.ApiConfig.CustomVersion))
        {
            LogsWindowViewModel.Instance.AddLog($"API Version has been overridden with custom value set in settings - '{customVersion}'. If you wish to use latest version you should remove value set in 'Override API Versions' in settings.", Logger.LogTags.Warning);
            await RetrieveData();

            return;
        }

        if (response.Success && responseData != null)
        {
            string latestSavedVersion = config.Core.ApiConfig.LatestVersion;

            // If version pattern request fails try substring version
            // For example change version "8.1.1" to "8.1"
            // This happens because BHVR sometimes keeps the same API version
            // while changing version of the build
            // Other possible solution would be to use separate version for API and build
            if (responseData.AvailableVersions.Count == 0)
            {
                versionContentUrl = versionContentUrl[..^2];
                API.ApiResponse desperateRequest = await API.FetchUrl(versionContentUrl);

                if (!desperateRequest.Success) throw new Exception($"Failed to fetch latest Kraken version: {desperateRequest.ErrorMessage}");
                responseData = JsonConvert.DeserializeObject<VersionData>(desperateRequest.Data);
            }

            if (responseData?.AvailableVersions == null) throw new Exception($"Failed to fetch latest Kraken version.");

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

    [GeneratedRegex(@"^(?<version>\d+\.\d+\.\d+)_.+(?<environment>live|qa|stage|dev|cert|uat)$")]
    private static partial Regex GetVersionAndEnvironmentRegex();

    private static string DeconstructKrakenApiVersion(string latestVersion)
    {
        var regex = GetVersionAndEnvironmentRegex();

        Match match = regex.Match(latestVersion);
        if (!match.Success)
        {
            return "";
        }

        string version = match.Groups["version"].Value;
        string environment = match.Groups["environment"].Value;

        return $"{version}_{environment}";
    }

    private static string ConstructDynamicAssetCdnUrl(string uri)
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
        string krakenApiVersion = DeconstructKrakenApiVersion(latestVersion);
        string s3AccessKey = config.Core.ApiConfig.S3AccessKeys[krakenApiVersion];

        string cdnBaseUrlWithBranch = string.Format(cdnBaseUrl, branch);
        string cdnFullUrl = cdnBaseUrlWithBranch + Path.Combine(contentSegment, latestVersion, s3AccessKey, uri).Replace("\\", "/");

        return cdnFullUrl;
    }

    public static async Task DownloadDynamicContent()
    {
        string dynamicContentFilePath = Path.Combine(GlobalVariables.rootDir, "Output", "API", GlobalVariables.versionWithBranch, "dynamicContent.json");

        if (File.Exists(dynamicContentFilePath))
        {
            DynamicContent dynamicContentData = FileUtils.LoadJsonFileWithTypeCheck<DynamicContent>(dynamicContentFilePath);

            int numberOfDownloadedAssets = 0;
            foreach (var (_, downloadStrategy, packagedPath, _, uri) in dynamicContentData.Entries)
            {
                string extension = Path.GetExtension(uri).TrimStart('.');
                string modifiedPackagedPath = StringUtils.ModifyPath(packagedPath, extension).TrimStart('/');
                string modifiedPackagedPathWithoutExtension = Path.Combine(
                    Path.GetDirectoryName(modifiedPackagedPath) ?? string.Empty,
                    Path.GetFileNameWithoutExtension(modifiedPackagedPath)
                ).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Replace(Path.DirectorySeparatorChar, '/')
                .Replace(Path.AltDirectorySeparatorChar, '/');

                string assetOutputPath = Path.Combine(GlobalVariables.pathToDynamicAssets, GlobalVariables.versionWithBranch, modifiedPackagedPath);
                //if (downloadStrategy == "preferRemote")
                //{
                //    assetOutputPath = Path.Combine(GlobalVariables.pathToDynamicAssets, GlobalVariables.versionWithBranch, modifiedPackagedPath);
                //}
                //else
                //{
                //    assetOutputPath = Path.Combine(GlobalVariables.pathToDynamicAssets, GlobalVariables.versionWithBranch, uri);
                //}
                bool fileExistsInPackagedAssets = FilesRegister.DoesFileExist(modifiedPackagedPathWithoutExtension);

#if DEBUG
                if (fileExistsInPackagedAssets)
                {
                    LogsWindowViewModel.Instance.AddLog($"File already exists in packaged assets: {modifiedPackagedPath}", Logger.LogTags.Debug);
                }
#endif

                if (fileExistsInPackagedAssets) continue;
                if (File.Exists(assetOutputPath)) continue;

                Directory.CreateDirectory(Path.GetDirectoryName(assetOutputPath) ?? throw new InvalidOperationException($"Invalid directory path for asset: {assetOutputPath}"));

                string cdnUrl = ConstructDynamicAssetCdnUrl(uri);

                try
                {
                    byte[] fileBytes = await API.FetchFileBytesAsync(cdnUrl);
                    await File.WriteAllBytesAsync(assetOutputPath, fileBytes);
                    numberOfDownloadedAssets++;
                    LogsWindowViewModel.Instance.AddLog($"Successfully downloaded: {modifiedPackagedPath}", Logger.LogTags.Info);
                }
                catch (Exception ex)
                {
                    LogsWindowViewModel.Instance.AddLog($"Failed to download {uri}: {ex.Message}", Logger.LogTags.Error);
                }
            }

            if (numberOfDownloadedAssets > 0)
            {
                LogsWindowViewModel.Instance.AddLog($"Successfully downloaded dynamic content. Total of {numberOfDownloadedAssets} asset(s).", Logger.LogTags.Success);
            }
            else
            {
                LogsWindowViewModel.Instance.AddLog("Not found any new dynamic assets to download.", Logger.LogTags.Info);
            }
        }
        else
        {
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            LogsWindowViewModel.Instance.AddLog("Failed to load Dynamic Content file. Did you forget to update API first?", Logger.LogTags.Error);
        }
    }
}