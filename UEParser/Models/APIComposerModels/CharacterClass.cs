using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UEParser.Models;

public class CharacterClass
{
    /// <summary>
    /// List of skills given character class gives.
    /// </summary>
    public required JArray Skills { get; set; }

    /// <summary>
    /// The name of the character class.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// A brief description of the character class.
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// Path to the character class icon.
    /// </summary>
    public required string IconPath { get; set; }

    /// <summary>
    /// Character class role.
    /// Killer or Survivor
    /// </summary>
    public required string Role { get; set; }

    // <summary>
    // Indicates whether class can be in inventory.
    // </summary>
    //public required bool Inventory { get; set; }

    // <summary>
    // Indicates whether character class can appear in the bloodweb.
    // </summary>
    //public required bool Bloodweb { get; set; }

    // <summary>
    // Indicates whether character class can be in a chest.
    // </summary>
    //public required bool Chest { get; set; }
}