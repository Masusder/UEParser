using ReactiveUI;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using UEParser.Kraken;
using System.Threading.Tasks;
using System;

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

    public ICommand? FetchDataCommand { get; }

    public APIViewModel()
    {
        FetchDataCommand = ReactiveCommand.Create(FetchData);
        ConstructFullVersion();
    }

    private async Task FetchData()
    {
        try
        {
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Running);
            await KrakenAPI.UpdateKrakenApi();
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
        }
        catch (Exception ex)
        {
            LogsWindowViewModel.Instance.AddLog(ex.Message, Logger.LogTags.Error);
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
        }
    }

    public void ConstructFullVersion()
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