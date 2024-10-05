using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Windowing;
using UEParser.Services;
using UEParser.Network.Kraken;
using UEParser.ViewModels;
using UEParser.Views;

namespace UEParser;

public partial class MainWindow : AppWindow
{
    public MainWindow()
    {
        InitializeComponent();
        SplashScreen = new MainAppSplashScreen();
        DataContext = new MainWindowViewModel();
        Loaded += MainWindow_Loaded;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key == Key.Escape)
        {
            // Handle ESC key globally
            CancellationTokenService.Instance.Cancel();
        }
    }

    private InitializationConfirmPopupView? _confirmationPopup;

    private async void MainWindow_Loaded(object? sender, EventArgs e)
    {
        var (hasVersionChanged, buildVersion, isVersionConfigured) = Initialize.CheckBuildVersion();

        var config = ConfigurationService.Config;
        bool updateAPIDuringInitialization = config.Global.UpdateAPIDuringInitialization;

        if (updateAPIDuringInitialization)
        {
            LogsWindowViewModel.Instance.AddLog("You have set up the application to check for Kraken API updates during initialization.", Logger.LogTags.Info);

            try
            {
                await KrakenManager.UpdateKrakenApi();
            }
            catch (Exception ex)
            {
                LogsWindowViewModel.Instance.AddLog($"Error occured, if you haven't initialized app yet this error may be ignored. {ex.Message}", Logger.LogTags.Error);
                LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            }
        }

        if (hasVersionChanged)
        {
            _confirmationPopup = new InitializationConfirmPopupView();
            var result = await _confirmationPopup.ShowDialog<bool>(this);
            _confirmationPopup = null;

            if (result)
            {
                await InitializeMain(hasVersionChanged, buildVersion);
            }
            else
            {
                LogsWindowViewModel.Instance.AddLog("App isn't initialized with detected build version. Errors may occur.", Logger.LogTags.Warning);
                LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Warning);
            }
        }
        else if (isVersionConfigured)
        {
            LogsWindowViewModel.Instance.AddLog("No new Dead by Daylight build has been detected.", Logger.LogTags.Info);
        }
    }

    private static async Task InitializeMain(bool hasVersionChanged, string buildVersion)
    {
        try
        {
            await Initialize.UpdateApp(hasVersionChanged, buildVersion);
        }
        catch (Exception ex)
        {
            LogsWindowViewModel.Instance.AddLog($"Fatal error! Initialization failed! {ex.Message}", Logger.LogTags.Error);
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
        }
    }

    internal class MainAppSplashScreen() : IApplicationSplashScreen
    {
        public string? AppName { get; }
        public IImage? AppIcon { get; }
        public object SplashScreenContent => new SplashScreenView();
        public int MinimumShowTime => 2000;

        public Action? InitApp { get; set; }

        public Task RunTasks(CancellationToken cancellationToken)
        {
            if (InitApp == null)
                return Task.CompletedTask;

            return Task.Run(InitApp, cancellationToken);
        }
    }
}