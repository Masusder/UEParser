using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using UEParser.Models.Netease;
using UEParser.Services;
using UEParser.ViewModels;
using UEParser.Utils;

namespace UEParser.Netease;

public class ContentDownloader(NeteaseViewModel viewModel)
{
    private readonly NeteaseViewModel ViewModel = viewModel;

    public async Task ConstructFilePathAndDownloadAsync(ManifestFileData fileData, string version, string platform, CancellationToken token)
    {
        string url = FormNeteaseDownloadUrl(version, fileData.FilePathWithExtension);
        string exportPath = Path.Combine(GlobalVariables.pathToNetease, platform, version, $"{fileData.FilePath}.{fileData.FileExtension}");
        Directory.CreateDirectory(Path.GetDirectoryName(exportPath)!);

        await DownloadFileAsync(url, exportPath, fileData, token);
    }

    public async Task DownloadFileAsync(string url, string filePath, ManifestFileData fileData, CancellationToken token)
    {
        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token);
        var totalBytes = response.Content.Headers.ContentLength ?? 0;

        ViewModel.MaxSize = StringUtils.FormatBytes(totalBytes);
        ViewModel.FileName = fileData.FilePathWithExtension;

        FileStream? fileStream = null;
        try
        {
            fileStream = File.Create(filePath);
            using var contentStream = await response.Content.ReadAsStreamAsync(token);
            var buffer = new byte[8192];
            long totalRead = 0;
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer, token)) > 0)
            {
                token.ThrowIfCancellationRequested();
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), token);
                totalRead += bytesRead;

                ViewModel.CurrentSize = StringUtils.FormatBytes(totalRead);
                ViewModel.ProgressPercentage = (double)totalRead / totalBytes * 100;
            }

            await fileStream.FlushAsync(token);
        }
        finally
        {
            fileStream?.Dispose(); // Explicitly disposing in case using block doesn't release fully
        }

        await Task.Delay(100, token);

        if (fileData.FileExtension == "pak")
        {
            await ContentManager.ChangeMagicValue(filePath);
        }
    }

    private static string FormNeteaseDownloadUrl(string version, string FilePathWithExtension)
    {
        var config = ConfigurationService.Config;
        string endpoint = string.Format(config.Netease.ContentConfig.NeteaseContentCdnEndpoint, config.Netease.Platform.ToString(), version, FilePathWithExtension);
        string url = string.Format(config.Netease.ContentConfig.NeteaseContentCdnBaseUrl, GlobalVariables.PlatformType) + endpoint;

        return url;
    }
}