using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using FluentAssertions;

namespace A2Ui.Avalonia.Tests.Integration.Basic;

/// <summary>
/// Integration test verifying that the 17_event-detail example correctly
/// resolves the compound <c>formatString</c> expression that combines
/// <c>formatDate</c> calls with path references and literal separators.
/// </summary>
public sealed class EventDetailFormatStringTests
{
    /// <summary>
    /// The <c>time-text</c> component uses:
    /// <code>
    /// formatString(value: "${formatDate(value: ${/start}, format: 'E, MMM d')}
    ///   • ${formatDate(value: ${/start}, format: 'h:mm a')}
    ///   - ${formatDate(value: ${/end}, format: 'h:mm a')}")
    /// </code>
    /// With data model: start = 2025-12-19T14:00:00Z, end = 2025-12-19T15:30:00Z
    /// Expected output resembles: "Fri, Dec 19 • 2:00 PM - 3:30 PM"
    /// </summary>
    [AvaloniaFact]
    public void EventDetail_TimeText_RendersFormattedDateString()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/17_event-detail.json");

        List<TextBlock> texts = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);

        // The time-text TextBlock should contain the formatted compound string.
        // formatDate with 'E, MMM d' on 2025-12-19T14:00:00Z → "Fri, Dec 19"
        // formatDate with 'h:mm a' on 2025-12-19T14:00:00Z → "2:00 PM"
        // formatDate with 'h:mm a' on 2025-12-19T15:30:00Z → "3:30 PM"
        // Combined: "Fri, Dec 19 • 2:00 PM - 3:30 PM"
        TextBlock? timeText = texts.FirstOrDefault(tb =>
            tb.Text?.Contains("\u2022") == true && tb.Text.Contains("Dec 19")
        );

        timeText
            .Should()
            .NotBeNull("the time-text component should render a formatted date string with bullet separator");
        timeText!
            .Text.Should()
            .Contain("Fri, Dec 19", "formatDate with 'E, MMM d' should produce abbreviated day and month");
        timeText.Text.Should().Contain("PM", "formatDate with 'h:mm a' should produce AM/PM time format");
    }
}
