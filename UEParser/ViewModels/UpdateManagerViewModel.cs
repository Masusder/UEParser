using Amazon.Runtime.Internal.Util;
using Amazon.S3.Transfer;
using Microsoft.VisualBasic;
using ReactiveUI;
using SharpGLTF.Schema2;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using UEParser.Services;
using UEParser.Utils;

namespace UEParser.ViewModels;

public class UpdateManagerViewModel : ReactiveObject
{
    public ICommand UploadParsedDataCommand { get; }
    public ICommand ConvertUEModelsCommand { get; }
    public ICommand ValidateAssetsCommand { get; }
    public ICommand UploadModelsDataCommand { get; }

    public UpdateManagerViewModel()
    {
        UploadParsedDataCommand = ReactiveCommand.Create(UploadParsedData);
        ConvertUEModelsCommand = ReactiveCommand.Create(ConvertUEModels);
        ValidateAssetsCommand = ReactiveCommand.Create(ValidateAssetsInBucket);
        UploadModelsDataCommand = ReactiveCommand.Create(UploadModelsData);
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
                            if (!path.Equals(correctCasePath))
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