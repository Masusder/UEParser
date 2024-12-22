using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace UEParser.Models.Shared;

public class Role
{
    private static readonly HashSet<string> _validRoles =
    [
        "None", "Killer", "Survivor"
    ];

    private readonly string _value;

    public string Value => _value;

    public Role(string value)
    {
        if (!_validRoles.Contains(value))
        {
            throw new ArgumentException("Invalid role. Allowed roles are 'None', 'Killer', or 'Survivor'.");
        }
        _value = value;
    }

    public override string ToString() => _value;

    public class RoleJsonConverter : JsonConverter<Role>
    {
        public override Role ReadJson(JsonReader reader, Type objectType, Role? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var roleValue = reader.Value?.ToString();
            if (roleValue == null || !_validRoles.Contains(roleValue))
            {
                throw new JsonSerializationException($"Invalid role value: {roleValue}");
            }
            return new Role(roleValue);
        }

        public override void WriteJson(JsonWriter writer, Role? value, JsonSerializer serializer)
        {
            writer.WriteValue(value?.ToString());
        }
    }
}