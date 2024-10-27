using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using UEParser.ViewModels;
using UEParser.Network.Kraken.CDN;
using UEParser.Network.Kraken.API;
using UEParser.AssetRegistry;
using UEParser.Models;
using UEParser.Models.KrakenCDN;
using UEParser.Utils;
using UEParser.Services;

namespace UEParser.Network.Kraken;

public class KrakenManager
{
    public static async Task RetrieveKrakenApiAuthenticated()
    {
        NetAPI.LoadAndValidateCookies();
        bool isCookieNotExpired = NetAPI.IsAnyCookieNotExpired();

        if (!isCookieNotExpired)
        {
            await KrakenAPI.AuthenticateWithDbd();
        }
        else
        {
#if DEBUG
            LogsWindowViewModel.Instance.AddLog("Using locally stored auth session token.", Logger.LogTags.Debug);
#endif
        }

        await KrakenAPI.GetPlayerFullProfileState();
        await KrakenAPI.GetCharacterData();

        Dictionary<string, string> krakenEndpoints = new()
        {
            { "inventory", "Player's Inventory"},
            { "storyStatus", "Stories Status" },
            { "config", "Config" },
            { "currencies", "Currencies" },
            { "getSpecialPacks", "Special Packs" }
        };

        await KrakenAPI.BulkGetKrakenEndpoints(krakenEndpoints);
    }

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

        string versionContentUrl = KrakenAPI.ConstructApiUrl("contentVersion", queryParams);

        NetAPI.ApiResponse response = await NetAPI.FetchUrl(versionContentUrl);

        var responseData = JsonConvert.DeserializeObject<KrakenAPI.KrakenVersionData>(response.Data);

        string? customVersion = config.Core.ApiConfig.CustomVersion;
        if (!string.IsNullOrEmpty(customVersion))
        {
            LogsWindowViewModel.Instance.AddLog($"API Version has been overridden with custom value set in settings - '{customVersion}'. If you wish to use latest version you should remove value set in 'Override API Versions' in settings.", Logger.LogTags.Warning);
            await RetrieveData(config, customVersion);

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
                NetAPI.ApiResponse desperateRequest = await NetAPI.FetchUrl(versionContentUrl);

                if (!desperateRequest.Success) throw new Exception($"Failed to fetch latest Kraken version: {desperateRequest.ErrorMessage}");
                responseData = JsonConvert.DeserializeObject<KrakenAPI.KrakenVersionData>(desperateRequest.Data);
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

            bool isNewVersion = latestSavedVersion != latestVersion;
            bool isForcedUpdate = config.Core.ApiConfig.ForceKrakenUpdate;
            if (isNewVersion || isForcedUpdate)
            {
                if (isNewVersion)
                {
                    LogsWindowViewModel.Instance.AddLog($"Detected new version: '{latestVersion}'.", Logger.LogTags.Info);
                }
                else if (isForcedUpdate)
                {
                    LogsWindowViewModel.Instance.AddLog($"Update has been forced, you can change that in the settings. Version: '{latestVersion}'", Logger.LogTags.Info);
                }

                await RetrieveData(config, latestVersion);
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

        static async Task RetrieveData(Configuration config, string latestVersion)
        {
            await KrakenCDN.FetchGeneralCdnContent(latestVersion);
            await KrakenCDN.FetchArchivesCdnContent(KrakenCDN.CDNOutputDirName.Tomes, latestVersion);
            await KrakenCDN.FetchArchivesCdnContent(KrakenCDN.CDNOutputDirName.Rifts, latestVersion);

            LogsWindowViewModel.Instance.AddLog("Creating game characters helper table from retrieved API.", Logger.LogTags.Info);
            Helpers.CreateCharacterTable();

            config.Core.ApiConfig.LatestVersion = latestVersion;

            await ConfigurationService.SaveConfiguration();

            LogsWindowViewModel.Instance.AddLog("Successfully retrieved Kraken API.", Logger.LogTags.Success);
        }
    }

    public static async Task DownloadDynamicContent()
    {
        string dynamicContentFilePath = Path.Combine(GlobalVariables.PathToKraken, GlobalVariables.VersionWithBranch, "CDN", "dynamicContent.json");

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

                string assetOutputPath = Path.Combine(GlobalVariables.PathToDynamicAssets, GlobalVariables.VersionWithBranch, modifiedPackagedPath);

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

                string cdnUrl = KrakenCDN.ConstructDynamicAssetCdnUrl(uri);

                try
                {
                    byte[] fileBytes = await NetAPI.FetchFileBytesAsync(cdnUrl);
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