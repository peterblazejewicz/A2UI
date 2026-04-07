using System.Text.Json;
using A2Ui.Core.Messages;
using FluentAssertions;

namespace A2Ui.Core.Tests;

public sealed class ChildListConverterTests
{
    private static readonly JsonSerializerOptions s_opts = new(JsonSerializerDefaults.Web);

    [Fact]
    public void Read_StaticArray_ParsesIds()
    {
        var result = JsonSerializer.Deserialize<ChildList>("""["a","b","c"]""", s_opts);

        result.Should().NotBeNull();
        result!.Ids.Should().BeEquivalentTo(["a", "b", "c"]);
        result.IsTemplate.Should().BeFalse();
    }

    [Fact]
    public void Read_Template_ParsesComponentIdAndPath()
    {
        var result = JsonSerializer.Deserialize<ChildList>("""{"componentId":"item-tmpl","path":"/items"}""", s_opts);

        result.Should().NotBeNull();
        result!.IsTemplate.Should().BeTrue();
        result.Template!.ComponentId.Should().Be("item-tmpl");
        result.Template.Path.Should().Be("/items");
    }

    [Fact]
    public void Write_StaticArray_ProducesJsonArray()
    {
        var cl = ChildList.FromIds("x", "y");
        var json = JsonSerializer.Serialize(cl, s_opts);
        json.Should().Be("""["x","y"]""");
    }

    [Fact]
    public void Write_Template_ProducesJsonObject()
    {
        var cl = ChildList.FromTemplate("tmpl", "/data");
        var json = JsonSerializer.Serialize(cl, s_opts);

        var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("componentId").GetString().Should().Be("tmpl");
        doc.RootElement.GetProperty("path").GetString().Should().Be("/data");
    }

    [Fact]
    public void RoundTrip_StaticArray_Preserves()
    {
        var original = ChildList.FromIds("a", "b", "c");
        var json = JsonSerializer.Serialize(original, s_opts);
        var restored = JsonSerializer.Deserialize<ChildList>(json, s_opts);

        restored!.Ids.Should().BeEquivalentTo(original.Ids);
    }

    [Fact]
    public void RoundTrip_Template_Preserves()
    {
        var original = ChildList.FromTemplate("tmpl", "/items");
        var json = JsonSerializer.Serialize(original, s_opts);
        var restored = JsonSerializer.Deserialize<ChildList>(json, s_opts);

        restored!.Template!.ComponentId.Should().Be("tmpl");
        restored.Template.Path.Should().Be("/items");
    }

    [Fact]
    public void Component_WithChildren_Deserializes()
    {
        var json = """{"id":"row1","component":"Row","children":["t1","t2","t3"]}""";
        var comp = JsonSerializer.Deserialize<A2UiComponent>(json, s_opts);

        comp!.Children.Should().NotBeNull();
        comp.Children!.Ids.Should().BeEquivalentTo(["t1", "t2", "t3"]);
    }

    [Fact]
    public void Component_WithTemplateChildren_Deserializes()
    {
        var json = """{"id":"list1","component":"List","children":{"componentId":"item","path":"/todos"}}""";
        var comp = JsonSerializer.Deserialize<A2UiComponent>(json, s_opts);

        comp!.Children.Should().NotBeNull();
        comp.Children!.IsTemplate.Should().BeTrue();
        comp.Children.Template!.Path.Should().Be("/todos");
    }

    [Fact]
    public void Component_WithAccessibility_Deserializes()
    {
        var json =
            """{"id":"btn1","component":"Button","accessibility":{"label":"Submit form","description":"Submits the reservation"}}""";
        var comp = JsonSerializer.Deserialize<A2UiComponent>(json, s_opts);

        comp!.Accessibility.Should().NotBeNull();
        comp.Accessibility!.Label!.StringLiteral.Should().Be("Submit form");
        comp.Accessibility.Description!.StringLiteral.Should().Be("Submits the reservation");
    }

    [Fact]
    public void Component_WithChecks_Deserializes()
    {
        var json =
            """{"id":"tf1","component":"TextField","checks":[{"condition":{"call":"required","args":{"value":{"path":"/name"}}},"message":"Name is required"}]}""";
        var comp = JsonSerializer.Deserialize<A2UiComponent>(json, s_opts);

        comp!.Checks.Should().HaveCount(1);
        comp.Checks![0].Condition.IsFunction.Should().BeTrue();
        comp.Checks[0].Condition.FunctionCall!.Call.Should().Be("required");
        comp.Checks[0].Message.Should().Be("Name is required");
    }

    [Fact]
    public void Component_WithChoiceOptions_Deserializes()
    {
        var json =
            """{"id":"cp1","component":"ChoicePicker","options":[{"label":"Red","value":"red"},{"label":"Blue","value":"blue"}],"displayStyle":"chips","filterable":true}""";
        var comp = JsonSerializer.Deserialize<A2UiComponent>(json, s_opts);

        comp!.Options.Should().HaveCount(2);
        comp.Options![0].Label.Should().Be("Red");
        comp.Options[0].Value.Should().Be("red");
        comp.DisplayStyle.Should().Be("chips");
        comp.Filterable.Should().BeTrue();
    }

    [Fact]
    public void Component_WithTabs_Deserializes()
    {
        var json =
            """{"id":"tabs1","component":"Tabs","tabs":[{"title":"Info","child":"panel1"},{"title":"Settings","child":"panel2"}]}""";
        var comp = JsonSerializer.Deserialize<A2UiComponent>(json, s_opts);

        comp!.Tabs.Should().HaveCount(2);
        comp.Tabs![0].Title.Should().Be("Info");
        comp.Tabs[0].Child.Should().Be("panel1");
    }

    [Fact]
    public void Component_WithLayoutProps_Deserializes()
    {
        var json = """{"id":"row1","component":"Row","justify":"spaceBetween","align":"center","weight":2.5}""";
        var comp = JsonSerializer.Deserialize<A2UiComponent>(json, s_opts);

        comp!.Justify.Should().Be("spaceBetween");
        comp.Align.Should().Be("center");
        comp.Weight.Should().Be(2.5);
    }
}
