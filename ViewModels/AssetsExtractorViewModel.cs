using Avalonia.Threading;
using ReactiveUI;
using System.Threading.Tasks;
using System.Windows.Input;
using UEParser.Parser;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System;

namespace UEParser.ViewModels;

public class AssetsExtractorViewModel
{
    public ICommand CheckMissingAssetsCommand { get; }
    public ICommand ExtractMeshesCommand { get; }
    public ICommand ExtractTexturesCommand { get; }
    public ICommand ExtractUICommand { get; }

    public AssetsExtractorViewModel()
    {
        CheckMissingAssetsCommand = ReactiveCommand.Create(CheckMissingAssets);
        ExtractMeshesCommand = ReactiveCommand.Create(ExtractMeshes);
        ExtractTexturesCommand = ReactiveCommand.Create(ExtractTextures);
        ExtractUICommand = ReactiveCommand.Create(ExtractUI);
    }

    private async Task ExtractMeshes()
    {
        LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Running);
        LogsWindowViewModel.Instance.AddLog("Starting meshes extraction..", Logger.LogTags.Info);

        await AssetsManager.ParseMeshes();

        LogsWindowViewModel.Instance.AddLog("Finished extracting meshes.", Logger.LogTags.Success);
        LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
    }

    private async Task ExtractTextures()
    {
        LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Running);
        LogsWindowViewModel.Instance.AddLog("Starting textures extraction..", Logger.LogTags.Info);

        await AssetsManager.ParseTextures();

        LogsWindowViewModel.Instance.AddLog("Finished extracting textures.", Logger.LogTags.Success);
        LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
    }

    private async Task ExtractUI()
    {
        LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Running);
        LogsWindowViewModel.Instance.AddLog("Starting UI extraction..", Logger.LogTags.Info);

        await AssetsManager.ParseUI();

        LogsWindowViewModel.Instance.AddLog("Finished extracting UI.", Logger.LogTags.Success);
        LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
    }

    private const string packageDataDirectory = "DeadByDaylight/Content/Data";
    private const string packageCharactersDirectory = "DeadByDaylight/Content/Characters";
    private const string packageMeshesDirectory = "DeadByDaylight/Content/Meshes";
    private const string packageEffectsDirectory = "DeadByDaylight/Content/Effects";
    private const string packagePluginsDirectory = "DeadByDaylight/Plugins/Runtime/Bhvr";
    private const string packageLocalizationDirectory = "DeadByDaylight/Content/Localization";
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
            packageLocalizationDirectory
        };

        List<string> missingAssetsList = [];
        await Task.Run(() =>
        {
            foreach (var file in fileRegisterDictionary)
            {
                // Check if file.Key starts with any of the specified directories
                if (directoriesToMatch.Any(dir => file.Key.StartsWith(dir, StringComparison.OrdinalIgnoreCase)))
                {
                    string localFilePath = Path.Combine(pathToExtractedAssets, file.Key + ".json");

                    if (!File.Exists(localFilePath) && file.Value.Extension == "uasset")
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
