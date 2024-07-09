using CUE4Parse.GameTypes.PUBG.Assets.Exports;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UEParser.Models;

namespace UEParser.APIComposers;

public class PerkUtils
{
    public static List<LocalizationEntry> ParsePerksDescription(dynamic input)
    {
        List<LocalizationEntry> description = [];
        if (input?["UIData"]?["Description"]["Key"] != null)
        {
            LocalizationEntry entry = new()
            {
                Key = input["UIData"]["Description"]["Key"],
                SourceString = input["UIData"]["Description"]["SourceString"]
            };

            description.Add(entry);
        }
        else if (input?["PerkLevel3Description"]["Key"] != null)
        {
            LocalizationEntry entry = new()
            {
                Key = input["PerkLevel3Description"]["Key"],
                SourceString = input["PerkLevel3Description"]["SourceString"]
            };

            description.Add(entry);
        }
        else
        {
            throw new Exception("Failed to parse perk description.");
        }

        //string key = input?["UIData"]?["Description"]?["Key"] ?? input?["PerkLevel3Description"]?["Key"] ?? throw new Exception("Failed to parse perk description.");

        return description;
    }

    // Tunables need to be arranged in custom format
    // As we're going to showcase all of them at once
    // instead of rarity-based like it is in-game
    public static List<List<string>> ArrangeTunables(dynamic item)
    {
        List<dynamic> tunablesArray = [];

        foreach (var i in item.Value["PerkLevelTunables"])
        {
            var perkLevelTunables = i["Tunables"];
            tunablesArray.Add(perkLevelTunables);
        }

        List<List<string>> finalArray = [];

        int insideLength = 0;

        if (tunablesArray.Count > 0)
        {
            insideLength = ((JArray)tunablesArray[0]).Count;
        }

        for (int i = 0; i < insideLength; i++)
        {
            List<string> innerList = [];

            foreach (JArray tunables in tunablesArray.Select(v => (JArray)v))
            {
                if (i < tunables.Count)
                {
                    innerList.Add(tunables[i].ToString());
                }
            }

            // Remove duplicates and add to the finalArray
            finalArray.Add(innerList.Distinct().ToList());
        }

        return finalArray;
    }

    public static void FormatDescriptionTunables(Dictionary<string, Perk> localizedPerksDB, string langKey)
    {
        foreach (var item in localizedPerksDB)
        {
            string perkId = item.Key;
            string description = item.Value.Description;
            List<List<string>> tunables = item.Value.Tunables;

            List<string> formattedTunables = [];

            foreach (var i in tunables)
            {
                int tunableLength = i.Count;
                if (tunableLength == 1)
                {
                    string singleValue = string.Format("<span class='uncommon-rarity-color'>{0}</span>", i[0]);
                    formattedTunables.Add(singleValue);
                }
                else if (tunableLength == 3)
                {
                    string tripleValue = string.Format("<span class='uncommon-rarity-color'>{0}</span><span class='slash-dbd-fix'>/</span><span class='rare-rarity-color'>{1}</span><span class='slash-dbd-fix'>/</span><span class='veryrare-rarity-color'>{2}</span>", i[0], i[1], i[2]);
                    formattedTunables.Add(tripleValue);
                }
            }

            int tunablesArrayLength = tunables.Count;
            if (tunablesArrayLength > 0)
            {
                // Hex Undying in zh-Hant language has placeholder index set incorrectly
                // {1} but it should be {0}
                if (langKey == "zh-Hant" && perkId == "HexUndying")
                {
                    string formattedDescription = description.Replace("{1}", formattedTunables[0].ToString());
                    item.Value.Description = formattedDescription;
                }
                else if (perkId == "K33P01") // Description expects 4 tunable values while it should be only 3
                {
                    string formattedDescription = string.Format(description.Replace("{3}", ""), [.. formattedTunables]);
                    item.Value.Description = formattedDescription;
                }
                else if (perkId == "S43P02") // Description expects 2 tunable values while it should be only 1
                {
                    string formattedDescription = string.Format(description.Replace("{1}", ""), [.. formattedTunables]);
                    item.Value.Description = formattedDescription;
                }
                else
                {
                    string formattedDescription = string.Format(description, [.. formattedTunables]);
                    item.Value.Description = formattedDescription;
                }
            }
            else
            {
                item.Value.Description = description;
            }
        }
    }
}
