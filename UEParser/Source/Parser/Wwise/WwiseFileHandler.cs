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
using Amazon.Runtime.Internal.Util;
using UEParser.Utils;

namespace UEParser.Parser.Wwise;

public partial class WwiseFileHandler
{
    private static readonly string temporaryDirectory = Path.Combine(GlobalVariables.pathToExtractedAudio, "WwiseTemporary");
    private static readonly string wwiseStructured = Path.Combine(GlobalVariables.pathToExtractedAudio, "WwiseStructured");

    public static bool DoesSoundBankExist()
    {
        string soundsBankNameJson = "SoundbanksInfo.json";
        string[] filePathsJson = Helpers.FindFilePathsInExtractedAssetsCaseInsensitive(soundsBankNameJson);

        if (filePathsJson.Length > 0)
        {
            return true;
        }

        return false;
    }

    public static void MoveCompressedAudio()
    {
        string pathToExtractedWwiseDirectory = Path.Combine(GlobalVariables.pathToExtractedAssets, "DeadByDaylight", "Content", "WwiseAudio", "Windows");

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

    public static void ConvertToOggAndMove()
    {
        string[] wemFiles = Directory.GetFiles(wwiseStructured, "*.wem", SearchOption.AllDirectories);

        var audioToParse = WwiseRegister.RetrieveAudioToParse(wemFiles);

        var commands = new List<WwiseUtilities.CommandModel>();

        int failedAudioConversionCount = 0;
        Parallel.ForEach(wemFiles, audioPath =>
        {
            string relativePath = Utils.StringUtils.StripDynamicDirectory(audioPath, wwiseStructured);
            string tempOggPath = Path.ChangeExtension(audioPath, ".ogg");

            string targetDirectory = Path.Combine(GlobalVariables.rootDir, "Output", "ExtractedAssets", "Audio", GlobalVariables.versionWithBranch);
            string finalOggPath = Path.Combine(targetDirectory, Path.ChangeExtension(relativePath, ".ogg"));

            Directory.CreateDirectory(Path.GetDirectoryName(finalOggPath)!);

            // Convert .wem to .ogg using ww2ogg
            WwiseUtilities.ExecuteCommand($"\"{audioPath}\" --pcb \"{GlobalVariables.packedCodebooksPath}\"", GlobalVariables.ww2oggPath);

            if (File.Exists(tempOggPath))
            {
                // Compress .ogg using revorb
                WwiseUtilities.ExecuteCommand($"\"{tempOggPath}\"", GlobalVariables.revorbPath);
            }
            else
            {
                failedAudioConversionCount++;
            }
        });

        if (failedAudioConversionCount > 0)
        {
            LogsWindowViewModel.Instance.AddLog($"Failed to convert total of {failedAudioConversionCount} files to OGG format.", Logger.LogTags.Warning);
        }

        MoveAudioToOutput();
    }

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
    public static void StructureAudio()
    {
        var soundBankDictionary = WwiseRegister.SoundBankDictionary;

        string[] wemFiles = Directory.GetFiles(temporaryDirectory, "*.wem", SearchOption.AllDirectories);

        static void MoveFile(string wemFilePath, string structuredOutputPath)
        {
            try
            {
                File.Move(wemFilePath, structuredOutputPath, true);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error moving file {wemFilePath} to {structuredOutputPath}: {ex.Message}");
            }
        }

        foreach (var wemFilePath in wemFiles)
        {
            string id = Path.GetFileNameWithoutExtension(wemFilePath);

            string? structuredPath = soundBankDictionary[id];

            if (string.IsNullOrEmpty(structuredPath))
            {
                string structuredOutputPath = Path.Combine(wwiseStructured, "Unknown");
                Directory.CreateDirectory(Path.GetDirectoryName(structuredOutputPath)!);

                MoveFile(wemFilePath, structuredOutputPath);
            }
            else
            {
                string structuredOutputPath = Path.Combine(wwiseStructured, structuredPath);

                Directory.CreateDirectory(Path.GetDirectoryName(structuredOutputPath)!);

                MoveFile(wemFilePath, structuredOutputPath);
            }
        }
    }
}