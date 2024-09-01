using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Xml;
using UEParser.ViewModels;
using System.Text.RegularExpressions;
using UEParser.Utils;
using UEParser.Models;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace UEParser.Parser.Wwise;

public partial class WwiseFileHandler
{
    private static readonly string temporaryDirectory = Path.Combine(GlobalVariables.pathToExtractedAudio, "WwiseTemporary");
    private static readonly string wwiseStructured = Path.Combine(GlobalVariables.pathToExtractedAudio, "WwiseStructured");
    private static readonly string pathToUEAssetsRegistry = Path.Combine(GlobalVariables.pathToExtractedAssets, "DeadByDaylight", "AssetRegistry.json");

    //public static (string filePath, string dataType) FindSoundBank()
    //{
    //    string soundsBankName = "SoundbanksInfo";

    //    string[] filePathsJson = Helpers.FindFilePathsInExtractedAssetsCaseInsensitive(soundsBankName + ".json");
    //    string[] filePathsXml = Helpers.FindFilePathsInExtractedAssetsCaseInsensitive(soundsBankName + ".xml");

    //    if (filePathsJson.Length == 0)
    //    {
    //        LogsWindowViewModel.Instance.AddLog("Sound bank wasn't found in JSON format, which is prefered.. searching for alternatives..", Logger.LogTags.Warning);

    //        if (filePathsXml.Length == 1)
    //        {
    //            string soundBankPath = filePathsXml[0];
    //            string dataType = "xml";

    //            return (soundBankPath, dataType); 
    //        }
    //        else
    //        {
    //            throw new Exception("Failed to find any Sound Banks.");
    //        }
    //    }
    //    else
    //    {
    //        var soundBankPath = filePathsJson[0];
    //        string dataType = "json";

    //        return (soundBankPath, dataType);
    //    }
    //}

    //public static bool DoesSoundBankExist()
    //{
    //    string soundsBankNameJson = "SoundbanksInfo.json";
    //    string[] filePathsJson = Helpers.FindFilePathsInExtractedAssetsCaseInsensitive(soundsBankNameJson);

    //    if (filePathsJson.Length > 0)
    //    {
    //        return true;
    //    }

    //    return false;
    //}

    public static void MoveCompressedAudio()
    {
        string pathToExtractedWwiseDirectory = Path.Combine(GlobalVariables.pathToExtractedAssets, "DeadByDaylight", "Content", "WwiseAudio");

        string[] fileExtensions = [
            "*.bnk",
            "*.wem"
        ];
        var filesToMove = fileExtensions.SelectMany(ext => Directory.GetFiles(pathToExtractedWwiseDirectory, ext, SearchOption.AllDirectories)).ToArray();

        if (!Directory.Exists(temporaryDirectory))
        {
            Directory.CreateDirectory(temporaryDirectory);
        }

        foreach (var filePath in filesToMove)
        {
            try
            {
                string fileName = Path.GetFileName(filePath);
                string destFilePath = Path.Combine(temporaryDirectory, fileName);

                if (File.Exists(destFilePath)) continue;

                File.Copy(filePath, destFilePath, overwrite: true);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to move compressed audio to temporary directory: {ex.Message}");
            }
        }
    }

    // Provide wwnames file (an artificial list of possible Wwise names) that will be used to reverse audio event names
    private static void ProvideWwnamesFile()
    {
        if (!File.Exists(pathToUEAssetsRegistry)) throw new Exception("Not found Unreal Engine assets registry.");

        string destinationFilePath = Path.Combine(temporaryDirectory, "wwnames.txt");

        File.Copy(pathToUEAssetsRegistry, destinationFilePath, true);
    }

    // Generate txtp files that will be used to play audio simulating Wwise
    public static void GenerateTxtp()
    {
        string bnkFilesDirectory = Path.Combine(temporaryDirectory, "*.bnk");

        ProvideWwnamesFile(); // Neccessary for reversing audio names

        string arguments = $"\"{GlobalVariables.wwiserPath}\" \"{bnkFilesDirectory}\" -g -go \"{temporaryDirectory}\"";

        // Generate txtp files with reversed names that we will use to convert to playable audio
        WwiseUtilities.ExecuteCommand(arguments, "python");
    }

    [GeneratedRegex(@"CAkEvent\[\d+\] (\d+)")]
    private static partial Regex CAkEventRegex();
    // Returns audio event ids associated with audio
    public static Dictionary<string, long> GrabAudioEventIds()
    {
        string[] txtpFiles = Directory.GetFiles(temporaryDirectory, "*.txtp", SearchOption.AllDirectories);

        if (txtpFiles.Length == 0) throw new Exception("Failed to find any generated txtp files.");

        Dictionary<string, long> kvp = [];
        foreach (string filePath in txtpFiles)
        {
            string fileContent = File.ReadAllText(filePath);

            Match match = CAkEventRegex().Match(fileContent);

            if (match.Success)
            {
                string cakEventId = match.Groups[1].Value; // Wwise audio event id
                kvp[filePath] = long.Parse(cakEventId);
            }
            else
            {
                LogsWindowViewModel.Instance.AddLog($"No CAkEvent ID found in: {filePath}", Logger.LogTags.Warning);
            }
        }

        return kvp;
    }

    public class AudioEventsLinkageData
    { 
        public required string PackagePath { get; set; }
        public required string WwiseName { get; set; }
        public required string WwiseGuid { get; set; }
    }

    public static Dictionary<long, AudioEventsLinkageData> ConstructAudioEventsLinkage()
    {
        dynamic assetsRegistryData = FileUtils.LoadDynamicJson(pathToUEAssetsRegistry);
        var preallocatedAssetDataBuffers = assetsRegistryData["PreallocatedAssetDataBuffers"];

        Dictionary<long, AudioEventsLinkageData> kvp = [];
        foreach (var assetData in preallocatedAssetDataBuffers)
        {
            string? assetClass = assetData["AssetClass"].ToString();

            // We only want to map audio events
            if (assetClass != "AkAudioEvent") continue;

            // In case package path is null this mapping won't be useful anyway
            string? packagePath = assetData["PackagePath"].ToString();

            if (packagePath == null) continue;

            var tagsAndValues = assetData["TagsAndValues"];

            if (tagsAndValues.TryGetValue("WwiseShortId", out JToken wwiseIdToken))
            {
                string wwiseId = wwiseIdToken.ToString(); 
                if (int.TryParse(wwiseId, out int wwiseIdInt))
                {
                    long adjustedWwiseId = wwiseIdInt;

                    // If audio event id is negative we need to add maxium integer value and use that instead
                    // For some reason ids overflow to negativity in assets registry and don't match to compiled audio event ids
                    if (wwiseIdInt < 0)
                    {
                        adjustedWwiseId = wwiseIdInt + 2L * int.MaxValue;
                    }

                    string wwiseName = tagsAndValues["WwiseName"].ToString();
                    string wwiseGuid = tagsAndValues["WwiseGuid"].ToString();

                    AudioEventsLinkageData model = new()
                    {
                        PackagePath = packagePath,
                        WwiseName = wwiseName,
                        WwiseGuid = wwiseGuid
                    };

                    kvp[adjustedWwiseId] = model;
                }
            };
        }

        return kvp;
    }

    public static void ReverseAudioStructure(Dictionary<string, long> associatedAudioEventIds, Dictionary<long, AudioEventsLinkageData> audioEventsLinkage)
    {
        foreach (var audioEvent in associatedAudioEventIds)
        {
            if (audioEventsLinkage.TryGetValue(audioEvent.Value, out AudioEventsLinkageData? audioEventData))
            {
                string reversedFileName = Path.GetFileName(audioEvent.Key);

                string combinedPath = Path.Combine(wwiseStructured, audioEventData.PackagePath.TrimStart('/'), reversedFileName);

                string finalPath = combinedPath.Replace("\\Game/", "/DeadByDaylight/");

                // TODO: create audio registry here

                Directory.CreateDirectory(Path.GetDirectoryName(finalPath) ?? string.Empty);

                File.Move(audioEvent.Key, finalPath);
            };
        }
    }

    // Generated txtp file contains relative path to audio banks
    // which won't match after we reverse audio structure
    private static void ConvertTxtpRelativePathsToAbsolute()
    {
        string[] txtpFiles = Directory.GetFiles(wwiseStructured, "*.txtp", SearchOption.AllDirectories);

        foreach (var txtpFilePath in txtpFiles)
        {
            string content = File.ReadAllText(txtpFilePath);

            string pattern = @"(?<=\s|^)(\.\./)+[\w/.\-]+(?=\s|$)";

            var matches = Regex.Matches(content, pattern);

            foreach (Match match in matches)
            {
                string relativePath = match.Value;
                string absolutePath = ConvertRelativePathToAbsolute(temporaryDirectory, relativePath);

                // Replace the relative path in the content with the absolute path
                content = content.Replace(relativePath, absolutePath);
            }

            // Write the updated content back to the .txtp file
            File.WriteAllText(txtpFilePath, content);
        }
    }

    private static string ConvertRelativePathToAbsolute(string baseDirectory, string relativePath)
    {
        try
        {
            relativePath = relativePath.Replace('/', Path.DirectorySeparatorChar);

            string fileName = Path.GetFileName(relativePath);

            string fullPath = Path.Combine(baseDirectory, fileName);

            return Path.GetFullPath(fullPath);
        }
        catch
        {
            return relativePath; // Return the original path in case of error
        }
    }

    //public static void ConvertTxtpToWav()
    //{
    //    // Get all .txtp files in the directory and its subdirectories
    //    var txtpFiles = Directory.GetFiles(wwiseStructured, "*.txtp", SearchOption.AllDirectories);

    //    ConvertTxtpRelativePathsToAbsolute();

    //    foreach (var filePath in txtpFiles)
    //    {
    //        // Construct the output .wav file path
    //        string outputFilePath = Path.ChangeExtension(filePath, ".wav");

    //        // Prepare the arguments for the vgmstream-cli command
    //        string arguments = $"-i -o \"{outputFilePath}\" \"{filePath}\"";

    //        // Convert txtp files to wav audio format
    //        WwiseUtilities.ExecuteCommand(arguments, GlobalVariables.vgmStreamCliPath);

    //        // If the command was successful, delete the .txtp file
    //        if (File.Exists(outputFilePath))
    //        {
    //            try
    //            {
    //                File.Delete(filePath);
    //            }
    //            catch (Exception ex)
    //            {
    //                LogsWindowViewModel.Instance.AddLog($"Failed to delete {filePath}: {ex.Message}", Logger.LogTags.Warning);
    //            }
    //        }
    //        else
    //        {
    //            LogsWindowViewModel.Instance.AddLog($"Conversion failed or .wav file was not created for: {filePath}", Logger.LogTags.Warning);
    //        }
    //    }
    //}

    public static void ConvertTxtpToWav()
    {
        // Get all .txtp files in the directory and its subdirectories
        var txtpFiles = Directory.GetFiles(wwiseStructured, "*.txtp", SearchOption.AllDirectories);

        ConvertTxtpRelativePathsToAbsolute();

        // Convert txtp files to wav audio format
        foreach (var filePath in txtpFiles)
        {
            // TODO: only convert files that are new/modified by reading audio registry

            // Generate a temporary short name
            string tempFilePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".txtp");

            try
            {
                // Copy the original file to the temporary path
                // Some reversed names are too long for conversion to work
                File.Copy(filePath, tempFilePath, overwrite: true);

                // Construct the output .wav file path
                string outputFilePath = Path.ChangeExtension(tempFilePath, ".wav");

                // Prepare the arguments for the vgmstream-cli command
                string arguments = $"-i -o \"{outputFilePath}\" \"{tempFilePath}\"";

                // Convert txtp files to wav audio format
                WwiseUtilities.ExecuteCommand(arguments, GlobalVariables.vgmStreamCliPath);

                // If the command was successful, delete the .txtp file
                if (File.Exists(outputFilePath))
                {
                    // Move the .wav file to the original location
                    File.Move(outputFilePath, Path.ChangeExtension(filePath, ".wav"));
                    File.Delete(tempFilePath);
                    File.Delete(filePath);
                }
                else
                {
                    LogsWindowViewModel.Instance.AddLog($"Conversion failed or .wav file was not created for: {filePath}", Logger.LogTags.Warning);
                    File.Delete(tempFilePath);
                }
            }
            catch (Exception ex)
            {
                LogsWindowViewModel.Instance.AddLog($"Error processing file {filePath.Replace(wwiseStructured, "").TrimStart(Path.AltDirectorySeparatorChar).TrimStart(Path.PathSeparator)}: {ex.Message}", Logger.LogTags.Warning);

                // Clean up temporary files if any exception occurs
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
        }
    }

    //public static async Task UnpackAudioBanks()
    //{
    //    string[] bnkFiles = Directory.GetFiles(temporaryDirectory, "*.bnk", SearchOption.AllDirectories);

    //    if (bnkFiles.Length == 0)
    //    {
    //        return;
    //    }

    //    List<WwiseUtilities.CommandModel> commands = [];
    //    foreach (var bnkFilePath in bnkFiles)
    //    {
    //        string command = $"--audio {bnkFilePath} --output {temporaryDirectory} --wems-only";

    //        WwiseUtilities.CommandModel model = new()
    //        {
    //            Argument = command,
    //            PathToExe = GlobalVariables.bnkExtractorPath

    //        };
    //        commands.Add(model);
    //    }

    //    await WwiseUtilities.ExecuteCommandsAsync(commands);
    //}

    //public static void ConvertToOggAndMove()
    //{
    //    string[] wemFiles = Directory.GetFiles(wwiseStructured, "*.wem", SearchOption.AllDirectories);

    //    var audioToParse = WwiseRegister.RetrieveAudioToParse(wemFiles);

    //    var commands = new List<WwiseUtilities.CommandModel>();

    //    int failedAudioConversionCount = 0;
    //    Parallel.ForEach(wemFiles, audioPath =>
    //    {
    //        if (!audioToParse.Contains(audioPath)) return;

    //        string relativePath = Utils.StringUtils.StripDynamicDirectory(audioPath, wwiseStructured);
    //        string tempOggPath = Path.ChangeExtension(audioPath, ".ogg");

    //        string targetDirectory = Path.Combine(GlobalVariables.rootDir, "Output", "ExtractedAssets", "Audio", GlobalVariables.versionWithBranch);
    //        string finalOggPath = Path.Combine(targetDirectory, Path.ChangeExtension(relativePath, ".ogg"));

    //        Directory.CreateDirectory(Path.GetDirectoryName(finalOggPath)!);

    //        // Convert .wem to .ogg using ww2ogg
    //        WwiseUtilities.ExecuteCommand($"\"{audioPath}\" --pcb \"{GlobalVariables.packedCodebooksPath}\"", GlobalVariables.ww2oggPath);

    //        if (File.Exists(tempOggPath))
    //        {
    //            // Compress .ogg using revorb
    //            WwiseUtilities.ExecuteCommand($"\"{tempOggPath}\"", GlobalVariables.revorbPath);
    //        }
    //        else
    //        {
    //            Interlocked.Increment(ref failedAudioConversionCount); // Thread-safe increment of failedAudioConversionCount
    //            //failedAudioConversionCount++;
    //        }
    //    });

    //    if (failedAudioConversionCount > 0)
    //    {
    //        LogsWindowViewModel.Instance.AddLog($"Failed to convert total of {failedAudioConversionCount} files to OGG format.", Logger.LogTags.Warning);
    //    }

    //    MoveAudioToOutput();
    //}

    private static void MoveAudioToOutput()
    {
        LogsWindowViewModel.Instance.AddLog("Moving converted audio into output.", Logger.LogTags.Info);

        string[] oggFiles = Directory.GetFiles(wwiseStructured, "*.ogg", SearchOption.AllDirectories);
        foreach (var tempOggPath in oggFiles)
        {
            try
            {
                string relativePath = Utils.StringUtils.StripDynamicDirectory(tempOggPath, wwiseStructured);
                string finalOggPath = Path.Combine(GlobalVariables.rootDir, "Output", "ExtractedAssets", "Audio", GlobalVariables.versionWithBranch, relativePath);

                Directory.CreateDirectory(Path.GetDirectoryName(finalOggPath)!);

                if (!File.Exists(tempOggPath))
                {
                    continue;
                }

                File.Copy(tempOggPath, finalOggPath, overwrite: true);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error copying file {tempOggPath} to target directory: {ex.Message}");
            }
        }
    }

    public static void CleanExtractedAudioDir()
    {
        string directoryPath = GlobalVariables.pathToExtractedAudio;
        if (Directory.Exists(directoryPath))
        {
            try
            {
                Directory.Delete(directoryPath, true);
            }
            catch
            {
                // do nothing
            }
        }
    }

    // Only call this after sound banks have been unpacked
    // Deprecated with 8.2.0_live update, new solution needs to be found
    //public static void StructureAudio()
    //{
    //    var soundBankDictionary = WwiseRegister.SoundBankDictionary;

    //    string[] wemFiles = Directory.GetFiles(temporaryDirectory, "*.wem", SearchOption.AllDirectories);

    //    static void MoveFile(string wemFilePath, string structuredOutputPath)
    //    {
    //        try
    //        {
    //            File.Move(wemFilePath, structuredOutputPath, true);
    //        }
    //        catch (Exception ex)
    //        {
    //            throw new Exception($"Error moving file {wemFilePath} to {structuredOutputPath}: {ex.Message}");
    //        }
    //    }

    //    foreach (var wemFilePath in wemFiles)
    //    {
    //        string id = Path.GetFileNameWithoutExtension(wemFilePath);

    //        string? structuredPath = soundBankDictionary[id];

    //        if (string.IsNullOrEmpty(structuredPath))
    //        {
    //            string structuredOutputPath = Path.Combine(wwiseStructured, "Unknown");
    //            Directory.CreateDirectory(Path.GetDirectoryName(structuredOutputPath)!);

    //            MoveFile(wemFilePath, structuredOutputPath);
    //        }
    //        else
    //        {
    //            string structuredOutputPath = Path.Combine(wwiseStructured, structuredPath);

    //            Directory.CreateDirectory(Path.GetDirectoryName(structuredOutputPath)!);

    //            MoveFile(wemFilePath, structuredOutputPath);
    //        }
    //    }
    //}
}