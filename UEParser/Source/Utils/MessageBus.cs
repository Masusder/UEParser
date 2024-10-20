using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using UEParser.Models.Netease;

namespace UEParser.Utils;

public static class MessageBus
{
    private static readonly Subject<DownloadContentMessage> _downloadContentStream = new();

    public static IObservable<DownloadContentMessage> DownloadContentStream => _downloadContentStream;

    public static void SendDownloadContentMessage(DownloadContentMessage message)
    {
        _downloadContentStream.OnNext(message);
    }
}

public class DownloadContentMessage(IEnumerable<ManifestFileData> selectedFiles, string version)
{
    public IEnumerable<ManifestFileData> SelectedFiles { get; set; } = selectedFiles;
    public string Version { get; set; } = version;
}