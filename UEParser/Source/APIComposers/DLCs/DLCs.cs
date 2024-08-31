using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UEParser.Models;
using UEParser.Parser;
using UEParser.Utils;
using UEParser.ViewModels;

namespace UEParser.APIComposers;

public class DLCs
{
    private static readonly Dictionary<string, Dictionary<string, List<LocalizationEntry>>> LocalizationData = [];

    public static async Task InitializeDlcsDB()
    {
        await Task.Run(async () =>
        {
            Dictionary<string, DLC> parsedDlcsDB = [];

            LogsWindowViewModel.Instance.AddLog($"[DLC] Starting parsing process..", Logger.LogTags.Info);

            ParseDLCs(parsedDlcsDB);

            LogsWindowViewModel.Instance.AddLog($"[DLC] Parsed total of {parsedDlcsDB.Count} items.", Logger.LogTags.Info);

            await ParseLocalizationAndSave(parsedDlcsDB);
        });
    }

    private static readonly string[] ignoreSteamIds =
    [
        "1",
        "-1",
        "0",
        "639710",
        "724650",
        "750380",
        "499080",
        "555440",
        "499080"
    ];
    private static void ParseDLCs(Dictionary<string, DLC> parsedDlcsDB)
    {
        string[] filePaths = Helpers.FindFilePathsInExtractedAssetsCaseInsensitive("DlcDB.json");

        foreach (string filePath in filePaths)
        {
            string packagePath = StringUtils.StripExtractedAssetsDir(filePath);
            LogsWindowViewModel.Instance.AddLog($"[DLC] Processing: {packagePath}", Logger.LogTags.Info);

            var assetItems = FileUtils.LoadDynamicJson(filePath);

            if ((assetItems?[0]?["Rows"]) == null) continue;

            foreach (var item in assetItems[0]["Rows"])
            {
                string dlcId = item.Name;

                string steamId = item.Value["DlcIdSteam"];

                if (ignoreSteamIds.Contains(steamId)) continue;

                string bannerImageRaw = item.Value["BannerImage"]["AssetPathName"];
                string bannerImage = StringUtils.TransformImagePathSpecialPacks(bannerImageRaw);

                //Dictionary<string, List<LocalizationEntry>> localizationModel = new()
                //{
                //    ["Name"] = [
                //    new LocalizationEntry
                //        {
                //            Key = item.Value["DisplayName"]["Key"],
                //            SourceString = item.Value["DisplayName"]["SourceString"]
                //        }
                //    ]
                //};

                //LocalizationData.TryAdd(dlcId, localizationModel);

                DLC model = new()
                {
                    Name = "",
                    DetailedDescription = "",
                    ReleaseDate = null,
                    Description = "",
                    HeaderImage = null,
                    BannerImage = bannerImage,
                    SteamId = steamId,
                    EpicId = item.Value["DlcIdEpic"],
                    DMMId = item.Value["DlcIdDmm"],
                    PS4Id = item.Value["DlcIdPS4"],
                    Xbox1Id = item.Value["Xbox1Id"],
                    XboxSeriesXId = item.Value["DlcIdXSX"],
                    SwitchId = item.Value["DlcIdSwitch"],
                    WindowsStoreId = item.Value["DlcIdGRDK"],
                    PS5Id = item.Value["DlcIdPS5"],
                    StadiaId = item.Value["DlcIdStadia"],
                    SortOrder = item.Value["UISortOrder"],
                    AllowsCrossProg = item.Value["AllowsCrossProg"],
                    Screenshots = null
                };

                parsedDlcsDB.Add(dlcId, model);
            }
        }
    }

    private static async Task ParseLocalizationAndSave(Dictionary<string, DLC> parsedDlcsDB)
    {
        LogsWindowViewModel.Instance.AddLog($"[DLC] Starting localization process..", Logger.LogTags.Info);

        string[] filePaths = Directory.GetFiles(Path.Combine(GlobalVariables.rootDir, "Dependencies", "Locres"), "*.json", SearchOption.TopDirectoryOnly);

        foreach (string filePath in filePaths)
        {
            //string jsonString = File.ReadAllText(filePath);
            string fileName = Path.GetFileName(filePath);

            string langKey = StringUtils.LangSplit(fileName);

            //Dictionary<string, string> languageKeys = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString) ?? throw new Exception($"Failed to load following locres file: {langKey}.");

            var objectString = JsonConvert.SerializeObject(parsedDlcsDB);
            Dictionary<string, DLC> localizedDlcsDB = JsonConvert.DeserializeObject<Dictionary<string, DLC>>(objectString) ?? [];

            //Helpers.LocalizeDB(localizedDlcsDB, LocalizationData, languageKeys, langKey);

            await DLCUtils.PopulateSteamAPIData(localizedDlcsDB, langKey);

            string outputPath = Path.Combine(GlobalVariables.pathToParsedData, GlobalVariables.versionWithBranch, langKey, "DLC.json");

            FileWriter.SaveParsedDB(localizedDlcsDB, outputPath, "DLC");
        }
    }
}