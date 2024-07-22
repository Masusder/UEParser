using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UEParser.Models;

public class Collection
{
    public required string CollectionId { get; set; }
    // public DateTime ActiveFromDate { get; set; }
    // public DateTime ActiveUntilDate { get; set; }
    public required JArray AdditionalImages { get; set; }
    public required string CollectionTitle { get; set; }
    public required string CollectionSubtitle { get; set; }
    public required string HeroImage { get; set; }
    public required string HeroVideo { get; set; }
    public required string InclusionVersion { get; set; }
    // public bool HasLimitedAvailabilityEndDate { get; set; }
    // public DateTime LimitedAvailabilityEndDate { get; set;}
    public required DateTime UpdatedDate { get; set; }
    // public bool IsFeatured { get; set; } // Server only
    // public bool IsNew { get; set; } // Server only
    // public bool IsSpecial { get; set; } // Server only
    // public bool IsEnabled { get; set; }
    public required JArray Items { get; set; }
    public required string SortOrder { get; set; }
    // public int Flags { get; set; }
}