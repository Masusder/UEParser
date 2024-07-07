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
    public required string Role { get; set; }

    /// <summary>
    /// Character gender
    /// </summary>
    public required string Gender { get; set; }

    /// <summary>
    /// Associated power to the character
    /// </summary>
    public required string ParentItem { get; set; }

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
    /// Character Id (it should be noted that it's different from character index!)
    /// </summary>
    public required string Id { get; set; }
}
