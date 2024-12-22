using System;
using System.IO;
using System.Collections.Generic;
using UEParser.Models;
using UEParser.Utils;
using UEParser.APIComposers;

namespace UEParser.Services;

internal class DataInitializer
{
    private static dynamic _catalogData = new object();
    private static Dictionary<string, Rift> _riftData = [];
    private static Dictionary<string, int> _catalogDictionary = [];
    private static Dictionary<string, Character> _characterData = [];
    private static Dictionary<string, string> _customizationCategories = [];

    public static dynamic CatalogData => _catalogData;
    public static Dictionary<string, Rift> RiftData => _riftData;
    public static Dictionary<string, int> CatalogDictionary => _catalogDictionary;
    public static Dictionary<string, Character> CharacterData => _characterData;
    public static Dictionary<string, string> CustomizationCategories => _customizationCategories;

    public enum DataToLoad
    {
        None = 0,
        Catalog = 1,
        CatalogDictionary = 2,
        Rifts = 3,
        Characters = 4,
        CustomizationCategories = 5
    }

    public static void InitializeData(IEnumerable<DataToLoad> dataToLoad)
    {
        foreach (var data in dataToLoad)
        {
            switch (data)
            {
                case DataToLoad.Catalog:
                    _catalogData = FileUtils.LoadDynamicJson(
                        Path.Combine(GlobalVariables.PathToKraken, GlobalVariables.VersionWithBranch, "CDN", "catalog.json")
                    ) ?? throw new Exception("Failed to load catalog data.");
                    break;

                case DataToLoad.Rifts:
                    _riftData = FileUtils.LoadJsonFileWithTypeCheck<Dictionary<string, Rift>>(
                        Path.Combine(GlobalVariables.PathToParsedData, GlobalVariables.VersionWithBranch, "en", "Rifts.json")
                    ) ?? throw new Exception("Failed to load rift data.");
                    break;

                case DataToLoad.CatalogDictionary:
                    _catalogDictionary = CosmeticUtils.CreateCatalogDictionary(CatalogData);
                    break;

                case DataToLoad.Characters:
                    _characterData = FileUtils.LoadJsonFileWithTypeCheck<Dictionary<string, Character>>(
                        Path.Combine(GlobalVariables.PathToParsedData, GlobalVariables.VersionWithBranch, "en", "Characters.json")
                    ) ?? throw new Exception("Failed to load characters data.");
                    break;

                case DataToLoad.CustomizationCategories:
                    _customizationCategories = FileUtils.LoadJsonFileWithTypeCheck<Dictionary<string, string>>(
                        Path.Combine(GlobalVariables.RootDir, "Dependencies", "HelperComponents", "customizationCategories.json")
                    ) ?? throw new Exception("Failed to load customization categories.");
                    break;

                default:
                    throw new ArgumentOutOfRangeException($"Data type {data} is not supported.");
            }
        }
    }

}