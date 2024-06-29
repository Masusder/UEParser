﻿using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;
using System;
using UEParser.Parser;
using UEParser.Services;
using UEParser.ViewModels;
using UEParser.Kraken;

namespace UEParser;

public class Initialize
{
    public static async Task UpdateApp(bool hasVersionChanged, string buildVersion)
    {
        LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Running);

        //await Task.Delay(1000); // Ugly delay to update UI thread, think of better solution instead

        CreateDefaultDirectories();

        // If build version number changed update necessary files
        if (hasVersionChanged)
        {
            LogsWindowViewModel.Instance.AddLog("Detected new build version of Dead by Daylight.. starting initialization process.", Logger.LogTags.Info);
            LogsWindowViewModel.Instance.AddLog("Exporting game assets.", Logger.LogTags.Info);
            //await Task.Delay(1000); // Ugly delay to update UI, think of better solution instead
            //AssetsManager.InitializeCUE4Parse();
            await AssetsManager.ParseGameAssets();
            LogsWindowViewModel.Instance.AddLog("Looking for new S3 Bucket Access Keys.", Logger.LogTags.Info);
            await S3AccessKeys.CheckKeys(); // Check if there's any new S3AccessKeys (method needs to be invoked after 'InitializeCUE4Parse')
            LogsWindowViewModel.Instance.AddLog("Creating helper components to speed up parsing process.", Logger.LogTags.Info);
            Helpers.Archives.CreateArchiveQuestObjectiveDB();
            Helpers.Archives.CreateQuestNodeDatabase();
            LogsWindowViewModel.Instance.AddLog("Creating patched localization files to speed up parsing process.", Logger.LogTags.Info);
            Helpers.CreateLocresFiles(); // Create fixed localization to speed up parsing
            LogsWindowViewModel.Instance.AddLog("Saving new build version of Dead by Daylight.", Logger.LogTags.Info);
            await SaveBuildVersion(buildVersion); // Save new build version
        }

        LogsWindowViewModel.Instance.AddLog("Initialization finished.", Logger.LogTags.Success);
        LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);

    }

    public static (bool, string) CheckBuildVersion()
    {
        var config = ConfigurationService.Config;
        string gameDirectoryPath = config.Core.PathToGameDirectory;
        string buildVersion = "";

        bool hasVersionChanged = false;
        if (!string.IsNullOrEmpty(gameDirectoryPath))
        {
            string[] buildVersionPath = Directory.GetFiles(gameDirectoryPath, "DeadByDaylightVersionNumber.txt", SearchOption.AllDirectories);
            if (buildVersionPath.Length != 0)
            {
                buildVersion = File.ReadAllText(buildVersionPath[0]);

                // Check if build version number has changed
                hasVersionChanged = ReadVersion(buildVersion);
            }
            else
            {
                LogsWindowViewModel.Instance.AddLog("Not found Version Number. Check if path to the game directory is set correctly in 'config.json'.", Logger.LogTags.Error);
                LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            }
        }
        else
        {
            LogsWindowViewModel.Instance.AddLog("Set path to game directory in 'config.json'.", Logger.LogTags.Error);
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
        }

        return (hasVersionChanged, buildVersion);
    }

    private static void CreateDefaultDirectories()
    {
        Directory.CreateDirectory(Path.Combine(GlobalVariables.rootDir, "Dependencies", "HelperComponents"));
        Directory.CreateDirectory(Path.Combine(GlobalVariables.rootDir, "Dependencies", "Locres"));
    }

    private static bool ReadVersion(string savedVersion)
    {
        var config = ConfigurationService.Config;

        // Read saved variable
        string? storedVersion = config.Core.BuildVersionNumber;

        // Compare stored version with current version and return boolean
        if (storedVersion != null)
        {
            if (storedVersion == savedVersion)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        else
        {
            return true;
        }
    }

    public static async Task SaveBuildVersion(string newBuildVersion)
    {
        var config = ConfigurationService.Config;

        config.Core.BuildVersionNumber = newBuildVersion;

        await ConfigurationService.SaveConfiguration();
    }
}