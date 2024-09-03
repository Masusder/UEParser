using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UEParser.Models;
using UEParser.Parser;
using UEParser.Utils;
using UEParser.ViewModels;

namespace UEParser.APIComposers;

public class CharacterClasses
{
    private static readonly Dictionary<string, Dictionary<string, List<LocalizationEntry>>> LocalizationData = [];

    public static async Task InitializeCharacterClassesDB()
    {
        await Task.Run(() =>
        {
            Dictionary<string, CharacterClass> parsedCharacterClassesDB = [];

            LogsWindowViewModel.Instance.AddLog($"Starting parsing process..", Logger.LogTags.Info, Logger.ELogExtraTag.CharacterClasses);

            ParseCharacterClasses(parsedCharacterClassesDB);

            LogsWindowViewModel.Instance.AddLog($"Parsed total of {parsedCharacterClassesDB.Count} items.", Logger.LogTags.Info, Logger.ELogExtraTag.CharacterClasses);

            ParseLocalizationAndSave(parsedCharacterClassesDB);
        });
    }

    private static void ParseCharacterClasses(Dictionary<string, CharacterClass> parsedCharacterClassesDB)
    {
        string[] filePaths = Helpers.FindFilePathsInExtractedAssetsCaseInsensitive("CharacterClassDB.json");

        foreach (string filePath in filePaths)
        {
            string packagePath = StringUtils.StripExtractedAssetsDir(filePath);
            LogsWindowViewModel.Instance.AddLog($"Processing: {packagePath}", Logger.LogTags.Info, Logger.ELogExtraTag.CharacterClasses);

            var assetItems = FileUtils.LoadDynamicJson(filePath);

            if ((assetItems?[0]?["Rows"]) == null) continue;

            foreach (var item in assetItems[0]["Rows"])
            {
                string characterClassId = item.Name;

                JArray skills = item.Value["Skills"];

                string roleRaw = item.Value["Role"];
                string role = StringUtils.StringSplitVE(roleRaw);

                string iconPathRaw = item.Value["UIData"]["IconFilePathList"][0];
                string iconPath = StringUtils.AddRootDirectory(iconPathRaw, "/images/");

                Dictionary<string, List<LocalizationEntry>> localizationModel = new()
                {
                    ["Name"] = [
                    new LocalizationEntry
                        {
                            Key = item.Value["UIData"]["DisplayName"]["Key"],
                            SourceString = item.Value["UIData"]["DisplayName"]["SourceString"]
                        }
                    ],
                    ["Description"] = [
                    new LocalizationEntry
                        {
                            Key = item.Value["UIData"]["Description"]["Key"],
                            SourceString = item.Value["UIData"]["Description"]["SourceString"]
                        }
                    ]
                };

                LocalizationData.TryAdd(characterClassId, localizationModel);

                CharacterClass model = new()
                {
                    Skills = skills,
                    Role = role,
                    Name = "",
                    Description = "",
                    IconPath = iconPath
                };

                parsedCharacterClassesDB.Add(characterClassId, model);
            }
        }
    }

    private static void ParseLocalizationAndSave(Dictionary<string, CharacterClass> parsedCharacterClassesDB)
    {
        LogsWindowViewModel.Instance.AddLog($"Starting localization process..", Logger.LogTags.Info, Logger.ELogExtraTag.CharacterClasses);

        string[] filePaths = Directory.GetFiles(Path.Combine(GlobalVariables.rootDir, "Dependencies", "Locres"), "*.json", SearchOption.TopDirectoryOnly);

        foreach (string filePath in filePaths)
        {
            string jsonString = File.ReadAllText(filePath);
            string fileName = Path.GetFileName(filePath);

            string langKey = StringUtils.LangSplit(fileName);

            Dictionary<string, string> languageKeys = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString) ?? throw new Exception($"Failed to load following locres file: {langKey}.");

            var objectString = JsonConvert.SerializeObject(parsedCharacterClassesDB);
            Dictionary<string, CharacterClass> localizedCharacterClassesDB = JsonConvert.DeserializeObject<Dictionary<string, CharacterClass>>(objectString) ?? [];

            Helpers.LocalizeDB(localizedCharacterClassesDB, LocalizationData, languageKeys, langKey);

            string outputPath = Path.Combine(GlobalVariables.pathToParsedData, GlobalVariables.versionWithBranch, langKey, "CharacterClasses.json");

            FileWriter.SaveParsedDB(localizedCharacterClassesDB, outputPath, "CharacterClasses");
        }
    }
}