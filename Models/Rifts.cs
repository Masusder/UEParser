using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UEParser.Models;

public class Rift
{
    public required string Name { get; set; }
    public int Requirement { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime StartDate { get; set; }
    public required List<TierInfo> TierInfo { get; set; }
}

public class TierInfo
{
    public required List<TierInfoItem> Free { get; set; }
    public required List<TierInfoItem> Premium { get; set; }
    public int TierGroup { get; set; }
    public int TierId { get; set; }
}

public class TierInfoItem
{
    public int Amount { get; set; }
    public required string Id { get; set; }
    public required string Type { get; set; }
}
