using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UEParser.Utils;
using UEParser.ViewModels;
using System.IO;
using Newtonsoft.Json;
using UEParser.Parser;

namespace UEParser.APIComposers;

public class Characters
{
    private static readonly Dictionary<string, Dictionary<string, Models.LocalizationEntry>> localizationData = [];

    public static async Task InitializeCharactersDB()
    {
        await Task.Run(() =>
        {
            Dictionary<string, Models.Character> parsedCharactersDB = [];

            LogsWindowViewModel.Instance.AddLog($"[Characters] Starting parsing process..", Logger.LogTags.Info);

            parsedCharactersDB = ParseCharacters(parsedCharactersDB);

            ParseLocalizationAndSave(parsedCharactersDB);
        });
    }

    private static Dictionary<string, Models.Character> ParseCharacters(Dictionary<string, Models.Character> parsedCharactersDB)
    {
        string[] filePaths = Helpers.FindFilePathsInExtractedAssetsCaseInsensitive("CharacterDescriptionDB.json");

        foreach (string filePath in filePaths)
        {
            string packagePath = StringUtils.StripExtractedAssetsDir(filePath);
            LogsWindowViewModel.Instance.AddLog($"[Rifts] Processing: {packagePath}", Logger.LogTags.Info);

            var assetItems = FileUtils.LoadDynamicJson(filePath);
            if ((assetItems?[0]?["Rows"]) == null)
            {
                continue;
            }

            foreach (var item in assetItems[0]["Rows"])
            {
                string characterIndex = item.Name;
                if (characterIndex == "-1")
                {
                    continue;
                }

                string roleInput = item.Value["Role"];
                string roleOutput = StringUtils.StringSplitVE(roleInput);

                string genderInput = item.Value["Gender"];
                string genderOutput = StringUtils.StringSplitVE(genderInput);

                string difficultyInput = item.Value["Difficulty"];
                string difficultyOutput = StringUtils.StringSplitVE(difficultyInput);

                string iconFilePath = item.Value["IconFilePath"];
                string iconFilePathFixed = StringUtils.AddRootDirectory(iconFilePath, "/images/");

                string backgroundImagePath = item.Value["BackgroundImagePath"];
                string backgroundImagePathFixed = StringUtils.AddRootDirectory(backgroundImagePath, "/images/");

                string characterId = item.Value["CharacterId"];

                string displayName = item.Value["DisplayName"]["Key"];
                string backStory = item.Value["Backstory"]["Key"];
                string biography = item.Value["Biography"]["Key"];

                Dictionary<string, Models.LocalizationEntry> localizationModel = new()
                {
                    ["Name"] = new Models.LocalizationEntry
                    {
                        Key = displayName,
                        SourceString = item.Value["DisplayName"]["SourceString"]
                    },
                    ["BackStory"] = new Models.LocalizationEntry
                    {
                        Key = backStory,
                        SourceString = item.Value["Backstory"]["SourceString"]
                    },
                    ["Biography"] = new Models.LocalizationEntry
                    {
                        Key = biography,
                        SourceString = item.Value["Biography"]["SourceString"]
                    }
                };

                localizationData.TryAdd(characterIndex, localizationModel);

                Models.Character model = new()
                {
                    Name = displayName,
                    Role = roleOutput,
                    Gender = genderOutput,
                    ParentItem = item.Value["DefaultItem"],
                    DLC = item.Value["ChapterDlcId"],
                    Difficulty = difficultyOutput,
                    BackStory = backStory,
                    Biography = biography,
                    IconFilePath = iconFilePathFixed,
                    BackgroundImagePath = backgroundImagePathFixed,
                    Id = characterId
                };

                parsedCharactersDB[characterIndex] = model;
            }
        }

        return parsedCharactersDB;
    }

    private static void ParseLocalizationAndSave(Dictionary<string, Models.Character> parsedCharactersDB)
    {
        LogsWindowViewModel.Instance.AddLog($"[Characters] Starting localization process..", Logger.LogTags.Info);

        string[] filePaths = Directory.GetFiles(Path.Combine(GlobalVariables.rootDir, "Dependencies", "Locres"), "*.json", SearchOption.TopDirectoryOnly);

        foreach (string filePath in filePaths)
        {
            string jsonString = File.ReadAllText(filePath);
            string fileName = Path.GetFileName(filePath);

            string langKey = StringUtils.LangSplit(fileName);

            Dictionary<string, string> languageKeys = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString) ?? throw new Exception($"Failed to load following locres file: {langKey}.");

            var objectString = JsonConvert.SerializeObject(parsedCharactersDB);
            Dictionary<string, Models.Character> localizedCharactersDB = JsonConvert.DeserializeObject<Dictionary<string, Models.Character>>(objectString) ?? [];

            foreach (var item in localizedCharactersDB)
            {
                string characterIndex = item.Key;
                var localizationDataEntry = localizationData[characterIndex];

                foreach (var entry in localizationDataEntry)
                {
                    try
                    {
                        string localizedString;
                        if (languageKeys.TryGetValue(entry.Value.Key, out string? langValue))
                        {
                            localizedString = langValue;
                        }
                        else
                        {
                            LogsWindowViewModel.Instance.AddLog($"Missing localization string -> Property: '{entry.Key}', LangKey: '{langKey}', RowId: '{characterIndex}', FallbackString: '{entry.Value.SourceString}'", Logger.LogTags.Warning);
                            localizedString = entry.Value.SourceString;
                        }

                        var propertyInfo = typeof(Models.Character).GetProperty(entry.Key);
                        propertyInfo?.SetValue(item.Value, localizedString);
                    }
                    catch (Exception ex)
                    {
                        LogsWindowViewModel.Instance.AddLog($"Missing localization string -> Property: '{entry.Key}', LangKey: '{langKey}', RowId: '{characterIndex}', FallbackString: '{entry.Value.SourceString}' <- {ex}", Logger.LogTags.Warning);
                    }
                }
            }

            string outputPath = Path.Combine(GlobalVariables.rootDir, "Output", "ParsedData", GlobalVariables.versionWithBranch, langKey, "Characters.json");

            FileWriter.SaveParsedDB(localizedCharactersDB, outputPath, "Characters");
        }
    }
}
