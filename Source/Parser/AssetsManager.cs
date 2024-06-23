using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Versions;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Localization;
using CUE4Parse.UE4.Readers;
using CUE4Parse.GameTypes.DBD.Encryption.Aes;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.IO;
using System;
using System.Linq;
using UEParser.Services;
using UEParser.Utils;
using UEParser.ViewModels;

namespace UEParser.Parser;

public class AssetsManager
{
    private const string packagePathToPaks = "DeadByDaylight/Content/Paks";
    public static void InitializeCUE4Parse()
    {
        var config = ConfigurationService.Config;

        string pathToGameDirectory = config.Core.PathToGameDirectory;
        if (pathToGameDirectory != null)
        {
            string pathToPaks = Path.Combine(pathToGameDirectory, packagePathToPaks);
            var provider = new DefaultFileProvider(pathToPaks, SearchOption.TopDirectoryOnly, true, new VersionContainer(EGame.GAME_DeadByDaylight))
            {
                MappingsContainer = new FileUsmapTypeMappingsProvider(config.Core.MappingsPath)
            };

            provider.CustomEncryption = provider.Versions.Game switch
            {
                EGame.GAME_DeadByDaylight => DBDAes.DbDDecrypt,
                _ => DBDAes.DbDDecrypt
            };

            provider.Initialize(); // will scan local files and read them to know what it has to deal with (PAK/UTOC/UCAS/UASSET/UMAP)
            provider.SubmitKey(new FGuid(), new FAesKey(config.Core.AesKey)); // decrypt basic info (1 guid - 1 key)

            provider.LoadLocalization(ELanguage.English);

            ParseGameAssets(provider);
        }
        else
        {
            LogsWindowViewModel.Instance.AddLog("Not found path to game directory in 'config.json'.", Logger.LogTags.Error);
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
        }
    }

    private static readonly string[] extensionsToSkip = ["ubulk", "uexp", "ufont", "uplugin", "uproject"];
    private static readonly string outputRootDirectory = Path.Combine(GlobalVariables.rootDir, "Dependencies", "ExtractedAssets");
    private const string packageDataDirectory = "DeadByDaylight/Content/Data";
    private const string packageCustomizationDirectory = "DeadByDaylight/Content/UI/UMGAssets/Icons/Customization";
    private const string packageCharactersDirectory = "DeadByDaylight/Content/Characters";
    private const string packageMeshesDirectory = "DeadByDaylight/Content/Meshes";
    private const string packageEffectsDirectory = "DeadByDaylight/Content/Effects";
    private const string packagePluginsDirectory = "DeadByDaylight/Plugins/Runtime/Bhvr";
    private const string packageConfigDirectory = "DeadByDaylight/Config";
    private const string packageLocalizationDirectory = "DeadByDaylight/Content/Localization";
    private static void ParseGameAssets(DefaultFileProvider provider)
    {
        var files = provider.Files.Values;
        foreach (var file in files)
        {
            try
            {
                string pathWithExtension = file.Path;
                string pathWithoutExtension = file.PathWithoutExtension;

                string extension = file.Extension;
                long size = file.Size;

                if (extensionsToSkip.Contains(extension)) continue;

                bool isInUIDirectory = pathWithExtension.Contains(packageCustomizationDirectory);
                bool isInDataDirectory = pathWithExtension.Contains(packageDataDirectory);
                bool isInCharactersDirectory = pathWithExtension.Contains(packageCharactersDirectory);
                bool isInMeshesDirectory = pathWithExtension.Contains(packageMeshesDirectory);
                bool isInEffectsDirectory = pathWithExtension.Contains(packageEffectsDirectory);
                bool isInPluginsDirectory = pathWithExtension.Contains(packagePluginsDirectory);
                bool isInConfigDirectory = pathWithExtension.Contains(packageConfigDirectory);
                bool isInLocalizationDirectory = pathWithExtension.Contains(packageLocalizationDirectory);

                bool fileDataChanged = UpdateFileInfoIfNeeded(pathWithoutExtension, extension, size);

                if (!fileDataChanged && !isInConfigDirectory) continue;

                if (!isInUIDirectory &&
                    !isInDataDirectory &&
                    !isInCharactersDirectory &&
                    !isInMeshesDirectory &&
                    !isInEffectsDirectory &&
                    !isInPluginsDirectory &&
                    !isInConfigDirectory &&
                    !isInLocalizationDirectory)
                {
                    continue;
                }

                string exportPath = Path.Combine(outputRootDirectory, pathWithoutExtension);

                switch (extension)
                {
                    case "uasset":
                        {
                            var allExports = provider.LoadAllObjects(pathWithoutExtension); // Load possible exports for asset

                            if (isInEffectsDirectory || isInCharactersDirectory || isInMeshesDirectory || isInDataDirectory || isInPluginsDirectory || isInLocalizationDirectory)
                            {
                                string exportData = JsonConvert.SerializeObject(allExports, Formatting.Indented);

                                FileWriter.SaveJsonFile(exportPath, exportData);
                            }

                            // if (isInUIDirectory || isInCharactersDirectory || isInPluginsDirectory)
                            // {
                            //     foreach (var asset in allExports)
                            //     {
                            //         switch (asset)
                            //         {
                            //             case UTexture texture:
                            //                 {
                            //                     FileWriter.SavePngFile(exportPath, pathWithoutExtension, texture);
                            //                     break;
                            //                 }
                            //             // case UStaticMesh:
                            //             // case USkeletalMesh:
                            //             //     {
                            //             //         FileWriter.SaveMeshes(asset, outputRootDirectory, pathWithoutExtension, exportPath);
                            //             //         break;
                            //             //     }
                            //             default:
                            //                 break;
                            //         }
                            //     }
                            // }
                            break;
                        }
                    case "locmeta":
                        {
                            if (provider.TryCreateReader(pathWithExtension, out FArchive archive))
                            {
                                var metadata = new FTextLocalizationMetaDataResource(archive);
                                string exportData = JsonConvert.SerializeObject(metadata, Formatting.Indented);
                                FileWriter.SaveJsonFile(exportPath, exportData);
                            }

                            break;
                        }
                    case "locres":
                        {
                            if (provider.TryCreateReader(pathWithExtension, out FArchive archive))
                            {
                                var locres = new FTextLocalizationResource(archive);
                                string exportData = JsonConvert.SerializeObject(locres, Formatting.Indented);
                                FileWriter.SaveJsonFile(exportPath, exportData);
                            }
                            break;
                        }
                    case "ini":
                        {
                            if (provider.TrySaveAsset(pathWithExtension, out byte[] data))
                            {
                                using var stream = new MemoryStream(data) { Position = 0 };
                                using var reader = new StreamReader(stream);
                                var iniData = reader.ReadToEnd();
                                FileWriter.SaveIniFile(exportPath, iniData);
                            }
                            break;
                        }
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                LogsWindowViewModel.Instance.AddLog($"Failed parsing asset: {ex}", Logger.LogTags.Error);
                LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            }
        }

        //DeleteUnusedFiles(); // TODO: dont delete all files, delete only datatables
        FilesRegister.SaveFileInfoDictionary();
        LogsWindowViewModel.Instance.AddLog("Finished exporting game assets.", Logger.LogTags.Info);
    }

    private static bool UpdateFileInfoIfNeeded(string packagePath, string extension, long size)
    {
        var fileInfo = FilesRegister.GetFileInfo(packagePath);
        bool fileDataChanged = fileInfo == null || fileInfo.Size != size;

        if (fileDataChanged)
        {
            LogsWindowViewModel.Instance.AddLog($"Asset size changed: {packagePath}", Logger.LogTags.Info);
            FilesRegister.UpdateFileInfo(packagePath, size, extension);
        }

        return fileDataChanged;
    }

    public static void MoveFilesToOutput(string exportPathWithExtension, string packagePath, string extension, string savedFilePath)
    {
        string versionWithBranch = Helpers.ConstructVersionHeaderWithBranch();

        string outputDirectory = Path.Combine(GlobalVariables.rootDir, "Output", "ExtractedAssets", versionWithBranch);

        string outputPathWithoutExtension = Path.Combine(outputDirectory, packagePath);

        var directoryPath = Path.GetDirectoryName(outputPathWithoutExtension);
        if (directoryPath != null)
        {
            Directory.CreateDirectory(directoryPath);
        }

        string outputPath = Path.ChangeExtension(outputPathWithoutExtension, extension);

        string exportPathToUse = exportPathWithExtension;
        if (!string.IsNullOrEmpty(savedFilePath))
        {
            exportPathToUse = savedFilePath;
        }

        if (!File.Exists(outputPath))
        {
            File.Copy(exportPathToUse, outputPath);
        }
    }

    // TODO: dont delete all files, delete only datatables
    //private static void DeleteUnusedFiles()
    //{
    //    Logger.SaveLog("Deletion process of unused assets started.", Logger.LogTags.Info);
    //    // Load fileInfoDictionary from FilesRegister class
    //    Dictionary<string, FilesRegister.FileInfo> fileInfoDictionary = FilesRegister.MountFileRegisterDictionary();

    //    if (!Directory.Exists(outputRootDirectory))
    //    {
    //        Logger.SaveLog("Output directory does not exist.", Logger.LogTags.Info);
    //        return;
    //    }

    //    string[] allFiles = Directory.GetFiles(outputRootDirectory, "*", SearchOption.AllDirectories);

    //    foreach (string file in allFiles)
    //    {
    //        string relativePath = StringUtils.GetRelativePathWithoutExtension(file, outputRootDirectory);

    //        if (!fileInfoDictionary.ContainsKey(relativePath))
    //        {
    //            File.Delete(file);
    //            Logger.SaveLog($"Deleted file: {file}", Logger.LogTags.Info);
    //        }
    //    }

    //    Logger.SaveLog("Deletion process completed.", Logger.LogTags.Info);
    //}
}