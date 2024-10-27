using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using UEParser.Models;
using UEParser.Parser;
using UEParser.Utils;
using UEParser.ViewModels;

namespace UEParser.APIComposers;

public class Offerings
{
    private static readonly Dictionary<string, Dictionary<string, List<LocalizationEntry>>> LocalizationData = [];

    public static async Task InitializeOfferingsDb(CancellationToken token)
    {
        await Task.Run(() =>
        {
            Dictionary<string, Offering> parsedOfferingsDb = [];

            LogsWindowViewModel.Instance.AddLog($"Starting parsing process..", Logger.LogTags.Info, Logger.ELogExtraTag.Offerings);

            ParseOfferings(parsedOfferingsDb, token);

            LogsWindowViewModel.Instance.AddLog($"Parsed total of {parsedOfferingsDb.Count} items.", Logger.LogTags.Info, Logger.ELogExtraTag.Offerings);

            ParseLocalizationAndSave(parsedOfferingsDb, token);
        }, token);
    }

    private static void ParseOfferings(Dictionary<string, Offering> parsedOfferingsDb, CancellationToken token)
    {
        string[] filePaths = Helpers.FindFilePathsInExtractedAssetsCaseInsensitive("OfferingDB.json");

        foreach (string filePath in filePaths)
        {
            string packagePath = StringUtils.StripExtractedAssetsDir(filePath);
            LogsWindowViewModel.Instance.AddLog($"Processing: {packagePath}", Logger.LogTags.Info, Logger.ELogExtraTag.Offerings);

            var assetItems = FileUtils.LoadDynamicJson(filePath);

            if ((assetItems?[0]?["Rows"]) == null) continue;

            foreach (var item in assetItems[0]["Rows"])
            {
                token.ThrowIfCancellationRequested();

                string offeringId = item.Name;

                string typeRaw = item.Value["OfferingType"];
                string type = StringUtils.DoubleDotsSplit(typeRaw);

                string availableRaw = item.Value["Availability"]["ItemAvailability"];
                string available = StringUtils.DoubleDotsSplit(availableRaw);

                string roleRaw = item.Value["Role"];
                string role = StringUtils.StringSplitVe(roleRaw);

                string rarityRaw = item.Value["Rarity"];
                string rarity = StringUtils.DoubleDotsSplit(rarityRaw);

                List<string> statusEffectsList = [];
                foreach (var row in item.Value["Effects"])
                {
                    string statusEffectRaw = row["Type"];
                    string statusEffect = StringUtils.DoubleDotsSplit(statusEffectRaw);
                    statusEffectsList.Add(statusEffect);
                }

                string[] statusEffectsArray = [.. statusEffectsList];

                string iconPathRaw = item.Value["UIData"]["IconFilePathList"][0];
                string iconPath = StringUtils.AddRootDirectory(iconPathRaw, "/images/");

                Dictionary<string, List<LocalizationEntry>> localizationModel = new()
                {
                    ["Name"] = [
                    new LocalizationEntry
                        {
                            Key = item.Value["UIData"]["DisplayName"]["Key"],
                            SourceString = item.Value["UIData"]["DisplayName"]["SourceString"]
                        }
                    ],
                    ["Description"] = [
                    new LocalizationEntry
                        {
                            Key = item.Value["UIData"]["Description"]["Key"],
                            SourceString = item.Value["UIData"]["Description"]["SourceString"]
                        }
                    ]
                };

                LocalizationData.TryAdd(offeringId, localizationModel);

                Offering model = new()
                {
                    Type = type,
                    StatusEffects = statusEffectsArray,
                    Tags = item.Value["Tags"],
                    Available = available,
                    Name = "",
                    Description = "",
                    Role = role,
                    Rarity = rarity,
                    Image = iconPath
                };

                parsedOfferingsDb.Add(offeringId, model);
            }
        }
    }

    private static void ParseLocalizationAndSave(Dictionary<string, Offering> parsedOfferingsDb, CancellationToken token)
    {
        LogsWindowViewModel.Instance.AddLog($"Starting localization process..", Logger.LogTags.Info, Logger.ELogExtraTag.Offerings);

        string[] filePaths = Directory.GetFiles(Path.Combine(GlobalVariables.RootDir, "Dependencies", "Locres"), "*.json", SearchOption.TopDirectoryOnly);

        foreach (string filePath in filePaths)
        {
            token.ThrowIfCancellationRequested();

            string jsonString = File.ReadAllText(filePath);
            string fileName = Path.GetFileName(filePath);
            string langKey = StringUtils.LangSplit(fileName);

            Dictionary<string, string> languageKeys = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString) ?? throw new Exception($"Failed to load following locres file: {langKey}.");

            var objectString = JsonConvert.SerializeObject(parsedOfferingsDb);
            Dictionary<string, Offering> localizedOfferingsDb = JsonConvert.DeserializeObject<Dictionary<string, Offering>>(objectString) ?? [];

            Helpers.LocalizeDb(localizedOfferingsDb, LocalizationData, languageKeys, langKey);

            string outputPath = Path.Combine(GlobalVariables.PathToParsedData, GlobalVariables.VersionWithBranch, langKey, "Offerings.json");

            FileWriter.SaveParsedDb(localizedOfferingsDb, outputPath, Logger.ELogExtraTag.Offerings);
        }
    }
}