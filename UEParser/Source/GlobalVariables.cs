using System;
using System.Collections.Generic;
using System.IO;

namespace UEParser;

public class GlobalVariables
{
    // Root directory
    public static readonly string rootDir = AppDomain.CurrentDomain.BaseDirectory;

    #region BINARIES/SCRIPTS PATHS
    // Binaries/Scripts paths
    public static readonly string bnkExtractorPath = Path.Combine(rootDir, ".data", "bnk-extract.exe");
    public static readonly string modelsConverterScriptPath = Path.Combine(rootDir, ".data", "UEModelsConverter.py");
    //public static readonly string revorbPath = Path.Combine(rootDir, ".data", "revorb.exe");
    //public static readonly string ww2oggPath = Path.Combine(rootDir, ".data", "ww2ogg.exe");
    //public static readonly string packedCodebooksPath = Path.Combine(rootDir, ".data", "packed_codebooks_aoTuV_603.bin");
    public static readonly string wwiserPath = Path.Combine(rootDir, ".data", "wwiser.pyz");
    public static readonly string vgmStreamCliPath = Path.Combine(rootDir, ".data", "VgmStream", "vgmstream-cli.exe");
    public static readonly string ffmpegPath = Path.Combine(rootDir, ".data", "ffmpeg.exe");
    #endregion

    #region DIRECTORIES PATHS
    // Directories paths
    public static readonly string pathToExtractedAssets = Path.Combine(rootDir, "Dependencies", "ExtractedAssets");
    public static readonly string pathToExtractedAudio = Path.Combine(rootDir, "Dependencies", "ExtractedAudio");
    public static readonly string pathToDynamicAssets = Path.Combine(rootDir, "Output", "DynamicAssets");
    public static readonly string pathToParsedData = Path.Combine(rootDir, "Output", "ParsedData");
    public static readonly string pathToKraken = Path.Combine(rootDir, "Output", "Kraken");
    public static readonly string pathToModelsData = Path.Combine(rootDir, "Output", "ModelsData");
    public static readonly string pathToStructuredWwise = Path.Combine(pathToExtractedAudio, "WwiseStructured");
    public static readonly string pathToTemporaryWwise = Path.Combine(pathToExtractedAudio, "WwiseTemporary");
    #endregion

    #region OTHER
    // Other
    public static readonly string versionWithBranch = Helpers.ConstructVersionHeaderWithBranch(); // Ex. "8.1.0_ptb"
    public static readonly string compareVersionWithBranch = Helpers.ConstructVersionHeaderWithBranch(true);
    #endregion

    public static readonly string dbdinfoBaseUrl = "https://dbd-info.com/";

    // List of assets that cause fatal crash of the app and cannot be parsed!
    public static readonly List<string> fatalCrashAssets = [
        "DeadByDaylight/Content/Effects/Niagara/NiagaraParticleSystem/Snowman/NS_Snowman_Destroy_Hit_Smoke",
        "DeadByDaylight/Content/Effects/Niagara/NiagaraParticleSystem/Halloween2023/VoidTile/NS_VoidTile_Halloween2023_Pillar",
        "DeadByDaylight/Content/Effects/Niagara/NiagaraParticleSystem/Slasher/K35/Mori/NS_K35_Mori_BloodMistDissolve",
        "DeadByDaylight/Plugins/Runtime/Bhvr/DBDCharacters/K37/Content/Blueprints/Abilities/FormSwitchingAbility/BP_K37FormSwitchingAbility" // This one doesn't cause a crash, but the parsing fails, let's keep it here to prevent errors
    ];
}