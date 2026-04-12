using System.Text.Json;
using System.Text.Json.Serialization;

namespace A2Ui.Core.Components;

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
