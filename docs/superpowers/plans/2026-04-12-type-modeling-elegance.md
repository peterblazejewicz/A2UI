# Type Modeling Elegance Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Transform A2UI .NET SDK types for NuGet-ready elegance: DynamicValue sealed hierarchy, typed per-component records, OneOf discriminated unions, and supporting fixes.

**Architecture:** Bottom-up type foundation (OneOf, DynamicValue hierarchy) followed by consumer migration (renderer, catalog entries), then typed components in two phases (polymorphism first, property migration second). Each unit is independently gated by build+test.

**Tech Stack:** .NET 10, System.Text.Json polymorphism, xUnit v3 + MTP, Avalonia 12.0 headless

**Spec:** `docs/superpowers/specs/2026-04-12-type-modeling-elegance-design.md`

---

## Task 1: FormatString Stub Correction (Unit 0)

**Files:**
- Modify: `renderers/avalonia/src/A2Ui.Avalonia/Functions/FunctionRegistry.cs:68`
- Modify: `renderers/avalonia/src/A2Ui.Avalonia/Functions/BuiltInFunctions.cs:142-146`
- Modify: `renderers/avalonia/tests/A2Ui.Avalonia.Tests/Functions/FunctionRegistryTests.cs`

- [ ] **Step 1: Add test that formatString is not a registry function**

In `renderers/avalonia/tests/A2Ui.Avalonia.Tests/Functions/FunctionRegistryTests.cs`, add:

```csharp
[Fact]
public void FormatString_IsNotRegistered_ReturnsNull()
{
    // formatString is a renderer-level special form handled by RenderContext,
    // not a registry function. Calling it via the registry should return null.
    string? result = this.Eval("formatString", ("value", "hello ${name}"));
    Assert.Null(result);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --project renderers/avalonia/tests/A2Ui.Avalonia.Tests --configuration Release --filter-method "*.FormatString_IsNotRegistered_ReturnsNull*"`

Expected: FAIL — currently `FormatString` IS registered and returns `"hello ${name}"`.

- [ ] **Step 3: Remove formatString registration from FunctionRegistry**

In `renderers/avalonia/src/A2Ui.Avalonia/Functions/FunctionRegistry.cs`, remove line 68:

```csharp
// REMOVE this line:
.Register("formatString", BuiltInFunctions.FormatString)
```

The `CreateDefault()` method should go directly from `.Register("formatDate", BuiltInFunctions.FormatDate)` to `.Register("pluralize", BuiltInFunctions.Pluralize)`.

- [ ] **Step 4: Remove FormatString method from BuiltInFunctions**

In `renderers/avalonia/src/A2Ui.Avalonia/Functions/BuiltInFunctions.cs`, remove lines 142-146:

```csharp
// REMOVE this entire method:
public static string? FormatString(IReadOnlyDictionary<string, string?> args)
{
    // For now, return the value template as-is (full expression parsing deferred)
    return GetArg(args, "value") ?? "";
}
```

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test --project renderers/avalonia/tests/A2Ui.Avalonia.Tests --configuration Release --no-build --filter-method "*.FormatString_IsNotRegistered_ReturnsNull*"`

Expected: PASS

- [ ] **Step 6: Run full solution build and tests**

Run: `dotnet build A2Ui.slnx --configuration Release && dotnet test A2Ui.slnx --configuration Release --no-build`

Expected: All tests pass. The real formatString handling in `RenderContext.ResolveFunction()` at `A2UiRenderer.cs:386` is unaffected.

- [ ] **Step 7: Commit**

```bash
git add renderers/avalonia/src/A2Ui.Avalonia/Functions/FunctionRegistry.cs renderers/avalonia/src/A2Ui.Avalonia/Functions/BuiltInFunctions.cs renderers/avalonia/tests/A2Ui.Avalonia.Tests/Functions/FunctionRegistryTests.cs
git commit -m "fix(avalonia-renderer): remove dead formatString stub from FunctionRegistry

The formatString function was registered in FunctionRegistry but only
returned args['value'] as-is. The real implementation lives in
RenderContext.ResolveFormatString() which intercepts formatString calls
before they reach the registry. Remove the dead stub to prevent
confusion when callers use the registry directly.

Refs: docs/superpowers/specs/2026-04-12-type-modeling-elegance-design.md"
```

---

## Task 2: EventType Enum Drift Test (Unit 1)

**Files:**
- Create: `agent_sdks/dotnet/tests/AgUi.Protocol.Tests/Events/EventTypeCoverageTests.cs`

- [ ] **Step 1: Write the drift detection test**

Create `agent_sdks/dotnet/tests/AgUi.Protocol.Tests/Events/EventTypeCoverageTests.cs`:

```csharp
using System.Reflection;
using System.Text.Json.Serialization;
using AgUi.Protocol.Events;

namespace AgUi.Protocol.Tests.Events;

/// <summary>
/// Guards against drift between the <see cref="EventType"/> enum
/// and the <see cref="JsonDerivedTypeAttribute"/> list on <see cref="BaseEvent"/>.
/// </summary>
public sealed class EventTypeCoverageTests
{
    /// <summary>
    /// Every [JsonDerivedType] discriminator on BaseEvent must have
    /// a matching EventType enum value (SCREAMING_SNAKE → PascalCase).
    /// </summary>
    [Fact]
    public void AllJsonDerivedTypes_HaveMatchingEnumValue()
    {
        var attributes = typeof(BaseEvent)
            .GetCustomAttributes<JsonDerivedTypeAttribute>()
            .ToList();

        Assert.NotEmpty(attributes);

        foreach (var attr in attributes)
        {
            string discriminator = (string)attr.TypeDiscriminator!;
            string pascalCase = SnakeToPascal(discriminator);

            bool parsed = Enum.TryParse<EventType>(pascalCase, out _);
            Assert.True(parsed, $"JsonDerivedType discriminator '{discriminator}' (→ '{pascalCase}') has no matching EventType enum value.");
        }
    }

    /// <summary>
    /// Every EventType enum value must have a matching [JsonDerivedType]
    /// discriminator on BaseEvent (PascalCase → SCREAMING_SNAKE).
    /// </summary>
    [Fact]
    public void AllEnumValues_HaveMatchingJsonDerivedType()
    {
        var discriminators = typeof(BaseEvent)
            .GetCustomAttributes<JsonDerivedTypeAttribute>()
            .Select(a => (string)a.TypeDiscriminator!)
            .ToHashSet();

        foreach (EventType value in Enum.GetValues<EventType>())
        {
            string expected = PascalToSnake(value.ToString());
            Assert.Contains(expected, discriminators);
        }
    }

    /// <summary>Converts "RUN_STARTED" → "RunStarted".</summary>
    private static string SnakeToPascal(string snake)
    {
        var parts = snake.Split('_');
        return string.Concat(parts.Select(p =>
            p.Length == 0 ? "" : char.ToUpperInvariant(p[0]) + p[1..].ToLowerInvariant()));
    }

    /// <summary>Converts "RunStarted" → "RUN_STARTED".</summary>
    private static string PascalToSnake(string pascal)
    {
        var chars = new List<char>();
        for (int i = 0; i < pascal.Length; i++)
        {
            if (i > 0 && char.IsUpper(pascal[i]))
            {
                chars.Add('_');
            }

            chars.Add(char.ToUpperInvariant(pascal[i]));
        }

        return new string([.. chars]);
    }
}
```

- [ ] **Step 2: Run tests to verify they pass**

Run: `dotnet test --project agent_sdks/dotnet/tests/AgUi.Protocol.Tests --configuration Release --filter-method "*.EventTypeCoverageTests.*"`

Expected: PASS — the current 28 enum values and 28 `[JsonDerivedType]` attributes are in sync.

- [ ] **Step 3: Verify the test catches drift (sanity check)**

Temporarily add a dummy value to `EventType` enum (e.g., `Dummy`), rebuild, and confirm `AllEnumValues_HaveMatchingJsonDerivedType` fails. Then revert.

- [ ] **Step 4: Commit**

```bash
git add agent_sdks/dotnet/tests/AgUi.Protocol.Tests/Events/EventTypeCoverageTests.cs
git commit -m "test(dotnet-sdk): add EventType enum drift detection test

Reflection-based test validates that every [JsonDerivedType] on BaseEvent
has a matching EventType enum value, and vice versa. Prevents silent
drift when adding new event types.

Refs: docs/superpowers/specs/2026-04-12-type-modeling-elegance-design.md"
```

---

## Task 3: OneOf Discriminated Union Types (Unit 2)

**Files:**
- Create: `agent_sdks/dotnet/src/A2Ui.Core/OneOf.cs`
- Create: `agent_sdks/dotnet/tests/A2Ui.Core.Tests/A2Ui/OneOfTests.cs`

- [ ] **Step 1: Write the OneOf tests first**

Create `agent_sdks/dotnet/tests/A2Ui.Core.Tests/A2Ui/OneOfTests.cs`:

```csharp
using A2Ui.Core;

namespace A2Ui.Core.Tests;

public sealed class OneOfTests
{
    // ── OneOf<T1, T2> ──────────────────────────────────────────

    [Fact]
    public void TwoArg_ImplicitConversion_T1_SetsIsT1()
    {
        OneOf<string, int> value = "hello";

        Assert.True(value.IsT1);
        Assert.False(value.IsT2);
    }

    [Fact]
    public void TwoArg_ImplicitConversion_T2_SetsIsT2()
    {
        OneOf<string, int> value = 42;

        Assert.False(value.IsT1);
        Assert.True(value.IsT2);
    }

    [Fact]
    public void TwoArg_Match_T1_ReturnsCorrectBranch()
    {
        OneOf<string, int> value = "hello";

        string result = value.Match(
            s => $"string:{s}",
            i => $"int:{i}");

        Assert.Equal("string:hello", result);
    }

    [Fact]
    public void TwoArg_Match_T2_ReturnsCorrectBranch()
    {
        OneOf<string, int> value = 42;

        string result = value.Match(
            s => $"string:{s}",
            i => $"int:{i}");

        Assert.Equal("int:42", result);
    }

    [Fact]
    public void TwoArg_Switch_T1_InvokesCorrectAction()
    {
        OneOf<string, int> value = "hello";
        string? captured = null;

        value.Switch(
            s => captured = s,
            _ => Assert.Fail("Should not invoke T2 action"));

        Assert.Equal("hello", captured);
    }

    [Fact]
    public void TwoArg_TryGetAsT1_ReturnsTrueForT1()
    {
        OneOf<string, int> value = "hello";

        Assert.True(value.TryGetAsT1(out string? result));
        Assert.Equal("hello", result);
    }

    [Fact]
    public void TwoArg_TryGetAsT1_ReturnsFalseForT2()
    {
        OneOf<string, int> value = 42;

        Assert.False(value.TryGetAsT1(out string? result));
        Assert.Null(result);
    }

    [Fact]
    public void TwoArg_RecordEquality_SameCaseSameValue_AreEqual()
    {
        OneOf<string, int> a = "hello";
        OneOf<string, int> b = "hello";

        Assert.Equal(a, b);
    }

    [Fact]
    public void TwoArg_RecordEquality_DifferentCases_AreNotEqual()
    {
        // Cannot directly compare "hello" vs 0 since they are different types,
        // but we can construct and compare via the abstract base.
        OneOf<string, int> a = "hello";
        OneOf<string, int> b = 0;

        Assert.NotEqual(a, b);
    }

    // ── OneOf<T1, T2, T3> ──────────────────────────────────────

    [Fact]
    public void ThreeArg_ImplicitConversion_T3_SetsIsT3()
    {
        OneOf<string, int, bool> value = true;

        Assert.False(value.IsT1);
        Assert.False(value.IsT2);
        Assert.True(value.IsT3);
    }

    [Fact]
    public void ThreeArg_Match_T3_ReturnsCorrectBranch()
    {
        OneOf<string, int, bool> value = true;

        string result = value.Match(
            s => $"string:{s}",
            i => $"int:{i}",
            b => $"bool:{b}");

        Assert.Equal("bool:True", result);
    }

    [Fact]
    public void ThreeArg_TryGetAsT3_ReturnsTrueForT3()
    {
        OneOf<string, int, bool> value = true;

        Assert.True(value.TryGetAsT3(out bool? result));
        Assert.Equal(true, result);
    }

    [Fact]
    public void ThreeArg_TryGetAsT1_ReturnsFalseForT3()
    {
        OneOf<string, int, bool> value = true;

        Assert.False(value.TryGetAsT1(out string? result));
        Assert.Null(result);
    }

    [Fact]
    public void ThreeArg_Switch_T2_InvokesCorrectAction()
    {
        OneOf<string, int, bool> value = 42;
        int? captured = null;

        value.Switch(
            _ => Assert.Fail("Should not invoke T1"),
            i => captured = i,
            _ => Assert.Fail("Should not invoke T3"));

        Assert.Equal(42, captured);
    }

    [Fact]
    public void ThreeArg_RecordEquality_SameCaseSameValue_AreEqual()
    {
        OneOf<string, int, bool> a = 42;
        OneOf<string, int, bool> b = 42;

        Assert.Equal(a, b);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test --project agent_sdks/dotnet/tests/A2Ui.Core.Tests --configuration Release --filter-method "*.OneOfTests.*"`

Expected: FAIL — `OneOf` type does not exist yet.

- [ ] **Step 3: Implement OneOf types**

Create `agent_sdks/dotnet/src/A2Ui.Core/OneOf.cs`:

```csharp
namespace A2Ui.Core;

/// <summary>
/// Discriminated union of two possible types.
/// Use <see cref="Match{T}"/> for exhaustive handling.
/// </summary>
/// <typeparam name="T1">The first possible type.</typeparam>
/// <typeparam name="T2">The second possible type.</typeparam>
public abstract record OneOf<T1, T2>
{
    private OneOf() { }

    /// <summary>Case holding a value of type <typeparamref name="T1"/>.</summary>
    public sealed record Case1(T1 Value) : OneOf<T1, T2>;

    /// <summary>Case holding a value of type <typeparamref name="T2"/>.</summary>
    public sealed record Case2(T2 Value) : OneOf<T1, T2>;

    /// <summary>True when this instance holds a <typeparamref name="T1"/> value.</summary>
    public bool IsT1 => this is Case1;

    /// <summary>True when this instance holds a <typeparamref name="T2"/> value.</summary>
    public bool IsT2 => this is Case2;

    /// <summary>Exhaustive match — exactly one branch executes.</summary>
    public T Match<T>(Func<T1, T> onT1, Func<T2, T> onT2) => this switch
    {
        Case1 c1 => onT1(c1.Value),
        Case2 c2 => onT2(c2.Value),
        _ => throw new InvalidOperationException("Unreachable"),
    };

    /// <summary>Exhaustive switch — exactly one action executes.</summary>
    public void Switch(Action<T1> onT1, Action<T2> onT2)
    {
        switch (this)
        {
            case Case1 c1:
                onT1(c1.Value);
                break;
            case Case2 c2:
                onT2(c2.Value);
                break;
            default:
                throw new InvalidOperationException("Unreachable");
        }
    }

    /// <summary>Try to extract the <typeparamref name="T1"/> value.</summary>
    public bool TryGetAsT1(out T1? value)
    {
        if (this is Case1 c1)
        {
            value = c1.Value;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>Try to extract the <typeparamref name="T2"/> value.</summary>
    public bool TryGetAsT2(out T2? value)
    {
        if (this is Case2 c2)
        {
            value = c2.Value;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>Implicit conversion from <typeparamref name="T1"/>.</summary>
    public static implicit operator OneOf<T1, T2>(T1 value) => new Case1(value);

    /// <summary>Implicit conversion from <typeparamref name="T2"/>.</summary>
    public static implicit operator OneOf<T1, T2>(T2 value) => new Case2(value);
}

/// <summary>
/// Discriminated union of three possible types.
/// Use <see cref="Match{T}"/> for exhaustive handling.
/// </summary>
/// <typeparam name="T1">The first possible type.</typeparam>
/// <typeparam name="T2">The second possible type.</typeparam>
/// <typeparam name="T3">The third possible type.</typeparam>
public abstract record OneOf<T1, T2, T3>
{
    private OneOf() { }

    /// <summary>Case holding a value of type <typeparamref name="T1"/>.</summary>
    public sealed record Case1(T1 Value) : OneOf<T1, T2, T3>;

    /// <summary>Case holding a value of type <typeparamref name="T2"/>.</summary>
    public sealed record Case2(T2 Value) : OneOf<T1, T2, T3>;

    /// <summary>Case holding a value of type <typeparamref name="T3"/>.</summary>
    public sealed record Case3(T3 Value) : OneOf<T1, T2, T3>;

    /// <summary>True when this instance holds a <typeparamref name="T1"/> value.</summary>
    public bool IsT1 => this is Case1;

    /// <summary>True when this instance holds a <typeparamref name="T2"/> value.</summary>
    public bool IsT2 => this is Case2;

    /// <summary>True when this instance holds a <typeparamref name="T3"/> value.</summary>
    public bool IsT3 => this is Case3;

    /// <summary>Exhaustive match — exactly one branch executes.</summary>
    public T Match<T>(Func<T1, T> onT1, Func<T2, T> onT2, Func<T3, T> onT3) => this switch
    {
        Case1 c1 => onT1(c1.Value),
        Case2 c2 => onT2(c2.Value),
        Case3 c3 => onT3(c3.Value),
        _ => throw new InvalidOperationException("Unreachable"),
    };

    /// <summary>Exhaustive switch — exactly one action executes.</summary>
    public void Switch(Action<T1> onT1, Action<T2> onT2, Action<T3> onT3)
    {
        switch (this)
        {
            case Case1 c1:
                onT1(c1.Value);
                break;
            case Case2 c2:
                onT2(c2.Value);
                break;
            case Case3 c3:
                onT3(c3.Value);
                break;
            default:
                throw new InvalidOperationException("Unreachable");
        }
    }

    /// <summary>Try to extract the <typeparamref name="T1"/> value.</summary>
    public bool TryGetAsT1(out T1? value)
    {
        if (this is Case1 c1)
        {
            value = c1.Value;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>Try to extract the <typeparamref name="T2"/> value.</summary>
    public bool TryGetAsT2(out T2? value)
    {
        if (this is Case2 c2)
        {
            value = c2.Value;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>Try to extract the <typeparamref name="T3"/> value.</summary>
    public bool TryGetAsT3(out T3? value)
    {
        if (this is Case3 c3)
        {
            value = c3.Value;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>Implicit conversion from <typeparamref name="T1"/>.</summary>
    public static implicit operator OneOf<T1, T2, T3>(T1 value) => new Case1(value);

    /// <summary>Implicit conversion from <typeparamref name="T2"/>.</summary>
    public static implicit operator OneOf<T1, T2, T3>(T2 value) => new Case2(value);

    /// <summary>Implicit conversion from <typeparamref name="T3"/>.</summary>
    public static implicit operator OneOf<T1, T2, T3>(T3 value) => new Case3(value);
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test --project agent_sdks/dotnet/tests/A2Ui.Core.Tests --configuration Release --filter-method "*.OneOfTests.*"`

Expected: All 14 tests PASS.

- [ ] **Step 5: Run full solution build**

Run: `dotnet build A2Ui.slnx --configuration Release`

Expected: Zero errors, zero warnings.

- [ ] **Step 6: Commit**

```bash
git add agent_sdks/dotnet/src/A2Ui.Core/OneOf.cs agent_sdks/dotnet/tests/A2Ui.Core.Tests/A2Ui/OneOfTests.cs
git commit -m "feat(dotnet-sdk): add OneOf<T1,T2> and OneOf<T1,T2,T3> discriminated unions

Hand-rolled generic discriminated unions with Match/Switch exhaustive
handling, TryGetAs* accessors, implicit conversions, and record equality.
Will be used by typed component properties in subsequent units.

Refs: docs/superpowers/specs/2026-04-12-type-modeling-elegance-design.md"
```

---

## Task 4: DynamicValue Sealed Hierarchy with Compat Properties (Unit 3)

**Files:**
- Modify: `agent_sdks/dotnet/src/A2Ui.Core/Messages/DynamicValue.cs`
- Modify: `agent_sdks/dotnet/src/A2Ui.Core/DataModel.cs:149-163`
- Modify: `agent_sdks/dotnet/tests/A2Ui.Core.Tests/A2Ui/DynamicValueConverterTests.cs`

- [ ] **Step 1: Add new tests for the hierarchy (alongside existing)**

In `agent_sdks/dotnet/tests/A2Ui.Core.Tests/A2Ui/DynamicValueConverterTests.cs`, add these tests at the end of the class:

```csharp
// ── Hierarchy tests (Unit 3) ──────────────────────────────────

[Fact]
public void FromString_ReturnsStringValue()
{
    DynamicValue dv = DynamicValue.FromString("hello");
    Assert.IsType<DynamicValue.StringValue>(dv);
}

[Fact]
public void FromNumber_ReturnsNumberValue()
{
    DynamicValue dv = DynamicValue.FromNumber(3.14);
    Assert.IsType<DynamicValue.NumberValue>(dv);
}

[Fact]
public void FromBool_ReturnsBoolValue()
{
    DynamicValue dv = DynamicValue.FromBool(true);
    Assert.IsType<DynamicValue.BoolValue>(dv);
}

[Fact]
public void FromPath_ReturnsPathValue()
{
    DynamicValue dv = DynamicValue.FromPath("/user/name");
    Assert.IsType<DynamicValue.PathValue>(dv);
}

[Fact]
public void Read_FunctionCall_ReturnsFunctionValue()
{
    var json = """{"call":"required","returnType":"boolean"}""";
    DynamicValue? dv = JsonSerializer.Deserialize<DynamicValue>(json, s_opts);
    Assert.IsType<DynamicValue.FunctionValue>(dv);
}

[Fact]
public void Match_StringValue_InvokesCorrectBranch()
{
    DynamicValue dv = DynamicValue.FromString("hello");

    string result = dv.Match(
        onString: s => $"str:{s.Value}",
        onNumber: _ => "num",
        onBool: _ => "bool",
        onArray: _ => "arr",
        onPath: _ => "path",
        onFunction: _ => "fn");

    Assert.Equal("str:hello", result);
}

[Fact]
public void RecordEquality_SameSubtypeSameValue_AreEqual()
{
    DynamicValue a = DynamicValue.FromString("test");
    DynamicValue b = DynamicValue.FromString("test");

    Assert.Equal(a, b);
}

[Fact]
public void RecordEquality_DifferentSubtype_AreNotEqual()
{
    DynamicValue a = DynamicValue.FromString("42");
    DynamicValue b = DynamicValue.FromNumber(42);

    Assert.NotEqual(a, b);
}
```

- [ ] **Step 2: Run new tests to verify they fail**

Run: `dotnet test --project agent_sdks/dotnet/tests/A2Ui.Core.Tests --configuration Release --filter-method "*.FromString_ReturnsStringValue*"`

Expected: FAIL — `DynamicValue.StringValue` type does not exist yet.

- [ ] **Step 3: Rewrite DynamicValue as sealed hierarchy**

Replace the entire content of `agent_sdks/dotnet/src/A2Ui.Core/Messages/DynamicValue.cs`:

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;

namespace A2Ui.Core.Messages;

/// <summary>
/// Union type representing a value that can be a literal, a data binding path,
/// or a function call. Maps to the A2UI v0.9 DynamicValue/DynamicString/DynamicNumber/
/// DynamicBoolean/DynamicStringList types from common_types.json.
///
/// Wire formats:
///   "hello"                          -> StringValue
///   42.5                             -> NumberValue
///   true                             -> BoolValue
///   ["a", "b"]                       -> ArrayValue
///   {"path": "/reservation/date"}    -> PathValue
///   {"call": "formatDate", ...}      -> FunctionValue
///
/// Use <see cref="Match{T}"/> for exhaustive handling of all variants.
/// </summary>
[JsonConverter(typeof(DynamicValueConverter))]
public abstract record DynamicValue
{
    private DynamicValue() { }

    // ── Subtypes ─────────────────────────────────────────────────────────

    /// <summary>A literal string value.</summary>
    public sealed record StringValue(string Value) : DynamicValue;

    /// <summary>A literal numeric value.</summary>
    public sealed record NumberValue(double Value) : DynamicValue;

    /// <summary>A literal boolean value.</summary>
    public sealed record BoolValue(bool Value) : DynamicValue;

    /// <summary>A literal JSON array value.</summary>
    public sealed record ArrayValue(JsonElement Value) : DynamicValue;

    /// <summary>A data binding path (RFC 6901 JSON Pointer).</summary>
    public sealed record PathValue(string Path) : DynamicValue;

    /// <summary>A client-side function call.</summary>
    public sealed record FunctionValue(FunctionCallValue Call) : DynamicValue;

    // ── Exhaustive match ─────────────────────────────────────────────────

    /// <summary>Exhaustive match — exactly one branch executes.</summary>
    public abstract T Match<T>(
        Func<StringValue, T> onString,
        Func<NumberValue, T> onNumber,
        Func<BoolValue, T> onBool,
        Func<ArrayValue, T> onArray,
        Func<PathValue, T> onPath,
        Func<FunctionValue, T> onFunction);

    /// <summary>Exhaustive switch — exactly one action executes.</summary>
    public abstract void Switch(
        Action<StringValue> onString,
        Action<NumberValue> onNumber,
        Action<BoolValue> onBool,
        Action<ArrayValue> onArray,
        Action<PathValue> onPath,
        Action<FunctionValue> onFunction);

    // ── Factory methods (preserved API) ──────────────────────────────────

    /// <summary>Create a string literal value.</summary>
    public static DynamicValue FromString(string value) => new StringValue(value);

    /// <summary>Create a data binding path value.</summary>
    public static DynamicValue FromPath(string path) => new PathValue(path);

    /// <summary>Create a numeric literal value.</summary>
    public static DynamicValue FromNumber(double value) => new NumberValue(value);

    /// <summary>Create a boolean literal value.</summary>
    public static DynamicValue FromBool(bool value) => new BoolValue(value);

    // ── Backward-compat properties (removed in Unit 4) ───────────────────

    /// <summary>String literal value, or null. Compat property — prefer Match.</summary>
    public string? StringLiteral => this is StringValue s ? s.Value : null;

    /// <summary>Numeric literal value, or null. Compat property — prefer Match.</summary>
    public double? NumberLiteral => this is NumberValue n ? n.Value : null;

    /// <summary>Boolean literal value, or null. Compat property — prefer Match.</summary>
    public bool? BoolLiteral => this is BoolValue b ? b.Value : null;

    /// <summary>Array literal value, or null. Compat property — prefer Match.</summary>
    public JsonElement? ArrayLiteral => this is ArrayValue a ? a.Value : null;

    /// <summary>Data binding path, or null. Compat property — prefer Match.</summary>
    public string? Path => this is PathValue p ? p.Path : null;

    /// <summary>Function call value, or null. Compat property — prefer Match.</summary>
    public FunctionCallValue? FunctionCall => this is FunctionValue f ? f.Call : null;

    /// <summary>True when this is a data binding path. Compat — prefer pattern matching.</summary>
    public bool IsBound => this is PathValue;

    /// <summary>True when this is a function call. Compat — prefer pattern matching.</summary>
    public bool IsFunction => this is FunctionValue;

    /// <summary>True when this is a literal (not a path or function). Compat — prefer pattern matching.</summary>
    public bool IsLiteral => this is not PathValue and not FunctionValue;
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
    public override DynamicValue? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
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
        {
            return new DynamicValue.PathValue(pathEl.GetString()!);
        }

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
                writer.WriteString("path", p.Path);
                writer.WriteEndObject();
            },
            onFunction: f => JsonSerializer.Serialize(writer, f.Call, options));
    }
}
```

Each subtype must implement the abstract `Match` and `Switch`. Add these implementations inside each nested record. The simplest approach: add the overrides in each subtype definition. Since we are using the nested record pattern where the subtype is defined inside the abstract, we need to add them. Here is the adjustment — each subtype needs:

For `StringValue`:
```csharp
public sealed record StringValue(string Value) : DynamicValue
{
    public override T Match<T>(Func<StringValue, T> onString, Func<NumberValue, T> onNumber, Func<BoolValue, T> onBool, Func<ArrayValue, T> onArray, Func<PathValue, T> onPath, Func<FunctionValue, T> onFunction) => onString(this);
    public override void Switch(Action<StringValue> onString, Action<NumberValue> onNumber, Action<BoolValue> onBool, Action<ArrayValue> onArray, Action<PathValue> onPath, Action<FunctionValue> onFunction) => onString(this);
}
```

Repeat the same pattern for each of the 6 subtypes, invoking the corresponding delegate.

- [ ] **Step 4: Update DataModel.Resolve() to use Match**

In `agent_sdks/dotnet/src/A2Ui.Core/DataModel.cs`, replace the `Resolve` method (lines 149-163):

```csharp
/// <summary>Resolve a DynamicValue. Returns null if path not found or FunctionCall.</summary>
public string? Resolve(DynamicValue? value)
{
    if (value is null)
    {
        return null;
    }

    return value.Match<string?>(
        onString: s => s.Value,
        onNumber: n => n.Value.ToString(CultureInfo.InvariantCulture),
        onBool: b => b.Value ? "true" : "false",
        onArray: _ => null,
        onPath: p => ResolvePathAsString(p.Path),
        onFunction: _ => null);
}
```

- [ ] **Step 5: Build the full solution**

Run: `dotnet build A2Ui.slnx --configuration Release`

Expected: Zero errors, zero warnings. All existing consumers continue to compile via compat properties.

- [ ] **Step 6: Run all tests**

Run: `dotnet test A2Ui.slnx --configuration Release --no-build`

Expected: ALL tests pass (existing 487 + 8 new hierarchy tests).

- [ ] **Step 7: Commit**

```bash
git add agent_sdks/dotnet/src/A2Ui.Core/Messages/DynamicValue.cs agent_sdks/dotnet/src/A2Ui.Core/DataModel.cs agent_sdks/dotnet/tests/A2Ui.Core.Tests/A2Ui/DynamicValueConverterTests.cs
git commit -m "feat(dotnet-sdk): transform DynamicValue into sealed hierarchy with Match

DynamicValue is now an abstract record with 6 sealed subtypes:
StringValue, NumberValue, BoolValue, ArrayValue, PathValue, FunctionValue.
Match<T> and Switch methods force exhaustive handling. Backward-compat
properties (StringLiteral, Path, IsBound, etc.) are preserved for
existing consumers — will be removed in a follow-up unit.
DataModel.Resolve() updated to use Match.

Refs: docs/superpowers/specs/2026-04-12-type-modeling-elegance-design.md"
```

---

## Task 5: Remove DynamicValue Compat Accessors (Unit 4)

**Files:**
- Modify: `agent_sdks/dotnet/src/A2Ui.Core/Messages/DynamicValue.cs`
- Modify: `renderers/avalonia/src/A2Ui.Avalonia/A2UiRenderer.cs:297-336,419-469`
- Modify: `renderers/avalonia/src/A2Ui.Avalonia/Catalog/Entries/InputCatalogEntries.cs`
- Modify: `agent_sdks/dotnet/tests/A2Ui.Core.Tests/A2Ui/DynamicValueConverterTests.cs`
- Modify: `agent_sdks/dotnet/tests/A2Ui.Core.Tests/A2Ui/A2UiMessageTests.cs`

- [ ] **Step 1: Remove compat properties from DynamicValue**

In `agent_sdks/dotnet/src/A2Ui.Core/Messages/DynamicValue.cs`, remove the entire "Backward-compat properties" section (the properties `StringLiteral`, `NumberLiteral`, `BoolLiteral`, `ArrayLiteral`, `Path`, `FunctionCall`, `IsBound`, `IsFunction`, `IsLiteral`).

- [ ] **Step 2: Build to identify all consumer breakages**

Run: `dotnet build A2Ui.slnx --configuration Release 2>&1 | head -100`

Expected: Compiler errors in every file that accesses the removed properties. Fix each systematically:

- [ ] **Step 3: Fix A2UiRenderer.ResolveCore()**

In `renderers/avalonia/src/A2Ui.Avalonia/A2UiRenderer.cs`, replace the `ResolveCore` method body with pattern matching:

```csharp
private string? ResolveCore(DynamicValue? value, int depth)
{
    if (value is null)
    {
        return null;
    }

    if (depth > MaxResolveDepth)
    {
        if (logger is not null)
        {
            RendererLog.ResolveExceededMaxDepth(logger, MaxResolveDepth);
        }

        return null;
    }

    if (value is DynamicValue.FunctionValue { Call: var fc })
    {
        return this.ResolveFunction(fc, depth);
    }

    if (value is DynamicValue.ArrayValue { Value: var arrayEl })
    {
        return this.ResolveArrayLiteral(arrayEl, depth);
    }

    // Scope relative paths when inside a template expansion
    if (basePath is not null && value is DynamicValue.PathValue { Path: var path } && !path.StartsWith('/'))
    {
        return this.ResolveScopedPath(path);
    }

    return surface.DataModel.Resolve(value);
}
```

- [ ] **Step 4: Fix A2UiRenderer.ResolveFormatString()**

Replace `valDv?.StringLiteral` access with pattern matching:

```csharp
string? template = valDv is DynamicValue.StringValue sv ? sv.Value : null;
```

- [ ] **Step 5: Fix A2UiRenderer.ExpandTemplate()**

Replace `tmpl.Path.TrimStart('/')` with:

```csharp
string arrayPath = ((DynamicValue.PathValue)tmpl).Path.TrimStart('/');
```

This cast is safe — `ExpandTemplate` is only called when the template has a path.

- [ ] **Step 6: Add DynamicValueExtensions helper for input binding path extraction**

In `renderers/avalonia/src/A2Ui.Avalonia/Catalog/Entries/InputCatalogEntries.cs` (or a new file if preferred), add a static helper at the top of the file:

```csharp
internal static class DynamicValueExtensions
{
    /// <summary>Extract the binding path from a DynamicValue, or null.</summary>
    public static string? GetBindingPath(this DynamicValue? value) =>
        value is DynamicValue.PathValue p ? p.Path : null;
}
```

Then replace the 4 sites that use `component.Value?.Path`:
- `TextFieldCatalogEntry`: `component.Value?.Path` → `component.Value.GetBindingPath()`
- `DateTimeInputCatalogEntry`: same pattern
- `ChoicePickerCatalogEntry`: same pattern
- `SliderCatalogEntry`: same pattern

- [ ] **Step 7: Fix test assertions**

In `DynamicValueConverterTests.cs`, update assertions that use removed properties:

```csharp
// Before: Assert.Equal("hello", result!.StringLiteral);
// After:
var sv = Assert.IsType<DynamicValue.StringValue>(result);
Assert.Equal("hello", sv.Value);

// Before: Assert.Equal(42.5, result!.NumberLiteral);
// After:
var nv = Assert.IsType<DynamicValue.NumberValue>(result);
Assert.Equal(42.5, nv.Value);

// Before: Assert.True(trueResult!.BoolLiteral);
// After:
var bv = Assert.IsType<DynamicValue.BoolValue>(trueResult);
Assert.True(bv.Value);

// Before: Assert.Equal("/user/name", result!.Path);
// After:
var pv = Assert.IsType<DynamicValue.PathValue>(result);
Assert.Equal("/user/name", pv.Path);

// Before: Assert.Equal("formatDate", result.FunctionCall!.Call);
// After:
var fv = Assert.IsType<DynamicValue.FunctionValue>(result);
Assert.Equal("formatDate", fv.Call.Call);
```

In `RoundTrip_PreservesAllForms`, use `Match` or pattern matching for equality assertions.

Fix similar patterns in `A2UiMessageTests.cs` and any other test files that reference removed compat properties.

- [ ] **Step 8: Build and run all tests**

Run: `dotnet build A2Ui.slnx --configuration Release && dotnet test A2Ui.slnx --configuration Release --no-build`

Expected: Zero errors, all tests pass.

- [ ] **Step 9: Commit**

```bash
git add -A
git commit -m "refactor(dotnet-sdk): remove DynamicValue compat properties, use Match everywhere

All consumers now use Match<T>, pattern matching (is PathValue), or the
GetBindingPath() extension instead of the flat compat properties.
DataModel.Resolve uses Match (done in prior commit). RenderContext uses
pattern matching. Input entries use GetBindingPath() extension.

Refs: docs/superpowers/specs/2026-04-12-type-modeling-elegance-design.md"
```

---

## Task 6: ICatalogEntry.Update Improvements (Unit 5)

**Files:**
- Modify: `renderers/avalonia/src/A2Ui.Avalonia/Catalog/Entries/ButtonCatalogEntry.cs:52`
- Modify: `renderers/avalonia/src/A2Ui.Avalonia/Catalog/Entries/LayoutCatalogEntries.cs:21,36,73`
- Modify: `renderers/avalonia/src/A2Ui.Avalonia/Catalog/Entries/InteractiveCatalogEntries.cs:29,52,144`
- Modify: `renderers/avalonia/src/A2Ui.Avalonia/Catalog/Entries/DisplayCatalogEntries.cs:124,139`
- Modify: `renderers/avalonia/src/A2Ui.Avalonia/Catalog/Entries/MediaCatalogEntries.cs:139,158`

This task is best split into sub-steps by risk level.

- [ ] **Step 1: Implement Video and AudioPlayer Update (trivial)**

In `DisplayCatalogEntries.cs`, replace Video and AudioPlayer Update methods:

```csharp
// VideoCatalogEntry
public bool Update(Control existing, A2UiComponent component, DataModel dataModel, IRenderContext context)
{
    if (existing is not TextBlock tb)
    {
        return false;
    }

    tb.Text = $"[Video: {context.Resolve(component.Url) ?? "no url"}]";
    return true;
}

// AudioPlayerCatalogEntry
public bool Update(Control existing, A2UiComponent component, DataModel dataModel, IRenderContext context)
{
    if (existing is not TextBlock tb)
    {
        return false;
    }

    tb.Text = $"[AudioPlayer: {context.Resolve(component.Url) ?? "no url"}]";
    return true;
}
```

- [ ] **Step 2: Implement ButtonCatalogEntry Update**

In `ButtonCatalogEntry.cs`, replace line 52:

```csharp
public bool Update(Control existing, A2UiComponent component, DataModel dataModel, IRenderContext context)
{
    if (existing is not Button btn)
    {
        return false;
    }

    // Update content (text/label or child)
    btn.Content = component.Child is not null
        ? context.RenderChild(component.Child)
        : (object?)(context.Resolve(component.Text) ?? context.Resolve(component.Label) ?? string.Empty);

    ApplyVariant(btn, component.Variant);

    // Re-evaluate checks — action handler closure is retained from Create
    if (component.Checks is { Length: > 0 })
    {
        bool allPass = CheckHelper.AllChecksPassing(component, context);
        btn.IsEnabled = allPass;
        string? failedMessage = allPass ? null : CheckHelper.FirstFailingMessage(component, context);
        ToolTip.SetTip(btn, failedMessage);
    }
    else
    {
        btn.IsEnabled = true;
        ToolTip.SetTip(btn, null);
    }

    return true;
}
```

- [ ] **Step 3: Implement SurfaceCatalogEntry Update**

In `MediaCatalogEntries.cs`, replace the SurfaceCatalogEntry Update method:

```csharp
public bool Update(Control existing, A2UiComponent component, DataModel dataModel, IRenderContext context)
{
    if (existing is not StackPanel panel)
    {
        return false;
    }

    panel.Children.Clear();
    foreach (var child in context.RenderChildren(component.Id))
    {
        panel.Children.Add(child);
    }

    return true;
}
```

- [ ] **Step 4: Implement ListCatalogEntry Update**

In `InteractiveCatalogEntries.cs`, replace the ListCatalogEntry Update method:

```csharp
public bool Update(Control existing, A2UiComponent component, DataModel dataModel, IRenderContext context)
{
    if (existing is not ScrollViewer sv || sv.Content is not StackPanel panel)
    {
        return false;
    }

    var newOrientation = component.Direction == "horizontal" ? Orientation.Horizontal : Orientation.Vertical;
    if (panel.Orientation != newOrientation)
    {
        return false; // Structural change — recreate
    }

    // Re-render children into existing panel (preserves scroll position)
    panel.Children.Clear();
    foreach (var child in context.RenderChildren(component.Id))
    {
        panel.Children.Add(child);
    }

    return true;
}
```

- [ ] **Step 5: Implement CardCatalogEntry Update (partial)**

In `LayoutCatalogEntries.cs`, replace the CardCatalogEntry Update method:

```csharp
public bool Update(Control existing, A2UiComponent component, DataModel dataModel, IRenderContext context)
{
    if (existing is not Border border)
    {
        return false;
    }

    if (component.Child is not null)
    {
        // Single-child path: update in place
        border.Child = context.RenderChild(component.Child);
        return true;
    }

    // Multi-child fallback: child list may have changed structurally
    return false;
}
```

- [ ] **Step 6: Implement TabsCatalogEntry Update**

In `InteractiveCatalogEntries.cs`, replace the TabsCatalogEntry Update method:

```csharp
public bool Update(Control existing, A2UiComponent component, DataModel dataModel, IRenderContext context)
{
    if (existing is not TabControl tc)
    {
        return false;
    }

    if (component.Tabs is not { } tabs || tabs.Length != tc.Items.Count)
    {
        return false; // Tab count changed — recreate
    }

    for (int i = 0; i < tabs.Length; i++)
    {
        if (tc.Items[i] is not TabItem tabItem)
        {
            return false;
        }

        tabItem.Header = tabs[i].Title;
        tabItem.Content = context.RenderChild(tabs[i].Child);
    }

    return true;
}
```

- [ ] **Step 7: Implement TableCatalogEntry Update**

In `MediaCatalogEntries.cs`, replace the TableCatalogEntry Update method:

```csharp
public bool Update(Control existing, A2UiComponent component, DataModel dataModel, IRenderContext context)
{
    if (existing is not DataGrid grid)
    {
        return false;
    }

    if (component.Columns is not { } cols || cols.Length != grid.Columns.Count)
    {
        return false; // Column schema changed — recreate
    }

    for (int i = 0; i < cols.Length; i++)
    {
        if (grid.Columns[i] is DataGridTextColumn col)
        {
            col.Header = cols[i].Header;
        }
    }

    return true;
}
```

- [ ] **Step 8: Document legitimate-recreate decisions**

In `LayoutCatalogEntries.cs`, update the Column and Row Update comments:

```csharp
// ColumnCatalogEntry
public bool Update(Control existing, A2UiComponent component, DataModel dataModel, IRenderContext context) =>
    false; // Panel type (StackPanel vs Grid) is selected at creation based on justify/weights — cannot be mutated in-place

// RowCatalogEntry
public bool Update(Control existing, A2UiComponent component, DataModel dataModel, IRenderContext context) =>
    false; // Panel type (StackPanel vs Grid) is selected at creation based on justify/weights — cannot be mutated in-place
```

In `InteractiveCatalogEntries.cs`, update the Modal Update comment:

```csharp
// ModalCatalogEntry
public bool Update(Control existing, A2UiComponent component, DataModel dataModel, IRenderContext context) =>
    false; // Popup.PlacementTarget goes stale if trigger is recreated; lifecycle handlers are not idempotent
```

- [ ] **Step 9: Build and run all tests**

Run: `dotnet build A2Ui.slnx --configuration Release && dotnet test A2Ui.slnx --configuration Release --no-build`

Expected: All tests pass.

- [ ] **Step 10: Commit**

```bash
git add renderers/avalonia/src/A2Ui.Avalonia/Catalog/Entries/
git commit -m "fix(avalonia-renderer): implement in-place Update for 8 catalog entries

Button, Card (single-child), List, Tabs, Table, Surface, Video, and
AudioPlayer now update controls in-place instead of always recreating.
Preserves scroll position (List), selected tab (Tabs), and click
handlers (Button). Column/Row/Modal documented as legitimate recreates.

Refs: docs/superpowers/specs/2026-04-12-type-modeling-elegance-design.md"
```

---

## Task 7: Typed Components Phase A — JSON Polymorphism (Unit 6)

**Files:**
- Modify: `agent_sdks/dotnet/src/A2Ui.Core/Messages/A2UiComponent.cs`
- Create: `agent_sdks/dotnet/src/A2Ui.Core/Messages/TypedComponents.cs`
- Create: `agent_sdks/dotnet/tests/A2Ui.Core.Tests/A2Ui/TypedComponentDeserializationTests.cs`

- [ ] **Step 1: Verify [JsonPolymorphic] with required discriminator works**

Create a minimal spike test in `TypedComponentDeserializationTests.cs`:

```csharp
using System.Text.Json;
using A2Ui.Core.Messages;

namespace A2Ui.Core.Tests;

public sealed class TypedComponentDeserializationTests
{
    private static readonly JsonSerializerOptions s_opts = new(JsonSerializerDefaults.Web);

    [Fact]
    public void TextComponent_Deserialize_ReturnsCorrectSubtype()
    {
        var json = """{"id":"t1","component":"Text","text":"hello"}""";

        var component = JsonSerializer.Deserialize<A2UiComponent>(json, s_opts);

        Assert.IsType<TextComponent>(component);
        Assert.Equal("t1", component!.Id);
        Assert.Equal("Text", component.Component);
    }
}
```

Run this test FIRST to validate the `[JsonPolymorphic]` + `required` discriminator combination works. If it fails, the `Component` property must change to `{ get; init; }` with subtype defaults.

- [ ] **Step 2: Make A2UiComponent abstract and add polymorphic attributes**

In `agent_sdks/dotnet/src/A2Ui.Core/Messages/A2UiComponent.cs`, change:

```csharp
// Before:
public sealed record A2UiComponent

// After:
[JsonPolymorphic(TypeDiscriminatorPropertyName = "component")]
[JsonDerivedType(typeof(TextComponent), "Text")]
[JsonDerivedType(typeof(ButtonComponent), "Button")]
[JsonDerivedType(typeof(ColumnComponent), "Column")]
[JsonDerivedType(typeof(RowComponent), "Row")]
[JsonDerivedType(typeof(CardComponent), "Card")]
[JsonDerivedType(typeof(IconComponent), "Icon")]
[JsonDerivedType(typeof(DividerComponent), "Divider")]
[JsonDerivedType(typeof(ImageComponent), "Image")]
[JsonDerivedType(typeof(VideoComponent), "Video")]
[JsonDerivedType(typeof(AudioPlayerComponent), "AudioPlayer")]
[JsonDerivedType(typeof(ListComponent), "List")]
[JsonDerivedType(typeof(TabsComponent), "Tabs")]
[JsonDerivedType(typeof(ModalComponent), "Modal")]
[JsonDerivedType(typeof(TextFieldComponent), "TextField")]
[JsonDerivedType(typeof(DateTimeInputComponent), "DateTimeInput")]
[JsonDerivedType(typeof(ChoicePickerComponent), "ChoicePicker")]
[JsonDerivedType(typeof(CheckBoxComponent), "CheckBox")]
[JsonDerivedType(typeof(SliderComponent), "Slider")]
[JsonDerivedType(typeof(TableComponent), "Table")]
[JsonDerivedType(typeof(SurfaceComponent), "Surface")]
public abstract record A2UiComponent
```

**Important**: When `A2UiComponent` becomes `abstract`, existing tests that do `new A2UiComponent { ... }` will fail to compile. If `[JsonPolymorphic]` requires the `Component` property to NOT be `required`, change it to `{ get; init; }` with a default. To fix the compilation, either:
- If `A2UiComponent` can no longer be instantiated directly, all `new A2UiComponent { Component = "Text", ... }` in tests must change to `new TextComponent { ... }`.
- OR keep `A2UiComponent` non-abstract temporarily and add an `[JsonDerivedType]` fallback.

The preferred approach: make `A2UiComponent` abstract AND update all test construction sites to use the typed subtypes. This means Task 7 is a larger step because ALL existing tests that construct `A2UiComponent` must be updated to use the specific subtype.

**Fallback if this is too large**: Make `A2UiComponent` non-sealed (remove `sealed`) but not abstract, and add the polymorphic attributes. Tests continue to construct `A2UiComponent` directly. Defer making it abstract to Phase B.

- [ ] **Step 3: Create TypedComponents.cs with empty subtypes**

Create `agent_sdks/dotnet/src/A2Ui.Core/Messages/TypedComponents.cs`:

```csharp
namespace A2Ui.Core.Messages;

/// <summary>Text display component.</summary>
public sealed record TextComponent : A2UiComponent;

/// <summary>Clickable button component.</summary>
public sealed record ButtonComponent : A2UiComponent;

/// <summary>Vertical layout container.</summary>
public sealed record ColumnComponent : A2UiComponent;

/// <summary>Horizontal layout container.</summary>
public sealed record RowComponent : A2UiComponent;

/// <summary>Card container with rounded borders.</summary>
public sealed record CardComponent : A2UiComponent;

/// <summary>Icon display component.</summary>
public sealed record IconComponent : A2UiComponent;

/// <summary>Visual divider (horizontal or vertical).</summary>
public sealed record DividerComponent : A2UiComponent;

/// <summary>Image display component.</summary>
public sealed record ImageComponent : A2UiComponent;

/// <summary>Video placeholder component.</summary>
public sealed record VideoComponent : A2UiComponent;

/// <summary>Audio player placeholder component.</summary>
public sealed record AudioPlayerComponent : A2UiComponent;

/// <summary>Scrollable list container.</summary>
public sealed record ListComponent : A2UiComponent;

/// <summary>Tabbed container component.</summary>
public sealed record TabsComponent : A2UiComponent;

/// <summary>Modal dialog component.</summary>
public sealed record ModalComponent : A2UiComponent;

/// <summary>Text input field component.</summary>
public sealed record TextFieldComponent : A2UiComponent;

/// <summary>Date/time input component.</summary>
public sealed record DateTimeInputComponent : A2UiComponent;

/// <summary>Choice picker (dropdown/radio/checkbox list).</summary>
public sealed record ChoicePickerComponent : A2UiComponent;

/// <summary>Checkbox input component.</summary>
public sealed record CheckBoxComponent : A2UiComponent;

/// <summary>Slider input component.</summary>
public sealed record SliderComponent : A2UiComponent;

/// <summary>Data table component (extension).</summary>
public sealed record TableComponent : A2UiComponent;

/// <summary>Root surface container (extension).</summary>
public sealed record SurfaceComponent : A2UiComponent;
```

- [ ] **Step 4: Add deserialization tests for all 20 types**

Complete `TypedComponentDeserializationTests.cs` with tests for all types. Pattern for each:

```csharp
[Fact]
public void ButtonComponent_Deserialize_ReturnsCorrectSubtype()
{
    var json = """{"id":"b1","component":"Button","text":"Click me"}""";
    var component = JsonSerializer.Deserialize<A2UiComponent>(json, s_opts);
    Assert.IsType<ButtonComponent>(component);
}

// ... repeat for all 20 types
```

- [ ] **Step 5: Update ALL existing test construction sites**

This is the largest mechanical step. Every `new A2UiComponent { Component = "Text", ... }` must become `new TextComponent { ... }`. Search all test files for `new A2UiComponent` and replace based on the `Component = "..."` value.

The `Component` property should still be settable on the subtype (it's inherited from the base). But if it's `required`, the subtype must provide a default. If `[JsonPolymorphic]` handles the discriminator transparently, the `Component` property on the base can become `{ get; init; }` without `required`, and each subtype need not set it — STJ handles it during deserialization.

For test construction, if `Component` is no longer `required`:
```csharp
// Before:
new A2UiComponent { Id = "t1", Component = "Text", Text = DynamicValue.FromString("hello") }

// After:
new TextComponent { Id = "t1", Text = DynamicValue.FromString("hello") }
```

If `Component` remains `required`:
```csharp
new TextComponent { Id = "t1", Component = "Text", Text = DynamicValue.FromString("hello") }
```

- [ ] **Step 6: Build and run all tests**

Run: `dotnet build A2Ui.slnx --configuration Release && dotnet test A2Ui.slnx --configuration Release --no-build`

Expected: All tests pass including 20 new deserialization tests.

- [ ] **Step 7: Commit**

```bash
git add agent_sdks/dotnet/src/A2Ui.Core/Messages/A2UiComponent.cs agent_sdks/dotnet/src/A2Ui.Core/Messages/TypedComponents.cs agent_sdks/dotnet/tests/
git commit -m "feat(dotnet-sdk): add typed per-component records with JSON polymorphism

A2UiComponent is now abstract with [JsonPolymorphic] and 20 concrete
subtypes (TextComponent, ButtonComponent, etc.). All properties remain
on the abstract base for now — subtypes are empty. Wire JSON
deserializes directly to the correct subtype. All tests updated to
construct typed components.

Refs: docs/superpowers/specs/2026-04-12-type-modeling-elegance-design.md"
```

---

## Task 8: Typed Components Phase B — Layout Property Migration (Unit 7a)

**Files:**
- Modify: `agent_sdks/dotnet/src/A2Ui.Core/Messages/A2UiComponent.cs`
- Modify: `agent_sdks/dotnet/src/A2Ui.Core/Messages/TypedComponents.cs`
- Modify: `renderers/avalonia/src/A2Ui.Avalonia/Catalog/Entries/LayoutCatalogEntries.cs`
- Modify: `renderers/avalonia/src/A2Ui.Avalonia/Catalog/Entries/InteractiveCatalogEntries.cs`
- Modify: relevant test files

Properties to migrate: `Justify`, `Align`, `Direction`, `Gap`

- [ ] **Step 1: Move layout properties from base to subtypes**

In `A2UiComponent.cs`, remove the layout properties (lines 66-73):
```csharp
// REMOVE from A2UiComponent:
[JsonPropertyName("justify")]
public string? Justify { get; init; }

[JsonPropertyName("align")]
public string? Align { get; init; }

[JsonPropertyName("direction")]
public string? Direction { get; init; }
```

In `TypedComponents.cs`, add them to the layout subtypes:

```csharp
public sealed record ColumnComponent : A2UiComponent
{
    [JsonPropertyName("justify")]
    public string? Justify { get; init; }

    [JsonPropertyName("align")]
    public string? Align { get; init; }

    [JsonPropertyName("direction")]
    public string? Direction { get; init; }
}

public sealed record RowComponent : A2UiComponent
{
    [JsonPropertyName("justify")]
    public string? Justify { get; init; }

    [JsonPropertyName("align")]
    public string? Align { get; init; }

    [JsonPropertyName("direction")]
    public string? Direction { get; init; }
}

public sealed record ListComponent : A2UiComponent
{
    [JsonPropertyName("direction")]
    public string? Direction { get; init; }
}
```

Note: `CardComponent` does not use `Justify`/`Align`/`Direction` — it stays empty.

- [ ] **Step 2: Update catalog entries to downcast**

In `LayoutCatalogEntries.cs`, add downcasts at the start of Create methods:

```csharp
// ColumnCatalogEntry.Create:
public Control Create(A2UiComponent component, DataModel dataModel, IRenderContext context)
{
    var col = (ColumnComponent)component;
    var children = context.RenderChildren(component.Id).ToList();
    double[]? weights = LayoutHelper.CollectWeights(component, context);
    return LayoutHelper.BuildLayout(Orientation.Vertical, children, col, weights);
}
```

Update `LayoutHelper.BuildLayout` to accept the component and read `Justify`/`Align` from the correct type. If `BuildLayout` currently takes `A2UiComponent`, it needs a generic parameter or the caller must pass the values explicitly.

In `InteractiveCatalogEntries.cs`, update `ListCatalogEntry.Create`:

```csharp
var list = (ListComponent)component;
var orientation = list.Direction == "horizontal" ? Orientation.Horizontal : Orientation.Vertical;
```

- [ ] **Step 3: Fix test compilation errors**

Update all test files that set `Justify`, `Align`, or `Direction` on component construction to use the correct typed record.

- [ ] **Step 4: Build and run all tests**

Run: `dotnet build A2Ui.slnx --configuration Release && dotnet test A2Ui.slnx --configuration Release --no-build`

Expected: All tests pass.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "refactor(dotnet-sdk): migrate layout properties to typed component records

Justify, Align, Direction moved from A2UiComponent base to
ColumnComponent, RowComponent, and ListComponent. Catalog entries
downcast to the typed component. CardComponent remains property-free.

Refs: docs/superpowers/specs/2026-04-12-type-modeling-elegance-design.md"
```

---

## Task 9: Typed Components Phase B — Display Property Migration (Unit 7b)

**Files:**
- Modify: `agent_sdks/dotnet/src/A2Ui.Core/Messages/A2UiComponent.cs`
- Modify: `agent_sdks/dotnet/src/A2Ui.Core/Messages/TypedComponents.cs`
- Modify: relevant catalog entries and test files

Properties to migrate: `Variant` (to TextComponent, ButtonComponent), `Url` (to Image, Video, AudioPlayer), `Name` (to Icon), `Fit` (to Image), `Axis` (to Divider), `Description` (to Text), `Alt` (if present, to Image).

`Text` and `Label` stay on the base per design decision.

- [ ] **Step 1: Move display properties from base to subtypes**

Remove from `A2UiComponent.cs`: `Variant`, `Url`, `Name`, `Fit`, `Axis`, `Description`.

Add to each subtype in `TypedComponents.cs`:

```csharp
public sealed record TextComponent : A2UiComponent
{
    [JsonPropertyName("variant")]
    public string? Variant { get; init; }

    [JsonPropertyName("description")]
    public DynamicValue? Description { get; init; }
}

public sealed record IconComponent : A2UiComponent
{
    [JsonPropertyName("name")]
    public DynamicValue? Name { get; init; }
}

public sealed record DividerComponent : A2UiComponent
{
    [JsonPropertyName("axis")]
    public string? Axis { get; init; }
}

public sealed record ImageComponent : A2UiComponent
{
    [JsonPropertyName("url")]
    public DynamicValue? Url { get; init; }

    [JsonPropertyName("fit")]
    public string? Fit { get; init; }
}

public sealed record VideoComponent : A2UiComponent
{
    [JsonPropertyName("url")]
    public DynamicValue? Url { get; init; }
}

public sealed record AudioPlayerComponent : A2UiComponent
{
    [JsonPropertyName("url")]
    public DynamicValue? Url { get; init; }
}
```

- [ ] **Step 2: Update catalog entries to downcast**

Each display catalog entry adds a downcast at its entry point (e.g., `var text = (TextComponent)component;`).

- [ ] **Step 3: Fix test compilation, build, and run all tests**

Run: `dotnet build A2Ui.slnx --configuration Release && dotnet test A2Ui.slnx --configuration Release --no-build`

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "refactor(dotnet-sdk): migrate display properties to typed component records

Variant, Url, Name, Fit, Axis, Description moved from A2UiComponent
base to TextComponent, IconComponent, DividerComponent, ImageComponent,
VideoComponent, AudioPlayerComponent. Text and Label remain on base.

Refs: docs/superpowers/specs/2026-04-12-type-modeling-elegance-design.md"
```

---

## Task 10: Typed Components Phase B — Input Property Migration (Unit 7c)

**Files:**
- Modify: `agent_sdks/dotnet/src/A2Ui.Core/Messages/A2UiComponent.cs`
- Modify: `agent_sdks/dotnet/src/A2Ui.Core/Messages/TypedComponents.cs`
- Modify: `renderers/avalonia/src/A2Ui.Avalonia/Catalog/Entries/InputCatalogEntries.cs`
- Modify: relevant test files

Properties to migrate: `Value` (to all 5 input types), `Placeholder`, `Rows`, `ValidationRegexp` (to TextField), `Min`, `Max` (to Slider, DateTimeInput), `Options`, `DisplayStyle`, `Filterable` (to ChoicePicker), `EnableDate`, `EnableTime` (to DateTimeInput).

- [ ] **Step 1: Move input properties from base to subtypes**

Follow the same pattern as Tasks 8-9. Remove from base, add to each input subtype with only its relevant properties.

```csharp
public sealed record TextFieldComponent : A2UiComponent
{
    [JsonPropertyName("value")]
    public DynamicValue? Value { get; init; }

    [JsonPropertyName("validationRegexp")]
    public string? ValidationRegexp { get; init; }

    [JsonPropertyName("rows")]
    public DynamicValue? Rows { get; init; }
}

public sealed record SliderComponent : A2UiComponent
{
    [JsonPropertyName("value")]
    public DynamicValue? Value { get; init; }

    [JsonPropertyName("min")]
    public DynamicValue? Min { get; init; }

    [JsonPropertyName("max")]
    public DynamicValue? Max { get; init; }
}

public sealed record DateTimeInputComponent : A2UiComponent
{
    [JsonPropertyName("value")]
    public DynamicValue? Value { get; init; }

    [JsonPropertyName("min")]
    public DynamicValue? Min { get; init; }

    [JsonPropertyName("max")]
    public DynamicValue? Max { get; init; }

    [JsonPropertyName("enableDate")]
    public bool? EnableDate { get; init; }

    [JsonPropertyName("enableTime")]
    public bool? EnableTime { get; init; }
}

public sealed record ChoicePickerComponent : A2UiComponent
{
    [JsonPropertyName("value")]
    public DynamicValue? Value { get; init; }

    [JsonPropertyName("options")]
    public ChoiceOption[]? Options { get; init; }

    [JsonPropertyName("displayStyle")]
    public string? DisplayStyle { get; init; }

    [JsonPropertyName("filterable")]
    public bool? Filterable { get; init; }
}

public sealed record CheckBoxComponent : A2UiComponent
{
    [JsonPropertyName("value")]
    public DynamicValue? Value { get; init; }
}
```

- [ ] **Step 2: Update InputCatalogEntries to downcast**

Each input catalog entry adds a downcast. The `GetBindingPath()` extension method (from Task 5) works on `DynamicValue?` so it still works after the property moves.

- [ ] **Step 3: Fix test compilation, build, and run all tests**

Run: `dotnet build A2Ui.slnx --configuration Release && dotnet test A2Ui.slnx --configuration Release --no-build`

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "refactor(dotnet-sdk): migrate input properties to typed component records

Value, Placeholder, Min, Max, Options, DisplayStyle, Filterable,
EnableDate, EnableTime, ValidationRegexp, Rows moved from A2UiComponent
base to TextFieldComponent, SliderComponent, DateTimeInputComponent,
ChoicePickerComponent, CheckBoxComponent.

Refs: docs/superpowers/specs/2026-04-12-type-modeling-elegance-design.md"
```

---

## Task 11: Typed Components Phase B — Interactive Property Migration (Unit 7d)

**Files:**
- Modify: `agent_sdks/dotnet/src/A2Ui.Core/Messages/A2UiComponent.cs`
- Modify: `agent_sdks/dotnet/src/A2Ui.Core/Messages/TypedComponents.cs`
- Modify: relevant catalog entries and test files

Properties to migrate: `Action` (to Button), `Variant` (to Button — already on Text from Unit 7b), `Tabs` (to TabsComponent), `Trigger`, `Content` (to ModalComponent), `Columns` (to TableComponent).

- [ ] **Step 1: Move interactive properties from base to subtypes**

```csharp
public sealed record ButtonComponent : A2UiComponent
{
    [JsonPropertyName("variant")]
    public string? Variant { get; init; }

    [JsonPropertyName("action")]
    public ComponentAction? Action { get; init; }
}

public sealed record TabsComponent : A2UiComponent
{
    [JsonPropertyName("tabs")]
    public TabDefinition[]? Tabs { get; init; }
}

public sealed record ModalComponent : A2UiComponent
{
    [JsonPropertyName("trigger")]
    public string? Trigger { get; init; }

    [JsonPropertyName("content")]
    public string? Content { get; init; }
}

public sealed record TableComponent : A2UiComponent
{
    [JsonPropertyName("columns")]
    public TableColumn[]? Columns { get; init; }

    [JsonPropertyName("rows")]
    public DynamicValue? Rows { get; init; }
}
```

`SurfaceComponent` has no unique properties — it stays empty.

- [ ] **Step 2: Remove ALL migrated properties from A2UiComponent base**

After this unit, `A2UiComponent` should only contain the universally-shared properties:
`Id`, `Component`, `Parent`, `Child`, `Children`, `Accessibility`, `Weight`, `Checks`, `Text`, `Label`, `Placeholder`, `ExtensionData`.

Verify by reviewing the base record — only properties listed in the "Properties remaining on base" table from the spec should remain.

- [ ] **Step 3: Update catalog entries, fix tests, build and run**

Run: `dotnet build A2Ui.slnx --configuration Release && dotnet test A2Ui.slnx --configuration Release --no-build`

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "refactor(dotnet-sdk): migrate interactive properties to typed component records

Action, Variant (Button), Tabs, Trigger, Content (Modal), Columns,
Rows (Table) moved from A2UiComponent base to ButtonComponent,
TabsComponent, ModalComponent, TableComponent. A2UiComponent base
now contains only universally-shared properties.

Refs: docs/superpowers/specs/2026-04-12-type-modeling-elegance-design.md"
```

---

## Task 12: XML Documentation (Unit 8)

**Files:**
- Modify: `agent_sdks/dotnet/src/A2Ui.Core/A2Ui.Core.csproj`
- Modify: `agent_sdks/dotnet/src/AgUi.Protocol/AgUi.Protocol.csproj`
- Modify: `renderers/avalonia/src/A2Ui.Avalonia/A2Ui.Avalonia.csproj`
- Modify: all test project `.csproj` files
- Modify: ~25 source files for doc comments

- [ ] **Step 1: Enable GenerateDocumentationFile in library projects**

Add to each library `.csproj`:

```xml
<GenerateDocumentationFile>true</GenerateDocumentationFile>
```

Add to each test project `.csproj`:

```xml
<NoWarn>$(NoWarn);CS1591</NoWarn>
```

- [ ] **Step 2: Build to identify all CS1591 warnings**

Run: `dotnet build A2Ui.slnx --configuration Release 2>&1 | grep CS1591 | wc -l`

This shows how many public members need docs. With `TreatWarningsAsErrors`, these become errors.

- [ ] **Step 3: Add XML docs to A2Ui.Core (NuGet-grade)**

Add `<summary>`, `<param>`, `<returns>`, `<typeparam>` to every public type, method, property, and parameter in:
- `DynamicValue.cs` (hierarchy + all subtypes)
- `A2UiComponent.cs` (base + all properties)
- `TypedComponents.cs` (all 20 subtypes)
- `DataModel.cs`
- `SurfaceManager.cs` (including `Surface` class)
- `A2UiMessage.cs`
- `ChildList.cs`
- `OneOf.cs`
- All supporting types (`CheckRule`, `ChoiceOption`, `TabDefinition`, `ComponentAction`, `ActionEvent`, `TableColumn`, `AccessibilityAttributes`, `FunctionCallValue`)

- [ ] **Step 4: Add XML docs to AgUi.Protocol (types + methods)**

Add `<summary>` to all public classes/records/enums and public methods in:
- `BaseEvent.cs` (already has it)
- All 28 event type records
- `EventType.cs` (already has it)
- `SseEventParser.cs`
- `ToolCallArgsAccumulator.cs`
- `RunAgentInput.cs`

- [ ] **Step 5: Add XML docs to A2Ui.Avalonia (types + methods)**

Add `<summary>` to all public interfaces, classes, and public methods in:
- `ICatalogEntry.cs` and `IRenderContext.cs`
- `CatalogRegistry.cs`
- `A2UiRenderer.cs`
- `FunctionRegistry.cs` and `IFunctionRegistry.cs`
- `AgentEventBridge.cs`
- All catalog entry classes

- [ ] **Step 6: Build and verify zero CS1591 warnings**

Run: `dotnet build A2Ui.slnx --configuration Release`

Expected: Zero errors, zero warnings.

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "docs(dotnet): add XML documentation to public API surface

NuGet-grade docs for A2Ui.Core (all public types, methods, properties,
parameters). Types+methods docs for AgUi.Protocol and A2Ui.Avalonia.
GenerateDocumentationFile enabled; CS1591 suppressed in test projects.

Refs: docs/superpowers/specs/2026-04-12-type-modeling-elegance-design.md"
```
