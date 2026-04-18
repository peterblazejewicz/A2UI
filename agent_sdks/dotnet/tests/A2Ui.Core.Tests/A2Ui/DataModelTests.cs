using System;
using System.Collections.Generic;
using System.Text.Json;
using A2Ui.Core.Components;
using A2Ui.Core.Messages;
using A2Ui.Core.Surfaces;

namespace A2Ui.Core.Tests.A2Ui;

public sealed class DataModelTests
{
    [Fact]
    public void Apply_UpdateDataModel_SetsNestedPath()
    {
        var dm = new DataModel();
        dm.Apply(
            new UpdateDataModel
            {
                SurfaceId = "main",
                Path = "/reservation/date",
                Value = JsonSerializer.SerializeToElement("2025-12-15"),
            }
        );

        Assert.Equal("2025-12-15", dm.Resolve(DynamicValue.FromPath("/reservation/date")));
    }

    [Fact]
    public void Apply_NullPath_ReplacesEntireModel()
    {
        var dm = new DataModel();
        dm.Apply(
            new UpdateDataModel
            {
                SurfaceId = "main",
                Path = "/name",
                Value = JsonSerializer.SerializeToElement("old"),
            }
        );

        dm.Apply(
            new UpdateDataModel
            {
                SurfaceId = "main",
                Path = null,
                Value = JsonSerializer.SerializeToElement(new { name = "replaced" }),
            }
        );

        Assert.Equal("replaced", dm.Resolve(DynamicValue.FromPath("/name")));
    }

    [Fact]
    public void Apply_RootPath_ReplacesEntireModel()
    {
        var dm = new DataModel();
        dm.Apply(
            new UpdateDataModel
            {
                SurfaceId = "main",
                Path = "/",
                Value = JsonSerializer.SerializeToElement(new { key = "val" }),
            }
        );

        Assert.Equal("val", dm.Resolve(DynamicValue.FromPath("/key")));
    }

    [Fact]
    public void Apply_NullValue_DeletesAtPath()
    {
        var dm = new DataModel();
        dm.Apply(
            new UpdateDataModel
            {
                SurfaceId = "main",
                Path = "/toDelete",
                Value = JsonSerializer.SerializeToElement("exists"),
            }
        );
        Assert.Equal("exists", dm.Resolve(DynamicValue.FromPath("/toDelete")));

        dm.Apply(
            new UpdateDataModel
            {
                SurfaceId = "main",
                Path = "/toDelete",
                Value = null,
            }
        );

        Assert.Null(dm.Resolve(DynamicValue.FromPath("/toDelete")));
    }

    [Fact]
    public void Apply_OverwriteExistingKey_ReplacesValue()
    {
        var dm = new DataModel();
        dm.Apply(
            new UpdateDataModel
            {
                SurfaceId = "s",
                Path = "/count",
                Value = JsonSerializer.SerializeToElement(1),
            }
        );
        dm.Apply(
            new UpdateDataModel
            {
                SurfaceId = "s",
                Path = "/count",
                Value = JsonSerializer.SerializeToElement(42),
            }
        );

        Assert.Equal("42", dm.Resolve(DynamicValue.FromPath("/count")));
    }

    [Fact]
    public void Resolve_LiteralValue_ReturnsLiteral()
    {
        var dm = new DataModel();
        Assert.Equal("Hello", dm.Resolve(DynamicValue.FromString("Hello")));
    }

    [Fact]
    public void Resolve_UnknownPath_ReturnsNull()
    {
        var dm = new DataModel();
        Assert.Null(dm.Resolve(DynamicValue.FromPath("/nonexistent/path")));
    }

    [Fact]
    public void Resolve_Null_ReturnsNull()
    {
        var dm = new DataModel();
        Assert.Null(dm.Resolve(null));
    }

    [Fact]
    public void Resolve_NumberLiteral_ReturnsStringRepresentation()
    {
        var dm = new DataModel();
        Assert.Equal("42.5", dm.Resolve(DynamicValue.FromNumber(42.5)));
    }

    [Fact]
    public void Resolve_BoolLiteral_ReturnsTrueOrFalse()
    {
        var dm = new DataModel();
        Assert.Equal("true", dm.Resolve(DynamicValue.FromBool(true)));
        Assert.Equal("false", dm.Resolve(DynamicValue.FromBool(false)));
    }

    [Fact]
    public void Resolve_NumericNodeInModel_ReturnsString()
    {
        var dm = new DataModel();
        dm.Apply(
            new UpdateDataModel
            {
                SurfaceId = "s",
                Path = "/count",
                Value = JsonSerializer.SerializeToElement(99),
            }
        );

        Assert.Equal("99", dm.Resolve(DynamicValue.FromPath("/count")));
    }

    [Fact]
    public void Resolve_BoolNodeInModel_ReturnsString()
    {
        var dm = new DataModel();
        dm.Apply(
            new UpdateDataModel
            {
                SurfaceId = "s",
                Path = "/active",
                Value = JsonSerializer.SerializeToElement(true),
            }
        );

        Assert.Equal("true", dm.Resolve(DynamicValue.FromPath("/active")));
    }

    [Fact]
    public void Resolve_ArrayIndex_ReturnsElement()
    {
        var dm = new DataModel();
        dm.Apply(
            new UpdateDataModel
            {
                SurfaceId = "s",
                Path = "/items",
                Value = JsonSerializer.SerializeToElement(new[] { "alpha", "beta", "gamma" }),
            }
        );

        Assert.Equal("beta", dm.Resolve(DynamicValue.FromPath("/items/1")));
    }

    [Fact]
    public void SetSnapshot_ReplacesEntireModel()
    {
        var dm = new DataModel();
        dm.Apply(
            new UpdateDataModel
            {
                SurfaceId = "s",
                Path = "/old",
                Value = JsonSerializer.SerializeToElement("data"),
            }
        );

        dm.SetSnapshot(JsonSerializer.SerializeToElement(new { fresh = "start" }));

        Assert.Null(dm.Resolve(DynamicValue.FromPath("/old")));
        Assert.Equal("start", dm.Resolve(DynamicValue.FromPath("/fresh")));
    }

    [Fact]
    public void Resolve_FunctionCall_ReturnsNull()
    {
        var dm = new DataModel();
        var fc = new DynamicValue.FunctionValue(new FunctionCallValue { Call = "formatDate" });

        Assert.Null(dm.Resolve(fc));
    }

    [Fact]
    public void Apply_ArrayIndexPath_CreatesArrayAndSetsElement()
    {
        var dm = new DataModel();
        dm.Apply(
            new UpdateDataModel
            {
                SurfaceId = "s",
                Path = "/items/0/name",
                Value = JsonSerializer.SerializeToElement("Alice"),
            }
        );

        Assert.Equal("Alice", dm.Resolve(DynamicValue.FromPath("/items/0/name")));
    }

    [Fact]
    public void Apply_ArrayIndexPath_MultipleElements()
    {
        var dm = new DataModel();
        dm.Apply(
            new UpdateDataModel
            {
                SurfaceId = "s",
                Path = "/items/0",
                Value = JsonSerializer.SerializeToElement(new { name = "Alice" }),
            }
        );
        dm.Apply(
            new UpdateDataModel
            {
                SurfaceId = "s",
                Path = "/items/1",
                Value = JsonSerializer.SerializeToElement(new { name = "Bob" }),
            }
        );

        Assert.Equal("Alice", dm.Resolve(DynamicValue.FromPath("/items/0/name")));
        Assert.Equal("Bob", dm.Resolve(DynamicValue.FromPath("/items/1/name")));
    }

    [Fact]
    public void Apply_ArrayIndexPath_PreservesExistingArray()
    {
        var dm = new DataModel();
        dm.SetSnapshot(JsonSerializer.SerializeToElement(new { items = new[] { new { name = "original" } } }));

        dm.Apply(
            new UpdateDataModel
            {
                SurfaceId = "s",
                Path = "/items/0/name",
                Value = JsonSerializer.SerializeToElement("updated"),
            }
        );

        Assert.Equal("updated", dm.Resolve(DynamicValue.FromPath("/items/0/name")));
    }

    [Fact]
    public void Apply_DeleteAtArrayPath_NullifiesElement()
    {
        var dm = new DataModel();
        dm.SetSnapshot(JsonSerializer.SerializeToElement(new { items = new[] { "alpha", "beta", "gamma" } }));

        dm.Apply(
            new UpdateDataModel
            {
                SurfaceId = "s",
                Path = "/items/1",
                Value = null,
            }
        );

        Assert.Null(dm.Resolve(DynamicValue.FromPath("/items/1")));
        Assert.Equal("gamma", dm.Resolve(DynamicValue.FromPath("/items/2")));
    }

    [Fact]
    public void Apply_PreservesExistingObjectWhenNextSegmentIsNumeric()
    {
        var dm = new DataModel();
        dm.SetSnapshot(
            JsonSerializer.SerializeToElement(new { totals = new Dictionary<string, int> { ["2025"] = 100 } })
        );

        dm.Apply(
            new UpdateDataModel
            {
                SurfaceId = "s",
                Path = "/totals/2026",
                Value = JsonSerializer.SerializeToElement(200),
            }
        );

        // Should preserve the object (not create an array) since "totals" already exists as object
        Assert.Equal("100", dm.Resolve(DynamicValue.FromPath("/totals/2025")));
        Assert.Equal("200", dm.Resolve(DynamicValue.FromPath("/totals/2026")));
    }

    [Fact]
    public void GetArrayLength_ReturnsCount_WhenPathIsArray()
    {
        var dm = new DataModel();
        dm.Apply(
            new UpdateDataModel
            {
                SurfaceId = "s",
                Path = "/",
                Value = JsonSerializer.SerializeToElement(new { items = new[] { 1, 2, 3 } }),
            }
        );
        Assert.Equal(3, dm.GetArrayLength("/items"));
    }

    [Fact]
    public void GetArrayLength_ReturnsNegativeOne_WhenPathIsObject()
    {
        var dm = new DataModel();
        dm.Apply(
            new UpdateDataModel
            {
                SurfaceId = "s",
                Path = "/",
                Value = JsonSerializer.SerializeToElement(new { data = new { key = "val" } }),
            }
        );
        Assert.Equal(-1, dm.GetArrayLength("/data"));
    }

    [Fact]
    public void GetArrayLength_ReturnsNegativeOne_WhenPathDoesNotExist()
    {
        var dm = new DataModel();
        Assert.Equal(-1, dm.GetArrayLength("/nonexistent"));
    }

    [Fact]
    public void GetArrayLength_ReturnsZero_WhenArrayIsEmpty()
    {
        var dm = new DataModel();
        dm.Apply(
            new UpdateDataModel
            {
                SurfaceId = "s",
                Path = "/",
                Value = JsonSerializer.SerializeToElement(new { items = Array.Empty<int>() }),
            }
        );
        Assert.Equal(0, dm.GetArrayLength("/items"));
    }

    [Fact]
    public void ToJson_EmptyModel_ReturnsEmptyObject()
    {
        var dm = new DataModel();
        Assert.Equal("{}", dm.ToJson());
    }

    [Fact]
    public void ToJson_AfterApply_ReflectsState()
    {
        var dm = new DataModel();
        dm.Apply(
            new UpdateDataModel
            {
                SurfaceId = "s",
                Path = "/name",
                Value = JsonSerializer.SerializeToElement("Alice"),
            }
        );

        var json = dm.ToJson();
        Assert.Contains("\"name\"", json);
        Assert.Contains("\"Alice\"", json);
    }

    [Fact]
    public void ToJson_Indented_ProducesFormattedOutput()
    {
        var dm = new DataModel();
        dm.Apply(
            new UpdateDataModel
            {
                SurfaceId = "s",
                Path = "/key",
                Value = JsonSerializer.SerializeToElement("val"),
            }
        );

        var json = dm.ToJson(indented: true);
        Assert.Contains("\n", json);
        Assert.Contains("  ", json);
    }

    // --- RFC 6901 JSON Pointer escaping tests ---

    [Fact]
    public void Apply_PathWithTilde1Escape_ResolvesSlashInKey()
    {
        var dm = new DataModel();
        // ~1 should unescape to / in the key name
        dm.Apply(
            new UpdateDataModel
            {
                SurfaceId = "s",
                Path = "/a~1b",
                Value = JsonSerializer.SerializeToElement("slash-key"),
            }
        );

        Assert.Equal("slash-key", dm.Resolve(DynamicValue.FromPath("/a~1b")));
    }

    [Fact]
    public void Apply_PathWithTilde0Escape_ResolvesTildeInKey()
    {
        var dm = new DataModel();
        // ~0 should unescape to ~ in the key name
        dm.Apply(
            new UpdateDataModel
            {
                SurfaceId = "s",
                Path = "/a~0b",
                Value = JsonSerializer.SerializeToElement("tilde-key"),
            }
        );

        Assert.Equal("tilde-key", dm.Resolve(DynamicValue.FromPath("/a~0b")));
    }

    [Fact]
    public void Apply_PathWithBothEscapes_ResolvesCorrectly()
    {
        var dm = new DataModel();
        // ~01 should become ~1 (tilde then 1), not /
        dm.Apply(
            new UpdateDataModel
            {
                SurfaceId = "s",
                Path = "/~01",
                Value = JsonSerializer.SerializeToElement("both"),
            }
        );

        Assert.Equal("both", dm.Resolve(DynamicValue.FromPath("/~01")));
    }

    [Fact]
    public void Apply_DeletePathWithEscapedSegments_RemovesKey()
    {
        var dm = new DataModel();
        dm.Apply(
            new UpdateDataModel
            {
                SurfaceId = "s",
                Path = "/a~1b",
                Value = JsonSerializer.SerializeToElement("to-delete"),
            }
        );

        dm.Apply(
            new UpdateDataModel
            {
                SurfaceId = "s",
                Path = "/a~1b",
                Value = null,
            }
        );

        Assert.Null(dm.Resolve(DynamicValue.FromPath("/a~1b")));
    }

    // --- RFC 6901 strict escape validation tests ---

    [Fact]
    public void Apply_PathWithInvalidEscape_ThrowsFormatException()
    {
        var dm = new DataModel();

        var ex = Assert.Throws<FormatException>(() =>
            dm.Apply(
                new UpdateDataModel
                {
                    SurfaceId = "s",
                    Path = "/~2invalid",
                    Value = JsonSerializer.SerializeToElement("nope"),
                }
            )
        );

        Assert.Contains("~2", ex.Message);
    }

    [Fact]
    public void Apply_PathWithTrailingTilde_ThrowsFormatException()
    {
        var dm = new DataModel();

        var ex = Assert.Throws<FormatException>(() =>
            dm.Apply(
                new UpdateDataModel
                {
                    SurfaceId = "s",
                    Path = "/trailing~",
                    Value = JsonSerializer.SerializeToElement("nope"),
                }
            )
        );

        Assert.Contains("trailing '~'", ex.Message);
    }

    [Fact]
    public void Resolve_PathWithInvalidEscape_ThrowsFormatException()
    {
        var dm = new DataModel();

        Assert.Throws<FormatException>(() => dm.Resolve(DynamicValue.FromPath("/~a")));
    }

    [Fact]
    public void Apply_TraverseThroughScalar_ThrowsJsonException()
    {
        // Seed: /user is a string primitive.
        var dm = new DataModel();
        dm.Apply(
            new UpdateDataModel
            {
                SurfaceId = "s",
                Path = "/user",
                Value = JsonSerializer.SerializeToElement("Alice"),
            }
        );

        // Attempting to write /user/name must throw rather than silently replace
        // "Alice" with a new object — otherwise existing data is destroyed with no
        // diagnostic surface.
        var ex = Assert.Throws<JsonException>(() =>
            dm.Apply(
                new UpdateDataModel
                {
                    SurfaceId = "s",
                    Path = "/user/name",
                    Value = JsonSerializer.SerializeToElement("Bob"),
                }
            )
        );
        Assert.Contains("primitive", ex.Message);
        // Original scalar preserved (exception fired before any overwrite).
        Assert.Equal("Alice", dm.Resolve(DynamicValue.FromPath("/user")));
    }

    [Fact]
    public void SetSnapshot_NonObjectJsonElement_ThrowsJsonException()
    {
        var dm = new DataModel();
        // Seed some data that a silent wipe would erase.
        dm.Apply(
            new UpdateDataModel
            {
                SurfaceId = "s",
                Path = "/keep",
                Value = JsonSerializer.SerializeToElement("me"),
            }
        );

        // Arrays, numbers, strings, booleans, and null all fail fast.
        Assert.Throws<JsonException>(() => dm.SetSnapshot(JsonSerializer.SerializeToElement(new[] { 1, 2, 3 })));
        Assert.Throws<JsonException>(() => dm.SetSnapshot(JsonSerializer.SerializeToElement(42)));
        Assert.Throws<JsonException>(() => dm.SetSnapshot(JsonSerializer.SerializeToElement("bare-string")));

        // Previous state is preserved (exception fired before any mutation).
        Assert.Equal("me", dm.Resolve(DynamicValue.FromPath("/keep")));
    }

    [Fact]
    public void Apply_LeafWriteOnArrayWithNonNumericSegment_ThrowsJsonException()
    {
        // Seed: /items is a JsonArray.
        var dm = new DataModel();
        dm.Apply(
            new UpdateDataModel
            {
                SurfaceId = "s",
                Path = "/items",
                Value = JsonSerializer.SerializeToElement(new[] { "a", "b" }),
            }
        );

        // Writing /items/newKey with a non-numeric final segment must throw rather
        // than silently no-op (which previously let the agent believe the update
        // applied while the data model diverged).
        var ex = Assert.Throws<JsonException>(() =>
            dm.Apply(
                new UpdateDataModel
                {
                    SurfaceId = "s",
                    Path = "/items/newKey",
                    Value = JsonSerializer.SerializeToElement("c"),
                }
            )
        );
        Assert.Contains("newKey", ex.Message);
    }
}
