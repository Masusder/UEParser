using System;
using System.Windows.Input;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using ReactiveUI;
using UEParser.Network.Kraken;

namespace UEParser.ViewModels;

public class APIViewModel
{
    private string _version = "";

    public string Version
    {
        get => _version;
        set
        {
            if (_version != value)
            {
                _version = value;
                OnPropertyChanged();
            }
        }
    }

    public ICommand FetchAPICommand { get; }
    public ICommand DownloadDynamicAssetsCommand { get; }
    public ICommand SteamLoginCommand { get; }

    public APIViewModel()
    {
        FetchAPICommand = ReactiveCommand.Create(FetchData);
        DownloadDynamicAssetsCommand = ReactiveCommand.Create(DownloadDynamicAssets);
        SteamLoginCommand = ReactiveCommand.Create(SteamLogin);
        ConstructFullVersion();
    }

    private async Task SteamLogin()
    {
        try
        {
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Running);

            await KrakenManager.RetrieveKrakenApiAuthenticated();

            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
        }
        catch (Exception ex)
        {
            LogsWindowViewModel.Instance.AddLog(ex.Message, Logger.LogTags.Error);
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
        }
    }

    private async Task FetchData()
    {
        try
        {
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Running);

            await KrakenManager.UpdateKrakenApi();

            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
        }
        catch (Exception ex)
        {
            LogsWindowViewModel.Instance.AddLog(ex.Message, Logger.LogTags.Error);
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
        }
    }

    private async Task DownloadDynamicAssets()
    {
        try
        {
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Running);

            await KrakenManager.DownloadDynamicContent();

            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
        }
        catch (Exception ex)
        {
            LogsWindowViewModel.Instance.AddLog(ex.Message, Logger.LogTags.Error);
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
        }
    }

    private void ConstructFullVersion()
    {
        string fullVersion = $"Selected version: {GlobalVariables.versionWithBranch}";
        Version = fullVersion;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}