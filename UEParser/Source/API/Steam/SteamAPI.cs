using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UEParser.ViewModels;

namespace UEParser.Network.Steam;

public class SteamAPI
{
    private const int MaxRetries = 5;

    public static async Task FetchDlcDetails(string steamId, string dlcId, string languageCode)
    {
        Dictionary<string, string> supportedLanguages = new()
        {
            {"ja", "japanese"},
            {"ko", "korean"},
            {"pl", "polish"},
            {"pt-BR", "brazilian"},
            {"ru", "russian"},
            {"th", "thai"},
            {"tr", "turkish"},
            {"zh-Hans", "schinese"},
            {"zh-Hant", "tchinese"},
            {"de", "german"},
            {"en", "english"},
            {"es", "spanish"},
            {"es-MX", "spanish"},
            {"fr", "french"},
            {"it", "italian"}
        };

        if (!string.IsNullOrEmpty(steamId))
        {
            string dlcDirectory = Path.Combine(GlobalVariables.rootDir, "Dependencies", "HelperComponents", "DLC", languageCode);
            Directory.CreateDirectory(dlcDirectory);
            string dlcFilePath = Path.Combine(dlcDirectory, $"{dlcId}.json");

            if (!File.Exists(dlcFilePath))
            {
                string apiUrl = $"https://store.steampowered.com/api/appdetails?appids={steamId}&l={supportedLanguages[languageCode]}";

                int retryCount = 0;
                bool success = false;

                while (retryCount < MaxRetries && !success)
                {
                    try
                    {
                        await Task.Delay(300); // Try to avoid rate-limit
                        NetAPI.ApiResponse response = await NetAPI.FetchUrl(apiUrl);
                        if (string.IsNullOrEmpty(response.Data))
                        {
                            throw new HttpRequestException("Response data is null or empty.");
                        }

                        JObject steamApiData = JObject.Parse(response.Data);
                        File.WriteAllText(dlcFilePath, steamApiData.ToString());
                        LogsWindowViewModel.Instance.AddLog($"Saved DLC data for SteamId '{steamId}' and language '{languageCode}'.", Logger.LogTags.Info);
                        success = true;
                    }
                    catch (HttpRequestException ex)
                    {
                        retryCount++;
                        LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
                        LogsWindowViewModel.Instance.AddLog($"Attempt {retryCount}/{MaxRetries}: Failed to fetch DLC data for SteamId {steamId} and language {languageCode}. Error: {ex.Message}", Logger.LogTags.Error);

                        if (retryCount >= MaxRetries)
                        {
                            LogsWindowViewModel.Instance.AddLog($"Exceeded maximum retry attempts for SteamId {steamId} and language {languageCode}. Most likely Steam rate-limit has been hit, you need to wait awhile.", Logger.LogTags.Error);
                            return;
                        }
                        else
                        {
                            await Task.Delay(ExponentialBackoff(retryCount));
                        }
                    }
                }
            }
        }
    }

    private static int ExponentialBackoff(int attempt)
    {
        // Exponential backoff with a maximum delay of 32 seconds
        return Math.Min((int)Math.Pow(2, attempt) * 1000, 32000);
    }
}