﻿using System;
using System.Web;
using System.Linq;
using System.Windows.Input;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Data;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ReactiveUI;
using UEParser.Views;
using UEParser.Models;
using UEParser.Services;
using UEParser.AssetRegistry;
using UEParser.Utils;

namespace UEParser.ViewModels;

public sealed partial class SettingsViewModel : INotifyPropertyChanged
{
    public ObservableCollection<string> AvailableComparisonVersions { get; set; }

    #region Path Settings
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

    private string? _blenderPath;
    public string? BlenderPath
    {
        get => _blenderPath;
        set => SetProperty(ref _blenderPath, value);
    }
    #endregion

    private bool _updateAPIDuringInitialization;
    public bool UpdateAPIDuringInitialization
    {
        get => _updateAPIDuringInitialization;
        set => SetProperty(ref _updateAPIDuringInitialization, value);
    }

    private bool _forceKrakenUpdate;
    public bool ForceKrakenUpdate
    {
        get => _forceKrakenUpdate;
        set => SetProperty(ref _forceKrakenUpdate, value);
    }

    private Branch _selectedCurrentBranch;
    public Branch SelectedCurrentBranch
    {
        get => _selectedCurrentBranch;
        set 
        { 
            if (SetProperty(ref _selectedCurrentBranch, value))
            {
                UpdateAvailableComparisonVersions(); // Update list when the branch changes
            }
        }
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
                if (!StringUtils.IsValidVersion(value))
                {
                    throw new DataValidationException("Invalid version");
                }
                UpdateAvailableComparisonVersions(); // Update list when the current version changes
            }
        }
    }

    private static bool FirstInitializationCompleted => ConfigurationService.Config.Global.FirstInitializationCompleted;
    private bool IsCurrentVersionConfigured => !string.IsNullOrEmpty(SelectedCurrentVersion);

    // I only want to allow download of register when current version is configured
    // and user initialized build at least once
    public bool CanDownloadRegisters => IsCurrentVersionConfigured && FirstInitializationCompleted;

    private string? _selectedComparisonVersion;
    public string? SelectedComparisonVersion
    {
        get => _selectedComparisonVersion;
        set
        {
            if (SetProperty(ref _selectedComparisonVersion, value))
            {
                if (!StringUtils.IsValidVersion(value))
                {
                    throw new DataValidationException("Invalid version");
                }
            }
        }
    }

    private string? _selectedComparisonVersionWithBranch;
    public string? SelectedComparisonVersionWithBranch
    {
        get => _selectedComparisonVersionWithBranch;
        set => SetProperty(ref _selectedComparisonVersionWithBranch, value);
    }

    private string? _newTome;
    public string? NewTome
    {
        get => _newTome;
        set => SetProperty(ref _newTome, value);
    }

    private string? _newEventTome;
    public string? NewEventTome
    {
        get => _newEventTome;
        set => SetProperty(ref _newEventTome, value);
    }

    private string? _aesKey;
    public string? AESKey
    {
        get => _aesKey;
        set => SetProperty(ref _aesKey, value);
    }

    private string? _customVersion;
    public string? CustomVersion
    {
        get => _customVersion;
        set => SetProperty(ref _customVersion, value);
    }

    private string? _s3AccessKey;
    public string? S3AccessKey
    {
        get => _s3AccessKey;
        set => SetProperty(ref _s3AccessKey, value);
    }

    private string? _s3SecretKey;
    public string? S3SecretKey
    {
        get => _s3SecretKey;
        set => SetProperty(ref _s3SecretKey, value);
    }

    private string? _s3BucketName;
    public string? S3BucketName
    {
        get => _s3BucketName;
        set => SetProperty(ref _s3BucketName, value);
    }

    private string? _awsRegion;
    public string? AWSRegion
    {
        get => _awsRegion;
        set => SetProperty(ref _awsRegion, value);
    }

    private string? _steamUsername;
    public string? SteamUsername
    {
        get => _steamUsername;
        set => SetProperty(ref _steamUsername, value);
    }

    private string? _steamPassword;
    public string? SteamPassword
    {
        get => _steamPassword;
        set => SetProperty(ref _steamPassword, value);
    }

    public ObservableCollection<string> TomesList { get; }
    public ObservableCollection<string> EventTomesList { get; }

    public static Branch[] Branches => Enum.GetValues(typeof(Branch)).Cast<Branch>().ToArray();

    public ICommand? OpenDirectoryDialogCommand { get; }
    public ICommand OpenFileDialogCommand { get; }
    public ICommand? SaveSettingsCommand { get; }
    public ICommand RemoveTomeCommand { get; }
    public ICommand RemoveEventTomeCommand { get; }

    public SettingsViewModel()
    {
        var config = ConfigurationService.Config;

        var currentVersionWithBranch = Helpers.ConstructVersionHeaderWithBranch();

        SelectedComparisonVersionWithBranch = Helpers.ConstructVersionHeaderWithBranch(true);
        var (version, branch) = Utils.StringUtils.SplitVersionAndBranch(SelectedComparisonVersionWithBranch);

        #region Paths
        PathToGameDirectory = config.Core.PathToGameDirectory;
        PathToMappings = config.Core.MappingsPath;
        BlenderPath = config.Global.BlenderPath;
        #endregion

        #region Branches
        SelectedCurrentBranch = config.Core.VersionData.Branch;
        SelectedComparisonBranch = (Branch)Enum.Parse(typeof(Branch), branch);
        #endregion

        #region Versions
        SelectedCurrentVersion = config.Core.VersionData.LatestVersionHeader;
        SelectedComparisonVersion = version;
        CustomVersion = config.Core.ApiConfig.CustomVersion;
        #endregion

        #region Booleans
        UpdateAPIDuringInitialization = config.Global.UpdateAPIDuringInitialization;
        ForceKrakenUpdate = config.Core.ApiConfig.ForceKrakenUpdate;
        #endregion

        #region Sensitive
        // AWS
        S3AccessKey = config.Sensitive.S3AccessKey;
        S3SecretKey = config.Sensitive.S3SecretKey;
        S3BucketName = config.Sensitive.S3BucketName;
        AWSRegion = config.Sensitive.AWSRegion;

        // Steam
        SteamUsername = config.Sensitive.SteamUsername;
        SteamPassword = config.Sensitive.SteamPassword;
        #endregion

        // Other
        var hashSetTomesList = config.Core.TomesList;
        TomesList = new ObservableCollection<string>(hashSetTomesList);

        var hashSetEventTomesList = config.Core.EventTomesList;
        EventTomesList = new ObservableCollection<string>(hashSetEventTomesList);

        AESKey = config.Core.AesKey;

        OpenDirectoryDialogCommand = ReactiveCommand.CreateFromTask<string>(OpenDirectoryDialog);
        OpenFileDialogCommand = ReactiveCommand.CreateFromTask<string>(OpenFileDialog);

        var availableComparisonVersions = FilesRegister.GrabAvailableComparisonVersions(currentVersionWithBranch);
        AvailableComparisonVersions = new ObservableCollection<string>(availableComparisonVersions);

        var canSave = this.WhenAnyValue(
            x => x.SelectedCurrentVersion,
            StringUtils.IsValidVersion
        );

        SaveSettingsCommand = ReactiveCommand.CreateFromTask(SaveSettings, canSave);
        RemoveTomeCommand = ReactiveCommand.Create<string>(RemoveTome);
        RemoveEventTomeCommand = ReactiveCommand.Create<string>(RemoveEventTome);
    }

    private void UpdateAvailableComparisonVersions()
    {
        var currentVersionWithBranch = $"{SelectedCurrentVersion}_{SelectedCurrentBranch}";
        var availableVersions = FilesRegister.GrabAvailableComparisonVersions(currentVersionWithBranch);

        if (AvailableComparisonVersions == null) return;

        AvailableComparisonVersions.Remove(currentVersionWithBranch);

        foreach (var version in availableVersions)
        {
            if (!AvailableComparisonVersions.Contains(version))
            {
                // I want to add it to the beginning of the list, as it is displayed in the UI
                AvailableComparisonVersions.Insert(0, version);
            }
        }
    }

    private async Task SaveSettings()
    {
        bool userConfirmedRestart = await ShowRestartPopup();
        if (userConfirmedRestart)
        {
            var config = ConfigurationService.Config;

            var (version, branch) = Utils.StringUtils.SplitVersionAndBranch(SelectedComparisonVersionWithBranch!);

            // Paths
            config.Core.PathToGameDirectory = PathToGameDirectory ?? "";
            config.Core.MappingsPath = PathToMappings ?? "";
            config.Global.BlenderPath = BlenderPath ?? "";

            // Branches
            config.Core.VersionData.Branch = SelectedCurrentBranch;
            config.Core.VersionData.CompareBranch = (Branch)Enum.Parse(typeof(Branch), branch);

            // Versions
            config.Core.VersionData.LatestVersionHeader = SelectedCurrentVersion;
            config.Core.VersionData.CompareVersionHeader = version;
            config.Core.ApiConfig.CustomVersion = CustomVersion;

            // Sensitive
            config.Sensitive.S3AccessKey = S3AccessKey;
            config.Sensitive.S3SecretKey = S3SecretKey;
            config.Sensitive.S3BucketName = S3BucketName;
            config.Sensitive.AWSRegion = AWSRegion;
            config.Sensitive.SteamUsername = SteamUsername;
            config.Sensitive.SteamPassword = SteamPassword;

            // Booleans
            config.Global.UpdateAPIDuringInitialization = UpdateAPIDuringInitialization;
            config.Core.ApiConfig.ForceKrakenUpdate = ForceKrakenUpdate;

            // Other
            config.Core.TomesList = [..TomesList];
            config.Core.EventTomesList = [..EventTomesList];
            config.Core.AesKey = AESKey ?? "";

            await ConfigurationService.SaveConfiguration();
            RestartApplication();
        }
    }

    public void AddTome()
    {
        if (!string.IsNullOrWhiteSpace(NewTome) && !TomesList.Contains(NewTome))
        {
            TomesList.Add(NewTome);
            NewTome = string.Empty;
        }
    }

    public void AddEventTome()
    {
        if (!string.IsNullOrWhiteSpace(NewEventTome) && !EventTomesList.Contains(NewEventTome))
        {
            EventTomesList.Add(NewEventTome);
            NewEventTome = string.Empty;
        }
    }

    private void RemoveTome(string tome)
    {
        TomesList.Remove(tome);
    }

    private void RemoveEventTome(string tome)
    {
        EventTomesList.Remove(tome);
    }

    private static void RestartApplication()
    {
        (Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.MainWindow?.Close();

        // Start a new instance of the application
        System.Diagnostics.Process.Start(Environment.ProcessPath ?? throw new Exception());

        Environment.Exit(0);
    }

    #region Dialogs
    private static async Task<bool> ShowRestartPopup()
    {
        var viewModel = new RestartApplicationPopupViewModel();
        var view = new RestartApplicationPopupView
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

        return await tcs.Task;
    }

    // Pick directory
    private async Task OpenDirectoryDialog(string propertyName)
    {
        var window = new Window();

        var storage = window.StorageProvider;

        var result = await storage.OpenFolderPickerAsync(new FolderPickerOpenOptions { AllowMultiple = false });
        if (result.Count > 0)
        {
            string selectedDirectoryPath = result[0].Path.AbsolutePath;
            string decodedPath = HttpUtility.UrlDecode(selectedDirectoryPath);

            UpdateProperty(propertyName, decodedPath);
        }
    }

    // Pick file
    private async Task OpenFileDialog(string propertyName)
    {
        var window = new Window();

        var storage = window.StorageProvider;

        var result = await storage.OpenFilePickerAsync(new FilePickerOpenOptions { AllowMultiple = false });
        if (result.Count > 0)
        {
            string selectedFilePath = result[0].Path.AbsolutePath;
            string decodedPath = HttpUtility.UrlDecode(selectedFilePath);

            UpdateProperty(propertyName, decodedPath);
        }
    }
    #endregion

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

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}