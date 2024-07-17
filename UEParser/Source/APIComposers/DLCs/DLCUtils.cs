using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System;
using UEParser.Models;
using UEParser.Utils;

namespace UEParser.APIComposers;

public class DLCUtils
{
    public static async Task PopulateSteamAPIData(Dictionary<string, DLC> localizedDlcsDB, string langKey)
    {
        foreach (var item in localizedDlcsDB)
        {
            string steamId = item.Value.SteamId;
            string dlcId = item.Key;

            await SteamAPI.FetchDlcDetails(steamId, dlcId, langKey);

            string dlcDetailsFilePath = Path.Combine(GlobalVariables.rootDir, "Dependencies", "HelperComponents", "DLC", langKey, $"{dlcId}.json");

            if (!File.Exists(dlcDetailsFilePath)) throw new Exception("Not found DLC details file.");

            var dlcData = FileUtils.LoadDynamicJson(dlcDetailsFilePath);

            string? dlcName = dlcData?[steamId]?["data"]?["name"].ToString();
            string? detailedDescription = dlcData?[steamId]?["data"]?["detailed_description"]?.ToString();
            string? releaseDate = dlcData?[steamId]?["data"]?["release_date"]?["date"]?.ToString();
            string? headerImage = dlcData?[steamId]?["data"]?["header_image"]?.ToString();
            IEnumerable<string?>? screenshots = null;

            if (dlcData?[steamId]?["data"]?["screenshots"] is IEnumerable<dynamic> screenshotsDynamic)
            {
                screenshots = screenshotsDynamic.Select(s => (string?)s.path_full);
            }

            item.Value.Name = dlcName;
            item.Value.DetailedDescription = detailedDescription;
            item.Value.ReleaseDate = releaseDate;
            item.Value.HeaderImage = headerImage;

            // Create a JArray from the screenshots IEnumerable<string?>
            if (screenshots != null)
            {
                JArray? screenshotsJArray = new(screenshots);
                item.Value.Screenshots = screenshotsJArray;
            }
        }
    }
}