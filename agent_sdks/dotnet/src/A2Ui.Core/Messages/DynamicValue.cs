using System.Text.Json;
using System.Text.Json.Serialization;

namespace A2Ui.Core.Messages;

/// <summary>
/// Union type representing a value that can be a literal, a data binding path,
/// or a function call. Maps to the A2UI v0.9 DynamicValue/DynamicString/DynamicNumber/
/// DynamicBoolean/DynamicStringList types from common_types.json.
///
/// Wire formats:
///   "hello"                          → StringLiteral
///   42.5                             → NumberLiteral
///   true                             → BoolLiteral
///   ["a", "b"]                       → ArrayLiteral
///   {"path": "/reservation/date"}    → DataBinding (Path)
///   {"call": "formatDate", ...}      → FunctionCall
/// </summary>
[JsonConverter(typeof(DynamicValueConverter))]
public sealed record DynamicValue
{
    public string? StringLiteral { get; init; }
    public double? NumberLiteral { get; init; }
    public bool? BoolLiteral { get; init; }
    public JsonElement? ArrayLiteral { get; init; }
    public string? Path { get; init; }
    public FunctionCallValue? FunctionCall { get; init; }

    public bool IsBound => Path is not null;
    public bool IsFunction => FunctionCall is not null;
    public bool IsLiteral => !IsBound && !IsFunction;

    public static DynamicValue FromString(string value) => new() { StringLiteral = value };

    public static DynamicValue FromPath(string path) => new() { Path = path };

    public static DynamicValue FromNumber(double value) => new() { NumberLiteral = value };

    public static DynamicValue FromBool(bool value) => new() { BoolLiteral = value };
}

/// <summary>
/// Invokes a named function on the client.
/// Maps to common_types.json#/$defs/FunctionCall.
/// </summary>
public sealed record FunctionCallValue
{
    [JsonPropertyName("call")]
    public required string Call { get; init; }

    [JsonPropertyName("args")]
    public Dictionary<string, JsonElement>? Args { get; init; }

    [JsonPropertyName("returnType")]
    public string? ReturnType { get; init; }
}

internal sealed class DynamicValueConverter : JsonConverter<DynamicValue>
{
    public override DynamicValue? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String => DynamicValue.FromString(reader.GetString()!),
            JsonTokenType.Number => DynamicValue.FromNumber(reader.GetDouble()),
            JsonTokenType.True => DynamicValue.FromBool(true),
            JsonTokenType.False => DynamicValue.FromBool(false),
            JsonTokenType.StartArray => new DynamicValue { ArrayLiteral = JsonElement.ParseValue(ref reader) },
            JsonTokenType.StartObject => ReadObject(ref reader),
            _ => null,
        };
    }

    private static DynamicValue? ReadObject(ref Utf8JsonReader reader)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (root.TryGetProperty("path", out var pathEl))
            return DynamicValue.FromPath(pathEl.GetString()!);

        if (root.TryGetProperty("call", out _))
        {
            var fc = root.Deserialize<FunctionCallValue>();
            return new DynamicValue { FunctionCall = fc };
        }

        return null;
    }

    public override void Write(Utf8JsonWriter writer, DynamicValue value, JsonSerializerOptions options)
    {
        if (value.Path is not null)
        {
            writer.WriteStartObject();
            writer.WriteString("path", value.Path);
            writer.WriteEndObject();
        }
        else if (value.FunctionCall is not null)
        {
            JsonSerializer.Serialize(writer, value.FunctionCall, options);
        }
        else if (value.NumberLiteral is not null)
        {
            writer.WriteNumberValue(value.NumberLiteral.Value);
        }
        else if (value.BoolLiteral is not null)
        {
            writer.WriteBooleanValue(value.BoolLiteral.Value);
        }
        else if (value.ArrayLiteral is not null)
        {
            value.ArrayLiteral.Value.WriteTo(writer);
        }
        else
        {
            writer.WriteStringValue(value.StringLiteral);
        }
    }
}
