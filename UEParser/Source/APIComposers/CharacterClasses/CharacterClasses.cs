using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UEParser.Models;
using UEParser.Parser;
using UEParser.Utils;
using UEParser.ViewModels;

namespace UEParser.APIComposers;

public class CharacterClasses
{
    private static readonly Dictionary<string, Dictionary<string, List<LocalizationEntry>>> LocalizationData = [];

    public static async Task InitializeCharacterClassesDb(CancellationToken token)
    {
        await Task.Run(() =>
        {
            Dictionary<string, CharacterClass> parsedCharacterClassesDb = [];

            LogsWindowViewModel.Instance.AddLog($"Starting parsing process..", Logger.LogTags.Info, Logger.ELogExtraTag.CharacterClasses);

            ParseCharacterClasses(parsedCharacterClassesDb, token);

            LogsWindowViewModel.Instance.AddLog($"Parsed total of {parsedCharacterClassesDb.Count} items.", Logger.LogTags.Info, Logger.ELogExtraTag.CharacterClasses);

            ParseLocalizationAndSave(parsedCharacterClassesDb, token);
        }, token);
    }

    private static void ParseCharacterClasses(Dictionary<string, CharacterClass> parsedCharacterClassesDb, CancellationToken token)
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
                token.ThrowIfCancellationRequested();

                string characterClassId = item.Name;

                JArray skills = item.Value["Skills"];

                string roleRaw = item.Value["Role"];
                string role = StringUtils.StringSplitVe(roleRaw);

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

                parsedCharacterClassesDb.Add(characterClassId, model);
            }
        }
    }

    private static void ParseLocalizationAndSave(Dictionary<string, CharacterClass> parsedCharacterClassesDb, CancellationToken token)
    {
        LogsWindowViewModel.Instance.AddLog($"Starting localization process..", Logger.LogTags.Info, Logger.ELogExtraTag.CharacterClasses);

        string[] filePaths = Directory.GetFiles(Path.Combine(GlobalVariables.RootDir, "Dependencies", "Locres"), "*.json", SearchOption.TopDirectoryOnly);

        foreach (string filePath in filePaths)
        {
            token.ThrowIfCancellationRequested();

            string jsonString = File.ReadAllText(filePath);
            string fileName = Path.GetFileName(filePath);
            string langKey = StringUtils.LangSplit(fileName);

            Dictionary<string, string> languageKeys = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString) ?? throw new Exception($"Failed to load following locres file: {langKey}.");

            var objectString = JsonConvert.SerializeObject(parsedCharacterClassesDb);
            Dictionary<string, CharacterClass> localizedCharacterClassesDb = JsonConvert.DeserializeObject<Dictionary<string, CharacterClass>>(objectString) ?? [];

            Helpers.LocalizeDb(localizedCharacterClassesDb, LocalizationData, languageKeys, langKey);

            string outputPath = Path.Combine(GlobalVariables.PathToParsedData, GlobalVariables.VersionWithBranch, langKey, "CharacterClasses.json");

            FileWriter.SaveParsedDb(localizedCharacterClassesDb, outputPath, Logger.ELogExtraTag.CharacterClasses);
        }
    }
}