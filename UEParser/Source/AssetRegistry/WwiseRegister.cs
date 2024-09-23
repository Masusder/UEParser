using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Collections.Concurrent;
using UEParser.ViewModels;
using UEParser.Parser.Wwise;

namespace UEParser.AssetRegistry.Wwise;

public partial class WwiseRegister
{
#pragma warning disable IDE0044
    private static ConcurrentDictionary<string, AudioInfo> AudioInfoDictionary = [];
#pragma warning restore IDE0044
    // TODO: Can be potentially used to handle unused audio
    private readonly static HashSet<string> WemFilesCollection = [];

    private static readonly string filesRegisterDirectoryPath = Path.Combine(GlobalVariables.rootDir, "Dependencies", "FilesRegister");
    public static readonly string PathToAudioRegister;

    public class AudioInfo(string hash, long size)
    {
        public string Hash { get; set; } = hash;
        public long Size { get; set; } = size;
    }

    static WwiseRegister()
    {
        PathToAudioRegister = ConstructPathToAudioRegister();
    }

    private static string ConstructPathToAudioRegister(bool isComparisonVersion = false)
    {
        string versionWithBranch = isComparisonVersion ? GlobalVariables.compareVersionWithBranch : GlobalVariables.versionWithBranch;
        string audioRegisterName = $"Core_{versionWithBranch}_FilesRegister.uinfo";
        return Path.Combine(filesRegisterDirectoryPath, audioRegisterName);
    }

    // Multi thread method
    public static void UpdateAudioRegister(string fileName, string hash, long size)
    {
        // Update or add a new AudioInfo entry in a thread-safe manner
        AudioInfoDictionary.AddOrUpdate(
            fileName,
            new AudioInfo(hash, size), // Value to add if the key doesn't exist
            (key, existingValue) => new AudioInfo(hash, size) // Value to update if the key does exist
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

    public static List<string> RetrieveAudioToParse(string[] txtpFiles)
    {
        List<string> audioToParse = [];
        var comparisonAudioRegister = LoadCompareAudioRegister();

        if (!comparisonAudioRegister.IsEmpty)
        {
            foreach (string txtpFilePath in txtpFiles)
            {
                string txtpFileName = Path.GetFileName(txtpFilePath);

                comparisonAudioRegister.TryGetValue(txtpFileName, out AudioInfo? audioInfo);

                if (audioInfo == null)
                {
                    audioToParse.Add(txtpFilePath);
                }
                else
                {
                    string txtpFileContent = File.ReadAllText(txtpFilePath);

                    string[] wemFiles = WwiseFileHandler.CollectWemFiles(txtpFileContent);

                    string compareHash = CalculateHashForMultipleFiles(wemFiles);

                    if (compareHash != audioInfo.Hash)
                    {
                        audioToParse.Add(txtpFilePath);
                    }
                }
            }
        }
        else
        {
            audioToParse = new List<string>(txtpFiles);
        }

        return audioToParse;
    }

    public static void PopulateAudioRegister()
    {
        string pathToTemporaryWwise = GlobalVariables.pathToTemporaryWwise;

        if (!Directory.Exists(pathToTemporaryWwise))
        {
            throw new Exception("Failed to construct Audio Register, extracted Wwise data does not exist.");
        }

        string[] txtpFiles = Directory.GetFiles(pathToTemporaryWwise, "*.txtp", SearchOption.AllDirectories);

        Parallel.ForEach(txtpFiles, txtpFilePath =>
        {
            string txtpFileName = Path.GetFileName(txtpFilePath);
            string txtpFileContent = File.ReadAllText(txtpFilePath);

            string[] wemFiles = WwiseFileHandler.CollectWemFiles(txtpFileContent);

            // Collect all used wem files
            lock (WemFilesCollection) // Use lock to ensure thread safety
            {
                foreach (var wemFile in wemFiles)
                {
                    WemFilesCollection.Add(wemFile);
                }
            }

            string hash = CalculateHashForMultipleFiles(wemFiles);
            long size = new FileInfo(txtpFilePath).Length;

            UpdateAudioRegister(txtpFileName, hash, size);
        });
    }

    private const string SHA256Empty = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855"; // SHA256 for empty data
    // We will calculate .txtp hash based on collection of included wem files
    private static string CalculateHashForMultipleFiles(string[] filePaths)
    {
        Array.Sort(filePaths); // Make sure wem files order is consistent

        using SHA256 sha256 = SHA256.Create();

        foreach (var filePath in filePaths)
        {
            if (File.Exists(filePath))
            {
                using var fileStream = File.OpenRead(filePath);
                // Update the hash with the contents of each valid file
                sha256.TransformBlock(ReadFully(fileStream), 0, (int)fileStream.Length, null, 0);
            }
        }

        sha256.TransformFinalBlock([], 0, 0);

        byte[] hashBytes = sha256.Hash ?? [];

        if (hashBytes.Length == 0)
        {
            return SHA256Empty;
        }

        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }

    static byte[] ReadFully(Stream input)
    {
        using MemoryStream ms = new();
        input.CopyTo(ms);
        return ms.ToArray();
    }

    //private static string CalculateFileHash(string filePath)
    //{
    //    using var sha256 = SHA256.Create();
    //    using var stream = File.OpenRead(filePath);
    //    byte[] hashBytes = sha256.ComputeHash(stream);

    //    return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
    //}

    public static void SaveAudioInfoDictionary()
    {
        var (assets, _) = RegistryManager.ReadFromUInfoFile(PathToAudioRegister);
        RegistryManager.WriteToUInfoFile(PathToAudioRegister, assets, AudioInfoDictionary);

        LogsWindowViewModel.Instance.AddLog("Saved audio register.", Logger.LogTags.Info);
    }
}