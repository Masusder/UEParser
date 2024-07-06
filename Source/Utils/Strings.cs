﻿using Newtonsoft.Json.Linq;
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

    public static string GetCollectionName(dynamic asset)
    {
        // Check if "CollectionName" exists in the root of the asset
        if (asset.Value["CollectionName"] != null)
        {
            // Check if "LocalizedString" exists within "CollectionName"
            if (asset.Value["CollectionName"]["LocalizedString"] != null)
            {
                return asset.Value["CollectionName"]["LocalizedString"].ToString();
            }
        }

        // Check if "UIData" exists
        if (asset.Value["UIData"] != null && asset.Value["UIData"]["CollectionName"] != null)
        {
            // Check if "LocalizedString" exists within nested "CollectionName"
            if (asset.Value["UIData"]["CollectionName"]["LocalizedString"] != null)
            {
                return asset.Value["UIData"]["CollectionName"]["LocalizedString"].ToString();
            }
        }

        // If any part of the path is missing or null, return empty string
        return "";
    }

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

        // Combine the images directory with the path, ensuring correct separators
        string fullPath = Path.Combine(rootDirectory, path);

        return fullPath;
    }

    private static bool IsInDBDCharactersDir(int characterIndex)
    {
        HashSet<int> charactersSet = [268435464, 41];
        return charactersSet.Contains(characterIndex);
    }

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
                modifiedPath = Path.Combine("/DeadByDaylight/Plugins/Runtime/Bhvr/DBDCharacters", modifiedPath);
            }
            else
            {
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

        return modifiedPath;
    }
}