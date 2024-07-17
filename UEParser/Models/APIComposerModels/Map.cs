namespace UEParser.Models;

public class Map
{
    public required string Realm { get; set; }
    public string? MapId { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public double HookMinDistance { get; set; }
    public int HookMinCount { get; set; }
    public int HookMaxCount { get; set; }
    public double PalletsMinDistance { get; set; }
    public int PalletsMinCount { get; set; }
    public int PalletsMaxCount { get; set; }
    public string? DLC { get; set; }
    public string? Thumbnail { get; set; }
}