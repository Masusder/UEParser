using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using UEParser.Services;
using UEParser.ViewModels;
using UEParser.Network.Steam;
using UEParser.Parser;

namespace UEParser.Network.Kraken.API;

public partial class KrakenAPI
{
    public class KrakenVersionData
    {
        public required Dictionary<string, string> AvailableVersions { get; set; }
    }

    public static string ConstructApiUrl(string endpoint, Dictionary<string, string>? queryParams = null)
    {
        var config = ConfigurationService.Config;

        string branch = config.Core.VersionData.Branch.ToString();
        string apiBaseUrl = config.Core.ApiConfig.ApiBaseUrl;

        // Branches live/ptb/qa use different base url, change it here
        string[] branchesToCheck = ["live", "ptb", "qa"];
        if (branchesToCheck.Contains(branch))
        {
            apiBaseUrl = config.Core.ApiConfig.SteamApiBaseUrl;
        }

        // Check if the endpoint exists in the configuration
        if (!config.Core.ApiConfig.ApiEndpoints.TryGetValue(endpoint, out string? apiEndpoint))
        {
            throw new ArgumentException($"Endpoint '{endpoint}' not found in configuration.");
        }

        string apiFullUrl = string.Format(apiBaseUrl, branch) + apiEndpoint;

        // Add query parameters if provided
        if (queryParams != null && queryParams.Count > 0)
        {
            if (apiFullUrl.Contains('?'))
            {
                apiFullUrl += '&';
            }
            else
            {
                apiFullUrl += '?';
            }

            foreach (var param in queryParams)
            {
                apiFullUrl += $"{param.Key}={param.Value}&";
            }

            apiFullUrl = apiFullUrl.TrimEnd('&');
        }

        return apiFullUrl;
    }

    private static string ByteSessionTokenToString(byte[] buffer)
    {
        var token = BitConverter.ToString(buffer, 0, (int)buffer.Length);
        return token.Replace("-", string.Empty);
    }

    public static async Task AuthenticateWithDbd()
    {
        await Task.Run(async () =>
        {
            var steamAuthenticator = new SteamAuthenticator();

            // Start the authentication process
            await steamAuthenticator.AuthenticateAsync();

            // Access the stored auth ticket
            var authTicket = steamAuthenticator.AuthTicket;

            if (authTicket != null)
            {
                string authTicketString = ByteSessionTokenToString(authTicket);
#if DEBUG
                LogsWindowViewModel.Instance.AddLog($"Stored Auth Ticket: {authTicketString}", Logger.LogTags.Debug);
#endif
                string dbdLoginUrl = ConstructApiUrl("loginWithSteamTokenBody");

                var config = ConfigurationService.Config;

                string latestVersion = config.Core.ApiConfig.LatestVersion;
                string krakenApiVersion = DeconstructKrakenApiVersion(latestVersion);

                config.Core.ApiConfig.S3AccessKeys.TryGetValue(krakenApiVersion, out string? s3AccessKey);

                if (string.IsNullOrEmpty(s3AccessKey)) throw new Exception("S3 Access Key was not present.");

                var headers = new Dictionary<string, string>
                {
                    { "x-kraken-content-secret-key", s3AccessKey },
                    { "x-kraken-content-version", JsonConvert.SerializeObject(new { contentVersionId = latestVersion }) },
                    { "x-kraken-client-resolution", "2560x1440" },
                    { "x-kraken-client-platform", "steam" },
                    { "x-kraken-client-provider", "steam" }
                };

                var loginPayload = new
                {
                    token = authTicketString
                };

                NetAPI.ApiResponse response = await NetAPI.PostRequest(dbdLoginUrl, headers, loginPayload);

                if (!response.Success)
                {
                    throw new Exception("Failed to authenticate with Dead by Daylight.");
                }
                else
                {
                    LogsWindowViewModel.Instance.AddLog("Succesfully authenticated with Dead by Daylight.", Logger.LogTags.Success);
                }
            }
            else
            {
                throw new Exception("Auth Ticket not available.");
            }
        });
    }

    [GeneratedRegex(@"^(?<version>\d+\.\d+\.\d+)_.+(?<environment>live|qa|stage|dev|cert|uat|ptb)$")]
    private static partial Regex GetVersionAndEnvironmentRegex();

    public static string DeconstructKrakenApiVersion(string latestVersion)
    {
        var regex = GetVersionAndEnvironmentRegex();

        Match match = regex.Match(latestVersion);
        if (!match.Success)
        {
            return "";
        }

        string version = match.Groups["version"].Value;
        string environment = match.Groups["environment"].Value;

        return $"{version}_{environment}";
    }

    public static async Task GetPlayerFullProfileState()
    {
        var config = ConfigurationService.Config;

        try
        {
            string fullProfileUrl = ConstructApiUrl("getPlayerFullProfileState");

            LogsWindowViewModel.Instance.AddLog($"Fetching and saving Full Profile..", Logger.LogTags.Info);

            NetAPI.ApiResponse response = await NetAPI.FetchUrl(fullProfileUrl);

            if (response.Success)
            {
                string branch = config.Core.VersionData.Branch.ToString();
                // Unlike other requests, Full Profile response needs to be decoded
                string decodedData = DbdDecryption.DecryptCDN(response.Data, branch);

                FileWriter.SaveApiResponseToFile(decodedData, "fullProfile.json");
            }
            else
            {
                throw new Exception($"API request failed - {response.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to fetch player's Full Profile state: {ex.Message}", ex);
        }
    }

    private class CharacterDataPayload
    {
        [JsonProperty("ownedCharacters")]
        public List<string> OwnedCharacters { get; set; } = [];
    }

    public static async Task GetCharacterData()
    {
        try
        {
            string characterDataUrl = ConstructApiUrl("dbdCharacterDataGetAll");

            LogsWindowViewModel.Instance.AddLog($"Fetching and saving Characters Data..", Logger.LogTags.Info);

            CharacterDataPayload payload = new(); // Empty owned characters payload retrieves all characters

            NetAPI.ApiResponse response = await NetAPI.PostRequest(characterDataUrl, null, payload);

            if (response.Success)
            {
                FileWriter.SaveApiResponseToFile(response.Data, "charactersData.json");
            }
            else
            {
                throw new Exception($"API request failed - {response.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"An error occurred while fetching character data: {ex.Message}", ex);
        }
    }

    public static async Task BulkGetKrakenEndpoints(Dictionary<string, string> endpoints)
    {
        foreach (var endpoint in endpoints)
        {
            await GetKrakenEndpoint(endpoint.Key, endpoint.Value);
        }
    }

    // Method for fetching general GET requests that don't need special treatment
    public static async Task GetKrakenEndpoint(string endpoint, string endpointPrettyName)
    {
        string url = ConstructApiUrl(endpoint);

        try
        {
            LogsWindowViewModel.Instance.AddLog($"Fetching and saving {endpointPrettyName}..", Logger.LogTags.Info);

            NetAPI.ApiResponse response = await NetAPI.FetchUrl(url);

            if (response.Success)
            {
                FileWriter.SaveApiResponseToFile(response.Data, endpoint + ".json");
            }
            else
            {
                throw new Exception($"API request failed - {response.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to fetch {endpointPrettyName}: {ex.Message}", ex);
        }
    }
}