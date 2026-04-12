using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using FluentAssertions;

namespace A2Ui.Avalonia.Tests.Integration.Basic;

/// <summary>
/// Integration tests for Modal popup overlay behavior using 29_movie-card.json.
/// Verifies that the trigger is rendered, the Popup control exists in the tree,
/// and that it starts in the closed state.
/// </summary>
public sealed class ModalPopupTests
{
    // ──────────────────────────────────────────────────────────────────
    // 29 Movie Card — Modal trigger + Popup structure
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void MovieCard_Modal_RendersTriggerButton()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/29_movie-card.json");

        // The trigger "watch-trailer-btn" is a Button with child text "Watch Trailer"
        List<Button> buttons = GalleryTestHelper.FindAll<Button>(result.RootControl);
        Button? trailerBtn = buttons.FirstOrDefault(b =>
            GalleryTestHelper.FindFirst<TextBlock>(b)?.Text == "Watch Trailer"
        );

        trailerBtn.Should().NotBeNull("trigger button 'Watch Trailer' must be visible");
    }

    [AvaloniaFact]
    public void MovieCard_Modal_ContainsPopupControl()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/29_movie-card.json");

        // The ModalCatalogEntry adds a Popup to the Panel container
        Popup? popup = FindPopup(result.RootControl);
        popup.Should().NotBeNull("Modal must create an Avalonia Popup control");
    }

    [AvaloniaFact]
    public void MovieCard_Modal_PopupIsInitiallyClosed()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/29_movie-card.json");

        Popup? popup = FindPopup(result.RootControl);
        popup.Should().NotBeNull();
        popup!.IsOpen.Should().BeFalse("popup must start in closed state");
    }

    [AvaloniaFact]
    public void MovieCard_Modal_PopupHasLightDismissEnabled()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/29_movie-card.json");

        Popup? popup = FindPopup(result.RootControl);
        popup.Should().NotBeNull();
        popup!.IsLightDismissEnabled.Should().BeTrue("clicking outside should close the popup");
    }

    [AvaloniaFact]
    public void MovieCard_Modal_PopupChildContainsCloseButton()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/29_movie-card.json");

        Popup? popup = FindPopup(result.RootControl);
        popup.Should().NotBeNull();

        // Popup.Child is a Border containing a StackPanel; first child is the close Button
        popup!.Child.Should().BeOfType<Border>("popup content is wrapped in a Border");
        var border = (Border)popup.Child!;
        border.Child.Should().BeOfType<StackPanel>();
        var stack = (StackPanel)border.Child!;

        Button? closeBtn = stack.Children.OfType<Button>().FirstOrDefault();
        closeBtn.Should().NotBeNull("close button must be present inside the popup");
        closeBtn!.Content.Should().Be("×", "close button must show the × character");
    }

    [AvaloniaFact]
    public void MovieCard_Modal_PopupChildHasModalCloseStyleClass()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/29_movie-card.json");

        Popup? popup = FindPopup(result.RootControl);
        popup.Should().NotBeNull();

        var border = (Border)popup!.Child!;
        var stack = (StackPanel)border.Child!;
        Button? closeBtn = stack.Children.OfType<Button>().FirstOrDefault();
        closeBtn.Should().NotBeNull();
        closeBtn!.Classes.Should().Contain("ModalClose", "close button must carry the ModalClose style class");
    }

    [AvaloniaFact]
    public void MovieCard_Modal_PopupPlacementIsCenter()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/29_movie-card.json");

        Popup? popup = FindPopup(result.RootControl);
        popup.Should().NotBeNull();
        popup!
            .Placement.Should()
            .Be(PlacementMode.Center, "modal popup must be centered relative to its placement target");
    }

    // ──────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Walk the logical tree looking for the first <see cref="Popup"/> child.
    /// <see cref="Popup"/> is not a <see cref="Control"/> subtype in the Panel.Children
    /// collection directly — it is added as a child of a <see cref="Panel"/> and
    /// implements <see cref="Avalonia.LogicalTree.ILogicalTreeNode"/>.
    /// We traverse Panel.Children directly to find it.
    /// </summary>
    private static Popup? FindPopup(Control root)
    {
        if (root is Panel panel)
        {
            foreach (var child in panel.Children)
            {
                if (child is Popup popup)
                {
                    return popup;
                }

                if (child is Control ctrl)
                {
                    Popup? nested = FindPopup(ctrl);
                    if (nested is not null)
                    {
                        return nested;
                    }
                }
            }
        }
        else
        {
            foreach (Control child in GalleryTestHelper.GetChildren(root))
            {
                Popup? found = FindPopup(child);
                if (found is not null)
                {
                    return found;
                }
            }
        }

        return null;
    }
}
