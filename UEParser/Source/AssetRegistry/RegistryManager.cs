using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Collections.Concurrent;
using UEParser.AssetRegistry.Wwise;
using UEParser.ViewModels;

namespace UEParser.AssetRegistry;

public class RegistryManager
{
    public static void WriteToUnifiedFile(string filePath, Dictionary<string, FilesRegister.FileInfo> assets, ConcurrentDictionary<string, WwiseRegister.AudioInfo> audio)
    {
        using var fileStream = new FileStream(filePath, FileMode.Create);
        using var compressionStream = new GZipStream(fileStream, CompressionLevel.Optimal);
        using var writer = new BinaryWriter(compressionStream);

        writer.Write(Encoding.ASCII.GetBytes("UINFO")); // Magic number
        writer.Write(Encoding.ASCII.GetBytes("ASSET")); // Section Identifier for assets
        writer.Write(assets.Count);
        foreach (var entry in assets)
        {
            WriteString(writer, entry.Key);
            WriteString(writer, entry.Value.Extension);
            writer.Write(entry.Value.Size);
        }

        writer.Write(Encoding.ASCII.GetBytes("AUDIO")); // Section Identifier for audio
        writer.Write(audio.Count);
        foreach (var entry in audio)
        {
            WriteString(writer, entry.Key);
            WriteString(writer, entry.Value.Id);
            WriteString(writer, entry.Value.Hash);
            writer.Write(entry.Value.Size);
        }
    }

    public static (Dictionary<string, FilesRegister.FileInfo> assets, ConcurrentDictionary<string, WwiseRegister.AudioInfo> audio) ReadFromUnifiedFile(string filePath)
    {
        var assets = new Dictionary<string, FilesRegister.FileInfo>();
        var audio = new ConcurrentDictionary<string, WwiseRegister.AudioInfo>();

        if (!File.Exists(filePath))
        {
#if DEBUG
            LogsWindowViewModel.Instance.AddLog("Couldn't read files registry, file doesn't exist", Logger.LogTags.Debug);
#endif
            return (assets, audio);
        }

        using var fileStream = new FileStream(filePath, FileMode.Open);
        using var compressionStream = new GZipStream(fileStream, CompressionMode.Decompress);
        using var reader = new BinaryReader(compressionStream);

        var magicNumber = Encoding.ASCII.GetString(reader.ReadBytes(5)); // Read magic number
        if (magicNumber != "UINFO")
        {
            throw new InvalidOperationException("Invalid file format");
        }

        while (true)
        {
            string? section = ReadSectionIdentifier(reader);
            if (section == null)
            {
                break; // End of stream or end of file
            }

            switch (section)
            {
                case "ASSET":
                    int assetCount = reader.ReadInt32();
                    for (int i = 0; i < assetCount; i++)
                    {
                        string path = ReadString(reader);
                        string extension = ReadString(reader);
                        long size = reader.ReadInt64();
                        assets[path] = new FilesRegister.FileInfo(extension, size);
                    }
                    break;

                case "AUDIO":
                    int audioCount = reader.ReadInt32();
                    for (int i = 0; i < audioCount; i++)
                    {
                        string path = ReadString(reader);
                        string id = ReadString(reader);
                        string hash = ReadString(reader);
                        long size = reader.ReadInt64();
                        audio[path] = new WwiseRegister.AudioInfo(id, hash, size);
                    }
                    break;

                default:
                    throw new InvalidOperationException($"Unknown section identifier '{section}' in file.");
            }
        }

        return (assets, audio);
    }

    static string? ReadSectionIdentifier(BinaryReader reader)
    {
        try
        {
            var bytes = reader.ReadBytes(5);
            if (bytes.Length < 5)
            {
                return null; // End of stream or incomplete data
            }
            return Encoding.ASCII.GetString(bytes);
        }
        catch (EndOfStreamException)
        {
            return null; // Handle end of stream
        }
    }

    static void WriteString(BinaryWriter writer, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        writer.Write(bytes.Length);
        writer.Write(bytes);
    }

    static string ReadString(BinaryReader reader)
    {
        int length = reader.ReadInt32();
        var bytes = reader.ReadBytes(length);
        return Encoding.UTF8.GetString(bytes);
    }
}