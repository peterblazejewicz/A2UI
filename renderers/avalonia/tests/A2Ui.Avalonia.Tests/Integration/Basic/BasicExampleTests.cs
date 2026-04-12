using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Xunit;

namespace A2Ui.Avalonia.Tests.Integration.Basic;

/// <summary>
/// Integration tests for all 33 basic catalog examples.
/// Each test replays the spec JSON through the rendering pipeline and asserts
/// on the resulting control tree structure, data-bound text, and interactions.
/// </summary>
public sealed class BasicExampleTests
{
    // ──────────────────────────────────────────────────────────────────
    // 01 Flight Status — Card+Column+Row+Text+Icon+Divider, formatDate
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Example_01_FlightStatus_RendersCorrectStructure()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/01_flight-status.json");

        // Root is a Card → Border
        Assert.IsType<Border>(result.RootControl);
        var border = (Border)result.RootControl;
        Assert.Equal(new global::Avalonia.CornerRadius(8), border.CornerRadius);

        // Data-bound text values
        List<TextBlock> texts = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        Assert.Contains(texts, tb => tb.Text == "OS 87");
        Assert.Contains(texts, tb => tb.Text == "Vienna");
        Assert.Contains(texts, tb => tb.Text == "New York");
        Assert.Contains(texts, tb => tb.Text == "On Time");

        // Literal text
        Assert.Contains(texts, tb => tb.Text == "Departs");
        Assert.Contains(texts, tb => tb.Text == "Arrives");

        // Separator for Divider
        Assert.NotEmpty(GalleryTestHelper.FindAll<Separator>(result.RootControl));

        // No interactive actions
        Assert.Empty(result.ActionLog);
    }

    // ──────────────────────────────────────────────────────────────────
    // 02 Email Compose — Card+Column+Row+Text+Button+Divider, interactive
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Example_02_EmailCompose_RendersCorrectStructure()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/02_email-compose.json");

        Assert.IsType<Border>(result.RootControl);

        List<TextBlock> texts = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        Assert.Contains(texts, tb => tb.Text == "FROM");
        Assert.Contains(texts, tb => tb.Text == "alex@acme.com");
        Assert.Contains(texts, tb => tb.Text == "jordan@acme.com");
        Assert.Contains(texts, tb => tb.Text == "Q4 Revenue Forecast");
        Assert.Contains(texts, tb => tb.Text == "Hi Jordan,");
        Assert.Contains(texts, tb => tb.Text == "Alex");

        // Buttons
        List<Button> buttons = GalleryTestHelper.FindAll<Button>(result.RootControl);
        Assert.True(buttons.Count >= 2);

        // Click send button
        Button? sendBtn = buttons.FirstOrDefault(b => GalleryTestHelper.FindFirst<TextBlock>(b)?.Text == "Send email");
        Assert.NotNull(sendBtn);
        GalleryTestHelper.ClickButton(sendBtn);
        Assert.Single(result.ActionLog, a => a.EventName == "send");
    }

    // ──────────────────────────────────────────────────────────────────
    // 03 Calendar Day — Card+Column+Row+Text+Button+Divider, formatDate + template children
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    [Trait("Gap", "TemplateChildren")]
    public void Example_03_CalendarDay_RendersCorrectStructure()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/03_calendar-day.json");

        Assert.IsType<Border>(result.RootControl);

        List<TextBlock> texts = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        // Literal text from buttons
        Assert.Contains(texts, tb => tb.Text == "Add to calendar");
        Assert.Contains(texts, tb => tb.Text == "Discard");

        // Buttons with actions
        List<Button> buttons = GalleryTestHelper.FindAll<Button>(result.RootControl);
        Assert.True(buttons.Count >= 2);
    }

    // ──────────────────────────────────────────────────────────────────
    // 04 Weather Current — Card+Column+Row+Text, formatDate+formatString + template
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    [Trait("Gap", "TemplateChildren")]
    public void Example_04_WeatherCurrent_RendersCorrectStructure()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/04_weather-current.json");

        Assert.IsType<Border>(result.RootControl);

        List<TextBlock> texts = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        Assert.Contains(texts, tb => tb.Text == "Austin, TX");
        Assert.Contains(texts, tb => tb.Text == "Clear skies with light breeze");

        Assert.Empty(result.ActionLog);
    }

    // ──────────────────────────────────────────────────────────────────
    // 05 Product Card — Card+Column+Row+Image+Text+Button, formatCurrency etc.
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Example_05_ProductCard_RendersCorrectStructure()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/05_product-card.json");

        Assert.IsType<Border>(result.RootControl);

        List<TextBlock> texts = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        Assert.Contains(texts, tb => tb.Text == "Wireless Headphones Pro");
        Assert.Contains(texts, tb => tb.Text == "\u2605\u2605\u2605\u2605\u2605");

        // Image present
        Assert.NotEmpty(GalleryTestHelper.FindAll<Image>(result.RootControl));

        // Add to Cart button
        List<Button> buttons = GalleryTestHelper.FindAll<Button>(result.RootControl);
        Button? cartBtn = buttons.FirstOrDefault(b => GalleryTestHelper.FindFirst<TextBlock>(b)?.Text == "Add to Cart");
        Assert.NotNull(cartBtn);
        GalleryTestHelper.ClickButton(cartBtn);
        Assert.Single(result.ActionLog, a => a.EventName == "addToCart");
    }

    // ──────────────────────────────────────────────────────────────────
    // 06 Music Player — Card+Column+Row+Image+Icon+Button+Slider+Text
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Example_06_MusicPlayer_RendersCorrectStructure()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/06_music-player.json");

        Assert.IsType<Border>(result.RootControl);

        List<TextBlock> texts = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        Assert.Contains(texts, tb => tb.Text == "Blinding Lights");
        Assert.Contains(texts, tb => tb.Text == "The Weeknd");
        Assert.Contains(texts, tb => tb.Text == "1:48");
        Assert.Contains(texts, tb => tb.Text == "4:22");

        // Image for album art
        Assert.NotEmpty(GalleryTestHelper.FindAll<Image>(result.RootControl));

        // Slider for progress
        Slider? slider = GalleryTestHelper.FindFirst<Slider>(result.RootControl);
        Assert.NotNull(slider);
        Assert.Equal(1, slider.Maximum);

        // Three buttons: prev, play/pause, next
        List<Button> buttons = GalleryTestHelper.FindAll<Button>(result.RootControl);
        Assert.True(buttons.Count >= 3);

        // Click play button
        GalleryTestHelper.ClickButton(buttons[1]);
        Assert.Single(result.ActionLog, a => a.EventName == "playPause");
    }

    // ──────────────────────────────────────────────────────────────────
    // 07 Task Card — Card+Row+Column+Text+CheckBox+DateTimeInput+Icon
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Example_07_TaskCard_RendersCorrectStructure()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/07_task-card.json");

        Assert.IsType<Border>(result.RootControl);

        List<TextBlock> texts = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        Assert.Contains(texts, tb => tb.Text == "Review pull request");

        // CheckBox
        CheckBox? cb = GalleryTestHelper.FindFirst<CheckBox>(result.RootControl);
        Assert.NotNull(cb);

        // DateTimeInput → CalendarDatePicker
        CalendarDatePicker? picker = GalleryTestHelper.FindFirst<CalendarDatePicker>(result.RootControl);
        Assert.NotNull(picker);

        Assert.Empty(result.ActionLog);
    }

    // ──────────────────────────────────────────────────────────────────
    // 08 User Profile — Card+Column+Row+Image+Text+Button, formatNumber
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Example_08_UserProfile_RendersCorrectStructure()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/08_user-profile.json");

        Assert.IsType<Border>(result.RootControl);

        List<TextBlock> texts = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        Assert.Contains(texts, tb => tb.Text == "Sarah Chen");
        Assert.Contains(texts, tb => tb.Text == "@sarahchen");

        // Literal text
        Assert.Contains(texts, tb => tb.Text == "Followers");
        Assert.Contains(texts, tb => tb.Text == "Following");

        // Image
        Assert.NotEmpty(GalleryTestHelper.FindAll<Image>(result.RootControl));

        // Follow button
        List<Button> buttons = GalleryTestHelper.FindAll<Button>(result.RootControl);
        Assert.NotEmpty(buttons);
        GalleryTestHelper.ClickButton(buttons[0]);
        Assert.Single(result.ActionLog, a => a.EventName == "follow");
    }

    // ──────────────────────────────────────────────────────────────────
    // 09 Login Form — Card+Column+Row+TextField+Button+Divider, HAS CHECKS
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Example_09_LoginForm_RendersCorrectStructure()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/09_login-form.json");

        Assert.IsType<Border>(result.RootControl);

        List<TextBlock> texts = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        Assert.Contains(texts, tb => tb.Text == "Welcome back");
        Assert.Contains(texts, tb => tb.Text == "Sign in to your account");
        Assert.Contains(texts, tb => tb.Text == "Don't have an account?");

        // TextFields for email and password
        List<TextBox> textBoxes = GalleryTestHelper.FindAll<TextBox>(result.RootControl);
        Assert.True(textBoxes.Count >= 2);

        // Divider
        Assert.NotEmpty(GalleryTestHelper.FindAll<Separator>(result.RootControl));

        // Buttons: Sign in and Sign up
        List<Button> buttons = GalleryTestHelper.FindAll<Button>(result.RootControl);
        Assert.True(buttons.Count >= 2);

        // Click signup link button
        Button? signupBtn = buttons.FirstOrDefault(b => GalleryTestHelper.FindFirst<TextBlock>(b)?.Text == "Sign up");
        Assert.NotNull(signupBtn);
        GalleryTestHelper.ClickButton(signupBtn);
        Assert.Single(result.ActionLog, a => a.EventName == "signup");
    }

    // ──────────────────────────────────────────────────────────────────
    // 10 Notification Permission — Card+Column+Row+Icon+Text+Button
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Example_10_NotificationPermission_RendersCorrectStructure()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/10_notification-permission.json");

        Assert.IsType<Border>(result.RootControl);

        List<TextBlock> texts = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        Assert.Contains(texts, tb => tb.Text == "Enable notification");
        Assert.Contains(texts, tb => tb.Text == "Get alerts for order status changes");
        // Icon check → ✔
        Assert.Contains(texts, tb => tb.Text == "\u2714");

        // Buttons: Yes and No
        List<Button> buttons = GalleryTestHelper.FindAll<Button>(result.RootControl);
        Assert.True(buttons.Count >= 2);

        Button? yesBtn = buttons.FirstOrDefault(b => GalleryTestHelper.FindFirst<TextBlock>(b)?.Text == "Yes");
        Assert.NotNull(yesBtn);
        GalleryTestHelper.ClickButton(yesBtn);
        Assert.Single(result.ActionLog, a => a.EventName == "accept");
    }

    // ──────────────────────────────────────────────────────────────────
    // 11 Purchase Complete — Card+Column+Row+Image+Icon+Text+Button+Divider, formatCurrency
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Example_11_PurchaseComplete_RendersCorrectStructure()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/11_purchase-complete.json");

        Assert.IsType<Border>(result.RootControl);

        List<TextBlock> texts = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        Assert.Contains(texts, tb => tb.Text == "Purchase Complete");
        Assert.Contains(texts, tb => tb.Text == "Wireless Headphones Pro");
        Assert.Contains(texts, tb => tb.Text == "Sold by:");
        Assert.Contains(texts, tb => tb.Text == "TechStore Official");

        // Image
        Assert.NotEmpty(GalleryTestHelper.FindAll<Image>(result.RootControl));

        // Button: View Order Details
        List<Button> buttons = GalleryTestHelper.FindAll<Button>(result.RootControl);
        Button? viewBtn = buttons.FirstOrDefault(b =>
            GalleryTestHelper.FindFirst<TextBlock>(b)?.Text == "View Order Details"
        );
        Assert.NotNull(viewBtn);
        GalleryTestHelper.ClickButton(viewBtn);
        Assert.Single(result.ActionLog, a => a.EventName == "view_details");
    }

    // ──────────────────────────────────────────────────────────────────
    // 12 Chat Message — Card+Column+Row+Image+Icon+Divider+Text, formatDate + template
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    [Trait("Gap", "TemplateChildren")]
    public void Example_12_ChatMessage_RendersCorrectStructure()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/12_chat-message.json");

        Assert.IsType<Border>(result.RootControl);

        List<TextBlock> texts = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        Assert.Contains(texts, tb => tb.Text == "project-updates");

        // Divider
        Assert.NotEmpty(GalleryTestHelper.FindAll<Separator>(result.RootControl));

        Assert.Empty(result.ActionLog);
    }

    // ──────────────────────────────────────────────────────────────────
    // 13 Coffee Order — Card+Column+Row+Icon+Text+Button+Divider, formatCurrency + template
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    [Trait("Gap", "TemplateChildren")]
    public void Example_13_CoffeeOrder_RendersCorrectStructure()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/13_coffee-order.json");

        Assert.IsType<Border>(result.RootControl);

        List<TextBlock> texts = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        Assert.Contains(texts, tb => tb.Text == "Sunrise Coffee");
        Assert.Contains(texts, tb => tb.Text == "Subtotal");
        Assert.Contains(texts, tb => tb.Text == "Tax");
        Assert.Contains(texts, tb => tb.Text == "Total");

        // Buttons: Purchase and Add to cart
        List<Button> buttons = GalleryTestHelper.FindAll<Button>(result.RootControl);
        Assert.True(buttons.Count >= 2);
    }

    // ──────────────────────────────────────────────────────────────────
    // 14 Sports Player — Card+Column+Row+Image+Divider+Text, path bindings only
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Example_14_SportsPlayer_RendersCorrectStructure()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/14_sports-player.json");

        Assert.IsType<Border>(result.RootControl);

        List<TextBlock> texts = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        Assert.Contains(texts, tb => tb.Text == "Marcus Johnson");
        Assert.Contains(texts, tb => tb.Text == "#23");
        Assert.Contains(texts, tb => tb.Text == "LA Lakers");
        Assert.Contains(texts, tb => tb.Text == "28.4");
        Assert.Contains(texts, tb => tb.Text == "PPG");

        // Image
        Assert.NotEmpty(GalleryTestHelper.FindAll<Image>(result.RootControl));

        // Divider
        Assert.NotEmpty(GalleryTestHelper.FindAll<Separator>(result.RootControl));

        Assert.Empty(result.ActionLog);
    }

    // ──────────────────────────────────────────────────────────────────
    // 15 Account Balance — Card+Column+Row+Icon+Divider+Text+Button, formatCurrency
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Example_15_AccountBalance_RendersCorrectStructure()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/15_account-balance.json");

        Assert.IsType<Border>(result.RootControl);

        List<TextBlock> texts = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        Assert.Contains(texts, tb => tb.Text == "Primary Checking");
        Assert.Contains(texts, tb => tb.Text == "Updated just now");
        Assert.Contains(texts, tb => tb.Text == "Transfer");
        Assert.Contains(texts, tb => tb.Text == "Pay Bill");

        // Buttons
        List<Button> buttons = GalleryTestHelper.FindAll<Button>(result.RootControl);
        Assert.True(buttons.Count >= 2);

        Button? transferBtn = buttons.FirstOrDefault(b =>
            GalleryTestHelper.FindFirst<TextBlock>(b)?.Text == "Transfer"
        );
        Assert.NotNull(transferBtn);
        GalleryTestHelper.ClickButton(transferBtn);
        Assert.Single(result.ActionLog, a => a.EventName == "transfer");
    }

    // ──────────────────────────────────────────────────────────────────
    // 16 Workout Summary — Card+Column+Row+Icon+Divider+Text, formatNumber etc.
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Example_16_WorkoutSummary_RendersCorrectStructure()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/16_workout-summary.json");

        Assert.IsType<Border>(result.RootControl);

        List<TextBlock> texts = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        Assert.Contains(texts, tb => tb.Text == "Workout Complete");
        // "Morning Run" is in data model as /workoutType but no component binds to it
        Assert.Contains(texts, tb => tb.Text == "32:15");
        Assert.Contains(texts, tb => tb.Text == "Duration");
        Assert.Contains(texts, tb => tb.Text == "Calories");
        Assert.Contains(texts, tb => tb.Text == "Distance");

        // Divider
        Assert.NotEmpty(GalleryTestHelper.FindAll<Separator>(result.RootControl));

        Assert.Empty(result.ActionLog);
    }

    // ──────────────────────────────────────────────────────────────────
    // 17 Event Detail — Card+Column+Row+Icon+Divider+Text+Button, formatString(formatDate)
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Example_17_EventDetail_RendersCorrectStructure()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/17_event-detail.json");

        Assert.IsType<Border>(result.RootControl);

        List<TextBlock> texts = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        Assert.Contains(texts, tb => tb.Text == "Product Launch Meeting");
        Assert.Contains(texts, tb => tb.Text == "Accept");
        Assert.Contains(texts, tb => tb.Text == "Decline");

        // Buttons
        List<Button> buttons = GalleryTestHelper.FindAll<Button>(result.RootControl);
        Assert.True(buttons.Count >= 2);

        Button? acceptBtn = buttons.FirstOrDefault(b => GalleryTestHelper.FindFirst<TextBlock>(b)?.Text == "Accept");
        Assert.NotNull(acceptBtn);
        GalleryTestHelper.ClickButton(acceptBtn);
        Assert.Single(result.ActionLog, a => a.EventName == "accept");
    }

    // ──────────────────────────────────────────────────────────────────
    // 18 Track List — Card+Column+Row+Icon+Image+Divider+Text, formatNumber + template
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    [Trait("Gap", "TemplateChildren")]
    public void Example_18_TrackList_RendersCorrectStructure()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/18_track-list.json");

        Assert.IsType<Border>(result.RootControl);

        List<TextBlock> texts = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        Assert.Contains(texts, tb => tb.Text == "Focus Flow");

        // Divider
        Assert.NotEmpty(GalleryTestHelper.FindAll<Separator>(result.RootControl));

        Assert.Empty(result.ActionLog);
    }

    // ──────────────────────────────────────────────────────────────────
    // 19 Software Purchase — Card+Column+Row+ChoicePicker+Text+Button+Divider, formatString(formatCurrency)
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Example_19_SoftwarePurchase_RendersCorrectStructure()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/19_software-purchase.json");

        Assert.IsType<Border>(result.RootControl);

        List<TextBlock> texts = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        Assert.Contains(texts, tb => tb.Text == "Design Suite Pro");
        Assert.Contains(texts, tb => tb.Text == "Purchase License");
        Assert.Contains(texts, tb => tb.Text == "Number of seats");
        Assert.Contains(texts, tb => tb.Text == "10 seats");

        // ChoicePicker → ComboBox
        ComboBox? combo = GalleryTestHelper.FindFirst<ComboBox>(result.RootControl);
        Assert.NotNull(combo);

        // Buttons: Confirm and Cancel
        List<Button> buttons = GalleryTestHelper.FindAll<Button>(result.RootControl);
        Assert.True(buttons.Count >= 2);
    }

    // ──────────────────────────────────────────────────────────────────
    // 20 Restaurant Card — Card+Column+Row+Image+Icon+Text, path bindings only
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Example_20_RestaurantCard_RendersCorrectStructure()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/20_restaurant-card.json");

        Assert.IsType<Border>(result.RootControl);

        List<TextBlock> texts = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        Assert.Contains(texts, tb => tb.Text == "The Italian Kitchen");
        Assert.Contains(texts, tb => tb.Text == "$$$");
        Assert.Contains(texts, tb => tb.Text == "Italian \u2022 Pasta \u2022 Wine Bar");

        // Image
        Assert.NotEmpty(GalleryTestHelper.FindAll<Image>(result.RootControl));

        Assert.Empty(result.ActionLog);
    }

    // ──────────────────────────────────────────────────────────────────
    // 21 Shipping Status — Card+Column+Row+Icon+Divider+Text, template children
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    [Trait("Gap", "TemplateChildren")]
    public void Example_21_ShippingStatus_RendersCorrectStructure()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/21_shipping-status.json");

        Assert.IsType<Border>(result.RootControl);

        List<TextBlock> texts = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        Assert.Contains(texts, tb => tb.Text == "Package Status");
        Assert.Contains(texts, tb => tb.Text == "Tracking: 1Z999AA10123456784");
        Assert.Contains(texts, tb => tb.Text == "Estimated delivery: Today by 8 PM");

        Assert.Empty(result.ActionLog);
    }

    // ──────────────────────────────────────────────────────────────────
    // 22 Credit Card — Card+Column+Row+Icon+Text, path bindings only
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Example_22_CreditCard_RendersCorrectStructure()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/22_credit-card.json");

        Assert.IsType<Border>(result.RootControl);

        List<TextBlock> texts = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        Assert.Contains(texts, tb => tb.Text == "VISA");
        Assert.Contains(
            texts,
            tb => tb.Text == "\u2022\u2022\u2022\u2022 \u2022\u2022\u2022\u2022 \u2022\u2022\u2022\u2022 4242"
        );
        Assert.Contains(texts, tb => tb.Text == "SARAH JOHNSON");
        Assert.Contains(texts, tb => tb.Text == "CARD HOLDER");
        Assert.Contains(texts, tb => tb.Text == "EXPIRES");

        Assert.Empty(result.ActionLog);
    }

    // ──────────────────────────────────────────────────────────────────
    // 23 Step Counter — Card+Column+Row+Icon+Divider+Text, formatNumber+formatString
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Example_23_StepCounter_RendersCorrectStructure()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/23_step-counter.json");

        Assert.IsType<Border>(result.RootControl);

        List<TextBlock> texts = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        Assert.Contains(texts, tb => tb.Text == "Today's Steps");
        Assert.Contains(texts, tb => tb.Text == "Distance");
        Assert.Contains(texts, tb => tb.Text == "Calories");

        // Divider
        Assert.NotEmpty(GalleryTestHelper.FindAll<Separator>(result.RootControl));

        Assert.Empty(result.ActionLog);
    }

    // ──────────────────────────────────────────────────────────────────
    // 24 Recipe Card — Card+Column+Row+Icon+Image+Tabs+Text, formatString+formatNumber+pluralize + template
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    [Trait("Gap", "TemplateChildren")]
    public void Example_24_RecipeCard_RendersCorrectStructure()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/24_recipe-card.json");

        // Root is a Card → Border
        Assert.IsType<Border>(result.RootControl);

        // Card wraps a TabControl directly — GetChildren doesn't traverse TabItem content,
        // so we verify structure at the TabControl level
        TabControl? tabs = GalleryTestHelper.FindFirst<TabControl>(result.RootControl);
        Assert.NotNull(tabs);
        Assert.Equal(3, tabs.Items.Count);

        // Verify tab headers
        var tabItems = tabs.Items.Cast<TabItem>().ToList();
        Assert.Equal("Overview", tabItems[0].Header);
        Assert.Equal("Ingredients", tabItems[1].Header);
        Assert.Equal("Instructions", tabItems[2].Header);

        Assert.Empty(result.ActionLog);
    }

    // ──────────────────────────────────────────────────────────────────
    // 25 Contact Card — Card+Column+Row+Image+Icon+Divider+Text+Button
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Example_25_ContactCard_RendersCorrectStructure()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/25_contact-card.json");

        Assert.IsType<Border>(result.RootControl);

        List<TextBlock> texts = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        Assert.Contains(texts, tb => tb.Text == "David Park");
        Assert.Contains(texts, tb => tb.Text == "Engineering Manager");
        Assert.Contains(texts, tb => tb.Text == "+1 (555) 234-5678");
        Assert.Contains(texts, tb => tb.Text == "Call");
        Assert.Contains(texts, tb => tb.Text == "Message");

        // Image
        Assert.NotEmpty(GalleryTestHelper.FindAll<Image>(result.RootControl));

        // Divider
        Assert.NotEmpty(GalleryTestHelper.FindAll<Separator>(result.RootControl));

        // Click Call button
        List<Button> buttons = GalleryTestHelper.FindAll<Button>(result.RootControl);
        Button? callBtn = buttons.FirstOrDefault(b => GalleryTestHelper.FindFirst<TextBlock>(b)?.Text == "Call");
        Assert.NotNull(callBtn);
        GalleryTestHelper.ClickButton(callBtn);
        Assert.Single(result.ActionLog, a => a.EventName == "call");
    }

    // ──────────────────────────────────────────────────────────────────
    // 26 Podcast Episode — Card+Column+Row+Image+AudioPlayer+Text, formatDate
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Example_26_PodcastEpisode_RendersCorrectStructure()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/26_podcast-episode.json");

        Assert.IsType<Border>(result.RootControl);

        List<TextBlock> texts = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        Assert.Contains(texts, tb => tb.Text == "Tech Talk Daily");
        Assert.Contains(texts, tb => tb.Text == "The Future of AI in Product Design");
        Assert.Contains(texts, tb => tb.Text == "45 min");

        // Image
        Assert.NotEmpty(GalleryTestHelper.FindAll<Image>(result.RootControl));

        // AudioPlayer renders as TextBlock placeholder
        Assert.Contains(texts, tb => tb.Text?.Contains("[AudioPlayer:") == true);

        Assert.Empty(result.ActionLog);
    }

    // ──────────────────────────────────────────────────────────────────
    // 27 Stats Card — Card+Column+Row+Icon+Text, formatCurrency+formatString
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Example_27_StatsCard_RendersCorrectStructure()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/27_stats-card.json");

        Assert.IsType<Border>(result.RootControl);

        List<TextBlock> texts = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        Assert.Contains(texts, tb => tb.Text == "Monthly Revenue");

        Assert.Empty(result.ActionLog);
    }

    // ──────────────────────────────────────────────────────────────────
    // 28 Countdown Timer — Card+Column+Row+Text, formatDate
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Example_28_CountdownTimer_RendersCorrectStructure()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/28_countdown-timer.json");

        Assert.IsType<Border>(result.RootControl);

        List<TextBlock> texts = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        Assert.Contains(texts, tb => tb.Text == "Product Launch");
        Assert.Contains(texts, tb => tb.Text == "14");
        Assert.Contains(texts, tb => tb.Text == "08");
        Assert.Contains(texts, tb => tb.Text == "Days");
        Assert.Contains(texts, tb => tb.Text == "Hours");
        Assert.Contains(texts, tb => tb.Text == "Minutes");

        Assert.Empty(result.ActionLog);
    }

    // ──────────────────────────────────────────────────────────────────
    // 29 Movie Card — Card+Column+Row+Image+Icon+Text+Button+Modal+Video
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Example_29_MovieCard_RendersCorrectStructure()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/29_movie-card.json");

        Assert.IsType<Border>(result.RootControl);

        List<TextBlock> texts = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        Assert.Contains(texts, tb => tb.Text == "Interstellar");
        Assert.Contains(texts, tb => tb.Text == "(2014)");
        Assert.Contains(texts, tb => tb.Text == "Sci-Fi \u2022 Adventure \u2022 Drama");
        Assert.Contains(texts, tb => tb.Text == "Watch Trailer");

        // Image (poster)
        Assert.NotEmpty(GalleryTestHelper.FindAll<Image>(result.RootControl));

        // Video renders as TextBlock placeholder
        Assert.Contains(texts, tb => tb.Text?.Contains("[Video:") == true);

        // Button: Watch Trailer → open_trailer
        List<Button> buttons = GalleryTestHelper.FindAll<Button>(result.RootControl);
        Button? trailerBtn = buttons.FirstOrDefault(b =>
            GalleryTestHelper.FindFirst<TextBlock>(b)?.Text == "Watch Trailer"
        );
        Assert.NotNull(trailerBtn);
        GalleryTestHelper.ClickButton(trailerBtn);
        Assert.Single(result.ActionLog, a => a.EventName == "open_trailer");
    }

    // ──────────────────────────────────────────────────────────────────
    // 30 Live Invitation Builder — Card+Column+Row+Image+TextField+DateTimeInput+ChoicePicker+Text, formatDate+formatString
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Example_30_LiveInvitationBuilder_RendersCorrectStructure()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/30_live-invitation-builder.json");

        // Root is Column → StackPanel (not Card)
        Assert.IsType<StackPanel>(result.RootControl);

        List<TextBlock> texts = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        Assert.Contains(texts, tb => tb.Text == "# Invitation Builder");
        Assert.Contains(texts, tb => tb.Text == "Live Preview");
        Assert.Contains(texts, tb => tb.Text == "Celebrating");

        // TextField
        List<TextBox> textBoxes = GalleryTestHelper.FindAll<TextBox>(result.RootControl);
        Assert.NotEmpty(textBoxes);

        // DateTimeInput → CalendarDatePicker
        CalendarDatePicker? picker = GalleryTestHelper.FindFirst<CalendarDatePicker>(result.RootControl);
        Assert.NotNull(picker);

        // ChoicePicker → ComboBox
        ComboBox? combo = GalleryTestHelper.FindFirst<ComboBox>(result.RootControl);
        Assert.NotNull(combo);
    }

    // ──────────────────────────────────────────────────────────────────
    // 31 Incremental Dashboard — Card+Column+Row+List+Text, template children
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    [Trait("Gap", "TemplateChildren")]
    public void Example_31_IncrementalDashboard_RendersCorrectStructure()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/31_incremental-dashboard.json");

        // Root is Column → StackPanel (not Card)
        Assert.IsType<StackPanel>(result.RootControl);

        List<TextBlock> texts = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        Assert.Contains(texts, tb => tb.Text == "System Dashboard");

        // List → ScrollViewer
        ScrollViewer? scrollViewer = GalleryTestHelper.FindFirst<ScrollViewer>(result.RootControl);
        Assert.NotNull(scrollViewer);

        Assert.Empty(result.ActionLog);
    }

    // ──────────────────────────────────────────────────────────────────
    // 32 Advanced Form Validator — Card+Column+TextField+CheckBox+Button+Text, formatString+formatDate, HAS CHECKS
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Example_32_AdvancedFormValidator_RendersCorrectStructure()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/32_advanced-form-validator.json");

        Assert.IsType<Border>(result.RootControl);

        List<TextBlock> texts = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        Assert.Contains(texts, tb => tb.Text == "Submit Registration");

        // TextFields
        List<TextBox> textBoxes = GalleryTestHelper.FindAll<TextBox>(result.RootControl);
        Assert.NotEmpty(textBoxes);

        // CheckBox
        CheckBox? cb = GalleryTestHelper.FindFirst<CheckBox>(result.RootControl);
        Assert.NotNull(cb);

        // Button: Submit Registration — disabled initially due to check validation
        List<Button> buttons = GalleryTestHelper.FindAll<Button>(result.RootControl);
        Assert.NotEmpty(buttons);
        Button? submitBtn = buttons.FirstOrDefault(b =>
            GalleryTestHelper.FindFirst<TextBlock>(b)?.Text == "Submit Registration"
        );
        Assert.NotNull(submitBtn);
        Assert.False(submitBtn.IsEnabled);
    }

    // ──────────────────────────────────────────────────────────────────
    // 33 Financial Data Grid — Card+Column+Row+Icon+List+Text, formatCurrency+formatString + template
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    [Trait("Gap", "TemplateChildren")]
    public void Example_33_FinancialDataGrid_RendersCorrectStructure()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/33_financial-data-grid.json");

        Assert.IsType<Border>(result.RootControl);

        List<TextBlock> texts = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        Assert.Contains(texts, tb => tb.Text == "Asset");
        Assert.Contains(texts, tb => tb.Text == "Price");
        Assert.Contains(texts, tb => tb.Text == "24h Change");
        Assert.Contains(texts, tb => tb.Text == "Market Cap");

        // List → ScrollViewer
        ScrollViewer? scrollViewer = GalleryTestHelper.FindFirst<ScrollViewer>(result.RootControl);
        Assert.NotNull(scrollViewer);

        Assert.Empty(result.ActionLog);
    }
}
