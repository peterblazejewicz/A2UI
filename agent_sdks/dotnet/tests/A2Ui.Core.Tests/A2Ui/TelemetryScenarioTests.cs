using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using A2Ui.Core.Components;
using A2Ui.Core.Messages;
using A2Ui.Core.Surfaces;
using A2Ui.TestHelpers;
using Microsoft.Extensions.Logging;

namespace A2Ui.Core.Tests.A2Ui;

/// <summary>
/// Phase 2 Slice C — representative telemetry scenarios that assert on
/// <see cref="TestLoggerProvider"/> captured entries and
/// <see cref="TestActivityListener"/> recorded spans for the SurfaceManager
/// hot path.
/// </summary>
public sealed class TelemetryScenarioTests
{
    private const string SurfaceManagerCategory = "A2Ui.Core.Surfaces.SurfaceManager";
    private const string CoreActivitySource = "A2Ui.Core";

    [Fact]
    public void Process_HappyPath_EmitsExpectedLogSitesAndSpans()
    {
        // Arrange
        using var provider = new TestLoggerProvider();
        using var factory = provider.CreateFactory();
        using var listener = new TestActivityListener(CoreActivitySource);
        var sm = new SurfaceManager(factory);

        var createMsg = new A2UiMessage
        {
            Version = "v0.9",
            CreateSurface = new CreateSurface
            {
                SurfaceId = "s-happy",
                CatalogId = "https://a2ui.org/specification/v0_9/basic_catalog.json",
                Theme = JsonSerializer.SerializeToElement(
                    new { primaryColor = "#112233", agentDisplayName = "Happy Agent" }
                ),
            },
        };
        var updateComponentsMsg = new A2UiMessage
        {
            Version = "v0.9",
            UpdateComponents = new UpdateComponents
            {
                SurfaceId = "s-happy",
                Components = [new ColumnComponent { Id = "root" }, new TextComponent { Id = "label", Parent = "root" }],
            },
        };
        var updateDataModelMsg = new A2UiMessage
        {
            Version = "v0.9",
            UpdateDataModel = new UpdateDataModel
            {
                SurfaceId = "s-happy",
                Path = "/greeting",
                Value = JsonSerializer.SerializeToElement("hello"),
            },
        };

        // Act
        sm.Process(createMsg);
        sm.Process(updateComponentsMsg);
        sm.Process(updateDataModelMsg);

        // Assert — log entries
        var entries = provider.Entries.Where(e => e.CategoryName == SurfaceManagerCategory).ToList();

        // MessageDispatched fires once per message
        Assert.Equal(3, entries.Count(e => e.EventId.Id == 9));

        var created = entries.SingleOrDefault(e => e.EventId.Id == 1);
        // SurfaceCreated (EventId 1) should fire exactly once
        Assert.NotNull(created);
        Assert.Equal(LogLevel.Information, created!.Level);
        Assert.Equal("s-happy", created.Properties["SurfaceId"]);
        Assert.Equal("https://a2ui.org/specification/v0_9/basic_catalog.json", created.Properties["CatalogId"]);
        Assert.Equal("#112233", created.Properties["PrimaryColor"]);
        Assert.Equal("Happy Agent", created.Properties["AgentDisplayName"]);

        var componentsUpdated = entries.SingleOrDefault(e => e.EventId.Id == 3);
        // ComponentsUpdated (EventId 3) should fire exactly once
        Assert.NotNull(componentsUpdated);
        Assert.Equal("s-happy", componentsUpdated!.Properties["SurfaceId"]);
        Assert.Equal(2, componentsUpdated.Properties["Count"]);
        Assert.Equal("root", componentsUpdated.Properties["RootComponentId"]);
        Assert.Equal("Column", componentsUpdated.Properties["RootComponentType"]);

        var dataModelUpdated = entries.SingleOrDefault(e => e.EventId.Id == 4);
        // DataModelUpdated (EventId 4) should fire exactly once
        Assert.NotNull(dataModelUpdated);
        Assert.Equal("s-happy", dataModelUpdated!.Properties["SurfaceId"]);
        Assert.Equal(1, dataModelUpdated.Properties["PathCount"]);
        Assert.Equal("greeting", dataModelUpdated.Properties["TopLevelKeys"]);

        // No ValidationFailed entries on the happy path
        Assert.DoesNotContain(entries, e => e.EventId.Id == 10);
        // No UnknownSurfaceOp entries on the happy path
        Assert.DoesNotContain(entries, e => e.EventId.Id == 6);
        // No RootComponentMissing entries on the happy path
        Assert.DoesNotContain(entries, e => e.EventId.Id == 7);

        // Assert — spans (filter by surface id to stay isolated from any
        // cross-test-class parallelism that might also write to A2Ui.Core).
        var spans = listener
            .StoppedActivities.Where(a =>
                a.Source.Name == CoreActivitySource && Equals(a.GetTagItem("a2ui.surface_id"), "s-happy")
            )
            .ToList();
        // Each dispatched message starts and stops exactly one span
        Assert.Equal(3, spans.Count);

        var createSpan = spans.Single(a => a.OperationName == "Surface.CreateSurface");
        Assert.Equal("CreateSurface", createSpan.GetTagItem("a2ui.message_type"));
        Assert.Equal(ActivityStatusCode.Unset, createSpan.Status);

        var updateComponentsSpan = spans.Single(a => a.OperationName == "Surface.UpdateComponents");
        Assert.Equal("UpdateComponents", updateComponentsSpan.GetTagItem("a2ui.message_type"));

        var updateDataModelSpan = spans.Single(a => a.OperationName == "Surface.UpdateDataModel");
        Assert.Equal("UpdateDataModel", updateDataModelSpan.GetTagItem("a2ui.message_type"));
    }

    [Fact]
    public void Process_ValidationFailure_LogsValidationFailedBeforeThrowing()
    {
        // Arrange
        using var provider = new TestLoggerProvider();
        using var factory = provider.CreateFactory();
        using var listener = new TestActivityListener(CoreActivitySource);
        var sm = new SurfaceManager(factory);

        // v0.8 is unsupported — Validate() will throw.
        // We still attach a CreateSurface so message.Operation resolves to a
        // well-known discriminator rather than "(empty)".
        var badMsg = new A2UiMessage
        {
            Version = "v0.8",
            CreateSurface = new CreateSurface { SurfaceId = "s-bad", CatalogId = "c" },
        };

        // Act
        var act = () => sm.Process(badMsg);

        // Assert — exception propagated
        Assert.Throws<A2UiMessageValidationException>(act);

        // Assert — log entries
        var entries = provider.Entries.Where(e => e.CategoryName == SurfaceManagerCategory).ToList();

        var dispatched = entries.SingleOrDefault(e => e.EventId.Id == 9);
        // MessageDispatched (EventId 9) fires before validation runs
        Assert.NotNull(dispatched);
        Assert.Equal("CreateSurface", dispatched!.Properties["MessageType"]);
        Assert.Equal("s-bad", dispatched.Properties["SurfaceId"]);

        var validationFailed = entries.SingleOrDefault(e => e.EventId.Id == 10);
        // ValidationFailed (EventId 10) should fire exactly once
        Assert.NotNull(validationFailed);
        Assert.Equal(LogLevel.Warning, validationFailed!.Level);
        Assert.Equal("CreateSurface", validationFailed.Properties["MessageType"]);
        Assert.Contains("ValidationError", validationFailed.Properties);
        var validationError = validationFailed.Properties["ValidationError"]?.ToString();
        Assert.NotNull(validationError);
        Assert.NotEmpty(validationError);
        Assert.Contains("v0.8", validationError);

        // No SurfaceCreated entry — validation blocked it before handlers ran.
        // Validation blocks SurfaceCreated
        Assert.DoesNotContain(entries, e => e.EventId.Id == 1);

        // Assert — span captured the error (filter by surface id to avoid
        // cross-test leakage from parallel test classes).
        var span = listener
            .StoppedActivities.Where(a =>
                a.Source.Name == CoreActivitySource && Equals(a.GetTagItem("a2ui.surface_id"), "s-bad")
            )
            .Single(a => a.OperationName == "Surface.CreateSurface");
        Assert.Equal("CreateSurface", span.GetTagItem("a2ui.message_type"));
        Assert.Equal(ActivityStatusCode.Error, span.Status);
    }

    [Fact]
    public void Process_UpdateComponentsWithoutRoot_LogsRootComponentMissingAndStillUpdates()
    {
        // Arrange
        using var provider = new TestLoggerProvider();
        using var factory = provider.CreateFactory();
        using var listener = new TestActivityListener(CoreActivitySource);
        var sm = new SurfaceManager(factory);

        sm.Process(
            new A2UiMessage
            {
                Version = "v0.9",
                CreateSurface = new CreateSurface { SurfaceId = "s-noroot", CatalogId = "c" },
            }
        );

        // Act — updateComponents with no component whose Id == "root".
        sm.Process(
            new A2UiMessage
            {
                Version = "v0.9",
                UpdateComponents = new UpdateComponents
                {
                    SurfaceId = "s-noroot",
                    Components = [new TextComponent { Id = "header" }, new TextComponent { Id = "body" }],
                },
            }
        );

        // Assert — log entries
        var entries = provider.Entries.Where(e => e.CategoryName == SurfaceManagerCategory).ToList();

        var missing = entries.SingleOrDefault(e => e.EventId.Id == 7);
        // RootComponentMissing (EventId 7) should fire exactly once
        Assert.NotNull(missing);
        Assert.Equal(LogLevel.Warning, missing!.Level);
        Assert.Equal("s-noroot", missing.Properties["SurfaceId"]);

        // ComponentsUpdated still fires — the missing-root condition is a warning, not a blocker.
        var componentsUpdated = entries.SingleOrDefault(e => e.EventId.Id == 3);
        // ComponentsUpdated (EventId 3) still fires when root is missing
        Assert.NotNull(componentsUpdated);
        Assert.Equal("s-noroot", componentsUpdated!.Properties["SurfaceId"]);
        Assert.Equal(2, componentsUpdated.Properties["Count"]);
        Assert.Equal("(none)", componentsUpdated.Properties["RootComponentId"]);
        Assert.Equal("(none)", componentsUpdated.Properties["RootComponentType"]);

        // The surface was actually mutated.
        var surface = sm.GetSurface("s-noroot");
        Assert.NotNull(surface);
        Assert.Equal(2, surface!.Components.Count);
        Assert.False(surface.Components.ContainsKey("root"));

        // Assert — span fired with message_type == "UpdateComponents" (filter
        // by surface id to avoid cross-test leakage from parallel test classes).
        var span = listener
            .StoppedActivities.Where(a =>
                a.Source.Name == CoreActivitySource && Equals(a.GetTagItem("a2ui.surface_id"), "s-noroot")
            )
            .Single(a => a.OperationName == "Surface.UpdateComponents");
        Assert.Equal("UpdateComponents", span.GetTagItem("a2ui.message_type"));
        Assert.Equal(ActivityStatusCode.Unset, span.Status);
    }
}
