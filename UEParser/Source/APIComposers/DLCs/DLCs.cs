using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using UEParser.Models;
using UEParser.Parser;
using UEParser.Utils;
using UEParser.ViewModels;

namespace UEParser.APIComposers;

public class DLCs
{
    private static readonly Dictionary<string, Dictionary<string, List<LocalizationEntry>>> LocalizationData = [];

    public static async Task InitializeDlcsDb(CancellationToken token)
    {
        await Task.Run(async () =>
        {
            Dictionary<string, DLC> parsedDlcsDb = [];

            LogsWindowViewModel.Instance.AddLog($"Starting parsing process..", Logger.LogTags.Info, Logger.ELogExtraTag.DLC);

            ParseDLCs(parsedDlcsDb, token);

            LogsWindowViewModel.Instance.AddLog($"Parsed total of {parsedDlcsDb.Count} items.", Logger.LogTags.Info, Logger.ELogExtraTag.DLC);

            await ParseLocalizationAndSave(parsedDlcsDb, token);
        }, token);
    }

    private static readonly string[] IgnoreSteamIds =
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
    private static void ParseDLCs(Dictionary<string, DLC> parsedDlcsDb, CancellationToken token)
    {
        string[] filePaths = Helpers.FindFilePathsInExtractedAssetsCaseInsensitive("DlcDB.json");

        foreach (string filePath in filePaths)
        {
            string packagePath = StringUtils.StripExtractedAssetsDir(filePath);
            LogsWindowViewModel.Instance.AddLog($"Processing: {packagePath}", Logger.LogTags.Info, Logger.ELogExtraTag.DLC);

            var assetItems = FileUtils.LoadDynamicJson(filePath);

            if ((assetItems?[0]?["Rows"]) == null) continue;

            foreach (var item in assetItems[0]["Rows"])
            {
                token.ThrowIfCancellationRequested();

                string dlcId = item.Name;

                string steamId = item.Value["DlcIdSteam"];

                if (IgnoreSteamIds.Contains(steamId)) continue;

                string bannerImageRaw = item.Value["BannerImage"]["AssetPathName"];
                string bannerImage = StringUtils.TransformImagePathSpecialPacks(bannerImageRaw);

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
                    PS4Id = item.Value["DlcIdPS4"],
                    XB1_XSX_GDK = item.Value["DlcId_XB1_XSX_GDK"],
                    SwitchId = item.Value["DlcIdSwitch"],
                    WindowsStoreId = item.Value["DlcIdGRDK"],
                    PS5Id = item.Value["DlcIdPS5"],
                    StadiaId = item.Value["DlcIdStadia"],
                    SortOrder = item.Value["UISortOrder"],
                    AllowsCrossProg = item.Value["AllowsCrossProg"],
                    Screenshots = null
                };

                parsedDlcsDb.Add(dlcId, model);
            }
        }
    }

    private static async Task ParseLocalizationAndSave(Dictionary<string, DLC> parsedDlcsDb, CancellationToken token)
    {
        LogsWindowViewModel.Instance.AddLog($"Starting localization process..", Logger.LogTags.Info, Logger.ELogExtraTag.DLC);

        string[] filePaths = Directory.GetFiles(Path.Combine(GlobalVariables.RootDir, "Dependencies", "Locres"), "*.json", SearchOption.TopDirectoryOnly);

        foreach (string filePath in filePaths)
        {
            token.ThrowIfCancellationRequested();

            string fileName = Path.GetFileName(filePath);
            string langKey = StringUtils.LangSplit(fileName);

            var objectString = JsonConvert.SerializeObject(parsedDlcsDb);
            Dictionary<string, DLC> localizedDlcsDb = JsonConvert.DeserializeObject<Dictionary<string, DLC>>(objectString) ?? [];

            await DLCUtils.PopulateSteamAPIData(localizedDlcsDb, langKey);

            string outputPath = Path.Combine(GlobalVariables.PathToParsedData, GlobalVariables.VersionWithBranch, langKey, "DLC.json");

            FileWriter.SaveParsedDb(localizedDlcsDb, outputPath, Logger.ELogExtraTag.DLC);
        }
    }
}