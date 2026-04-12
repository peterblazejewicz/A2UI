using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Xunit;

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

        Assert.NotNull(trailerBtn);
    }

    [AvaloniaFact]
    public void MovieCard_Modal_ContainsPopupControl()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/29_movie-card.json");

        // The ModalCatalogEntry adds a Popup to the Panel container
        Popup? popup = FindPopup(result.RootControl);
        Assert.NotNull(popup);
    }

    [AvaloniaFact]
    public void MovieCard_Modal_PopupIsInitiallyClosed()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/29_movie-card.json");

        Popup? popup = FindPopup(result.RootControl);
        Assert.NotNull(popup);
        Assert.False(popup.IsOpen);
    }

    [AvaloniaFact]
    public void MovieCard_Modal_PopupHasLightDismissEnabled()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/29_movie-card.json");

        Popup? popup = FindPopup(result.RootControl);
        Assert.NotNull(popup);
        Assert.True(popup.IsLightDismissEnabled);
    }

    [AvaloniaFact]
    public void MovieCard_Modal_PopupChildContainsCloseButton()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/29_movie-card.json");

        Popup? popup = FindPopup(result.RootControl);
        Assert.NotNull(popup);

        // Popup.Child is a Border containing a StackPanel; first child is the close Button
        var border = Assert.IsType<Border>(popup.Child);
        var stack = Assert.IsType<StackPanel>(border.Child);

        Button? closeBtn = stack.Children.OfType<Button>().FirstOrDefault();
        Assert.NotNull(closeBtn);
        Assert.Equal("\u00d7", closeBtn.Content);
    }

    [AvaloniaFact]
    public void MovieCard_Modal_PopupChildHasModalCloseStyleClass()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/29_movie-card.json");

        Popup? popup = FindPopup(result.RootControl);
        Assert.NotNull(popup);

        var border = (Border)popup.Child!;
        var stack = (StackPanel)border.Child!;
        Button? closeBtn = stack.Children.OfType<Button>().FirstOrDefault();
        Assert.NotNull(closeBtn);
        Assert.Contains("ModalClose", closeBtn.Classes);
    }

    [AvaloniaFact]
    public void MovieCard_Modal_PopupPlacementIsCenter()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/29_movie-card.json");

        Popup? popup = FindPopup(result.RootControl);
        Assert.NotNull(popup);
        Assert.Equal(PlacementMode.Center, popup.Placement);
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
