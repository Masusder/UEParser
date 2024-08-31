using System;
using System.Threading.Tasks;
using SteamKit2;
using UEParser.ViewModels;
using UEParser.Services;
using SteamKit2.Authentication;
using QRCoder;

namespace UEParser.Network.Steam;

// Credits to Jesterret for initial AuthSessionTicket implementation
public class SteamAuthenticator
{
    private readonly SteamClient steamClient;
    private readonly SteamUser steamUser;
    private readonly SteamAuthTicket steamAuthTicket;
    private readonly CallbackManager manager;
    private TaskCompletionSource<bool>? authenticationCompletionSource;

    private byte[]? authTicket;
    public byte[]? AuthTicket => authTicket;

    public SteamAuthenticator()
    {
        steamClient = new SteamClient();
        manager = new CallbackManager(steamClient);
        steamUser = steamClient.GetHandler<SteamUser>()!;
        steamAuthTicket = steamClient.GetHandler<SteamAuthTicket>()!;

        // Setup the callbacks
        manager.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
        manager.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnected);

        manager.Subscribe<SteamUser.LoggedOnCallback>(OnLoggedOn);
        manager.Subscribe<SteamUser.LoggedOffCallback>(OnLoggedOff);
    }

    public async Task AuthenticateAsync()
    {
        // Initialize the completion source
        authenticationCompletionSource = new TaskCompletionSource<bool>();

        // Start the connection to Steam
        steamClient.Connect();

        // Wait for authentication to complete
        // Run the callback loop in a separate task
        await Task.Run(() =>
        {
            while (authenticationCompletionSource?.Task.IsCompleted == false)
            {
                manager.RunWaitCallbacks(TimeSpan.FromSeconds(1));
            }
        });
    }

    async void OnConnected(SteamClient.ConnectedCallback callback)
    {
        // Start an authentication session by requesting a link
        var authSession = await steamClient.Authentication.BeginAuthSessionViaQRAsync(new AuthSessionDetails());

        // Steam will periodically refresh the challenge url, this callback allows you to draw a new qr code
        authSession.ChallengeURLChanged = () =>
        {
            LogsWindowViewModel.Instance.AddLog("Steam has refreshed the challenge url", Logger.LogTags.Info);

            DrawQRCode(authSession);
        };

        // Draw current qr right away
        DrawQRCode(authSession);

        // Starting polling Steam for authentication response
        // This response is later used to logon to Steam after connecting
        var pollResponse = await authSession.PollingWaitForResultAsync();

        LogsWindowViewModel.Instance.AddLog($"Logging in as '{pollResponse.AccountName}'...", Logger.LogTags.Info);

        // Logon to Steam with the access token we have received
        steamUser.LogOn(new SteamUser.LogOnDetails
        {
            Username = pollResponse.AccountName,
            AccessToken = pollResponse.RefreshToken,
        });
    }

    private async void OnLoggedOn(SteamUser.LoggedOnCallback callback)
    {
        if (callback.Result == EResult.OK)
        {
            LogsWindowViewModel.Instance.AddLog("Successfully logged in to Steam.", Logger.LogTags.Info);

            try
            {
                uint appId = 381210;
                var ticket = await steamAuthTicket.GetAuthSessionTicket(appId) ?? throw new Exception("Auth session ticket was null.");

                authTicket = ticket.Ticket;
                authenticationCompletionSource?.TrySetResult(true);

                steamUser.LogOff();
            }
            catch (Exception ex)
            {
                steamClient.Disconnect();
                steamUser.LogOff();

                LogsWindowViewModel.Instance.AddLog($"Error getting auth ticket: {ex.Message}", Logger.LogTags.Error);
                authenticationCompletionSource?.TrySetResult(true);
            }
        }
        else
        {
            steamClient.Disconnect();
            LogsWindowViewModel.Instance.AddLog($"Failed to log in: {callback.Result}", Logger.LogTags.Error);
            authenticationCompletionSource?.TrySetResult(true);
        }
    }

    private void OnLoggedOff(SteamUser.LoggedOffCallback callback)
    {
        LogsWindowViewModel.Instance.AddLog($"Logged off from Steam: {callback.Result}", Logger.LogTags.Info);
        authenticationCompletionSource?.TrySetResult(true);
    }

    private void OnDisconnected(SteamClient.DisconnectedCallback callback)
    {
        LogsWindowViewModel.Instance.AddLog("Disconnected from Steam.", Logger.LogTags.Info);
        authenticationCompletionSource?.TrySetResult(true);

        //if (!callback.UserInitiated)
        //    steamClient.Connect();
    }

    static void DrawQRCode(QrAuthSession authSession)
    {
        LogsWindowViewModel.Instance.AddLog($"Challenge URL: {authSession.ChallengeURL}", Logger.LogTags.Info);

        // Encode the link as a QR code
        using var qrGenerator = new QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode(authSession.ChallengeURL, QRCodeGenerator.ECCLevel.L);
        using var qrCode = new AsciiQRCode(qrCodeData);
        var qrCodeAsAsciiArt = qrCode.GetGraphic(1, drawQuietZones: true);

        LogsWindowViewModel.Instance.AddLog("Use the Steam Mobile App to sign in via QR code:", Logger.LogTags.Info);
        LogsWindowViewModel.Instance.AddLog(qrCodeAsAsciiArt, Logger.LogTags.Info);
    }
}