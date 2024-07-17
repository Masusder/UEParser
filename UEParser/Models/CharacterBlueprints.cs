using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace UEParser.Models;

public class CharacterBlueprintsModel
{
    public required Dictionary<string, CharacterData> Characters { get; set; }
    public required Dictionary<string, CosmeticData> Cosmetics { get; set; }
}

public class CharacterData
{
    public required string GameBlueprint { get; set; }
    public required string MenuBlueprint { get; set; }
}

public class CosmeticData
{
    public required JArray CosmeticItems { get; set; }
    public required string GameBlueprint { get; set; }
    public required string MenuBlueprint { get; set; }
}
