using System;
using System.Threading.Tasks;
using QRCoder;
using SteamKit2;
using SteamKit2.Authentication;
using UEParser.ViewModels;

namespace UEParser.Network.Steam;

// Credits to Jesterret for initial AuthSessionTicket implementation
public class SteamAuthenticator
{
    private readonly SteamClient _steamClient;
    private readonly SteamUser _steamUser;
    private readonly SteamAuthTicket _steamAuthTicket;
    private readonly CallbackManager _manager;
    private TaskCompletionSource<bool>? _authenticationCompletionSource;

    private byte[]? _authTicket;
    public byte[]? AuthTicket => _authTicket;

    public SteamAuthenticator()
    {
        _steamClient = new SteamClient();
        _manager = new CallbackManager(_steamClient);
        _steamUser = _steamClient.GetHandler<SteamUser>()!;
        _steamAuthTicket = _steamClient.GetHandler<SteamAuthTicket>()!;

        // Setup the callbacks
        _manager.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
        _manager.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnected);

        _manager.Subscribe<SteamUser.LoggedOnCallback>(OnLoggedOn);
        _manager.Subscribe<SteamUser.LoggedOffCallback>(OnLoggedOff);
    }

    public async Task AuthenticateAsync()
    {
        // Initialize the completion source
        _authenticationCompletionSource = new TaskCompletionSource<bool>();

        // Start the connection to Steam
        _steamClient.Connect();

        // Wait for authentication to complete
        // Run the callback loop in a separate task
        await Task.Run(() =>
        {
            while (_authenticationCompletionSource?.Task.IsCompleted == false)
            {
                _manager.RunWaitCallbacks(TimeSpan.FromSeconds(1));
            }
        });
    }

    async void OnConnected(SteamClient.ConnectedCallback callback)
    {
        // Start an authentication session by requesting a link
        var authSession = await _steamClient.Authentication.BeginAuthSessionViaQRAsync(new AuthSessionDetails());

        // Steam will periodically refresh the challenge url, this callback allows you to draw a new qr code
        authSession.ChallengeURLChanged = () =>
        {
            LogsWindowViewModel.Instance.AddLog("Steam has refreshed the challenge url.", Logger.LogTags.Info);

            DrawQRCode(authSession);
        };

        // Draw current qr right away
        DrawQRCode(authSession);

        // Starting polling Steam for authentication response
        // This response is later used to logon to Steam after connecting
        var pollResponse = await authSession.PollingWaitForResultAsync();

        LogsWindowViewModel.Instance.AddLog($"Logging in as '{pollResponse.AccountName}'..", Logger.LogTags.Info);

        // Logon to Steam with the access token we have received
        _steamUser.LogOn(new SteamUser.LogOnDetails
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
                var ticket = await _steamAuthTicket.GetAuthSessionTicket(appId) ?? throw new Exception("Auth session ticket was null.");

                _authTicket = ticket.Ticket;
                _authenticationCompletionSource?.TrySetResult(true);

                _steamUser.LogOff();
            }
            catch (Exception ex)
            {
                _steamClient.Disconnect();
                _steamUser.LogOff();

                LogsWindowViewModel.Instance.AddLog($"Error getting auth ticket: {ex.Message}", Logger.LogTags.Error);
                _authenticationCompletionSource?.TrySetResult(true);
            }
        }
        else
        {
            _steamClient.Disconnect();
            LogsWindowViewModel.Instance.AddLog($"Failed to log in: {callback.Result}", Logger.LogTags.Error);
            _authenticationCompletionSource?.TrySetResult(true);
        }
    }

    private void OnLoggedOff(SteamUser.LoggedOffCallback callback)
    {
        LogsWindowViewModel.Instance.AddLog($"Logged off from Steam: {callback.Result}", Logger.LogTags.Info);
        _authenticationCompletionSource?.TrySetResult(true);
    }

    private void OnDisconnected(SteamClient.DisconnectedCallback callback)
    {
        LogsWindowViewModel.Instance.AddLog("Disconnected from Steam.", Logger.LogTags.Info);
        _authenticationCompletionSource?.TrySetResult(true);
    }

    static void DrawQRCode(QrAuthSession authSession)
    {
        LogsWindowViewModel.Instance.AddLog($"Challenge URL: {authSession.ChallengeURL}", Logger.LogTags.Info);

        // Encode the link as a QR code
        using var qrGenerator = new QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode(authSession.ChallengeURL, QRCodeGenerator.ECCLevel.L);
        using var qrCode = new AsciiQRCode(qrCodeData);
        var qrCodeAsAsciiArt = qrCode.GetGraphic(1, drawQuietZones: false);

        LogsWindowViewModel.Instance.AddLog("Use the Steam Mobile App to sign in via QR code:", Logger.LogTags.Info);
        LogsWindowViewModel.Instance.AddLog("\n" + qrCodeAsAsciiArt, Logger.LogTags.Info);
    }
}