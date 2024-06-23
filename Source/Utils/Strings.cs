using Newtonsoft.Json.Linq;
using System;
using System.IO;

namespace UEParser.Utils;

public class StringUtils
{
    public static string ChangeToNeteasePath(string path)
    {
        string pathWithoutGame = path.Replace("/Game/", "");
        if (pathWithoutGame.StartsWith('/'))
        {
            pathWithoutGame = pathWithoutGame[1..];
        }
        string modifiedPath = Path.Combine("/netease", "assets", pathWithoutGame);
        string unifiedSlashes = modifiedPath.Replace("\\", "/");

        return unifiedSlashes;
    }

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

        string directoryToRemove = "Assets/";

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
}