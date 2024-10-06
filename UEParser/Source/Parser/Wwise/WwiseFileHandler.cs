using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UEParser.AssetRegistry.Wwise;
using UEParser.Utils;
using UEParser.ViewModels;

namespace UEParser.Parser.Wwise;

public partial class WwiseFileHandler
{
    private static readonly string TemporaryDirectory = Path.Combine(GlobalVariables.pathToExtractedAudio, "WwiseTemporary");
    private static readonly string WwiseStructured = Path.Combine(GlobalVariables.pathToExtractedAudio, "WwiseStructured");
    private static readonly string PathToUEAssetsRegistry = Path.Combine(GlobalVariables.pathToExtractedAssets, "DeadByDaylight", "AssetRegistry.json");

    public static void MoveCompressedAudio(CancellationToken token)
    {
        string pathToExtractedWwiseDirectory = Path.Combine(GlobalVariables.pathToExtractedAssets, "DeadByDaylight", "Content", "WwiseAudio", "Cooked");

        string[] fileExtensions = [
            "*.bnk",
            "*.wem"
        ];
        var filesToMove = fileExtensions.SelectMany(ext => Directory.GetFiles(pathToExtractedWwiseDirectory, ext, SearchOption.AllDirectories)).ToArray();

        if (!Directory.Exists(TemporaryDirectory))
        {
            Directory.CreateDirectory(TemporaryDirectory);
        }

        foreach (var filePath in filesToMove)
        {
            token.ThrowIfCancellationRequested();

            try
            {
                string originalFilePath = StringUtils.StripDynamicDirectory(filePath, pathToExtractedWwiseDirectory);

                string extension = Path.GetExtension(originalFilePath);

                // We need all wems to be in one folder as wwiser is gonna grab wems from specific folder we set
                if (extension == ".wem")
                {
                    string fileName = Path.GetFileName(filePath);

                    string destFilePath = Path.Combine(TemporaryDirectory, fileName);

                    if (File.Exists(destFilePath)) continue;

                    File.Copy(filePath, destFilePath, overwrite: true);
                }
                else
                {
                    // Preserve banks original structure as bank names aren't unique
                    string destFilePath = Path.Combine(TemporaryDirectory, originalFilePath);

                    Directory.CreateDirectory(Path.GetDirectoryName(destFilePath)!);

                    if (File.Exists(destFilePath)) continue;

                    File.Copy(filePath, destFilePath, overwrite: true);
                }
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
        if (!File.Exists(PathToUEAssetsRegistry)) throw new Exception("Not found Unreal Engine assets registry.");
        if (!File.Exists(GlobalVariables.preGeneratedWwnames)) throw new Exception("Not found pre-generated Wwnames.");

        string destinationFilePath = Path.Combine(TemporaryDirectory, "wwnames.txt");

        File.Copy(GlobalVariables.preGeneratedWwnames, destinationFilePath, true);

        string jsonContent = File.ReadAllText(PathToUEAssetsRegistry);

        JObject jsonObject = JObject.Parse(jsonContent);

        string formattedJson = jsonObject.ToString(Formatting.Indented);
        File.AppendAllText(destinationFilePath, formattedJson);
    }

    // Generate txtp files that will be used to play audio simulating Wwise
    public static void GenerateTxtp()
    {
        string bnkFilesDirectory = Path.Combine(TemporaryDirectory, "*.bnk");
        string bnkFilesSubDirectories = Path.Combine(TemporaryDirectory, "**", "*.bnk"); // Wwiser fails to find banks recursively (even with -f option in arguments) for some reason, so search inside subfolders too

        ProvideWwnamesFile(); // Neccessary for reversing audio names

        string arguments = $"\"{GlobalVariables.wwiserPath}\" \"{bnkFilesDirectory}\" \"{bnkFilesSubDirectories}\" -g --txtp-wemdir \"{TemporaryDirectory}\" --txtp-lang en fr jp";

        if (!IsPythonInstalled()) throw new Exception("Python is not installed or not accessible from the command line, which is required for audio extraction to work.");

        // Generate txtp files with reversed names that we will use to convert to playable audio
        CommandUtils.ExecuteCommand(arguments, "python", TemporaryDirectory);
    }

    // We need to check if Python is installed in order for user to be able to decompile audio
    private static bool IsPythonInstalled()
    {
        try
        {
            // Get the system's PATH environment variable
            var path = Environment.GetEnvironmentVariable("PATH");

            if (path == null)
                return false;

            // Split the PATH into individual directories
            var pathDirs = path.Split(Path.PathSeparator);

            // Check each directory for the presence of the Python executable
            foreach (var dir in pathDirs)
            {
                try
                {
                    // Check for both 'python.exe' (Windows) and 'python' (Unix-based)
                    if (File.Exists(Path.Combine(dir, "python.exe")) || File.Exists(Path.Combine(dir, "python")))
                    {
                        return true;
                    }
                }
                catch
                {
                    // Ignore any exceptions due to inaccessible directories and continue
                    continue;
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    [GeneratedRegex(@"CAkEvent\[\d+\] (\d+)")]
    private static partial Regex CAkEventRegex();
    // Returns audio event ids associated with the audio
    public static Dictionary<string, long> GrabAudioEventIds()
    {
        string[] txtpFiles = Directory.GetFiles(TemporaryDirectory, "*.txtp", SearchOption.AllDirectories);

        if (txtpFiles.Length == 0) throw new Exception("Failed to find any generated txtp files.");

        Dictionary<string, long> kvp = [];
        foreach (string filePath in txtpFiles)
        {
            string fileContent = File.ReadAllText(filePath);
            string fileName = Path.GetFileName(filePath);

            Match match = CAkEventRegex().Match(fileContent);

            if (match.Success)
            {
                string cakEventId = match.Groups[1].Value; // Wwise audio event id
                kvp[fileName] = long.Parse(cakEventId);
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
    // Returns audio events data
    public static Dictionary<long, AudioEventsLinkageData> ConstructAudioEventsLinkage()
    {
        dynamic assetsRegistryData = FileUtils.LoadDynamicJson(PathToUEAssetsRegistry);
        var preallocatedAssetDataBuffers = assetsRegistryData["PreallocatedAssetDataBuffers"];

        Dictionary<long, AudioEventsLinkageData> kvp = [];
        foreach (var assetData in preallocatedAssetDataBuffers)
        {
            string? assetClass = assetData["AssetClass"].ToString();

            // We only want to map audio events
            if (assetClass != "AkAudioEvent") continue;

            string? packagePath = assetData["PackagePath"].ToString();

            // In case package path is null this mapping won't be useful anyway
            if (packagePath == null) continue;

            var tagsAndValues = assetData["TagsAndValues"];

            if (tagsAndValues.TryGetValue("WwiseShortId", out JToken wwiseIdToken))
            {
                string wwiseId = wwiseIdToken.ToString();
                if (long.TryParse(wwiseId, out long wwiseIdLong))
                {
                    long adjustedWwiseId = wwiseIdLong;

                    // If audio event id is negative we need to add maxium unsigned integer value and use that instead
                    // Ids overflow to negativity in assets registry and don't match to compiled audio event ids because of int type mismatch
                    // In some cases adjusted value is also off by 2
                    if (wwiseIdLong < 0)
                    {
                        adjustedWwiseId = wwiseIdLong + uint.MaxValue;
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
        const int Offset = 2;
        foreach (var audioEvent in associatedAudioEventIds)
        {
            bool matchFound = false;
            // We need to try twice, sometimes value is off by 2, hence why we need to subtract 2
            for (int attempt = 0; attempt < 2; attempt++)
            {
                long audioEventValue = audioEvent.Value;
                if (attempt == 1)
                {
                    audioEventValue -= Offset;
                }

                if (audioEventsLinkage.TryGetValue(audioEventValue, out AudioEventsLinkageData? audioEventData))
                {
                    matchFound = true;
                    string reversedFileName = audioEvent.Key;

                    string combinedPath = Path.Combine(WwiseStructured, audioEventData.PackagePath.TrimStart('/'), Path.ChangeExtension(reversedFileName, ".wav"));

                    string finalPath = combinedPath.Replace("\\Game/", "/DeadByDaylight/");

                    Directory.CreateDirectory(Path.GetDirectoryName(finalPath) ?? string.Empty);

                    string wavFilePath = Path.Combine(GlobalVariables.tempDir, Path.ChangeExtension(reversedFileName, ".wav"));
                    if (File.Exists(wavFilePath))
                    {
                        File.Move(wavFilePath, finalPath, overwrite: true);
                    }

                    break; // Exit the loop after successfully handling the file
                }
            }

            if (!matchFound)
            {
                LogsWindowViewModel.Instance.AddLog($"Failed to find audio event linkage data for: {audioEvent.Key}", Logger.LogTags.Info);
            }
        }
    }

    private static readonly string[] suppressedTxtpFiles = [
        "AudioEvent_Masquerade_Charge_Loop [AudioSwitchMasqueradeState=Charged] {m}.txtp",
        "AudioEvent_Masquerade_Charge_Loop [AudioSwitchMasqueradeState=Idle] {m}.txtp",
        "AudioEvent_K23_Comet_Status_Start_InGame [AudioSwitchKillerStatus=Crazy] {r} {m}.txtp",
        "AudioEvent_K23_Comet_Status_Start_Menu [AudioSwitchKillerStatus=Crazy] {r} {m}.txtp",
    ];
    public static void ConvertTxtpToWav(CancellationToken token)
    {
        Directory.CreateDirectory(GlobalVariables.tempDir);

        // We need to copy all txtp files to shorter file path
        Directory.EnumerateFiles(TemporaryDirectory, "*.txtp", SearchOption.AllDirectories)
            .ToList()
            .ForEach(txtpFile =>
        {
            token.ThrowIfCancellationRequested();

            File.Copy(txtpFile, Path.Combine(GlobalVariables.tempDir, Path.GetFileName(txtpFile)), overwrite: true);
        });

        var txtpFiles = Directory.GetFiles(GlobalVariables.tempDir, "*.txtp", SearchOption.TopDirectoryOnly);

        var audioToParse = WwiseRegister.RetrieveAudioToParse(txtpFiles);

        int conversionCount = 0;
        // Convert txtp files to wav audio format
        Parallel.ForEach(txtpFiles, new ParallelOptions { MaxDegreeOfParallelism = 4 }, filePath =>
        {
            token.ThrowIfCancellationRequested();

            if (!audioToParse.Contains(filePath)) return; // We only want to convert new/modified audio

            try
            {
                // Construct the output .wav file path
                // We need directory close to the root
                // Otherwise audio might fail to convert due to path length limit
                string outputFilePath = Path.Combine(GlobalVariables.tempDir, Path.ChangeExtension(Path.GetFileName(filePath), ".wav"));
                if (File.Exists(outputFilePath)) return;

                // Prepare the arguments for the vgmstream-cli command
                string arguments = $"-i -o \"{outputFilePath}\" \"{filePath}\"";

                // Convert txtp files to wav audio format
                CommandUtils.ExecuteCommand(arguments, GlobalVariables.vgmStreamCliPath, GlobalVariables.rootDir);

                string txtpFileName = Path.GetFileName(filePath);

                // Some warnings for specific txtp files are suppressed since I don't know how to fix them :c
                if (!File.Exists(outputFilePath) && !suppressedTxtpFiles.Contains(txtpFileName))
                {
                    LogsWindowViewModel.Instance.AddLog($"Conversion failed for: {filePath.Replace(Path.Combine(TemporaryDirectory, "txtp"), "").TrimStart(Path.AltDirectorySeparatorChar).TrimStart(Path.PathSeparator).TrimStart(Path.DirectorySeparatorChar)}", Logger.LogTags.Warning);
                }
                else
                {
                    Interlocked.Increment(ref conversionCount);
                }
            }
            catch (Exception ex)
            {
                LogsWindowViewModel.Instance.AddLog($"Error processing file {filePath.Replace(Path.Combine(TemporaryDirectory, "txtp"), "").TrimStart(Path.AltDirectorySeparatorChar).TrimStart(Path.PathSeparator)}: {ex.Message}", Logger.LogTags.Warning);
            }
        });

        if (conversionCount > 0)
        {
            LogsWindowViewModel.Instance.AddLog($"Converted total of {conversionCount} audio files.", Logger.LogTags.Info);
        }
    }

    public static async Task UnpackAudioBanks()
    {
        string[] bnkFiles = Directory.GetFiles(TemporaryDirectory, "*.bnk", SearchOption.AllDirectories);

        if (bnkFiles.Length == 0)
        {
            return;
        }

        List<CommandUtils.CommandModel> commands = [];
        foreach (var bnkFilePath in bnkFiles)
        {
            string command = $"--audio {bnkFilePath} --output {TemporaryDirectory} --wems-only";

            CommandUtils.CommandModel model = new()
            {
                Argument = command,
                PathToExe = GlobalVariables.bnkExtractorPath

            };
            commands.Add(model);
        }

        await CommandUtils.ExecuteCommandsAsync(commands);
    }

    [GeneratedRegex(@"\b\d+\.wem\b", RegexOptions.Compiled)]
    private static partial Regex WemFileRegex();
    public static string[] CollectWemFiles(string inputText)
    {
        MatchCollection matches = WemFileRegex().Matches(inputText);

        List<string> wemFiles = [];

        foreach (Match match in matches)
        {
            string wemFilePath = Path.Combine(TemporaryDirectory, match.Value);
            wemFiles.Add(wemFilePath);
        }

        return [.. wemFiles];
    }

    // We use WAV format now, but in case I will need OGG I will leave this method for a reference

    //public static void ConvertToOggAndMove()
    //{
    //    string[] wemFiles = Directory.GetFiles(WwiseStructured, "*.wem", SearchOption.AllDirectories);

    //    var audioToParse = WwiseRegister.RetrieveAudioToParse(wemFiles);

    //    var commands = new List<WwiseUtilities.CommandModel>();

    //    int failedAudioConversionCount = 0;
    //    Parallel.ForEach(wemFiles, audioPath =>
    //    {
    //        if (!audioToParse.Contains(audioPath)) return;

    //        string relativePath = Utils.StringUtils.StripDynamicDirectory(audioPath, WwiseStructured);
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

    public static void MoveAudioToOutput()
    {
        string[] wavFiles = Directory.GetFiles(WwiseStructured, "*.wav", SearchOption.AllDirectories);
        foreach (var tempWavPath in wavFiles)
        {
            try
            {
                string relativePath = StringUtils.StripDynamicDirectory(tempWavPath, WwiseStructured);
                string finalWavPath = Path.Combine(GlobalVariables.rootDir, "Output", "ExtractedAssets", "Audio", GlobalVariables.versionWithBranch, relativePath);

                Directory.CreateDirectory(Path.GetDirectoryName(finalWavPath)!);

                if (!File.Exists(tempWavPath))
                {
                    continue;
                }

                File.Move(tempWavPath, finalWavPath, overwrite: true);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error moving file {tempWavPath} to target directory: {ex.Message}");
            }
        }
    }

    private const int MaxFileNameLength = 233; // A little below actual max file name length limit, otherwise we can't perform basic operations on the file
    [GeneratedRegex(@"(?<!\S)([^#\n]*\.txtp)", RegexOptions.Compiled)]
    private static partial Regex TxtpFileRegex();
    // In case txtp file path is too long it gets truncated and moved out of temporary wwise directory
    // We need to handle them manually
    public static void RenameAndMoveTruncatedTxtpFiles()
    {
        string[] txtpFiles = Directory.GetFiles(GlobalVariables.pathToExtractedAudio, "*.txtp");

        if (txtpFiles.Length == 0) return;

        foreach (var txtpFilePath in txtpFiles)
        {
            string txtpFileContent = File.ReadAllText(txtpFilePath);
            // Use the regex to find the full .txtp path in the content
            MatchCollection matches = TxtpFileRegex().Matches(txtpFileContent);

            if (matches.Count == 1)
            {
                foreach (Match match in matches)
                {
                    string matchedTxtpPath = match.Value;
                    string originalFileName = Path.GetFileName(matchedTxtpPath);

                    // Check if the filename is too long (due to file path length limit)
                    string newFileName = originalFileName.Length > MaxFileNameLength
                        ? string.Concat(originalFileName.AsSpan(0, MaxFileNameLength), Path.GetExtension(originalFileName))
                        : originalFileName;

                    // Construct the full destination path with long path prefix
                    string destinationPath = $@"\\?\{Path.Combine(TemporaryDirectory, "txtp", newFileName)}";

                    try
                    {
                        File.Move(txtpFilePath, destinationPath, true);
                    }
                    catch
                    {
                        // Exit the method completely on failure
                        // Cause that most likely means user doesnt have support for long file paths
                        LogsWindowViewModel.Instance.AddLog($"Failed to move: {originalFileName}. File path may be too long, you need to enable support for long file paths in your system to fix that. Abandoning handling of truncated files..", Logger.LogTags.Warning);
                        return;
                    }

                    if (!File.Exists(destinationPath))
                    {
                        LogsWindowViewModel.Instance.AddLog($"Failed to move: {originalFileName}. File path may be too long.", Logger.LogTags.Warning);
                    }
                }
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
}