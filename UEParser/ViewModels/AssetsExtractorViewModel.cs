using ReactiveUI;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System;
using UEParser.Parser;
using UEParser.Services;
using UEParser.AssetRegistry;

namespace UEParser.ViewModels;

public class AssetsExtractorViewModel : ReactiveObject
{
    public static bool IsComparisonVersionAvailable =>
        !string.IsNullOrEmpty(ConfigurationService.Config.Core.VersionData.CompareVersionHeader);

    private bool _canExtract = true;
    public bool CanExtract
    {
        get => _canExtract;
        set
        {
            this.RaiseAndSetIfChanged(ref _canExtract, value);
            // Notify that CanExtractWithVersionCheck has also changed
            this.RaisePropertyChanged(nameof(CanExtractWithVersionCheck));
        }
    }

    public bool CanExtractWithVersionCheck => IsComparisonVersionAvailable && CanExtract;

    public ICommand CheckMissingAssetsCommand { get; }
    public ICommand ExtractMeshesCommand { get; }
    public ICommand ExtractTexturesCommand { get; }
    public ICommand ExtractUICommand { get; }
    public ICommand ExtractAnimationsCommand { get; }
    public ICommand ExtractAudioCommand { get; }

    public AssetsExtractorViewModel()
    {
        CheckMissingAssetsCommand = ReactiveCommand.CreateFromTask(() => CheckMissingAssets(CancellationTokenService.Instance.Token));
        ExtractMeshesCommand = ReactiveCommand.CreateFromTask(() => ExtractMeshes(CancellationTokenService.Instance.Token));
        ExtractTexturesCommand = ReactiveCommand.CreateFromTask(() => ExtractTextures(CancellationTokenService.Instance.Token));
        ExtractUICommand = ReactiveCommand.CreateFromTask(() => ExtractUI(CancellationTokenService.Instance.Token));
        ExtractAnimationsCommand = ReactiveCommand.CreateFromTask(() => ExtractAnimations(CancellationTokenService.Instance.Token));
        ExtractAudioCommand = ReactiveCommand.CreateFromTask(() => ExtractAudio(CancellationTokenService.Instance.Token));
    }

    private async Task ExtractMeshes(CancellationToken token)
    {
        try
        {
            CanExtract = false;
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.RunningWithCancellation);
            LogsWindowViewModel.Instance.AddLog("Starting meshes extraction..", Logger.LogTags.Info);

            await AssetsManager.ParseMeshes(token);

            LogsWindowViewModel.Instance.AddLog("Finished extracting meshes.", Logger.LogTags.Success);
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
        }
        catch (OperationCanceledException)
        {
            LogsWindowViewModel.Instance.AddLog("Extraction was canceled by the user.", Logger.LogTags.Warning);
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Warning);
        }
        catch (Exception ex)
        {
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            LogsWindowViewModel.Instance.AddLog($"{ex.Message}", Logger.LogTags.Error);
        }
        finally
        {
            CanExtract = true;
        }
    }

    private async Task ExtractTextures(CancellationToken token)
    {
        try
        {
            CanExtract = false;
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.RunningWithCancellation);
            LogsWindowViewModel.Instance.AddLog("Starting textures extraction..", Logger.LogTags.Info);

            await AssetsManager.ParseTextures(token);

            LogsWindowViewModel.Instance.AddLog("Finished extracting textures.", Logger.LogTags.Success);
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
        }
        catch (OperationCanceledException)
        {
            LogsWindowViewModel.Instance.AddLog("Extraction was canceled by the user.", Logger.LogTags.Warning);
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Warning);
        }
        catch (Exception ex)
        {
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            LogsWindowViewModel.Instance.AddLog($"{ex.Message}", Logger.LogTags.Error);
        }
        finally
        {
            CanExtract = true;
        }
    }

    private async Task ExtractUI(CancellationToken token)
    {
        try
        {
            CanExtract = false;
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.RunningWithCancellation);
            LogsWindowViewModel.Instance.AddLog("Starting UI extraction..", Logger.LogTags.Info);

            await AssetsManager.ParseUI(token);

            LogsWindowViewModel.Instance.AddLog("Finished extracting UI.", Logger.LogTags.Success);
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
        }
        catch (OperationCanceledException)
        {
            LogsWindowViewModel.Instance.AddLog("Extraction was canceled by the user.", Logger.LogTags.Warning);
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Warning);
        }
        catch (Exception ex)
        {
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            LogsWindowViewModel.Instance.AddLog($"{ex.Message}", Logger.LogTags.Error);
        }
        finally
        {
            CanExtract = true;
        }
    }

    private async Task ExtractAnimations(CancellationToken token)
    {
        try
        {
            CanExtract = false;
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.RunningWithCancellation);
            LogsWindowViewModel.Instance.AddLog("Starting animations extraction..", Logger.LogTags.Info);

            await AssetsManager.ParseAnimations(token);

            LogsWindowViewModel.Instance.AddLog("Finished extracting animations.", Logger.LogTags.Success);
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
        }
        catch (OperationCanceledException)
        {
            LogsWindowViewModel.Instance.AddLog("Extraction was canceled by the user.", Logger.LogTags.Warning);
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Warning);
        }
        catch (Exception ex)
        {
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            LogsWindowViewModel.Instance.AddLog($"{ex.Message}", Logger.LogTags.Error);
        }
        finally
        {
            CanExtract = true;
        }
    }

    private async Task ExtractAudio(CancellationToken token)
    {
        try
        {
            CanExtract = false;
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.RunningWithCancellation);
            LogsWindowViewModel.Instance.AddLog("Starting audio extraction..", Logger.LogTags.Info);

            await AssetsManager.ParseAudio(token);

            LogsWindowViewModel.Instance.AddLog("Finished extracting audio.", Logger.LogTags.Success);
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
        }
        catch (OperationCanceledException)
        {
            LogsWindowViewModel.Instance.AddLog("Extraction was canceled by the user.", Logger.LogTags.Warning);
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Warning);
        }
        catch (Exception ex)
        {
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            LogsWindowViewModel.Instance.AddLog($"{ex.Message}", Logger.LogTags.Error);
        }
        finally
        {
            CanExtract = true;
        }
    }

    private const string packageDataDirectory = "DeadByDaylight/Content/Data";
    private const string packageCharactersDirectory = "DeadByDaylight/Content/Characters";
    private const string packageMeshesDirectory = "DeadByDaylight/Content/Meshes";
    private const string packageEffectsDirectory = "DeadByDaylight/Content/Effects";
    private const string packagePluginsDirectory = "DeadByDaylight/Plugins";
    private const string packageLocalizationDirectory = "DeadByDaylight/Content/Localization";
    private const string packageWwiseDirectory = "DeadByDaylight/Content/WwiseAudio";
    private async Task CheckMissingAssets(CancellationToken token)
    {
        LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.RunningWithCancellation);
        LogsWindowViewModel.Instance.AddLog("Looking for missing assets..", Logger.LogTags.Info);

        CanExtract = false;

        try
        {
            await Task.Run(async () =>
        {
            var fileRegisterDictionary = FilesRegister.MountFileRegisterDictionary();
            string pathToExtractedAssets = GlobalVariables.pathToExtractedAssets;

            var directoriesToMatch = new List<string>
            {
                packageDataDirectory,
                packageCharactersDirectory,
                packageMeshesDirectory,
                packageEffectsDirectory,
                packagePluginsDirectory,
                packageLocalizationDirectory,
                packageWwiseDirectory
            };

            List<string> missingAssetsList = [];

            foreach (var file in fileRegisterDictionary)
            {
                token.ThrowIfCancellationRequested();
                // Check if file.Key starts with any of the specified directories
                if (directoriesToMatch.Any(dir => file.Key.StartsWith(dir, StringComparison.OrdinalIgnoreCase)))
                {
                    string extension = file.Value.Extension;

                    string[] acceptedExtensions = ["uasset", "wem", "xml", "bnk", "json", "bin"];
                    if (!acceptedExtensions.Contains(extension)) continue;

                    string insertExtension = extension == "uasset" ? ".json" : '.' + extension;
                    string localFilePath = Path.Combine(pathToExtractedAssets, file.Key + insertExtension);

                    if (!File.Exists(localFilePath))
                    {
                        missingAssetsList.Add(file.Key);
                    }
                }
            }

            var fatalCrashAssets = GlobalVariables.fatalCrashAssets;

            // Remove any strings from missingAssetsList that are in fatalCrashAssets
            missingAssetsList.RemoveAll(asset => fatalCrashAssets.Contains(asset));

            var missingAssetsCount = missingAssetsList.Count;
            if (missingAssetsCount == 0)
            {
                LogsWindowViewModel.Instance.AddLog("No missing assets have been detected!", Logger.LogTags.Success);
                LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
            }
            else
            {
                LogsWindowViewModel.Instance.AddLog($"Detected total of: {missingAssetsCount} missing assets. Starting export process..", Logger.LogTags.Warning);

                await AssetsManager.ParseMissingAssets(missingAssetsList, token);

                LogsWindowViewModel.Instance.AddLog("Finished exporting.", Logger.LogTags.Success);
                LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
            }
        }, token);
        }
        catch (OperationCanceledException)
        {
            LogsWindowViewModel.Instance.AddLog("Extraction was canceled by the user.", Logger.LogTags.Warning);
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Warning);
        }
        finally
        {
            CanExtract = true;
        }
    }
}