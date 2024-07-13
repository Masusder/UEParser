using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UEParser.Models;
using UEParser.Parser;
using UEParser.ViewModels;
using UEParser.Utils;
using UEParser.Services;
using Newtonsoft.Json.Linq;
using System.Text.Json.Nodes;
using System.Reflection;
using System.Collections;

namespace UEParser.APIComposers;

// Works but is outdated, should be reworked
public class Tomes
{
    private static readonly Dictionary<string, object> CosmeticsData = FileUtils.LoadJsonFileWithTypeCheck<Dictionary<string, object>>(Path.Combine(GlobalVariables.rootDir, "Output", "ParsedData", GlobalVariables.versionWithBranch, "en", "Cosmetics.json"));
    private static readonly Dictionary<string, object> QuestNodeDatabase = FileUtils.LoadJsonFileWithTypeCheck<Dictionary<string, object>>(Path.Combine(GlobalVariables.rootDir, "Dependencies", "HelperComponents", "questNodeDatabase.json"));
    private static readonly Dictionary<string, object> QuestObjectiveDatabase = FileUtils.LoadJsonFileWithTypeCheck<Dictionary<string, object>>(Path.Combine(GlobalVariables.rootDir, "Dependencies", "HelperComponents", "questObjectiveDatabase.json"));
    private static readonly Dictionary<string, int> CharacterIds = FileUtils.LoadJsonFileWithTypeCheck<Dictionary<string, int>>(Path.Combine(GlobalVariables.rootDir, "Dependencies", "HelperComponents", "characterIds.json"));
    private static readonly Dictionary<string, Dictionary<string, List<LocalizationEntry>>> LocalizationData = [];

    public static async Task InitializeTomesDB()
    {
        await Task.Run(() =>
        {
            Dictionary<string, Tome> parsedTomesDB = [];

            LogsWindowViewModel.Instance.AddLog($"[Tomes] Starting parsing process..", Logger.LogTags.Info);

            ParseTomes(parsedTomesDB);

            LogsWindowViewModel.Instance.AddLog($"[Tomes] Parsed total of {parsedTomesDB.Count} items.", Logger.LogTags.Info);

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
            LogsWindowViewModel.Instance.AddLog($"[Tomes] Processing: {packagePath}", Logger.LogTags.Info);

            var assetItems = FileUtils.LoadDynamicJson(filePath);

            if ((assetItems?[0]?["Rows"]) == null)
            {
                continue;
            }

            foreach (var item in assetItems[0]["Rows"])
            {
                string tomeId = item.Name;
                string tomeIdTitleCase = TomeUtils.TomeToTitleCase(tomeId);

                string pathToTomeFile = Path.Combine(GlobalVariables.rootDir, "Output", "API", GlobalVariables.versionWithBranch, "Tomes", $"{tomeIdTitleCase}.json");
                if (!File.Exists(pathToTomeFile))
                {
                    LogsWindowViewModel.Instance.AddLog("Not found Tome data. Make sure to update API first.", Logger.LogTags.Error);
                    LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
                    continue;
                }

                var tomeData = FileUtils.LoadDynamicJson(pathToTomeFile);

                Dictionary<string, List<LocalizationEntry>> localizationModel = [];

                List<Level> levels = CreateLevels(tomeIdTitleCase, tomeData, localizationModel);

                //localizationModel["Name"] = [
                //    new LocalizationEntry
                //    {
                //        Key = item.Value["Title"]["Key"],
                //        SourceString = item.Value["Title"]["SourceString"]
                //    }
                //];

                //localizationModel["Description"] = [
                //    new LocalizationEntry
                //    {
                //        Key = item.Value["PurchasePassPopupMessage"]["Key"],
                //        SourceString = item.Value["PurchasePassPopupMessage"]["SourceString"]
                //    }
                //];


                //if (!string.IsNullOrEmpty(rulesDescriptionKey) && !string.IsNullOrEmpty(rulesDescriptionSourceString))
                //{
                //    localizationModel[localizationNameString] =
                //    [
                //        new LocalizationEntry
                //{
                //    Key = rulesDescriptionKey,
                //    SourceString = rulesDescriptionSourceString
                //}
                //    ];
                //}

                LocalizationData.TryAdd(tomeIdTitleCase, localizationModel);

                Tome model = new()
                {
                    Name = "",
                    Description = "",
                    Levels = levels,
                    RiftID = tomeData?.GetValue(item.Name, StringComparison.OrdinalIgnoreCase)?["riftId"],
                    NewTomePopup = tomeData?.GetValue(item.Name, StringComparison.OrdinalIgnoreCase)?["newTomePopup"]
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

        string nodeName = clientValue["DisplayName"]["Key"];

        string iconPathRaw = clientValue["IconPath"];
        string iconPath = StringUtils.AddRootDirectory(iconPathRaw, "/images/");
            //"/images/" + clientValue?["IconPath"];
        string playerRoleRaw = clientValue["PlayerRole"];
        string? rulesDescription = null;
        string? objectiveDescription;

        string? rulesDescriptionKey = null;
        string? rulesDescriptionSourceString = null;
        if (QuestObjectiveDatabase.TryGetValue(questId, out dynamic? value))
        {
            objectiveDescription = value["Description"]["Key"];
            rulesDescription = value["RulesDescription"]["Key"];

            rulesDescriptionKey = value["RulesDescription"]["Key"];
            rulesDescriptionSourceString = value["RulesDescription"]["SourceString"];
        }
        else
        {
            QuestNodeDatabase.TryGetValue(questId, out dynamic? objValue);

            // May I ask BHVR why descriptions for some nodes are split between two databases?
            objectiveDescription = objValue?["Description"]["Key"];
        }

        string playerRole = StringUtils.StringSplitVE(playerRoleRaw);

        // Add custom image for 'end' and 'start' nodes
        // For Reward set icon path for cosmetic icon
        if (clientQuestId == "end" || clientQuestId == "start")
        {
            iconPath = "/images/Archives/StartEndNode.png";
        }
        else if (clientQuestId == "reward")
        {
            nodeName = "Reward";
            for (int rewardIndex = 0; rewardIndex < tomesJson.GetValue(tomeId, StringComparison.OrdinalIgnoreCase)["level"][levelIndex]["nodes"][nodeId]["reward"].Count; rewardIndex++)
            {
                string rewardId = tomesJson.GetValue(tomeId, StringComparison.OrdinalIgnoreCase)["level"][levelIndex]["nodes"][nodeId]["reward"][rewardIndex]["id"];

                CosmeticsData.TryGetValue(rewardId, out dynamic? cosmeticValue);

                if (cosmeticValue == null) throw new Exception("Not found rewardId in cosmetics data.");

                iconPath = cosmeticValue["IconFilePathList"];
            }
        }

        string localizationRulesDescriptionString = $"Levels.{levelIndex}.Nodes.{nodeId}.RulesDescription";
        //string localizationNameString = $"Levels.{levelIndex}.Nodes.{nodeId}.Name";

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
        LogsWindowViewModel.Instance.AddLog($"[Tomes] Starting localization process..", Logger.LogTags.Info);

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

            //LocalizeTomesDB(localizedTomesDB, languageKeys, langKey);

            string outputPath = Path.Combine(GlobalVariables.rootDir, "Output", "ParsedData", GlobalVariables.versionWithBranch, langKey, "Tomes.json");

            FileWriter.SaveParsedDB(localizedTomesDB, outputPath, "Tomes");
        }
    }

    //public static void LocalizeTomesDB<T>(Dictionary<string, T> localizedDB, Dictionary<string, string> languageKeys, string langKey)
    //{
    //    foreach (var item in localizedDB)
    //    {
    //        string id = item.Key;
    //        LocalizationData.TryGetValue(id, out var localizationDataEntry);

    //        if (localizationDataEntry == null)
    //        {
    //            continue;
    //        }

    //        foreach (var entry in localizationDataEntry)
    //        {
    //            string propertiesKeys = entry.Key;
    //            string[] splitKeys = propertiesKeys.Split('.');

    //            if (item.Value == null) continue;

    //            object nestedValue = GetNestedValue(item.Value, splitKeys);

    //            // Here, nestedValue should hold the final value of the nested property
    //            if (nestedValue != null)
    //            {
    //                // Modify nestedValue (example: append "Modified" to string)
    //                if (nestedValue is string stringValue)
    //                {
    //                    nestedValue = "@#Modified";
    //                }
    //                else if (nestedValue is IList list)
    //                {
    //                    // Example: modify the first element of the list
    //                    if (list.Count > 0)
    //                    {
    //                        list[0] = "@#Modified";
    //                    }
    //                }
    //                else
    //                {
    //                    // Handle other types as needed
    //                }

    //                // Update localizedDB with modified value
    //                UpdateNestedValue(localizedDB, id, splitKeys, nestedValue);
    //            }
    //        }
    //    }
    //}

    //private static void UpdateNestedValue<T>(Dictionary<string, T> dictionary, string id, string[] keys, object newValue)
    //{
    //    // Get the original value from the dictionary
    //    if (dictionary.TryGetValue(id, out T originalValue))
    //    {
    //        // Navigate to the nested property using keys and update its value
    //        object current = originalValue;
    //        for (int i = 0; i < keys.Length - 1; i++)
    //        {
    //            string key = keys[i];
    //            Type currentType = current.GetType();

    //            if (currentType.IsGenericType && currentType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
    //            {
    //                PropertyInfo indexer = currentType.GetProperty("Item");
    //                current = indexer.GetValue(current, new object[] { key });
    //            }
    //            else if (typeof(IEnumerable<object>).IsAssignableFrom(currentType))
    //            {
    //                current = GetEnumerableElement(current, key);
    //            }
    //            else
    //            {
    //                PropertyInfo propInfo = currentType.GetProperty(key);
    //                if (propInfo != null)
    //                {
    //                    current = propInfo.GetValue(current);
    //                }
    //                else
    //                {
    //                    return; // Property not found
    //                }
    //            }
    //        }

    //        // Update the final nested property with newValue
    //        string lastKey = keys[keys.Length - 1];
    //        Type finalType = current.GetType();

    //        if (finalType.IsGenericType && finalType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
    //        {
    //            PropertyInfo indexer = finalType.GetProperty("Item");
    //            indexer.SetValue(current, newValue, new object[] { lastKey });
    //        }
    //        else if (typeof(IList).IsAssignableFrom(finalType))
    //        {
    //            if (int.TryParse(lastKey, out int index))
    //            {
    //                IList list = (IList)current;
    //                if (index >= 0 && index < list.Count)
    //                {
    //                    list[index] = newValue;
    //                }
    //            }
    //            // Handle other types of lists if needed
    //        }
    //        else
    //        {
    //            PropertyInfo propInfo = finalType.GetProperty(lastKey);
    //            if (propInfo != null && propInfo.CanWrite)
    //            {
    //                propInfo.SetValue(current, newValue);
    //            }
    //        }

    //        // Update the dictionary with the modified originalValue
    //        dictionary[id] = originalValue;
    //    }
    //}

    //private static object GetNestedValue(object obj, string[] keys)
    //{
    //    foreach (var key in keys)
    //    {
    //        if (obj == null)
    //        {
    //            return null;
    //        }

    //        Type objType = obj.GetType();

    //        if (objType.IsGenericType && objType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
    //        {
    //            // Handle case where obj is a dictionary
    //            PropertyInfo indexer = objType.GetProperty("Item");
    //            obj = indexer.GetValue(obj, new object[] { key });
    //        }
    //        else if (typeof(IEnumerable<object>).IsAssignableFrom(objType))
    //        {
    //            // Handle case where obj is an IEnumerable<object>
    //            obj = GetEnumerableElement(obj, key);
    //        }
    //        else
    //        {
    //            // Handle case where obj is a class or object
    //            PropertyInfo propInfo = objType.GetProperty(key);
    //            if (propInfo != null)
    //            {
    //                obj = propInfo.GetValue(obj);
    //            }
    //            else
    //            {
    //                obj = null;
    //                break;
    //            }
    //        }
    //    }

    //    return obj;
    //}

    //private static object GetEnumerableElement(object obj, string key)
    //{
    //    IEnumerable<object> enumerable = (IEnumerable<object>)obj;

    //    // Attempt to parse key as an index
    //    if (int.TryParse(key, out int index))
    //    {
    //        // Access by index
    //        try
    //        {
    //            obj = enumerable.ElementAt(index);
    //        }
    //        catch (ArgumentOutOfRangeException)
    //        {
    //            obj = null;
    //        }
    //    }
    //    else
    //    {
    //        // Access by property name (not supported in this simplified example)
    //        obj = null;
    //    }

    //    return obj;
    //}
}