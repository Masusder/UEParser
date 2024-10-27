using System;
using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using UEParser.Services;

namespace UEParser.ViewModels;

public class InitializationConfirmPopupViewModel : ReactiveObject
{
    public event Action<bool>? CloseAction; // Event to notify the view to close the popup

    private string? _currentVersion;
    public string? CurrentVersion
    {
        get => _currentVersion;
        set => this.RaiseAndSetIfChanged(ref _currentVersion, value);
    }

    private string? _compareVersion;
    public string? CompareVersion
    {
        get => _compareVersion;
        set => this.RaiseAndSetIfChanged(ref _compareVersion, value);
    }

    private string? _canContinue;
    public string? CanContinue
    {
        get => _canContinue;
        set => this.RaiseAndSetIfChanged(ref _canContinue, value);
    }

    public ReactiveCommand<Unit, Unit> YesCommand { get; }
    public ReactiveCommand<Unit, Unit> NoCommand { get; }

    public InitializationConfirmPopupViewModel()
    {
        var config = ConfigurationService.Config;
        string pathToGameDirectory = config.Core.PathToGameDirectory;

        CurrentVersion = SetVersion();
        CompareVersion = SetVersion(true);

        // Block initialization if current version build isn't defined, same for path to game directory
        var canExecuteYesCommand = this.WhenAnyValue(x => x.CurrentVersion)
                                        .Select(version => !string.IsNullOrEmpty(version) && version != "---" && !string.IsNullOrEmpty(pathToGameDirectory));
        YesCommand = ReactiveCommand.Create(OnYesClicked, canExecuteYesCommand);
        NoCommand = ReactiveCommand.Create(OnNoClicked);
    }

    private static string SetVersion(bool isCompareVersion = false)
    {
        string version = isCompareVersion ? GlobalVariables.CompareVersionWithBranch : GlobalVariables.VersionWithBranch;

        if (string.IsNullOrEmpty(version) || version.StartsWith('_'))
        {
            version = "---";
        }

        return version;
    }

    private void OnYesClicked()
    {
        CloseAction?.Invoke(true);
    }

    private void OnNoClicked()
    {
        CloseAction?.Invoke(false);
    }
}