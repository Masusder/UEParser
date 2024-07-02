using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace UEParser.Models;

public class Configuration
{
    public CoreConfig Core { get; set; }
    public NeteaseConfig Netease { get; set; }
    public GlobalConfig Global { get; set; }

    public Configuration()
    {
        Core = new CoreConfig();
        Netease = new NeteaseConfig();
        Global = new GlobalConfig();
    }
}

// Global config
public class GlobalConfig
{
    public Dictionary<string, string> BranchRoots { get; set; }
    public bool UpdateAPIDuringInitialization { get; set; }

    public GlobalConfig()
    {
        BranchRoots = [];
        UpdateAPIDuringInitialization = false;
    }
}

// Config for core
public class CoreConfig
{
    public string BuildVersionNumber { get; set; }
    public string PathToGameDirectory { get; set; }
    public string MappingsPath { get; set; }
    public string AesKey { get; set; }
    public List<string> TomesList { get; set; }
    public List<string> EventTomesList { get; set; }
    public VersionData VersionData { get; set; }
    public KrakenApiConfig ApiConfig { get; set; }
    //public HTMLTagConverters HTMLTagConverters { get; set; }

    public CoreConfig()
    {
        BuildVersionNumber = "";
        PathToGameDirectory = "";
        MappingsPath = "";
        AesKey = "";
        TomesList = [];
        EventTomesList = [];
        VersionData = new VersionData();
        ApiConfig = new KrakenApiConfig();
        //HTMLTagConverters = new HTMLTagConverters();
    }
}

// Version data
public class VersionData
{
    public string LatestVersionHeader { get; set; }
    [JsonConverter(typeof(StringEnumConverter))]
    public Branch Branch { get; set; }
    public string CompareVersionHeader { get; set; }
    [JsonConverter(typeof(StringEnumConverter))]
    public Branch CompareBranch { get; set; }

    public VersionData()
    {
        LatestVersionHeader = "";
        CompareVersionHeader = "";
        Branch = Branch.live;
        CompareBranch = Branch.live;
    }
}

// Kraken (API/CDN)
public class KrakenApiConfig
{
    public string LatestVersion { get; set; }
    public string ApiBaseUrl { get; set; }
    public string SteamApiBaseUrl { get; set; }
    public string CdnBaseUrl { get; set; }
    public string CdnContentSegment { get; set; }
    public Dictionary<string, string> DynamicCdnEndpoints { get; set; }
    public Dictionary<string, string> CdnEndpoints { get; set; }
    public Dictionary<string, string> ApiEndpoints { get; set; }
    public Dictionary<string, string> S3AccessKeys { get; set; }

    public KrakenApiConfig()
    {
        LatestVersion = "";
        ApiBaseUrl = "";
        SteamApiBaseUrl = "";
        CdnBaseUrl = "";
        CdnContentSegment = "";
        DynamicCdnEndpoints = [];
        CdnEndpoints = [];
        ApiEndpoints = [];
        S3AccessKeys = [];
    }
}

// Netease config
public class NeteaseConfig
{
    public string ExtractedVersion { get; set; }
    public string NeteaseVersion { get; set; }
    public string BackupNeteaseVersion { get; set; }
    [JsonConverter(typeof(StringEnumConverter))]
    public Branch Branch { get; set; }
    [JsonConverter(typeof(StringEnumConverter))]
    public Platform Platform { get; set; }
    public string PathToGameDirectory { get; set; }
    public NeteaseApiConfig ApiConfig { get; set; }

    public NeteaseConfig()
    {
        ExtractedVersion = "";
        NeteaseVersion = "";
        BackupNeteaseVersion = "";
        Branch = Branch.live;
        Platform = Platform.ios;
        PathToGameDirectory = "";
        ApiConfig = new NeteaseApiConfig();
    }
}

// Netease API config
public class NeteaseApiConfig
{
    public string LatestVersion { get; set; }
    public string ApiBaseUrl { get; set; }
    public string CdnBaseUrl { get; set; }
    public string CdnContentSegment { get; set; }
    public Dictionary<string, string> CdnEndpoints { get; set; }
    public Dictionary<string, string> ApiEndpoints { get; set; }

    public NeteaseApiConfig()
    {
        LatestVersion = "";
        ApiBaseUrl = "";
        CdnBaseUrl = "";
        CdnContentSegment = "";
        CdnEndpoints = [];
        ApiEndpoints = [];
    }
}

// HTML Tag converters
//public class HTMLTagConverters
//{
//    public TomeGlyphs TomeGlyphs { get; set; }
//    public TomeCoreMemory TomeCoreMemory { get; set; }

//    public HTMLTagConverters()
//    {
//        TomeGlyphs = new TomeGlyphs();
//        TomeCoreMemory = new TomeCoreMemory();
//    }
//}

//// Tome glyphs
//public class TomeGlyphs
//{
//    public List<string> GlyphsArray { get; set; }
//    public Dictionary<string, GlyphDetail> GlyphDetails { get; set; }

//    public TomeGlyphs()
//    {
//        GlyphsArray = [];
//        GlyphDetails = [];
//    }
//}

//// Glyph detail
//public class GlyphDetail
//{
//    public string? Id { get; set; }
//    public string? Html { get; set; }
//}

// Tome core memory
//public class TomeCoreMemory
//{
//    public List<string> CoreMemoryArray { get; set; }
//    public Dictionary<string, CoreMemoryDetail> CoreMemoryDetails { get; set; }

//    public TomeCoreMemory()
//    {
//        CoreMemoryArray = [];
//        CoreMemoryDetails = [];
//    }
//}

// Core memory detail
//public class CoreMemoryDetail
//{
//    public string? Id { get; set; }
//    public string? Html { get; set; }
//}

// Game branches
public enum Branch
{
    live,
    ptb,
    dev,
    qa,
    stage,
    cert,
    uat
}

public enum Platform
{
    ios,
    android
}