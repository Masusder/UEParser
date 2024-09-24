using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UEParser.Utils;
using UEParser.Models;
using UEParser.ViewModels;

namespace UEParser.APIComposers;

public class TomeUtils
{
    public static JArray DescriptionParameters(dynamic node, string questId, Dictionary<string, dynamic> questObjectiveDatabaseJson)
    {
        string questIdLower = questId.ToLower();
        JArray objectiveParams = [];

        if (questObjectiveDatabaseJson.TryGetValue(questIdLower, out dynamic? value))
        {
            objectiveParams = value["DescriptionParameters"].DeepClone();
            for (int paramIndex = 0; paramIndex < objectiveParams.Count; paramIndex++)
            {
                string? paramString = (string?)objectiveParams[paramIndex];
                if (paramString == "maxProgression")
                {
                    int paramValueRaw = node.Value["objectives"][questId]["neededProgression"];
                    string progressionTypeRaw = value["ProgressionType"];
                    string progressionType = StringUtils.DoubleDotsSplit(progressionTypeRaw);

                    if (progressionType == "Percentage")
                    {
                        int modifiedParamValue = paramValueRaw / 100;
                        objectiveParams[paramIndex] = modifiedParamValue;
                    }
                    else
                    {
                        objectiveParams[paramIndex] = paramValueRaw;
                    }
                }
                else if (paramString == "perk" || paramString == "exclusivePerk" || paramString == "randomPerks")
                {
                    JArray conditions = node.Value["objectives"][questId]["conditions"];
                    for (int conditionIndex = 0; conditionIndex < conditions.Count; conditionIndex++)
                    {
                        if (node.Value["objectives"][questId]["conditions"][conditionIndex]["key"] == "perk" || node.Value["objectives"][questId]["conditions"][conditionIndex]["key"] == "exclusivePerk")
                        {
                            JArray conditionsList = node.Value["objectives"][questId]["conditions"][conditionIndex]["value"];
                            if (conditionsList.Count > 1)
                            {
                                for (int perkIndex = 0; perkIndex < node.Value["objectives"][questId]["conditions"][conditionIndex]["value"].Count; perkIndex++)
                                {
                                    string perkId = node.Value["objectives"][questId]["conditions"][conditionIndex]["value"][perkIndex];
                                    for (int duplicatePerkIndex = 0; duplicatePerkIndex < objectiveParams.Count; duplicatePerkIndex++)
                                    {
                                        // Check if perk already exists in objective params
                                        // This will make sure there's no duplicate perks in description
                                        bool exists = objectiveParams.Any(jv => (string?)jv == perkId);
                                        string keyToCheck = node.Value["objectives"][questId]["conditions"][conditionIndex]["key"];
                                        if (objectiveParams[duplicatePerkIndex].ToString() == keyToCheck.ToString() && !exists)
                                        {
                                            objectiveParams[duplicatePerkIndex] = perkId;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                string perkId = node.Value["objectives"][questId]["conditions"][conditionIndex]["value"][0];
                                objectiveParams[paramIndex] = perkId;
                            }
                        }
                        else if (node.Value["objectives"][questId]["conditions"][conditionIndex]["key"] == "randomPerks")
                        {
                            JArray randomPerksArray = node.Value["objectives"][questId]["conditions"][conditionIndex]["value"];
                            int amoutOfPerks = randomPerksArray.Count;

                            objectiveParams[paramIndex] = amoutOfPerks;
                        }
                    }
                }
                else if (paramString == "character")
                {
                    JArray conditions = node.Value["objectives"][questId]["conditions"];
                    for (int conditionIndex = 0; conditionIndex < conditions.Count; conditionIndex++)
                    {
                        if (node.Value["objectives"][questId]["conditions"][conditionIndex]["key"] == "character")
                        {
                            string characterString = node.Value["objectives"][questId]["conditions"][conditionIndex]["value"][0];
                            objectiveParams[paramIndex] = characterString;
                        }
                    }
                }
                else if (paramString == "class")
                {
                    JArray conditions = node.Value["objectives"][questId]["conditions"];
                    for (int conditionIndex = 0; conditionIndex < conditions.Count; conditionIndex++)
                    {
                        if (node.Value["objectives"][questId]["conditions"][conditionIndex]["key"] == "class")
                        {
                            string classValue = node.Value["objectives"][questId]["conditions"][conditionIndex]["value"][0];
                            objectiveParams[paramIndex] = $"class::{classValue}"; // Combined with "class::" to avoid conflicts in format description method
                        }
                    }
                }

                JArray questEventArray = node.Value["objectives"][questId]["questEvent"];
                for (int questEventIndex = 0; questEventIndex < questEventArray.Count; questEventIndex++)
                {
                    if (paramString != null)
                    {
                        string questEventId = node.Value["objectives"][questId]["questEvent"][questEventIndex]["questEventId"];
                        if (paramString.Equals(questEventId, StringComparison.CurrentCultureIgnoreCase))
                        {
                            int modifiedParamValue = node.Value["objectives"][questId]["questEvent"][questEventIndex]["repetition"];
                            objectiveParams[paramIndex] = modifiedParamValue;
                        }
                    }
                }


            }
        }

        return objectiveParams;
    }

    public static Dictionary<string, double> CastCoordinates(dynamic node)
    {
        var coordinatesObject = node.Value["coordinates"];
        Dictionary<string, double> coordinates = [];
        if (coordinatesObject is JObject coordinatesJObject)
        {

            foreach (var property in coordinatesJObject.Properties())
            {
                if (double.TryParse(property.Value.ToString(), out double value))
                {
                    coordinates.Add(property.Name, value);
                }
                else
                {
                    throw new InvalidCastException($"Unable to cast value of '{property.Name}' to double.");
                }
            }
        }

        return coordinates;
    }

    private static Dictionary<string, string> CreatePerksDictionary(Dictionary<string, Perk> PerksData)
    {
        var perksDictionary = new Dictionary<string, string>();

        foreach (var item in PerksData)
        {
            string keyLower = item.Key.ToLower();
            perksDictionary[keyLower] = item.Key;
        }

        return perksDictionary;
    }

    public static void FormatDescriptionParameters(Dictionary<string, Tome> localizedTomesDB, Dictionary<string, int> CharacterIds, Dictionary<string, Character> CharactersData, Dictionary<string, Perk> PerksData, TagConverters HTMLTagConverters, Dictionary<string, CharacterClass> CharacterClassesData)
    {
        var perksDictionary = CreatePerksDictionary(PerksData);
        foreach (var item in localizedTomesDB)
        {
            if (item.Value.Levels == null) continue;

            for (int levelIndex = 0; levelIndex < item.Value.Levels.Count; levelIndex++)
            {
                var level = item.Value.Levels[levelIndex];

                if (level.Nodes == null) continue;

                foreach (var node in level.Nodes)
                {
                    string? nodeDescription = node.Value.Description;

                    if (nodeDescription == null) continue;

                    JArray? descriptionParametersArray = node.Value.DescriptionParams;
                    List<dynamic>? descriptionParameters = descriptionParametersArray?.Select(x => (dynamic)x).ToList();

                    foreach (var tag in HTMLTagConverters.HTMLTagConverters)
                    {
                        nodeDescription = nodeDescription.Replace(tag.Key, tag.Value);
                    }

                    node.Value.Description = nodeDescription;

                    if (descriptionParameters?.Count > 0)
                    {
                        for (int i = 0; i < descriptionParameters.Count; i++)
                        {
                            dynamic param = descriptionParameters[i];
                            string paramString = param.ToString();

                            if (paramString.StartsWith("class::"))
                            {
                                var characterClassId = StringUtils.DoubleDotsSplit(paramString);
                                if (CharacterClassesData.TryGetValue(characterClassId, out CharacterClass? value))
                                {
                                    string characterClassName = value.Name;
                                    descriptionParameters[i] = characterClassName;
                                }
                            }

                            if (CharacterIds.ContainsKey(paramString.ToLower()))
                            {
                                var characterId = CharacterIds[paramString.ToLower()];
                                string? characterString = characterId.ToString();

                                if (characterString != null)
                                {
                                    string characterName = CharactersData[characterString].Name;
                                    descriptionParameters[i] = characterName;
                                }
                            }

                            if (perksDictionary.TryGetValue(paramString.ToLower(), out string? matchingString))
                            {
                                string? perkName = PerksData[matchingString].Name;
                                if (perkName != null)
                                {
                                    descriptionParameters[i] = perkName;
                                }
                                else
                                {
                                    LogsWindowViewModel.Instance.AddLog($"Not found perk param: {paramString}.", Logger.LogTags.Warning);
                                }
                            }
                        }

                        string formattedDescription = string.Format(nodeDescription, descriptionParameters.ToArray());

                        node.Value.Description = formattedDescription;
                    }
                }
            }
        }
    }
}