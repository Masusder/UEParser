using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UEParser.Services;

namespace UEParser;

public class Helpers
{
    public static string ConstructVersionHeaderWithBranch()
    {
        var config = ConfigurationService.Config;
        var versionHeader = config.Core.VersionData.LatestVersionHeader;
        var branch = config.Core.VersionData.Branch;
        var versionWithBranch = $"{versionHeader}_{branch}";

        return versionWithBranch;
    }

    public static void CreateLocresFiles()
    {
        // Search for locres files
        string[] filePaths = Directory.GetFiles(Path.Combine(GlobalVariables.rootDir, "Dependencies\\ExtractedAssets\\DeadByDaylight\\Content\\Localization\\DeadByDaylight\\"), "DeadByDaylight.json", SearchOption.AllDirectories);

        List<string> localizationsList = new(filePaths);

        // Grab locres definition and remove from original list 
        // Note that this will only work if locres defintion is first (and should be first) on the list
        string locresDefinitionPath = localizationsList.First();
        localizationsList.RemoveRange(0, Math.Min(1, localizationsList.Count));

        // Loop through locres files
        string? outputName = null;
        foreach (var directoryItem in localizationsList)
        {
            // Empty object to add fixed locres to
            var emptyObject = new Dictionary<string, string>();

            // Read locres file
            string locresJsonItem = File.ReadAllText(directoryItem);
            Dictionary<string, dynamic>? locresJson = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(locresJsonItem);
            if (locresJson != null)
            {
                foreach (var locresItem in locresJson)
                {
                    foreach (var singleItem in locresItem.Value)
                    {
                        if (!emptyObject.ContainsKey(singleItem.Name))
                        {
                            emptyObject.Add(singleItem.Name, singleItem.Value.ToString());
                        }
                    }
                }
            }

            // Split directory path to search for language key
            string[] directoryPathSplit = directoryItem.Split(Path.DirectorySeparatorChar);

            // Read available language keys
            dynamic? locresDefintion = JsonConvert.DeserializeObject(File.ReadAllText(locresDefinitionPath));

            if (locresDefintion != null)
            {
                foreach (var langKey in locresDefintion["CompiledCultures"])
                {
                    // Get name of the language
                    bool exists = Array.Exists(directoryPathSplit, element => element == langKey.Value);

                    if (exists)
                    {
                        outputName = langKey;
                    }
                }
            }

            // Output fixed localization file
            string combinedJsonString = JsonConvert.SerializeObject(emptyObject, Formatting.Indented);

            File.WriteAllText($"Dependencies/Locres/locres_{outputName}.json", combinedJsonString);
        }
    }

    public static void CreateCharacterTable()
    {
        string versionWithBranch = ConstructVersionHeaderWithBranch();
        string catalogPath = Path.Combine(GlobalVariables.rootDir, "Output", "API", versionWithBranch, "catalog.json");
        string outputPath = Path.Combine(GlobalVariables.rootDir, "Dependencies", "HelperComponents", "characterIds.json");
        string catalog = File.ReadAllText(catalogPath);
        List<Dictionary<string, dynamic>>? catalogJson = JsonConvert.DeserializeObject<List<Dictionary<string, dynamic>>>(catalog);

        Dictionary<string, int> jsonObject = [];

        if (catalogJson != null)
        {
            foreach (var catalogKey in catalogJson)
            {
                JArray categoryArray = catalogKey["categories"];
                List<string>? categoryList = categoryArray.ToObject<List<string>>();

                if (categoryList != null)
                {
                    if (categoryList.Contains("character"))
                    {
                        string characterId = catalogKey["id"];
                        jsonObject.Add(characterId.ToLower(), Convert.ToInt32(catalogKey["metaData"]["character"]));
                    }
                }
            }
        }

        string combinedJsonString = JsonConvert.SerializeObject(jsonObject, Formatting.Indented);

        File.WriteAllText(outputPath, combinedJsonString);
    }

    public class Archives
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
}