using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UEParser.Utils;

namespace UEParser.APIComposers;

public class TomeUtils
{
    // BHVR uses codenames such as "TOME19", to make it consistent in all cases turn it into "Tome"
    public static string TomeToTitleCase(string input)
    {
        if (string.IsNullOrEmpty(input) || !input.StartsWith("tome", StringComparison.OrdinalIgnoreCase))
        {
            return input;
        }

        string firstChar = input[..1].ToUpper();
        string restOfChars = input[1..].ToLower();
        return firstChar + restOfChars;
    }

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
}