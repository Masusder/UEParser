using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System;
using System.Linq;
using UEParser.Services;
using UEParser.ViewModels;
using System.Text.RegularExpressions;
using UEParser.Network.Steam;

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
            AppAuthTicket? authTicket = steamAuthenticator.AuthTicket;
            if (authTicket != null)
            {
                string authTicketString = ByteSessionTokenToString(authTicket.Ticket);
#if DEBUG
                LogsWindowViewModel.Instance.AddLog($"Stored Auth Ticket: {authTicketString}", Logger.LogTags.Debug);
#endif

                string dbdLoginUrl = ConstructApiUrl("loginWithSteamTokenBody");

                var config = ConfigurationService.Config;

                string latestVersion = config.Core.ApiConfig.LatestVersion;
                string krakenApiVersion = DeconstructKrakenApiVersion(latestVersion);
                string s3AccessKey = config.Core.ApiConfig.S3AccessKeys[krakenApiVersion];

                var headers = new Dictionary<string, string>
                {
                    { "x-kraken-content-secret-key", s3AccessKey },
                    { "x-kraken-content-version", JsonConvert.SerializeObject(new { contentVersionId = latestVersion }) },
                    { "x-kraken-client-resolution", "2560x1440" },
                    { "x-kraken-client-platform", "steam" },
                    { "x-kraken-client-provider", "steam" }
                };

                // Define optional payload
                var loginPayload = new
                {
                    token = authTicketString
                };

                Network.API.ApiResponse response = await Network.API.PostRequest(dbdLoginUrl, headers, loginPayload);

                if (!response.Success)
                {
                    throw new Exception("Failed to authenticate with DBD.");
                }
                else
                {
                    LogsWindowViewModel.Instance.AddLog("Succesfully authenticated with DBD.", Logger.LogTags.Success);
                }

                // Extract and set the krakenSession cookie if present
                //var responseCookies = Network.API.GetCookies(new Uri(dbdLoginUrl));
                //var krakenSessionCookie = responseCookies["krakenSession"];

                //if (krakenSessionCookie != null)
                //{
                //    Network.API.SetCookie(new Uri(dbdLoginUrl), new Cookie("krakenSession", krakenSessionCookie.Value));
                //}
                //else
                //{
                //    LogsWindowViewModel.Instance.AddLog("Not found Kraken sesssion cookie.", Logger.LogTags.Error);
                //}
            }
            //else
            //{
            //    LogsWindowViewModel.Instance.AddLog("Auth Ticket not available.", Logger.LogTags.Warning);
            //}
        });
    }

    [GeneratedRegex(@"^(?<version>\d+\.\d+\.\d+)_.+(?<environment>live|qa|stage|dev|cert|uat)$")]
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

            LogsWindowViewModel.Instance.AddLog($"Fetching Full Profile.", Logger.LogTags.Info);

            Network.API.ApiResponse response = await Network.API.FetchUrl(fullProfileUrl);

            if (response.Success)
            {
                LogsWindowViewModel.Instance.AddLog($"Decrypting Full Profile.", Logger.LogTags.Info);

                string branch = config.Core.VersionData.Branch.ToString();

                string decodedData = DbdDecryption.DecryptCDN(response.Data, branch);

                LogsWindowViewModel.Instance.AddLog($"Saving Full Profile.", Logger.LogTags.Info);

                string outputPath = Path.Combine(GlobalVariables.pathToKrakenApi, "fullProfile.json");

                File.WriteAllText(outputPath, decodedData);
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to fetch player's Full Profile state: {ex.Message}", ex);
        }
    }
}