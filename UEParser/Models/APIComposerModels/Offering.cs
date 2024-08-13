using Newtonsoft.Json.Linq;

namespace UEParser.Models;

public class Offering
{
    /// <summary>
    /// The type of the offering.
    /// </summary>
    public required string Type { get; set; }

    /// <summary>
    /// A list of status effects associated with the offering.
    /// </summary>
    public required string[] StatusEffects { get; set; }

    /// <summary>
    /// A collection of tags providing additional classification for the offering.
    /// </summary>
    public required JArray Tags { get; set; }

    /// <summary>
    /// Availability status of the offering.
    /// </summary>
    public required string Available { get; set; }

    /// <summary>
    /// The name of the offering.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// A detailed description of the offering, explaining its effects or purpose.
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// The role associated with the offering.
    /// </summary>
    public required string Role { get; set; }

    /// <summary>
    /// The rarity level of the offering.
    /// </summary>
    public required string Rarity { get; set; }

    /// <summary>
    /// The path to the image representing the offering.
    /// </summary>
    public required string Image { get; set; }
}
