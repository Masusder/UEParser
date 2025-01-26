using Newtonsoft.Json.Linq;

namespace UEParser.Models;

public class DLC
{
    /// <summary>
    /// The name of the downloadable content (DLC).
    /// </summary>
    public required string? Name { get; set; }

    /// <summary>
    /// The URL (from Steam) to the header image associated with the DLC.
    /// </summary>
    public required string? HeaderImage { get; set; }

    /// <summary>
    /// The path (from packaged assets) to the banner image used for the DLC.
    /// </summary>
    public required string BannerImage { get; set; }

    /// <summary>
    /// A detailed description providing in-depth information about the DLC.
    /// </summary>
    public required string? DetailedDescription { get; set; }

    /// <summary>
    /// A brief description summarizing the DLC.
    /// </summary>
    public required string? Description { get; set; }

    /// <summary>
    /// The unique identifier for the DLC on the Steam platform.
    /// </summary>
    public required string SteamId { get; set; }

    /// <summary>
    /// The unique identifier for the DLC on the Epic Games platform.
    /// </summary>
    public required string EpicId { get; set; }

    /// <summary>
    /// The unique identifier for the DLC on the PlayStation 4 platform.
    /// </summary>
    public required string PS4Id { get; set; }

    /// <summary>
    /// The unique identifier for the DLC on the Xbox One, Xbox Series X platforms and for Game Development Kit.
    /// </summary>
    public required string XB1_XSX_GDK { get; set; }

    /// <summary>
    /// The unique identifier for the DLC on the Nintendo Switch platform.
    /// </summary>
    public required string SwitchId { get; set; }

    /// <summary>
    /// The unique identifier for the DLC on the Windows Store platform.
    /// </summary>
    public required string WindowsStoreId { get; set; }

    /// <summary>
    /// The unique identifier for the DLC on the PlayStation 5 platform.
    /// </summary>
    public required string PS5Id { get; set; }

    /// <summary>
    /// The unique identifier for the DLC on the Stadia platform.
    /// </summary>
    public required string StadiaId { get; set; }

    /// <summary>
    /// The release date of the DLC, if available.
    /// </summary>
    public required string? ReleaseDate { get; set; }

    /// <summary>
    /// Indicates whether the DLC allows cross-progression between different platforms.
    /// </summary>
    public required bool AllowsCrossProg { get; set; }

    /// <summary>
    /// A collection of screenshots related to the DLC.
    /// </summary>
    public required JArray? Screenshots { get; set; }

    /// <summary>
    /// An integer value used to determine the display order of the DLC in lists.
    /// </summary>
    public int SortOrder { get; set; }
}