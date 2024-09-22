using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using ReactiveUI;
using UEParser.Network;
using UEParser.Services;
using UEParser.Utils;
using UEParser.AssetRegistry;
using UEParser.AssetRegistry.Wwise;

namespace UEParser.ViewModels;

public class UpdateManagerViewModel : ReactiveObject
{
    public static bool IsDebug
    {
        get
        {
#if DEBUG
            return true;
#else
        return false;
#endif
        }
    }

    public ICommand UploadParsedDataCommand { get; }
    public ICommand ConvertUEModelsCommand { get; }
    public ICommand ValidateAssetsCommand { get; }
    public ICommand UploadModelsDataCommand { get; }
    public ICommand ConvertAudioToOggFormatCommand { get; }
    public ICommand CleanupLocalAudioArchiveCommand { get; }

    public UpdateManagerViewModel()
    {
        UploadParsedDataCommand = ReactiveCommand.Create(UploadParsedData);
        ConvertUEModelsCommand = ReactiveCommand.Create(ConvertUEModels);
        ValidateAssetsCommand = ReactiveCommand.Create(ValidateAssetsInBucket);
        UploadModelsDataCommand = ReactiveCommand.Create(UploadModelsData);
        ConvertAudioToOggFormatCommand = ReactiveCommand.Create(ConvertAudioToOggFormat);
        CleanupLocalAudioArchiveCommand = ReactiveCommand.Create(CleanupLocalAudioArchive);
    }

    // Debug method only, should not be shipped to release
    // Can be hidden in UI using IsDebug property
    public static async Task CleanupLocalAudioArchive()
    {
        LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Running);
        LogsWindowViewModel.Instance.AddLog("Cleaning local audio archive..", Logger.LogTags.Info);

        var config = ConfigurationService.Config;
        await Task.Run(() =>
        {
            try
            {
                string localAudioArchivePath = config.Global.LocalAudioArchivePath;

                if (!Directory.Exists(localAudioArchivePath)) throw new Exception("Local audio archive doesn't exist.");

                string[] localFiles = Directory.GetFiles(localAudioArchivePath, "*.*", SearchOption.AllDirectories);

                if (!File.Exists(WwiseRegister.PathToAudioRegister)) throw new Exception("Audio registry doesn't exist.");

                var (_, audio) = RegistryManager.ReadFromUInfoFile(WwiseRegister.PathToAudioRegister);

                List<string> audioToDelete = [];
                foreach (var localFilePath in localFiles) 
                {
                    string fileName = Path.GetFileName(localFilePath);
                    string txtpFileName = Path.ChangeExtension(fileName, ".txtp");

                    if (!audio.ContainsKey(txtpFileName))
                    {
                        audioToDelete.Add(localFilePath);
                    }
                }

                foreach (var audioFilePath in audioToDelete)
                {
                    if (File.Exists(audioFilePath))
                    {
                        File.Delete(audioFilePath);
                    }
                }

                LogsWindowViewModel.Instance.AddLog("Finished cleaning up audio archive.", Logger.LogTags.Success);
            }
            catch (Exception ex)
            {
                LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
                LogsWindowViewModel.Instance.AddLog(ex.Message, Logger.LogTags.Error);
            }
            finally
            {
                LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
            }
        });
    }

    public static async Task ConvertAudioToOggFormat()
    {
        LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Running);
        LogsWindowViewModel.Instance.AddLog("Starting audio conversion..", Logger.LogTags.Info);

        await Task.Run(async () =>
        {
            try
            {
                int convertedAudioCount = 0;
                await DownloadFfmpegDependency(); // FFmpeg isn't included in UEParser build due to its size and the fact it might be useful in rare cases

                string extractedAudioPath = Path.Combine(GlobalVariables.rootDir, "Output", "ExtractedAssets", "Audio", GlobalVariables.versionWithBranch);
                string[] wavFiles = Directory.GetFiles(extractedAudioPath, "*.wav", SearchOption.AllDirectories);

                LogsWindowViewModel.Instance.AddLog($"Detected total of {wavFiles.Length} files to convert.", Logger.LogTags.Info);

                foreach (string file in wavFiles)
                {
                    string? dir = Path.GetDirectoryName(file);

                    if (string.IsNullOrEmpty(dir)) continue;

                    string baseName = Path.GetFileNameWithoutExtension(file);
                    string outputFilePath = Path.Combine(dir, $"{baseName}.ogg");

                    string arguments = $"-i \"{file}\" -c:a libvorbis \"{outputFilePath}\" -y";

                    CommandUtils.ExecuteCommand(arguments, GlobalVariables.ffmpegPath, GlobalVariables.rootDir);
                    convertedAudioCount++;

                    if (File.Exists(outputFilePath) && !FileUtils.IsFileLocked(file))
                    {
                        File.Delete(file);
                    }
                }

                if (convertedAudioCount > 0)
                {
                    LogsWindowViewModel.Instance.AddLog($"Converted total of {convertedAudioCount} audio files.", Logger.LogTags.Info);
                }

                LogsWindowViewModel.Instance.AddLog("Finished audio conversion.", Logger.LogTags.Success);
            }
            catch (Exception ex)
            {
                LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
                LogsWindowViewModel.Instance.AddLog(ex.Message, Logger.LogTags.Error);
            }
            finally
            {
                LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
            }
        });
    }

    private static async Task DownloadFfmpegDependency()
    {
        if (File.Exists(GlobalVariables.ffmpegPath))
        {
            return;
        }

        LogsWindowViewModel.Instance.AddLog("Please wait, downloading FFmpeg dependency..", Logger.LogTags.Info);

        string ffmpegDownloadUrl = GlobalVariables.dbdinfoBaseUrl + $"UEParser/ffmpeg.exe";

        try
        {
            byte[] fileBytes = await NetAPI.FetchFileBytesAsync(ffmpegDownloadUrl);

            File.WriteAllBytes(GlobalVariables.ffmpegPath, fileBytes);
            LogsWindowViewModel.Instance.AddLog($"Succesffully downloaded FFmpeg dependency.", Logger.LogTags.Success);
        }
        catch (Exception ex)
        {
            LogsWindowViewModel.Instance.AddLog($"Error downloading ffmpeg: {ex.Message}", Logger.LogTags.Error);
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
        }
    }

    // Assets validation in the bucket
    // basically I wanna know if there's any assets that are present in models mappings
    // but are missing in the S3 bucket or if assets use incorrect path case
    // which needs to be checked as dbd uses case-insensitive paths while
    // I need to take into account case-sensitivity
    public static async Task ValidateAssetsInBucket()
    {
        LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Running);

        await Task.Run(async () =>
        {
            try
            {
                S3Service s3Service = S3Service.CreateFromConfig();

                var config = ConfigurationService.Config;
                string? bucketName = config.Sensitive.S3BucketName;

                if (string.IsNullOrEmpty(bucketName))
                {
                    throw new Exception("Bucket name is not set in settings");
                }

                LogsWindowViewModel.Instance.AddLog("Loading list of objects in the bucket..", Logger.LogTags.Info);

                await Task.Delay(100);

                var objectKeys = s3Service.ListAllObjectsInFolder(bucketName, "assets");

                LogsWindowViewModel.Instance.AddLog("Loading list of objects in the models mappings..", Logger.LogTags.Info);

                await Task.Delay(100);

                var paths = Helpers.ListPathsFromModelsMapping();

                static List<string> FindMissingPaths(List<string> objectKeys, List<string> paths)
                {
                    var normalizedObjectKeys = new HashSet<string>(
                        objectKeys
                            .Where(k => !string.IsNullOrWhiteSpace(k))
                            .Select(k => k.ToLowerInvariant())
                    );

                    var normalizedPaths = paths.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p.ToLowerInvariant());

                    var missingPathsSet = new HashSet<string>();

                    foreach (var path in paths)
                    {
                        if (!string.IsNullOrWhiteSpace(path) && !normalizedObjectKeys.Contains(path.ToLowerInvariant()))
                        {
                            missingPathsSet.Add(path);
                        }
                    }

                    return [.. missingPathsSet];
                }

                // TODO: something is off and needs to be fixed
                static List<string> GetCorrectlyCasedPaths(List<string> objectKeys, List<string> paths)
                {
                    var normalizedObjectKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                    foreach (var key in paths.Where(k => !string.IsNullOrWhiteSpace(k)))
                    {
                        var lowerCaseKey = key.ToLowerInvariant();
                        if (!normalizedObjectKeys.ContainsKey(lowerCaseKey))
                        {
                            normalizedObjectKeys[lowerCaseKey] = key;
                        }
                    }

                    var correctlyCasedPaths = new HashSet<string>();

                    foreach (var path in objectKeys)
                    {
                        if (!string.IsNullOrWhiteSpace(path) && normalizedObjectKeys.TryGetValue(path.ToLowerInvariant(), out var correctCasePath))
                        {
                            // Check if the original case differs from the current path
                            // only then we will know asset needs to be moved to path with correct case
                            if (!path.Equals(correctCasePath, StringComparison.Ordinal))
                            {
                                correctlyCasedPaths.Add(correctCasePath);
                            }
                        }
                    }

                    return [.. correctlyCasedPaths];
                }

                static void LogPaths(string path, string tag)
                {
                    string message = string.Empty;

                    switch (tag)
                    {
                        case "missing":
                            message = $"Detected missing asset: {path}";
                            break;
                        case "wrongCase":
                            message = $"Asset uses wrong case in its path: {path}";
                            break;
                        default:
                            break;
                    }

                    LogsWindowViewModel.Instance.AddLog(message, Logger.LogTags.Warning);
                }

                var missingAssets = FindMissingPaths(objectKeys, paths);
                var correctlyCasedPaths = GetCorrectlyCasedPaths(objectKeys, paths);

                int missingAssetsAmount = missingAssets.Count;
                int correctlyCasedPathsAmount = correctlyCasedPaths.Count;

                if (missingAssetsAmount > 0)
                {
                    LogsWindowViewModel.Instance.AddLog($"Found total of {missingAssetsAmount} missing assets.", Logger.LogTags.Warning);
                    foreach (var path in missingAssets)
                    {
                        await Task.Delay(50);
                        LogPaths(path, "missing");
                    }
                }
                else
                {
                    LogsWindowViewModel.Instance.AddLog($"Not found missing assets.", Logger.LogTags.Info);
                }

                if (correctlyCasedPathsAmount > 0)
                {
                    LogsWindowViewModel.Instance.AddLog($"Found total of {correctlyCasedPathsAmount} assets with incorrect case.", Logger.LogTags.Warning);
                    foreach (var path in correctlyCasedPaths)
                    {
                        LogPaths(path, "wrongCase");
                    }
                }
                else
                {
                    LogsWindowViewModel.Instance.AddLog($"Not found assets that use incorrect case.", Logger.LogTags.Info);
                }

                LogsWindowViewModel.Instance.AddLog($"Assets validation has been completed.", Logger.LogTags.Success);
                LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
            }
            catch (Exception ex)
            {
                LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
                LogsWindowViewModel.Instance.AddLog(ex.Message, Logger.LogTags.Error);
            }
        });
    }

    public static async Task ConvertUEModels()
    {
        LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Running);

        await Task.Run(() =>
        {
            try
            {
                var config = ConfigurationService.Config;
                string blenderPath = config.Global.BlenderPath;
                string rootDirectory = GlobalVariables.rootDir;
                string inputDirectory = Path.Combine(GlobalVariables.rootDir, "Output", "ExtractedAssets", "Meshes", GlobalVariables.versionWithBranch);
                string inputMappingDirectory = Path.Combine(GlobalVariables.rootDir, "Output", "ModelsData", GlobalVariables.versionWithBranch);
                string outputDirectory = Path.Combine(GlobalVariables.rootDir, "Output", "ConvertedModels", GlobalVariables.versionWithBranch);

                string command = ($"-b -P \"{GlobalVariables.modelsConverterScriptPath}\" " +
                                $"-- --root_directory \"{rootDirectory}\" " +
                                $"--input_directory \"{inputDirectory + '\\'}\" " +
                                $"--input_mapping_directory \"{inputMappingDirectory + '\\'}\" " +
                                $"--output_directory \"{outputDirectory + '\\'}\"")
                                .Replace(Path.DirectorySeparatorChar, '/')
                                .Replace(Path.AltDirectorySeparatorChar, '/');

                ProcessStartInfo processInfo = new()
                {
                    FileName = blenderPath,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Arguments = command
                };

                using Process? process = Process.Start(processInfo);

                string? output = process?.StandardOutput.ReadToEnd();
                string? error = process?.StandardError.ReadToEnd();

                process?.WaitForExit();

                string cleanOutput = string.Join(Environment.NewLine,
                    output?.Replace("Info: Deleted 0 object(s)", "")
                           .Split(new[] { Environment.NewLine }, StringSplitOptions.None)
                           .Select(line => line.Trim())
                           .Where(line => !string.IsNullOrEmpty(line))
                    ?? []);

                LogsWindowViewModel.Instance.AddLog(cleanOutput ?? "", Logger.LogTags.Info);

                if (!string.IsNullOrEmpty(error))
                {
                    LogsWindowViewModel.Instance.AddLog(error, Logger.LogTags.Error);
                }
            }
            catch (Exception ex)
            {
                LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
                LogsWindowViewModel.Instance.AddLog(ex.Message, Logger.LogTags.Error);
            }
        });

        LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
    }

    private static async Task UploadParsedData()
    {
        LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Running);

        await Task.Run(async () =>
        {
            try
            {
                var config = ConfigurationService.Config;
                var accessKey = config.Sensitive.S3AccessKey;
                var secretKey = config.Sensitive.S3SecretKey;
                var region = config.Sensitive.AWSRegion;
                var bucketName = config.Sensitive.S3BucketName;

                if (string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(secretKey) ||
                    string.IsNullOrEmpty(region) || string.IsNullOrEmpty(bucketName))
                {
                    throw new ArgumentNullException("One or more required configuration values are null or empty. Please check S3AccessKey, S3SecretKey, AWSRegion, and S3BucketName.");
                }

                S3Service s3Service = new(accessKey, secretKey, region);

                string pathToParsedData = Path.Combine(GlobalVariables.pathToParsedData, GlobalVariables.versionWithBranch);

                ChangeFilesIndentation(pathToParsedData, true);

                await s3Service.UploadDirectoryAsync(bucketName, pathToParsedData, "api/");

                ChangeFilesIndentation(pathToParsedData, false);

                LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
            }
            catch (Exception ex)
            {
                LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
                LogsWindowViewModel.Instance.AddLog(ex.Message, Logger.LogTags.Error);
            }
        });
    }

    private static async Task UploadModelsData()
    {
        LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Running);

        await Task.Run(async () =>
        {
            try
            {
                S3Service s3Service = S3Service.CreateFromConfig();

                var config = ConfigurationService.Config;
                string? bucketName = config.Sensitive.S3BucketName;

                if (string.IsNullOrEmpty(bucketName))
                {
                    throw new Exception("Bucket name is not set in settings");
                }

                string pathToModelsData = Path.Combine(GlobalVariables.pathToModelsData, GlobalVariables.versionWithBranch);

                ChangeFilesIndentation(pathToModelsData, true);

                await s3Service.UploadDirectoryAsync(bucketName, pathToModelsData, "assets/");

                ChangeFilesIndentation(pathToModelsData, false);

                LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
            }
            catch (Exception ex)
            {
                LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
                LogsWindowViewModel.Instance.AddLog(ex.Message, Logger.LogTags.Error);
            }
        });
    }

    private static void ChangeFilesIndentation(string directoryPath, bool minify = false)
    {
        foreach (string filePath in Directory.EnumerateFiles(directoryPath, "*.json", SearchOption.AllDirectories))
        {
            if (minify)
            {
                MinifyJsonFile(filePath);
            }
            else
            {
                PrettifyJsonFile(filePath);
            }
        }
    }

    private static void MinifyJsonFile(string filePath)
    {
        string jsonContent = File.ReadAllText(filePath);

        // Parse the JSON content and write it back in minified format
        var jsonDocument = JsonDocument.Parse(jsonContent);
        var options = new JsonWriterOptions { Indented = false };

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, options))
        {
            jsonDocument.WriteTo(writer);
        }
        File.WriteAllBytes(filePath, stream.ToArray());

    }

    private static void PrettifyJsonFile(string filePath)
    {
        string jsonContent = File.ReadAllText(filePath);

        // Parse the JSON content and write it back in indented format
        var jsonDocument = JsonDocument.Parse(jsonContent);
        var options = new JsonWriterOptions { Indented = true };

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, options))
        {
            jsonDocument.WriteTo(writer);
        }

        File.WriteAllBytes(filePath, stream.ToArray());
    }
}