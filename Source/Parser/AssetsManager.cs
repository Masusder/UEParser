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
using System.Threading.Tasks;

namespace UEParser.Parser;

public class AssetsManager
{
    private const string packagePathToPaks = "DeadByDaylight/Content/Paks";

    private static DefaultFileProvider? provider;
    private static readonly object lockObject = new();

    public static DefaultFileProvider Provider
    {
        get
        {
            if (provider == null)
            {
                lock (lockObject)
                {
                    if (provider == null)
                    {
                        InitializeCUE4Parse();
                    }
                }
            }
            return provider!;
        }
    }

    public static void InitializeCUE4Parse()
    {
        if (provider != null)
        {
            // Provider is already initialized
            return;
        }

        lock (lockObject)
        {
            if (provider != null)
            {
                // Double-check to ensure provider is still null inside the lock
                return;
            }

            var config = ConfigurationService.Config;

            string pathToGameDirectory = config.Core.PathToGameDirectory;
            if (pathToGameDirectory != null)
            {
                string pathToPaks = Path.Combine(pathToGameDirectory, packagePathToPaks);
                provider = new DefaultFileProvider(pathToPaks, SearchOption.TopDirectoryOnly, true, new VersionContainer(EGame.GAME_DeadByDaylight))
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
            }
            else
            {
                LogsWindowViewModel.Instance.AddLog("Not found path to game directory in 'config.json'.", Logger.LogTags.Error);
                LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
                return;
            }
        }

        //await ParseGameAssets(provider);
    }

    //public static async Task InitializeCUE4Parse()
    //{
    //    var config = ConfigurationService.Config;

    //    string pathToGameDirectory = config.Core.PathToGameDirectory;
    //    if (pathToGameDirectory != null)
    //    {
    //        string pathToPaks = Path.Combine(pathToGameDirectory, packagePathToPaks);
    //        var provider = new DefaultFileProvider(pathToPaks, SearchOption.TopDirectoryOnly, true, new VersionContainer(EGame.GAME_DeadByDaylight))
    //        {
    //            MappingsContainer = new FileUsmapTypeMappingsProvider(config.Core.MappingsPath)
    //        };

    //        provider.CustomEncryption = provider.Versions.Game switch
    //        {
    //            EGame.GAME_DeadByDaylight => DBDAes.DbDDecrypt,
    //            _ => DBDAes.DbDDecrypt
    //        };

    //        provider.Initialize(); // will scan local files and read them to know what it has to deal with (PAK/UTOC/UCAS/UASSET/UMAP)
    //        provider.SubmitKey(new FGuid(), new FAesKey(config.Core.AesKey)); // decrypt basic info (1 guid - 1 key)

    //        provider.LoadLocalization(ELanguage.English);

    //        await ParseGameAssets(provider);
    //    }
    //    else
    //    {
    //        LogsWindowViewModel.Instance.AddLog("Not found path to game directory in 'config.json'.", Logger.LogTags.Error);
    //        LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
    //    }
    //}

    private static readonly string[] extensionsToSkip = ["ubulk", "uexp", "ufont"];
    private static readonly string outputRootDirectory = Path.Combine(GlobalVariables.rootDir, "Dependencies", "ExtractedAssets");
    private const string packageDataDirectory = "DeadByDaylight/Content/Data";
    private const string packageCustomizationDirectory = "DeadByDaylight/Content/UI/UMGAssets/Icons/Customization";
    private const string packageUMGAssetsDirectory = "DeadByDaylight/Content/UI/UMGAssets";
    private const string packageUIDirectory = "DeadByDaylight/Content/UI/UMGAssets/Icons";
    private const string packageCharactersDirectory = "DeadByDaylight/Content/Characters";
    private const string packageMeshesDirectory = "DeadByDaylight/Content/Meshes";
    private const string packageEffectsDirectory = "DeadByDaylight/Content/Effects";
    private const string packagePluginsDirectory = "DeadByDaylight/Plugins/Runtime/Bhvr";
    private const string packageConfigDirectory = "DeadByDaylight/Config";
    private const string packageLocalizationDirectory = "DeadByDaylight/Content/Localization";
    public static async Task ParseGameAssets()
    {
        var files = Provider.Files.Values.ToList();
        int batchSize = 10;  // Process files in smaller batches to manage memory usage
        int totalFiles = files.Count;

        for (int i = 0; i < totalFiles; i += batchSize)
        {
            var batch = files.Skip(i).Take(batchSize);

            foreach (var file in batch)
            {
                try
                {
                    string pathWithoutExtension = file.PathWithoutExtension;
                    if (GlobalVariables.fatalCrashAssets.Contains(pathWithoutExtension)) continue;

                    string extension = file.Extension;
                    if (extensionsToSkip.Contains(extension)) continue;

                    string pathWithExtension = file.Path;
                    long size = file.Size;

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
                                //var allExports = provider.LoadAllObjects(pathWithExtension); // Load possible exports for asset

                                if (isInEffectsDirectory || isInCharactersDirectory || isInMeshesDirectory || isInDataDirectory || isInPluginsDirectory || isInLocalizationDirectory)
                                {
                                    var allExports = await Task.Run(() => Provider.LoadAllObjects(pathWithExtension));
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
                                if (Provider.TryCreateReader(pathWithExtension, out FArchive archive))
                                {
                                    var metadata = new FTextLocalizationMetaDataResource(archive);
                                    string exportData = JsonConvert.SerializeObject(metadata, Formatting.Indented);
                                    FileWriter.SaveJsonFile(exportPath, exportData);
                                }

                                break;
                            }
                        case "locres":
                            {
                                if (Provider.TryCreateReader(pathWithExtension, out FArchive archive))
                                {
                                    var locres = new FTextLocalizationResource(archive);
                                    string exportData = JsonConvert.SerializeObject(locres, Formatting.Indented);
                                    FileWriter.SaveJsonFile(exportPath, exportData);
                                }
                                break;
                            }
                        case "uplugin":
                        case "uproject":
                        case "ini":
                            {
                                if (Provider.TrySaveAsset(pathWithExtension, out byte[] data))
                                {
                                    using var stream = new MemoryStream(data) { Position = 0 };
                                    using var reader = new StreamReader(stream);
                                    var iniData = reader.ReadToEnd();
                                    FileWriter.SaveMemoryStreamFile(exportPath, iniData, extension);
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
        }

        // Clean up the fileInfoDictionary
        FilesRegister.CleanUpFileInfoDictionary(files);
        FilesRegister.SaveFileInfoDictionary();
        LogsWindowViewModel.Instance.AddLog("Finished exporting game assets.", Logger.LogTags.Info);
    }

    public static async Task ParseMeshes()
    {
        await Task.Run(() =>
        {
            var files = Provider.Files.Values.ToList();
            var newAssets = FilesRegister.NewAssets;

            foreach (var file in files)
            {
                try
                {
                    string pathWithoutExtension = file.PathWithoutExtension;
                    if (!newAssets.ContainsKey(pathWithoutExtension)) continue;

                    if (GlobalVariables.fatalCrashAssets.Contains(pathWithoutExtension)) continue;

                    string extension = file.Extension;
                    if (extensionsToSkip.Contains(extension)) continue;

                    string pathWithExtension = file.Path;
                    long size = file.Size;

                    string versionWithBranch = Helpers.ConstructVersionHeaderWithBranch();
                    string outputDirectory = Path.Combine(GlobalVariables.rootDir, "Output", "ExtractedAssets", "Meshes", versionWithBranch);
                    string outputPathWithoutExtension = Path.Combine(outputDirectory, pathWithoutExtension);
                    string outputPath = Path.ChangeExtension(outputPathWithoutExtension, "glb");

                    if (File.Exists(outputPath)) continue;

                    switch (extension)
                    {
                        case "uasset":
                            {
                                var allExports = Provider.LoadAllObjects(pathWithExtension);

                                foreach (var asset in allExports)
                                {
                                    switch (asset)
                                    {

                                        case UStaticMesh:
                                        case USkeletalMesh:
                                            {
                                                var directoryPath = Path.GetDirectoryName(outputPathWithoutExtension);
                                                if (directoryPath != null)
                                                {
                                                    Directory.CreateDirectory(directoryPath);
                                                }

                                                FileWriter.SaveMeshes(asset, pathWithoutExtension, outputPath);
                                                break;
                                            }
                                        default:
                                            break;
                                    }
                                }

                                break;
                            }
                        default:
                            break;
                    }
                }
                catch (Exception ex)
                {
                    LogsWindowViewModel.Instance.AddLog($"Failed parsing mesh: {ex}", Logger.LogTags.Error);
                    LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
                }
            }
        });
    }

    public static async Task ParseTextures()
    {
        await Task.Run(() =>
        {
            var files = Provider.Files.Values.ToList();
            var newAssets = FilesRegister.NewAssets;

            foreach (var file in files)
            {
                try
                {
                    string pathWithoutExtension = file.PathWithoutExtension;
                    if (!newAssets.ContainsKey(pathWithoutExtension)) continue;

                    if (GlobalVariables.fatalCrashAssets.Contains(pathWithoutExtension)) continue;

                    string extension = file.Extension;
                    if (extensionsToSkip.Contains(extension)) continue;

                    string pathWithExtension = file.Path;
                    bool isInUIDirectory = pathWithExtension.Contains(packageUMGAssetsDirectory);
                    if (isInUIDirectory) continue;
                    long size = file.Size;

                    string versionWithBranch = Helpers.ConstructVersionHeaderWithBranch();
                    string outputDirectory = Path.Combine(GlobalVariables.rootDir, "Output", "ExtractedAssets", "Textures", versionWithBranch);
                    string outputPathWithoutExtension = Path.Combine(outputDirectory, pathWithoutExtension);
                    string outputPath = Path.ChangeExtension(outputPathWithoutExtension, "png");

                    if (File.Exists(outputPath)) continue;

                    switch (extension)
                    {
                        case "uasset":
                            {
                                var allExports = Provider.LoadAllObjects(pathWithExtension);

                                foreach (var asset in allExports)
                                {
                                    switch (asset)
                                    {
                                        case UTexture texture:
                                            {
                                                FileWriter.SavePngFile(outputPath, pathWithoutExtension, texture);
                                                break;
                                            }
                                        default:
                                            break;
                                    }
                                }

                                break;
                            }
                        default:
                            break;
                    }
                }
                catch (Exception ex)
                {
                    LogsWindowViewModel.Instance.AddLog($"Failed parsing texture: {ex}", Logger.LogTags.Error);
                    LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
                }
            }
        });
    }

    public static async Task ParseMissingAssets(List<string> missingAssetsList)
    {
        var files = Provider.Files.Values.ToList();

        foreach (var file in files)
        {
            try
            {
                string pathWithoutExtension = file.PathWithoutExtension;

                if (GlobalVariables.fatalCrashAssets.Contains(pathWithoutExtension)) continue;
                if (!missingAssetsList.Contains(pathWithoutExtension)) continue;

                string extension = file.Extension;
                if (extensionsToSkip.Contains(extension)) continue;

                string pathWithExtension = file.Path;
                long size = file.Size;

                string exportPath = Path.Combine(outputRootDirectory, pathWithoutExtension);

                switch (extension)
                {
                    case "uasset":
                        {
                            var allExports = await Task.Run(() => Provider.LoadAllObjects(pathWithExtension));
                            string exportData = JsonConvert.SerializeObject(allExports, Formatting.Indented);

                            FileWriter.SaveJsonFile(exportPath, exportData);

                            break;
                        }
                    case "locmeta":
                        {
                            if (Provider.TryCreateReader(pathWithExtension, out FArchive archive))
                            {
                                var metadata = new FTextLocalizationMetaDataResource(archive);
                                string exportData = JsonConvert.SerializeObject(metadata, Formatting.Indented);
                                FileWriter.SaveJsonFile(exportPath, exportData);
                            }

                            break;
                        }
                    case "locres":
                        {
                            if (Provider.TryCreateReader(pathWithExtension, out FArchive archive))
                            {
                                var locres = new FTextLocalizationResource(archive);
                                string exportData = JsonConvert.SerializeObject(locres, Formatting.Indented);
                                FileWriter.SaveJsonFile(exportPath, exportData);
                            }
                            break;
                        }
                    case "uplugin":
                    case "uproject":
                    case "ini":
                        {
                            if (Provider.TrySaveAsset(pathWithExtension, out byte[] data))
                            {
                                using var stream = new MemoryStream(data) { Position = 0 };
                                using var reader = new StreamReader(stream);
                                var memoryData = reader.ReadToEnd();
                                FileWriter.SaveMemoryStreamFile(exportPath, memoryData, extension);
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

        FilesRegister.CleanUpFileInfoDictionary(files);
        FilesRegister.SaveFileInfoDictionary();
    }

    private static bool UpdateFileInfoIfNeeded(string packagePath, string extension, long size)
    {
        var fileInfo = FilesRegister.GetFileInfo(packagePath);
        bool fileDataChanged = fileInfo == null || fileInfo.Size != size;

        if (fileDataChanged)
        {
            Logger.SaveLog($"Asset size changed: {packagePath}", Logger.LogTags.Info);
            //LogsWindowViewModel.Instance.AddLog($"Asset size changed: {packagePath}", Logger.LogTags.Info);
            FilesRegister.UpdateFileInfo(packagePath, size, extension);
        }

        return fileDataChanged;
    }

    //public static void MoveFilesToOutput(string exportPathWithExtension, string packagePath, string extension, string savedFilePath)
    //{
    //    string versionWithBranch = Helpers.ConstructVersionHeaderWithBranch();
    //    string directoryAssetName = extension switch
    //    {
    //        "glb" => "Meshes",
    //        "png" => "Textures",
    //        _ => "Unknown",
    //    };

    //    string outputDirectory = Path.Combine(GlobalVariables.rootDir, "Output", "ExtractedAssets", directoryAssetName, versionWithBranch);

    //    string outputPathWithoutExtension = Path.Combine(outputDirectory, packagePath);

    //    var directoryPath = Path.GetDirectoryName(outputPathWithoutExtension);
    //    if (directoryPath != null)
    //    {
    //        Directory.CreateDirectory(directoryPath);
    //    }

    //    string outputPath = Path.ChangeExtension(outputPathWithoutExtension, extension);

    //    string exportPathToUse = exportPathWithExtension;
    //    if (!string.IsNullOrEmpty(savedFilePath))
    //    {
    //        exportPathToUse = savedFilePath;
    //    }

    //    if (!File.Exists(outputPath))
    //    {
    //        File.Copy(exportPathToUse, outputPath);
    //    }
    //}

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