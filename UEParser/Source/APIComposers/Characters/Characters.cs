using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UEParser.Models;
using UEParser.Utils;
using UEParser.ViewModels;
using UEParser.Parser;

namespace UEParser.APIComposers;

public class Characters
{
    private static readonly Dictionary<string, Dictionary<string, List<LocalizationEntry>>> localizationData = [];

    public static async Task InitializeCharactersDB()
    {
        await Task.Run(() =>
        {
            Dictionary<string, Character> parsedCharactersDB = [];

            LogsWindowViewModel.Instance.AddLog($"Starting parsing process..", Logger.LogTags.Info, Logger.ELogExtraTag.Characters);

            parsedCharactersDB = ParseCharacters(parsedCharactersDB);

            LogsWindowViewModel.Instance.AddLog($"Parsed total of {parsedCharactersDB.Count} items.", Logger.LogTags.Info, Logger.ELogExtraTag.Characters);

            ParseLocalizationAndSave(parsedCharactersDB);
        });
    }

    private static Dictionary<string, Character> ParseCharacters(Dictionary<string, Character> parsedCharactersDB)
    {
        string[] filePaths = Helpers.FindFilePathsInExtractedAssetsCaseInsensitive("CharacterDescriptionDB.json");

        foreach (string filePath in filePaths)
        {
            string packagePath = StringUtils.StripExtractedAssetsDir(filePath);
            LogsWindowViewModel.Instance.AddLog($"Processing: {packagePath}", Logger.LogTags.Info, Logger.ELogExtraTag.Characters);

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

                Dictionary<string, List<LocalizationEntry>> localizationModel = new()
                {
                    ["Name"] = [
                    new LocalizationEntry
                        {
                            Key = displayName,
                            SourceString = item.Value["DisplayName"]["SourceString"]
                        }
                    ],
                    ["BackStory"] = [
                    new LocalizationEntry
                        {
                            Key = backStory,
                            SourceString = item.Value["Backstory"]["SourceString"]
                        }
                    ],
                    ["Biography"] = [
                        new LocalizationEntry
                        {
                            Key = biography,
                            SourceString = item.Value["Biography"]["SourceString"]
                        }
                    ]
                };

                localizationData.TryAdd(characterIndex, localizationModel);

                Character model = new()
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

    private static void ParseLocalizationAndSave(Dictionary<string, Character> parsedCharactersDB)
    {
        LogsWindowViewModel.Instance.AddLog($"Starting localization process..", Logger.LogTags.Info, Logger.ELogExtraTag.Characters);

        string[] filePaths = Directory.GetFiles(Path.Combine(GlobalVariables.rootDir, "Dependencies", "Locres"), "*.json", SearchOption.TopDirectoryOnly);

        foreach (string filePath in filePaths)
        {
            string jsonString = File.ReadAllText(filePath);
            string fileName = Path.GetFileName(filePath);

            string langKey = StringUtils.LangSplit(fileName);

            Dictionary<string, string> languageKeys = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString) ?? throw new Exception($"Failed to load following locres file: {langKey}.");

            var objectString = JsonConvert.SerializeObject(parsedCharactersDB);
            Dictionary<string, Character> localizedCharactersDB = JsonConvert.DeserializeObject<Dictionary<string, Character>>(objectString) ?? [];

            Helpers.LocalizeDB(localizedCharactersDB, localizationData, languageKeys, langKey);

            string outputPath = Path.Combine(GlobalVariables.pathToParsedData, GlobalVariables.versionWithBranch, langKey, "Characters.json");

            FileWriter.SaveParsedDB(localizedCharactersDB, outputPath, Logger.ELogExtraTag.Characters);
        }
    }
}
