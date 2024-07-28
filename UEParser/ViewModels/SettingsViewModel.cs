using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ReactiveUI;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using UEParser.Services;
using UEParser.Views;
using Avalonia;
using System.Web;
using System.Collections.Generic;
using UEParser.Models;
using System.Linq;
using System.Text.RegularExpressions;
using Avalonia.Data;

namespace UEParser.ViewModels;

public partial class SettingsViewModel : INotifyPropertyChanged
{
    private string? _pathToGameDirectory;
    public string? PathToGameDirectory
    {
        get => _pathToGameDirectory;
        set => SetProperty(ref _pathToGameDirectory, value);
    }

    private string? _pathToMappings;
    public string? PathToMappings
    {
        get => _pathToMappings;
        set => SetProperty(ref _pathToMappings, value);
    }

    private bool _updateApiDuringInitialization = false;
    public bool UpdateApiDuringInitialization
    {
        get => _updateApiDuringInitialization;
        set => SetProperty(ref _updateApiDuringInitialization, value);
    }

    private Branch _selectedCurrentBranch;
    public Branch SelectedCurrentBranch
    {
        get => _selectedCurrentBranch;
        set => SetProperty(ref _selectedCurrentBranch, value);
    }

    private Branch _selectedComparisonBranch;
    public Branch SelectedComparisonBranch
    {
        get => _selectedComparisonBranch;
        set => SetProperty(ref _selectedComparisonBranch, value);

    }

    private string? _selectedCurrentVersion;
    public string? SelectedCurrentVersion
    {
        get => _selectedCurrentVersion;
        set 
        {
            if (SetProperty(ref _selectedCurrentVersion, value))
            {
                if (!IsValidVersion(value))
                {
                    throw new DataValidationException("Invalid version");
                }
            }
        }
    }

    private string? _selectedComparisonVersion;
    public string? SelectedComparisonVersion
    {
        get => _selectedComparisonVersion;
        set
        {
            if (SetProperty(ref _selectedComparisonVersion, value))
            {
                if (!IsValidVersion(value))
                {
                    throw new DataValidationException("Invalid version");
                }
            }
        }
    }

    public static Branch[] Branches => Enum.GetValues(typeof(Branch)).Cast<Branch>().ToArray();

    public ICommand? OpenDirectoryDialogCommand { get; }
    public ICommand? SaveSettingsCommand { get; }

    public SettingsViewModel()
    {
        var config = ConfigurationService.Config;
        PathToGameDirectory = config.Core.PathToGameDirectory;
        PathToMappings = config.Core.MappingsPath;
        SelectedCurrentBranch = config.Core.VersionData.Branch;
        SelectedComparisonBranch = config.Core.VersionData.CompareBranch;
        SelectedCurrentVersion = config.Core.VersionData.LatestVersionHeader;
        SelectedComparisonVersion = config.Core.VersionData.CompareVersionHeader;
        UpdateApiDuringInitialization = config.Global.UpdateAPIDuringInitialization;
        OpenDirectoryDialogCommand = ReactiveCommand.CreateFromTask<string>(OpenDirectoryDialog);

        var canSave = this.WhenAnyValue(
            x => x.SelectedCurrentVersion,
            x => x.SelectedComparisonVersion,
            (currentVersion, comparisonVersion) => IsValidVersion(currentVersion) && IsValidVersion(comparisonVersion)
        );

        SaveSettingsCommand = ReactiveCommand.CreateFromTask(SaveSettings, canSave);
    }

    private async Task SaveSettings()
    {
        bool userConfirmedRestart = await ShowRestartPopup();
        if (userConfirmedRestart)
        {
            var config = ConfigurationService.Config;
            config.Core.PathToGameDirectory = PathToGameDirectory ?? "";
            config.Core.MappingsPath = PathToMappings ?? "";
            config.Global.UpdateAPIDuringInitialization = UpdateApiDuringInitialization;
            config.Core.VersionData.Branch = SelectedCurrentBranch;
            config.Core.VersionData.CompareBranch = SelectedComparisonBranch;
            config.Core.VersionData.LatestVersionHeader = SelectedCurrentVersion;
            config.Core.VersionData.CompareVersionHeader = SelectedComparisonVersion;
            await ConfigurationService.SaveConfiguration();
            RestartApplication();
        }
    }

    [GeneratedRegex(@"^[0-9]+(\.[0-9]+)*$")]
    private static partial Regex VersionRegex();
    private static bool IsValidVersion(string? version)
    {
        if (string.IsNullOrWhiteSpace(version)) return true;

        var regex = VersionRegex();
        return regex.IsMatch(version);
    }

    private static void RestartApplication()
    {
        (Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.MainWindow?.Close();

        // Start a new instance of the application
        System.Diagnostics.Process.Start(Environment.ProcessPath ?? throw new Exception());

        Environment.Exit(0);
    }


    private static async Task<bool> ShowRestartPopup()
    {
        var viewModel = new RestartApplicationPopupViewModel();
        var view = new RestartApplicationPopup
        {
            DataContext = viewModel
        };
        var tcs = new TaskCompletionSource<bool>();

        view.Closed += (sender, e) =>
        {
            bool userConfirmedRestart = viewModel.UserConfirmedRestart;
            tcs.SetResult(userConfirmedRestart);
        };

        view.Show();

        //bool userConfirmedRestart = viewModel.UserConfirmedRestart;

        return await tcs.Task;
    }

    private async Task OpenDirectoryDialog(string propertyName)
    {
        var window = new Window();

        var storage = window.StorageProvider;

        var result = await storage.OpenFolderPickerAsync(new FolderPickerOpenOptions { AllowMultiple = false});
        if (result.Count > 0)
        {
            string selectedDirectoryPath = result[0].Path.AbsolutePath;
            string decodedPath = HttpUtility.UrlDecode(selectedDirectoryPath);

            //PathToGameDirectory = decodedPath;

            UpdateProperty(propertyName, decodedPath);
        }
    }

    private void UpdateProperty(string propertyName, string path)
    {
        var property = GetType().GetProperty(propertyName);
        if (property != null && property.PropertyType == typeof(string))
        {
            property.SetValue(this, path);
            OnPropertyChanged(propertyName);
        }
    }

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}