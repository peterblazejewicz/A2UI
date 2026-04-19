using System.Text.Json;
using A2Ui.Core.Components;
using A2Ui.Core.Messages;

namespace A2Ui.Core.Tests.A2Ui;

public sealed class ChildListConverterTests
{
    private static readonly JsonSerializerOptions s_opts = new(JsonSerializerDefaults.Web)
    {
        AllowOutOfOrderMetadataProperties = true,
    };

    [Fact]
    public void Read_StaticArray_ParsesIds()
    {
        var result = JsonSerializer.Deserialize<ChildList>("""["a","b","c"]""", s_opts);

        Assert.NotNull(result);
        Assert.Equivalent(new[] { "a", "b", "c" }, result!.Ids);
        Assert.False(result.IsTemplate);
    }

    [Fact]
    public void Read_Template_ParsesComponentIdAndPath()
    {
        var result = JsonSerializer.Deserialize<ChildList>("""{"componentId":"item-tmpl","path":"/items"}""", s_opts);

        Assert.NotNull(result);
        Assert.True(result!.IsTemplate);
        Assert.Equal("item-tmpl", result.Template!.ComponentId);
        Assert.Equal("/items", result.Template.Path);
    }

    [Fact]
    public void Write_StaticArray_ProducesJsonArray()
    {
        var cl = ChildList.FromIds("x", "y");
        var json = JsonSerializer.Serialize(cl, s_opts);
        Assert.Equal("""["x","y"]""", json);
    }

    [Fact]
    public void Write_Template_ProducesJsonObject()
    {
        var cl = ChildList.FromTemplate("tmpl", "/data");
        var json = JsonSerializer.Serialize(cl, s_opts);

        var doc = JsonDocument.Parse(json);
        Assert.Equal("tmpl", doc.RootElement.GetProperty("componentId").GetString());
        Assert.Equal("/data", doc.RootElement.GetProperty("path").GetString());
    }

    [Fact]
    public void RoundTrip_StaticArray_Preserves()
    {
        var original = ChildList.FromIds("a", "b", "c");
        var json = JsonSerializer.Serialize(original, s_opts);
        var restored = JsonSerializer.Deserialize<ChildList>(json, s_opts);

        Assert.Equivalent(original.Ids, restored!.Ids);
    }

    [Fact]
    public void RoundTrip_Template_Preserves()
    {
        var original = ChildList.FromTemplate("tmpl", "/items");
        var json = JsonSerializer.Serialize(original, s_opts);
        var restored = JsonSerializer.Deserialize<ChildList>(json, s_opts);

        Assert.Equal("tmpl", restored!.Template!.ComponentId);
        Assert.Equal("/items", restored.Template.Path);
    }

    [Fact]
    public void Component_WithChildren_Deserializes()
    {
        var json = """{"id":"row1","component":"Row","children":["t1","t2","t3"]}""";
        var comp = JsonSerializer.Deserialize<A2UiComponent>(json, s_opts);

        Assert.NotNull(comp!.Children);
        Assert.Equivalent(new[] { "t1", "t2", "t3" }, comp.Children!.Ids);
    }

    [Fact]
    public void Component_WithTemplateChildren_Deserializes()
    {
        var json = """{"id":"list1","component":"List","children":{"componentId":"item","path":"/todos"}}""";
        var comp = JsonSerializer.Deserialize<A2UiComponent>(json, s_opts);

        Assert.NotNull(comp!.Children);
        Assert.True(comp.Children!.IsTemplate);
        Assert.Equal("/todos", comp.Children.Template!.Path);
    }

    [Fact]
    public void Component_WithAccessibility_Deserializes()
    {
        var json =
            """{"id":"btn1","component":"Button","accessibility":{"label":"Submit form","description":"Submits the reservation"}}""";
        var comp = JsonSerializer.Deserialize<A2UiComponent>(json, s_opts);

        Assert.NotNull(comp!.Accessibility);
        var labelSv = Assert.IsType<DynamicValue.StringValue>(comp.Accessibility!.Label);
        Assert.Equal("Submit form", labelSv.Value);
        var descSv = Assert.IsType<DynamicValue.StringValue>(comp.Accessibility.Description);
        Assert.Equal("Submits the reservation", descSv.Value);
    }

    [Fact]
    public void Component_WithChecks_Deserializes()
    {
        var json =
            """{"id":"tf1","component":"TextField","checks":[{"condition":{"call":"required","args":{"value":{"path":"/name"}}},"message":"Name is required"}]}""";
        var comp = JsonSerializer.Deserialize<A2UiComponent>(json, s_opts);

        var check = Assert.Single(comp!.Checks!);
        var condFv = Assert.IsType<DynamicValue.FunctionValue>(check.Condition);
        Assert.Equal("required", condFv.Call.Call);
        Assert.Equal("Name is required", check.Message);
    }

    [Fact]
    public void Component_WithChoiceOptions_Deserializes()
    {
        var json =
            """{"id":"cp1","component":"ChoicePicker","options":[{"label":"Red","value":"red"},{"label":"Blue","value":"blue"}],"displayStyle":"chips","filterable":true}""";
        var comp = Assert.IsType<ChoicePickerComponent>(JsonSerializer.Deserialize<A2UiComponent>(json, s_opts));

        Assert.Equal(2, comp.Options!.Length);
        Assert.Equal(DynamicValue.FromString("Red"), comp.Options![0].Label);
        Assert.Equal("red", comp.Options[0].Value);
        Assert.Equal("chips", comp.DisplayStyle);
        Assert.True(comp.Filterable);
    }

    [Fact]
    public void Component_WithTabs_Deserializes()
    {
        var json =
            """{"id":"tabs1","component":"Tabs","tabs":[{"title":"Info","child":"panel1"},{"title":"Settings","child":"panel2"}]}""";
        var comp = Assert.IsType<TabsComponent>(JsonSerializer.Deserialize<A2UiComponent>(json, s_opts));

        Assert.Equal(2, comp.Tabs!.Length);
        Assert.Equal(DynamicValue.FromString("Info"), comp.Tabs![0].Title);
        Assert.Equal("panel1", comp.Tabs[0].Child);
    }

    [Fact]
    public void Component_WithLayoutProps_Deserializes()
    {
        var json = """{"id":"row1","component":"Row","justify":"spaceBetween","align":"center","weight":2.5}""";
        var comp = Assert.IsType<RowComponent>(JsonSerializer.Deserialize<A2UiComponent>(json, s_opts));

        Assert.Equal("spaceBetween", comp.Justify);
        Assert.Equal("center", comp.Align);
        Assert.Equal(2.5, comp.Weight);
    }

    [Fact]
    public void ChoiceOption_WithBoundLabel_DeserializesToPathValue()
    {
        // Spec: basic_catalog.json types ChoiceOption.label as DynamicString,
        // which means it must accept path-bound values, not just literal strings.
        var json =
            """{"id":"cp1","component":"ChoicePicker","options":[{"label":{"path":"/labels/0"},"value":"first"}]}""";
        var comp = Assert.IsType<ChoicePickerComponent>(JsonSerializer.Deserialize<A2UiComponent>(json, s_opts));

        var pathValue = Assert.IsType<DynamicValue.PathValue>(comp.Options![0].Label);
        Assert.Equal("/labels/0", pathValue.DataPath);
        Assert.Equal("first", comp.Options[0].Value);
    }

    [Fact]
    public void TabDefinition_WithBoundTitle_DeserializesToPathValue()
    {
        // Spec: basic_catalog.json types TabDefinition.title as DynamicString.
        var json = """{"id":"tabs1","component":"Tabs","tabs":[{"title":{"path":"/tabTitles/0"},"child":"panel1"}]}""";
        var comp = Assert.IsType<TabsComponent>(JsonSerializer.Deserialize<A2UiComponent>(json, s_opts));

        var pathValue = Assert.IsType<DynamicValue.PathValue>(comp.Tabs![0].Title);
        Assert.Equal("/tabTitles/0", pathValue.DataPath);
    }

    [Fact]
    public void ChoiceOption_WithFunctionCallLabel_DeserializesToFunctionValue()
    {
        var json =
            """{"id":"cp1","component":"ChoicePicker","options":[{"label":{"call":"formatString","args":{"value":"hi"}},"value":"first"}]}""";
        var comp = Assert.IsType<ChoicePickerComponent>(JsonSerializer.Deserialize<A2UiComponent>(json, s_opts));

        var fn = Assert.IsType<DynamicValue.FunctionValue>(comp.Options![0].Label);
        Assert.Equal("formatString", fn.Call.Call);
    }

    // ── F4: non-null diagnostics ─────────────────────────────────────────

    [Fact]
    public void ChildList_Read_TemplateMissingPath_ThrowsJsonException()
    {
        // Previously the converter silently returned null for a template object
        // missing one of the required keys, which would collapse the entire
        // children declaration.
        var ex = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<ChildList>("""{"componentId":"tmpl"}""", s_opts)
        );

        Assert.Contains("componentId", ex.Message);
        Assert.Contains("path", ex.Message);
    }

    [Fact]
    public void ChildList_Read_NullToken_ReturnsNull()
    {
        var result = JsonSerializer.Deserialize<ChildList>("null", s_opts);

        Assert.Null(result);
    }
}
