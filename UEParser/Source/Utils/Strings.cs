using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.IO;
using UEParser.Models;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace UEParser.Utils;

public partial class StringUtils
{
    //public static string ChangeToNeteasePath(string path)
    //{
    //    string pathWithoutGame = path.Replace("/Game/", "");
    //    if (pathWithoutGame.StartsWith('/'))
    //    {
    //        pathWithoutGame = pathWithoutGame[1..];
    //    }
    //    string modifiedPath = Path.Combine("/netease", "assets", pathWithoutGame);
    //    string unifiedSlashes = modifiedPath.Replace("\\", "/");

    //    return unifiedSlashes;
    //}

    //public static List<LocalizationEntry> GetCollectionName(dynamic asset)
    //{
    //    var localizationEntries = new List<LocalizationEntry>();
    //    // Check if "CollectionName" exists in the root of the asset
    //    if (asset.Value["CollectionName"] != null)
    //    {
    //        // Check if "LocalizedString" exists within "CollectionName"
    //        if (asset.Value["CollectionName"]["LocalizedString"] != null)
    //        {
    //            LocalizationEntry entry = new()
    //            {
    //                Key = asset.Value["CollectionName"]["Key"].ToString(),
    //                SourceString = asset.Value["CollectionName"]["SourceString"].ToString()
    //            };
    //            localizationEntries.Add(entry);

    //            return localizationEntries;
    //        }
    //    }

    //    // Check if "UIData" exists
    //    if (asset.Value["UIData"] != null && asset.Value["UIData"]["CollectionName"] != null)
    //    {
    //        // Check if "LocalizedString" exists within nested "CollectionName"
    //        if (asset.Value["UIData"]["CollectionName"]["LocalizedString"] != null)
    //        {
    //            LocalizationEntry entry = new()
    //            {
    //                Key = asset.Value["UIData"]["CollectionName"]["Key"].ToString(),
    //                SourceString = asset.Value["UIData"]["CollectionName"]["SourceString"].ToString()
    //            };
    //            localizationEntries.Add(entry);

    //            return localizationEntries;
    //        }
    //    }

    //    // If any part of the path is missing or null, return empty string
    //    return localizationEntries;
    //}

    public static string GetRelativePathWithoutExtension(string fullPath, string rootPath)
    {
        Uri fullPathUri = new(fullPath);
        Uri rootPathUri = new(rootPath);
        string relativePath = Uri.UnescapeDataString(rootPathUri.MakeRelativeUri(fullPathUri).ToString().Replace('\\', '/'));

        string directoryToRemove = "ExtractedAssets/";

        // Check if the relative path starts with the directory to remove
        if (relativePath.StartsWith(directoryToRemove, StringComparison.OrdinalIgnoreCase))
        {
            // Remove the directory and the following directory separator
            relativePath = relativePath[directoryToRemove.Length..];
        }

        // Strip extension from relative path
        string relativePathWithoutExtension = Path.ChangeExtension(relativePath, null);
        return relativePathWithoutExtension;
    }

    // Convert HTML tags using custom values
    public static string ConvertHTMLTags(string input)
    {
        string htmlTagConvertersFile = Path.Combine(GlobalVariables.rootDir, "Dependencies", "HelperComponents", "tagConverters.json");

        if (!File.Exists(htmlTagConvertersFile)) throw new Exception("Not found file with HTML Tag Converters.");

        string json = File.ReadAllText(htmlTagConvertersFile);
        TagConverters tagConverters = JsonConvert.DeserializeObject<TagConverters>(json) ?? throw new Exception("HTML Tag Converters file is empty.");

        foreach (var kvp in tagConverters.HTMLTagConverters)
        {
            if (input.Contains(kvp.Key))
            {
                input = input.Replace(kvp.Key, kvp.Value);
            }
        }

        return input;
    }

    public static string StripExtractedAssetsDir(string fullPath)
    {
        string pathToExtractedAssets = GlobalVariables.pathToExtractedAssets;

        // Check if fullPath starts with pathToExtractedAssets
        if (fullPath.StartsWith(pathToExtractedAssets, StringComparison.OrdinalIgnoreCase))
        {
            // Strip pathToExtractedAssets from fullPath
            string strippedPath = fullPath[pathToExtractedAssets.Length..];

            // Remove leading directory separator if present
            if (strippedPath.StartsWith(Path.DirectorySeparatorChar) || strippedPath.StartsWith(Path.AltDirectorySeparatorChar))
            {
                strippedPath = strippedPath[1..];
            }

            return strippedPath;
        }
        else
        {
            // Return fullPath unchanged if pathToExtractedAssets is not found
            return fullPath;
        }
    }

    public static string StripDynamicDirectory(string fullPath, string directoryRoot)
    {
        // Check if fullPath starts with directoryRoot
        if (fullPath.StartsWith(directoryRoot, StringComparison.OrdinalIgnoreCase))
        {
            // Strip directoryRoot from fullPath
            string strippedPath = fullPath[directoryRoot.Length..];

            // Remove leading directory separator if present
            if (strippedPath.StartsWith(Path.DirectorySeparatorChar) || strippedPath.StartsWith(Path.AltDirectorySeparatorChar))
            {
                strippedPath = strippedPath[1..];
            }

            return strippedPath;
        }
        else
        {
            // Return fullPath unchanged if directoryRoot is not found
            return fullPath;
        }
    }

    public static string StripRootDir(string fullPath) 
    {
        string rootDir = GlobalVariables.rootDir;

        // Check if fullPath starts with rootDir
        if (fullPath.StartsWith(rootDir, StringComparison.OrdinalIgnoreCase))
        {
            // Strip rootDir from fullPath
            string strippedPath = fullPath[rootDir.Length..];

            // Remove leading directory separator if present
            if (strippedPath.StartsWith(Path.DirectorySeparatorChar) || strippedPath.StartsWith(Path.AltDirectorySeparatorChar))
            {
                strippedPath = strippedPath[1..];
            }

            return strippedPath;
        }
        else
        {
            // Return fullPath unchanged if rootDir is not found
            return fullPath;
        }
    }

    public static string LangSplit(string input)
    {
        string[] parts = input.Split('_', '.');
        string result = parts[1];
        return result;
    }

    public static string DoubleDotsSplit(string input)
    {
        string[] parts = input.Split("::");
        string result = parts[1];

        return result;
    }

    [GeneratedRegex("VE_(.*)$")]
    private static partial Regex SplitVERegex();
    public static string StringSplitVE(string input)
    {
        // Split the input string using "VE_" as the separator
        Match match = SplitVERegex().Match(input);
        if (match.Success)
        {
            // Return the substring after "VE_"
            // In specific cases (ex. Camper) return custom string
            return match.Groups[1].Value switch
            {
                "Camper" => "Survivor",
                "Slasher" => "Killer",
                _ => match.Groups[1].Value,
            };
        }
        else
        {
            throw new ArgumentException("Input string is not in the expected format.");
        }
    }

    public static string AddRootDirectory(string path, string rootDirectory)
    {
        // Normalize the path separators to handle both '\' and '/'
        path = path.Replace('\\', '/');

        // Ensure the path does not start with a directory separator
        if (path.StartsWith('/') || path.StartsWith('\\'))
        {
            path = path[1..];
        }

        // Combine the root directory with the path, ensuring correct separators
        string fullPath = Path.Combine(rootDirectory, path);

        return fullPath;
    }

    private static bool IsInDBDCharactersDir(int characterIndex)
    {
        List<int> charactersSet = [268435464, 41, 42, 268435469, 268435466, 268435465];
        return charactersSet.Contains(characterIndex);
    }

    // I hate this
    public static string ModifyPath(string path, string replacement, bool isInDBDCharacters = false, int characterIndex = -1)
    {
        if (!isInDBDCharacters)
        {
            isInDBDCharacters = IsInDBDCharactersDir(characterIndex);
        }

        // Check if the delimiter exists in the original path
        if (!path.Contains('.'))
        {
            return path;
        }

        string[] pathParts = path.Split('.');
        int lastIndex = pathParts.Length - 1;

        // Replace the last part with the specified string
        pathParts[lastIndex] = replacement;
        string fixedPath = string.Join(".", pathParts);
        string modifiedPath = fixedPath;

        if (!path.Contains("/Game") && !path.Contains("DeadByDaylight") && !path.Contains("/Engine"))
        {
            int dynamicPartIndex = fixedPath.IndexOf('/', 1); // Start searching after the first slash

            if (dynamicPartIndex != -1)
            {
                // Insert "/Content/" after the dynamic part
                modifiedPath = fixedPath.Insert(dynamicPartIndex + 1, "Content/");
            }

            if (isInDBDCharacters)
            {
                modifiedPath = modifiedPath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                modifiedPath = Path.Combine("/DeadByDaylight/Plugins/Runtime/Bhvr/DBDCharacters", modifiedPath);
            }
            else
            {
                modifiedPath = modifiedPath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                modifiedPath = Path.Combine("/DeadByDaylight/Plugins/Runtime/Bhvr/DLC", modifiedPath);
            }
        }
        else
        {
            // Replace the specified part with the custom replacement
            modifiedPath = fixedPath.Replace("/Game", "/DeadByDaylight/Content");
        }

        // Remove double slashes (because BHVR isn't consistent)
        modifiedPath = modifiedPath.Replace("//", "/");
        modifiedPath = modifiedPath.Replace("\\", "/");

        return modifiedPath;
    }

    public static string SplitTextureName(string fullName)
    {
        const string prefix = "Texture2D ";

        if (fullName.StartsWith(prefix))
        {
            return fullName[prefix.Length..];
        }

        return fullName;
    }

    public static string ExtractStringInSingleQuotes(string input)
    {
        const string prefix = "MaterialInstanceConstant ";
        const string prefix2 = "Material ";

        int start = input.IndexOf('\'') + 1; // Find the index of the opening single quote
        int end = input.IndexOf('\'', start); // Find the index of the closing single quote
        if (start >= 0 && end > start)
        {
            return input[start..end];
        }
        else if (input.StartsWith(prefix))
        {
            // Extract the object name by removing the prefix
            return input[prefix.Length..];
        }
        else if (input.StartsWith(prefix2))
        {
            return input[prefix2.Length..];
        }
        else
        {
            return input;
        }
    }

    public static string ExtractObjectName(string fullName)
    {
        // Assuming the object name always starts with one of the specified prefixes
        const string prefix1 = "Material ";
        const string prefix2 = "MaterialInstanceConstant'";
        const string prefix3 = "Material'";

        if (fullName.StartsWith(prefix1))
        {
            return fullName[prefix1.Length..];
        }
        else if (fullName.StartsWith(prefix2))
        {
            int startIndex = prefix2.Length;
            int length = fullName.Length - startIndex - 1;
            return fullName.Substring(startIndex, length);
        }
        else if (fullName.StartsWith(prefix3))
        {
            int startIndex = prefix3.Length;
            int length = fullName.Length - startIndex - 1;
            return fullName.Substring(startIndex, length);
        }

        // If none of the prefixes are found, return the full name as is
        return fullName;
    }

    public static string RemoveDoubleSlashes(string path)
    {
        string replacedPath = path.Replace("//", "/");

        return replacedPath;
    }

    public static string GetSubstringAfterLastDot(string path)
    {
        int lastDotIndex = path.LastIndexOf('.');
        return lastDotIndex >= 0 ? path[(lastDotIndex + 1)..] : path;
    }

    public static string TransformImagePathSpecialPacks(string input)
    {
        // Define the regex pattern to match both formats
        string pattern = @"/Game/UI/UMGAssets/Icons/Banners/BundleBanners/(SpecialPack|ChapterBundles)/([^./]+)\.\w+";

        // Replace the matched pattern with the desired format
        string transformedString = Regex.Replace(input, pattern, "/images/UI/Icons/Banners/BundleBanners/$1/$2.png");

        return transformedString;
    }

    // BHVR uses codenames such as "TOME19", to make it consistent in all cases turn it into "Tome"
    public static string TomeToTitleCase(string input)
    {
        if (string.IsNullOrEmpty(input) || !input.StartsWith("tome", StringComparison.OrdinalIgnoreCase))
        {
            return input;
        }

        string firstChar = input[..1].ToUpper();
        string restOfChars = input[1..].ToLower();
        return firstChar + restOfChars;
    }

    public static (string version, string branch) SplitVersionAndBranch(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ArgumentException("Input cannot be null or empty", nameof(input));
        }

        var parts = input.Split('_');
        if (parts.Length != 2)
        {
            throw new ArgumentException("Input must contain exactly one underscore", nameof(input));
        }

        return (parts[0], parts[1]);
    }
}