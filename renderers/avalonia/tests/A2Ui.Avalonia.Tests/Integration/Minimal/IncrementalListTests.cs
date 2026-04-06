using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using FluentAssertions;
using Xunit;

namespace A2Ui.Avalonia.Tests.Integration.Minimal;

/// <summary>
/// Integration tests for minimal/7_incremental.json.
/// Column with template children from /restaurants array, data model with 4 restaurants.
/// Template expansion is stubbed (gap).
/// </summary>
public sealed class IncrementalListTests
{
    [AvaloniaFact]
    public void Surface_HasExpectedComponents()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/7_incremental.json");

        result.Surface.Components.Keys.Should().Contain("root");
        result.Surface.Components.Keys.Should().Contain("restaurant_card");
        result.Surface.Components.Keys.Should().Contain("rc_title");
        result.Surface.Components.Keys.Should().Contain("rc_subtitle");
        result.Surface.Components.Keys.Should().Contain("rc_address");
        result.Surface.Components.Keys.Should().Contain("rc_button");
        result.Surface.Components.Keys.Should().Contain("rc_button_label");
    }

    [AvaloniaFact]
    public void DataModel_HasRestaurantsArray_With4Items()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/7_incremental.json");

        string json = result.Surface.DataModel.ToJson();
        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement restaurants = doc.RootElement.GetProperty("restaurants");
        restaurants.GetArrayLength().Should().Be(4);
    }

    [AvaloniaFact]
    public void DataModel_FourthRestaurant_IsSpiceRoute()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/7_incremental.json");

        string json = result.Surface.DataModel.ToJson();
        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement restaurants = doc.RootElement.GetProperty("restaurants");
        JsonElement fourth = restaurants[3];
        fourth.GetProperty("title").GetString().Should().Be("Spice Route");
    }

    [AvaloniaFact]
    public void RootControl_Renders_WithoutException()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/7_incremental.json");

        result.RootControl.Should().NotBeNull();
    }

    [AvaloniaFact]
    [Trait("Gap", "TemplateChildren")]
    public void TemplateStub_RendersOnlyTemplateComponent_NotExpandedCards()
    {
        // Template children expansion is stubbed: the root Column renders the
        // restaurant_card template once instead of expanding per-item.
        // This test documents the current behavior.
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/7_incremental.json");

        // The root is rendered (Column with template children stub)
        // With the stub, we expect to find TextBlocks from the template component
        // but NOT 4 expanded copies of the card.
        List<Button> buttons = GalleryTestHelper.FindAll<Button>(result.RootControl);
        // Stub renders template once, so at most 1 button (the "Book now" button)
        buttons.Count.Should().BeLessThanOrEqualTo(1,
            "template expansion is stubbed; only the template itself renders, not per-item copies");
    }
}
