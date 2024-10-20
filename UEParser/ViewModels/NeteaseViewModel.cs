using System;
using System.Net.Http;
using System.Windows.Input;
using System.Threading.Tasks;
using Avalonia.Threading;
using ReactiveUI;
using UEParser.Utils;
using UEParser.Views;
using UEParser.Network.Netease;
using System.Threading;
using UEParser.Services;
using UEParser.Netease;

namespace UEParser.ViewModels;

public class NeteaseViewModel : ReactiveObject
{
    #region Properties
    private string? _fileName;
    private string? _currentSize;
    private string? _totalMaxSize;
    private string? _maxSize;
    private double _progressPercentage;
    private bool _isDownloading;

    public string? FileName
    {
        get => _fileName;
        set => this.RaiseAndSetIfChanged(ref _fileName, value);
    }

    public string? CurrentSize
    {
        get => _currentSize;
        set => this.RaiseAndSetIfChanged(ref _currentSize, value);
    }

    public string? TotalMaxSize
    {
        get => _totalMaxSize;
        set => this.RaiseAndSetIfChanged(ref _totalMaxSize, value);
    }

    public string? MaxSize
    {
        get => _maxSize;
        set => this.RaiseAndSetIfChanged(ref _maxSize, value);
    }

    public double ProgressPercentage
    {
        get => _progressPercentage;
        set
        {
            if (_progressPercentage != value)
            {
                _progressPercentage = value;
                this.RaisePropertyChanged(nameof(ProgressPercentage));

                IsDownloading = _progressPercentage > 0 && _progressPercentage != 100.0;
            }
        }
    }

    public bool IsDownloading
    {
        get => _isDownloading;
        private set
        {
            if (_isDownloading != value)
            {
                _isDownloading = value;
                this.RaisePropertyChanged(nameof(IsDownloading));
            }
        }
    }
    #endregion

    public NeteaseViewModel()
    {
        IsDownloading = false;
        DownloadLatestContentCommand = ReactiveCommand.Create(DownloadLatestContent);
        ProgressPercentage = 0;
        FileName = "";
        CurrentSize = "0 B";
        MaxSize = "0 B";
        Utils.MessageBus.DownloadContentStream.Subscribe(OnDownloadContentReceived);
    }

    #region Commands
    public ICommand DownloadLatestContentCommand { get; }

    private NeteaseFileDialogView? FileDialog;
    private async Task DownloadLatestContent()
    {
        try
        {
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Running);
            LogsWindowViewModel.Instance.AddLog("Looking for latest manifest..", Logger.LogTags.Info);

            var parsedManifest = await NeteaseAPI.BruteForceLatestManifest();

            if (FileDialog == null || !FileDialog.IsVisible)
            {
                FileDialog = new NeteaseFileDialogView();
                FileDialog.Load(parsedManifest.FileDataList, parsedManifest.Version);
                FileDialog.Show();
            }
            else
            {
                FileDialog.Activate();
            }
        }
        catch (Exception ex)
        {
            LogsWindowViewModel.Instance.AddLog($"Failed fetching manifest: {ex}", Logger.LogTags.Error);
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
        }
        finally
        {
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
        }
    }
    #endregion

    #region Callbacks
    private async void OnDownloadContentReceived(DownloadContentMessage message)
    {
        CancellationToken token = CancellationTokenService.Instance.Token;

        try
        {
            LogsWindowViewModel.Instance.AddLog("Starting downloading content..", Logger.LogTags.Info);
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.RunningWithCancellation);

            var config = ConfigurationService.Config;

            var filesToProcess = message.SelectedFiles;

            var contentDownloader = new ContentDownloader(this);
            foreach (var file in filesToProcess)
            {
                token.ThrowIfCancellationRequested();

                LogsWindowViewModel.Instance.AddLog($"Downloading: {file.FilePathWithExtension}", Logger.LogTags.Info);

                await contentDownloader.ConstructFilePathAndDownloadAsync(file, message.Version, config.Netease.Platform.ToString(), token);
            }

            LogsWindowViewModel.Instance.AddLog("Finished downloading content.", Logger.LogTags.Success);
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
        }
        catch (OperationCanceledException)
        {
            LogsWindowViewModel.Instance.AddLog("Download was canceled by the user.", Logger.LogTags.Warning);
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Warning);
        }
        catch (Exception ex)
        {
            LogsWindowViewModel.Instance.AddLog(ex.ToString(), Logger.LogTags.Error);
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
        }
    }
    #endregion
}