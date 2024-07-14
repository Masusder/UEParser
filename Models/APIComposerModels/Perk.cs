using System.Collections.Generic;

namespace UEParser.Models;

public class Perk
{
    /// <summary>
    /// Gets or sets the character associated with the perk.
    /// </summary>
    public int Character { get; set; }

    /// <summary>
    /// Gets or sets the name of the perk.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the description of the perk.
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// Gets or sets the file path for the icon associated with the perk.
    /// </summary>
    public required string IconFilePathList { get; set; }

    /// <summary>
    /// Gets or sets the categories associated with the perk.
    /// </summary>
    public required string? Categories { get; set; }

    /// <summary>
    /// Gets or sets the tag associated with the perk.
    /// </summary>
    public required string Tag { get; set; }

    /// <summary>
    /// Gets or sets the role associated with the perk.
    /// </summary>
    public required string Role { get; set; }

    /// <summary>
    /// Gets or sets the teachable level of the perk.
    /// </summary>
    public int TeachableLevel { get; set; }

    /// <summary>
    /// Gets or sets the tunables associated with the perk.
    /// </summary>
    public required List<List<string>> Tunables { get; set; }
}