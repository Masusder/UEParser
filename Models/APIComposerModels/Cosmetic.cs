using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace UEParser.Models;

public class Outfit
{
    // <summary>
    // Cosmetic Id
    // </summary>
    public required string CosmeticId { get; set; }

    // <summary>
    // Cosmetic name
    // </summary>
    public required string CosmeticName { get; set; }

    // <summary>
    // Cosmetic description
    // </summary>
    public required string Description { get; set; }

    // <summary>
    // Icon Path (originally a list, but cosmetics always use only one icon)
    // </summary>
    public required string IconFilePathList { get; set; }

    // <summary>
    // Id of the event cosmetic is associated with
    // </summary>
    public required string? EventId { get; set; }

    // <summary>
    // Name of collection cosmetic is associated to
    // </summary>
    public required string CollectionName { get; set; }

    // <summary>
    // Inclusion version of the cosmetic
    // This property was introduced in 5.5.0 version
    // Therefore it only goes back to this version
    // Versions before 5.5.0 are under 'Legacy' name
    // </summary>
    public required string InclusionVersion { get; set; }

    // <summary>
    // Type of the cosmetic
    // </summary>
    public required string Type { get; set; }

    // <summary>
    // Associated character index
    // -1 means no character is associated
    // (both server and client sided)
    // </summary>
    public required int? Character { get; set; }

    // <summary>
    // Indicates whether cosmetic is a linked set
    // </summary>
    public bool Unbreakable { get; set; }

    // <summary>
    // Indicates whether cosmetic is available for purchase (server-sided)
    // </summary>
    public bool Purchasable { get; set; }

    // <summary>
    // Release date of the cosmetics (server-sided)
    // </summary>
    public DateTime ReleaseDate { get; set; }

    // <summary>
    // Date of when cosmetic won't show up in the shop anymore
    // If this date is past current date cosmetic disappears from the shop
    // even when purchasable state is set to true
    // (server-sided)
    // </summary>
    public DateTime? LimitedTimeEndDate { get; set; }

    // <summary>
    // Rarity of the cosmetic
    // </summary>
    public required string? Rarity { get; set; }

    // <summary>
    // Customization modifier
    // Used for changing UI, such as blood on cosmetic background
    // Ex. value 'Visceral' is used for cosmetics with custom mori
    // </summary>
    public string? Prefix { get; set; }

    // <summary>
    // If cosmetic previously appeared in the rift this value won't be null
    // (this is a custom property and does not exist in-game!)
    // (server-sided)
    // </summary>
    public string? TomeId { get; set; }

    // <summary>
    // List of cosmetic pieces outfit consist of
    // </summary>
    public required JArray OutfitItems { get; set; }

    // <summary>
    // Discount percentage (server-sided)
    // </summary>
    public double DiscountPercentage { get; set; }

    // <summary>
    // Indicates whether outfit is currently discounted
    // If set to true 'TemporaryDiscount' should be populated
    // (server-sided)
    // </summary>
    public bool IsDiscounted { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    // <summary>
    // Temporary discount data
    // (server-sided)
    // </summary>
    public List<TemporaryDiscount>? TemporaryDiscounts { get; set; }

    // <summary>
    // List of available currencies cosmetic can be bought with
    // (server-sided)
    // </summary>
    public required List<Dictionary<string, int>>? Prices { get; set; }
}

public class CustomzatiomItem
{
    // <summary>
    // Cosmetic Id
    // </summary>
    public required string CosmeticId { get; set; }

    // <summary>
    // Cosmetic name
    // </summary>
    public required string CosmeticName { get; set; }

    // <summary>
    // Cosmetic description
    // </summary>
    public required string Description { get; set; }

    // <summary>
    // Tags cosmetic can be searched with
    // It should be noted BHVR usually only adds
    // search tags for Badges/Banners/Charms
    // </summary>
    public required JArray SearchTags { get; set; }

    // <summary>
    // Icon path (originally a list, but cosmetics always use only one icon)
    // </summary>
    public required string IconFilePathList { get; set; }

    // <summary>
    // Secondary icon path, used for cosmetics such as Banners
    // </summary>
    public required string SecondaryIcon { get; set; }

    // <summary>
    // Id of the event cosmetic is associated with
    // </summary>
    public required string? EventId { get; set; }

    // <summary>
    // Path to data used to load 3D Model of the cosmetic
    // (this is a custom property and does not exist in-game!)
    // </summary>
    public required string ModelDataPath { get; set; }

    // <summary>
    // Name of collection cosmetic is associated to
    // </summary>
    public required object CollectionName { get; set; }

    // <summary>
    // Inclusion version of the cosmetic
    // This property was introduced in 5.5.0 version
    // Therefore it only goes back to this version
    // Versions before 5.5.0 are under 'Legacy' name
    // </summary>
    public required string InclusionVersion { get; set; }

    // <summary>
    // Release date of the cosmetics (server-sided)
    // </summary>
    public required DateTime ReleaseDate { get; set; }

    // <summary>
    // Date of when cosmetic won't show up in the shop anymore
    // If this date is past current date cosmetic disappears from the shop
    // even when purchasable state is set to true
    // (server-sided)
    // </summary>
    public DateTime? LimitedTimeEndDate { get; set; }

    // <summary>
    // Indicates on which role cosmetic can be used
    // It should be noted only certain cosmetic types such as Charms
    // use this value
    // If role equals to None it should be ignore
    // </summary>
    public required string Role { get; set; }

    // <summary>
    // Type of the cosmetic
    // </summary>
    public required string Type { get; set; }

    // <summary>
    // Associated character index
    // -1 means no character is associated
    // (both server and client sided)
    // </summary>
    public int Character { get; set; }

    // <summary>
    // Indicates whether cosmetic is available for purchase (server-sided)
    // </summary>
    public bool Purchasable { get; set; }

    // <summary>
    // Rarity of the cosmetic
    // </summary>
    public required string Rarity { get; set; }

    // <summary>
    // Customization modifier
    // Used for changing UI, such as blood on cosmetic background
    // Ex. value 'Visceral' is used for cosmetics with custom mori
    // </summary>
    public required string Prefix { get; set; }

    // <summary>
    // If cosmetic previously appeared in the rift this value won't be null
    // (this is a custom property and does not exist in-game!)
    // (server-sided)
    // </summary>
    public string? TomeId { get; set; }

    // <summary>
    // List of available currencies cosmetic can be bought with
    // (server-sided)
    // </summary>
    public List<Dictionary<string, int>>? Prices { get; set; }
}

// Everything inside temporary discount is fully server-sided
public class TemporaryDiscount
{
    // <summary>
    // Currency id that should be discounted
    // </summary>
    public required string CurrencyId { get; set; }

    // <summary>
    // Percentage of the discount
    // </summary>
    public double DiscountPercentage { get; set; }

    // <summary>
    // End date of the discount
    // </summary>
    public DateTime EndDate { get; set; }

    // <summary>
    // Start date of the discount
    // </summary>
    public DateTime StartDate { get; set; }
}


// Currencies should be separated from the cosmetics BUT
// due to the fact Rift treats currency bundles as cosmetics
// I add them to cosmetics to make my life simpler
// They can be easily ignored by checking type, which is "Currency"
public class Currency
{
    public required string CosmeticId { get; set; }
    public required string CosmeticName { get; set; }
    public required string? Description { get; set; }
    public required string IconFilePathList { get; set; }
    public required string Type { get; set; }
    public required string? TomeId { get; set; }
}
