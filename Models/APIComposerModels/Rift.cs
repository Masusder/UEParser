using System;
using System.Collections.Generic;

namespace UEParser.Models;

public class Rift
{
    // <summary>
    // Rift name
    // </summary>
    public required string Name { get; set; }

    // <summary>
    // Number of required Tier Fragments to reach a tier
    // </summary>
    public int Requirement { get; set; }

    // <summary>
    // End date of the Rift in ISO 8601 format
    // </summary>
    public DateTime EndDate { get; set; }

    // <summary>
    // Start date of the Rift in ISO 8601 format
    // </summary>
    public DateTime StartDate { get; set; }

    // <summary>
    // Rift tier data
    // </summary>
    public required List<TierInfo> TierInfo { get; set; }
}

public class TierInfo
{
    // <summary>
    // Free Rift track
    // </summary>
    public required List<TierInfoItem> Free { get; set; }

    // <summary>
    // Premuim Rift track
    // </summary>
    public required List<TierInfoItem> Premium { get; set; }

    // <summary>
    // Indicates whether we're in normal or bonus track
    // 0 = Normal Track
    // 1 = Bonus Track
    // </summary>
    public int TierGroup { get; set; }

    // <summary>
    // Rift tier Id
    // </summary>
    public int TierId { get; set; }
}

public class TierInfoItem
{
    // <summary>
    // Amount of items
    // </summary>
    public int Amount { get; set; }

    // <summary>
    // Id of the item
    // </summary>
    public required string Id { get; set; }

    // <summary>
    // Type of the item
    // </summary>
    public required string Type { get; set; }
}
