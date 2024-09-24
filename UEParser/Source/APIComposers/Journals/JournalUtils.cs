using System.IO;
using System.Collections.Generic;
using UEParser.Models;
using UEParser.Utils;

namespace UEParser.APIComposers;

public class JournalUtils
{
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