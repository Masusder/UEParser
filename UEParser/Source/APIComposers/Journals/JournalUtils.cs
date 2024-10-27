using System.IO;
using System.Collections.Generic;
using UEParser.Models;
using UEParser.Utils;

namespace UEParser.APIComposers;

public class JournalUtils
{
    public static void PopulateTomeNames(Dictionary<string, Journal> localizedJournalsDb, string langKey)
    {
        Dictionary<string, Tome> tomesData = FileUtils.LoadJsonFileWithTypeCheck<Dictionary<string, Tome>>(Path.Combine(GlobalVariables.PathToParsedData, GlobalVariables.VersionWithBranch, langKey, "Tomes.json"));

        foreach (var journalTome in localizedJournalsDb)
        {
            string journalTomeId = journalTome.Key;
            string tomeName = tomesData[journalTomeId].Name;

            journalTome.Value.TomeName = tomeName;
        }
    }
}