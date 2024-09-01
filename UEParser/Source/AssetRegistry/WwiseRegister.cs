using Newtonsoft.Json;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;
using System.Xml;
using System.Xml.Linq;
using UEParser.Models;
using UEParser.Utils;
using UEParser.ViewModels;
using UEParser.Parser.Wwise;

namespace UEParser.AssetRegistry.Wwise;

public partial class WwiseRegister
{
#pragma warning disable IDE0044
    private static ConcurrentDictionary<string, AudioInfo> AudioInfoDictionary = [];
#pragma warning restore IDE0044

    private static readonly string filesRegisterDirectoryPath = Path.Combine(GlobalVariables.rootDir, "Dependencies", "FilesRegister");
    private static readonly string PathToAudioRegister;

    //public static Dictionary<string, string> SoundBankDictionary { get; private set; }

    public class AudioInfo(string id, string hash, long size)
    {
        public string Id { get; set; } = id;
        public string Hash { get; set; } = hash;
        public long Size { get; set; } = size;
    }

    static WwiseRegister()
    {
        PathToAudioRegister = ConstructPathToAudioRegister();
        //SoundBankDictionary = PopulateSoundBankDictionary();
    }

    private static string ConstructPathToAudioRegister(bool isComparisonVersion=false)
    {
        string versionWithBranch = isComparisonVersion ? GlobalVariables.compareVersionWithBranch : GlobalVariables.versionWithBranch;
        string audioRegisterName = $"Core_{versionWithBranch}_FilesRegister.uinfo";
        return Path.Combine(filesRegisterDirectoryPath, audioRegisterName);
    }

    //[GeneratedRegex("_[0-9A-F]+\\.wem$")]
    //private static partial Regex WemFileHashRegex();
    //public static Dictionary<string, string> PopulateSoundBankDictionary()
    //{
    //    var (soundBankData, dataType) = LoadSoundsBank();

    //    Dictionary<string, string> kvp = [];

    //    if (dataType == "json")
    //    {
    //        var soundBanksDataJson = (SoundBanksInfoRoot)soundBankData;
    //        var soundBanksList = soundBanksDataJson.SoundBanksInfo.SoundBanks;
    //        foreach (var soundBank in soundBanksList)
    //        {
    //            var media = soundBank?.Media;

    //            if (media == null) continue;

    //            foreach (var mediaFile in media)
    //            {
    //                string id = mediaFile.Id;
    //                string path = mediaFile.CachePath;
    //                string pathWithoutHash = WemFileHashRegex().Replace(path, ".wem");
    //                kvp[id] = pathWithoutHash;
    //            }
    //        }
    //    }
    //    else if (dataType == "xml")
    //    {
    //        var soundBanksDataXml = (XmlDocument)soundBankData;
    //        XmlNodeList? files = soundBanksDataXml.SelectNodes("//File");

    //        if (files == null)
    //        {
    //            return kvp;
    //        }

    //        foreach (XmlNode file in files)
    //        {
    //            string? id = file.Attributes?["Id"]?.Value;
    //            string? path = file["Path"]?.InnerText;

    //            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(path)) continue;

    //            string pathWithoutHash = WemFileHashRegex().Replace(path, ".wem")
    //                .Replace(Path.DirectorySeparatorChar, '/')
    //                .Replace(Path.AltDirectorySeparatorChar, '/');

    //            kvp[id] = pathWithoutHash;
    //        }
    //    }

    //    return kvp;
    //}

    //private static (object, string) LoadSoundsBank()
    //{
    //    var (filePath, dataType) = WwiseFileHandler.FindSoundBank();

    //    switch (dataType)
    //    {
    //        case "json":
    //            var data = FileUtils.LoadJsonFileWithTypeCheck<SoundBanksInfoRoot>(filePath);
    //            return (data, dataType);
    //        case "xml":
    //            var xmlDocument = new XmlDocument();
    //            xmlDocument.Load(filePath);
    //            return (xmlDocument, dataType);
    //        default:
    //            throw new Exception("Invalid Sound Bank format.");
    //    }
    //}

    // Single thread method
    //public static void UpdateAudioRegister(string id, string path, string hash, long size)
    //{
    //    if (AudioInfoDictionary.ContainsKey(path))
    //    {
    //        // Update existing AudioInfo
    //        AudioInfoDictionary[path] = new AudioInfo(id, hash, size);
    //    }
    //    else
    //    {
    //        // Add new AudioInfo
    //        AudioInfoDictionary.Add(path, new AudioInfo(id, hash, size));
    //    }
    //}

    // Multi thread method
    public static void UpdateAudioRegister(string id, string path, string hash, long size)
    {
        // Update or add a new AudioInfo entry in a thread-safe manner
        AudioInfoDictionary.AddOrUpdate(
            path,
            new AudioInfo(id, hash, size), // Value to add if the key doesn't exist
            (key, existingValue) => new AudioInfo(id, hash, size) // Value to update if the key does exist
        );
    }

    private static ConcurrentDictionary<string, AudioInfo> LoadCompareAudioRegister()
    {
        string pathToCompareAudioRegister = ConstructPathToAudioRegister(true);

        if (File.Exists(pathToCompareAudioRegister))
        {
            var (_, audio) = RegistryManager.ReadFromUInfoFile(pathToCompareAudioRegister);

#if DEBUG
            LogsWindowViewModel.Instance.AddLog("Loaded audio registry from .uinfo file.", Logger.LogTags.Debug);
#endif
            ConcurrentDictionary<string, AudioInfo> concurrentAudio = new(audio);
            return concurrentAudio;
        }
        else
        {
            LogsWindowViewModel.Instance.AddLog("Audio register does not exist, audio database is gonna be processed in its entirety.", Logger.LogTags.Warning);
            return [];
        }
    }

    public static List<string> RetrieveAudioToParse(string[] wemFiles)
    {
        List<string> audioToParse = [];
        var comparisonAudioRegister = LoadCompareAudioRegister();

        if (!comparisonAudioRegister.IsEmpty)
        {
            foreach (string wemFilePath in wemFiles)
            {
                string relativeWemFilePath = StringUtils.StripDynamicDirectory(wemFilePath, GlobalVariables.pathToStructuredWwise)
                    .Replace(Path.DirectorySeparatorChar, '/')
                    .Replace(Path.AltDirectorySeparatorChar, '/');

                comparisonAudioRegister.TryGetValue(relativeWemFilePath, out AudioInfo? audioInfo);

                if (audioInfo == null)
                {
                    audioToParse.Add(wemFilePath);
                }
                else
                {
                    string compareHash = CalculateFileHash(wemFilePath);

                    if (compareHash != audioInfo.Hash)
                    {
                        audioToParse.Add(wemFilePath);
                    }
                }
            }
        }
        else
        {
            audioToParse = new List<string>(wemFiles);
        }

        return audioToParse;
    }

    //public static void PopulateAudioRegister()
    //{
    //    string pathToStructuredWwise = GlobalVariables.pathToStructuredWwise;

    //    if (!Directory.Exists(pathToStructuredWwise))
    //    {
    //        LogsWindowViewModel.Instance.AddLog("Failed to construct Audio Register, extracted Wwise data does not exist.", Logger.LogTags.Error);
    //        return;
    //    }

    //    string[] wemFiles = Directory.GetFiles(pathToStructuredWwise, "*.wem", SearchOption.AllDirectories);

    //    var reversedSoundBankDictionary = SoundBankDictionary
    //        .GroupBy(kvp => kvp.Value)
    //        .ToDictionary(group => group.Key, group => group.First().Key);

    //    //foreach (var wemFilePath in wemFiles)
    //    //{
    //    //    string relativeWemFilePath = StringUtils.StripDynamicDirectory(wemFilePath, pathToStructuredWwise);

    //    //    string id = reversedSoundBankDictionary[relativeWemFilePath.Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/')];
    //    //    string hash = CalculateFileHash(wemFilePath);
    //    //    long size = new FileInfo(wemFilePath).Length;

    //    //    UpdateAudioRegister(id, relativeWemFilePath, hash, size);
    //    //}

    //    Parallel.ForEach(wemFiles, wemFilePath =>
    //    {
    //        string relativeWemFilePath = StringUtils.StripDynamicDirectory(wemFilePath, pathToStructuredWwise)
    //            .Replace(Path.DirectorySeparatorChar, '/')
    //            .Replace(Path.AltDirectorySeparatorChar, '/');

    //        if (reversedSoundBankDictionary.TryGetValue(relativeWemFilePath, out string? id))
    //        {
    //            string hash = CalculateFileHash(wemFilePath);
    //            long size = new FileInfo(wemFilePath).Length;

    //            UpdateAudioRegister(id, relativeWemFilePath, hash, size);
    //        }
    //    });
    //}

    private static string CalculateFileHash(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        byte[] hashBytes = sha256.ComputeHash(stream);

        return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
    }

    public static void SaveAudioInfoDictionary()
    {
        var (assets, _) = RegistryManager.ReadFromUInfoFile(PathToAudioRegister);
        RegistryManager.WriteToUInfoFile(PathToAudioRegister, assets, AudioInfoDictionary);

        LogsWindowViewModel.Instance.AddLog("Saved audio register.", Logger.LogTags.Info);
    }
}