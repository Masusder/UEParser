using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UEParser.Models;

public class Bundle
{
    public required string Id { get; set; }
    public required string SpecialPackTitle { get; set; }
    public required string ImagePath { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public required int SortOrder { get; set; }
    public required int MinNumberOfUnownedForPurchase { get; set; }
    public required bool IsChapterBundle { get; set; }
    public required bool IsLicensedBundle { get; set; }
    public required string? DlcId { get; set; }
    public required List<FullPrice> FullPrice { get; set; }
    public required float Discount { get; set; }
    public required List<ConsumptionRewards> ConsumptionRewards { get; set; }
    public required bool Consumable { get; set; }
    public required string Type { get; set; }
    public JArray? SegmentationTags { get; set; }
    public bool Purchasable { get; set; }
}

public class GameSpecificData
{
    public required bool IgnoreOwnership { get; set; }
    public required bool IncludeInOwnership { get; set; }
    public required bool IncludeInPricing { get; set; }
    public required string Type { get; set; }
}

public class FullPrice
{
    public required string CurrencyId { get; set; }
    public required int Price { get; set; }
}

public class ConsumptionRewards
{
    public required int Amount { get; set; }
    public required string Id { get; set; }
    public required string Type { get; set; }
    public required GameSpecificData GameSpecificData { get; set; }
}