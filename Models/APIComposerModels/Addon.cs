using Newtonsoft.Json.Linq;

namespace UEParser.Models;

public class Addon
{
    /// <summary>
    /// The type of the addon.
    /// </summary>
    public required string Type { get; set; }

    /// <summary>
    /// The type of the item associated with the addon.
    /// </summary>
    public required string ItemType { get; set; }

    /// <summary>
    /// A JSON array of parent items.
    /// </summary>
    public required JArray ParentItem { get; set; }

    /// <summary>
    /// The ability of the killer associated with the addon.
    /// </summary>
    public required string KillerAbility { get; set; }

    /// <summary>
    /// The name of the addon.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// A brief description of the addon.
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// The role associated with the addon.
    /// </summary>
    public required string Role { get; set; }

    /// <summary>
    /// The rarity of the addon.
    /// </summary>
    public required string Rarity { get; set; }

    /// <summary>
    /// Indicates whether the addon can be used after the event has ended.
    /// </summary>
    public bool CanBeUsedAfterEvent { get; set; }

    /// <summary>
    /// Indicates whether the addon is available in the bloodweb.
    /// </summary>
    public bool Bloodweb { get; set; }

    /// <summary>
    /// The path to the image associated with the addon.
    /// </summary>
    public required string Image { get; set; }
}