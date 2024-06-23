using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using UEParser.Services;

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
            string version = config.Core.VersionData.CompareVersionHeader;
            string branch = config.Core.VersionData.CompareBranch.ToString();

            string filesRegisterName = $"Core_{version}_{branch}_FilesRegister.json";
            string path = Path.Combine(filesRegisterDirectoryPath, filesRegisterName);

            return path;
        }
        else
        {
            string version = config.Core.VersionData.LatestVersionHeader;
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
        Logger.SaveLog("Saved files register", Logger.LogTags.Info);
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
}