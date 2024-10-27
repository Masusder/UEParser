using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace UEParser;

public partial class Helpers
{
    public static void CreateLocresFiles()
    {
        // Search for locres files
        string[] filePaths = Directory.GetFiles(Path.Combine(GlobalVariables.RootDir, "Dependencies\\ExtractedAssets\\DeadByDaylight\\Content\\Localization\\DeadByDaylight\\"), "DeadByDaylight.json", SearchOption.AllDirectories);

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

            if (!File.Exists(locresDefinitionPath))
            {
                throw new FileNotFoundException("Locres definition was not found.");
            }

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
}
