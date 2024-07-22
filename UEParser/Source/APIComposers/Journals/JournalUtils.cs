using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using UEParser.Models;
using UEParser.Utils;

namespace UEParser.APIComposers;

public class JournalUtils
{
    // Helper method to add localization entries to the model
    //public static void AddLocalizationEntry(Dictionary<string, List<LocalizationEntry>> localizationModel, string key, string entryKey, string entrySourceString)
    //{
    //    if (!string.IsNullOrEmpty(entryKey) && !string.IsNullOrEmpty(entrySourceString))
    //    {
    //        localizationModel[key] =
    //        [
    //            new() {
    //                Key = entryKey,
    //                SourceString = entrySourceString
    //            }
    //        ];
    //    }
    //}

    public static void PopulateTomeNames(Dictionary<string, Journal> localizedJournalsDB, string langKey)
    {
        Dictionary<string, Tome> tomesData = FileUtils.LoadJsonFileWithTypeCheck<Dictionary<string, Tome>>(Path.Combine(GlobalVariables.pathToParsedData, GlobalVariables.versionWithBranch, langKey, "Tomes.json"));

        foreach (var journalTome in localizedJournalsDB)
        {
            string journalTomeId = journalTome.Key;
            string tomeName = tomesData[journalTomeId].Name;

            journalTome.Value.TomeName = tomeName;
        }
    }
}