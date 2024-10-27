using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UEParser.ViewModels;
using UEParser.Utils;

namespace UEParser.Netease;

public static class ContentManager
{
    public static void UnpackPakFiles(string inputDirectoryPath)
    {
        var pakFiles = Directory.GetFiles(inputDirectoryPath, "*.pak")
            .Select(Path.GetFileName)
            .ToArray();

        if (pakFiles.Length <= 0 || !pakFiles.Any(f => f != null && f.Contains("optional", StringComparison.OrdinalIgnoreCase)))
            throw new FileNotFoundException("You need to provide at least one regular .pak file, and at least one optional .pak file.");

        var extractDirectory =
            Path.Combine(inputDirectoryPath, "UEPakOutput");
        Directory.CreateDirectory(extractDirectory);

        foreach (var pakFileName in pakFiles)
        {
            if (pakFileName == null) continue;

            LogsWindowViewModel.Instance.AddLog($"Unpacking PAK file: {pakFileName}", Logger.LogTags.Info);
            var pakFilePath = Path.Combine(inputDirectoryPath, pakFileName);

            var arguments = $"unpack \"{pakFilePath}\" -o=\"{extractDirectory}\"";

            CommandUtils.ExecuteCommand(arguments, GlobalVariables.RepakPath, GlobalVariables.RootDir);
        }
    }

    public static void RepackPakFiles(string inputDirectoryPath, string extractDirectory)
    {
        var outputPakPath = Path.Combine(extractDirectory, "CombinedPak.pak");
        var arguments = $"\"{outputPakPath}\" -Create={extractDirectory}";

        CommandUtils.ExecuteCommand(arguments, GlobalVariables.UnrealPakPath, GlobalVariables.RootDir);

        Directory.Delete(inputDirectoryPath, true); // Delete unpacked files after repacking is completed
    }

    public static async Task ChangeMagicValue(string filePath)
    {
        byte[] searchHex = [0x4D, 0xE6, 0x40, 0xBB]; // NetEase magic value
        byte[] replaceHex = [0xE1, 0x12, 0x6F, 0x5A]; // Default Unreal Engine magic value

        try
        {
            byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
            for (int i = fileBytes.Length - searchHex.Length; i >= 0; i--) // Search in reverse as magic value is at the end of the file
            {
                bool foundMatch = true;

                for (int j = 0; j < searchHex.Length; j++)
                {
                    if (fileBytes[i + j] != searchHex[j])
                    {
                        foundMatch = false;
                        break;
                    }
                }

                if (foundMatch)
                {
                    for (int j = 0; j < replaceHex.Length; j++)
                    {
                        fileBytes[i + j] = replaceHex[j];
                    }

                    break;
                }
            }

            await File.WriteAllBytesAsync(filePath, fileBytes);
            LogsWindowViewModel.Instance.AddLog(
                $"Pak '{Path.GetFileName(filePath)}' has been successfully patched.", Logger.LogTags.Info);
        }
        catch (Exception ex)
        {
            LogsWindowViewModel.Instance.AddLog(
                $"Error processing file '{Path.GetFileName(filePath)}': {ex.Message}", Logger.LogTags.Error);
        }
    }
}