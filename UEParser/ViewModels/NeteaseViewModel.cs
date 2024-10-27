using System;
using System.IO;
using System.Linq;
using System.Windows.Input;
using System.IO.Compression;
using System.Threading.Tasks;
using Avalonia.Controls;
using ReactiveUI;
using UEParser.Utils;
using UEParser.Views;
using UEParser.Network.Netease;
using UEParser.Network;
using UEParser.Services;
using UEParser.Netease;

namespace UEParser.ViewModels;

public class NeteaseViewModel : ReactiveObject
{
    #region Properties

    private string? _fileName;
    private string? _currentSize;
    private string? _combinedCurrentSize;
    private long _combinedCurrentSizeInBytes;
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

    public string? CombinedCurrentSize
    {
        get => _combinedCurrentSize;
        set => this.RaiseAndSetIfChanged(ref _combinedCurrentSize, value);
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
        TextureStreamingPatchCommand = ReactiveCommand.CreateFromTask<Window>(TextureStreamingPatch);
        ProgressPercentage = 0;
        FileName = "";
        CombinedCurrentSize = "0 B";
        CurrentSize = "0 B";
        TotalMaxSize = "0 B";
        MaxSize = "0 B";
        Utils.MessageBus.DownloadContentStream.Subscribe(OnDownloadContentReceived);
    }

    #region Commands

    public ICommand DownloadLatestContentCommand { get; }
    public ICommand TextureStreamingPatchCommand { get; }

    private NeteaseFileDialogView? _fileDialog;

    private async Task DownloadLatestContent()
    {
        try
        {
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Running);
            LogsWindowViewModel.Instance.AddLog("Looking for latest manifest..", Logger.LogTags.Info);

            var parsedManifest = await NeteaseAPI.BruteForceLatestManifest();

            if (_fileDialog is not { IsVisible: true })
            {
                _fileDialog = new NeteaseFileDialogView();
                _fileDialog.Load(parsedManifest.FileDataList, parsedManifest.Version);
                _fileDialog.Show();
            }
            else
            {
                _fileDialog.Activate();
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

    private static async Task TextureStreamingPatch(Window owner)
    {
        try
        {
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Running);
            LogsWindowViewModel.Instance.AddLog("Processing texture streaming patch..", Logger.LogTags.Info);

            var config = ConfigurationService.Config;
            await DownloadTextureStreamingPatchDependencies(); // We need Repak and UnrealPak

            var neteaseOutputDirectory = Path.Combine(GlobalVariables.PathToNetease,
                config.Netease.Platform.ToString().ToLower());

            if (!Directory.Exists(neteaseOutputDirectory))
                throw new Exception(
                    "Netease output directory does not exist. Make sure to download NetEase content first.");

            var availableVersions = Directory.GetDirectories(neteaseOutputDirectory)
                .Select(Path.GetFileName)
                .Where(version => version != null)
                .Select(version => version!)
                .ToArray();

            if (availableVersions.Length == 0)
                throw new Exception("No available versions found. Make sure to download NetEase content first.");

            var version = await NeteaseVersionSelectionDialog.ShowDialogCustom(availableVersions);
            if (string.IsNullOrEmpty(version))
            {
                LogsWindowViewModel.Instance.AddLog("Version selection was canceled.", Logger.LogTags.Warning);
                return;
            }

            LogsWindowViewModel.Instance.AddLog($"Selected version: {version}", Logger.LogTags.Info);

            var paksOutputPath = Path.Combine(neteaseOutputDirectory, version);

            await Task.Run(() =>
            {
                ContentManager.UnpackPakFiles(paksOutputPath);
                ContentManager.RepackPakFiles(Path.Combine(paksOutputPath, "UEPakOutput"), paksOutputPath);
            });

            LogsWindowViewModel.Instance.AddLog(
                "Finished texture streaming patch, all paks have been combined into one.", Logger.LogTags.Success);
        }
        catch (Exception ex)
        {
            LogsWindowViewModel.Instance.AddLog($"Failed to patch pakchunks: {ex}", Logger.LogTags.Error);
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
        if (IsDownloading)
        {
            LogsWindowViewModel.Instance.AddLog("Download is already in progress.", Logger.LogTags.Warning);
            return;
        }

        var token = CancellationTokenService.Instance.Token;

        try
        {
            LogsWindowViewModel.Instance.AddLog("Starting downloading content..", Logger.LogTags.Info);
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.RunningWithCancellation);

            var config = ConfigurationService.Config;

            var filesToProcess = message.SelectedFiles;

            TotalMaxSize = StringUtils.FormatBytes(
                filesToProcess.Where(file => file.IsSelected)
                    .Sum(file => file.FileSize)
            );

            var contentDownloader = new ContentDownloader(this);
            foreach (var file in filesToProcess)
            {
                token.ThrowIfCancellationRequested();

                LogsWindowViewModel.Instance.AddLog($"Downloading: {file.FilePathWithExtension}", Logger.LogTags.Info);

                config.Netease.ContentConfig.LatestContentVersion = message.Version;
                await ConfigurationService.SaveConfiguration();

                await contentDownloader.ConstructFilePathAndDownloadAsync(file, message.Version,
                    config.Netease.Platform.ToString(), token);
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
        finally
        {
            // "Dispose"
            IsDownloading = false;
            TotalMaxSize = "0 B";
            CombinedCurrentSize = "0 B";
            _combinedCurrentSizeInBytes = 0;
        }
    }

    #endregion

    #region Utils

    private static async Task DownloadTextureStreamingPatchDependencies()
    {
        var repakPath = GlobalVariables.RepakPath;
        var unrealPakPath = GlobalVariables.UnrealPakPath;

        if (!File.Exists(repakPath))
        {
            const string repakDownloadUrl = GlobalVariables.DbdinfoBaseUrl + "UEParser/repak.exe";
            await DownloadDependency(repakDownloadUrl, repakPath, "Repak", false);
        }

        if (!File.Exists(unrealPakPath))
        {
            const string unrealPakDownloadUrl = GlobalVariables.DbdinfoBaseUrl + "UEParser/UnrealPak.zip";
            await DownloadDependency(unrealPakDownloadUrl, unrealPakPath, "UnrealPak", true);
        }
    }

    private static async Task DownloadDependency(string url, string targetFilePath, string dependencyName,
        bool isZip = false)
    {
        try
        {
            var filePath = isZip ? Path.ChangeExtension(targetFilePath, ".zip") : targetFilePath;

            var directory = Path.GetDirectoryName(targetFilePath)!;
            Directory.CreateDirectory(directory);

            var fileBytes = await NetAPI.FetchFileBytesAsync(url);
            await File.WriteAllBytesAsync(filePath, fileBytes);

            LogsWindowViewModel.Instance.AddLog($"Successfully downloaded {dependencyName} dependency.",
                Logger.LogTags.Success);

            if (isZip)
            {
                ZipFile.ExtractToDirectory(filePath, GlobalVariables.DotDataDir);
                File.Delete(filePath);
            }
        }
        catch (Exception ex)
        {
            LogsWindowViewModel.Instance.AddLog($"Error downloading {dependencyName}: {ex.Message}",
                Logger.LogTags.Error);
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
        }
    }

    public void AddToCombinedSize(long bytesRead)
    {
        _combinedCurrentSizeInBytes += bytesRead;
        CombinedCurrentSize = StringUtils.FormatBytes(_combinedCurrentSizeInBytes);
    }

    #endregion
}