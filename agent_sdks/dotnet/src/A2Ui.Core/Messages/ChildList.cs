using System.Text.Json;
using System.Text.Json.Serialization;

namespace A2Ui.Core.Messages;

/// <summary>
/// A2UI v0.9 ChildList — either a static array of component IDs
/// or a template for dynamic list generation from the data model.
///
/// Wire formats:
///   ["id1", "id2", "id3"]                    → static Ids
///   {"componentId": "tmpl", "path": "/items"} → template
/// </summary>
[JsonConverter(typeof(ChildListConverter))]
public sealed record ChildList
{
    /// <summary>Static list of child component IDs.</summary>
    public string[]? Ids { get; init; }

    /// <summary>Template for dynamic children from a data model array.</summary>
    public ChildTemplate? Template { get; init; }

    public bool IsTemplate => Template is not null;

    public static ChildList FromIds(params string[] ids) => new() { Ids = ids };

    public static ChildList FromTemplate(string componentId, string path) =>
        new()
        {
            Template = new ChildTemplate { ComponentId = componentId, Path = path },
        };
}

/// <summary>
/// Template for generating dynamic children by iterating over a data model array.
/// </summary>
public sealed record ChildTemplate
{
    [JsonPropertyName("componentId")]
    public required string ComponentId { get; init; }

    [JsonPropertyName("path")]
    public required string Path { get; init; }
}

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
