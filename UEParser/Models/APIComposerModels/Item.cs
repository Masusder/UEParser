namespace UEParser.Models;

public class Item
{
    /// <summary>
    /// The ability required to use the item.
    /// </summary>
    public required string RequiredAbility { get; set; }

    /// <summary>
    /// The role associated with the item.
    /// </summary>
    public required string Role { get; set; }

    /// <summary>
    /// The rarity of the item.
    /// </summary>
    public required string Rarity { get; set; }

    /// <summary>
    /// The type of the item.
    /// </summary>
    public required string Type { get; set; }

    /// <summary>
    /// The specific item type.
    /// </summary>
    public required string ItemType { get; set; }

    /// <summary>
    /// The name of the item.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// A brief description of the item.
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// The file path to the item's icon.
    /// </summary>
    public required string IconFilePathList { get; set; }

    /// <summary>
    /// Indicates whether item can be in inventory.
    /// </summary>
    public required bool Inventory { get; set; }

    /// <summary>
    /// Indicates whether item can appear in a chest.
    /// </summary>
    public required bool Chest { get; set; }

    /// <summary>
    /// Indicates whether item can appear in the bloodweb.
    /// </summary>
    public required bool Bloodweb { get; set; }

    /// <summary>
    /// Indicates whether item can be used by bots.
    /// </summary>
    public required bool IsBotSupported { get; set; }
}
