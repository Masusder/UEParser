using Amazon.S3.Transfer;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace UEParser.ViewModels;

public class UpdateManagerViewModel : ReactiveObject
{
    public ICommand UploadParsedDataCommand { get; }

    public UpdateManagerViewModel()
    {
        UploadParsedDataCommand = ReactiveCommand.Create(UploadParsedData);
    }

    private static void UploadParsedData()
    {
        string pathToParsedData = Path.Combine(GlobalVariables.pathToParsedData, GlobalVariables.versionWithBranch);

        foreach (string filePath in Directory.EnumerateFiles(pathToParsedData, "*.json", SearchOption.AllDirectories))
        {
            string? minifiedFilePath = MinifyJsonFile(filePath);

            if (minifiedFilePath != null)
            {
                // TODO: add upload logic
                //await UploadFileToS3(minifiedFilePath);
                //PrettifyJsonFile(filePath); // Re-indent the JSON file after upload
            }
        }
    }

    private static string? MinifyJsonFile(string filePath)
    {
        try
        {
            string jsonContent = File.ReadAllText(filePath);

            // Parse the JSON content and write it back in minified format
            var jsonDocument = JsonDocument.Parse(jsonContent);
            var options = new JsonWriterOptions { Indented = false };

            using (var stream = new MemoryStream())
            {
                using (var writer = new Utf8JsonWriter(stream, options))
                {
                    jsonDocument.WriteTo(writer);
                }
                File.WriteAllBytes(filePath, stream.ToArray());
            }

            return filePath;
        }
        catch
        {
            //Console.WriteLine($"An error occurred while minifying {filePath}: {ex.Message}");
            return null;
        }
    }

    private static void PrettifyJsonFile(string filePath)
    {
        try
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
        catch
        {
            //Console.WriteLine($"An error occurred while prettifying {filePath}: {ex.Message}");
        }
    }
}