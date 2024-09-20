using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UEParser.Models;
using UEParser.Parser;
using UEParser.ViewModels;
using UEParser.Utils;

namespace UEParser.APIComposers;

// Works but is outdated, should be reworked
public class Tomes
{
    private static readonly Dictionary<string, Dictionary<string, List<LocalizationEntry>>> LocalizationData = [];
    private static readonly Dictionary<string, object> CosmeticsData = FileUtils.LoadJsonFileWithTypeCheck<Dictionary<string, object>>(Path.Combine(GlobalVariables.pathToParsedData, GlobalVariables.versionWithBranch, "en", "Cosmetics.json")) ?? throw new Exception("Failed to load cosmetics data.");
    private static readonly Dictionary<string, object> QuestNodeDatabase = FileUtils.LoadJsonFileWithTypeCheck<Dictionary<string, object>>(Path.Combine(GlobalVariables.rootDir, "Dependencies", "HelperComponents", "questNodeDatabase.json")) ?? throw new Exception("Failed to load quest node database.");
    private static readonly Dictionary<string, object> QuestObjectiveDatabase = FileUtils.LoadJsonFileWithTypeCheck<Dictionary<string, object>>(Path.Combine(GlobalVariables.rootDir, "Dependencies", "HelperComponents", "questObjectiveDatabase.json")) ?? throw new Exception("Failed to load quest objective database.");
    private static readonly Dictionary<string, int> CharacterIds = FileUtils.LoadJsonFileWithTypeCheck<Dictionary<string, int>>(Path.Combine(GlobalVariables.rootDir, "Dependencies", "HelperComponents", "characterIds.json")) ?? throw new Exception("Failed to load Characters ID table.");
    private static readonly TagConverters HTMLTagConverters = FileUtils.LoadJsonFileWithTypeCheck<TagConverters>(Path.Combine(GlobalVariables.rootDir, "Dependencies", "HelperComponents", "tagConverters.json")) ?? throw new Exception("Failed to load html tag converters.");

    public static async Task InitializeTomesDB()
    {
        await Task.Run(() =>
        {
            Dictionary<string, Tome> parsedTomesDB = [];

            LogsWindowViewModel.Instance.AddLog($"Starting parsing process..", Logger.LogTags.Info, Logger.ELogExtraTag.Tomes);

            ParseTomes(parsedTomesDB);

            LogsWindowViewModel.Instance.AddLog($"Parsed total of {parsedTomesDB.Count} items.", Logger.LogTags.Info, Logger.ELogExtraTag.Tomes);

            ParseLocalizationAndSave(parsedTomesDB);
        });
    }

    private static void ParseTomes(Dictionary<string, Tome> parsedTomesDB)
    {
        string[] filePaths = Helpers.FindFilePathsInExtractedAssetsCaseInsensitive("ArchiveDB.json");

        foreach (string filePath in filePaths)
        {
            // Duplicated Tome, grrrrrrrrr
            if (filePath.Contains(@"DeadByDaylight\Content\Data\Events\Bacon\ArchiveDB.json"))
            {
                continue;
            }

            string packagePath = StringUtils.StripExtractedAssetsDir(filePath);
            LogsWindowViewModel.Instance.AddLog($"Processing: {packagePath}", Logger.LogTags.Info, Logger.ELogExtraTag.Tomes);

            var assetItems = FileUtils.LoadDynamicJson(filePath);

            if ((assetItems?[0]?["Rows"]) == null)
            {
                continue;
            }

            foreach (var item in assetItems[0]["Rows"])
            {
                string tomeId = item.Name;
                string tomeIdTitleCase = StringUtils.TomeToTitleCase(tomeId);

                string pathToTomeFile = Path.Combine(GlobalVariables.pathToKraken, GlobalVariables.versionWithBranch, "CDN", "Tomes", $"{tomeIdTitleCase}.json");
                if (!File.Exists(pathToTomeFile))
                {
                    LogsWindowViewModel.Instance.AddLog("Not found Tome data. Make sure to update API first.", Logger.LogTags.Error);
                    LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
                    continue;
                }

                var tomeData = FileUtils.LoadDynamicJson(pathToTomeFile);

                Dictionary<string, List<LocalizationEntry>> localizationModel = [];

                List<Level> levels = CreateLevels(tomeIdTitleCase, tomeData, localizationModel);

                localizationModel["Name"] = [
                    new LocalizationEntry
                    {
                        Key = item.Value["Title"]["Key"],
                        SourceString = item.Value["Title"]["SourceString"]
                    }
                ];

                if (item.Value["PurchasePassPopupMessage"]["Key"] != null)
                {
                    localizationModel["Description"] = [
                        new LocalizationEntry
                    {
                        Key = item.Value["PurchasePassPopupMessage"]["Key"],
                        SourceString = item.Value["PurchasePassPopupMessage"]["SourceString"]
                    }
                    ];
                }

                LocalizationData.TryAdd(tomeIdTitleCase, localizationModel);

                Tome model = new()
                {
                    Name = "",
                    Description = "",
                    Levels = levels,
                    RiftID = tomeData.GetValue(tomeId, StringComparison.OrdinalIgnoreCase)["riftId"],
                    NewTomePopup = tomeData.GetValue(tomeId, StringComparison.OrdinalIgnoreCase)["newTomePopup"]
                };

                parsedTomesDB.Add(tomeIdTitleCase, model);
            }
        }
    }

    private static List<Level> CreateLevels(string tomeId, dynamic tomeData, Dictionary<string, List<LocalizationEntry>> localizationModel)
    {
        var levels = new List<Level>();
        for (int levelIndex = 0; levelIndex < tomeData.GetValue(tomeId, StringComparison.OrdinalIgnoreCase)["level"].Count; levelIndex++)
        {
            string? endNodeRewardIconPath = null;

            for (int typeIndex = 0; typeIndex < tomeData.GetValue(tomeId, StringComparison.OrdinalIgnoreCase)["level"][levelIndex]["endNodeRewards"].Count; typeIndex++)
            {
                if (tomeData.GetValue(tomeId, StringComparison.OrdinalIgnoreCase)["level"][levelIndex]["endNodeRewards"][typeIndex]["type"] == "inventory")
                {
                    string endNodeRewardId = tomeData.GetValue(tomeId, StringComparison.OrdinalIgnoreCase)["level"][levelIndex]["endNodeRewards"][typeIndex]["id"];

                    if (CosmeticsData.TryGetValue(endNodeRewardId, out dynamic? value))
                    {
                       endNodeRewardIconPath = value["IconFilePathList"]; 
                    }
                    else
                    {
                        LogsWindowViewModel.Instance.AddLog($"End node reward '{endNodeRewardId}' was not present in Cosmetics dictionary.", Logger.LogTags.Error);
                        LogsWindowViewModel.Instance.AddLog("You can try to fix the issue by parsing new Cosmetics database.", Logger.LogTags.Error);
                        LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
                    }
                }
            }

            Dictionary<string, Node> nodes = CreateNodes(tomeId, levelIndex, tomeData, localizationModel);

            Level model = new()
            {
                Nodes = nodes,
                StartDate = tomeData.GetValue(tomeId, StringComparison.OrdinalIgnoreCase)["level"][levelIndex]["startDate"],
                StartNodes = tomeData.GetValue(tomeId, StringComparison.OrdinalIgnoreCase)["level"][levelIndex]["start"],
                EndNodes = tomeData.GetValue(tomeId, StringComparison.OrdinalIgnoreCase)["level"][levelIndex]["end"],
                EndNodeReward = endNodeRewardIconPath
            };

            levels.Add(model);
        }

        return levels;
    }

    private static Dictionary<string, Node> CreateNodes(string tomeId, int levelIndex, dynamic tomeData, Dictionary<string, List<LocalizationEntry>> localizationModel)
    {
        Dictionary<string, Node> finalOutput = [];
        foreach (var node in tomeData.GetValue(tomeId, StringComparison.OrdinalIgnoreCase)["level"][levelIndex]["nodes"])
        {
            string? questId;
            Dictionary<string, double> coordinates = TomeUtils.CastCoordinates(node);
            JArray nodeNeighbors = node.Value["neighbors"];
            string nodeType = node.Value["nodeType"];
            JArray journal = node.Value["journal"];
            JArray? reward = null;

            if (node.Value.ContainsKey("objectives"))
            {
                JObject objectivesValue = node.Value["objectives"];
                questId = objectivesValue.Properties().Select(p => p.Name).FirstOrDefault();
            }
            else
            {
                questId = node.Value["clientInfoId"];
            }

            if (questId == null) throw new Exception("Not found questId.");

            reward = questId switch
            {
                "End" => (JArray)tomeData.GetValue(tomeId, StringComparison.OrdinalIgnoreCase)["level"][levelIndex]["endNodeRewards"],
                _ => (JArray)node.Value["reward"],
            };

            JArray descriptionsParams = TomeUtils.DescriptionParameters(node, questId, QuestObjectiveDatabase);
            NodeData nodeData = ReturnNodeData(tomeId, levelIndex, node.Name, tomeData, questId, localizationModel);

            Node model = new()
            {
                QuestID = questId,
                Coordinates = coordinates,
                Neighbors = nodeNeighbors,
                NodeType = nodeType,
                Journals = journal,
                // IsCommunityChallenge = false,
                // CommunityProgression = "",
                Name = nodeData.NodeName,
                Description = nodeData.ObjectiveDescription,
                DescriptionParams = descriptionsParams,
                RulesDescription = nodeData.RulesDescription,
                PlayerRole = nodeData.PlayerRole,
                IconPath = nodeData.IconPath,
                Reward = reward
            };

            finalOutput.Add(node.Name, model);
        }

        LocalizationData.TryAdd(tomeId, localizationModel);

        return finalOutput;
    }

    private static NodeData ReturnNodeData(string tomeId, int levelIndex, string nodeId, dynamic tomesJson, string questIdString, Dictionary<string, List<LocalizationEntry>> localizationModel)
    {
        string clientQuestIdJValue = tomesJson.GetValue(tomeId, StringComparison.OrdinalIgnoreCase)["level"][levelIndex]["nodes"][nodeId]["clientInfoId"];
        string clientQuestId = clientQuestIdJValue.ToLower();

        string questId = questIdString.ToLower();

        QuestNodeDatabase.TryGetValue(clientQuestId, out dynamic? clientValue);

        if (clientValue == null) throw new Exception("Node data client value is null.");

        string nodeNameKey = clientValue["DisplayName"]["Key"];
        string nodeNameSourceString = clientValue["DisplayName"]["SourceString"];

        string iconPathRaw = clientValue["IconPath"];
        string iconPath = StringUtils.AddRootDirectory(iconPathRaw, "/images/");

        string playerRoleRaw = clientValue["PlayerRole"];
        string playerRole = StringUtils.StringSplitVE(playerRoleRaw);

        string? objectiveDescriptionKey;
        string? objectiveDescriptionSourceString;

        string? rulesDescriptionKey = null;
        string? rulesDescriptionSourceString = null;
        if (QuestObjectiveDatabase.TryGetValue(questId, out dynamic? value))
        {
            objectiveDescriptionKey = value["Description"]["Key"];
            objectiveDescriptionSourceString = value["Description"]["SourceString"];

            rulesDescriptionKey = value["RulesDescription"]["Key"];
            rulesDescriptionSourceString = value["RulesDescription"]["SourceString"];
        }
        else
        {
            QuestNodeDatabase.TryGetValue(questId, out dynamic? objValue);

            // May I ask BHVR why descriptions for some nodes are split between two databases?
            objectiveDescriptionKey = objValue?["Description"]["Key"];
            objectiveDescriptionSourceString = objValue?["Description"]["SourceString"];
        }

        // Add custom image for 'end' and 'start' nodes
        // For Reward set icon path for cosmetic icon
        if (clientQuestId == "end" || clientQuestId == "start")
        {
            iconPath = "/images/Archives/StartEndNode.png";
        }
        else if (clientQuestId == "reward")
        {
            //nodeNameKey = "Reward";
            for (int rewardIndex = 0; rewardIndex < tomesJson.GetValue(tomeId, StringComparison.OrdinalIgnoreCase)["level"][levelIndex]["nodes"][nodeId]["reward"].Count; rewardIndex++)
            {
                string rewardId = tomesJson.GetValue(tomeId, StringComparison.OrdinalIgnoreCase)["level"][levelIndex]["nodes"][nodeId]["reward"][rewardIndex]["id"];

                CosmeticsData.TryGetValue(rewardId, out dynamic? cosmeticValue);

                if (cosmeticValue == null) throw new Exception("Not found rewardId in cosmetics data.");

                iconPath = cosmeticValue["IconFilePathList"];
            }
        }

        string localizationRulesDescriptionString = $"Levels.{levelIndex}.Nodes.{nodeId}.RulesDescription";
        string localizationNameString = $"Levels.{levelIndex}.Nodes.{nodeId}.Name";
        string localizationDescriptionString = $"Levels.{levelIndex}.Nodes.{nodeId}.Description";

        if (!string.IsNullOrEmpty(rulesDescriptionKey) && !string.IsNullOrEmpty(rulesDescriptionSourceString))
        {
            localizationModel[localizationRulesDescriptionString] =
            [
                new LocalizationEntry
                {
                    Key = rulesDescriptionKey,
                    SourceString = rulesDescriptionSourceString
                }
            ];
        }

        if (!string.IsNullOrEmpty(nodeNameKey) && !string.IsNullOrEmpty(nodeNameSourceString))
        {
            localizationModel[localizationNameString] =
            [
                new LocalizationEntry
                {
                    Key = nodeNameKey,
                    SourceString = nodeNameSourceString
                }
            ];
        }

        if (!string.IsNullOrEmpty(objectiveDescriptionKey) && !string.IsNullOrEmpty(objectiveDescriptionSourceString))
        {
            localizationModel[localizationDescriptionString] =
            [
                new LocalizationEntry
                {
                    Key = objectiveDescriptionKey,
                    SourceString = objectiveDescriptionSourceString
                }
            ];
        }

        NodeData nodeModel = new()
        {
            NodeName = "",
            IconPath = iconPath,
            ObjectiveDescription = "",
            PlayerRole = playerRole,
            RulesDescription = ""
        };

        return nodeModel;
    }

    private static void ParseLocalizationAndSave(Dictionary<string, Tome> parsedTomesDB)
    {
        LogsWindowViewModel.Instance.AddLog($"Starting localization process..", Logger.LogTags.Info, Logger.ELogExtraTag.Tomes);

        string[] filePaths = Directory.GetFiles(Path.Combine(GlobalVariables.rootDir, "Dependencies", "Locres"), "*.json", SearchOption.TopDirectoryOnly);

        foreach (string filePath in filePaths)
        {
            string jsonString = File.ReadAllText(filePath);
            string fileName = Path.GetFileName(filePath);

            string langKey = StringUtils.LangSplit(fileName);

            Dictionary<string, string> languageKeys = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString) ?? throw new Exception($"Failed to load following locres file: {langKey}.");

            var objectString = JsonConvert.SerializeObject(parsedTomesDB);
            Dictionary<string, Tome> localizedTomesDB = JsonConvert.DeserializeObject<Dictionary<string, Tome>>(objectString) ?? [];

            Helpers.LocalizeDB(localizedTomesDB, LocalizationData, languageKeys, langKey);

            var charactersData = FileUtils.LoadJsonFileWithTypeCheck<Dictionary<string, Character>>(Path.Combine(GlobalVariables.pathToParsedData, GlobalVariables.versionWithBranch, langKey, "Characters.json"));
            var perksData = FileUtils.LoadJsonFileWithTypeCheck<Dictionary<string, Perk>>(Path.Combine(GlobalVariables.pathToParsedData, GlobalVariables.versionWithBranch, langKey, "Perks.json"));
            var characterClassesData = FileUtils.LoadJsonFileWithTypeCheck<Dictionary<string, CharacterClass>>(Path.Combine(GlobalVariables.pathToParsedData, GlobalVariables.versionWithBranch, langKey, "CharacterClasses.json"));

            TomeUtils.FormatDescriptionParameters(localizedTomesDB, CharacterIds, charactersData, perksData, HTMLTagConverters, characterClassesData);

            string outputPath = Path.Combine(GlobalVariables.pathToParsedData, GlobalVariables.versionWithBranch, langKey, "Tomes.json");

            FileWriter.SaveParsedDB(localizedTomesDB, outputPath, Logger.ELogExtraTag.Tomes);
        }
    }
}