using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse_Conversion.UEFormat.Enums;
using CUE4Parse_Conversion.Animations;
using CUE4Parse_Conversion;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse.UE4.Assets.Exports.Material;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SkiaSharp;
using System.IO;
using System;
using CUE4Parse.UE4.Assets.Exports;
using UEParser.ViewModels;
using System.Collections.Generic;
using System.Linq;
using UEParser.Utils;

namespace UEParser.Parser;

public class FileWriter
{
    public static void SaveParsedDB<T>(Dictionary<string, T> data, string path, string tag)
    {
        try
        {
            var sortedObj = data.OrderBy(kvp =>
            {
                if (!int.TryParse(kvp.Key.Replace("Tome", ""), out int result))
                    return int.MaxValue; // If parsing fails, place it at the end
                return result;
            }, Comparer<int>.Default)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            string json = JsonConvert.SerializeObject(sortedObj, Formatting.Indented);

            var directoryPath = Path.GetDirectoryName(path);
            if (directoryPath != null)
            {
                Directory.CreateDirectory(directoryPath);
            }

            File.WriteAllText(path, json);

            string strippedPath = StringUtils.StripRootDir(path);

            LogsWindowViewModel.Instance.AddLog($"[{tag}] Database saved to: {strippedPath}", Logger.LogTags.Info);
        }
        catch (Exception ex)
        {
            LogsWindowViewModel.Instance.AddLog($"Error saving '{tag}' database: {ex}", Logger.LogTags.Error);
        }
    }

    public static void SaveJsonFile(string exportPath, string data)
    {
        try
        {
            string exportPathWithExtension = Path.ChangeExtension(exportPath, ".json");

            var directoryPath = Path.GetDirectoryName(exportPathWithExtension);
            if (directoryPath != null)
            {
                Directory.CreateDirectory(directoryPath);
            }

            File.WriteAllText(exportPathWithExtension, data);

        }
        catch (Exception ex)
        {
            Logger.SaveLog($"Error saving JSON file: {ex}", Logger.LogTags.Error);
        }
    }

    public static void SaveMeshes(UObject asset, string packagePath, string outputPath)
    {
        var exportOptions = new ExporterOptions
        {
            LodFormat = ELodFormat.FirstLod,
            MeshFormat = EMeshFormat.Gltf2,
            AnimFormat = EAnimFormat.ActorX,
            MaterialFormat = EMaterialFormat.AllLayersNoRef,
            TextureFormat = ETextureFormat.Png,
            CompressionFormat = EFileCompressionFormat.None,
            Platform = ETexturePlatform.DesktopMobile,
            SocketFormat = ESocketFormat.Bone,
            ExportMorphTargets = true,
            ExportMaterials = false
        };

        // Export the file to a temporary directory because TryWriteToDir method creates directories which I dont need
        var tempOutputDirectory = Path.Combine(Path.GetTempPath(), "ExportTemp");

        Directory.CreateDirectory(tempOutputDirectory);

        var toSave = new Exporter(asset, exportOptions);
        var directoryInfo = new DirectoryInfo(tempOutputDirectory);
        var success = toSave.TryWriteToDir(directoryInfo, out _, out var savedFilePath);

        if (success)
        {
            File.Move(savedFilePath, outputPath);
            LogsWindowViewModel.Instance.AddLog($"Exported mesh: {packagePath}", Logger.LogTags.Info);
        }

        // Clean up temporary directory
        Directory.Delete(tempOutputDirectory, true);
    }

    public static void SaveAnimations(UObject asset, string packagePath, string outputPath)
    {
        var exportOptions = new ExporterOptions
        {
            LodFormat = ELodFormat.FirstLod,
            MeshFormat = EMeshFormat.Gltf2,
            AnimFormat = EAnimFormat.ActorX,
            MaterialFormat = EMaterialFormat.AllLayersNoRef,
            TextureFormat = ETextureFormat.Png,
            CompressionFormat = EFileCompressionFormat.None,
            Platform = ETexturePlatform.DesktopMobile,
            SocketFormat = ESocketFormat.Bone,
            ExportMorphTargets = false,
            ExportMaterials = false
        };

        // Export the file to a temporary directory because TryWriteToDir method creates directories which I dont need
        var tempOutputDirectory = Path.Combine(Path.GetTempPath(), "ExportTemp");

        Directory.CreateDirectory(tempOutputDirectory);

        var toSave = new Exporter(asset, exportOptions);
        var directoryInfo = new DirectoryInfo(tempOutputDirectory);
        var success = toSave.TryWriteToDir(directoryInfo, out _, out var savedFilePath);

        if (success)
        {
            File.Move(savedFilePath, outputPath);
            LogsWindowViewModel.Instance.AddLog($"Exported animation: {packagePath}", Logger.LogTags.Info);
        }

        // Clean up temporary directory
        Directory.Delete(tempOutputDirectory, true);
    }

    public static void SavePngFile(string outputPath, string packagePath, UTexture texture)
    {
        try
        {
            var directoryPath = Path.GetDirectoryName(outputPath);
            if (directoryPath != null)
            {
                Directory.CreateDirectory(directoryPath);
            }

            var img = texture.Decode(ETexturePlatform.DesktopMobile);

            if (img != null)
            {
                using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.Read);
                // Now, we have exclusive access to the file and can proceed with writing to it
                img.Encode(SKEncodedImageFormat.Png, 100).SaveTo(fileStream);
                LogsWindowViewModel.Instance.AddLog($"Exported texture: {packagePath}", Logger.LogTags.Info);
            }
            else
            {
                LogsWindowViewModel.Instance.AddLog($"Error saving PNG file, decoded texture is null", Logger.LogTags.Error);
            }
        }
        catch (Exception ex)
        {
            LogsWindowViewModel.Instance.AddLog($"Error saving PNG file: {ex}", Logger.LogTags.Error);
        }
    }

    public static void SaveMemoryStreamFile(string exportPath, string exportData, string extension)
    {
        string exportPathWithExtension = Path.ChangeExtension(exportPath, $".{extension}");

        var directoryPath = Path.GetDirectoryName(exportPathWithExtension);
        if (directoryPath != null)
        {
            Directory.CreateDirectory(directoryPath);
        }

        File.WriteAllText(exportPathWithExtension, exportData);
    }
}
