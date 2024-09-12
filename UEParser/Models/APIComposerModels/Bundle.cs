using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace UEParser.Models;

/// <summary>
/// Represents a bundle of items that can be purchased through in-game shop.
/// (everything in Bundle is server-sided)
/// </summary>
public class Bundle
{
    /// <summary>
    /// Unique identifier for the bundle.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Title of the bundle.
    /// </summary>
    public required string SpecialPackTitle { get; set; }

    /// <summary>
    /// Path to the image representing the bundle.
    /// Can be overridden with <see cref="ImageComposition"/>.
    /// </summary>
    public required string? ImagePath { get; set; }

    /// <summary>
    /// Date when the bundle becomes available.
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Date when the bundle is no longer available.
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Order in which the bundle should be sorted.
    /// </summary>
    public required int SortOrder { get; set; }

    /// <summary>
    /// Minimum number of unowned items required for purchase.
    /// </summary>
    public required int MinNumberOfUnownedForPurchase { get; set; }

    /// <summary>
    /// Indicates whether bundle is available for purchase.
    /// </summary>
    public required bool Purchasable { get; set; }

    /// <summary>
    /// Indicates if the bundle is related to a chapter.
    /// (this is a custom property and does not exist in-game!)
    /// </summary>
    public required bool IsChapterBundle { get; set; }

    /// <summary>
    /// Indicates if the bundle is licensed.
    /// (this is a custom property and does not exist in-game!)
    /// </summary>
    public required bool IsLicensedBundle { get; set; }

    /// <summary>
    /// Identifier for the associated downloadable content (DLC).
    /// </summary>
    public required string? DlcId { get; set; }

    /// <summary>
    /// List of full prices for the bundle in different currencies.
    /// Such as Shards, Cells etc.
    /// </summary>
    public required List<FullPrice> FullPrice { get; set; }

    /// <summary>
    /// If present, image representing the bundle is auto-generated using <see cref="ConsumptionRewards"/> items.
    /// </summary>
    public ImageComposition? ImageComposition { get; set; }

    /// <summary>
    /// Discount applied to the bundle.
    /// </summary>
    public required float Discount { get; set; }

    /// <summary>
    /// List of rewards for consuming the bundle.
    /// </summary>
    public required List<ConsumptionRewards> ConsumptionRewards { get; set; }

    /// <summary>
    /// Indicates if the bundle is consumable.
    /// </summary>
    public required bool Consumable { get; set; }

    /// <summary>
    /// Type of the bundle.
    /// </summary>
    public required string Type { get; set; }

    /// <summary>
    /// Segmentation tags associated with the bundle.
    /// Tags are checked on the server-side to filter if for example user shouldn't be allowed to see that bundle.
    /// These tags are not displayed to the user.
    /// </summary>
    public JArray? SegmentationTags { get; set; }
}

public class ImageComposition
{
    /// <summary>
    /// Maximum number of items in the composed image.
    /// </summary>
    public required int MaxItemCount { get; set; }

    /// <summary>
    /// Indicates whether we should override default image path.
    /// </summary>
    public required bool OverrideDefaults { get; set; }

    /// <summary>
    /// Type of the composed image.
    /// </summary>
    public required string Type { get; set; }
}

/// <summary>
/// Represents game-specific data associated with a reward.
/// </summary>
public class GameSpecificData
{
    /// <summary>
    /// Indicates if ownership should be ignored.
    /// </summary>
    public required bool IgnoreOwnership { get; set; }

    /// <summary>
    /// Indicates if the item should be included in ownership calculations.
    /// </summary>
    public required bool IncludeInOwnership { get; set; }

    /// <summary>
    /// Indicates if the item should be included in pricing calculations.
    /// </summary>
    public required bool IncludeInPricing { get; set; }

    /// <summary>
    /// Type of the game-specific data.
    /// </summary>
    public required string Type { get; set; }
}

/// <summary>
/// Represents the full price details of a bundle in a specific currency.
/// </summary>
public class FullPrice
{
    /// <summary>
    /// Identifier for the currency.
    /// </summary>
    public required string CurrencyId { get; set; }

    /// <summary>
    /// Price of the bundle in the specified currency.
    /// </summary>
    public required int Price { get; set; }
}

/// <summary>
/// Represents rewards given upon consumption of a bundle.
/// </summary>
public class ConsumptionRewards
{
    /// <summary>
    /// Amount of the reward.
    /// </summary>
    public required int Amount { get; set; }

    /// <summary>
    /// Identifier for the reward.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Type of the reward.
    /// </summary>
    public required string Type { get; set; }

    /// <summary>
    /// Game-specific data related to the reward.
    /// </summary>
    public required GameSpecificData GameSpecificData { get; set; }
}