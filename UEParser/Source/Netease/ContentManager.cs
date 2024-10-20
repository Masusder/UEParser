using System;
using System.IO;
using System.Threading.Tasks;
using UEParser.ViewModels;

namespace UEParser.Netease;

public class ContentManager
{
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

            File.WriteAllBytes(filePath, fileBytes);
            LogsWindowViewModel.Instance.AddLog($"Pak '{Path.GetFileName(filePath)}' has been successfully patched.", Logger.LogTags.Info);
        }
        catch (Exception ex)
        {
            LogsWindowViewModel.Instance.AddLog($"Error processing file '{Path.GetFileName(filePath)}': {ex.Message}", Logger.LogTags.Error);
        }
    }
}