using System.Collections.Generic;
using System.Text.Json;
using A2Ui.Core;
using A2Ui.Core.Messages;
using FluentAssertions;

namespace A2Ui.Core.Tests;

public sealed class DataModelTests
{
    [Fact]
    public void Apply_UpdateDataModel_SetsNestedPath()
    {
        var dm = new DataModel();
        dm.Apply(new UpdateDataModel
        {
            SurfaceId = "main",
            Path = "/reservation/date",
            Value = JsonSerializer.SerializeToElement("2025-12-15"),
        });

        dm.Resolve(DynamicValue.FromPath("/reservation/date")).Should().Be("2025-12-15");
    }

    [Fact]
    public void Apply_NullPath_ReplacesEntireModel()
    {
        var dm = new DataModel();
        dm.Apply(new UpdateDataModel
        {
            SurfaceId = "main",
            Path = "/name",
            Value = JsonSerializer.SerializeToElement("old"),
        });

        dm.Apply(new UpdateDataModel
        {
            SurfaceId = "main",
            Path = null,
            Value = JsonSerializer.SerializeToElement(new { name = "replaced" }),
        });

        dm.Resolve(DynamicValue.FromPath("/name")).Should().Be("replaced");
    }

    [Fact]
    public void Apply_RootPath_ReplacesEntireModel()
    {
        var dm = new DataModel();
        dm.Apply(new UpdateDataModel
        {
            SurfaceId = "main",
            Path = "/",
            Value = JsonSerializer.SerializeToElement(new { key = "val" }),
        });

        dm.Resolve(DynamicValue.FromPath("/key")).Should().Be("val");
    }

    [Fact]
    public void Apply_NullValue_DeletesAtPath()
    {
        var dm = new DataModel();
        dm.Apply(new UpdateDataModel
        {
            SurfaceId = "main",
            Path = "/toDelete",
            Value = JsonSerializer.SerializeToElement("exists"),
        });
        dm.Resolve(DynamicValue.FromPath("/toDelete")).Should().Be("exists");

        dm.Apply(new UpdateDataModel
        {
            SurfaceId = "main",
            Path = "/toDelete",
            Value = null,
        });

        dm.Resolve(DynamicValue.FromPath("/toDelete")).Should().BeNull();
    }

    [Fact]
    public void Apply_OverwriteExistingKey_ReplacesValue()
    {
        var dm = new DataModel();
        dm.Apply(new UpdateDataModel
        {
            SurfaceId = "s",
            Path = "/count",
            Value = JsonSerializer.SerializeToElement(1),
        });
        dm.Apply(new UpdateDataModel
        {
            SurfaceId = "s",
            Path = "/count",
            Value = JsonSerializer.SerializeToElement(42),
        });

        dm.Resolve(DynamicValue.FromPath("/count")).Should().Be("42");
    }

    [Fact]
    public void Resolve_LiteralValue_ReturnsLiteral()
    {
        var dm = new DataModel();
        dm.Resolve(DynamicValue.FromString("Hello")).Should().Be("Hello");
    }

    [Fact]
    public void Resolve_UnknownPath_ReturnsNull()
    {
        var dm = new DataModel();
        dm.Resolve(DynamicValue.FromPath("/nonexistent/path")).Should().BeNull();
    }

    [Fact]
    public void Resolve_Null_ReturnsNull()
    {
        var dm = new DataModel();
        dm.Resolve(null).Should().BeNull();
    }

    [Fact]
    public void Resolve_NumberLiteral_ReturnsStringRepresentation()
    {
        var dm = new DataModel();
        dm.Resolve(DynamicValue.FromNumber(42.5)).Should().Be("42.5");
    }

    [Fact]
    public void Resolve_BoolLiteral_ReturnsTrueOrFalse()
    {
        var dm = new DataModel();
        dm.Resolve(DynamicValue.FromBool(true)).Should().Be("true");
        dm.Resolve(DynamicValue.FromBool(false)).Should().Be("false");
    }

    [Fact]
    public void Resolve_NumericNodeInModel_ReturnsString()
    {
        var dm = new DataModel();
        dm.Apply(new UpdateDataModel
        {
            SurfaceId = "s",
            Path = "/count",
            Value = JsonSerializer.SerializeToElement(99),
        });

        dm.Resolve(DynamicValue.FromPath("/count")).Should().Be("99");
    }

    [Fact]
    public void Resolve_BoolNodeInModel_ReturnsString()
    {
        var dm = new DataModel();
        dm.Apply(new UpdateDataModel
        {
            SurfaceId = "s",
            Path = "/active",
            Value = JsonSerializer.SerializeToElement(true),
        });

        dm.Resolve(DynamicValue.FromPath("/active")).Should().Be("true");
    }

    [Fact]
    public void Resolve_ArrayIndex_ReturnsElement()
    {
        var dm = new DataModel();
        dm.Apply(new UpdateDataModel
        {
            SurfaceId = "s",
            Path = "/items",
            Value = JsonSerializer.SerializeToElement(new[] { "alpha", "beta", "gamma" }),
        });

        dm.Resolve(DynamicValue.FromPath("/items/1")).Should().Be("beta");
    }

    [Fact]
    public void SetSnapshot_ReplacesEntireModel()
    {
        var dm = new DataModel();
        dm.Apply(new UpdateDataModel
        {
            SurfaceId = "s",
            Path = "/old",
            Value = JsonSerializer.SerializeToElement("data"),
        });

        dm.SetSnapshot(JsonSerializer.SerializeToElement(new { fresh = "start" }));

        dm.Resolve(DynamicValue.FromPath("/old")).Should().BeNull();
        dm.Resolve(DynamicValue.FromPath("/fresh")).Should().Be("start");
    }

    [Fact]
    public void Resolve_FunctionCall_ReturnsNull()
    {
        var dm = new DataModel();
        var fc = new DynamicValue
        {
            FunctionCall = new FunctionCallValue { Call = "formatDate" },
        };

        dm.Resolve(fc).Should().BeNull();
    }

    [Fact]
    public void Apply_ArrayIndexPath_CreatesArrayAndSetsElement()
    {
        var dm = new DataModel();
        dm.Apply(new UpdateDataModel
        {
            SurfaceId = "s",
            Path = "/items/0/name",
            Value = JsonSerializer.SerializeToElement("Alice"),
        });

        dm.Resolve(DynamicValue.FromPath("/items/0/name")).Should().Be("Alice");
    }

    [Fact]
    public void Apply_ArrayIndexPath_MultipleElements()
    {
        var dm = new DataModel();
        dm.Apply(new UpdateDataModel
        {
            SurfaceId = "s",
            Path = "/items/0",
            Value = JsonSerializer.SerializeToElement(new { name = "Alice" }),
        });
        dm.Apply(new UpdateDataModel
        {
            SurfaceId = "s",
            Path = "/items/1",
            Value = JsonSerializer.SerializeToElement(new { name = "Bob" }),
        });

        dm.Resolve(DynamicValue.FromPath("/items/0/name")).Should().Be("Alice");
        dm.Resolve(DynamicValue.FromPath("/items/1/name")).Should().Be("Bob");
    }

    [Fact]
    public void Apply_ArrayIndexPath_PreservesExistingArray()
    {
        var dm = new DataModel();
        dm.SetSnapshot(JsonSerializer.SerializeToElement(new
        {
            items = new[] { new { name = "original" } }
        }));

        dm.Apply(new UpdateDataModel
        {
            SurfaceId = "s",
            Path = "/items/0/name",
            Value = JsonSerializer.SerializeToElement("updated"),
        });

        dm.Resolve(DynamicValue.FromPath("/items/0/name")).Should().Be("updated");
    }

    [Fact]
    public void Apply_DeleteAtArrayPath_NullifiesElement()
    {
        var dm = new DataModel();
        dm.SetSnapshot(JsonSerializer.SerializeToElement(new
        {
            items = new[] { "alpha", "beta", "gamma" }
        }));

        dm.Apply(new UpdateDataModel
        {
            SurfaceId = "s",
            Path = "/items/1",
            Value = null,
        });

        dm.Resolve(DynamicValue.FromPath("/items/1")).Should().BeNull();
        dm.Resolve(DynamicValue.FromPath("/items/2")).Should().Be("gamma");
    }

    [Fact]
    public void Apply_PreservesExistingObjectWhenNextSegmentIsNumeric()
    {
        var dm = new DataModel();
        dm.SetSnapshot(JsonSerializer.SerializeToElement(new
        {
            totals = new Dictionary<string, int> { ["2025"] = 100 }
        }));

        dm.Apply(new UpdateDataModel
        {
            SurfaceId = "s",
            Path = "/totals/2026",
            Value = JsonSerializer.SerializeToElement(200),
        });

        // Should preserve the object (not create an array) since "totals" already exists as object
        dm.Resolve(DynamicValue.FromPath("/totals/2025")).Should().Be("100");
        dm.Resolve(DynamicValue.FromPath("/totals/2026")).Should().Be("200");
    }

    [Fact]
    public void ToJson_EmptyModel_ReturnsEmptyObject()
    {
        var dm = new DataModel();
        dm.ToJson().Should().Be("{}");
    }

    [Fact]
    public void ToJson_AfterApply_ReflectsState()
    {
        var dm = new DataModel();
        dm.Apply(new UpdateDataModel
        {
            SurfaceId = "s",
            Path = "/name",
            Value = JsonSerializer.SerializeToElement("Alice"),
        });

        var json = dm.ToJson();
        json.Should().Contain("\"name\"");
        json.Should().Contain("\"Alice\"");
    }

    [Fact]
    public void ToJson_Indented_ProducesFormattedOutput()
    {
        var dm = new DataModel();
        dm.Apply(new UpdateDataModel
        {
            SurfaceId = "s",
            Path = "/key",
            Value = JsonSerializer.SerializeToElement("val"),
        });

        var json = dm.ToJson(indented: true);
        json.Should().Contain("\n");
        json.Should().Contain("  ");
    }
}
