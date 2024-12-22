using System;
using System.Linq;
using System.Windows.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ReactiveUI;
using UEParser.Models.Netease;
using UEParser.Utils;
using UEParser.Services;
using System.Reactive.Linq;

namespace UEParser.ViewModels;

public class NeteaseFileDialogViewModel : ReactiveObject
{
    public event Action<bool>? CloseAction;

    #region Manifest Properties
    private ObservableCollection<ManifestFileData> _files = [];
    public ObservableCollection<ManifestFileData> Files
    {
        get => _files;
        set
        {
            this.RaiseAndSetIfChanged(ref _files, value);
            UpdateTotalSize();
        }
    }

    private bool _hasContentBeenDownloaded;
    public bool HasContentBeenDownloaded
    {
        get => _hasContentBeenDownloaded;
        set
        {
            this.RaiseAndSetIfChanged(ref _hasContentBeenDownloaded, value);
        }
    }

    private string? _version;
    public string? Version
    {
        get => _version;
        set
        {
            this.RaiseAndSetIfChanged(ref _version, value);
            UpdateDisplayedVersion();
            UpdateHasContentBeenDownloaded();
        }
    }

    private string? _versionDisplayed;
    public string? VersionDisplayed
    {
        get => _versionDisplayed;
        set => this.RaiseAndSetIfChanged(ref _versionDisplayed, value);
    }

    private string? _totalSize;
    public string? TotalSize
    {
        get => _totalSize;
        set => this.RaiseAndSetIfChanged(ref _totalSize, value);
    }
    #endregion

    #region Selection States
    private bool _isAllSelected;
    public bool IsAllSelected
    {
        get => _isAllSelected;
        set
        {
            this.RaiseAndSetIfChanged(ref _isAllSelected, value);
            if (value)
            {
                IsRegularPaksSelected = false;
                IsOnlyPaksSelected = false;
                IsOptionalPaksSelected = false;
                IsScriptPaksSelected = false;
            }
            RefreshFiles(value, SelectionType.All);
        }
    }

    private bool _isRegularPaksSelected;
    public bool IsRegularPaksSelected
    {
        get => _isRegularPaksSelected;
        set
        {
            this.RaiseAndSetIfChanged(ref _isRegularPaksSelected, value);
            if (value)
            {
                IsAllSelected = false;
                IsOnlyPaksSelected = false;
                IsOptionalPaksSelected = false;
                IsScriptPaksSelected = false;
            }
            RefreshFiles(value, SelectionType.RegularPaks);
        }
    }

    private bool _isOnlyPaksSelected;
    public bool IsOnlyPaksSelected
    {
        get => _isOnlyPaksSelected;
        set
        {
            this.RaiseAndSetIfChanged(ref _isOnlyPaksSelected, value);
            if (value)
            {
                IsAllSelected = false;
                IsRegularPaksSelected = false;
                IsOptionalPaksSelected = false;
                IsScriptPaksSelected = false;
            }
            RefreshFiles(value, SelectionType.OnlyPaks);
        }
    }

    private bool _isOptionalPaksSelected;
    public bool IsOptionalPaksSelected
    {
        get => _isOptionalPaksSelected;
        set
        {
            this.RaiseAndSetIfChanged(ref _isOptionalPaksSelected, value);
            if (value)
            {
                IsAllSelected = false;
                IsRegularPaksSelected = false;
                IsOnlyPaksSelected = false;
                IsScriptPaksSelected = false;
            }
            RefreshFiles(value, SelectionType.OptionalPaks);
        }
    }

    private bool _isScriptPaksSelected;
    public bool IsScriptPaksSelected
    {
        get => _isScriptPaksSelected;
        set
        {
            this.RaiseAndSetIfChanged(ref _isScriptPaksSelected, value);
            if (value)
            {
                IsAllSelected = false;
                IsRegularPaksSelected = false;
                IsOnlyPaksSelected = false;
                IsOptionalPaksSelected = false;
            }
            RefreshFiles(value, SelectionType.ScriptPaks);
        }
    }
    #endregion

    private bool _isDownloading;
    public bool IsDownloading
    {
        get => _isDownloading;
        set => this.RaiseAndSetIfChanged(ref _isDownloading, value);
    }

    public NeteaseFileDialogViewModel()
    {
        Version = "";
        VersionDisplayed = "Version: ";
        TotalSize = "Size: 0 B";
        DownloadContentCommand = ReactiveCommand.Create(DownloadContent, this.WhenAnyValue(x => x.IsDownloading).Select(isDownloading => !isDownloading));
    }

    #region Commands
    public ICommand DownloadContentCommand { get; }

    private void DownloadContent()
    {
        if (IsDownloading) return;
        CloseAction?.Invoke(true);

        IsDownloading = true;

        try
        {
            var selectedFiles = GetSelectedFiles();

            if (selectedFiles.Any())
            {
                if (string.IsNullOrEmpty(Version)) throw new Exception("Version is null.");

                var message = new DownloadContentMessage(selectedFiles, Version);
                Utils.MessageBus.SendDownloadContentMessage(message);
            }
        }
        catch (Exception ex) 
        { 
            LogsWindowViewModel.Instance.AddLog(ex.ToString(), Logger.LogTags.Error);
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
        }
        finally
        {
            IsDownloading = false;
        }
    }
    #endregion

    #region Other
    private void RefreshFiles(bool isSelected, SelectionType selectionType)
    {
        foreach (var file in _files)
        {
            bool shouldSelect = false;

            switch (selectionType)
            {
                case SelectionType.All:
                    shouldSelect = isSelected;
                    break;

                case SelectionType.RegularPaks:
                    if (file.FileExtension == "pak" && !file.FilePath.Contains("optional") && !file.FilePath.Contains("script"))
                    {
                        shouldSelect = isSelected;
                    }
                    break;

                case SelectionType.OnlyPaks:
                    if (file.FileExtension == "pak")
                    {
                        shouldSelect = isSelected;
                    }
                    break;

                case SelectionType.ScriptPaks:
                    if (file.FileExtension == "pak" && file.FilePath.Contains("script"))
                    {
                        shouldSelect = isSelected;
                    }
                    break;

                case SelectionType.OptionalPaks:
                    if (file.FileExtension == "pak" && file.FilePath.Contains("optional"))
                    {
                        shouldSelect = isSelected;
                    }
                    break;
            }

            file.IsSelected = shouldSelect;
        }
    }

    public void UpdateTotalSize()
    {
        var totalSizeBytes = Files.Where(file => file.IsSelected)
                                  .Sum(file => file.FileSize);

        TotalSize = $"Size: {StringUtils.FormatBytes(totalSizeBytes)}";
    }

    public void UpdateDisplayedVersion()
    {
        VersionDisplayed = $"Version: {Version}";
    }

    public void UpdateHasContentBeenDownloaded()
    {
        var config = ConfigurationService.Config;
        if (config.Netease.ContentConfig.LatestContentVersion != Version)
        {
            HasContentBeenDownloaded = false;
        }
        else
        {
            HasContentBeenDownloaded = true;
        }
    }

    public void LoadFiles(IEnumerable<ManifestFileData> files)
    {
        Files.Clear();

        foreach (var file in files)
        {
            file.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ManifestFileData.IsSelected))
                {
                    UpdateTotalSize();
                }
            };
            Files.Add(file);
        }
    }

    public void LoadVersion(string version)
    {
        Version = version;
    }

    public IEnumerable<ManifestFileData> GetSelectedFiles()
    {
        return Files.Where(file => file.IsSelected);
    }

    private enum SelectionType
    {
        All,
        RegularPaks,
        OnlyPaks,
        ScriptPaks,
        OptionalPaks
    }
    #endregion
}