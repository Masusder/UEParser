using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse_Conversion.UEFormat.Enums;
using CUE4Parse_Conversion.Animations;
using CUE4Parse_Conversion;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse.UE4.Assets.Exports.Material;
using Newtonsoft.Json;
using SkiaSharp;
using System.IO;
using System;

namespace UEParser.Parser;

public class FileWriter
{
    // public static void SaveParsedDB<T>(T data, string path, string tag)
    // {
    //     try
    //     {
    //         string json = JsonConvert.SerializeObject(data, Formatting.Indented);

    //         File.WriteAllText(path, json);

    //         Logger.SaveLog($"[{tag}] Database saved to: {path}", Logger.LogTags.Info);
    //     }
    //     catch (Exception ex)
    //     {
    //         Logger.SaveLog($"Error saving '{tag}' database: {ex}", Logger.LogTags.Error);
    //     }
    // }

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

    public static void SaveMeshes(dynamic asset, string outputRootDirectory, string packagePath, string exportPath)
    {
        string exportPathWithExtension = Path.ChangeExtension(exportPath, ".psk");

        var exportOptions = new ExporterOptions
        {
            LodFormat = ELodFormat.FirstLod,
            MeshFormat = EMeshFormat.ActorX,
            AnimFormat = EAnimFormat.ActorX,
            MaterialFormat = EMaterialFormat.AllLayersNoRef,
            TextureFormat = ETextureFormat.Png,
            CompressionFormat = EFileCompressionFormat.None,
            Platform = ETexturePlatform.DesktopMobile,
            SocketFormat = ESocketFormat.Bone,
            ExportMorphTargets = true,
            ExportMaterials = false
        };

        var toSave = new Exporter(asset, exportOptions);
        var directoryInfo = new DirectoryInfo(outputRootDirectory);
        var success = toSave.TryWriteToDir(directoryInfo, out _, out var savedFilePath);

        if (success)
        {
            AssetsManager.MoveFilesToOutput(exportPathWithExtension, packagePath, "psk", savedFilePath);
        }
    }

    public static void SavePngFile(string exportPath, string packagePath, UTexture texture)
    {
        try
        {
            string exportPathWithExtension = Path.ChangeExtension(exportPath, ".png");

            var directoryPath = Path.GetDirectoryName(exportPathWithExtension);
            if (directoryPath != null)
            {
                Directory.CreateDirectory(directoryPath);
            }

            var img = texture.Decode(ETexturePlatform.DesktopMobile);

            if (img != null)
            {
                using (var fileStream = new FileStream(exportPathWithExtension, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
                {
                    // Now, we have exclusive access to the file and can proceed with writing to it
                    img.Encode(SKEncodedImageFormat.Png, 100).SaveTo(fileStream);
                }

                AssetsManager.MoveFilesToOutput(exportPathWithExtension, packagePath, "png", "");
            }
            else
            {
                Logger.SaveLog($"Error saving PNG file, decoded texture is null", Logger.LogTags.Error);
            }
        }
        catch (Exception ex)
        {
            Logger.SaveLog($"Error saving PNG file: {ex}", Logger.LogTags.Error);
        }
    }

    public static void SaveIniFile(string exportPath, dynamic exportData)
    {
        string exportPathWithExtension = Path.ChangeExtension(exportPath, ".ini");

        var directoryPath = Path.GetDirectoryName(exportPathWithExtension);
        if (directoryPath != null)
        {
            Directory.CreateDirectory(directoryPath);
        }

        File.WriteAllText(exportPathWithExtension, exportData);
    }
}
