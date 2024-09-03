using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Collections.Concurrent;
using UEParser.ViewModels;

namespace UEParser.AssetRegistry.Wwise;

public partial class WwiseRegister
{
#pragma warning disable IDE0044
    private static ConcurrentDictionary<string, AudioInfo> AudioInfoDictionary = [];
#pragma warning restore IDE0044

    private static readonly string filesRegisterDirectoryPath = Path.Combine(GlobalVariables.rootDir, "Dependencies", "FilesRegister");
    private static readonly string PathToAudioRegister;

    public class AudioInfo(string hash, long size)
    {
        public string Hash { get; set; } = hash;
        public long Size { get; set; } = size;
    }

    static WwiseRegister()
    {
        PathToAudioRegister = ConstructPathToAudioRegister();
    }

    private static string ConstructPathToAudioRegister(bool isComparisonVersion=false)
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
                    string compareHash = CalculateFileHash(txtpFilePath);

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

            string hash = CalculateFileHash(txtpFilePath);
            long size = new FileInfo(txtpFilePath).Length;

            UpdateAudioRegister(txtpFileName, hash, size);
        });
    }

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