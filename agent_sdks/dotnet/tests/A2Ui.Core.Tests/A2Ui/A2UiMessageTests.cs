using System.Text.Json;
using A2Ui.Core.Messages;

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

        Assert.Equal("v0.9", msg.Version);
        Assert.NotNull(msg.CreateSurface);
        Assert.Equal("main", msg.CreateSurface!.SurfaceId);
        Assert.Equal("https://a2ui.org/specification/v0_9/basic_catalog.json", msg.CreateSurface.CatalogId);
        Assert.NotNull(msg.CreateSurface.Theme);
        Assert.True(msg.CreateSurface.SendDataModel);
    }

    [Fact]
    public void DeleteSurface_RoundTrip()
    {
        var json = """{"version":"v0.9","deleteSurface":{"surfaceId":"main"}}""";

        var msg = JsonSerializer.Deserialize<A2UiMessage>(json, s_opts)!;

        Assert.NotNull(msg.DeleteSurface);
        Assert.Equal("main", msg.DeleteSurface!.SurfaceId);
    }

    [Fact]
    public void UpdateComponents_RoundTrip_WithComponentProperties()
    {
        var json =
            """{"version":"v0.9","updateComponents":{"surfaceId":"main","components":[{"id":"root","component":"Column"},{"id":"t1","component":"Text","text":"Hello","parent":"root"}]}}""";

        var msg = JsonSerializer.Deserialize<A2UiMessage>(json, s_opts)!;

        Assert.NotNull(msg.UpdateComponents);
        Assert.Equal(2, msg.UpdateComponents!.Components.Length);
        var textSv = Assert.IsType<DynamicValue.StringValue>(msg.UpdateComponents.Components[1].Text);
        Assert.Equal("Hello", textSv.Value);
    }

    [Fact]
    public void UpdateDataModel_RoundTrip_WithPath()
    {
        var json = """{"version":"v0.9","updateDataModel":{"surfaceId":"main","path":"/user/name","value":"Alice"}}""";

        var msg = JsonSerializer.Deserialize<A2UiMessage>(json, s_opts)!;

        Assert.NotNull(msg.UpdateDataModel);
        Assert.Equal("main", msg.UpdateDataModel!.SurfaceId);
        Assert.Equal("/user/name", msg.UpdateDataModel.Path);
        Assert.NotNull(msg.UpdateDataModel.Value);
    }

    [Fact]
    public void UpdateDataModel_NullPathAndValue_Deserializes()
    {
        var json = """{"version":"v0.9","updateDataModel":{"surfaceId":"main"}}""";

        var msg = JsonSerializer.Deserialize<A2UiMessage>(json, s_opts)!;

        Assert.NotNull(msg.UpdateDataModel);
        Assert.Null(msg.UpdateDataModel!.Path);
        Assert.Null(msg.UpdateDataModel.Value);
    }

    [Fact]
    public void ComponentAction_WithEvent_RoundTrip()
    {
        var json =
            """{"id":"btn1","component":"Button","action":{"event":{"name":"submit","context":{"date":{"path":"/reservation/date"}}}}}""";

        var comp = JsonSerializer.Deserialize<A2UiComponent>(json, s_opts)!;

        Assert.NotNull(comp.Action);
        Assert.NotNull(comp.Action!.Event);
        Assert.Equal("submit", comp.Action.Event!.Name);
        Assert.Contains("date", comp.Action.Event.Context!);
        var datePv = Assert.IsType<DynamicValue.PathValue>(comp.Action.Event.Context!["date"]);
        Assert.Equal("/reservation/date", datePv.DataPath);
    }

    [Fact]
    public void ComponentAction_WithFunctionCall_RoundTrip()
    {
        var json =
            """{"id":"link1","component":"Button","action":{"functionCall":{"call":"openUrl","args":{"url":"https://example.com"},"returnType":"void"}}}""";

        var comp = JsonSerializer.Deserialize<A2UiComponent>(json, s_opts)!;

        Assert.NotNull(comp.Action);
        Assert.NotNull(comp.Action!.FunctionCall);
        Assert.Equal("openUrl", comp.Action.FunctionCall!.Call);
        Assert.Equal("void", comp.Action.FunctionCall.ReturnType);
    }

    [Fact]
    public void DynamicValue_BoundPath_RoundTrip()
    {
        var json = """{"id":"t1","component":"Text","text":{"path":"/user/name"}}""";

        var comp = JsonSerializer.Deserialize<A2UiComponent>(json, s_opts)!;

        Assert.NotNull(comp.Text);
        var textPv = Assert.IsType<DynamicValue.PathValue>(comp.Text);
        Assert.Equal("/user/name", textPv.DataPath);
    }

    [Fact]
    public void ClientToServer_Action_RoundTrip()
    {
        var json =
            """{"version":"v0.9","action":{"name":"submit","surfaceId":"main","sourceComponentId":"btn1","timestamp":"2025-12-15T10:30:00Z","context":{"date":"2025-12-15"}}}""";

        var msg = JsonSerializer.Deserialize<ClientToServerMessage>(json, s_opts)!;

        Assert.Equal("v0.9", msg.Version);
        Assert.NotNull(msg.Action);
        Assert.Equal("submit", msg.Action!.Name);
        Assert.Equal("btn1", msg.Action.SourceComponentId);
        Assert.Equal("2025-12-15T10:30:00Z", msg.Action.Timestamp);
    }

    [Fact]
    public void ClientToServer_ValidationError_RoundTrip()
    {
        var json =
            """{"version":"v0.9","error":{"code":"VALIDATION_FAILED","surfaceId":"main","message":"Invalid date","path":"/components/0/value"}}""";

        var msg = JsonSerializer.Deserialize<ClientToServerMessage>(json, s_opts)!;

        Assert.NotNull(msg.Error);
        Assert.Equal("VALIDATION_FAILED", msg.Error!.Code);
        Assert.Equal("/components/0/value", msg.Error.Path);
    }

    [Fact]
    public void ClientToServer_GenericError_RoundTrip()
    {
        var json =
            """{"version":"v0.9","error":{"code":"NETWORK_ERROR","surfaceId":"main","message":"Connection lost"}}""";

        var msg = JsonSerializer.Deserialize<ClientToServerMessage>(json, s_opts)!;

        Assert.NotNull(msg.Error);
        Assert.Equal("NETWORK_ERROR", msg.Error!.Code);
        Assert.Null(msg.Error.Path);
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
        var ex = Assert.Throws<A2UiMessageValidationException>(act);
        Assert.Contains("version", ex.Message);
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
        var ex = Assert.Throws<A2UiMessageValidationException>(act);
        Assert.Contains("v0.8", ex.Message);
    }

    [Fact]
    public void Validate_ZeroOperations_Throws()
    {
        var msg = new A2UiMessage { Version = "v0.9" };
        var act = () => msg.Validate();
        var ex = Assert.Throws<A2UiMessageValidationException>(act);
        Assert.Contains("exactly one operation", ex.Message);
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
        var ex = Assert.Throws<A2UiMessageValidationException>(act);
        Assert.Contains("2 operations", ex.Message);
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
        act();
    }

    [Fact]
    public void OperationType_ReturnsCorrectDiscriminator()
    {
        var msg = new A2UiMessage
        {
            Version = "v0.9",
            UpdateComponents = new UpdateComponents { SurfaceId = "s", Components = [] },
        };
        Assert.Equal(A2UiOperationType.UpdateComponents, msg.Operation);
    }

    [Fact]
    public void OperationType_NoOperation_ReturnsNull()
    {
        var msg = new A2UiMessage { Version = "v0.9" };
        Assert.Null(msg.Operation);
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
        var ex = Assert.Throws<A2UiMessageValidationException>(act);
        Assert.Contains("both", ex.Message);
    }

    [Fact]
    public void ClientToServer_Validate_NeitherActionNorError_Throws()
    {
        var msg = new ClientToServerMessage();
        var act = () => msg.Validate();
        var ex = Assert.Throws<A2UiMessageValidationException>(act);
        Assert.Contains("exactly one", ex.Message);
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
        act();
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
        act();
    }
}
