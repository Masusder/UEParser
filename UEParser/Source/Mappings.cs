using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UEParser.Models;
using UEParser.Services;
using System.IO;
using UEParser.ViewModels;
using UEParser.Network;

namespace UEParser;

public class Mappings
{
    private const string mappingsGithubUrl = @"https://raw.githubusercontent.com/Masusder/Unreal-Mappings-Archive/main/Dead%20by%20Daylight/{0}/Mappings.usmap";
    public static async Task DownloadMappings()
    {
        
        var config = ConfigurationService.Config;
        string? versionHeader = config.Core.VersionData.LatestVersionHeader;
        Branch branch = config.Core.VersionData.Branch;

        string url;
        if (branch == Branch.live)
        {
            url = string.Format(mappingsGithubUrl, versionHeader);
        }
        else
        {
            string branchString = branch.ToString().ToUpper();
            string versionWithBranch = $"{versionHeader}%20{branchString}";
            url = string.Format(mappingsGithubUrl, versionWithBranch);
        }

        string versionHeaderWithBranch = Helpers.ConstructVersionHeaderWithBranch();

        string mappingsDirectory = Path.Combine(GlobalVariables.rootDir, "Dependencies", "Mappings", versionHeaderWithBranch);
        string mappingsOutputPath = Path.Combine(mappingsDirectory, "Mappings.usmap");

        Directory.CreateDirectory(mappingsDirectory);

        try
        {
            byte[] fileBytes = await API.FetchFileBytesAsync(url);

            await File.WriteAllBytesAsync(mappingsOutputPath, fileBytes);

            LogsWindowViewModel.Instance.AddLog($"Downloaded mappings for {versionHeaderWithBranch} version. Saving path to mappings in config.", Logger.LogTags.Success);

            await SaveMappingsPath(mappingsOutputPath);
        }
        catch
        {
            LogsWindowViewModel.Instance.AddLog("Failed to fetch mappings from archive, you need to provide mappings manually.", Logger.LogTags.Error);
        }
    }

    public static bool CheckIfMappingsExist()
    {
        string versionHeaderWithBranch = Helpers.ConstructVersionHeaderWithBranch();
        string mappingsOutputPath = Path.Combine(GlobalVariables.rootDir, "Dependencies", "Mappings", versionHeaderWithBranch, "Mappings.usmap");

        if (File.Exists(mappingsOutputPath))
        {
            return true;
        }

        return false;
    }

    private static async Task SaveMappingsPath(string mappingsOutputPath)
    {
        var config = ConfigurationService.Config;
        config.Core.MappingsPath = mappingsOutputPath;

        await ConfigurationService.SaveConfiguration();
    }
}
