using Avalonia.Threading;
using ReactiveUI;
using System.Threading.Tasks;
using System.Windows.Input;
using UEParser.Parser;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System;
using UEParser.Services;
using UEParser.AssetRegistry;

namespace UEParser.ViewModels;

public class AssetsExtractorViewModel
{
    public static bool IsComparisonVersionAvailable =>
        !string.IsNullOrEmpty(ConfigurationService.Config.Core.VersionData.CompareVersionHeader);

    public ICommand CheckMissingAssetsCommand { get; }
    public ICommand ExtractMeshesCommand { get; }
    public ICommand ExtractTexturesCommand { get; }
    public ICommand ExtractUICommand { get; }
    public ICommand ExtractAnimationsCommand { get; }
    public ICommand ExtractAudioCommand { get; }

    public AssetsExtractorViewModel()
    {
        CheckMissingAssetsCommand = ReactiveCommand.Create(CheckMissingAssets);
        ExtractMeshesCommand = ReactiveCommand.Create(ExtractMeshes);
        ExtractTexturesCommand = ReactiveCommand.Create(ExtractTextures);
        ExtractUICommand = ReactiveCommand.Create(ExtractUI);
        ExtractAnimationsCommand = ReactiveCommand.Create(ExtractAnimations);
        ExtractAudioCommand = ReactiveCommand.Create(ExtractAudio);
    }

    private async Task ExtractMeshes()
    {
        try
        {
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Running);
            LogsWindowViewModel.Instance.AddLog("Starting meshes extraction..", Logger.LogTags.Info);

            await AssetsManager.ParseMeshes();

            LogsWindowViewModel.Instance.AddLog("Finished extracting meshes.", Logger.LogTags.Success);
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
        }
        catch (Exception ex)
        {
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            LogsWindowViewModel.Instance.AddLog($"{ex.Message}", Logger.LogTags.Error);
        }
    }

    private async Task ExtractTextures()
    {
        try
        {
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Running);
            LogsWindowViewModel.Instance.AddLog("Starting textures extraction..", Logger.LogTags.Info);

            await AssetsManager.ParseTextures();

            LogsWindowViewModel.Instance.AddLog("Finished extracting textures.", Logger.LogTags.Success);
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
        }
        catch (Exception ex)
        {
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            LogsWindowViewModel.Instance.AddLog($"{ex.Message}", Logger.LogTags.Error);
        }
    }

    private async Task ExtractUI()
    {
        try
        {
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Running);
            LogsWindowViewModel.Instance.AddLog("Starting UI extraction..", Logger.LogTags.Info);

            await AssetsManager.ParseUI();

            LogsWindowViewModel.Instance.AddLog("Finished extracting UI.", Logger.LogTags.Success);
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
        }
        catch (Exception ex)
        {
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            LogsWindowViewModel.Instance.AddLog($"{ex.Message}", Logger.LogTags.Error);
        }
    }

    private async Task ExtractAnimations()
    {
        try
        {
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Running);
            LogsWindowViewModel.Instance.AddLog("Starting animations extraction..", Logger.LogTags.Info);

            await AssetsManager.ParseAnimations();

            LogsWindowViewModel.Instance.AddLog("Finished extracting animations.", Logger.LogTags.Success);
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
        }
        catch (Exception ex)
        {
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            LogsWindowViewModel.Instance.AddLog($"{ex.Message}", Logger.LogTags.Error);
        }
    }

    private async Task ExtractAudio()
    {
        try
        {
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Running);
            LogsWindowViewModel.Instance.AddLog("Starting audio extraction..", Logger.LogTags.Info);

            await AssetsManager.ParseAudio();

            LogsWindowViewModel.Instance.AddLog("Finished extracting audio.", Logger.LogTags.Success);
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
        }
        catch (Exception ex)
        {
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            LogsWindowViewModel.Instance.AddLog($"{ex.Message}", Logger.LogTags.Error);
        }
    }

    private const string packageDataDirectory = "DeadByDaylight/Content/Data";
    private const string packageCharactersDirectory = "DeadByDaylight/Content/Characters";
    private const string packageMeshesDirectory = "DeadByDaylight/Content/Meshes";
    private const string packageEffectsDirectory = "DeadByDaylight/Content/Effects";
    private const string packagePluginsDirectory = "DeadByDaylight/Plugins";
    private const string packageLocalizationDirectory = "DeadByDaylight/Content/Localization";
    private const string packageWwiseDirectory = "DeadByDaylight/Content/WwiseAudio";
    private async Task CheckMissingAssets()
    {
        LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Running);
        LogsWindowViewModel.Instance.AddLog("Looking for missing assets..", Logger.LogTags.Info);

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
        await Task.Run(() =>
        {
            foreach (var file in fileRegisterDictionary)
            {
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
        });

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

            await AssetsManager.ParseMissingAssets(missingAssetsList);

            LogsWindowViewModel.Instance.AddLog("Finished exporting.", Logger.LogTags.Success);
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
        }
    }
}
