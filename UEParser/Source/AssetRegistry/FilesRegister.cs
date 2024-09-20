using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UEParser.Services;
using UEParser.ViewModels;
using CUE4Parse.FileProvider.Objects;
using System.Text.RegularExpressions;

namespace UEParser.AssetRegistry;

public class FilesRegister
{
    private static Dictionary<string, FileInfo> fileInfoDictionary = [];
    private static readonly object lockObject = new();
    private static bool isLoaded = false;

    private static readonly string filesRegisterDirectoryPath = Path.Combine(GlobalVariables.rootDir, "Dependencies", "FilesRegister");
    private static readonly string pathToFileRegister;
    public class FileInfo(string extension, long size)
    {
        public string Extension { get; set; } = extension;
        public long Size { get; set; } = size;
    }

    static FilesRegister()
    {
        string path = FilesRegisterPathConstructor(false);
        pathToFileRegister = path;

        if (!Directory.Exists(filesRegisterDirectoryPath))
        {
            Directory.CreateDirectory(filesRegisterDirectoryPath);
        }

        LoadFileInfoDictionary();
    }

    private static string FilesRegisterPathConstructor(bool isBackupVersion = false)
    {
        var config = ConfigurationService.Config;

        if (isBackupVersion)
        {
            string? version = config.Core.VersionData.CompareVersionHeader;
            string branch = config.Core.VersionData.CompareBranch.ToString();

            string filesRegisterName = $"Core_{version}_{branch}_FilesRegister.uinfo";
            string path = Path.Combine(filesRegisterDirectoryPath, filesRegisterName);

            return path;
        }
        else
        {
            string? version = config.Core.VersionData.LatestVersionHeader;
            string branch = config.Core.VersionData.Branch.ToString();

            string filesRegisterName = $"Core_{version}_{branch}_FilesRegister.uinfo";
            string path = Path.Combine(filesRegisterDirectoryPath, filesRegisterName);

            return path;
        }
    }

    public static void UpdateFileInfo(string filePath, long dataSize, string extension)
    {
        lock (lockObject)
        {
            if (fileInfoDictionary!.ContainsKey(filePath))
            {
                // Update existing FileInfo
                fileInfoDictionary[filePath] = new FileInfo(extension, dataSize);
            }
            else
            {
                // Add new FileInfo
                fileInfoDictionary.Add(filePath, new FileInfo(extension, dataSize));
            }
        }
    }

    public static void SaveFileInfoDictionary()
    {
        var (_, audio) = RegistryManager.ReadFromUInfoFile(pathToFileRegister);
        RegistryManager.WriteToUInfoFile(pathToFileRegister, fileInfoDictionary, audio);

        LogsWindowViewModel.Instance.AddLog("Saved files register.", Logger.LogTags.Info);
    }

    public static void CleanUpFileInfoDictionary(List<GameFile> files)
    {
        lock (lockObject)
        {
            var filesSet = new HashSet<string>(files.Select(f => f.PathWithoutExtension));

            // Create a list of keys to remove
            var keysToRemove = new List<string>();
            foreach (var key in fileInfoDictionary.Keys)
            {
                if (!filesSet.Contains(key))
                {
                    keysToRemove.Add(key);
                }
            }

            // Remove keys from dictionary
            foreach (var key in keysToRemove)
            {
                fileInfoDictionary.Remove(key);
            }
        }
    }

    public static Dictionary<string, FileInfo> MountFileRegisterDictionary()
    {
        return fileInfoDictionary;
    }

    private static void LoadFileInfoDictionary()
    {
        if (!isLoaded)
        {
            lock (lockObject)
            {
                if (!isLoaded)
                {
                    isLoaded = true;

                    // Check for .uinfo file first
                    if (File.Exists(pathToFileRegister))
                    {
                        var (assets, audio) = RegistryManager.ReadFromUInfoFile(pathToFileRegister);
                        fileInfoDictionary = assets;
#if DEBUG
                        LogsWindowViewModel.Instance.AddLog("Loaded files registry from .uinfo file.", Logger.LogTags.Debug);
#endif
                    }
                    else
                    {
                        string backupUinfoFilePath = FilesRegisterPathConstructor(true);
                        if (File.Exists(backupUinfoFilePath))
                        {
                            var (assets, audio) = RegistryManager.ReadFromUInfoFile(backupUinfoFilePath);
                            fileInfoDictionary = assets;
#if DEBUG
                            LogsWindowViewModel.Instance.AddLog("Loaded files registry from backup .uinfo file.", Logger.LogTags.Debug);
#endif
                            RegistryManager.WriteToUInfoFile(backupUinfoFilePath, assets, audio);
                        }
                    }
                }
            }
        }
    }


    private static readonly Lazy<Dictionary<string, FileInfo>> _newAssets = new(RetrieveNewAssets);
    private static readonly Lazy<Dictionary<string, FileInfo>> _modifiedAssets = new(RetrieveModifiedAssets);

    public static Dictionary<string, FileInfo> NewAssets => _newAssets.Value;
    public static Dictionary<string, FileInfo> ModifiedAssets => _modifiedAssets.Value;

    public static Dictionary<string, FileInfo> RetrieveNewAssets()
    {
        return RetrieveAssets(FindNewAssets);
    }

    public static Dictionary<string, FileInfo> RetrieveModifiedAssets()
    {
        return RetrieveAssets(FindModifiedAssets);
    }

    public static Dictionary<string, FileInfo> RetrieveAssets(Func<Dictionary<string, FileInfo>, Dictionary<string, FileInfo>, Dictionary<string, FileInfo>> findAssets)
    {
        string versionWithBranch = Helpers.ConstructVersionHeaderWithBranch();
        string compareVersionWithBranch = Helpers.ConstructVersionHeaderWithBranch(true);

        string filesRegisterName = $"Core_{versionWithBranch}_FilesRegister.uinfo";
        string compareFilesRegisterName = $"Core_{compareVersionWithBranch}_FilesRegister.uinfo";

        string filesRegisterPath = Path.Combine(filesRegisterDirectoryPath, filesRegisterName);
        string compareFilesRegisterPath = Path.Combine(filesRegisterDirectoryPath, compareFilesRegisterName);

        if (File.Exists(filesRegisterPath) && File.Exists(compareFilesRegisterPath))
        {
            var (assets, _) = RegistryManager.ReadFromUInfoFile(filesRegisterPath);
            var (compareAssets, _) = RegistryManager.ReadFromUInfoFile(compareFilesRegisterPath);

            if (assets == null || compareAssets == null) return [];

            return findAssets(assets, compareAssets); // Choose new or modified method
        }
        else
        {
            LogsWindowViewModel.Instance.AddLog("Not found file registers.", Logger.LogTags.Error);
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            return [];
        }
    }

    private static Dictionary<string, FileInfo> FindNewAssets(Dictionary<string, FileInfo> filesRegister, Dictionary<string, FileInfo> compareFilesRegister)
    {
        var newAssets = new Dictionary<string, FileInfo>();

        foreach (var kvp in filesRegister)
        {
            if (!compareFilesRegister.ContainsKey(kvp.Key))
            {
                newAssets[kvp.Key] = kvp.Value;
            }
        }

        return newAssets;
    }

    private static Dictionary<string, FileInfo> FindModifiedAssets(Dictionary<string, FileInfo> filesRegister, Dictionary<string, FileInfo> compareFilesRegister)
    {
        var modifiedAssets = new Dictionary<string, FileInfo>();

        foreach (var kvp in filesRegister)
        {
            if (compareFilesRegister.TryGetValue(kvp.Key, out FileInfo? comparedFile))
            {
                if (comparedFile != null && kvp.Value.Size != comparedFile.Size)
                {
                    modifiedAssets[kvp.Key] = kvp.Value;
                }
            }
        }

        return modifiedAssets;
    }

    public static FileInfo? GetFileInfo(string filePath)
    {
        if (fileInfoDictionary.TryGetValue(filePath, out FileInfo? value))
        {
            return value;
        }
        else
        {
            return null;
        }
    }

    public static bool DoesFileExist(string filePath)
    {
        return fileInfoDictionary.ContainsKey(filePath); 
    }

    public static bool DoesComparedRegisterExist()
    {
        string compareVersionWithBranch = Helpers.ConstructVersionHeaderWithBranch(true);

        string compareFilesRegisterName = $"Core_{compareVersionWithBranch}_FilesRegister.uinfo";

        string compareFilesRegisterPath = Path.Combine(filesRegisterDirectoryPath, compareFilesRegisterName);

        if (File.Exists(compareFilesRegisterPath))
        {
            return true;
        }

        return false;
    }

    public static HashSet<string> GrabAvailableComparisonVersions(string? currentVersion)
    {
        string compareFilesRegisterPath = Path.Combine(filesRegisterDirectoryPath);

        string pattern = @"Core_(?<version>.+)_FilesRegister\.uinfo";

        HashSet<string> versions = [];

        string[] files = Directory.GetFiles(compareFilesRegisterPath, "*.uinfo");

        foreach (string file in files)
        {
            string fileName = Path.GetFileName(file);

            Match match = Regex.Match(fileName, pattern);

            if (match.Success)
            {
                // Extract the version part from the matched file name
                string version = match.Groups["version"].Value;

                if (version == currentVersion) continue;

                versions.Add(version);
            }
        }

        // Reverse the HashSet by converting to list, reversing, and converting back to HashSet
        versions = new HashSet<string>(versions.Reverse());

        return versions;
    }
}