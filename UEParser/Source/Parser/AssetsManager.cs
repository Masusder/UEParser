using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
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
using CUE4Parse.UE4.AssetRegistry;
using UEParser.Services;
using UEParser.Utils;
using UEParser.ViewModels;
using UEParser.AssetRegistry;
using UEParser.AssetRegistry.Wwise;
using UEParser.Parser.Wwise;

namespace UEParser.Parser;

public class AssetsManager
{
    private const string PackagePathToPaks = "DeadByDaylight/Content/Paks";

    private static DefaultFileProvider? _provider;
    private static readonly object LockObject = new();

    public static DefaultFileProvider Provider
    {
        get
        {
            if (_provider == null)
            {
                lock (LockObject)
                {
                    if (_provider == null)
                    {
                        InitializeCUE4Parse();
                    }
                }
            }
            return _provider!;
        }
    }

    public static void ForceReloadProvider()
    {
        lock (LockObject)
        {
            _provider = null;
        }

        // Call the provider again to reinitialize
        _ = Provider;
    }

    public static void InitializeCUE4Parse()
    {
        if (_provider != null)
        {
            // Provider is already initialized
            return;
        }

        lock (LockObject)
        {
            if (_provider != null)
            {
                // Double-check to ensure provider is still null inside the lock
                return;
            }

            LogsWindowViewModel.Instance.AddLog("[CUE4Parse] Initializing files provider..", Logger.LogTags.Info);

            var config = ConfigurationService.Config;

            string pathToGameDirectory = config.Core.PathToGameDirectory;
            if (pathToGameDirectory != null)
            {
                string pathToPaks = Path.Combine(pathToGameDirectory, PackagePathToPaks);

                var oodleDirectory = Path.Combine(GlobalVariables.RootDir, ".data");
                var oodlePath = Path.Combine(oodleDirectory, OodleHelper.OODLE_DLL_NAME);

                var mappingsPath = config.Core.MappingsPath;

                if (!File.Exists(mappingsPath))
                {
                    LogsWindowViewModel.Instance.AddLog("[CUE4Parse] Not found build mappings. You need to provide path to them manually in settings.", Logger.LogTags.Info);
                    return;
                }

                Directory.CreateDirectory(oodleDirectory);

                if (!File.Exists(oodlePath))
                {
                    OodleHelper.DownloadOodleDll(oodlePath);
                }

                OodleHelper.Initialize(oodlePath);

                _provider = new DefaultFileProvider(pathToPaks, SearchOption.TopDirectoryOnly, true, new VersionContainer(EGame.GAME_DeadByDaylight))
                {
                    MappingsContainer = new FileUsmapTypeMappingsProvider(mappingsPath)
                };

                _provider.CustomEncryption = _provider.Versions.Game switch
                {
                    EGame.GAME_DeadByDaylight => DBDAes.DbDDecrypt,
                    _ => DBDAes.DbDDecrypt
                };

                _provider.Initialize(); // will scan local files and read them to know what it has to deal with (PAK/UTOC/UCAS/UASSET/UMAP)
                _provider.SubmitKey(new FGuid(), new FAesKey(config.Core.AesKey)); // decrypt basic info (1 guid - 1 key)

                _provider.LoadLocalization(ELanguage.English);

                LogsWindowViewModel.Instance.AddLog("[CUE4Parse] Initialized successfully.", Logger.LogTags.Info);
            }
            else
            {
                LogsWindowViewModel.Instance.AddLog("Not found path to game directory set in settings.", Logger.LogTags.Error);
                LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
                return;
            }
        }
    }

    private static readonly string[] ExtensionsToSkip = ["ubulk", "uexp", "ufont"];
    private static readonly string OutputRootDirectory = Path.Combine(GlobalVariables.RootDir, "Dependencies", "ExtractedAssets");
    private const string PackageDataDirectory = "DeadByDaylight/Content/Data";
    private const string PackageCustomizationDirectory = "DeadByDaylight/Content/UI/UMGAssets/Icons/Customization";
    private const string PackageUMGAssetsDirectory = "DeadByDaylight/Content/UI/UMGAssets";
    private const string PackageUIDirectory = "DeadByDaylight/Content/UI/UMGAssets/Icons";
    private const string PackageCharactersDirectory = "DeadByDaylight/Content/Characters";
    private const string PackageMeshesDirectory = "DeadByDaylight/Content/Meshes";
    private const string PackageEffectsDirectory = "DeadByDaylight/Content/Effects";
    private const string PackagePluginsDirectory = "DeadByDaylight/Plugins";
    private const string PackageConfigDirectory = "DeadByDaylight/Config";
    private const string PackageLocalizationDirectory = "DeadByDaylight/Content/Localization";
    private const string PackageWwiseDirectory = "DeadByDaylight/Content/WwiseAudio";
    private const string PackageAssetsRegistryFile = "DeadByDaylight/AssetRegistry.bin";
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
                        if (GlobalVariables.FatalCrashAssets.Contains(pathWithoutExtension)) continue;

                        string extension = file.Extension;
                        if (ExtensionsToSkip.Contains(extension)) continue;

                        string pathWithExtension = file.Path;
                        long size = file.Size;

                        bool isInUiDirectory = pathWithExtension.Contains(PackageCustomizationDirectory);
                        bool isInDataDirectory = pathWithExtension.Contains(PackageDataDirectory);
                        bool isInCharactersDirectory = pathWithExtension.Contains(PackageCharactersDirectory);
                        bool isInMeshesDirectory = pathWithExtension.Contains(PackageMeshesDirectory);
                        bool isInEffectsDirectory = pathWithExtension.Contains(PackageEffectsDirectory);
                        bool isInPluginsDirectory = pathWithExtension.Contains(PackagePluginsDirectory);
                        bool isInConfigDirectory = pathWithExtension.Contains(PackageConfigDirectory);
                        bool isInLocalizationDirectory = pathWithExtension.Contains(PackageLocalizationDirectory);
                        bool isInWwiseDirectory = pathWithExtension.Contains(PackageWwiseDirectory);
                        bool isAssetsRegistry = pathWithExtension.Contains(PackageAssetsRegistryFile);

                        bool fileDataChanged = UpdateFileInfoIfNeeded(pathWithoutExtension, extension, size);

                        if (!fileDataChanged && !isInConfigDirectory) continue;

                        if (!isInUiDirectory &&
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

                        string exportPath = Path.Combine(OutputRootDirectory, pathWithoutExtension);

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

    public static async Task ParseMeshes(CancellationToken token)
    {
        await Task.Run(() =>
        {
            token.ThrowIfCancellationRequested();

            var files = Provider.Files.Values.ToList();
            var newAssets = FilesRegister.NewAssets;
            var modifiedAssets = FilesRegister.ModifiedAssets;
            int extractedAssetsCount = 0;

            LogsWindowViewModel.Instance.AddLog($"Detected total of {newAssets.Count} new assets.", Logger.LogTags.Info);
            LogsWindowViewModel.Instance.AddLog($"Detected total of {modifiedAssets.Count} modified assets.", Logger.LogTags.Info);

            foreach (var file in files)
            {
                token.ThrowIfCancellationRequested();

                try
                {
                    string pathWithoutExtension = file.PathWithoutExtension;
                    if (!newAssets.ContainsKey(pathWithoutExtension) && !modifiedAssets.ContainsKey(pathWithoutExtension)) continue;

                    if (GlobalVariables.FatalCrashAssets.Contains(pathWithoutExtension)) continue;

                    string extension = file.Extension;
                    if (ExtensionsToSkip.Contains(extension)) continue;

                    string pathWithExtension = file.Path;
                    long size = file.Size;

                    string versionWithBranch = Helpers.ConstructVersionHeaderWithBranch();
                    string outputDirectory = Path.Combine(GlobalVariables.RootDir, "Output", "ExtractedAssets", "Meshes", versionWithBranch);
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
        }, token);
    }

    public static async Task ParseAnimations(CancellationToken token)
    {
        await Task.Run(() =>
        {
            token.ThrowIfCancellationRequested();

            var files = Provider.Files.Values.ToList();
            var newAssets = FilesRegister.NewAssets;
            var modifiedAssets = FilesRegister.ModifiedAssets;
            int extractedAssetsCount = 0;

            LogsWindowViewModel.Instance.AddLog($"Detected total of {newAssets.Count} new assets.", Logger.LogTags.Info);
            LogsWindowViewModel.Instance.AddLog($"Detected total of {modifiedAssets.Count} modified assets.", Logger.LogTags.Info);

            foreach (var file in files)
            {
                token.ThrowIfCancellationRequested();

                try
                {
                    string pathWithoutExtension = file.PathWithoutExtension;
                    if (!newAssets.ContainsKey(pathWithoutExtension) && !modifiedAssets.ContainsKey(pathWithoutExtension)) continue;

                    if (GlobalVariables.FatalCrashAssets.Contains(pathWithoutExtension)) continue;

                    string extension = file.Extension;
                    if (ExtensionsToSkip.Contains(extension)) continue;

                    string pathWithExtension = file.Path;
                    long size = file.Size;

                    string versionWithBranch = Helpers.ConstructVersionHeaderWithBranch();
                    string outputDirectory = Path.Combine(GlobalVariables.RootDir, "Output", "ExtractedAssets", "Animations", versionWithBranch);
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
        }, token);
    }

    public static async Task ParseTextures(CancellationToken token)
    {
        await Task.Run(() =>
        {
            token.ThrowIfCancellationRequested();

            var files = Provider.Files.Values.ToList();
            var newAssets = FilesRegister.NewAssets;
            var modifiedAssets = FilesRegister.ModifiedAssets;
            int extractedAssetsCount = 0;

            LogsWindowViewModel.Instance.AddLog($"Detected total of {newAssets.Count} new assets.", Logger.LogTags.Info);
            LogsWindowViewModel.Instance.AddLog($"Detected total of {modifiedAssets.Count} modified assets.", Logger.LogTags.Info);

            foreach (var file in files)
            {
                token.ThrowIfCancellationRequested();

                try
                {
                    string pathWithoutExtension = file.PathWithoutExtension;
                    if (!newAssets.ContainsKey(pathWithoutExtension) && !modifiedAssets.ContainsKey(pathWithoutExtension)) continue;

                    if (GlobalVariables.FatalCrashAssets.Contains(pathWithoutExtension)) continue;

                    string extension = file.Extension;
                    if (ExtensionsToSkip.Contains(extension)) continue;

                    string pathWithExtension = file.Path;
                    bool isInUiDirectory = pathWithExtension.Contains(PackageUMGAssetsDirectory);
                    if (isInUiDirectory) continue;
                    long size = file.Size;

                    string versionWithBranch = Helpers.ConstructVersionHeaderWithBranch();
                    string outputDirectory = Path.Combine(GlobalVariables.RootDir, "Output", "ExtractedAssets", "Textures", versionWithBranch);
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
        }, token);
    }

    public static async Task ParseUi(CancellationToken token)
    {
        await Task.Run(() =>
        {
            token.ThrowIfCancellationRequested();

            var files = Provider.Files.Values.ToList();
            var newAssets = FilesRegister.NewAssets;
            var modifiedAssets = FilesRegister.ModifiedAssets;
            int extractedAssetsCount = 0;

            LogsWindowViewModel.Instance.AddLog($"Detected total of {newAssets.Count} new assets.", Logger.LogTags.Info);
            LogsWindowViewModel.Instance.AddLog($"Detected total of {modifiedAssets.Count} modified assets.", Logger.LogTags.Info);

            foreach (var file in files)
            {
                token.ThrowIfCancellationRequested();

                try
                {
                    string pathWithoutExtension = file.PathWithoutExtension;
                    if (!newAssets.ContainsKey(pathWithoutExtension) && !modifiedAssets.ContainsKey(pathWithoutExtension)) continue;

                    if (GlobalVariables.FatalCrashAssets.Contains(pathWithoutExtension)) continue;

                    string extension = file.Extension;
                    if (ExtensionsToSkip.Contains(extension)) continue;

                    string pathWithExtension = file.Path;
                    bool isInUiDirectory = pathWithExtension.Contains(PackageUIDirectory);
                    if (!isInUiDirectory) continue;

                    long size = file.Size;

                    string versionWithBranch = Helpers.ConstructVersionHeaderWithBranch();
                    string outputDirectory = Path.Combine(GlobalVariables.RootDir, "Output", "ExtractedAssets", "UI", versionWithBranch);
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
        }, token);
    }

    public static async Task ParseAudio(CancellationToken token)
    {
        LogsWindowViewModel.Instance.AddLog("Audio extraction is highly intensive process, which may take even up to an hour, depending whether audio registry is available..", Logger.LogTags.Info);
        await Task.Run(async () =>
        {
            LogsWindowViewModel.Instance.AddLog("Moving compressed audio into temporary folder..", Logger.LogTags.Info);
            Helpers.ExecuteWithCancellation(() => WwiseFileHandler.MoveCompressedAudio(token), token);

            LogsWindowViewModel.Instance.AddLog("Unpacking audio banks..", Logger.LogTags.Info);
            await Helpers.ExecuteWithCancellation(() => WwiseFileHandler.UnpackAudioBanks(), token);

            LogsWindowViewModel.Instance.AddLog("Generating txtp files from compiled audio..", Logger.LogTags.Info);
            Helpers.ExecuteWithCancellation(() => WwiseFileHandler.GenerateTxtp(), token);

            LogsWindowViewModel.Instance.AddLog("Handling truncated txtp files..", Logger.LogTags.Info);
            Helpers.ExecuteWithCancellation(() => WwiseFileHandler.RenameAndMoveTruncatedTxtpFiles(), token);

            LogsWindowViewModel.Instance.AddLog("Collecting associated audio event IDs..", Logger.LogTags.Info);
            var associatedAudioEventIds = Helpers.ExecuteWithCancellation(() => WwiseFileHandler.GrabAudioEventIds(), token);

            LogsWindowViewModel.Instance.AddLog("Collecting Wwise data from preallocated buffers..", Logger.LogTags.Info);
            var audioEventsLinkage = Helpers.ExecuteWithCancellation(() => WwiseFileHandler.ConstructAudioEventsLinkage(), token);

            LogsWindowViewModel.Instance.AddLog("Populating audio registry..", Logger.LogTags.Info);
            Helpers.ExecuteWithCancellation(() => WwiseRegister.PopulateAudioRegister(), token);
            Helpers.ExecuteWithCancellation(() => WwiseRegister.SaveAudioInfoDictionary(), token);

            LogsWindowViewModel.Instance.AddLog("Converting audio to WAV audio format..", Logger.LogTags.Info);
            Helpers.ExecuteWithCancellation(() => WwiseFileHandler.ConvertTxtpToWav(token), token);

            LogsWindowViewModel.Instance.AddLog("Reversing audio structure..", Logger.LogTags.Info);
            Helpers.ExecuteWithCancellation(() => WwiseFileHandler.ReverseAudioStructure(associatedAudioEventIds, audioEventsLinkage), token);

            LogsWindowViewModel.Instance.AddLog("Moving converted audio into output directory..", Logger.LogTags.Info);
            Helpers.ExecuteWithCancellation(() => WwiseFileHandler.MoveAudioToOutput(), token);

            LogsWindowViewModel.Instance.AddLog("Deleting temporary audio folder..", Logger.LogTags.Info);
            Helpers.ExecuteWithCancellation(() => WwiseFileHandler.CleanExtractedAudioDir(), token);
        }, token);
    }

    public static async Task ParseMissingAssets(List<string> missingAssetsList, CancellationToken token)
    {
        await Task.Run(() =>
        {
            token.ThrowIfCancellationRequested();

            var files = Provider.Files.Values.ToList();

            foreach (var file in files)
            {
                token.ThrowIfCancellationRequested();

                try
                {
                    string pathWithoutExtension = file.PathWithoutExtension;

                    if (GlobalVariables.FatalCrashAssets.Contains(pathWithoutExtension)) continue;
                    if (!missingAssetsList.Contains(pathWithoutExtension)) continue;

                    string extension = file.Extension;
                    if (ExtensionsToSkip.Contains(extension)) continue;

                    string pathWithExtension = file.Path;
                    long size = file.Size;

                    string exportPath = Path.Combine(OutputRootDirectory, pathWithoutExtension);

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
        }, token);
    }

    // We need to update game config for initialization check
    public static async Task UpdateGameIni()
    {
        await Task.Run(() =>
        {
            try
            {
                var files = Provider.Files.Values.ToList();

                var file = files.FirstOrDefault(f => f.Path == "DeadByDaylight/Config/DefaultGame.ini");

                if (file == null) return;

                string pathWithoutExtension = file.PathWithoutExtension;

                string extension = file.Extension;

                string pathWithExtension = file.Path;
                long size = file.Size;

                string exportPath = Path.Combine(OutputRootDirectory, pathWithoutExtension);

                switch (extension)
                {
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
                LogsWindowViewModel.Instance.AddLog($"Failed to update .ini: {ex}", Logger.LogTags.Error);
                LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            }

        });
    }

    private static bool UpdateFileInfoIfNeeded(string packagePath, string extension, long size)
    {
        var fileInfo = FilesRegister.GetFileInfo(packagePath);
        bool fileDataChanged = fileInfo == null || fileInfo.Size != size;

        if (fileDataChanged)
        {
            Logger.SaveLog($"Asset size changed: {packagePath}", Logger.LogTags.Info, Logger.ELogExtraTag.None);
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

        if (!Directory.Exists(OutputRootDirectory))
        {
            LogsWindowViewModel.Instance.AddLog("Output directory does not exist.", Logger.LogTags.Error);
            return;
        }

        string[] allFiles = Directory.GetFiles(OutputRootDirectory, "*", SearchOption.AllDirectories);

        List<string> listOfDeletedFiles = [];
        foreach (string file in allFiles)
        {
            string relativePath = StringUtils.GetRelativePathWithoutExtension(file, OutputRootDirectory);

            // Check if the relativePath includes "Data" or is in Wwise dir, as I only want to clean up datatables and audio
            if (!relativePath.Contains("Data", StringComparison.OrdinalIgnoreCase) &&
                !relativePath.Contains(PackageWwiseDirectory, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Convert relativePath to lowercase for case-insensitive comparison
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