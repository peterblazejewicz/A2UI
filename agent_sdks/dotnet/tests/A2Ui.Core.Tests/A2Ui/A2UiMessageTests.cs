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
        var json =
            """{"version":"v0.9","createSurface":{"surfaceId":"main","catalogId":"https://a2ui.org/specification/v0_9/basic_catalog.json","theme":{"primaryColor":"#00BFFF"},"sendDataModel":true}}""";

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
        var json =
            """{"version":"v0.9","updateComponents":{"surfaceId":"main","components":[{"id":"root","component":"Column"},{"id":"t1","component":"Text","text":"Hello","parent":"root"}]}}""";

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
        var json =
            """{"id":"btn1","component":"Button","action":{"event":{"name":"submit","context":{"date":{"path":"/reservation/date"}}}}}""";

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
        var json =
            """{"id":"link1","component":"Button","action":{"functionCall":{"call":"openUrl","args":{"url":"https://example.com"},"returnType":"void"}}}""";

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
        var json =
            """{"version":"v0.9","action":{"name":"submit","surfaceId":"main","sourceComponentId":"btn1","timestamp":"2025-12-15T10:30:00Z","context":{"date":"2025-12-15"}}}""";

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
        var json =
            """{"version":"v0.9","error":{"code":"VALIDATION_FAILED","surfaceId":"main","message":"Invalid date","path":"/components/0/value"}}""";

        var msg = JsonSerializer.Deserialize<ClientToServerMessage>(json, s_opts)!;

        msg.Error.Should().NotBeNull();
        msg.Error!.Code.Should().Be("VALIDATION_FAILED");
        msg.Error.Path.Should().Be("/components/0/value");
    }

    [Fact]
    public void ClientToServer_GenericError_RoundTrip()
    {
        var json =
            """{"version":"v0.9","error":{"code":"NETWORK_ERROR","surfaceId":"main","message":"Connection lost"}}""";

        var msg = JsonSerializer.Deserialize<ClientToServerMessage>(json, s_opts)!;

        msg.Error.Should().NotBeNull();
        msg.Error!.Code.Should().Be("NETWORK_ERROR");
        msg.Error.Path.Should().BeNull();
    }

    // --- A2UiMessage.Validate() tests ---

    [Fact]
    public void Validate_MissingVersion_Throws()
    {
        var msg = new A2UiMessage
        {
            CreateSurface = new CreateSurface { SurfaceId = "s", CatalogId = "c" },
        };
        var act = () => msg.Validate();
        act.Should().Throw<A2UiMessageValidationException>().Which.Message.Should().Contain("version");
    }

    [Fact]
    public void Validate_WrongVersion_Throws()
    {
        var msg = new A2UiMessage
        {
            Version = "v0.8",
            CreateSurface = new CreateSurface { SurfaceId = "s", CatalogId = "c" },
        };
        var act = () => msg.Validate();
        act.Should().Throw<A2UiMessageValidationException>().Which.Message.Should().Contain("v0.8");
    }

    [Fact]
    public void Validate_ZeroOperations_Throws()
    {
        var msg = new A2UiMessage { Version = "v0.9" };
        var act = () => msg.Validate();
        act.Should().Throw<A2UiMessageValidationException>().Which.Message.Should().Contain("exactly one operation");
    }

    [Fact]
    public void Validate_MultipleOperations_Throws()
    {
        var msg = new A2UiMessage
        {
            Version = "v0.9",
            CreateSurface = new CreateSurface { SurfaceId = "s", CatalogId = "c" },
            DeleteSurface = new DeleteSurface { SurfaceId = "s" },
        };
        var act = () => msg.Validate();
        act.Should().Throw<A2UiMessageValidationException>().Which.Message.Should().Contain("2 operations");
    }

    [Theory]
    [InlineData("createSurface")]
    [InlineData("deleteSurface")]
    [InlineData("updateComponents")]
    [InlineData("updateDataModel")]
    public void Validate_ExactlyOneOperation_Succeeds(string opType)
    {
        A2UiMessage msg = opType switch
        {
            "createSurface" => new A2UiMessage
            {
                Version = "v0.9",
                CreateSurface = new CreateSurface { SurfaceId = "s", CatalogId = "c" },
            },
            "deleteSurface" => new A2UiMessage
            {
                Version = "v0.9",
                DeleteSurface = new DeleteSurface { SurfaceId = "s" },
            },
            "updateComponents" => new A2UiMessage
            {
                Version = "v0.9",
                UpdateComponents = new UpdateComponents { SurfaceId = "s", Components = [] },
            },
            "updateDataModel" => new A2UiMessage
            {
                Version = "v0.9",
                UpdateDataModel = new UpdateDataModel { SurfaceId = "s" },
            },
            _ => throw new System.ArgumentException(opType),
        };

        var act = () => msg.Validate();
        act.Should().NotThrow();
    }

    [Fact]
    public void OperationType_ReturnsCorrectDiscriminator()
    {
        var msg = new A2UiMessage
        {
            Version = "v0.9",
            UpdateComponents = new UpdateComponents { SurfaceId = "s", Components = [] },
        };
        msg.Operation.Should().Be(A2UiOperationType.UpdateComponents);
    }

    [Fact]
    public void OperationType_NoOperation_ReturnsNull()
    {
        var msg = new A2UiMessage { Version = "v0.9" };
        msg.Operation.Should().BeNull();
    }

    // --- ClientToServerMessage.Validate() tests ---

    [Fact]
    public void ClientToServer_Validate_BothActionAndError_Throws()
    {
        var msg = new ClientToServerMessage
        {
            Action = new ClientAction
            {
                Name = "click",
                SurfaceId = "s",
                SourceComponentId = "b",
                Timestamp = "2025-01-01T00:00:00Z",
                Context = JsonSerializer.SerializeToElement(new { }),
            },
            Error = new ClientError
            {
                Code = "ERR",
                SurfaceId = "s",
                Message = "fail",
            },
        };
        var act = () => msg.Validate();
        act.Should().Throw<A2UiMessageValidationException>().Which.Message.Should().Contain("both");
    }

    [Fact]
    public void ClientToServer_Validate_NeitherActionNorError_Throws()
    {
        var msg = new ClientToServerMessage();
        var act = () => msg.Validate();
        act.Should().Throw<A2UiMessageValidationException>().Which.Message.Should().Contain("exactly one");
    }

    [Fact]
    public void ClientToServer_Validate_ActionOnly_Succeeds()
    {
        var msg = new ClientToServerMessage
        {
            Action = new ClientAction
            {
                Name = "click",
                SurfaceId = "s",
                SourceComponentId = "b",
                Timestamp = "2025-01-01T00:00:00Z",
                Context = JsonSerializer.SerializeToElement(new { }),
            },
        };
        var act = () => msg.Validate();
        act.Should().NotThrow();
    }

    [Fact]
    public void ClientToServer_Validate_ErrorOnly_Succeeds()
    {
        var msg = new ClientToServerMessage
        {
            Error = new ClientError
            {
                Code = "ERR",
                SurfaceId = "s",
                Message = "fail",
            },
        };
        var act = () => msg.Validate();
        act.Should().NotThrow();
    }
}
