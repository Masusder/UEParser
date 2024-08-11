using System;
using System.Collections.Generic;
using System.IO;

namespace UEParser;

public class GlobalVariables
{
    // Paths
    public static readonly string rootDir = AppDomain.CurrentDomain.BaseDirectory;

    // Binaries/Scripts paths
    public static readonly string bnkExtractorPath = Path.Combine(rootDir, ".data", "bnk-extract.exe");
    public static readonly string modelsConverterScriptPath = Path.Combine(rootDir, ".data", "UEModelsConverter.py");
    public static readonly string revorbPath = Path.Combine(rootDir, ".data", "revorb.exe");
    public static readonly string ww2oggPath = Path.Combine(rootDir, ".data", "ww2ogg.exe");
    public static readonly string packedCodebooksPath = Path.Combine(rootDir, ".data", "packed_codebooks_aoTuV_603.bin");

    public static readonly string pathToExtractedAssets = Path.Combine(rootDir, "Dependencies", "ExtractedAssets");
    public static readonly string pathToExtractedAudio = Path.Combine(rootDir, "Dependencies", "ExtractedAudio");
    public static readonly string pathToDynamicAssets = Path.Combine(rootDir, "Output", "DynamicAssets");
    public static readonly string pathToParsedData = Path.Combine(rootDir, "Output", "ParsedData");
    public static readonly string pathToKrakenApi = Path.Combine(rootDir, "Output", "API");
    public static readonly string pathToModelsData = Path.Combine(rootDir, "Output", "ModelsData");
    public static readonly string pathToStructuredWwise = Path.Combine(rootDir, pathToExtractedAudio, "WwiseStructured");

    // Other
    public static readonly string versionWithBranch = Helpers.ConstructVersionHeaderWithBranch(); // Ex. "8.1.0_ptb"
    public static readonly string compareVersionWithBranch = Helpers.ConstructVersionHeaderWithBranch(true);

    // List of assets that cause fatal crash of the app and cannot be parsed!
    public static readonly List<string> fatalCrashAssets = [
        "DeadByDaylight/Content/Effects/Niagara/NiagaraParticleSystem/Snowman/NS_Snowman_Destroy_Hit_Smoke",
        "DeadByDaylight/Content/Effects/Niagara/NiagaraParticleSystem/Halloween2023/VoidTile/NS_VoidTile_Halloween2023_Pillar",
        "DeadByDaylight/Content/Effects/Niagara/NiagaraParticleSystem/Slasher/K35/Mori/NS_K35_Mori_BloodMistDissolve"
    ];
}