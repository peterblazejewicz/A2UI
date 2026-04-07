using System.Linq;
using System.Text.Json;
using A2Ui.Core;
using A2Ui.Core.Messages;
using FluentAssertions;

namespace A2Ui.Core.Tests;

public sealed class SurfaceManagerTests
{
    [Fact]
    public void Process_CreateSurface_RaisesEvent()
    {
        var sm = new SurfaceManager();
        Surface? created = null;
        sm.SurfaceCreated += (_, e) => created = e.Surface;

        sm.Process(
            new A2UiMessage
            {
                Version = "v0.9",
                CreateSurface = new CreateSurface
                {
                    SurfaceId = "main",
                    CatalogId = "https://a2ui.org/specification/v0_9/basic_catalog.json",
                },
            }
        );

        created.Should().NotBeNull();
        created!.SurfaceId.Should().Be("main");
        created.CatalogId.Should().Be("https://a2ui.org/specification/v0_9/basic_catalog.json");
    }

    [Fact]
    public void Process_CreateSurface_StoresThemeAndSendDataModel()
    {
        var sm = new SurfaceManager();
        Surface? created = null;
        sm.SurfaceCreated += (_, e) => created = e.Surface;

        sm.Process(
            new A2UiMessage
            {
                Version = "v0.9",
                CreateSurface = new CreateSurface
                {
                    SurfaceId = "themed",
                    CatalogId = "test",
                    Theme = JsonSerializer.SerializeToElement(new { primaryColor = "#FF0000" }),
                    SendDataModel = true,
                },
            }
        );

        created!.Theme.Should().NotBeNull();
        created.SendDataModel.Should().BeTrue();
    }

    [Fact]
    public void Process_DuplicateCreate_IgnoresSecond()
    {
        var sm = new SurfaceManager();
        int createCount = 0;
        sm.SurfaceCreated += (_, _) => createCount++;

        var msg = new A2UiMessage
        {
            Version = "v0.9",
            CreateSurface = new CreateSurface { SurfaceId = "s1", CatalogId = "c" },
        };

        sm.Process(msg);
        sm.Process(msg);

        createCount.Should().Be(1);
    }

    [Fact]
    public void Process_DeleteSurface_RemovesSurface()
    {
        var sm = new SurfaceManager();
        Surface? deleted = null;
        sm.SurfaceDeleted += (_, e) => deleted = e.Surface;

        sm.Process(
            new A2UiMessage
            {
                Version = "v0.9",
                CreateSurface = new CreateSurface { SurfaceId = "s1", CatalogId = "c" },
            }
        );
        sm.Process(
            new A2UiMessage
            {
                Version = "v0.9",
                DeleteSurface = new DeleteSurface { SurfaceId = "s1" },
            }
        );

        deleted.Should().NotBeNull();
        deleted!.SurfaceId.Should().Be("s1");
        sm.GetSurface("s1").Should().BeNull();
    }

    [Fact]
    public void Process_DeleteNonexistent_NoOp()
    {
        var sm = new SurfaceManager();
        bool deleteFired = false;
        sm.SurfaceDeleted += (_, _) => deleteFired = true;

        sm.Process(
            new A2UiMessage
            {
                Version = "v0.9",
                DeleteSurface = new DeleteSurface { SurfaceId = "ghost" },
            }
        );

        deleteFired.Should().BeFalse();
    }

    [Fact]
    public void Process_UpdateComponents_StoresComponents()
    {
        var sm = new SurfaceManager();
        ComponentsUpdatedEventArgs? updatedArgs = null;
        sm.ComponentsUpdated += (_, e) => updatedArgs = e;

        sm.Process(
            new A2UiMessage
            {
                Version = "v0.9",
                CreateSurface = new CreateSurface { SurfaceId = "s1", CatalogId = "c" },
            }
        );
        sm.Process(
            new A2UiMessage
            {
                Version = "v0.9",
                UpdateComponents = new UpdateComponents
                {
                    SurfaceId = "s1",
                    Components =
                    [
                        new A2UiComponent { Id = "root", Component = "Column" },
                        new A2UiComponent
                        {
                            Id = "t1",
                            Component = "Text",
                            Parent = "root",
                        },
                    ],
                },
            }
        );

        updatedArgs.Should().NotBeNull();
        var surface = sm.GetSurface("s1");
        surface!.Components.Should().HaveCount(2);
        surface.Components["t1"].Component.Should().Be("Text");
    }

    [Fact]
    public void Process_UpdateComponents_MissingSurface_NoOp()
    {
        var sm = new SurfaceManager();
        bool fired = false;
        sm.ComponentsUpdated += (_, _) => fired = true;

        sm.Process(
            new A2UiMessage
            {
                Version = "v0.9",
                UpdateComponents = new UpdateComponents
                {
                    SurfaceId = "ghost",
                    Components = [new A2UiComponent { Id = "t1", Component = "Text" }],
                },
            }
        );

        fired.Should().BeFalse();
    }

    [Fact]
    public void Process_UpdateDataModel_AppliesUpdate()
    {
        var sm = new SurfaceManager();
        bool fired = false;
        sm.DataModelUpdated += (_, _) => fired = true;

        sm.Process(
            new A2UiMessage
            {
                Version = "v0.9",
                CreateSurface = new CreateSurface { SurfaceId = "s1", CatalogId = "c" },
            }
        );
        sm.Process(
            new A2UiMessage
            {
                Version = "v0.9",
                UpdateDataModel = new UpdateDataModel
                {
                    SurfaceId = "s1",
                    Path = "/user/name",
                    Value = JsonSerializer.SerializeToElement("Alice"),
                },
            }
        );

        fired.Should().BeTrue();
        var surface = sm.GetSurface("s1")!;
        surface.DataModel.Resolve(DynamicValue.FromPath("/user/name")).Should().Be("Alice");
    }

    [Fact]
    public void Process_UpdateDataModel_MissingSurface_NoOp()
    {
        var sm = new SurfaceManager();
        bool fired = false;
        sm.DataModelUpdated += (_, _) => fired = true;

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
        );

        fired.Should().BeFalse();
    }

    [Fact]
    public void Process_EmptyMessage_ThrowsValidation()
    {
        var sm = new SurfaceManager();

        var act = () => sm.Process(new A2UiMessage());

        act.Should().Throw<A2UiMessageValidationException>();
    }

    [Fact]
    public void GetSurface_ExistingSurface_ReturnsSurface()
    {
        var sm = new SurfaceManager();
        sm.Process(
            new A2UiMessage
            {
                Version = "v0.9",
                CreateSurface = new CreateSurface { SurfaceId = "s1", CatalogId = "c" },
            }
        );

        sm.GetSurface("s1").Should().NotBeNull();
        sm.GetSurface("nope").Should().BeNull();
    }

    [Fact]
    public void Surface_GetRootComponents_ReturnsNullParentComponents()
    {
        var sm = new SurfaceManager();
        sm.Process(
            new A2UiMessage
            {
                Version = "v0.9",
                CreateSurface = new CreateSurface { SurfaceId = "s1", CatalogId = "c" },
            }
        );
        sm.Process(
            new A2UiMessage
            {
                Version = "v0.9",
                UpdateComponents = new UpdateComponents
                {
                    SurfaceId = "s1",
                    Components =
                    [
                        new A2UiComponent { Id = "root", Component = "Column" },
                        new A2UiComponent
                        {
                            Id = "child1",
                            Component = "Text",
                            Parent = "root",
                        },
                        new A2UiComponent
                        {
                            Id = "child2",
                            Component = "Text",
                            Parent = "root",
                        },
                    ],
                },
            }
        );

        var surface = sm.GetSurface("s1")!;
        var roots = surface.GetRootComponents().ToList();
        roots.Should().ContainSingle();
        roots[0].Id.Should().Be("root");
    }

    [Fact]
    public void Surface_GetRootComponents_V09ForwardRef_FindsRootById()
    {
        var sm = new SurfaceManager();
        sm.Process(
            new A2UiMessage
            {
                Version = "v0.9",
                CreateSurface = new CreateSurface { SurfaceId = "s1", CatalogId = "c" },
            }
        );
        sm.Process(
            new A2UiMessage
            {
                Version = "v0.9",
                UpdateComponents = new UpdateComponents
                {
                    SurfaceId = "s1",
                    Components =
                    [
                        new A2UiComponent
                        {
                            Id = "root",
                            Component = "Column",
                            Children = ChildList.FromIds("t1", "t2"),
                        },
                        new A2UiComponent { Id = "t1", Component = "Text" },
                        new A2UiComponent { Id = "t2", Component = "Text" },
                    ],
                },
            }
        );

        var roots = sm.GetSurface("s1")!.GetRootComponents().ToList();
        roots.Should().ContainSingle();
        roots[0].Id.Should().Be("root");
    }

    [Fact]
    public void Surface_GetRootComponents_NoExplicitRoot_ExcludesReferencedChildren()
    {
        var sm = new SurfaceManager();
        sm.Process(
            new A2UiMessage
            {
                Version = "v0.9",
                CreateSurface = new CreateSurface { SurfaceId = "s1", CatalogId = "c" },
            }
        );
        sm.Process(
            new A2UiMessage
            {
                Version = "v0.9",
                UpdateComponents = new UpdateComponents
                {
                    SurfaceId = "s1",
                    Components =
                    [
                        new A2UiComponent
                        {
                            Id = "col1",
                            Component = "Column",
                            Children = ChildList.FromIds("t1", "t2"),
                        },
                        new A2UiComponent { Id = "t1", Component = "Text" },
                        new A2UiComponent { Id = "t2", Component = "Text" },
                    ],
                },
            }
        );

        var roots = sm.GetSurface("s1")!.GetRootComponents().ToList();
        roots.Should().ContainSingle();
        roots[0].Id.Should().Be("col1");
    }
}
