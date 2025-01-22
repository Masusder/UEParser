using Newtonsoft.Json;
using UEParser.Models.Shared;

namespace UEParser.Models;

public class Character
{
    /// <summary>
    /// Character name
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Character role
    /// Killer or Survivor
    /// </summary>
    [JsonConverter(typeof(Role.RoleJsonConverter))]
    public required Role Role { get; set; }

    /// <summary>
    /// Character gender
    /// </summary>
    public required string Gender { get; set; }

    /// <summary>
    /// Associated power to the character
    /// </summary>
    public required string ParentItem { get; set; }

    /// <summary>
    /// Default items for the character.
    /// </summary>
    public required string[] DefaultItems { get; set; }

    /// <summary>
    /// Associated DLC to the character
    /// </summary>
    public required string DLC { get; set; }

    /// <summary>
    /// Difficulty of the character
    /// </summary>
    public required string Difficulty { get; set; }

    /// <summary>
    /// Bakc story of the character
    /// </summary>
    public required string BackStory { get; set; }

    /// <summary>
    /// Biography of the cahracter
    /// </summary>
    public required string Biography { get; set; }

    /// <summary>
    /// Path to portrait of the character
    /// </summary>
    public required string IconFilePath { get; set; }

    /// <summary>
    /// Path to background of the character
    /// </summary>
    public required string BackgroundImagePath { get; set; }

    /// <summary>
    /// Customization categories overriden for the character.
    /// </summary>
    public required string[] CustomizationCategories { get; set; }

    /// <summary>
    /// List of hints associated with the character.
    /// </summary>
    public required Hint[] Hints { get; set; }

    /// <summary>
    /// Character Id (it should be noted that it's different from character index!)
    /// </summary>
    public required string Id { get; set; }
}

public class Hint
{
    /// <summary>
    /// Role associated with the hint (e.g., Survivor, Killer).
    /// </summary>
    [JsonConverter(typeof(Role.RoleJsonConverter))]
    public required Role Role { get; set; }

    /// <summary>
    /// Title of the hint.
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// Detailed description of the hint.
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// File path to the icon representing the hint.
    /// </summary>
    public required string IconPath { get; set; } 
}