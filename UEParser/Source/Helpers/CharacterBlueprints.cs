using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using UEParser.Models;

namespace UEParser;

public partial class Helpers
{
    public static void CombineCharacterBlueprints()
    {
        string outputPath = Path.Combine(GlobalVariables.RootDir, "Dependencies", "HelperComponents", "characterBlueprintsLinkage.json");

        var characters = TraverseCharacterDescriptionDb();
        var cosmetics = TraverseCharacterDescriptionOverrideDb();

        var characterBlueprints = new CharacterBlueprintsModel
        {
            Characters = characters,
            Cosmetics = cosmetics
        };

        string data = JsonConvert.SerializeObject(characterBlueprints, Formatting.Indented);

        File.WriteAllText(outputPath, data);
    }

    private static Dictionary<string, CharacterData> TraverseCharacterDescriptionDb()
    {
        string[] filePaths = FindFilePathsInExtractedAssetsCaseInsensitive("CharacterDescriptionDB.json");

        var characters = new Dictionary<string, CharacterData>();

        foreach (string filePath in filePaths)
        {
            string jsonString = File.ReadAllText(filePath);
            List<Dictionary<string, dynamic>>? items = JsonConvert.DeserializeObject<List<Dictionary<string, dynamic>>>(jsonString);
            if (items?[0]?["Rows"] != null)
            {
                foreach (var item in items[0]["Rows"])
                {
                    string characterIndex = item.Name;

                    string gameBlueprintPathRaw = item.Value["GamePawn"]["AssetPathName"];
                    string menuBlueprintPathRaw = item.Value["MenuPawn"]["AssetPathName"];

                    characters[characterIndex] = new CharacterData
                    {
                        GameBlueprint = gameBlueprintPathRaw,
                        MenuBlueprint = menuBlueprintPathRaw
                    };
                }
            }
        }

        return characters;
    }

    private static Dictionary<string, CosmeticData> TraverseCharacterDescriptionOverrideDb()
    {
        string[] filePaths = FindFilePathsInExtractedAssetsCaseInsensitive("CharacterDescriptionOverrideDB.json");

        var cosmetics = new Dictionary<string, CosmeticData>();

        foreach (string filePath in filePaths)
        {
            string jsonString = File.ReadAllText(filePath);
            List<Dictionary<string, dynamic>>? items = JsonConvert.DeserializeObject<List<Dictionary<string, dynamic>>>(jsonString);
            if (items?[0]?["Rows"] != null)
            {
                foreach (var item in items[0]["Rows"])
                {
                    string cosmeticId = item.Name;
                    JArray cosmeticItems = item.Value["RequiredItemIds"];
                    string gameBlueprintPathRaw = item.Value["GameBlueprint"]["AssetPathName"];
                    string menuBlueprintPathRaw = item.Value["MenuBlueprint"]["AssetPathName"];

                    cosmetics[cosmeticId] = new CosmeticData
                    {
                        CosmeticItems = cosmeticItems,
                        GameBlueprint = gameBlueprintPathRaw,
                        MenuBlueprint = menuBlueprintPathRaw
                    };
                }
            }
        }

        return cosmetics;
    }
}