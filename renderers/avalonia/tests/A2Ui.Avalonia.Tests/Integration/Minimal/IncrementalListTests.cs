using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using FluentAssertions;

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

        int length = result.Surface.DataModel.GetArrayLength("/restaurants");
        length.Should().Be(4);
    }

    [AvaloniaFact]
    public void RootControl_HasFourChildren_OnePerRestaurant()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/7_incremental.json");

        // Root is a Column (StackPanel) with template children expanded
        List<Control> rootChildren = GalleryTestHelper.GetChildren(result.RootControl).ToList();
        rootChildren.Should().HaveCount(4,
            "the /restaurants array has 4 items so template expansion should produce 4 cards");
    }

    [AvaloniaFact]
    public void TemplateExpansion_ResolvesRestaurantTitles()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/7_incremental.json");

        List<TextBlock> textBlocks = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        List<string> allTexts = textBlocks
            .Select(tb => tb.Text)
            .Where(t => t is not null)
            .Cast<string>()
            .ToList();

        allTexts.Should().Contain("The Golden Fork");
        allTexts.Should().Contain("Ocean's Bounty");
        allTexts.Should().Contain("Pizzeria Roma");
        allTexts.Should().Contain("Spice Route");
    }

    [AvaloniaFact]
    public void TemplateExpansion_ResolvesAddresses()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/7_incremental.json");

        List<TextBlock> textBlocks = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        List<string> allTexts = textBlocks
            .Select(tb => tb.Text)
            .Where(t => t is not null)
            .Cast<string>()
            .ToList();

        allTexts.Should().Contain("123 Gastronomy Lane");
        allTexts.Should().Contain("456 Shoreline Dr");
        allTexts.Should().Contain("789 Napoli Way");
        allTexts.Should().Contain("101 Silk Road St");
    }

    [AvaloniaFact]
    public void TemplateExpansion_EachCardHasBookNowButton()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/7_incremental.json");

        List<Button> buttons = GalleryTestHelper.FindAll<Button>(result.RootControl);
        buttons.Should().HaveCount(4,
            "each of the 4 restaurant cards should have a 'Book now' button");
    }

    [AvaloniaFact]
    public void TemplateExpansion_BookNowButton_FiresActionWithScopedContext()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/7_incremental.json");

        // Click the first "Book now" button
        List<Button> buttons = GalleryTestHelper.FindAll<Button>(result.RootControl);
        buttons.Should().HaveCountGreaterThan(0);

        GalleryTestHelper.ClickButton(buttons[0]);

        result.ActionLog.Should().HaveCount(1);
        result.ActionLog[0].EventName.Should().Be("book_now");

        // The action context should have resolved the scoped "title" path
        // for the first restaurant (The Golden Fork)
        var context = result.ActionLog[0].Payload as Dictionary<string, string?>;
        context.Should().NotBeNull();
        context!["restaurantName"].Should().Be("The Golden Fork");
    }

    [AvaloniaFact]
    public void TemplateExpansion_SecondCard_HasCorrectContent()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/7_incremental.json");

        // Get the second card (index 1)
        List<Control> rootChildren = GalleryTestHelper.GetChildren(result.RootControl).ToList();
        rootChildren.Should().HaveCountGreaterThan(1);

        Control secondCard = rootChildren[1];
        List<TextBlock> textBlocks = GalleryTestHelper.FindAll<TextBlock>(secondCard);
        List<string> texts = textBlocks
            .Select(tb => tb.Text)
            .Where(t => t is not null)
            .Cast<string>()
            .ToList();

        texts.Should().Contain("Ocean's Bounty");
        texts.Should().Contain("Fresh Daily Seafood");
        texts.Should().Contain("456 Shoreline Dr");
    }
}
