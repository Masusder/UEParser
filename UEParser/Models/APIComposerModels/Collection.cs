using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UEParser.Models;

/// <summary>
/// Everything inside Collection is fully server-sided.
/// </summary>
public class Collection
{
    /// <summary>
    /// Unique identifier for the collection.
    /// </summary>
    public required string CollectionId { get; set; }

    /// <summary>
    /// Array of additional images associated with the collection.
    /// </summary>
    public required JArray AdditionalImages { get; set; }

    /// <summary>
    /// Title of the collection.
    /// </summary>
    public required string CollectionTitle { get; set; }

    /// <summary>
    /// Subtitle of the collection.
    /// </summary>
    public required string CollectionSubtitle { get; set; }

    /// <summary>
    /// Image representing the collection.
    /// </summary>
    public required string HeroImage { get; set; }

    /// <summary>
    /// Hero video associated with the collection.
    /// </summary>
    public required string HeroVideo { get; set; }

    /// <summary>
    /// Version in which collection was introduced.
    /// </summary>
    public required string InclusionVersion { get; set; }

    /// <summary>
    /// Date when the collection was last updated.
    /// </summary>
    public required DateTime UpdatedDate { get; set; }

    /// <summary>
    /// Start date for the availability of the collection.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public required DateTime? LimitedAvailabilityStartDate { get; set; }

    /// <summary>
    /// Array of items included in the collection.
    /// </summary>
    public required JArray Items { get; set; }

    /// <summary>
    /// Order in which the collection is sorted.
    /// </summary>
    public required string SortOrder { get; set; }

    /// <summary>
    /// Indicates if the collection is visible before the start date.
    /// (start date = 'LimitedAvailabilityStartDate' property)
    /// </summary>
    public required bool VisibleBeforeStartDate { get; set; }
}
