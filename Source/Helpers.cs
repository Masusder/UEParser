using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UEParser.Services;

namespace UEParser;

public class Helpers
{
    public static string ConstructVersionHeaderWithBranch()
    {
        var config = ConfigurationService.Config;
        var versionHeader = config.Core.VersionData.LatestVersionHeader;
        var branch = config.Core.VersionData.Branch;
        var versionWithBranch = $"{versionHeader}_{branch}";

        return versionWithBranch;
    }
}