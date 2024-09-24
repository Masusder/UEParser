using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace UEParser;

public partial class Helpers
{
    public static void CreateArchiveQuestObjectiveDB()
    {
        string[] filePaths = Directory.GetFiles(Path.Combine(GlobalVariables.rootDir, "Dependencies", "ExtractedAssets"), "ArchiveQuestObjectiveDB.json", SearchOption.AllDirectories);

        string outputPath = Path.Combine(GlobalVariables.rootDir, "Dependencies", "HelperComponents", "questObjectiveDatabase.json");

        Dictionary<string, object> jsonObject = [];

        foreach (string filePath in filePaths)
        {
            string jsonString = File.ReadAllText(filePath);
            List<Dictionary<string, dynamic>>? items = JsonConvert.DeserializeObject<List<Dictionary<string, dynamic>>>(jsonString);

            if (items?[0]?["Rows"] != null)
            {
                foreach (var item in items[0]["Rows"])
                {
                    if (!jsonObject.ContainsKey(item.Name.ToLower()))
                    {
                        jsonObject.Add(item.Name.ToLower(), item.Value);
                    }
                }
            }
        }

        string combinedJsonString = JsonConvert.SerializeObject(jsonObject, Formatting.Indented);

        File.WriteAllText(outputPath, combinedJsonString);
    }

    public static void CreateQuestNodeDatabase()
    {
        string[] filePaths = Directory.GetFiles(Path.Combine(GlobalVariables.rootDir, "Dependencies", "ExtractedAssets"), "ArchiveNodeDB.json", SearchOption.AllDirectories);

        string outputPath = Path.Combine(GlobalVariables.rootDir, "Dependencies", "HelperComponents", "questNodeDatabase.json");

        Dictionary<string, object> jsonObject = [];

        foreach (string filePath in filePaths)
        {
            string jsonString = File.ReadAllText(filePath);
            List<Dictionary<string, dynamic>>? items = JsonConvert.DeserializeObject<List<Dictionary<string, dynamic>>>(jsonString);

            if (items?[0]?["Rows"] != null)
            {
                foreach (var item in items[0]["Rows"])
                {
                    if (!jsonObject.ContainsKey(item.Name.ToLower()))
                    {
                        jsonObject.Add(item.Name.ToLower(), item.Value);
                    }
                }
            }
        }

        string combinedJsonString = JsonConvert.SerializeObject(jsonObject, Formatting.Indented);

        File.WriteAllText(outputPath, combinedJsonString);
    }
}