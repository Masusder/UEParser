using System;
using Newtonsoft.Json.Linq;

namespace UEParser.Models;

public class SpecialEvent
{
    /// <summary>
    /// Name of the event.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Description of the event.
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// Items available for purchase during event.
    /// </summary>
    public required JArray StoreItemIds { get; set; }

    /// <summary>
    /// End date of the event in ISO 8601 format.
    /// (server-sided)
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// Start date of the event in ISO 8601 format.
    /// (server-sided)
    /// </summary>
    public DateTime StartTime { get; set; }
}