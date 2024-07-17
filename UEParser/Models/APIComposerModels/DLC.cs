using Newtonsoft.Json.Linq;

namespace UEParser.Models;

public class DLC
{
    public required string? Name { get; set; }
    public required string? HeaderImage { get; set; }
    public required string BannerImage { get; set; }
    public required string? DetailedDescription { get; set; }
    public required string? Description { get; set; }
    public required string SteamId { get; set; }
    public required string EpicId { get; set; }
    public required string DMMId { get; set; }
    public required string PS4Id { get; set; }
    public required string Xbox1Id { get; set; }
    public required string XboxSeriesXId { get; set; }
    public required string SwitchId { get; set; }
    public required string WindowsStoreId { get; set; }
    public required string PS5Id { get; set; }
    public required string StadiaId { get; set; }
    public required string? ReleaseDate { get; set; }
    public required bool AllowsCrossProg { get; set; }
    public required JArray? Screenshots { get; set; }
    public int SortOrder { get; set; }
}