﻿using System.IO;
using System.Collections.Generic;
using UEParser.Models;
using UEParser.Utils;

namespace UEParser.APIComposers;

public class CharacterUtils
{
    public static Hint[] MapCharacterHints(string characterIndex, Dictionary<string, List<LocalizationEntry>> localizationModel)
    {
        string hintPath = Path.Combine(GlobalVariables.PathToExtractedAssets, "DeadByDaylight", "Content", "Data", "HintsDB.json");

        var items = FileUtils.LoadDynamicJson(hintPath);

        int hintIndex = 0;
        var hints = new List<Hint>();
        foreach (var item in items[0]["Rows"])
        {
            string hintCharacterIndex = item.Value["RelevantCharacterID"].ToString();

            if (hintCharacterIndex != characterIndex) continue;

            string roleRaw = item.Value["playerTeam"];
            string role = StringUtils.StringSplitVe(roleRaw);

            string iconPath = item.Value["IconPath"];
            string iconPathFixed = StringUtils.AddRootDirectory(iconPath, "/images/");

            string localizationTitleString = $"Hints.{hintIndex}.Title";
            string localizationDescriptionString = $"Hints.{hintIndex}.Description";

            localizationModel[localizationTitleString] =
            [
                new LocalizationEntry
                {
                    Key = item.Value["Title"]["Key"],
                    SourceString = item.Value["Title"]["SourceString"]
                }
            ];

            localizationModel[localizationDescriptionString] =
            [
                new LocalizationEntry
                {
                    Key = item.Value["Description"]["Key"],
                    SourceString = item.Value["Description"]["SourceString"]
                }
            ];

            Hint hintModel = new()
            {
                Role = role,
                Title = "",
                Description = "",
                IconPath = iconPathFixed,
            };

            hintIndex++;

            hints.Add(hintModel);
        }

        return [.. hints];
    }
}