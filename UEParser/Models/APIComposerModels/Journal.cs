using System.Collections.Generic;

namespace UEParser.Models;

public class Journal
{
    /// <summary>
    /// Represents the name of the tome associated with this journal.
    /// </summary>
    public required string TomeName { get; set; }

    /// <summary>
    /// A collection of vignettes that make up the content of the journal.
    /// </summary>
    public required List<Vignette> Vignettes { get; set; }
}

public class Vignette
{
    /// <summary>
    /// A unique identifier for the vignette.
    /// </summary>
    public required string VignetteId { get; set; }

    /// <summary>
    /// The title of the vignette.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// A subtitle providing additional context or detail about the vignette.
    /// </summary>
    public required string SubTitle { get; set; }

    /// <summary>
    /// A list of entries that belong to this vignette, detailing its contents.
    /// </summary>
    public required List<Entry> Entries { get; set; }
}

public class Entry
{
    /// <summary>
    /// The title of the journal entry.
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// The main text or narrative content of the journal entry.
    /// </summary>
    public required string Text { get; set; }

    /// <summary>
    /// Audio content associated with the journal entry.
    /// </summary>
    public required Audio Audio { get; set; }

    /// <summary>
    /// An image representing the reward linked to this journal entry.
    /// </summary>
    public required RewardImage RewardImage { get; set; }
}

public class Audio
{
    /// <summary>
    /// The file path to the audio content, if available.
    /// </summary>
    public required string? Path { get; set; }

    /// <summary>
    /// Indicates whether audio content is present.
    /// </summary>
    public bool HasAudio { get; set; }
}

public class RewardImage
{
    /// <summary>
    /// The path to the asset representing the reward image.
    /// </summary>
    public required string? AssetPathName { get; set; }

    /// <summary>
    /// A subpath or specific identifier within the asset path.
    /// </summary>
    public required string SubPathString { get; set; }
}
