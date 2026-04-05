using System.Text.Json;
using A2Ui.Core.Messages;
using FluentAssertions;

namespace A2Ui.Core.Tests;

public sealed class A2UiMessageTests
{
    private static readonly JsonSerializerOptions s_opts = new(JsonSerializerDefaults.Web);

    [Fact]
    public void CreateSurface_RoundTrip_PreservesAllFields()
    {
        var json = """{"version":"v0.9","createSurface":{"surfaceId":"main","catalogId":"https://a2ui.org/specification/v0_9/basic_catalog.json","theme":{"primaryColor":"#00BFFF"},"sendDataModel":true}}""";

        var msg = JsonSerializer.Deserialize<A2UiMessage>(json, s_opts)!;

        msg.Version.Should().Be("v0.9");
        msg.CreateSurface.Should().NotBeNull();
        msg.CreateSurface!.SurfaceId.Should().Be("main");
        msg.CreateSurface.CatalogId.Should().Be("https://a2ui.org/specification/v0_9/basic_catalog.json");
        msg.CreateSurface.Theme.Should().NotBeNull();
        msg.CreateSurface.SendDataModel.Should().BeTrue();
    }

    [Fact]
    public void DeleteSurface_RoundTrip()
    {
        var json = """{"version":"v0.9","deleteSurface":{"surfaceId":"main"}}""";

        var msg = JsonSerializer.Deserialize<A2UiMessage>(json, s_opts)!;

        msg.DeleteSurface.Should().NotBeNull();
        msg.DeleteSurface!.SurfaceId.Should().Be("main");
    }

    [Fact]
    public void UpdateComponents_RoundTrip_WithComponentProperties()
    {
        var json = """{"version":"v0.9","updateComponents":{"surfaceId":"main","components":[{"id":"root","component":"Column"},{"id":"t1","component":"Text","text":"Hello","parent":"root"}]}}""";

        var msg = JsonSerializer.Deserialize<A2UiMessage>(json, s_opts)!;

        msg.UpdateComponents.Should().NotBeNull();
        msg.UpdateComponents!.Components.Should().HaveCount(2);
        msg.UpdateComponents.Components[1].Text!.StringLiteral.Should().Be("Hello");
    }

    [Fact]
    public void UpdateDataModel_RoundTrip_WithPath()
    {
        var json = """{"version":"v0.9","updateDataModel":{"surfaceId":"main","path":"/user/name","value":"Alice"}}""";

        var msg = JsonSerializer.Deserialize<A2UiMessage>(json, s_opts)!;

        msg.UpdateDataModel.Should().NotBeNull();
        msg.UpdateDataModel!.SurfaceId.Should().Be("main");
        msg.UpdateDataModel.Path.Should().Be("/user/name");
        msg.UpdateDataModel.Value.Should().NotBeNull();
    }

    [Fact]
    public void UpdateDataModel_NullPathAndValue_Deserializes()
    {
        var json = """{"version":"v0.9","updateDataModel":{"surfaceId":"main"}}""";

        var msg = JsonSerializer.Deserialize<A2UiMessage>(json, s_opts)!;

        msg.UpdateDataModel.Should().NotBeNull();
        msg.UpdateDataModel!.Path.Should().BeNull();
        msg.UpdateDataModel.Value.Should().BeNull();
    }

    [Fact]
    public void ComponentAction_WithEvent_RoundTrip()
    {
        var json = """{"id":"btn1","component":"Button","action":{"event":{"name":"submit","context":{"date":{"path":"/reservation/date"}}}}}""";

        var comp = JsonSerializer.Deserialize<A2UiComponent>(json, s_opts)!;

        comp.Action.Should().NotBeNull();
        comp.Action!.Event.Should().NotBeNull();
        comp.Action.Event!.Name.Should().Be("submit");
        comp.Action.Event.Context.Should().ContainKey("date");
        comp.Action.Event.Context!["date"].IsBound.Should().BeTrue();
        comp.Action.Event.Context["date"].Path.Should().Be("/reservation/date");
    }

    [Fact]
    public void ComponentAction_WithFunctionCall_RoundTrip()
    {
        var json = """{"id":"link1","component":"Button","action":{"functionCall":{"call":"openUrl","args":{"url":"https://example.com"},"returnType":"void"}}}""";

        var comp = JsonSerializer.Deserialize<A2UiComponent>(json, s_opts)!;

        comp.Action.Should().NotBeNull();
        comp.Action!.FunctionCall.Should().NotBeNull();
        comp.Action.FunctionCall!.Call.Should().Be("openUrl");
        comp.Action.FunctionCall.ReturnType.Should().Be("void");
    }

    [Fact]
    public void DynamicValue_BoundPath_RoundTrip()
    {
        var json = """{"id":"t1","component":"Text","text":{"path":"/user/name"}}""";

        var comp = JsonSerializer.Deserialize<A2UiComponent>(json, s_opts)!;

        comp.Text.Should().NotBeNull();
        comp.Text!.IsBound.Should().BeTrue();
        comp.Text.Path.Should().Be("/user/name");
    }

    [Fact]
    public void ClientToServer_Action_RoundTrip()
    {
        var json = """{"version":"v0.9","action":{"name":"submit","surfaceId":"main","sourceComponentId":"btn1","timestamp":"2025-12-15T10:30:00Z","context":{"date":"2025-12-15"}}}""";

        var msg = JsonSerializer.Deserialize<ClientToServerMessage>(json, s_opts)!;

        msg.Version.Should().Be("v0.9");
        msg.Action.Should().NotBeNull();
        msg.Action!.Name.Should().Be("submit");
        msg.Action.SourceComponentId.Should().Be("btn1");
        msg.Action.Timestamp.Should().Be("2025-12-15T10:30:00Z");
    }

    [Fact]
    public void ClientToServer_ValidationError_RoundTrip()
    {
        var json = """{"version":"v0.9","error":{"code":"VALIDATION_FAILED","surfaceId":"main","message":"Invalid date","path":"/components/0/value"}}""";

        var msg = JsonSerializer.Deserialize<ClientToServerMessage>(json, s_opts)!;

        msg.Error.Should().NotBeNull();
        msg.Error!.Code.Should().Be("VALIDATION_FAILED");
        msg.Error.Path.Should().Be("/components/0/value");
    }

    [Fact]
    public void ClientToServer_GenericError_RoundTrip()
    {
        var json = """{"version":"v0.9","error":{"code":"NETWORK_ERROR","surfaceId":"main","message":"Connection lost"}}""";

        var msg = JsonSerializer.Deserialize<ClientToServerMessage>(json, s_opts)!;

        msg.Error.Should().NotBeNull();
        msg.Error!.Code.Should().Be("NETWORK_ERROR");
        msg.Error.Path.Should().BeNull();
    }
}
