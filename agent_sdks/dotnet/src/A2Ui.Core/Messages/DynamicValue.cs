using System.Text.Json;
using System.Text.Json.Serialization;

namespace A2Ui.Core.Messages;

/// <summary>
/// Union type representing a value that can be a literal, a data binding path,
/// or a function call. Maps to the A2UI v0.9 DynamicValue/DynamicString/DynamicNumber/
/// DynamicBoolean/DynamicStringList types from common_types.json.
///
/// Wire formats:
///   "hello"                          → StringValue
///   42.5                             → NumberValue
///   true                             → BoolValue
///   ["a", "b"]                       → ArrayValue
///   {"path": "/reservation/date"}    → PathValue (data binding)
///   {"call": "formatDate", ...}      → FunctionValue
///
/// Use <see cref="Match{T}"/> for exhaustive dispatch over all subtypes.
/// </summary>
[JsonConverter(typeof(DynamicValueConverter))]
public abstract record DynamicValue
{
    private DynamicValue() { }

    /// <summary>A string literal value.</summary>
    public sealed record StringValue(string Value) : DynamicValue
    {
        /// <inheritdoc />
        public override T Match<T>(
            Func<StringValue, T> onString,
            Func<NumberValue, T> onNumber,
            Func<BoolValue, T> onBool,
            Func<ArrayValue, T> onArray,
            Func<PathValue, T> onPath,
            Func<FunctionValue, T> onFunction
        ) => onString(this);

        /// <inheritdoc />
        public override void Switch(
            Action<StringValue> onString,
            Action<NumberValue> onNumber,
            Action<BoolValue> onBool,
            Action<ArrayValue> onArray,
            Action<PathValue> onPath,
            Action<FunctionValue> onFunction
        ) => onString(this);
    }

    /// <summary>A numeric literal value.</summary>
    public sealed record NumberValue(double Value) : DynamicValue
    {
        /// <inheritdoc />
        public override T Match<T>(
            Func<StringValue, T> onString,
            Func<NumberValue, T> onNumber,
            Func<BoolValue, T> onBool,
            Func<ArrayValue, T> onArray,
            Func<PathValue, T> onPath,
            Func<FunctionValue, T> onFunction
        ) => onNumber(this);

        /// <inheritdoc />
        public override void Switch(
            Action<StringValue> onString,
            Action<NumberValue> onNumber,
            Action<BoolValue> onBool,
            Action<ArrayValue> onArray,
            Action<PathValue> onPath,
            Action<FunctionValue> onFunction
        ) => onNumber(this);
    }

    /// <summary>A boolean literal value.</summary>
    public sealed record BoolValue(bool Value) : DynamicValue
    {
        /// <inheritdoc />
        public override T Match<T>(
            Func<StringValue, T> onString,
            Func<NumberValue, T> onNumber,
            Func<BoolValue, T> onBool,
            Func<ArrayValue, T> onArray,
            Func<PathValue, T> onPath,
            Func<FunctionValue, T> onFunction
        ) => onBool(this);

        /// <inheritdoc />
        public override void Switch(
            Action<StringValue> onString,
            Action<NumberValue> onNumber,
            Action<BoolValue> onBool,
            Action<ArrayValue> onArray,
            Action<PathValue> onPath,
            Action<FunctionValue> onFunction
        ) => onBool(this);
    }

    /// <summary>A JSON array literal value.</summary>
    public sealed record ArrayValue(JsonElement Value) : DynamicValue
    {
        /// <inheritdoc />
        public override T Match<T>(
            Func<StringValue, T> onString,
            Func<NumberValue, T> onNumber,
            Func<BoolValue, T> onBool,
            Func<ArrayValue, T> onArray,
            Func<PathValue, T> onPath,
            Func<FunctionValue, T> onFunction
        ) => onArray(this);

        /// <inheritdoc />
        public override void Switch(
            Action<StringValue> onString,
            Action<NumberValue> onNumber,
            Action<BoolValue> onBool,
            Action<ArrayValue> onArray,
            Action<PathValue> onPath,
            Action<FunctionValue> onFunction
        ) => onArray(this);
    }

    /// <summary>A data binding path reference (e.g. "/user/name").</summary>
    public sealed record PathValue(string DataPath) : DynamicValue
    {
        /// <inheritdoc />
        public override T Match<T>(
            Func<StringValue, T> onString,
            Func<NumberValue, T> onNumber,
            Func<BoolValue, T> onBool,
            Func<ArrayValue, T> onArray,
            Func<PathValue, T> onPath,
            Func<FunctionValue, T> onFunction
        ) => onPath(this);

        /// <inheritdoc />
        public override void Switch(
            Action<StringValue> onString,
            Action<NumberValue> onNumber,
            Action<BoolValue> onBool,
            Action<ArrayValue> onArray,
            Action<PathValue> onPath,
            Action<FunctionValue> onFunction
        ) => onPath(this);
    }

    /// <summary>A client-side function call.</summary>
    public sealed record FunctionValue(FunctionCallValue Call) : DynamicValue
    {
        /// <inheritdoc />
        public override T Match<T>(
            Func<StringValue, T> onString,
            Func<NumberValue, T> onNumber,
            Func<BoolValue, T> onBool,
            Func<ArrayValue, T> onArray,
            Func<PathValue, T> onPath,
            Func<FunctionValue, T> onFunction
        ) => onFunction(this);

        /// <inheritdoc />
        public override void Switch(
            Action<StringValue> onString,
            Action<NumberValue> onNumber,
            Action<BoolValue> onBool,
            Action<ArrayValue> onArray,
            Action<PathValue> onPath,
            Action<FunctionValue> onFunction
        ) => onFunction(this);
    }

    /// <summary>
    /// Exhaustive pattern match over all <see cref="DynamicValue"/> subtypes.
    /// Each branch receives the concrete subtype instance.
    /// </summary>
    public abstract T Match<T>(
        Func<StringValue, T> onString,
        Func<NumberValue, T> onNumber,
        Func<BoolValue, T> onBool,
        Func<ArrayValue, T> onArray,
        Func<PathValue, T> onPath,
        Func<FunctionValue, T> onFunction
    );

    /// <summary>
    /// Exhaustive side-effecting dispatch over all <see cref="DynamicValue"/> subtypes.
    /// </summary>
    public abstract void Switch(
        Action<StringValue> onString,
        Action<NumberValue> onNumber,
        Action<BoolValue> onBool,
        Action<ArrayValue> onArray,
        Action<PathValue> onPath,
        Action<FunctionValue> onFunction
    );

    // ── Factory methods ──────────────────────────────────────────────

    /// <summary>Creates a <see cref="StringValue"/> from a string literal.</summary>
    public static DynamicValue FromString(string value) => new StringValue(value);

    /// <summary>Creates a <see cref="PathValue"/> from a data binding path.</summary>
    public static DynamicValue FromPath(string path) => new PathValue(path);

    /// <summary>Creates a <see cref="NumberValue"/> from a numeric literal.</summary>
    public static DynamicValue FromNumber(double value) => new NumberValue(value);

    /// <summary>Creates a <see cref="BoolValue"/> from a boolean literal.</summary>
    public static DynamicValue FromBool(bool value) => new BoolValue(value);
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
