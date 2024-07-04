using Newtonsoft.Json;
using System;
using System.IO;

namespace UEParser.Utils;

public class FileUtils
{
    // Used to load exported game properties
    public static dynamic? LoadDynamicJson(string pathToJsonFile)
    {
        string data = File.ReadAllText(pathToJsonFile) ?? throw new Exception("File content is empty.");
        dynamic? deserializedData = JsonConvert.DeserializeObject<dynamic>(data);

        return deserializedData;
    }
}
