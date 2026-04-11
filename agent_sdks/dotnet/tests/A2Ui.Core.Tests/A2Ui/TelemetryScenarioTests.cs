using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using A2Ui.Core;
using A2Ui.Core.Messages;
using A2Ui.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;

namespace A2Ui.Core.Tests;

/// <summary>
/// Phase 2 Slice C — representative telemetry scenarios that assert on
/// <see cref="TestLoggerProvider"/> captured entries and
/// <see cref="TestActivityListener"/> recorded spans for the SurfaceManager
/// hot path.
/// </summary>
public sealed class TelemetryScenarioTests
{
    private const string SurfaceManagerCategory = "A2Ui.Core.SurfaceManager";
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
                Components =
                [
                    new A2UiComponent { Id = "root", Component = "Column" },
                    new A2UiComponent
                    {
                        Id = "label",
                        Component = "Text",
                        Parent = "root",
                    },
                ],
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

        entries.Count(e => e.EventId.Id == 9).Should().Be(3, "MessageDispatched fires once per message");

        var created = entries.SingleOrDefault(e => e.EventId.Id == 1);
        created.Should().NotBeNull("SurfaceCreated (EventId 1) should fire exactly once");
        created!.Level.Should().Be(LogLevel.Information);
        created.Properties["SurfaceId"].Should().Be("s-happy");
        created.Properties["CatalogId"].Should().Be("https://a2ui.org/specification/v0_9/basic_catalog.json");
        created.Properties["PrimaryColor"].Should().Be("#112233");
        created.Properties["AgentDisplayName"].Should().Be("Happy Agent");

        var componentsUpdated = entries.SingleOrDefault(e => e.EventId.Id == 3);
        componentsUpdated.Should().NotBeNull("ComponentsUpdated (EventId 3) should fire exactly once");
        componentsUpdated!.Properties["SurfaceId"].Should().Be("s-happy");
        componentsUpdated.Properties["Count"].Should().Be(2);
        componentsUpdated.Properties["RootComponentId"].Should().Be("root");
        componentsUpdated.Properties["RootComponentType"].Should().Be("Column");

        var dataModelUpdated = entries.SingleOrDefault(e => e.EventId.Id == 4);
        dataModelUpdated.Should().NotBeNull("DataModelUpdated (EventId 4) should fire exactly once");
        dataModelUpdated!.Properties["SurfaceId"].Should().Be("s-happy");
        dataModelUpdated.Properties["PathCount"].Should().Be(1);
        dataModelUpdated.Properties["TopLevelKeys"].Should().Be("greeting");

        entries.Any(e => e.EventId.Id == 10).Should().BeFalse("no ValidationFailed entries on the happy path");
        entries.Any(e => e.EventId.Id == 6).Should().BeFalse("no UnknownSurfaceOp entries on the happy path");
        entries.Any(e => e.EventId.Id == 7).Should().BeFalse("no RootComponentMissing entries on the happy path");

        // Assert — spans (filter by surface id to stay isolated from any
        // cross-test-class parallelism that might also write to A2Ui.Core).
        var spans = listener
            .StoppedActivities.Where(a =>
                a.Source.Name == CoreActivitySource && Equals(a.GetTagItem("a2ui.surface_id"), "s-happy")
            )
            .ToList();
        spans.Should().HaveCount(3, "each dispatched message starts and stops exactly one span");

        var createSpan = spans.Single(a => a.OperationName == "Surface.CreateSurface");
        createSpan.GetTagItem("a2ui.message_type").Should().Be("CreateSurface");
        createSpan.Status.Should().Be(ActivityStatusCode.Unset);

        var updateComponentsSpan = spans.Single(a => a.OperationName == "Surface.UpdateComponents");
        updateComponentsSpan.GetTagItem("a2ui.message_type").Should().Be("UpdateComponents");

        var updateDataModelSpan = spans.Single(a => a.OperationName == "Surface.UpdateDataModel");
        updateDataModelSpan.GetTagItem("a2ui.message_type").Should().Be("UpdateDataModel");
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
        act.Should().Throw<A2UiMessageValidationException>();

        // Assert — log entries
        var entries = provider.Entries.Where(e => e.CategoryName == SurfaceManagerCategory).ToList();

        var dispatched = entries.SingleOrDefault(e => e.EventId.Id == 9);
        dispatched.Should().NotBeNull("MessageDispatched (EventId 9) fires before validation runs");
        dispatched!.Properties["MessageType"].Should().Be("CreateSurface");
        dispatched.Properties["SurfaceId"].Should().Be("s-bad");

        var validationFailed = entries.SingleOrDefault(e => e.EventId.Id == 10);
        validationFailed.Should().NotBeNull("ValidationFailed (EventId 10) should fire exactly once");
        validationFailed!.Level.Should().Be(LogLevel.Warning);
        validationFailed.Properties["MessageType"].Should().Be("CreateSurface");
        validationFailed.Properties.Should().ContainKey("ValidationError");
        var validationError = validationFailed.Properties["ValidationError"]?.ToString();
        validationError.Should().NotBeNullOrEmpty().And.Subject.Should().Contain("v0.8");

        // No SurfaceCreated entry — validation blocked it before handlers ran.
        entries.Any(e => e.EventId.Id == 1).Should().BeFalse("validation blocks SurfaceCreated");

        // Assert — span captured the error (filter by surface id to avoid
        // cross-test leakage from parallel test classes).
        var span = listener
            .StoppedActivities.Where(a =>
                a.Source.Name == CoreActivitySource && Equals(a.GetTagItem("a2ui.surface_id"), "s-bad")
            )
            .Single(a => a.OperationName == "Surface.CreateSurface");
        span.GetTagItem("a2ui.message_type").Should().Be("CreateSurface");
        span.Status.Should().Be(ActivityStatusCode.Error);
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
                    Components =
                    [
                        new A2UiComponent { Id = "header", Component = "Text" },
                        new A2UiComponent { Id = "body", Component = "Text" },
                    ],
                },
            }
        );

        // Assert — log entries
        var entries = provider.Entries.Where(e => e.CategoryName == SurfaceManagerCategory).ToList();

        var missing = entries.SingleOrDefault(e => e.EventId.Id == 7);
        missing.Should().NotBeNull("RootComponentMissing (EventId 7) should fire exactly once");
        missing!.Level.Should().Be(LogLevel.Warning);
        missing.Properties["SurfaceId"].Should().Be("s-noroot");

        // ComponentsUpdated still fires — the missing-root condition is a warning, not a blocker.
        var componentsUpdated = entries.SingleOrDefault(e => e.EventId.Id == 3);
        componentsUpdated.Should().NotBeNull("ComponentsUpdated (EventId 3) still fires when root is missing");
        componentsUpdated!.Properties["SurfaceId"].Should().Be("s-noroot");
        componentsUpdated.Properties["Count"].Should().Be(2);
        componentsUpdated.Properties["RootComponentId"].Should().Be("(none)");
        componentsUpdated.Properties["RootComponentType"].Should().Be("(none)");

        // The surface was actually mutated.
        var surface = sm.GetSurface("s-noroot");
        surface.Should().NotBeNull();
        surface!.Components.Should().HaveCount(2);
        surface.Components.ContainsKey("root").Should().BeFalse();

        // Assert — span fired with message_type == "UpdateComponents" (filter
        // by surface id to avoid cross-test leakage from parallel test classes).
        var span = listener
            .StoppedActivities.Where(a =>
                a.Source.Name == CoreActivitySource && Equals(a.GetTagItem("a2ui.surface_id"), "s-noroot")
            )
            .Single(a => a.OperationName == "Surface.UpdateComponents");
        span.GetTagItem("a2ui.message_type").Should().Be("UpdateComponents");
        span.Status.Should().Be(ActivityStatusCode.Unset);
    }
}
