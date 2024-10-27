using System;
using System.IO;
using System.Collections.Generic;

namespace UEParser;

public static class GlobalVariables
{
    // Root directory
    public static readonly string RootDir = AppDomain.CurrentDomain.BaseDirectory;
    public static readonly string DotDataDir = Path.Combine(RootDir, ".data");

    #region Binaries/Scripts Paths
    public static readonly string BnkExtractorPath = Path.Combine(RootDir, ".data", "bnk-extract.exe");
    public static readonly string ModelsConverterScriptPath = Path.Combine(RootDir, ".data", "UEModelsConverter.py");
    //public static readonly string revorbPath = Path.Combine(rootDir, ".data", "revorb.exe");
    //public static readonly string ww2oggPath = Path.Combine(rootDir, ".data", "ww2ogg.exe");
    //public static readonly string packedCodebooksPath = Path.Combine(rootDir, ".data", "packed_codebooks_aoTuV_603.bin");
    public static readonly string WwiserPath = Path.Combine(RootDir, ".data", "wwiser.pyz");
    public static readonly string VgmStreamCliPath = Path.Combine(RootDir, ".data", "VgmStream", "vgmstream-cli.exe");
    public static readonly string FfmpegPath = Path.Combine(RootDir, ".data", "ffmpeg.exe");
    public static readonly string RepakPath = Path.Combine(RootDir, ".data", "Repak.exe");
    public static readonly string UnrealPakPath = Path.Combine(RootDir, ".data", "UnrealPak", "UnrealPak.exe");
    public static readonly string PreGeneratedWwnames = Path.Combine(RootDir, ".data", "wwnames.txt");
    public static readonly string TempDir = Path.Combine(Path.GetPathRoot(RootDir) ?? "C:\\", "WWISE");
    #endregion

    #region Directories Paths
    public static readonly string PathToExtractedAssets = Path.Combine(RootDir, "Dependencies", "ExtractedAssets");
    public static readonly string PathToExtractedAudio = Path.Combine(RootDir, "Dependencies", "ExtractedAudio");
    public static readonly string PathToDynamicAssets = Path.Combine(RootDir, "Output", "DynamicAssets");
    public static readonly string PathToParsedData = Path.Combine(RootDir, "Output", "ParsedData");
    public static readonly string PathToKraken = Path.Combine(RootDir, "Output", "Kraken");
    public static readonly string PathToModelsData = Path.Combine(RootDir, "Output", "ModelsData");
    public static readonly string PathToStructuredWwise = Path.Combine(PathToExtractedAudio, "WwiseStructured");
    public static readonly string PathToTemporaryWwise = Path.Combine(PathToExtractedAudio, "WwiseTemporary");
    public static readonly string PathToNetease = Path.Combine(RootDir, "Output", "Netease");
    #endregion

    #region Other
    public static readonly string VersionWithBranch = Helpers.ConstructVersionHeaderWithBranch(); // Ex. "8.1.0_ptb"
    public static readonly string CompareVersionWithBranch = Helpers.ConstructVersionHeaderWithBranch(true);
    public const string DbdinfoBaseUrl = "https://dbd-info.com/";
    public const string PlatformType = "naxx2jp"; // Can also be "ena" but this is useless as its content is always behind "naxx2jp"
    #endregion

    // List of assets that cause fatal crash of the app and cannot be parsed!
    public static readonly List<string> FatalCrashAssets = [
        "DeadByDaylight/Content/Effects/Niagara/NiagaraParticleSystem/Snowman/NS_Snowman_Destroy_Hit_Smoke",
        "DeadByDaylight/Content/Effects/Niagara/NiagaraParticleSystem/Halloween2023/VoidTile/NS_VoidTile_Halloween2023_Pillar",
        "DeadByDaylight/Content/Effects/Niagara/NiagaraParticleSystem/Slasher/K35/Mori/NS_K35_Mori_BloodMistDissolve",
        "DeadByDaylight/Plugins/Runtime/Bhvr/DBDCharacters/K37/Content/Blueprints/Abilities/FormSwitchingAbility/BP_K37FormSwitchingAbility" // This one doesn't cause a crash, but the parsing fails, let's keep it here to prevent errors
    ];
}