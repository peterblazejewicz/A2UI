using System.Text.Json;
using A2Ui.Core.Messages;

namespace A2Ui.Core.Tests;

public sealed class DynamicValueConverterTests
{
    private static readonly JsonSerializerOptions s_opts = new(JsonSerializerDefaults.Web);

    [Fact]
    public void Read_StringLiteral_ParsesCorrectly()
    {
        var result = JsonSerializer.Deserialize<DynamicValue>("\"hello\"", s_opts);

        Assert.NotNull(result);
        Assert.Equal("hello", result!.StringLiteral);
        Assert.True(result.IsLiteral);
    }

    [Fact]
    public void Read_NumberLiteral_ParsesCorrectly()
    {
        var result = JsonSerializer.Deserialize<DynamicValue>("42.5", s_opts);

        Assert.NotNull(result);
        Assert.Equal(42.5, result!.NumberLiteral);
        Assert.True(result.IsLiteral);
    }

    [Fact]
    public void Read_BoolLiteral_ParsesCorrectly()
    {
        var trueResult = JsonSerializer.Deserialize<DynamicValue>("true", s_opts);
        var falseResult = JsonSerializer.Deserialize<DynamicValue>("false", s_opts);

        Assert.True(trueResult!.BoolLiteral);
        Assert.False(falseResult!.BoolLiteral);
    }

    [Fact]
    public void Read_ArrayLiteral_ParsesCorrectly()
    {
        var result = JsonSerializer.Deserialize<DynamicValue>("""["a","b","c"]""", s_opts);

        Assert.NotNull(result);
        Assert.NotNull(result!.ArrayLiteral);
        Assert.Equal(3, result.ArrayLiteral!.Value.GetArrayLength());
        Assert.True(result.IsLiteral);
    }

    [Fact]
    public void Read_DataBinding_ParsesCorrectly()
    {
        var result = JsonSerializer.Deserialize<DynamicValue>("""{"path":"/user/name"}""", s_opts);

        Assert.NotNull(result);
        Assert.Equal("/user/name", result!.Path);
        Assert.True(result.IsBound);
        Assert.False(result.IsLiteral);
    }

    [Fact]
    public void Read_FunctionCall_ParsesCorrectly()
    {
        var json = """{"call":"formatDate","args":{"value":{"path":"/date"}},"returnType":"string"}""";
        var result = JsonSerializer.Deserialize<DynamicValue>(json, s_opts);

        Assert.NotNull(result);
        Assert.NotNull(result!.FunctionCall);
        Assert.Equal("formatDate", result.FunctionCall!.Call);
        Assert.Equal("string", result.FunctionCall.ReturnType);
        Assert.Contains("value", result.FunctionCall.Args!);
        Assert.True(result.IsFunction);
    }

    [Fact]
    public void Write_StringLiteral_ProducesString()
    {
        var dv = DynamicValue.FromString("hello");
        var json = JsonSerializer.Serialize(dv, s_opts);
        Assert.Equal("\"hello\"", json);
    }

    [Fact]
    public void Write_NumberLiteral_ProducesNumber()
    {
        var dv = DynamicValue.FromNumber(3.14);
        var json = JsonSerializer.Serialize(dv, s_opts);
        Assert.Equal("3.14", json);
    }

    [Fact]
    public void Write_BoolLiteral_ProducesBool()
    {
        var dv = DynamicValue.FromBool(true);
        var json = JsonSerializer.Serialize(dv, s_opts);
        Assert.Equal("true", json);
    }

    [Fact]
    public void Write_DataBinding_ProducesPathObject()
    {
        var dv = DynamicValue.FromPath("/user/name");
        var json = JsonSerializer.Serialize(dv, s_opts);
        Assert.Equal("""{"path":"/user/name"}""", json);
    }

    [Fact]
    public void Write_FunctionCall_ProducesFunctionObject()
    {
        DynamicValue dv = new DynamicValue.FunctionValue(
            new FunctionCallValue { Call = "required", ReturnType = "boolean" }
        );
        var json = JsonSerializer.Serialize(dv, s_opts);

        var doc = JsonDocument.Parse(json);
        Assert.Equal("required", doc.RootElement.GetProperty("call").GetString());
        Assert.Equal("boolean", doc.RootElement.GetProperty("returnType").GetString());
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

            Assert.Equal(original.StringLiteral, restored!.StringLiteral);
            Assert.Equal(original.NumberLiteral, restored.NumberLiteral);
            Assert.Equal(original.BoolLiteral, restored.BoolLiteral);
            Assert.Equal(original.Path, restored.Path);
        }
    }

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
            onFunction: _ => "fn"
        );
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
}
