using System.Text.Json;
using System.Text.Json.Nodes;
using A2Ui.Core.Messages;

namespace A2Ui.Core;

/// <summary>
/// Per-surface data model store.
/// Resolves BoundValue JSON Pointer paths against the stored state.
/// Applies RFC 6902-style patch operations from UpdateDataModel messages.
/// </summary>
public sealed class DataModel
{
    private JsonObject _root = new();

    /// <summary>Replace the entire data model.</summary>
    public void SetSnapshot(JsonElement snapshot)
    {
        _root = JsonObject.Create(snapshot) ?? new JsonObject();
    }

    /// <summary>Apply a single UpdateDataModel message (JSON Pointer set).</summary>
    public void Apply(UpdateDataModel update)
    {
        // Simple JSON Pointer set — "/reservation/date" → ["reservation"]["date"]
        var segments = update.Path.TrimStart('/').Split('/');
        JsonObject current = _root;

        for (int i = 0; i < segments.Length - 1; i++)
        {
            string seg = segments[i];
            if (!current.ContainsKey(seg) || current[seg] is not JsonObject child)
            {
                child = new JsonObject();
                current[seg] = child;
            }
            current = child;
        }

        current[segments[^1]] = JsonNode.Parse(update.Value.GetRawText());
    }

    /// <summary>Apply v0.8 DataModelUpdate (key-value map).</summary>
    public void Apply(DataModelUpdate update)
    {
        foreach (var content in update.Contents)
            ApplyContent(_root, content);
    }

    /// <summary>Resolve a BoundValue path. Returns null if not found.</summary>
    public string? Resolve(BoundOrLiteral? bound)
    {
        if (bound is null) return null;
        if (!bound.IsBound) return bound.Literal;

        var segments = bound.Path!.TrimStart('/').Split('/');
        JsonNode? node = _root;
        foreach (var seg in segments)
        {
            if (node is JsonObject obj && obj.ContainsKey(seg))
                node = obj[seg];
            else
                return null;
        }
        return node?.GetValue<string>();
    }

    private static void ApplyContent(JsonObject target, DataModelContent content)
    {
        if (content.ValueString is not null) { target[content.Key] = content.ValueString; return; }
        if (content.ValueInt    is not null) { target[content.Key] = content.ValueInt;    return; }
        if (content.ValueBool   is not null) { target[content.Key] = content.ValueBool;   return; }
        if (content.ValueMap    is not null)
        {
            var child = new JsonObject();
            foreach (var c in content.ValueMap) ApplyContent(child, c);
            target[content.Key] = child;
        }
    }
}