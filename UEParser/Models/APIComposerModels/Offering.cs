using Newtonsoft.Json.Linq;

namespace UEParser.Models;

public class Offering
{
    public required string Type { get; set; }
    public required string[] StatusEffects { get; set; }
    public required JArray Tags { get; set; }
    public required string Available { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required string Role { get; set; }
    public required string Rarity { get; set; }
    public required string Image { get; set; }
}