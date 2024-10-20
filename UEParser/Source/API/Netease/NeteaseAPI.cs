using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UEParser.Models.Netease;
using UEParser.Services;

namespace UEParser.Network.Netease;

public partial class NeteaseAPI
{
    #region Regex
    [GeneratedRegex("ver (\\d+)")]
    private static partial Regex VersionRegex();

    [GeneratedRegex("(\\S+)\\.(\\S+)\\s+(\\S+)\\s+(\\d+)")]
    private static partial Regex FileDataRegex();
    #endregion

    public class ParsedManifest
    {
        public required string Version { get; set; }
        public required List<ManifestFileData> FileDataList { get; set; }
    }

    public static async Task<ParsedManifest> BruteForceLatestManifest()
    {
        var config = ConfigurationService.Config;
        string manifestString = "";

        int latestManifestVersion = config.Netease.ContentConfig.LatestManifestVersion;
        int incrementedManifestVersion = latestManifestVersion;

        int retryCount = 0;
        const int maxRetries = 10;
        while (retryCount < maxRetries)
        {
            string platform = config.Netease.ContentConfig.Platform.ToString();
            string baseUrl = config.Netease.ContentConfig.NeteaseManifestBaseUrl;
            string endpoint = config.Netease.ContentConfig.NeteaseManifestEndpoint;

            string manifestUrl = $"{string.Format(baseUrl, GlobalVariables.PlatformType)}{string.Format(endpoint, incrementedManifestVersion, platform)}";

            var manifestData = await NetAPI.FetchUrl(manifestUrl);

            if (manifestData.Success && !string.IsNullOrEmpty(manifestData.Data))
            {
                latestManifestVersion = incrementedManifestVersion;
                manifestString = manifestData.Data;
            }
            else
            {
                break;
            }

            retryCount++;
            incrementedManifestVersion++;
        }

        if (string.IsNullOrEmpty(manifestString)) throw new Exception("Failed to find latest manifest data.");

        config.Netease.ContentConfig.LatestManifestVersion = latestManifestVersion;
        await ConfigurationService.SaveConfiguration();

        var parsedManifest = ParseNeteaseManifest(manifestString);

        return parsedManifest;
    }

    #region Utils
    public static ParsedManifest ParseNeteaseManifest(string manifestString)
    {
        var result = new ParsedManifest
        {
            Version = "",
            FileDataList = []
        };

        string[] lines = manifestString.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length > 0)
        {
            Regex versionRegex = VersionRegex();
            Match versionMatch = versionRegex.Match(lines[0]);
            if (versionMatch.Success)
            {
                result.Version = versionMatch.Groups[1].Value;
            }
        }

        Regex fileDataRegex = FileDataRegex();
        foreach (string line in lines)
        {
            Match fileDataMatch = fileDataRegex.Match(line);
            if (fileDataMatch.Success)
            {
                string filePath = fileDataMatch.Groups[1].Value;
                string fileExtension = fileDataMatch.Groups[2].Value;
                string hash = fileDataMatch.Groups[3].Value;
                long fileSize = long.Parse(fileDataMatch.Groups[4].Value);
                string filePathWithExtension = $"{filePath}.{fileExtension}";

                ManifestFileData fileDataInstance = new()
                {
                    FilePath = filePath,
                    FileExtension = fileExtension,
                    FilePathWithExtension = filePathWithExtension,
                    FileSize = fileSize,
                    FileHash = hash
                };

                result.FileDataList.Add(fileDataInstance);
            }
        }

        if (result.FileDataList.Count == 0) throw new Exception("Manifest doesn't contain any files to download.");

        result.FileDataList.Sort((x, y) =>
        {
            bool xIsPak = x.FileExtension.StartsWith("pak", StringComparison.OrdinalIgnoreCase);
            bool yIsPak = y.FileExtension.StartsWith("pak", StringComparison.OrdinalIgnoreCase);

            // Prioritize "pak" extensions
            if (xIsPak && !yIsPak) return -1;
            if (!xIsPak && yIsPak) return 1;

            return string.Compare(x.FileExtension, y.FileExtension, StringComparison.OrdinalIgnoreCase);
        });

        return result;
    }

    public static long CalculateTotalSize(List<ManifestFileData> fileData)
    {
        long totalSize = 0;
        foreach (ManifestFileData file in fileData)
        {
            totalSize += file.FileSize;
        }

        return totalSize;
    }
    #endregion
}