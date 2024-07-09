using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using UEParser.Services;
using UEParser.Utils;
using UEParser.Models;
using UEParser.ViewModels;

namespace UEParser;

public class Helpers
{
    public static string[] FindFilePathsInExtractedAssetsCaseInsensitive(string fileToFind)
    {
        return Directory.GetFiles(Path.Combine(GlobalVariables.pathToExtractedAssets), "*", SearchOption.AllDirectories)
            .Where(file => string.Equals(Path.GetFileName(file), fileToFind, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    public static string ConstructVersionHeaderWithBranch(bool switchToCompareVersion = false)
    {
        var config = ConfigurationService.Config;

        string versionWithBranch;
        if (switchToCompareVersion)
        {
            var versionHeader = config.Core.VersionData.CompareVersionHeader;
            var branch = config.Core.VersionData.CompareBranch;
            versionWithBranch = $"{versionHeader}_{branch}";
        }
        else
        {
            var versionHeader = config.Core.VersionData.LatestVersionHeader;
            var branch = config.Core.VersionData.Branch;
            versionWithBranch = $"{versionHeader}_{branch}";
        }

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

    // This method creates file that contains HTML tag converters
    // In the game HTML tags are converted to Rich Text Tag and can be found in 'CoreHTMLTagConvertDB.uasset' datatable
    // For our purpose we use custom values, this can be configured to whatever you need inside 'Dependencies/HelperComponents/tagConverters.json'
    public static void CreateTagConverters()
    {
        string outputPathDirectory = Path.Combine(GlobalVariables.rootDir, "Dependencies", "HelperComponents");
        string outputPath = Path.Combine(outputPathDirectory, "tagConverters.json");

        Directory.CreateDirectory(outputPathDirectory);

        if (File.Exists(outputPath)) return;

        var htmlTagConverters = new Dictionary<string, string>
        {
            { "_GFX::CQGRIcon", "<img src=\"/images/Archives/Glyphs/ChallengeIcon_redGlyph.png\" style=\"vertical-align:middle;\" height=\"25px\" width=\"25px\">" },
            { "_GFX::CQGIBIcon", "<img src=\"/images/Archives/Glyphs/ChallengeIcon_whiteGlyph.png\" style=\"vertical-align:middle;\" height=\"25px\" width=\"25px\">" },
            { "_GFX::CQGBIcon", "<img src=\"/images/Archives/Glyphs/ChallengeIcon_blueGlyph.png\" style=\"vertical-align:middle;\" height=\"25px\" width=\"25px\">" },
            { "_GFX::CQGPIcon", "<img src=\"/images/Archives/Glyphs/ChallengeIcon_purpleGlyph.png\" style=\"vertical-align:middle;\" height=\"25px\" width=\"25px\">" },
            { "_GFX::CQGYIcon", "<img src=\"/images/Archives/Glyphs/ChallengeIcon_yellowGlyph.png\" style=\"vertical-align:middle;\" height=\"25px\" width=\"25px\">" },
            { "_GFX::CQGGIcon", "<img src=\"/images/Archives/Glyphs/ChallengeIcon_greenGlyph.png\" style=\"vertical-align:middle;\" height=\"25px\" width=\"25px\">" },
            { "_GFX::CQGOIcon", "<img src=\"/images/Archives/Glyphs/ChallengeIcon_orangeGlyph.png\" style=\"vertical-align:middle;\" height=\"25px\" width=\"25px\">" },
            { "_GFX::CQGPkIcon", "<img src=\"/images/Archives/Glyphs/ChallengeIcon_pinkGlyph.png\" style=\"vertical-align:middle;\" height=\"25px\" width=\"25px\">" },
            { "_GFX::CQFrgIcon", "<img src=\"/images/Archives/Glyphs/ChallengeIcon_coreMemory.png\" style=\"vertical-align:middle;\" height=\"25px\" width=\"25px\">" },
            { "_GFX::CQFrg02", "<img src=\"/images/Archives/Glyphs/ChallengeIcon_coreMemory02.png\" style=\"vertical-align:middle;\" height=\"25px\" width=\"25px\">" },
            { "_GFX::CQFrg03", "<img src=\"/images/Archives/Glyphs/ChallengeIcon_coreMemory03.png\" style=\"vertical-align:middle;\" height=\"25px\" width=\"25px\">" },
            { "_GFX::CQFrg04", "<img src=\"/images/Archives/Glyphs/ChallengeIcon_coreMemory04.png\" style=\"vertical-align:middle;\" height=\"25px\" width=\"25px\">" }
        };

        TagConverters data = new()
        {
            HTMLTagConverters = htmlTagConverters
        };

        string jsonString = JsonConvert.SerializeObject(data, Formatting.Indented);

        File.WriteAllText(outputPath, jsonString);
    }

    public class CharacterBlueprints
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

    // Dynamic localization method, TODO: support JSON nesting
    // Might abandon trying to make it work for all parsers since it deeply depends on the data
    private static readonly string[] itemsWithoutLocalization = [
        "C_Head01",
        "D_Head01",
        "J_Head01",
        "M_Head01",
        "S01_Head01",
        "DF_Head04",
        "D_Head02",
        "TR_Head03",
        "Default_Badge",
        "Default_Banner"
    ];
    public static void LocalizeDB<T>(Dictionary<string, T> localizedDB, Dictionary<string, Dictionary<string, List<LocalizationEntry>>> localizationData, Dictionary<string, string> languageKeys, string langKey)
    {
        foreach (var item in localizedDB)
        {
            string id = item.Key;
            var localizationDataEntry = localizationData[id];

            foreach (var entry in localizationDataEntry)
            {
                dynamic? dynamicItem = item.Value;

                if (dynamicItem == null)
                {
                    continue;
                }

                Type entryType = dynamicItem[entry.Key].GetType();

                if (entry.Value.Count > 0 && entryType == typeof(JArray))
                {
                    for (int i = 0; i < entry.Value.Count; i++)
                    {
                        JArray localizedStrings = [];

                        if (languageKeys.TryGetValue(entry.Value[i].Key, out string? langValue))
                        {
                            localizedStrings.Add(langValue);
                        }
                        else
                        {
                            if (!itemsWithoutLocalization.Contains(id))
                            {
                                LogsWindowViewModel.Instance.AddLog($"Missing localization string -> LangKey: '{langKey}', Property: '{entry.Key}', StringKey: '{entry.Value[i].Key}', RowId: '{id}', FallbackString: '{entry.Value[i].SourceString}'", Logger.LogTags.Warning);
                            }

                            localizedStrings.Add(entry.Value[i].SourceString);
                        }

                        dynamicItem[entry.Key] = localizedStrings;
                    }
                }
                else if (entry.Value.Count == 1)
                {
                    try
                    {
                        string localizedString;

                        if (languageKeys.TryGetValue(entry.Value[0].Key, out string? langValue))
                        {
                            localizedString = langValue;
                        }
                        else
                        {
                            if (!itemsWithoutLocalization.Contains(id))
                            {
                                LogsWindowViewModel.Instance.AddLog($"Missing localization string -> LangKey: '{langKey}', Property: '{entry.Key}', StringKey: '{entry.Value[0].Key}', RowId: '{id}', FallbackString: '{entry.Value[0].SourceString}'", Logger.LogTags.Warning);
                            }

                            localizedString = entry.Value[0].SourceString;
                        }

                        dynamicItem[entry.Key] = localizedString.ToString();
                    }
                    catch (Exception ex)
                    {
                        LogsWindowViewModel.Instance.AddLog($"Missing localization string -> LangKey: '{langKey}', Property: '{entry.Key}', StringKey: '{entry.Value[0].Key}', RowId: '{id}', FallbackString: '{entry.Value[0].SourceString}' <- {ex}", Logger.LogTags.Warning);
                    }
                }
            }
        }
    }
}