using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using UEParser.Services;
using UEParser.ViewModels;
using System.Threading.Tasks;

namespace UEParser.Kraken;

partial class S3AccessKeys
{
    public static async Task CheckKeys()
    {
        string iniFile = Path.Combine(GlobalVariables.rootDir, "Dependencies/ExtractedAssets/DeadByDaylight/Config/DefaultGame.ini");
        if (!File.Exists(iniFile))
        {
            LogsWindowViewModel.Instance.AddLog("Not found ini file that contains S3 Access Keys.", Logger.LogTags.Error);
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            return;
        }

        string iniFileData = File.ReadAllText(iniFile);

        Dictionary<string, string> s3AccessKeys = [];
        bool inAccessKeysSection = false;

        using (StringReader reader = new(iniFileData))
        {
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();

                if (line.StartsWith("[/Script/S3Command.AccessKeys]"))
                {
                    inAccessKeysSection = true;
                }
                else if (line.StartsWith('[') && inAccessKeysSection)
                {
                    inAccessKeysSection = false;
                }
                else if (inAccessKeysSection && line.StartsWith("+AccessKeys="))
                {
                    string keyId = KeyIdRegex().Match(line).Groups[1].Value;
                    string key = KeyRegex().Match(line).Groups[1].Value;
                    s3AccessKeys.Add(keyId, key);
                }
            }
        }

        var config = ConfigurationService.Config;
        if (config != null)
        {
            var configS3AccessKeys = config.Core.ApiConfig.S3AccessKeys;
            bool newKeysDetected = false;

            foreach (var item in s3AccessKeys)
            {
                if (!configS3AccessKeys.ContainsKey(item.Key))
                {
                    LogsWindowViewModel.Instance.AddLog($"Detected new S3AccessKey: {item.Key}", Logger.LogTags.Info);
                    config.Core.ApiConfig.S3AccessKeys[item.Key] = item.Value;
                    newKeysDetected = true;
                }
            }

            if (newKeysDetected)
            {
                // Sort the S3AccessKeys
                config.Core.ApiConfig.S3AccessKeys = config.Core.ApiConfig.S3AccessKeys.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
            }

            await ConfigurationService.SaveConfiguration();
        }
    }

    [GeneratedRegex("KeyId=\"([^\"]+)\"")]
    private static partial Regex KeyIdRegex();
    [GeneratedRegex("Key=\"([^\"]+)\"")]
    private static partial Regex KeyRegex();
}