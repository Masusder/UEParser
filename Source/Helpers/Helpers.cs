using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Data;

using UEParser.Services;
using UEParser.Models;
using UEParser.ViewModels;

namespace UEParser;

public partial class Helpers
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
        "Default_Banner",
        "Item_Camper_K33MagicItem_Bracers",
        "Item_Camper_K33MagicItem_Boots",
        "jnk_gasstation",
        "Frm_Farmhouse",
        "Frm_Slaughterhouse",
        "Frm_Silo",
        "Frm_CornField",
        "Frm_Barn"
    ];
    // Dynamic localization method
    // Supports localzation of nested data - ex. 'Levels.0.Nodes.node_T19_L1_01.Name'
    public static void LocalizeDB<T>(
        Dictionary<string, T> localizedDB,
        Dictionary<string, Dictionary<string, List<LocalizationEntry>>> localizationData,
        Dictionary<string, string> languageKeys,
        string langKey)
    {
        foreach (var item in localizedDB)
        {
            string id = item.Key;
            var localizationDataEntry = localizationData[id];

            foreach (var entry in localizationDataEntry)
            {
                string propertiesKeys = entry.Key;
                string[] splitKeys = propertiesKeys.Split('.');

                if (item.Value == null) continue;

                object currentObject = item.Value;
                //Type currentType = currentObject.GetType();

                object? nestedValue = GetNestedValue(currentObject, splitKeys);
                if (nestedValue == null) continue;

                if (entry.Value.Count > 0 && nestedValue.GetType() == typeof(JArray))
                {
                    JArray localizedStrings = [];

                    foreach (var localizationEntry in entry.Value)
                    {
                        if (languageKeys.TryGetValue(localizationEntry.Key, out string? langValue))
                        {
                            localizedStrings.Add(langValue);
                        }
                        else
                        {
                            if (!itemsWithoutLocalization.Contains(id))
                            {
                                LogsWindowViewModel.Instance.AddLog($"Missing localization string -> LangKey: '{langKey}', Property: '{entry.Key}', StringKey: '{localizationEntry.Key}', RowId: '{id}', FallbackString: '{localizationEntry.SourceString}'", Logger.LogTags.Warning);
                            }

                            localizedStrings.Add(localizationEntry.SourceString);
                        }
                    }

                    UpdateNestedValue(localizedDB, id, splitKeys, localizedStrings);
                }
                else if (entry.Value.Count == 1)
                {
                    string localizedString;
                    string? localizationKey = entry.Value[0].Key;

                    if (localizationKey == null)
                    {
                        if (!itemsWithoutLocalization.Contains(id))
                        {
                            LogsWindowViewModel.Instance.AddLog($"Null localization key -> LangKey: '{langKey}', Property: '{entry.Key}', RowId: '{id}', FallbackString: 'null'", Logger.LogTags.Warning);
                        }
                        localizedString = entry.Value[0].SourceString;
                    }
                    else if (languageKeys.TryGetValue(localizationKey, out string? langValue))
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

                    UpdateNestedValue(localizedDB, id, splitKeys, localizedString);
                }
            }
        }
    }

    private static void UpdateNestedValue<T>(Dictionary<string, T> dictionary, string id, string[] keys, object newValue)
    {
        // Get the original value from the dictionary
        if (dictionary.TryGetValue(id, out T? originalValue))
        {
            // Navigate to the nested property using keys and update its value
            object? current = originalValue;
            for (int i = 0; i < keys.Length - 1; i++)
            {
                string key = keys[i];
                Type? currentType = current?.GetType();

                if (currentType == null) return;

                if (currentType.IsGenericType && currentType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    PropertyInfo? indexer = currentType.GetProperty("Item");
                    current = indexer?.GetValue(current, [key]);
                }
                else if (typeof(IEnumerable<object>).IsAssignableFrom(currentType))
                {
                    current = GetEnumerableElement(current, key);
                }
                else
                {
                    PropertyInfo? propInfo = currentType.GetProperty(key);
                    if (propInfo != null)
                    {
                        current = propInfo.GetValue(current);
                    }
                    else
                    {
                        return; // Property not found
                    }
                }
            }

            // Update the final nested property with newValue
            string lastKey = keys[^1];
            Type? finalType = current?.GetType();

            if (finalType == null) return;

            if (finalType.IsGenericType && finalType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                PropertyInfo? indexer = finalType?.GetProperty("Item");
                indexer?.SetValue(current, newValue, [lastKey]);
            }
            else if (typeof(JObject).IsAssignableFrom(finalType))
            {
                if (current is JObject jObject)
                {
                    jObject[lastKey] = JToken.FromObject(newValue);
                }
            }
            else if (typeof(IList).IsAssignableFrom(finalType))
            {
                if (int.TryParse(lastKey, out int index))
                {
                    IList? list = (IList?)current;
                    if (index >= 0 && index < list?.Count)
                    {
                        list[index] = newValue;
                    }
                }
            }
            else
            {
                PropertyInfo? propInfo = finalType.GetProperty(lastKey);
                if (propInfo != null && propInfo.CanWrite)
                {
                    propInfo.SetValue(current, newValue);
                }
            }

            dictionary[id] = originalValue;
        }
    }

    private static object? GetNestedValue(object? obj, string[] keys)
    {
        foreach (var key in keys)
        {
            if (obj == null)
            {
                return null;
            }

            Type objType = obj.GetType();

            if (objType.IsGenericType && objType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                // Handle case where obj is a dictionary
                PropertyInfo? indexer = objType?.GetProperty("Item");
                obj = indexer?.GetValue(obj, [key]);
            }
            else if (typeof(IEnumerable<object>).IsAssignableFrom(objType))
            {
                // Handle case where obj is an IEnumerable<object>
                obj = GetEnumerableElement(obj, key);
            }
            else
            {
                // Handle case where obj is a class or object
                PropertyInfo? propInfo = objType?.GetProperty(key);
                if (propInfo != null)
                {
                    obj = propInfo.GetValue(obj);
                }
                else
                {
                    obj = null;
                    break;
                }
            }
        }

        return obj;
    }

    private static object? GetEnumerableElement(object? obj, string key)
    {
        IEnumerable<object?>? enumerable = (IEnumerable<object?>?)obj;

        if (enumerable == null) return obj;

        // Attempt to parse key as an index
        if (int.TryParse(key, out int index))
        {
            // Access by index
            try
            {
                obj = enumerable?.ElementAt(index);
            }
            catch (ArgumentOutOfRangeException)
            {
                obj = null;
            }
        }
        else
        {
            // Access by property name
            foreach (var element in enumerable)
            {
                if (element != null)
                {
                    if (element is JObject jObject)
                    {
                        if (jObject[key] != null)
                        {
                            obj = jObject[key];
                            break;
                        }
                    }
                    else if (element is JProperty jProperty)
                    {
                        // Handle nested JObject inside JProperty value
                        if (jProperty.Value is JObject nestedJObject)
                        {
                            if (nestedJObject[key] != null)
                            {
                                obj = nestedJObject[key];
                                break;
                            }
                        }
                        else if (jProperty.Value is JValue jValue && jProperty.Name == key)
                        {
                            obj = jValue.Value;
                            break;
                        }
                    }
                }
            }
        }

        return obj;
    }

}