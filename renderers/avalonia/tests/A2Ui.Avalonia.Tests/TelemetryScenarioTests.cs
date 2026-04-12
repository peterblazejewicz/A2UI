using A2Ui.Avalonia.Catalog;
using A2Ui.Core;
using A2Ui.Core.Messages;
using A2Ui.TestHelpers;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using FluentAssertions;
using Microsoft.Extensions.Logging;

namespace A2Ui.Avalonia.Tests;

/// <summary>
/// Phase 2 Slice C — representative telemetry scenario exercising the
/// renderer hot path when the agent references a component type that is
/// not registered in the catalog.
/// </summary>
public sealed class TelemetryScenarioTests
{
    private const string RendererCategory = "A2Ui.Avalonia.A2UiRenderer";
    private const string CatalogRegistryCategory = "A2Ui.Avalonia.Catalog.CatalogRegistry";
    private const string RendererActivitySource = "A2Ui.Avalonia.Renderer";

    [AvaloniaFact]
    public void Render_UnknownComponentType_LogsMissAndRendersFallbackTextBlock()
    {
        // Arrange
        using var provider = new TestLoggerProvider();
        using var factory = provider.CreateFactory();
        using var listener = new TestActivityListener(RendererActivitySource);

        var catalog = CatalogRegistry.CreateDefault(factory);
        var renderer = new A2UiRenderer(catalog, functionRegistry: null, loggerFactory: factory);

        var sm = new SurfaceManager(factory);
        sm.Process(
            new A2UiMessage
            {
                Version = "v0.9",
                CreateSurface = new CreateSurface { SurfaceId = "s-unknown", CatalogId = "c" },
            }
        );
        sm.Process(
            new A2UiMessage
            {
                Version = "v0.9",
                UpdateComponents = new UpdateComponents
                {
                    SurfaceId = "s-unknown",
                    Components = [new A2UiComponent { Id = "root", Component = "NotARealComponent" }],
                },
            }
        );
        Surface surface = sm.GetSurface("s-unknown")!;

        // Act
        Control rendered = renderer.Render(surface);

        // Assert — fallback control
        rendered.Should().BeOfType<TextBlock>();
        ((TextBlock)rendered).Text.Should().Be("[Unknown component: NotARealComponent]");

        // Assert — CatalogRegistry lookup-miss entry (EventId 2)
        var catalogMiss = provider
            .Entries.Where(e => e.CategoryName == CatalogRegistryCategory && e.EventId.Id == 2)
            .ToList();
        catalogMiss.Should().NotBeEmpty("CatalogRegistry should log a lookup miss for unknown types");
        var miss = catalogMiss[0];
        miss.Level.Should().Be(LogLevel.Warning);
        miss.Properties["ComponentType"].Should().Be("NotARealComponent");

        // Assert — renderer fallback entry (EventId 12)
        var rendererFallback = provider
            .Entries.Where(e => e.CategoryName == RendererCategory && e.EventId.Id == 12)
            .ToList();
        rendererFallback.Should().NotBeEmpty("A2UiRenderer should log UnknownComponentTypeRendered");
        var fallback = rendererFallback[0];
        fallback.Level.Should().Be(LogLevel.Warning);
        fallback.Properties["ComponentId"].Should().Be("root");
        fallback.Properties["ComponentType"].Should().Be("NotARealComponent");

        // Assert — Renderer.Render span carries the surface id tag (filter by
        // surface id to avoid cross-test leakage from parallel test classes,
        // since ActivityListener subscription is global).
        var span = listener
            .StoppedActivities.Where(a =>
                a.Source.Name == RendererActivitySource && Equals(a.GetTagItem("a2ui.surface_id"), "s-unknown")
            )
            .Single(a => a.OperationName == "Renderer.Render");
        span.GetTagItem("a2ui.catalog_id").Should().Be("c");
    }
}
