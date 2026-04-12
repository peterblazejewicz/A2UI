using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using A2Ui.Core.Components;
using A2Ui.Core.Messages;

namespace A2Ui.Core.Surfaces;

/// <summary>
/// Per-surface data model store.
/// Resolves DynamicValue paths against the stored state.
/// Applies UpdateDataModel messages using JSON Pointer set/delete semantics.
/// </summary>
public sealed class DataModel
{
    private static readonly JsonSerializerOptions s_compactOptions = new();
    private static readonly JsonSerializerOptions s_indentedOptions = new() { WriteIndented = true };

    private JsonObject _root = new();

    /// <summary>
    /// Snapshot of the top-level keys currently present in the data model root.
    /// Not thread-safe — callers must synchronize with concurrent
    /// <see cref="Apply"/> / <see cref="SetSnapshot"/> invocations (matches the
    /// rest of <see cref="DataModel"/>: mutation and enumeration must not race).
    /// </summary>
    public IReadOnlyCollection<string> TopLevelKeys => [.. _root.Select(kvp => kvp.Key)];

    /// <summary>
    /// Split a JSON Pointer path into unescaped segments per RFC 6901.
    /// Order matters: ~1 → / before ~0 → ~.
    /// </summary>
    private static string[] SplitPath(string path) => path.TrimStart('/').Split('/').Select(UnescapeSegment).ToArray();

    private static string UnescapeSegment(string segment)
    {
        int tildeIdx = segment.IndexOf('~');
        if (tildeIdx < 0)
        {
            return segment;
        }

        var sb = new System.Text.StringBuilder(segment.Length);
        for (int i = 0; i < segment.Length; i++)
        {
            if (segment[i] != '~')
            {
                sb.Append(segment[i]);
                continue;
            }

            if (i + 1 >= segment.Length)
            {
                throw new FormatException($"Invalid JSON Pointer escape: trailing '~' in segment '{segment}'.");
            }

            char next = segment[i + 1];
            switch (next)
            {
                case '0':
                    sb.Append('~');
                    break;
                case '1':
                    sb.Append('/');
                    break;
                default:
                    throw new FormatException(
                        $"Invalid JSON Pointer escape '~{next}' in segment '{segment}'. Only ~0 and ~1 are valid (RFC 6901)."
                    );
            }

            i++; // skip the character after ~
        }

        return sb.ToString();
    }

    /// <summary>Serialize the current data model state to a JSON string.</summary>
    public string ToJson(bool indented = false) => _root.ToJsonString(indented ? s_indentedOptions : s_compactOptions);

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

        var segments = SplitPath(update.Path);

        // Null value → delete at path
        if (update.Value is null)
        {
            DeleteAtPath(segments);
            return;
        }

        // Set value at path — supports both object keys and array indices
        JsonNode container = _root;
        for (int i = 0; i < segments.Length - 1; i++)
        {
            string seg = segments[i];
            string nextSeg = segments[i + 1];
            bool nextIsIndex = int.TryParse(nextSeg, out _);

            container = NavigateOrCreate(container, seg, createArray: nextIsIndex);
        }

        string lastSeg = segments[^1];
        JsonNode? newValue = JsonNode.Parse(update.Value.Value.GetRawText());

        if (container is JsonObject obj)
        {
            obj[lastSeg] = newValue;
        }
        else if (container is JsonArray arr && int.TryParse(lastSeg, out int idx))
        {
            while (arr.Count <= idx)
                arr.Add(null);
            arr[idx] = newValue;
        }
    }

    /// <summary>
    /// Get the number of elements in the array at the given JSON Pointer path,
    /// or -1 if the path does not point to an array.
    /// </summary>
    public int GetArrayLength(string path)
    {
        JsonNode? node = ResolvePath(path);
        return node is JsonArray arr ? arr.Count : -1;
    }

    /// <summary>Resolve a DynamicValue. Returns null if path not found or FunctionCall.</summary>
    public string? Resolve(DynamicValue? value)
    {
        if (value is null)
            return null;

        return value.Match<string?>(
            onString: s => s.Value,
            onNumber: n => n.Value.ToString(CultureInfo.InvariantCulture),
            onBool: b => b.Value ? "true" : "false",
            onArray: _ => null,
            onPath: p => ResolvePathAsString(p.DataPath),
            onFunction: _ => null
        );
    }

    private string? ResolvePathAsString(string path)
    {
        JsonNode? node = ResolvePath(path);
        return NodeToString(node);
    }

    private JsonNode? ResolvePath(string path)
    {
        var segments = SplitPath(path);
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

    private static JsonNode NavigateOrCreate(JsonNode parent, string segment, bool createArray)
    {
        if (parent is JsonObject obj)
        {
            // Preserve existing container regardless of createArray hint
            if (obj[segment] is JsonObject or JsonArray)
                return obj[segment]!;

            // No container at this key — create based on hint
            JsonNode child = createArray ? new JsonArray() : new JsonObject();
            obj[segment] = child;
            return child;
        }

        if (parent is JsonArray arr && int.TryParse(segment, out int idx))
        {
            while (arr.Count <= idx)
                arr.Add(null);

            if (arr[idx] is JsonObject or JsonArray)
                return arr[idx]!;

            JsonNode child = createArray ? new JsonArray() : new JsonObject();
            arr[idx] = child;
            return child;
        }

        throw new JsonException(
            $"Cannot navigate segment '{segment}' on a {parent.GetType().Name} node. "
                + "Expected JsonObject or JsonArray as parent."
        );
    }

    private void DeleteAtPath(string[] segments)
    {
        if (segments.Length == 1)
        {
            _root.Remove(segments[0]);
            return;
        }

        // Navigate to parent of the target — supports both object and array nodes
        JsonNode current = _root;
        for (int i = 0; i < segments.Length - 1; i++)
        {
            string seg = segments[i];
            if (current is JsonObject obj && obj.ContainsKey(seg))
                current = obj[seg]!;
            else if (
                current is JsonArray arr
                && int.TryParse(seg, out int idx)
                && idx >= 0
                && idx < arr.Count
                && arr[idx] is not null
            )
                current = arr[idx]!;
            else
                return; // path doesn't exist, nothing to delete
        }

        // Remove the final segment from its parent
        string lastSeg = segments[^1];
        if (current is JsonObject parentObj)
            parentObj.Remove(lastSeg);
        else if (
            current is JsonArray parentArr
            && int.TryParse(lastSeg, out int lastIdx)
            && lastIdx >= 0
            && lastIdx < parentArr.Count
        )
            parentArr[lastIdx] = null;
    }

    private static string? NodeToString(JsonNode? node)
    {
        if (node is null)
            return null;

        if (node is JsonValue val)
        {
            if (val.TryGetValue<string>(out var s))
                return s;
            if (val.TryGetValue<double>(out var d))
                return d.ToString(CultureInfo.InvariantCulture);
            if (val.TryGetValue<bool>(out var b))
                return b ? "true" : "false";
            if (val.TryGetValue<int>(out var i))
                return i.ToString(CultureInfo.InvariantCulture);
            if (val.TryGetValue<long>(out var l))
                return l.ToString(CultureInfo.InvariantCulture);
        }

        // Objects and arrays: JSON stringify
        return node.ToJsonString();
    }
}
