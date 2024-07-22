using System;
using System.Collections.Generic;
using System.IO;

namespace UEParser;

public class GlobalVariables
{
    public static readonly string rootDir = AppDomain.CurrentDomain.BaseDirectory;
    public static readonly string pathToExtractedAssets = Path.Combine(rootDir, "Dependencies", "ExtractedAssets");
    public static readonly string pathToDynamicAssets = Path.Combine(rootDir, "Output", "DynamicAssets");
    public static readonly string pathToParsedData = Path.Combine(rootDir, "Output", "ParsedData");
    public static readonly string pathToKrakenApi = Path.Combine(rootDir, "Output", "API");
    public static readonly string versionWithBranch = Helpers.ConstructVersionHeaderWithBranch(); // Ex. "8.1.0_ptb"

    // List of assets that cause fatal crash of the app and cannot be parsed!
    public static readonly List<string> fatalCrashAssets = [
        "DeadByDaylight/Content/Effects/Niagara/NiagaraParticleSystem/Snowman/NS_Snowman_Destroy_Hit_Smoke",
        "DeadByDaylight/Content/Effects/Niagara/NiagaraParticleSystem/Halloween2023/VoidTile/NS_VoidTile_Halloween2023_Pillar",
        "DeadByDaylight/Content/Effects/Niagara/NiagaraParticleSystem/Slasher/K35/Mori/NS_K35_Mori_BloodMistDissolve"
    ];
}