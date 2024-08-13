namespace UEParser.Models;

public class Map
{
    /// <summary>
    /// Represents the realm map is apart of.
    /// </summary>
    public required string Realm { get; set; }

    /// <summary>
    /// Unique identifier for the map.
    /// </summary>
    public string? MapId { get; set; }

    /// <summary>
    /// The name of the map.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// A detailed description of the map.
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// Minimum allowable distance between hooks on the map.
    /// </summary>
    public double HookMinDistance { get; set; }

    /// <summary>
    /// Minimum number of hooks that can appear on the map.
    /// </summary>
    public int HookMinCount { get; set; }

    /// <summary>
    /// Maximum number of hooks that can appear on the map.
    /// </summary>
    public int HookMaxCount { get; set; }

    /// <summary>
    /// Minimum allowable distance between pallets on the map.
    /// </summary>
    public double PalletsMinDistance { get; set; }

    /// <summary>
    /// Minimum number of pallets that can appear on the map.
    /// </summary>
    public int PalletsMinCount { get; set; }

    /// <summary>
    /// Maximum number of pallets that can appear on the map.
    /// </summary>
    public int PalletsMaxCount { get; set; }

    /// <summary>
    /// Optional downloadable content (DLC) associated with the map.
    /// </summary>
    public string? DLC { get; set; }

    /// <summary>
    /// Optional thumbnail image representing the map.
    /// </summary>
    public string? Thumbnail { get; set; }
}
