using System.Text.Json;
using A2Ui.Core.Messages;
using A2Ui.Core.Surfaces;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Xunit;

namespace A2Ui.Avalonia.Tests.Integration.Minimal;

/// <summary>
/// Integration tests for minimal/7_incremental.json.
/// Column with template children from /restaurants array, data model with 4 restaurants.
/// Verifies template expansion renders one card per array item with scoped path resolution.
/// </summary>
public sealed class IncrementalListTests
{
    [AvaloniaFact]
    public void Surface_HasExpectedComponents()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/7_incremental.json");

        Assert.Contains("root", result.Surface.Components.Keys);
        Assert.Contains("restaurant_card", result.Surface.Components.Keys);
        Assert.Contains("rc_title", result.Surface.Components.Keys);
        Assert.Contains("rc_subtitle", result.Surface.Components.Keys);
        Assert.Contains("rc_address", result.Surface.Components.Keys);
        Assert.Contains("rc_button", result.Surface.Components.Keys);
        Assert.Contains("rc_button_label", result.Surface.Components.Keys);
    }

    [AvaloniaFact]
    public void DataModel_HasRestaurantsArray_With4Items()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/7_incremental.json");

        int length = result.Surface.DataModel.GetArrayLength("/restaurants");
        Assert.Equal(4, length);
    }

    [AvaloniaFact]
    public void RootControl_HasFourChildren_OnePerRestaurant()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/7_incremental.json");

        // Root is a Column (StackPanel) with template children expanded
        List<Control> rootChildren = GalleryTestHelper.GetChildren(result.RootControl).ToList();
        Assert.Equal(4, rootChildren.Count);
    }

    [AvaloniaFact]
    public void TemplateExpansion_ResolvesRestaurantTitles()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/7_incremental.json");

        List<TextBlock> textBlocks = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        List<string> allTexts = textBlocks.Select(tb => tb.Text).Where(t => t is not null).Cast<string>().ToList();

        Assert.Contains("The Golden Fork", allTexts);
        Assert.Contains("Ocean's Bounty", allTexts);
        Assert.Contains("Pizzeria Roma", allTexts);
        Assert.Contains("Spice Route", allTexts);
    }

    [AvaloniaFact]
    public void TemplateExpansion_ResolvesAddresses()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/7_incremental.json");

        List<TextBlock> textBlocks = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        List<string> allTexts = textBlocks.Select(tb => tb.Text).Where(t => t is not null).Cast<string>().ToList();

        Assert.Contains("123 Gastronomy Lane", allTexts);
        Assert.Contains("456 Shoreline Dr", allTexts);
        Assert.Contains("789 Napoli Way", allTexts);
        Assert.Contains("101 Silk Road St", allTexts);
    }

    [AvaloniaFact]
    public void TemplateExpansion_EachCardHasBookNowButton()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/7_incremental.json");

        List<Button> buttons = GalleryTestHelper.FindAll<Button>(result.RootControl);
        Assert.Equal(4, buttons.Count);
    }

    [AvaloniaFact]
    public void TemplateExpansion_BookNowButton_FiresActionWithScopedContext()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/7_incremental.json");

        // Click the first "Book now" button
        List<Button> buttons = GalleryTestHelper.FindAll<Button>(result.RootControl);
        Assert.NotEmpty(buttons);

        GalleryTestHelper.ClickButton(buttons[0]);

        Assert.Single(result.ActionLog);
        Assert.Equal("book_now", result.ActionLog[0].EventName);

        // The action context should have resolved the scoped "title" path
        // for the first restaurant (The Golden Fork)
        var context = Assert.IsType<Dictionary<string, string?>>(result.ActionLog[0].Payload);
        Assert.Equal("The Golden Fork", context["restaurantName"]);
    }

    [AvaloniaFact]
    public void TemplateExpansion_EmptyArray_ProducesZeroChildren()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/7_incremental.json");

        // Replace the restaurants array with an empty array
        result.SurfaceManager.Process(
            new A2UiMessage
            {
                Version = "v0.9",
                UpdateDataModel = new UpdateDataModel
                {
                    SurfaceId = result.Surface.SurfaceId,
                    Path = "/restaurants",
                    Value = JsonSerializer.SerializeToElement(Array.Empty<object>()),
                },
            }
        );

        Control reRendered = result.ReRender();
        List<Control> rootChildren = GalleryTestHelper.GetChildren(reRendered).ToList();
        Assert.Empty(rootChildren);
    }

    [AvaloniaFact]
    public void TemplateExpansion_SecondCard_HasCorrectContent()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/7_incremental.json");

        // Get the second card (index 1)
        List<Control> rootChildren = GalleryTestHelper.GetChildren(result.RootControl).ToList();
        Assert.True(rootChildren.Count > 1);

        Control secondCard = rootChildren[1];
        List<TextBlock> textBlocks = GalleryTestHelper.FindAll<TextBlock>(secondCard);
        List<string> texts = textBlocks.Select(tb => tb.Text).Where(t => t is not null).Cast<string>().ToList();

        Assert.Contains("Ocean's Bounty", texts);
        Assert.Contains("Fresh Daily Seafood", texts);
        Assert.Contains("456 Shoreline Dr", texts);
    }
}
