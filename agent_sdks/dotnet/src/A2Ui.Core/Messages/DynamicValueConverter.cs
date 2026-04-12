using System.Text.Json;
using System.Text.Json.Serialization;

namespace A2Ui.Core.Messages;

internal sealed class DynamicValueConverter : JsonConverter<DynamicValue>
{
    public override DynamicValue? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String => new DynamicValue.StringValue(reader.GetString()!),
            JsonTokenType.Number => new DynamicValue.NumberValue(reader.GetDouble()),
            JsonTokenType.True => new DynamicValue.BoolValue(true),
            JsonTokenType.False => new DynamicValue.BoolValue(false),
            JsonTokenType.StartArray => new DynamicValue.ArrayValue(JsonElement.ParseValue(ref reader)),
            JsonTokenType.StartObject => ReadObject(ref reader),
            _ => null,
        };
    }

    private static DynamicValue? ReadObject(ref Utf8JsonReader reader)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (root.TryGetProperty("path", out var pathEl))
            return new DynamicValue.PathValue(pathEl.GetString()!);

        if (root.TryGetProperty("call", out _))
        {
            var fc = root.Deserialize<FunctionCallValue>();
            return new DynamicValue.FunctionValue(fc!);
        }

        return null;
    }

    public override void Write(Utf8JsonWriter writer, DynamicValue value, JsonSerializerOptions options)
    {
        value.Switch(
            onString: s => writer.WriteStringValue(s.Value),
            onNumber: n => writer.WriteNumberValue(n.Value),
            onBool: b => writer.WriteBooleanValue(b.Value),
            onArray: a => a.Value.WriteTo(writer),
            onPath: p =>
            {
                writer.WriteStartObject();
                writer.WriteString("path", p.DataPath);
                writer.WriteEndObject();
            },
            onFunction: f => JsonSerializer.Serialize(writer, f.Call, options)
        );
    }
}
