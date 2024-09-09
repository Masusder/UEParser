using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Versions;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Localization;
using CUE4Parse.UE4.Readers;
using CUE4Parse.GameTypes.DBD.Encryption.Aes;
using CUE4Parse.Compression;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;
using UEParser.Services;
using UEParser.Utils;
using UEParser.ViewModels;
using System.Threading.Tasks;
using UEParser.AssetRegistry;
using UEParser.AssetRegistry.Wwise;
using UEParser.Parser.Wwise;
using CUE4Parse.UE4.AssetRegistry;

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

                var oodleDirectory = Path.Combine(GlobalVariables.rootDir, ".data");
                var oodlePath = Path.Combine(oodleDirectory, OodleHelper.OODLE_DLL_NAME);

                var mappingsPath = config.Core.MappingsPath;

                if (!File.Exists(mappingsPath)) throw new Exception("Not found build mappings. You need to provide path to them in settings.");

                Directory.CreateDirectory(oodleDirectory);

                if (!File.Exists(oodlePath))
                {
                    OodleHelper.DownloadOodleDll(oodlePath);
                }

                OodleHelper.Initialize(oodlePath);

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
                LogsWindowViewModel.Instance.AddLog("Not found path to game directory set in settings.", Logger.LogTags.Error);
                LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
                return;
            }
        }
    }

    private static readonly string[] extensionsToSkip = ["ubulk", "uexp", "ufont"];
    private static readonly string outputRootDirectory = Path.Combine(GlobalVariables.rootDir, "Dependencies", "ExtractedAssets");
    private const string packageDataDirectory = "DeadByDaylight/Content/Data";
    private const string packageCustomizationDirectory = "DeadByDaylight/Content/UI/UMGAssets/Icons/Customization";
    private const string packageUMGAssetsDirectory = "DeadByDaylight/Content/UI/UMGAssets";
    private const string packageUIDirectory = "DeadByDaylight/Content/UI/UMGAssets/Icons";
    private const string packageCharactersDirectory = "DeadByDaylight/Content/Characters";
    private const string packageMeshesDirectory = "DeadByDaylight/Content/Meshes";
    private const string packageEffectsDirectory = "DeadByDaylight/Content/Effects";
    private const string packagePluginsDirectory = "DeadByDaylight/Plugins";
    private const string packageConfigDirectory = "DeadByDaylight/Config";
    private const string packageLocalizationDirectory = "DeadByDaylight/Content/Localization";
    private const string packageWwiseDirectory = "DeadByDaylight/Content/WwiseAudio";
    private const string packageAssetsRegistryFile = "DeadByDaylight/AssetRegistry.bin";
    public static async Task ParseGameAssets()
    {
        await Task.Run(() =>
        {
            var files = Provider.Files.Values.ToList();
            int batchSize = 10; // Process files in smaller batches to manage memory usage
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
                        bool isInWwiseDirectory = pathWithExtension.Contains(packageWwiseDirectory);
                        bool isAssetsRegistry = pathWithExtension.Contains(packageAssetsRegistryFile);

                        bool fileDataChanged = UpdateFileInfoIfNeeded(pathWithoutExtension, extension, size);

                        if (!fileDataChanged && !isInConfigDirectory) continue;

                        if (!isInUIDirectory &&
                            !isInDataDirectory &&
                            !isInCharactersDirectory &&
                            !isInMeshesDirectory &&
                            !isInEffectsDirectory &&
                            !isInPluginsDirectory &&
                            !isInConfigDirectory &&
                            !isInLocalizationDirectory &&
                            !isInWwiseDirectory &&
                            !isAssetsRegistry)
                        {
                            continue;
                        }

                        string exportPath = Path.Combine(outputRootDirectory, pathWithoutExtension);

                        switch (extension)
                        {
                            case "uasset":
                                {
                                    if (isInEffectsDirectory || isInCharactersDirectory || isInMeshesDirectory || isInDataDirectory || isInPluginsDirectory || isInLocalizationDirectory)
                                    {
                                        var allExports = Provider.LoadAllObjects(pathWithExtension);
                                        string exportData = JsonConvert.SerializeObject(allExports, Formatting.Indented);

                                        FileWriter.SaveJsonFile(exportPath, exportData);
                                    }

                                    break;
                                }
                            case "bin":
                                {
                                    if (Provider.TryCreateReader(pathWithExtension, out var archive))
                                    {
                                        var registry = new FAssetRegistryState(archive);
                                        string exportData = JsonConvert.SerializeObject(registry, Formatting.Indented);

                                        FileWriter.SaveJsonFile(exportPath, exportData);
                                    }

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
                            case "json":
                            case "xml":
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
                            case "wem":
                            case "bnk":
                                {
                                    if (Provider.TrySaveAsset(pathWithExtension, out var data))
                                    {
                                        FileWriter.SaveBinaryStreamFile(exportPath, data, extension);
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

            // Deletion of unused files needs to invoked after fileInfoDictionary cleanup!
            DeleteUnusedFiles();
            FilesRegister.SaveFileInfoDictionary();
            LogsWindowViewModel.Instance.AddLog("Finished exporting game assets.", Logger.LogTags.Info);
        });
    }

    public static async Task ParseMeshes()
    {
        await Task.Run(() =>
        {
            var files = Provider.Files.Values.ToList();
            var newAssets = FilesRegister.NewAssets;
            var modifiedAssets = FilesRegister.ModifiedAssets;
            int extractedAssetsCount = 0;

            LogsWindowViewModel.Instance.AddLog($"Detected total of {newAssets.Count} new assets.", Logger.LogTags.Info);
            LogsWindowViewModel.Instance.AddLog($"Detected total of {modifiedAssets.Count} modified assets.", Logger.LogTags.Info);

            foreach (var file in files)
            {
                try
                {
                    string pathWithoutExtension = file.PathWithoutExtension;
                    //if (!newAssets.ContainsKey(pathWithoutExtension)) continue;
                    if (!newAssets.ContainsKey(pathWithoutExtension) && !modifiedAssets.ContainsKey(pathWithoutExtension)) continue;

                    if (GlobalVariables.fatalCrashAssets.Contains(pathWithoutExtension)) continue;

                    string extension = file.Extension;
                    if (extensionsToSkip.Contains(extension)) continue;

                    string pathWithExtension = file.Path;
                    long size = file.Size;

                    string versionWithBranch = Helpers.ConstructVersionHeaderWithBranch();
                    string outputDirectory = Path.Combine(GlobalVariables.rootDir, "Output", "ExtractedAssets", "Meshes", versionWithBranch);
                    string outputPathWithoutExtension = Path.Combine(outputDirectory, pathWithoutExtension);
                    string outputPath = Path.ChangeExtension(outputPathWithoutExtension, "psk");

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

                                                FileWriter.SaveMeshes(asset, pathWithoutExtension, outputPath, ref extractedAssetsCount);
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

            if (extractedAssetsCount > 0)
            {
                LogsWindowViewModel.Instance.AddLog($"Extracted total of {extractedAssetsCount} mesh(es).", Logger.LogTags.Info);
            }
        });
    }

    public static async Task ParseAnimations()
    {
        await Task.Run(() =>
        {
            var files = Provider.Files.Values.ToList();
            var newAssets = FilesRegister.NewAssets;
            var modifiedAssets = FilesRegister.ModifiedAssets;
            int extractedAssetsCount = 0;

            LogsWindowViewModel.Instance.AddLog($"Detected total of {newAssets.Count} new assets.", Logger.LogTags.Info);
            LogsWindowViewModel.Instance.AddLog($"Detected total of {modifiedAssets.Count} modified assets.", Logger.LogTags.Info);

            foreach (var file in files)
            {
                try
                {
                    string pathWithoutExtension = file.PathWithoutExtension;
                    if (!newAssets.ContainsKey(pathWithoutExtension) && !modifiedAssets.ContainsKey(pathWithoutExtension)) continue;
                    //if (!newAssets.ContainsKey(pathWithoutExtension)) continue;

                    if (GlobalVariables.fatalCrashAssets.Contains(pathWithoutExtension)) continue;

                    string extension = file.Extension;
                    if (extensionsToSkip.Contains(extension)) continue;

                    string pathWithExtension = file.Path;
                    long size = file.Size;

                    string versionWithBranch = Helpers.ConstructVersionHeaderWithBranch();
                    string outputDirectory = Path.Combine(GlobalVariables.rootDir, "Output", "ExtractedAssets", "Animations", versionWithBranch);
                    string outputPathWithoutExtension = Path.Combine(outputDirectory, pathWithoutExtension);
                    string outputPath = Path.ChangeExtension(outputPathWithoutExtension, "psa");

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

                                        case UAnimSequence:
                                        case UAnimMontage:
                                        case UAnimComposite:
                                            {
                                                var directoryPath = Path.GetDirectoryName(outputPathWithoutExtension);
                                                if (directoryPath != null)
                                                {
                                                    Directory.CreateDirectory(directoryPath);
                                                }

                                                FileWriter.SaveAnimations(asset, pathWithoutExtension, outputPath, ref extractedAssetsCount);
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
                    LogsWindowViewModel.Instance.AddLog($"Failed parsing animation: {ex}", Logger.LogTags.Error);
                    LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
                }
            }

            if (extractedAssetsCount > 0)
            {
                LogsWindowViewModel.Instance.AddLog($"Extracted total of {extractedAssetsCount} animation(s).", Logger.LogTags.Info);
            }
        });
    }

    public static async Task ParseTextures()
    {
        await Task.Run(() =>
        {
            var files = Provider.Files.Values.ToList();
            var newAssets = FilesRegister.NewAssets;
            var modifiedAssets = FilesRegister.ModifiedAssets;
            int extractedAssetsCount = 0;

            LogsWindowViewModel.Instance.AddLog($"Detected total of {newAssets.Count} new assets.", Logger.LogTags.Info);
            LogsWindowViewModel.Instance.AddLog($"Detected total of {modifiedAssets.Count} modified assets.", Logger.LogTags.Info);

            foreach (var file in files)
            {
                try
                {
                    string pathWithoutExtension = file.PathWithoutExtension;
                    if (!newAssets.ContainsKey(pathWithoutExtension) && !modifiedAssets.ContainsKey(pathWithoutExtension)) continue;
                    //if (!newAssets.ContainsKey(pathWithoutExtension)) continue;

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
                                                FileWriter.SavePngFile(outputPath, pathWithoutExtension, texture, ref extractedAssetsCount);
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

            if (extractedAssetsCount > 0)
            {
                LogsWindowViewModel.Instance.AddLog($"Extracted total of {extractedAssetsCount} texture(s).", Logger.LogTags.Info);
            }
        });
    }

    public static async Task ParseUI()
    {
        await Task.Run(() =>
        {
            var files = Provider.Files.Values.ToList();
            var newAssets = FilesRegister.NewAssets;
            var modifiedAssets = FilesRegister.ModifiedAssets;
            int extractedAssetsCount = 0;

            LogsWindowViewModel.Instance.AddLog($"Detected total of {newAssets.Count} new assets.", Logger.LogTags.Info);
            LogsWindowViewModel.Instance.AddLog($"Detected total of {modifiedAssets.Count} modified assets.", Logger.LogTags.Info);

            foreach (var file in files)
            {
                try
                {
                    string pathWithoutExtension = file.PathWithoutExtension;
                    if (!newAssets.ContainsKey(pathWithoutExtension) && !modifiedAssets.ContainsKey(pathWithoutExtension)) continue;

                    if (GlobalVariables.fatalCrashAssets.Contains(pathWithoutExtension)) continue;

                    string extension = file.Extension;
                    if (extensionsToSkip.Contains(extension)) continue;

                    string pathWithExtension = file.Path;
                    bool isInUIDirectory = pathWithExtension.Contains(packageUIDirectory);
                    if (!isInUIDirectory) continue;

                    long size = file.Size;

                    string versionWithBranch = Helpers.ConstructVersionHeaderWithBranch();
                    string outputDirectory = Path.Combine(GlobalVariables.rootDir, "Output", "ExtractedAssets", "UI", versionWithBranch);
                    string outputPathWithoutExtension = Path.Combine(outputDirectory, pathWithoutExtension);

                    string outputPathWithoutExtensionDirectory = Path.GetDirectoryName(outputPathWithoutExtension)!;

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
                                                string newOutputPathWithoutExtension = Path.Combine(outputPathWithoutExtensionDirectory, texture.Name);
                                                string finalOutputPath = Path.ChangeExtension(newOutputPathWithoutExtension, "png");

                                                string packagePathDirectory = Path.GetDirectoryName(pathWithoutExtension)!;
                                                string packagePathWithExportName = Path.Combine(packagePathDirectory, texture.Name);

                                                FileWriter.SavePngFile(finalOutputPath, packagePathWithExportName, texture, ref extractedAssetsCount);
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
                    LogsWindowViewModel.Instance.AddLog($"Failed parsing UI: {ex}", Logger.LogTags.Error);
                    LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
                }
            }

            if (extractedAssetsCount > 0)
            {
                LogsWindowViewModel.Instance.AddLog($"Extracted total of {extractedAssetsCount} UI asset(s).", Logger.LogTags.Info);
            }
        });
    }

    // TODO: add collection of unused audio
    public static async Task ParseAudio()
    {
        LogsWindowViewModel.Instance.AddLog("Audio extraction is highly intensive process, which may take even up to an hour, depending whether audio registry is available..", Logger.LogTags.Info);
        await Task.Run(async () =>
        {
            LogsWindowViewModel.Instance.AddLog("Moving compressed audio into temporary folder..", Logger.LogTags.Info);

            WwiseFileHandler.MoveCompressedAudio();

            LogsWindowViewModel.Instance.AddLog("Unpacking audio banks..", Logger.LogTags.Info);

            await WwiseFileHandler.UnpackAudioBanks();

            LogsWindowViewModel.Instance.AddLog("Generating txtp files from compiled audio..", Logger.LogTags.Info);

            WwiseFileHandler.GenerateTxtp();

            LogsWindowViewModel.Instance.AddLog("Collecting associated audio event IDs..", Logger.LogTags.Info);

            var associatedAudioEventIds = WwiseFileHandler.GrabAudioEventIds();

            LogsWindowViewModel.Instance.AddLog("Collecting Wwise data from preallocated buffers..", Logger.LogTags.Info);

            var audioEventsLinkage = WwiseFileHandler.ConstructAudioEventsLinkage();

            LogsWindowViewModel.Instance.AddLog("Populating audio registry..", Logger.LogTags.Info);

            WwiseRegister.PopulateAudioRegister();
            WwiseRegister.SaveAudioInfoDictionary();

            LogsWindowViewModel.Instance.AddLog("Converting audio to WAV audio format..", Logger.LogTags.Info);

            WwiseFileHandler.ConvertTxtpToWav();

            LogsWindowViewModel.Instance.AddLog("Reversing audio structure..", Logger.LogTags.Info);

            WwiseFileHandler.ReverseAudioStructure(associatedAudioEventIds, audioEventsLinkage);

            LogsWindowViewModel.Instance.AddLog("Moving converted audio into output directory..", Logger.LogTags.Info);

            WwiseFileHandler.MoveAudioToOutput();

            LogsWindowViewModel.Instance.AddLog("Deleting temporary audio folder..", Logger.LogTags.Info);

            WwiseFileHandler.CleanExtractedAudioDir();
        });
    }

    public static async Task ParseMissingAssets(List<string> missingAssetsList)
    {
        await Task.Run(() =>
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
                                var allExports = Provider.LoadAllObjects(pathWithExtension);
                                string exportData = JsonConvert.SerializeObject(allExports, Formatting.Indented);

                                FileWriter.SaveJsonFile(exportPath, exportData);

                                break;
                            }
                        case "bin":
                            {
                                if (Provider.TryCreateReader(pathWithExtension, out var archive))
                                {
                                    var registry = new FAssetRegistryState(archive);
                                    string exportData = JsonConvert.SerializeObject(registry, Formatting.Indented);

                                    FileWriter.SaveJsonFile(exportPath, exportData);
                                }

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
                        case "json":
                        case "xml":
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
                        case "wem":
                        case "bnk":
                            {
                                if (Provider.TrySaveAsset(pathWithExtension, out var data))
                                {
                                    FileWriter.SaveBinaryStreamFile(exportPath, data, extension);
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

            // Deletion of unused files needs to invoked after fileInfoDictionary cleanup!
            DeleteUnusedFiles();
            FilesRegister.SaveFileInfoDictionary();
        });
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

    private static void DeleteUnusedFiles()
    {
        LogsWindowViewModel.Instance.AddLog("Deletion process of unused assets started.", Logger.LogTags.Info);
        // Load fileInfoDictionary from FilesRegister class
        Dictionary<string, FilesRegister.FileInfo> fileInfoDictionary = FilesRegister.MountFileRegisterDictionary();

        if (!Directory.Exists(outputRootDirectory))
        {
            LogsWindowViewModel.Instance.AddLog("Output directory does not exist.", Logger.LogTags.Error);
            return;
        }

        string[] allFiles = Directory.GetFiles(outputRootDirectory, "*", SearchOption.AllDirectories);

        List<string> listOfDeletedFiles = [];
        foreach (string file in allFiles)
        {
            string relativePath = StringUtils.GetRelativePathWithoutExtension(file, outputRootDirectory);

            // Check if the relativePath includes "Data" or is in Wwise dir, as I only want to cleanup datatables and audio
            if (!relativePath.Contains("Data", StringComparison.OrdinalIgnoreCase) &&
                !relativePath.Contains(packageWwiseDirectory, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Convert relativePath to lowercase for case insensitive comparison
            string lowerRelativePath = relativePath.ToLowerInvariant();

            if (!fileInfoDictionary.Keys.Any(key => key.Equals(lowerRelativePath, StringComparison.InvariantCultureIgnoreCase)))
            {
                listOfDeletedFiles.Add(relativePath);
                File.Delete(file);
                LogsWindowViewModel.Instance.AddLog($"Deleted file: {file}", Logger.LogTags.Info);
            }
        }

        LogsWindowViewModel.Instance.AddLog($"Deleted total of: {listOfDeletedFiles.Count} files", Logger.LogTags.Info);
        LogsWindowViewModel.Instance.AddLog("Deletion process completed.", Logger.LogTags.Info);
    }
}