using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UEParser.Services;
using UEParser.ViewModels;
using CUE4Parse.FileProvider.Objects;
using System.Text.RegularExpressions;

namespace UEParser.Parser;

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

            string filesRegisterName = $"Core_{version}_{branch}_FilesRegister.json";
            string path = Path.Combine(filesRegisterDirectoryPath, filesRegisterName);

            return path;
        }
        else
        {
            string? version = config.Core.VersionData.LatestVersionHeader;
            string branch = config.Core.VersionData.Branch.ToString();

            string filesRegisterName = $"Core_{version}_{branch}_FilesRegister.json";
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
        string json = JsonConvert.SerializeObject(fileInfoDictionary);

        File.WriteAllText(pathToFileRegister, json);
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

                    if (File.Exists(pathToFileRegister))
                    {
                        string json = File.ReadAllText(pathToFileRegister);
                        fileInfoDictionary = JsonConvert.DeserializeObject<Dictionary<string, FileInfo>>(json) ?? [];
                    }
                    else
                    {
                        string pathToBackupFileRegister = FilesRegisterPathConstructor(true);
                        if (File.Exists(pathToBackupFileRegister))
                        {
                            string jsonBackup = File.ReadAllText(pathToBackupFileRegister);
                            fileInfoDictionary = JsonConvert.DeserializeObject<Dictionary<string, FileInfo>>(jsonBackup) ?? [];
                            File.WriteAllText(pathToFileRegister, jsonBackup);
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

        string filesRegisterName = $"Core_{versionWithBranch}_FilesRegister.json";
        string compareFilesRegisterName = $"Core_{compareVersionWithBranch}_FilesRegister.json";

        string filesRegisterPath = Path.Combine(filesRegisterDirectoryPath, filesRegisterName);
        string compareFilesRegisterPath = Path.Combine(filesRegisterDirectoryPath, compareFilesRegisterName);

        if (File.Exists(filesRegisterPath) && File.Exists(compareFilesRegisterPath))
        {
            string filesRegisterJson = File.ReadAllText(filesRegisterPath);
            string compareFilesRegisterJson = File.ReadAllText(compareFilesRegisterPath);

            var filesRegister = JsonConvert.DeserializeObject<Dictionary<string, FileInfo>>(filesRegisterJson);
            var compareFilesRegister = JsonConvert.DeserializeObject<Dictionary<string, FileInfo>>(compareFilesRegisterJson);

            if (filesRegister == null || compareFilesRegister == null) return [];

            return findAssets(filesRegister, compareFilesRegister); // Choose new or modified method
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

        string compareFilesRegisterName = $"Core_{compareVersionWithBranch}_FilesRegister.json";

        string compareFilesRegisterPath = Path.Combine(filesRegisterDirectoryPath, compareFilesRegisterName);

        if (File.Exists(compareFilesRegisterPath))
        {
            return true;
        }

        return false;
    }

    public static HashSet<string> GrabAvailableComparisonVersions()
    {
        string compareFilesRegisterPath = Path.Combine(filesRegisterDirectoryPath);

        string pattern = @"Core_(?<version>.+)_FilesRegister\.json";

        HashSet<string> versions = [];

        string[] files = Directory.GetFiles(compareFilesRegisterPath, "*.json");

        foreach (string file in files)
        {
            string fileName = Path.GetFileName(file);

            Match match = Regex.Match(fileName, pattern);

            if (match.Success)
            {
                // Extract the version part from the matched file name
                string version = match.Groups["version"].Value;

                versions.Add(version);
            }
        }

        // Reverse the HashSet by converting to list, reversing, and converting back to HashSet
        versions = new HashSet<string>(versions.Reverse());

        return versions;
    }
}