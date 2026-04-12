using System.Text.Json;
using System.Text.Json.Serialization;

namespace A2Ui.Core.Components;

internal sealed class ChildListConverter : JsonConverter<ChildList>
{
    public override ChildList? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.StartArray)
        {
            var ids = new List<string>();
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                if (reader.TokenType == JsonTokenType.String)
                    ids.Add(reader.GetString()!);
            }
            return ChildList.FromIds(ids.ToArray());
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;
            if (root.TryGetProperty("componentId", out var compIdEl) && root.TryGetProperty("path", out var pathEl))
            {
                return ChildList.FromTemplate(compIdEl.GetString()!, pathEl.GetString()!);
            }
        }

        return null;
    }

    public override void Write(Utf8JsonWriter writer, ChildList value, JsonSerializerOptions options)
    {
        if (value.Template is not null)
        {
            JsonSerializer.Serialize(writer, value.Template, options);
        }
        else if (value.Ids is not null)
        {
            writer.WriteStartArray();
            foreach (var id in value.Ids)
                writer.WriteStringValue(id);
            writer.WriteEndArray();
        }
        else
        {
            writer.WriteStartArray();
            writer.WriteEndArray();
        }
    }
}
