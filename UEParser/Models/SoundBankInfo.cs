using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UEParser.Models;

public class SoundBanksInfoRoot
{
    public required SoundBanksInfo SoundBanksInfo { get; set; }
}

public class SoundBanksInfo
{
    public required string Platform { get; set; }
    public required string BasePlatform { get; set; }
    public required string SchemaVersion { get; set; }
    public required string SoundBankVersion { get; set; }
    public required RootPaths RootPaths { get; set; }
    public required List<object> DialogueEvents { get; set; }
    public required List<SoundBank> SoundBanks { get; set; }
}

public class RootPaths
{
    public required string ProjectRoot { get; set; }
    public required string SourceFilesRoot { get; set; }
    public required string SoundBanksRoot { get; set; }
    public required string ExternalSourcesInputFile { get; set; }
    public required string ExternalSourcesOutputRoot { get; set; }
}

public class SoundBank
{
    public required string Id { get; set; }
    public required string Type { get; set; }
    public required string GUID { get; set; }
    public required string Language { get; set; }
    public required string Hash { get; set; }
    public required string ObjectPath { get; set; }
    public required string ShortName { get; set; }
    public required string Path { get; set; }
    public required List<Media> Media { get; set; }
}

public class Media
{
    public required string Id { get; set; }
    public required string Language { get; set; }
    public required string Streaming { get; set; }
    public required string Location { get; set; }
    public required string ShortName { get; set; }
    public required string CachePath { get; set; }
}