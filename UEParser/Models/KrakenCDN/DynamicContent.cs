using System.Collections.Generic;

namespace UEParser.Models.KrakenCDN;

public class DynamicContent
{
    /// <summary>
    /// List of possible dynamic assets to download.
    /// </summary>
    public required List<Entry> Entries { get; set; }
}

public class Entry
{
    /// <summary>
    /// Asset hash.
    /// </summary>
    public required string ContentVersion { get; set; }

    /// <summary>
    /// Download strategy
    /// preferRemote -> download from CDN
    /// preferPackaged -> use locally packaged asset
    /// </summary>
    public required string DownloadStrategy { get; set; }

    /// <summary>
    /// Packaged path to file.
    /// </summary>
    public required string PackagedPath { get; set; }

    /// <summary>
    /// S3 bucket schema they use.
    /// </summary>
    public required string Schema { get; set; }

    /// <summary>
    /// URI to asset.
    /// </summary>
    public required string Uri { get; set; }

    // Let's deconstruct shall we
    public void Deconstruct(out string contentVersion, out string downloadStrategy, out string packagedPath, out string schema, out string uri)
    {
        contentVersion = ContentVersion;
        downloadStrategy = DownloadStrategy;
        packagedPath = PackagedPath;
        schema = Schema;
        uri = Uri;
    }
}