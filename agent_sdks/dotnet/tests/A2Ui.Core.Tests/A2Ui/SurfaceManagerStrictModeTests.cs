using System.Text.Json;
using A2Ui.Core.Bindings;
using A2Ui.Core.Components;
using A2Ui.Core.Messages;
using A2Ui.Core.Surfaces;

namespace A2Ui.Core.Tests.A2Ui;

public sealed class SurfaceManagerStrictModeTests
{
    private static readonly SurfaceManagerOptions s_strict = new() { StrictMode = true };

    private static A2UiMessage CreateMsg(string surfaceId) =>
        new()
        {
            Version = "v0.9",
            CreateSurface = new CreateSurface { SurfaceId = surfaceId, CatalogId = "basic" },
        };

    [Fact]
    public void Process_DuplicateCreate_StrictMode_Throws()
    {
        var sm = new SurfaceManager(options: s_strict);
        sm.Process(CreateMsg("main"));

        var ex = Assert.Throws<A2UiMessageValidationException>(() => sm.Process(CreateMsg("main")));

        Assert.Contains("Duplicate createSurface", ex.Message);
        Assert.Contains("main", ex.Message);
    }

    [Fact]
    public void Process_DuplicateCreate_LenientMode_NoThrow()
    {
        var sm = new SurfaceManager();
        sm.Process(CreateMsg("main"));

        // No throw — lenient path is preserved for default-constructed SurfaceManager
        sm.Process(CreateMsg("main"));

        Assert.NotNull(sm.GetSurface("main"));
    }

    [Fact]
    public void Process_DeleteUnknownSurface_StrictMode_Throws()
    {
        var sm = new SurfaceManager(options: s_strict);

        var ex = Assert.Throws<A2UiMessageValidationException>(() =>
            sm.Process(
                new A2UiMessage
                {
                    Version = "v0.9",
                    DeleteSurface = new DeleteSurface { SurfaceId = "ghost" },
                }
            )
        );

        Assert.Contains("unknown surface", ex.Message);
        Assert.Contains("ghost", ex.Message);
    }

    [Fact]
    public void Process_UpdateComponentsUnknownSurface_StrictMode_Throws()
    {
        var sm = new SurfaceManager(options: s_strict);

        var ex = Assert.Throws<A2UiMessageValidationException>(() =>
            sm.Process(
                new A2UiMessage
                {
                    Version = "v0.9",
                    UpdateComponents = new UpdateComponents
                    {
                        SurfaceId = "ghost",
                        Components = [new TextComponent { Id = "root", Text = DynamicValue.FromString("hi") }],
                    },
                }
            )
        );

        Assert.Contains("updateComponents", ex.Message);
        Assert.Contains("ghost", ex.Message);
    }

    [Fact]
    public void Process_UpdateComponentsMissingRoot_StrictMode_Throws()
    {
        var sm = new SurfaceManager(options: s_strict);
        sm.Process(CreateMsg("main"));

        var ex = Assert.Throws<A2UiMessageValidationException>(() =>
            sm.Process(
                new A2UiMessage
                {
                    Version = "v0.9",
                    UpdateComponents = new UpdateComponents
                    {
                        SurfaceId = "main",
                        // Intentionally no component with id "root" — spec violation.
                        Components = [new TextComponent { Id = "only", Text = DynamicValue.FromString("hi") }],
                    },
                }
            )
        );

        Assert.Contains("no 'root' component", ex.Message);
    }

    [Fact]
    public void Process_UpdateDataModelUnknownSurface_StrictMode_Throws()
    {
        var sm = new SurfaceManager(options: s_strict);

        var ex = Assert.Throws<A2UiMessageValidationException>(() =>
            sm.Process(
                new A2UiMessage
                {
                    Version = "v0.9",
                    UpdateDataModel = new UpdateDataModel
                    {
                        SurfaceId = "ghost",
                        Path = "/x",
                        Value = JsonSerializer.SerializeToElement(1),
                    },
                }
            )
        );

        Assert.Contains("updateDataModel", ex.Message);
        Assert.Contains("ghost", ex.Message);
    }

    [Fact]
    public void Process_UpdateDataModelMalformedPath_StrictMode_ThrowsWithInner()
    {
        var sm = new SurfaceManager(options: s_strict);
        sm.Process(CreateMsg("main"));
        // Seed a scalar that can't be traversed through.
        sm.Process(
            new A2UiMessage
            {
                Version = "v0.9",
                UpdateDataModel = new UpdateDataModel
                {
                    SurfaceId = "main",
                    Path = "/user",
                    Value = JsonSerializer.SerializeToElement("Alice"),
                },
            }
        );

        var ex = Assert.Throws<A2UiMessageValidationException>(() =>
            sm.Process(
                new A2UiMessage
                {
                    Version = "v0.9",
                    UpdateDataModel = new UpdateDataModel
                    {
                        SurfaceId = "main",
                        Path = "/user/name",
                        Value = JsonSerializer.SerializeToElement("Bob"),
                    },
                }
            )
        );

        Assert.Contains("updateDataModel", ex.Message);
        Assert.IsType<JsonException>(ex.InnerException);
    }

    [Fact]
    public void Process_UpdateDataModelMalformedPath_LenientMode_LogsAndContinues()
    {
        var sm = new SurfaceManager();
        sm.Process(CreateMsg("main"));
        sm.Process(
            new A2UiMessage
            {
                Version = "v0.9",
                UpdateDataModel = new UpdateDataModel
                {
                    SurfaceId = "main",
                    Path = "/user",
                    Value = JsonSerializer.SerializeToElement("Alice"),
                },
            }
        );

        // Lenient: malformed update is swallowed, previous scalar survives.
        sm.Process(
            new A2UiMessage
            {
                Version = "v0.9",
                UpdateDataModel = new UpdateDataModel
                {
                    SurfaceId = "main",
                    Path = "/user/name",
                    Value = JsonSerializer.SerializeToElement("Bob"),
                },
            }
        );

        Assert.Equal("Alice", sm.GetSurface("main")!.DataModel.Resolve(DynamicValue.FromPath("/user")));
    }
}
