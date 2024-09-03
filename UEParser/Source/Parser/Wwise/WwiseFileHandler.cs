using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UEParser.ViewModels;
using System.Text.RegularExpressions;
using UEParser.Utils;
using System.Threading;
using Newtonsoft.Json.Linq;
using UEParser.AssetRegistry.Wwise;

namespace UEParser.Parser.Wwise;

public partial class WwiseFileHandler
{
    private static readonly string temporaryDirectory = Path.Combine(GlobalVariables.pathToExtractedAudio, "WwiseTemporary");
    private static readonly string wwiseStructured = Path.Combine(GlobalVariables.pathToExtractedAudio, "WwiseStructured");
    private static readonly string pathToUEAssetsRegistry = Path.Combine(GlobalVariables.pathToExtractedAssets, "DeadByDaylight", "AssetRegistry.json");

    public static void MoveCompressedAudio()
    {
        string pathToExtractedWwiseDirectory = Path.Combine(GlobalVariables.pathToExtractedAssets, "DeadByDaylight", "Content", "WwiseAudio", "Cooked");

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
                string originalFilePath = StringUtils.StripDynamicDirectory(filePath, pathToExtractedWwiseDirectory);

                string extension = Path.GetExtension(originalFilePath);

                // We need all wems to be in one folder as wwiser is gonna grab wems from specific folder we set
                if (extension == ".wem")
                {
                    string fileName = Path.GetFileName(filePath);

                    string destFilePath = Path.Combine(temporaryDirectory, fileName);

                    if (File.Exists(destFilePath)) continue;

                    File.Copy(filePath, destFilePath, overwrite: true);
                }
                else
                {
                    // Preserve banks original structure as bank names aren't unique
                    string destFilePath = Path.Combine(temporaryDirectory, originalFilePath);

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
        if (!File.Exists(pathToUEAssetsRegistry)) throw new Exception("Not found Unreal Engine assets registry.");

        string destinationFilePath = Path.Combine(temporaryDirectory, "wwnames.txt");

        File.Copy(pathToUEAssetsRegistry, destinationFilePath, true);
    }

    // Generate txtp files that will be used to play audio simulating Wwise
    public static void GenerateTxtp()
    {
        string bnkFilesDirectory = Path.Combine(temporaryDirectory, "*.bnk");
        string bnkFilesSubDirectories = Path.Combine(temporaryDirectory, "**", "*.bnk"); // Wwiser fails to find banks recursively (even with -f option in arguments) for some reason, so search inside subfolders too

        ProvideWwnamesFile(); // Neccessary for reversing audio names

        string arguments = $"\"{GlobalVariables.wwiserPath}\" \"{bnkFilesDirectory}\" \"{bnkFilesSubDirectories}\" -g --txtp-wemdir \"{temporaryDirectory}\"";

        if (!IsPythonInstalled()) throw new Exception("Python is not installed or not accessible from the command line, which is required for audio extraction to work.");

        // Generate txtp files with reversed names that we will use to convert to playable audio
        WwiseUtilities.ExecuteCommand(arguments, "python", temporaryDirectory);
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
    // Returns audio events data
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

            string? packagePath = assetData["PackagePath"].ToString();

            // In case package path is null this mapping won't be useful anyway
            if (packagePath == null) continue;

            var tagsAndValues = assetData["TagsAndValues"];

            if (tagsAndValues.TryGetValue("WwiseShortId", out JToken wwiseIdToken))
            {
                string wwiseId = wwiseIdToken.ToString(); 
                if (long.TryParse(wwiseId, out long wwiseIdInt))
                {
                    long adjustedWwiseId = wwiseIdInt;

                    // If audio event id is negative we need to add double maxium integer value and use that instead
                    // For some reason ids overflow to negativity in assets registry and don't match to compiled audio event ids
                    // In some cases adjusted value is also off by 2
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
            bool matchFound = false;
            // We need to try twice, sometimes value is off by 2, hence why we need to subtract 2
            for (int attempt = 0; attempt < 2; attempt++)
            {
                long audioEventValue = audioEvent.Value;
                if (attempt == 1)
                {
                    audioEventValue -= 2;
                }

                if (audioEventsLinkage.TryGetValue(audioEventValue, out AudioEventsLinkageData? audioEventData))
                {
                    matchFound = true;
                    string reversedFileName = Path.GetFileName(audioEvent.Key);

                    string combinedPath = Path.Combine(wwiseStructured, audioEventData.PackagePath.TrimStart('/'), Path.ChangeExtension(reversedFileName, ".wav"));

                    string finalPath = combinedPath.Replace("\\Game/", "/DeadByDaylight/");

                    Directory.CreateDirectory(Path.GetDirectoryName(finalPath) ?? string.Empty);

                    if (File.Exists(Path.ChangeExtension(audioEvent.Key, ".wav")))
                    {
                        File.Move(Path.ChangeExtension(audioEvent.Key, ".wav"), finalPath, overwrite: true);
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
        "AudioEvent_Masquerade_Charge_Loop [2766023450=3146088667] {m}.txtp",
        "AudioEvent_Masquerade_Charge_Loop [2766023450=Idle] {m}.txtp",
        "AudioEvent_K23_Comet_Status_Start_InGame [567149230=2930662992] {r} {m}.txtp",
        "AudioEvent_K23_Comet_Status_Start_Menu [567149230=2930662992] {r} {m}.txtp",
        "AudioEvent_K28_BODY_Default_BlackSmoke_Lp_Start {m}.txtp",
        "AudioEvent_K28_Menu_Status_Start [2095043200=1597592862] {r} {m}.txtp",
        "AudioEvent_K28_Menu_Status_Start [2095043200=2631872382] {r} {m}.txtp",
        "AudioEvent_K28_Menu_Status_Start [2095043200=2648649944] {r} {m}.txtp",
        "AudioEvent_K28_Menu_Status_Start [2095043200=3229799049] {r} {m}.txtp",
        "AudioEvent_K28_Menu_Status_Start [2095043200=632232018] {r} {m}.txtp",
        "AudioEvent_K28_Menu_Status_Start [2095043200=786316536] {r} {m}.txtp",
        "AudioEvent_K28_OBJ_Remnant_Spawn {m}.txtp",
        "AudioEvent_K28_PWR_Teleport_ChargeStart {s}=(4268632856=3102261578) {r} {m}.txtp",
        "AudioEvent_K28_PWR_Teleport_ChargeStart {s}=(4268632856=932695537) {r} {m}.txtp",
        "AudioEvent_K28_PWR_Teleport_ChargeStart {s}=- {r} {m}.txtp",
        "AudioEvent_K28_Status_Start {r} {m}.txtp",
        "ARC_TOME20_HOA_01_ENTRY {l=en}.txtp",
        "ARC_TOME20_HOA_02_ENTRY {l=en}.txtp",
        "ARC_TOME20_HOA_03_ENTRY {l=en}.txtp",
        "ARC_TOME20_HOA_05_ENTRY {l=en}.txtp",
        "ARC_TOME20_HOA_06_ENTRY {l=en}.txtp",
        "ARC_TOME20_HOA_08_ENTRY {l=en}.txtp",
        "ARC_TOME20_HOA_09_ENTRY {l=en}.txtp",
        "ARC_TOME20_SPIRIT_02_ENTRY {l=en}.txtp",
        "ARC_TOME20_SPIRIT_03_ENTRY {l=en}.txtp",
        "ARC_TOME20_SPIRIT_04_ENTRY {l=en}.txtp",
        "ARC_TOME20_SPIRIT_05_ENTRY {l=en}.txtp",
        "ARC_TOME20_SPIRIT_07_ENTRY {l=en}.txtp",
        "ARC_TOME20_SPIRIT_08_ENTRY {l=en}.txtp",
        "ARC_TOME20_SPIRIT_09_ENTRY {l=en}.txtp",
        "ARC_TOME20_YUI_02_ENTRY {l=en}.txtp",
        "ARC_TOME20_YUI_03_ENTRY {l=en}.txtp",
        "ARC_TOME20_YUI_04_ENTRY {l=en}.txtp",
        "ARC_TOME20_YUI_05_ENTRY {l=en}.txtp",
        "ARC_TOME20_YUI_07_ENTRY {l=en}.txtp",
        "ARC_TOME20_YUI_08_ENTRY {l=en}.txtp",
        "ARC_TOME20_YUI_09_ENTRY {l=en}.txtp"
    ];
    public static void ConvertTxtpToWav()
    {
        // Get all .txtp files in the directory and its subdirectories
        var txtpFiles = Directory.GetFiles(temporaryDirectory, "*.txtp", SearchOption.AllDirectories);

        var audioToParse = WwiseRegister.RetrieveAudioToParse(txtpFiles);

        int conversionCount = 0;
        // Convert txtp files to wav audio format
        Parallel.ForEach(txtpFiles, new ParallelOptions { MaxDegreeOfParallelism = 4 }, filePath =>
        {
            if (!audioToParse.Contains(filePath)) return; // We only want to convert new/modified audio

            try
            {
                // Construct the output .wav file path
                string outputFilePath = Path.ChangeExtension(filePath, ".wav");
                if (File.Exists(outputFilePath)) return;

                // Prepare the arguments for the vgmstream-cli command
                string arguments = $"-i -o \"{outputFilePath}\" \"{filePath}\"";

                // Convert txtp files to wav audio format
                WwiseUtilities.ExecuteCommand(arguments, GlobalVariables.vgmStreamCliPath, GlobalVariables.rootDir);

                string txtpFileName = Path.GetFileName(filePath);

                // Some warnings for specific txtp files are suppressed since I don't know how to fix them :c
                if (!File.Exists(outputFilePath) && !suppressedTxtpFiles.Contains(txtpFileName))
                {
                    LogsWindowViewModel.Instance.AddLog($"Conversion failed for: {filePath.Replace(Path.Combine(temporaryDirectory, "txtp"), "").TrimStart(Path.AltDirectorySeparatorChar).TrimStart(Path.PathSeparator)}", Logger.LogTags.Warning);
                }
                else
                {
                    Interlocked.Increment(ref conversionCount);
                }
            }
            catch (Exception ex)
            {
                LogsWindowViewModel.Instance.AddLog($"Error processing file {filePath.Replace(Path.Combine(temporaryDirectory, "txtp"), "").TrimStart(Path.AltDirectorySeparatorChar).TrimStart(Path.PathSeparator)}: {ex.Message}", Logger.LogTags.Warning);
            }
        });

        if (conversionCount > 0)
        {
            LogsWindowViewModel.Instance.AddLog($"Converted total of {conversionCount} audio files.", Logger.LogTags.Info);
        }
    }

    public static async Task UnpackAudioBanks()
    {
        string[] bnkFiles = Directory.GetFiles(temporaryDirectory, "*.bnk", SearchOption.AllDirectories);

        if (bnkFiles.Length == 0)
        {
            return;
        }

        List<WwiseUtilities.CommandModel> commands = [];
        foreach (var bnkFilePath in bnkFiles)
        {
            string command = $"--audio {bnkFilePath} --output {temporaryDirectory} --wems-only";

            WwiseUtilities.CommandModel model = new()
            {
                Argument = command,
                PathToExe = GlobalVariables.bnkExtractorPath

            };
            commands.Add(model);
        }

        await WwiseUtilities.ExecuteCommandsAsync(commands);
    }

    // We use WAV format now, but in case I will need OGG I will leave this method for a reference

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

    public static void MoveAudioToOutput()
    {
        string[] wavFiles = Directory.GetFiles(wwiseStructured, "*.wav", SearchOption.AllDirectories);
        foreach (var tempWavPath in wavFiles)
        {
            try
            {
                string relativePath = StringUtils.StripDynamicDirectory(tempWavPath, wwiseStructured);
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