using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using A2Ui.Core.Messages;

namespace A2Ui.Core;

/// <summary>
/// Per-surface data model store.
/// Resolves DynamicValue paths against the stored state.
/// Applies UpdateDataModel messages using JSON Pointer set/delete semantics.
/// </summary>
public sealed class DataModel
{
    private JsonObject _root = new();

    /// <summary>Replace the entire data model.</summary>
    public void SetSnapshot(JsonElement snapshot)
    {
        _root = JsonObject.Create(snapshot) ?? new JsonObject();
    }

    /// <summary>
    /// Apply a single UpdateDataModel message.
    /// Null or "/" path → replace entire model.
    /// Null value → delete at path.
    /// </summary>
    public void Apply(UpdateDataModel update)
    {
        // Null or root path → replace entire model or clear it
        if (string.IsNullOrEmpty(update.Path) || update.Path == "/")
        {
            if (update.Value is { } val)
                SetSnapshot(val);
            else
                _root = new JsonObject();
            return;
        }

        var segments = update.Path.TrimStart('/').Split('/');

        // Null value → delete at path
        if (update.Value is null)
        {
            DeleteAtPath(segments);
            return;
        }

        // Set value at path
        JsonObject current = _root;
        for (int i = 0; i < segments.Length - 1; i++)
        {
            string seg = segments[i];
            if (current[seg] is not JsonObject child)
            {
                child = new JsonObject();
                current[seg] = child;
            }
            current = child;
        }

        current[segments[^1]] = JsonNode.Parse(update.Value.Value.GetRawText());
    }

    /// <summary>Resolve a DynamicValue. Returns null if path not found or FunctionCall.</summary>
    public string? Resolve(DynamicValue? value)
    {
        if (value is null) return null;
        if (value.StringLiteral is not null) return value.StringLiteral;
        if (value.NumberLiteral is not null) return value.NumberLiteral.Value.ToString(CultureInfo.InvariantCulture);
        if (value.BoolLiteral is not null) return value.BoolLiteral.Value ? "true" : "false";
        if (value.Path is not null) return ResolvePathAsString(value.Path);
        // FunctionCall and ArrayLiteral: not resolvable to string at model layer
        return null;
    }

    private string? ResolvePathAsString(string path)
    {
        JsonNode? node = ResolvePath(path);
        return NodeToString(node);
    }

    private JsonNode? ResolvePath(string path)
    {
        var segments = path.TrimStart('/').Split('/');
        JsonNode? node = _root;

        foreach (var seg in segments)
        {
            if (node is JsonObject obj && obj.ContainsKey(seg))
            {
                node = obj[seg];
            }
            else if (node is JsonArray arr && int.TryParse(seg, out int idx) && idx >= 0 && idx < arr.Count)
            {
                node = arr[idx];
            }
            else
            {
                return null;
            }
        }

        return node;
    }

    private void DeleteAtPath(string[] segments)
    {
        if (segments.Length == 1)
        {
            _root.Remove(segments[0]);
            return;
        }

        JsonObject current = _root;
        for (int i = 0; i < segments.Length - 1; i++)
        {
            if (current[segments[i]] is JsonObject child)
                current = child;
            else
                return; // path doesn't exist, nothing to delete
        }
        current.Remove(segments[^1]);
    }

    private static string? NodeToString(JsonNode? node)
    {
        if (node is null) return null;

        if (node is JsonValue val)
        {
            if (val.TryGetValue<string>(out var s)) return s;
            if (val.TryGetValue<double>(out var d)) return d.ToString(CultureInfo.InvariantCulture);
            if (val.TryGetValue<bool>(out var b)) return b ? "true" : "false";
            if (val.TryGetValue<int>(out var i)) return i.ToString(CultureInfo.InvariantCulture);
            if (val.TryGetValue<long>(out var l)) return l.ToString(CultureInfo.InvariantCulture);
        }

        // Objects and arrays: JSON stringify
        return node.ToJsonString();
    }
}
