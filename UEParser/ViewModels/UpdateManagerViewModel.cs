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

    public UpdateManagerViewModel()
    {
        UploadParsedDataCommand = ReactiveCommand.Create(UploadParsedData);
        ConvertUEModelsCommand = ReactiveCommand.Create(ConvertUEModels);
    }

    //private const string BlenderPath = @"C:\Program Files\Blender Foundation\Blender 2.92\blender.exe";
    //private const string UEModelsConverterScriptPath = @"C:\Users\mateu\Downloads\CodeProjects\UParser\UEModelsConverter\UEModelsConverter.py";
    public static void ConvertUEModels()
    {
        LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Running);

        var config = ConfigurationService.Config;
        string blenderPath = config.Global.BlenderPath;
        string rootDirectory = GlobalVariables.rootDir;
        string inputDirectory = Path.Combine(GlobalVariables.rootDir, "Input");
        string inputMappingDirectory = Path.Combine(GlobalVariables.rootDir, "Output", "ModelsData", GlobalVariables.versionWithBranch);
        string outputDirectory = Path.Combine(GlobalVariables.rootDir, "Output", "ConvertedModels", GlobalVariables.versionWithBranch);

        string command = ($"-b -P \"{GlobalVariables.modelsConverterScriptPath}\" " +
                        $"-- --root_directory \"{rootDirectory}\" " +
                        $"--input_directory \"{inputDirectory}\" " +
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

        LogsWindowViewModel.Instance.AddLog(output ?? "", Logger.LogTags.Info);

        if (!string.IsNullOrEmpty(error))
        {
            LogsWindowViewModel.Instance.AddLog(error, Logger.LogTags.Error);
        }

        LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
    }

    private static async Task UploadParsedData()
    {
        LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Running);

        var config = ConfigurationService.Config;
        var accessKey = config.Sensitive.S3AccessKey;
        var secretKey = config.Sensitive.S3SecretKey;
        var region = config.Sensitive.AWSRegion;
        var bucketName = config.Sensitive.S3BucketName;

        S3Service s3Service = new(accessKey, secretKey, region);

        string pathToParsedData = Path.Combine(GlobalVariables.pathToParsedData, GlobalVariables.versionWithBranch);

        ChangeFilesIndentation(pathToParsedData, true);

        await s3Service.UploadDirectoryAsync(bucketName, pathToParsedData, "api/");

        ChangeFilesIndentation(pathToParsedData, false);

        LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
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