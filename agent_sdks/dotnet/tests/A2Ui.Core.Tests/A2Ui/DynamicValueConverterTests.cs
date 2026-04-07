using System.Text.Json;
using A2Ui.Core.Messages;
using FluentAssertions;

namespace A2Ui.Core.Tests;

public sealed class DynamicValueConverterTests
{
    private static readonly JsonSerializerOptions s_opts = new(JsonSerializerDefaults.Web);

    [Fact]
    public void Read_StringLiteral_ParsesCorrectly()
    {
        var result = JsonSerializer.Deserialize<DynamicValue>("\"hello\"", s_opts);

        result.Should().NotBeNull();
        result!.StringLiteral.Should().Be("hello");
        result.IsLiteral.Should().BeTrue();
    }

    [Fact]
    public void Read_NumberLiteral_ParsesCorrectly()
    {
        var result = JsonSerializer.Deserialize<DynamicValue>("42.5", s_opts);

        result.Should().NotBeNull();
        result!.NumberLiteral.Should().Be(42.5);
        result.IsLiteral.Should().BeTrue();
    }

    [Fact]
    public void Read_BoolLiteral_ParsesCorrectly()
    {
        var trueResult = JsonSerializer.Deserialize<DynamicValue>("true", s_opts);
        var falseResult = JsonSerializer.Deserialize<DynamicValue>("false", s_opts);

        trueResult!.BoolLiteral.Should().BeTrue();
        falseResult!.BoolLiteral.Should().BeFalse();
    }

    [Fact]
    public void Read_ArrayLiteral_ParsesCorrectly()
    {
        var result = JsonSerializer.Deserialize<DynamicValue>("""["a","b","c"]""", s_opts);

        result.Should().NotBeNull();
        result!.ArrayLiteral.Should().NotBeNull();
        result.ArrayLiteral!.Value.GetArrayLength().Should().Be(3);
        result.IsLiteral.Should().BeTrue();
    }

    [Fact]
    public void Read_DataBinding_ParsesCorrectly()
    {
        var result = JsonSerializer.Deserialize<DynamicValue>("""{"path":"/user/name"}""", s_opts);

        result.Should().NotBeNull();
        result!.Path.Should().Be("/user/name");
        result.IsBound.Should().BeTrue();
        result.IsLiteral.Should().BeFalse();
    }

    [Fact]
    public void Read_FunctionCall_ParsesCorrectly()
    {
        var json = """{"call":"formatDate","args":{"value":{"path":"/date"}},"returnType":"string"}""";
        var result = JsonSerializer.Deserialize<DynamicValue>(json, s_opts);

        result.Should().NotBeNull();
        result!.FunctionCall.Should().NotBeNull();
        result.FunctionCall!.Call.Should().Be("formatDate");
        result.FunctionCall.ReturnType.Should().Be("string");
        result.FunctionCall.Args.Should().ContainKey("value");
        result.IsFunction.Should().BeTrue();
    }

    [Fact]
    public void Write_StringLiteral_ProducesString()
    {
        var dv = DynamicValue.FromString("hello");
        var json = JsonSerializer.Serialize(dv, s_opts);
        json.Should().Be("\"hello\"");
    }

    [Fact]
    public void Write_NumberLiteral_ProducesNumber()
    {
        var dv = DynamicValue.FromNumber(3.14);
        var json = JsonSerializer.Serialize(dv, s_opts);
        json.Should().Be("3.14");
    }

    [Fact]
    public void Write_BoolLiteral_ProducesBool()
    {
        var dv = DynamicValue.FromBool(true);
        var json = JsonSerializer.Serialize(dv, s_opts);
        json.Should().Be("true");
    }

    [Fact]
    public void Write_DataBinding_ProducesPathObject()
    {
        var dv = DynamicValue.FromPath("/user/name");
        var json = JsonSerializer.Serialize(dv, s_opts);
        json.Should().Be("""{"path":"/user/name"}""");
    }

    [Fact]
    public void Write_FunctionCall_ProducesFunctionObject()
    {
        var dv = new DynamicValue
        {
            FunctionCall = new FunctionCallValue { Call = "required", ReturnType = "boolean" },
        };
        var json = JsonSerializer.Serialize(dv, s_opts);

        var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("call").GetString().Should().Be("required");
        doc.RootElement.GetProperty("returnType").GetString().Should().Be("boolean");
    }

    [Fact]
    public void RoundTrip_PreservesAllForms()
    {
        var values = new DynamicValue[]
        {
            DynamicValue.FromString("test"),
            DynamicValue.FromNumber(99),
            DynamicValue.FromBool(false),
            DynamicValue.FromPath("/x"),
        };

        foreach (var original in values)
        {
            var json = JsonSerializer.Serialize(original, s_opts);
            var restored = JsonSerializer.Deserialize<DynamicValue>(json, s_opts);

            restored!.StringLiteral.Should().Be(original.StringLiteral);
            restored.NumberLiteral.Should().Be(original.NumberLiteral);
            restored.BoolLiteral.Should().Be(original.BoolLiteral);
            restored.Path.Should().Be(original.Path);
        }
    }
}
