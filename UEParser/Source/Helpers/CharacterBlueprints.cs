using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using UEParser.Models;
using UEParser.Utils;

namespace UEParser;

public partial class Helpers
{
    public static void CombineCharacterBlueprints()
    {
        string outputPath = Path.Combine(GlobalVariables.rootDir, "Dependencies/HelperComponents/characterBlueprintsLinkage.json");

        var characters = TraverseCharacterDescriptionDB();
        var cosmetics = TraverseCharacterDescriptionOverrideDB();

        var characterBlueprints = new CharacterBlueprintsModel
        {
            Characters = characters,
            Cosmetics = cosmetics
        };

        string data = JsonConvert.SerializeObject(characterBlueprints, Formatting.Indented);

        File.WriteAllText(outputPath, data);
    }

    private static Dictionary<string, CharacterData> TraverseCharacterDescriptionDB()
    {
        string[] filePaths = FindFilePathsInExtractedAssetsCaseInsensitive("CharacterDescriptionDB.json");
        //Directory.GetFiles(Path.Combine(GlobalVariables.rootDir, "Dependencies", "ExtractedAssets", "DeadByDaylight"), "CharacterDescriptionDB.json", SearchOption.AllDirectories);
        var characters = new Dictionary<string, CharacterData>();

        foreach (string filePath in filePaths)
        {
            bool isInDBDCharacters = false;
            if (filePath.Contains("DBDCharacters"))
            {
                isInDBDCharacters = true;
            }

            string jsonString = File.ReadAllText(filePath);
            List<Dictionary<string, dynamic>>? items = JsonConvert.DeserializeObject<List<Dictionary<string, dynamic>>>(jsonString);
            if (items?[0]?["Rows"] != null)
            {
                foreach (var item in items[0]["Rows"])
                {
                    string characterIndex = item.Name;

                    string gameBlueprintPathRaw = item.Value["GamePawn"]["AssetPathName"];
                    string gameBlueprintPath = StringUtils.ModifyPath(gameBlueprintPathRaw, "json", isInDBDCharacters, int.Parse(characterIndex));

                    string menuBlueprintPathRaw = item.Value["MenuPawn"]["AssetPathName"];
                    string menuBlueprintPath = StringUtils.ModifyPath(menuBlueprintPathRaw, "json", isInDBDCharacters, int.Parse(characterIndex));

                    characters[characterIndex] = new CharacterData
                    {
                        GameBlueprint = gameBlueprintPath,
                        MenuBlueprint = menuBlueprintPath
                    };
                }
            }
        }

        return characters;
    }

    private static Dictionary<string, CosmeticData> TraverseCharacterDescriptionOverrideDB()
    {
        string[] filePaths = FindFilePathsInExtractedAssetsCaseInsensitive("CharacterDescriptionOverrideDB.json");
        //Directory.GetFiles(Path.Combine(Constants.ROOT_DIR, "Dependencies", "ExtractedAssets", "DeadByDaylight", "Content", "Data"), "CharacterDescriptionOverrideDB.json", SearchOption.AllDirectories);
        var cosmetics = new Dictionary<string, CosmeticData>();

        foreach (string filePath in filePaths)
        {
            bool isInDBDCharacters = false;
            if (filePath.Contains("DBDCharacters"))
            {
                isInDBDCharacters = true;
            }

            string jsonString = File.ReadAllText(filePath);
            List<Dictionary<string, dynamic>>? items = JsonConvert.DeserializeObject<List<Dictionary<string, dynamic>>>(jsonString);
            if (items?[0]?["Rows"] != null)
            {
                foreach (var item in items[0]["Rows"])
                {
                    string cosmeticId = item.Name;
                    JArray cosmeticItems = item.Value["RequiredItemIds"];
                    string gameBlueprintPathRaw = item.Value["GameBlueprint"]["AssetPathName"];
                    string gameBlueprintPath = StringUtils.ModifyPath(gameBlueprintPathRaw, "json", isInDBDCharacters);

                    string menuBlueprintPathRaw = item.Value["MenuBlueprint"]["AssetPathName"];
                    string menuBlueprintPath = StringUtils.ModifyPath(menuBlueprintPathRaw, "json", isInDBDCharacters);

                    cosmetics[cosmeticId] = new CosmeticData
                    {
                        CosmeticItems = cosmeticItems,
                        GameBlueprint = gameBlueprintPath,
                        MenuBlueprint = menuBlueprintPath
                    };
                }
            }
        }

        return cosmetics;
    }
}