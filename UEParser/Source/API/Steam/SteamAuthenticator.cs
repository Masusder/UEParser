//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;
//using SteamKit2;
//using SteamKit2.Internal;
//using UEParser.Services;
//using UEParser.ViewModels;
//using System.Diagnostics;

//namespace UEParser.Network.Steam;

//public class SteamAuthenticator
//{
//    private readonly ConcurrentQueue<byte[]> gcTokens;
//    private readonly TaskCompletionSource<bool> gcTokensComplete;

//    private readonly SteamClient steamClient;
//    private readonly CallbackManager manager;
//    private readonly SteamUser? steamUser;
//    private readonly SteamApps? steamApps;
//    private TaskCompletionSource<bool>? authenticationCompletionSource;

//    private byte[]? authTicket;
//    public byte[]? AuthTicket => authTicket;

//    public SteamAuthenticator()
//    {
//        steamClient = new SteamClient();
//        manager = new CallbackManager(steamClient);
//        steamUser = steamClient.GetHandler<SteamUser>();
//        steamApps = steamClient.GetHandler<SteamApps>();

//        gcTokens = new ConcurrentQueue<byte[]>();
//        gcTokensComplete = new TaskCompletionSource<bool>();

//        // Setup the callbacks
//        manager.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
//        manager.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnected);
//        manager.Subscribe<SteamUser.LoggedOnCallback>(OnLoggedOn);
//        manager.Subscribe<SteamUser.LoggedOffCallback>(OnLoggedOff);
//        manager.Subscribe<SteamApps.GameConnectTokensCallback>(OnGcTokens);
//    }

//    public async Task AuthenticateAsync()
//    {
//        // Initialize the completion source
//        authenticationCompletionSource = new TaskCompletionSource<bool>();

//        if (steamClient.IsConnected) // Check if already connected
//        {
//            steamClient.Disconnect(); // Disconnect if already connected
//        }

//        // Start the connection to Steam
//        steamClient.Connect();

//        // Wait for authentication to complete
//        // Run the callback loop in a separate task
//        await Task.Run(() =>
//        {
//            while (authenticationCompletionSource?.Task.IsCompleted == false)
//            {
//                manager.RunWaitCallbacks(TimeSpan.FromSeconds(1));
//            }
//        });
//    }

//    private void OnGcTokens(SteamApps.GameConnectTokensCallback obj)
//    {
//        foreach (var token in obj.Tokens) gcTokens.Enqueue(token);
//        while (gcTokens.Count > obj.TokensToKeep) gcTokens.TryDequeue(out _);
//        gcTokensComplete.TrySetResult(true);
//    }

//    private void OnConnected(SteamClient.ConnectedCallback callback)
//    {
//        LogsWindowViewModel.Instance.AddLog("Connected to Steam! Logging in..", Logger.LogTags.Info);

//        var config = ConfigurationService.Config;
//        string? steamUsername = config.Sensitive.SteamUsername;
//        string? steamPassword = config.Sensitive.SteamPassword;

//        if (string.IsNullOrEmpty(steamUsername) || string.IsNullOrEmpty(steamPassword)) throw new Exception("Missing Steam credentials");

//        // Log in using provided credentials
//        steamUser?.LogOn(new SteamUser.LogOnDetails
//        {
//            Username = steamUsername,
//            Password = steamPassword
//        });
//    }

//    private void OnDisconnected(SteamClient.DisconnectedCallback callback)
//    {
//        LogsWindowViewModel.Instance.AddLog("Disconnected from Steam.", Logger.LogTags.Info);
//    }

//    private async void OnLoggedOn(SteamUser.LoggedOnCallback callback)
//    {
//        if (callback.Result == EResult.OK)
//        {
//            LogsWindowViewModel.Instance.AddLog("Successfully logged in to Steam.", Logger.LogTags.Info);

//            try
//            {
//                authTicket = await GetAuthSessionTicket(new GameID(381210));
//                authenticationCompletionSource?.TrySetResult(true);
//            }
//            catch (Exception ex)
//            {
//                LogsWindowViewModel.Instance.AddLog($"Error getting auth ticket: {ex.Message}", Logger.LogTags.Error);
//                authenticationCompletionSource?.TrySetResult(false);
//            }
//        }
//        else
//        {
//            LogsWindowViewModel.Instance.AddLog($"Failed to log in: {callback.Result}", Logger.LogTags.Error);
//            authenticationCompletionSource?.TrySetResult(false);
//        }
//    }

//    public async Task<byte[]> GetAuthSessionTicket(GameID gameId)
//    {
//        await gcTokensComplete.Task;
//        if (!gcTokens.TryDequeue(out var token)) throw new Exception("Failed to get gc token.");

//        var ticket = new MemoryStream();
//        using var writer = new BinaryWriter(ticket);

//        CreateAuthTicket(token, writer);

//        var appTicket = await steamApps?.GetAppOwnershipTicket(gameId.AppID)! ?? throw new Exception("Failed to get app ticket.");

//        writer.Write(appTicket.Ticket.Length);
//        writer.Write(appTicket.Ticket);

//        return ticket.ToArray();
//    }

//    private static void CreateAuthTicket(byte[] gcToken, BinaryWriter stream)
//    {
//        stream.Write(gcToken.Length);
//        stream.Write(gcToken.ToArray());

//        stream.Write(24);   // length
//        stream.Write(1);    // unk 1
//        stream.Write(2);    // unk 2
//        stream.Write(0);    // pub ip addr
//        stream.Write(0);    // padding
//        stream.Write((uint)Stopwatch.GetTimestamp() / (Stopwatch.Frequency / 1000)); // ms connected
//        stream.Write(1);    // connection count
//    }

//    private void OnLoggedOff(SteamUser.LoggedOffCallback callback)
//    {
//        LogsWindowViewModel.Instance.AddLog($"Logged off from Steam: {callback.Result}", Logger.LogTags.Info);
//    }
//}

using SteamKit2;
using SteamKit2.Internal;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using UEParser.ViewModels;
using UEParser.Services;
using System.Collections.Generic;
using Force.Crc32;
using System.Runtime.InteropServices;
using System.Linq;
using System.IO;
using System.Diagnostics;
using System.Buffers;
using System.Security.Cryptography;
using System.Threading;
using LicenseList = System.Collections.Generic.List<SteamKit2.SteamApps.LicenseListCallback.License>;

namespace UEParser.Network.Steam;

// Steam app authentication
// Based on https://github.com/jesterret/SteamUserAuthToken library
public class AppAuthTicket : IDisposable
{
    internal uint TicketCRC { get; }
    public uint AppID { get; }
    public byte[] Ticket { get; }
    internal AppAuthTicket(SteamAuthenticator handler, uint appID, byte[] ticket)
    {
        _handler = handler;
        AppID = appID;
        Ticket = ticket;
        TicketCRC = Crc32Algorithm.Compute(ticket);
    }

    public void Dispose() => _handler.CancelAuthTicket(this);

    private readonly SteamAuthenticator _handler;
}

public class TicketAckCallback : CallbackMsg
{
    public List<uint> AppIDs { get; }
    public List<uint> TicketCRCs { get; }
    public uint MessageSequence { get; }

    internal TicketAckCallback(JobID targetJobID, CMsgClientAuthListAck body)
    {
        JobID = targetJobID;
        AppIDs = body.app_ids;
        TicketCRCs = body.ticket_crc;
        MessageSequence = body.message_sequence;
    }
}

public interface IUserDataSerializer
{
    void Serialize(BinaryWriter writer);
}

public class DefaultAuthTicketBuilder(IUserDataSerializer userDataSerializer) : SteamAuthenticator.IAuthTicketBuilder
{
    public byte[] Build(byte[] gameConnectToken)
    {
        const int sessionSize =
            4 + // unknown, always 1
            4 + // unknown, always 2
            4 + // public IP v4, optional
            4 + // private IP v4, optional
            4 + // timestamp & uint.MaxValue
            4;  // sequence

        using var stream = new MemoryStream(gameConnectToken.Length + 4 + sessionSize);
        using (var writer = new BinaryWriter(stream))
        {
            writer.Write(gameConnectToken.Length);
            writer.Write(gameConnectToken);

            writer.Write(sessionSize);
            writer.Write(1);
            writer.Write(2);

            _userDataSerializer.Serialize(writer);
            writer.Write((uint)Stopwatch.GetTimestamp());
            writer.Write(++_sequence);
        }
        return stream.ToArray();
    }

    private uint _sequence;
    private readonly IUserDataSerializer _userDataSerializer = userDataSerializer;
}

public class RandomUserDataSerializer : IUserDataSerializer
{
    static readonly RandomNumberGenerator _random = RandomNumberGenerator.Create();

    public void Serialize(BinaryWriter writer)
    {
        var bytes = ArrayPool<byte>.Shared.Rent(8);
        try
        {
            _random.GetNonZeroBytes(bytes);
            writer.Write(bytes, 0, 8);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(bytes);
        }
    }
}

public class SteamAuthenticator
{
    private TaskCompletionSource<bool>? authenticationCompletionSource;
    private readonly SteamClient steamClient;
    private readonly CallbackManager manager;
    private readonly SteamUser? steamUser;
    private readonly SteamApps? steamApps;

    public readonly TaskCompletionSource<bool> LicenseCompletionSource;
    public LicenseList Licenses;

    private AppAuthTicket? authTicket;
    public AppAuthTicket? AuthTicket => authTicket;

    private ConcurrentQueue<byte[]> GameConnectTokens { get; } = new ConcurrentQueue<byte[]>();
    private readonly TaskCompletionSource<bool> GameConnectTokensComplete;
    private ConcurrentDictionary<uint, List<CMsgAuthTicket>> TicketsByGame { get; } = new ConcurrentDictionary<uint, List<CMsgAuthTicket>>();

    private readonly object _ticketChangeLock = new();
    private readonly IAuthTicketBuilder _authTicketBuilder;

    public interface IAuthTicketBuilder
    {
        byte[] Build(byte[] gameConnectToken);
    }

    internal void CancelAuthTicket(AppAuthTicket authTicket)
    {
        lock (_ticketChangeLock)
        {
            if (TicketsByGame.TryGetValue(authTicket.AppID, out var tickets))
            {
                tickets.RemoveAll(x => x.ticket_crc == authTicket.TicketCRC);
            }
        }
        SendTickets();
    }

    public SteamAuthenticator()
    {
        steamClient = new SteamClient();
        manager = new CallbackManager(steamClient);
        steamUser = steamClient.GetHandler<SteamUser>();
        steamApps = steamClient.GetHandler<SteamApps>();

        GameConnectTokens = new ConcurrentQueue<byte[]>();
        GameConnectTokensComplete = new TaskCompletionSource<bool>();

        Licenses = [];
        LicenseCompletionSource = new TaskCompletionSource<bool>(false);

        // Setup the callbacks
        manager.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
        manager.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnected);
        manager.Subscribe<SteamUser.LoggedOnCallback>(OnLoggedOn);
        manager.Subscribe<SteamUser.LoggedOffCallback>(OnLoggedOff);
        manager.Subscribe<SteamApps.GameConnectTokensCallback>(OnGcTokens);
        manager.Subscribe<SteamApps.LicenseListCallback>(OnLicenseList);

        _authTicketBuilder = new DefaultAuthTicketBuilder(new RandomUserDataSerializer());
    }

    public SteamAuthenticator(IAuthTicketBuilder authTicketBuilder) : this() => _authTicketBuilder = authTicketBuilder;

    public SteamAuthenticator(IUserDataSerializer userDataSerializer) : this(new DefaultAuthTicketBuilder(userDataSerializer)) { }

    private static byte[] BuildToken(byte[] authTicket, byte[] appTicket)
    {
        var len = appTicket.Length;
        var token = new byte[authTicket.Length + 4 + len];
        var mem = token.AsSpan();
        authTicket.CopyTo(mem);
        MemoryMarshal.Write(mem[authTicket.Length..], in len);
        appTicket.CopyTo(mem[(authTicket.Length + 4)..]);
        return token;
    }

    public async Task<AppAuthTicket?> GetAuthSessionTicket(uint appid)
    {
        await GameConnectTokensComplete.Task;
        if (steamApps is not null && GameConnectTokens.TryDequeue(out var token))
        {
            var authTicket = _authTicketBuilder.Build(token);
            var ticket = await VerifyTicket(appid, authTicket, out var crc);
            var appTicket = await steamApps.GetAppOwnershipTicket(appid);

            // Verify just in case
            if (ticket.TicketCRCs.Any(x => x == crc) && appTicket.Result == EResult.OK)
            {
                var tok = BuildToken(authTicket, appTicket.Ticket);
                return new AppAuthTicket(this, appid, tok);
            }
        }

        return null;
    }

    private AsyncJob<TicketAckCallback> VerifyTicket(uint appID, byte[] authTicket, out uint crc)
    {
        crc = Crc32Algorithm.Compute(authTicket, 0, authTicket.Length);
        lock (_ticketChangeLock)
        {
            var items = TicketsByGame.GetOrAdd(appID, []);
            items.Add(new CMsgAuthTicket
            {
                gameid = appID,
                ticket = authTicket,
                ticket_crc = crc
            });
        }

        return SendTickets();
    }

    private const int TicketSendTimeoutMilliseconds = 20000;
    private AsyncJob<TicketAckCallback> SendTickets()
    {
        using var cts = new CancellationTokenSource(TicketSendTimeoutMilliseconds);
        var auth = new ClientMsgProtobuf<CMsgClientAuthList>(EMsg.ClientAuthList);
        auth.Body.tokens_left = (uint)GameConnectTokens.Count;
        lock (_ticketChangeLock)
        {
            auth.Body.app_ids.AddRange(TicketsByGame.Keys);
            // Flatten dictionary into ticket list
            auth.Body.tickets.AddRange(TicketsByGame.Values.SelectMany(x => x));
        }
        auth.SourceJobID = steamClient.GetNextJobID();
        //steamClient.Send(auth);
        var sendTask = Task.Run(() => steamClient.Send(auth), cts.Token);

        return new AsyncJob<TicketAckCallback>(steamClient, auth.SourceJobID);
    }

    public async Task AuthenticateAsync()
    {
        // Initialize the completion source
        authenticationCompletionSource = new TaskCompletionSource<bool>();

        //if (steamClient.IsConnected) // Check if already connected
        //{
        //    steamClient.Disconnect(); // Disconnect if already connected
        //}

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

    private void OnConnected(SteamClient.ConnectedCallback callback)
    {
        LogsWindowViewModel.Instance.AddLog("Connected to Steam! Logging in..", Logger.LogTags.Info);

        var config = ConfigurationService.Config;
        string? steamUsername = config.Sensitive.SteamUsername;
        string? steamPassword = config.Sensitive.SteamPassword;

        if (string.IsNullOrEmpty(steamUsername) || string.IsNullOrEmpty(steamPassword)) throw new Exception("Missing Steam credentials.");

        // Log in using provided credentials
        steamUser?.LogOn(new SteamUser.LogOnDetails
        {
            Username = steamUsername,
            Password = steamPassword
        });
    }

    private void OnGcTokens(SteamApps.GameConnectTokensCallback obj)
    {
        foreach (var token in obj.Tokens) GameConnectTokens.Enqueue(token);
        while (GameConnectTokens.Count > obj.TokensToKeep) GameConnectTokens.TryDequeue(out _);
        GameConnectTokensComplete.TrySetResult(true);
    }

    private async void OnLoggedOn(SteamUser.LoggedOnCallback callback)
    {
        if (callback.Result == EResult.OK)
        {
            LogsWindowViewModel.Instance.AddLog("Successfully logged in to Steam.", Logger.LogTags.Info);

            try
            {
                uint appId = 381210;
                authTicket = await GetAuthSessionTicket(appId);
                authenticationCompletionSource?.TrySetResult(true);
            }
            catch (Exception ex)
            {
                LogsWindowViewModel.Instance.AddLog($"Error getting auth ticket: {ex.Message}", Logger.LogTags.Error);
                authenticationCompletionSource?.TrySetResult(true);
            }
        }
        else
        {
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
    }

    private void OnLicenseList(SteamApps.LicenseListCallback obj)
    {
        if (obj.Result != EResult.OK) return;

        Licenses.Clear();
        Licenses.AddRange(obj.LicenseList);

        if (!LicenseCompletionSource.Task.IsCompleted)
            LicenseCompletionSource.TrySetResult(true);
    }
}