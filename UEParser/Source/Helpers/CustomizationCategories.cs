using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using UEParser.Utils;

namespace UEParser;

public partial class Helpers
{
    public static void CreateCustomizationCategoriesTable()
    {
        string outputPath = Path.Combine(GlobalVariables.RootDir, "Dependencies", "HelperComponents", "customizationCategories.json");

        string[] filePaths = FindFilePathsInExtractedAssetsCaseInsensitive("CustomizationCategoriesDB.json");

        var customizationCategories = new Dictionary<string, string>();
        foreach (string filePath in filePaths)
        {
            string jsonString = File.ReadAllText(filePath);
            List<Dictionary<string, dynamic>>? items = JsonConvert.DeserializeObject<List<Dictionary<string, dynamic>>>(jsonString);
            if (items?[0]?["Rows"] != null)
            {
                foreach (var item in items[0]["Rows"])
                {
                    string id = item.Name;

                    string category = item.Value["Category"];
                    string categoryId = StringUtils.DoubleDotsSplit(category);

                    customizationCategories[id] = categoryId;
                }
            }
        }

        string data = JsonConvert.SerializeObject(customizationCategories, Formatting.Indented);

        File.WriteAllText(outputPath, data);
    }
}