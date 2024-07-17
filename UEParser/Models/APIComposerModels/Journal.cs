using System.Collections.Generic;

namespace UEParser.Models;

public class Journal
{
    public required string TomeName { get; set; }
    public required List<Vignette> Vignettes { get; set; }
}

public class Vignette
{
    public required string VignetteId { get; set; }
    public required string Name { get; set; }
    public required string SubTitle { get; set; }
    public required List<Entry> Entries { get; set; }
}

public class Entry
{
    public required string Title { get; set; }
    public required string Text { get; set; }
    public required Audio Audio { get; set; }
    public required RewardImage RewardImage { get; set; }
}

public class Audio
{
    public required string Path { get; set; }
    public bool HasAudio { get; set; }
}

public class RewardImage
{
    public required string AssetPathName { get; set; }
    public required string SubPathString { get; set; }
}