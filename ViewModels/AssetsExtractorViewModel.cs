﻿using Avalonia.Threading;
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
    public ICommand ExtractMissingAssetsCommand { get; }
    public ICommand CheckMissingAssetsCommand {  get; }

    public AssetsExtractorViewModel()
    {
        ExtractMissingAssetsCommand = ReactiveCommand.Create(ExtractMissingAssets);
        CheckMissingAssetsCommand = ReactiveCommand.Create(CheckMissingAssets);
    }

    private async void ExtractMissingAssets()
    {
        LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Running);
        var newAssets = FilesRegister.NewAssets;


        foreach (var file in newAssets)
        {
            await Task.Delay(100);
                                   
            Dispatcher.UIThread.Post(() =>
            {
                LogsWindowViewModel.Instance.AddLog($"New asset: {file.Key}", Logger.LogTags.Info);
            });
        }

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
        await Task.Delay(100);

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