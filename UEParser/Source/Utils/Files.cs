using System;
using System.IO;
using Newtonsoft.Json;

namespace UEParser.Utils;

public class FileUtils
{
    // Used to load exported game properties
    public static dynamic LoadDynamicJson(string pathToJsonFile)
    {
        if (!File.Exists(pathToJsonFile)) throw new Exception($"JSON file doesn't exist: {pathToJsonFile}");

        string? data = File.ReadAllText(pathToJsonFile) ?? throw new Exception("File content is empty.");
        dynamic? deserializedData = JsonConvert.DeserializeObject<dynamic>(data);

        return deserializedData ?? throw new Exception("Loaded dynamic data is null.");
    }

    public static T LoadJsonFileWithTypeCheck<T>(string pathToJsonFile)
    {
        string data = File.ReadAllText(pathToJsonFile) ?? throw new Exception("File content is empty.");
        T? deserializedData = JsonConvert.DeserializeObject<T>(data);

        return deserializedData ?? throw new Exception("Loaded data is null.");
    }

    public static T ReadApiResponseFromFile<T>(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));

        string baseDirectory = Path.Combine(GlobalVariables.PathToKraken, GlobalVariables.VersionWithBranch, "API");
        string fullPath = Path.Combine(baseDirectory, fileName);

        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"The file '{fileName}' does not exist in the directory '{baseDirectory}'.");

        string jsonData = File.ReadAllText(fullPath);
        return JsonConvert.DeserializeObject<T>(jsonData)
               ?? throw new InvalidOperationException("Failed to deserialize the JSON file.");
    }

    public static bool IsFileLocked(string filePath)
    {
        try
        {
            using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
        }
        catch (IOException)
        {
            return true;
        }

        return false;
    }
}