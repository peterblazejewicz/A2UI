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
}
