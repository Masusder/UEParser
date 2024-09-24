using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace UEParser.Models;

/// <summary>
/// Represents a tome with a name, description, levels, rift ID, and a popup indicator.
/// </summary>
public class Tome
{
    /// <summary>
    /// The name of the tome.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// A brief description of the tome.
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// A list of levels associated with the tome.
    /// </summary>
    public required List<Level> Levels { get; set; }

    /// <summary>
    /// A unique identifier for the rift.
    /// </summary>
    public required string RiftID { get; set; }

    /// <summary>
    /// Indicates whether a new tome popup is enabled.
    /// </summary>
    public required bool NewTomePopup { get; set; }
}

/// <summary>
/// Represents a level with nodes, start date, start nodes, end nodes, and an end node reward.
/// </summary>
public class Level
{
    /// <summary>
    /// List of nodes in a level.
    /// </summary>
    public required Dictionary<string, Node> Nodes { get; set; }

    /// <summary>
    /// The start date of the level.
    /// </summary>
    public required DateTime StartDate { get; set; }

    /// <summary>
    /// A JSON array of starting nodes.
    /// </summary>
    public required JArray StartNodes { get; set; }

    /// <summary>
    /// A JSON array of ending nodes.
    /// </summary>
    public required JArray EndNodes { get; set; }

    /// <summary>
    /// A reward associated with the end node.
    /// </summary>
    public required string? EndNodeReward { get; set; }
}

/// <summary>
/// Represents a node with a quest ID, coordinates, neighbors, type, journals, and other details.
/// </summary>
public class Node
{
    /// <summary>
    /// The identifier for the quest.
    /// </summary>
    public required string QuestID { get; set; }

    /// <summary>
    /// A dictionary containing the coordinates of the node.
    /// </summary>
    public required Dictionary<string, double> Coordinates { get; set; }

    /// <summary>
    /// A JSON array of neighboring nodes.
    /// </summary>
    public required JArray Neighbors { get; set; }

    /// <summary>
    /// The type of the node.
    /// </summary>
    public required string NodeType { get; set; }

    /// <summary>
    /// A JSON array of journals associated with the node.
    /// </summary>
    public required JArray Journals { get; set; }

    /// <summary>
    /// The name of the node.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// A brief description of the node.
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// A JSON array of description parameters.
    /// </summary>
    public required JArray DescriptionParams { get; set; }

    /// <summary>
    /// The rules description, if any.
    /// </summary>
    public string? RulesDescription { get; set; }

    /// <summary>
    /// The role of the player associated with the node.
    /// </summary>
    public required string PlayerRole { get; set; }

    /// <summary>
    /// The path to the icon associated with the node.
    /// </summary>
    public required string IconPath { get; set; }

    /// <summary>
    /// A JSON array of rewards associated with the node.
    /// </summary>
    public required JArray Reward { get; set; }
}

/// <summary>
/// This is only used to populate node class.
/// </summary>
public class NodeData
{
    public required string NodeName { get; set; }
    public required string IconPath { get; set; }
    public required string ObjectiveDescription { get; set; }
    public required string PlayerRole { get; set; }
    public string? RulesDescription { get; set; }
}