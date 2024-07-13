using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UEParser.Models;

public class Tome
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public List<Level>? Levels { get; set; }
    public string? RiftID { get; set; }
    public bool NewTomePopup { get; set; }
}

public class Level
{
    public Dictionary<string, Node>? Nodes { get; set; }
    public DateTime StartDate { get; set; }
    public JArray? StartNodes { get; set; }
    public JArray? EndNodes { get; set; }
    public string? EndNodeReward { get; set; }
}

public class Node
{
    public string? QuestID { get; set; }
    public Dictionary<string, double>? Coordinates { get; set; }
    public JArray? Neighbors { get; set; }
    public string? NodeType { get; set; }
    public JArray? Journals { get; set; }
    // public bool IsCommunityChallenge { get; set; }
    // public string? CommunityProgression { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public JArray? DescriptionParams { get; set; }
    public string? RulesDescription { get; set; }
    public string? PlayerRole { get; set; }
    public string? IconPath { get; set; }
    public JArray? Reward { get; set; }
}

public class NodeData
{
    public string? NodeName { get; set; }
    public string? IconPath { get; set; }
    public string? ObjectiveDescription { get; set; }
    public string? PlayerRole { get; set; }
    public string? RulesDescription { get; set; }
}
