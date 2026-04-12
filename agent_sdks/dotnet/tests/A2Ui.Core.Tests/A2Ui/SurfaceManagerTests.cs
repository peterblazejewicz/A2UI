using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using A2Ui.Core.Components;
using A2Ui.Core.Messages;

namespace A2Ui.Core.Tests.A2Ui;

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

        Assert.NotNull(created);
        Assert.Equal("main", created!.SurfaceId);
        Assert.Equal("https://a2ui.org/specification/v0_9/basic_catalog.json", created.CatalogId);
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

        Assert.NotNull(created!.Theme);
        Assert.True(created.SendDataModel);
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

        Assert.Equal(1, createCount);
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

        Assert.NotNull(deleted);
        Assert.Equal("s1", deleted!.SurfaceId);
        Assert.Null(sm.GetSurface("s1"));
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

        Assert.False(deleteFired);
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
                        new ColumnComponent { Id = "root" },
                        new TextComponent { Id = "t1", Parent = "root" },
                    ],
                },
            }
        );

        Assert.NotNull(updatedArgs);
        var surface = sm.GetSurface("s1");
        Assert.Equal(2, surface!.Components.Count);
        Assert.Equal("Text", surface.Components["t1"].Component);
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
                    Components = [new TextComponent { Id = "t1" }],
                },
            }
        );

        Assert.False(fired);
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

        Assert.True(fired);
        var surface = sm.GetSurface("s1")!;
        Assert.Equal("Alice", surface.DataModel.Resolve(DynamicValue.FromPath("/user/name")));
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

        Assert.False(fired);
    }

    [Fact]
    public void Process_EmptyMessage_ThrowsValidation()
    {
        var sm = new SurfaceManager();

        var act = () => sm.Process(new A2UiMessage());

        Assert.Throws<A2UiMessageValidationException>(act);
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

        Assert.NotNull(sm.GetSurface("s1"));
        Assert.Null(sm.GetSurface("nope"));
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
                        new ColumnComponent { Id = "root" },
                        new TextComponent { Id = "child1", Parent = "root" },
                        new TextComponent { Id = "child2", Parent = "root" },
                    ],
                },
            }
        );

        var surface = sm.GetSurface("s1")!;
        var roots = surface.GetRootComponents().ToList();
        var single = Assert.Single(roots);
        Assert.Equal("root", single.Id);
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
                        new ColumnComponent { Id = "root", Children = ChildList.FromIds("t1", "t2") },
                        new TextComponent { Id = "t1" },
                        new TextComponent { Id = "t2" },
                    ],
                },
            }
        );

        var roots = sm.GetSurface("s1")!.GetRootComponents().ToList();
        var single = Assert.Single(roots);
        Assert.Equal("root", single.Id);
    }

    [Fact]
    public void Clear_RemovesAllSurfaces_FiresDeletedForEach()
    {
        var sm = new SurfaceManager();
        var deletedIds = new List<string>();
        sm.SurfaceDeleted += (_, e) => deletedIds.Add(e.Surface.SurfaceId);

        foreach (var id in new[] { "s1", "s2", "s3" })
        {
            sm.Process(
                new A2UiMessage
                {
                    Version = "v0.9",
                    CreateSurface = new CreateSurface { SurfaceId = id, CatalogId = "c" },
                }
            );
        }

        sm.Clear();

        Assert.Equal(3, deletedIds.Count);
        Assert.Contains("s1", deletedIds);
        Assert.Contains("s2", deletedIds);
        Assert.Contains("s3", deletedIds);
        Assert.Null(sm.GetSurface("s1"));
        Assert.Null(sm.GetSurface("s2"));
        Assert.Null(sm.GetSurface("s3"));
    }

    [Fact]
    public void Clear_EmptyManager_NoEventsRaised()
    {
        var sm = new SurfaceManager();
        bool fired = false;
        sm.SurfaceDeleted += (_, _) => fired = true;

        sm.Clear();

        Assert.False(fired);
    }

    [Fact]
    public void Clear_AllowsRecreateAfterClear()
    {
        var sm = new SurfaceManager();
        sm.Process(
            new A2UiMessage
            {
                Version = "v0.9",
                CreateSurface = new CreateSurface { SurfaceId = "s1", CatalogId = "c" },
            }
        );

        sm.Clear();

        Surface? recreated = null;
        sm.SurfaceCreated += (_, e) => recreated = e.Surface;
        sm.Process(
            new A2UiMessage
            {
                Version = "v0.9",
                CreateSurface = new CreateSurface { SurfaceId = "s1", CatalogId = "c" },
            }
        );

        Assert.NotNull(recreated);
        Assert.Equal("s1", recreated!.SurfaceId);
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
                        new ColumnComponent { Id = "col1", Children = ChildList.FromIds("t1", "t2") },
                        new TextComponent { Id = "t1" },
                        new TextComponent { Id = "t2" },
                    ],
                },
            }
        );

        var roots = sm.GetSurface("s1")!.GetRootComponents().ToList();
        var single = Assert.Single(roots);
        Assert.Equal("col1", single.Id);
    }
}
