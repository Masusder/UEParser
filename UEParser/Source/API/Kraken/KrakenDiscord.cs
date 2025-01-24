using System.Collections.Generic;
using Newtonsoft.Json;
using UEParser.Parser;
using UEParser.Utils;
using UEParser.ViewModels;

namespace UEParser.Network.Kraken;

public class KrakenDiscord
{
    public static void StructurePlayerInventory()
    {
        LogsWindowViewModel.Instance.AddLog("Preparing and saving Player's Inventory for Discord..", Logger.LogTags.Info);

        var inventoryData = FileUtils.ReadApiResponseFromFile<PlayerInventoryDataRoot>("inventory.json");
        var charactersData = FileUtils.ReadApiResponseFromFile<DbdCharacterDataRoot>("charactersData.json");
        var playerNameData = FileUtils.ReadApiResponseFromFile<PlayerName>("playername.json");

        if (inventoryData == null || charactersData == null || playerNameData == null)
        {
            LogsWindowViewModel.Instance.AddLog("Failed to prepare Discord data..", Logger.LogTags.Error);
            return;
        }

        // Map character data to the new schema structure to match to that of GDPR
        var wrappedCharacterData = MapCharacterDataToSchema(charactersData.List);

        var ueParser = new UEParserDiscord
        {
            PlayerInventory = inventoryData.Data.Inventory,
            PlayerName = playerNameData,
            SplinteredState = new SplinteredState
            {
                DbdCharacterData = wrappedCharacterData
            }
        };

        var serializedData = new Dictionary<string, UEParserDiscord>
        {
            { "UEParser", ueParser }
        };

        string serializedDataJson = JsonConvert.SerializeObject(serializedData, Formatting.Indented);

        FileWriter.SaveApiResponseToFile(serializedDataJson, "playerInventory_Discord.json");
    }

    private static List<DbdCharacterItemSchema> MapCharacterDataToSchema(List<DbdCharacterItem> characterData)
    {
        List<DbdCharacterItemSchema> schemaData = [];

        foreach (var character in characterData)
        {
            var characterItemSchema = new DbdCharacterItemSchema
            {
                ObjectId = character.CharacterName,
                Data = new DbdCharacterItem
                {
                    CharacterName = character.CharacterName,
                    LegacyPrestigeLevel = character.LegacyPrestigeLevel,
                    CharacterItems = character.CharacterItems,
                    BloodWebLevel = character.BloodWebLevel,
                    BloodWebData = character.BloodWebData,
                    PrestigeLevel = character.PrestigeLevel
                },
                Version = 1,
                SchemaVersion = 1
            };

            schemaData.Add(characterItemSchema);
        }

        return schemaData;
    }
}

public class UEParserDiscord
{
    [JsonProperty("playerInventory")]
    public required List<InventoryItem> PlayerInventory { get; set; }
    [JsonProperty("playerName")]
    public required PlayerName PlayerName { get; set; }
    [JsonProperty("splinteredState")]
    public required SplinteredState SplinteredState { get; set; }
}

public class PlayerInventoryDataRoot
{
    [JsonProperty("data")]
    public required PlayerInventoryData Data { get; set; }
}

public class PlayerInventoryData
{
    [JsonProperty("playerId")]
    public required string PlayerId { get; set; }
    [JsonProperty("inventory")]
    public required List<InventoryItem> Inventory { get; set; }
}

public class InventoryItem
{
    [JsonProperty("objectId")]
    public required string ObjectId { get; set; }
    [JsonProperty("quantity")]
    public int Quantity { get; set; }
    [JsonProperty("lastUpdateAt")]
    public long LastUpdateAt { get; set; }
}

public class PlayerName
{
    [JsonProperty("userId")]
    public required string UserId { get; set; }
    [JsonProperty("playerName")]
    public required string PlayerNameValue { get; set; }
    [JsonProperty("providerPlayerNames")]
    public required Dictionary<string, string> ProviderPlayerNames { get; set; }
}

public class SplinteredState
{
    [JsonProperty("dbd_character_data")]
    public List<DbdCharacterItemSchema>? DbdCharacterData { get; set; }
}

public class DbdCharacterDataRoot
{
    [JsonProperty("list")]
    public required List<DbdCharacterItem> List { get; set; }
}

public class DbdCharacterItem
{
    [JsonProperty("characterName")]
    public required string CharacterName { get; set; }
    [JsonProperty("legacyPrestigeLevel")]
    public int LegacyPrestigeLevel { get; set; }
    [JsonProperty("bloodWebLevel")]
    public int BloodWebLevel { get; set; }
    [JsonProperty("characterItems")]
    public required List<CharacterItem> CharacterItems { get; set; }
    [JsonProperty("bloodWebData")]
    public BloodWebData? BloodWebData { get; set; }
    [JsonProperty("prestigeLevel")]
    public int PrestigeLevel { get; set; }
}

public class DbdCharacterItemSchema
{
    [JsonProperty("objectId")]
    public required string ObjectId { get; set; }
    [JsonProperty("data")]
    public required DbdCharacterItem Data { get; set; }
    [JsonProperty("version")]
    public int Version { get; set; }
    [JsonProperty("schemaVersion")]
    public int SchemaVersion { get; set; }
}

public class CharacterItem
{
    [JsonProperty("itemId")]
    public required string ItemId { get; set; }
    [JsonProperty("quantity")]
    public int Quantity { get; set; }
}

public class BloodWebData
{
    [JsonProperty("paths")]
    public List<string>? Paths { get; set; }
    [JsonProperty("ringData")]
    public List<RingData>? RingData { get; set; }
}

public class RingData
{
    [JsonProperty("nodeData")]
    public List<NodeData>? NodeData { get; set; }
}

public class NodeData
{
    [JsonProperty("nodeId")]
    public string? NodeId { get; set; }
    [JsonProperty("contentId")]
    public string? ContentId { get; set; }
    [JsonProperty("state")]
    public string? State { get; set; }
}