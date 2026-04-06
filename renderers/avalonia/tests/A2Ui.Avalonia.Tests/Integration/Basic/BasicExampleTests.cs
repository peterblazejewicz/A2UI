using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using FluentAssertions;
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
    [Trait("Gap", "FunctionCall")]
    public void Example_01_FlightStatus_RendersCorrectStructure()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/01_flight-status.json");

        // Root is a Card → Border
        result.RootControl.Should().BeOfType<Border>();
        var border = (Border)result.RootControl;
        border.CornerRadius.Should().Be(new global::Avalonia.CornerRadius(8));

        // Data-bound text values
        List<TextBlock> texts = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        texts.Should().Contain(tb => tb.Text == "OS 87");
        texts.Should().Contain(tb => tb.Text == "Vienna");
        texts.Should().Contain(tb => tb.Text == "New York");
        texts.Should().Contain(tb => tb.Text == "On Time");

        // Literal text
        texts.Should().Contain(tb => tb.Text == "Departs");
        texts.Should().Contain(tb => tb.Text == "Arrives");

        // Separator for Divider
        GalleryTestHelper.FindAll<Separator>(result.RootControl).Should().NotBeEmpty();

        // No interactive actions
        result.ActionLog.Should().BeEmpty();
    }

    // ──────────────────────────────────────────────────────────────────
    // 02 Email Compose — Card+Column+Row+Text+Button+Divider, interactive
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Example_02_EmailCompose_RendersCorrectStructure()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/02_email-compose.json");

        result.RootControl.Should().BeOfType<Border>();

        List<TextBlock> texts = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        texts.Should().Contain(tb => tb.Text == "FROM");
        texts.Should().Contain(tb => tb.Text == "alex@acme.com");
        texts.Should().Contain(tb => tb.Text == "jordan@acme.com");
        texts.Should().Contain(tb => tb.Text == "Q4 Revenue Forecast");
        texts.Should().Contain(tb => tb.Text == "Hi Jordan,");
        texts.Should().Contain(tb => tb.Text == "Alex");

        // Buttons
        List<Button> buttons = GalleryTestHelper.FindAll<Button>(result.RootControl);
        buttons.Should().HaveCountGreaterThanOrEqualTo(2);

        // Click send button
        Button? sendBtn = buttons.FirstOrDefault(b =>
            GalleryTestHelper.FindFirst<TextBlock>(b)?.Text == "Send email");
        sendBtn.Should().NotBeNull();
        GalleryTestHelper.ClickButton(sendBtn!);
        result.ActionLog.Should().ContainSingle(a => a.EventName == "send");
    }

    // ──────────────────────────────────────────────────────────────────
    // 03 Calendar Day — Card+Column+Row+Text+Button+Divider, formatDate + template children
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    [Trait("Gap", "FunctionCall")]
    [Trait("Gap", "TemplateChildren")]
    public void Example_03_CalendarDay_RendersCorrectStructure()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/03_calendar-day.json");

        result.RootControl.Should().BeOfType<Border>();

        List<TextBlock> texts = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        // Literal text from buttons
        texts.Should().Contain(tb => tb.Text == "Add to calendar");
        texts.Should().Contain(tb => tb.Text == "Discard");

        // Buttons with actions
        List<Button> buttons = GalleryTestHelper.FindAll<Button>(result.RootControl);
        buttons.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    // ──────────────────────────────────────────────────────────────────
    // 04 Weather Current — Card+Column+Row+Text, formatDate+formatString + template
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    [Trait("Gap", "FunctionCall")]
    [Trait("Gap", "TemplateChildren")]
    public void Example_04_WeatherCurrent_RendersCorrectStructure()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/04_weather-current.json");

        result.RootControl.Should().BeOfType<Border>();

        List<TextBlock> texts = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        texts.Should().Contain(tb => tb.Text == "Austin, TX");
        texts.Should().Contain(tb => tb.Text == "Clear skies with light breeze");

        result.ActionLog.Should().BeEmpty();
    }

    // ──────────────────────────────────────────────────────────────────
    // 05 Product Card — Card+Column+Row+Image+Text+Button, formatCurrency etc.
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    [Trait("Gap", "FunctionCall")]
    public void Example_05_ProductCard_RendersCorrectStructure()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/05_product-card.json");

        result.RootControl.Should().BeOfType<Border>();

        List<TextBlock> texts = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        texts.Should().Contain(tb => tb.Text == "Wireless Headphones Pro");
        texts.Should().Contain(tb => tb.Text == "\u2605\u2605\u2605\u2605\u2605");

        // Image present
        GalleryTestHelper.FindAll<Image>(result.RootControl).Should().NotBeEmpty();

        // Add to Cart button
        List<Button> buttons = GalleryTestHelper.FindAll<Button>(result.RootControl);
        Button? cartBtn = buttons.FirstOrDefault(b =>
            GalleryTestHelper.FindFirst<TextBlock>(b)?.Text == "Add to Cart");
        cartBtn.Should().NotBeNull();
        GalleryTestHelper.ClickButton(cartBtn!);
        result.ActionLog.Should().ContainSingle(a => a.EventName == "addToCart");
    }

    // ──────────────────────────────────────────────────────────────────
    // 06 Music Player — Card+Column+Row+Image+Icon+Button+Slider+Text
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Example_06_MusicPlayer_RendersCorrectStructure()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/06_music-player.json");

        result.RootControl.Should().BeOfType<Border>();

        List<TextBlock> texts = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        texts.Should().Contain(tb => tb.Text == "Blinding Lights");
        texts.Should().Contain(tb => tb.Text == "The Weeknd");
        texts.Should().Contain(tb => tb.Text == "1:48");
        texts.Should().Contain(tb => tb.Text == "4:22");

        // Image for album art
        GalleryTestHelper.FindAll<Image>(result.RootControl).Should().NotBeEmpty();

        // Slider for progress
        Slider? slider = GalleryTestHelper.FindFirst<Slider>(result.RootControl);
        slider.Should().NotBeNull();
        slider!.Maximum.Should().Be(1);

        // Three buttons: prev, play/pause, next
        List<Button> buttons = GalleryTestHelper.FindAll<Button>(result.RootControl);
        buttons.Should().HaveCountGreaterThanOrEqualTo(3);

        // Click play button
        GalleryTestHelper.ClickButton(buttons[1]);
        result.ActionLog.Should().ContainSingle(a => a.EventName == "playPause");
    }

    // ──────────────────────────────────────────────────────────────────
    // 07 Task Card — Card+Row+Column+Text+CheckBox+DateTimeInput+Icon
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Example_07_TaskCard_RendersCorrectStructure()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/07_task-card.json");

        result.RootControl.Should().BeOfType<Border>();

        List<TextBlock> texts = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        texts.Should().Contain(tb => tb.Text == "Review pull request");

        // CheckBox
        CheckBox? cb = GalleryTestHelper.FindFirst<CheckBox>(result.RootControl);
        cb.Should().NotBeNull();

        // DateTimeInput → CalendarDatePicker
        CalendarDatePicker? picker = GalleryTestHelper.FindFirst<CalendarDatePicker>(result.RootControl);
        picker.Should().NotBeNull();

        result.ActionLog.Should().BeEmpty();
    }

    // ──────────────────────────────────────────────────────────────────
    // 08 User Profile — Card+Column+Row+Image+Text+Button, formatNumber
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    [Trait("Gap", "FunctionCall")]
    public void Example_08_UserProfile_RendersCorrectStructure()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/08_user-profile.json");

        result.RootControl.Should().BeOfType<Border>();

        List<TextBlock> texts = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        texts.Should().Contain(tb => tb.Text == "Sarah Chen");
        texts.Should().Contain(tb => tb.Text == "@sarahchen");

        // Literal text
        texts.Should().Contain(tb => tb.Text == "Followers");
        texts.Should().Contain(tb => tb.Text == "Following");

        // Image
        GalleryTestHelper.FindAll<Image>(result.RootControl).Should().NotBeEmpty();

        // Follow button
        List<Button> buttons = GalleryTestHelper.FindAll<Button>(result.RootControl);
        buttons.Should().NotBeEmpty();
        GalleryTestHelper.ClickButton(buttons[0]);
        result.ActionLog.Should().ContainSingle(a => a.EventName == "follow");
    }

    // ──────────────────────────────────────────────────────────────────
    // 09 Login Form — Card+Column+Row+TextField+Button+Divider, HAS CHECKS
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    [Trait("Gap", "CheckValidation")]
    public void Example_09_LoginForm_RendersCorrectStructure()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/09_login-form.json");

        result.RootControl.Should().BeOfType<Border>();

        List<TextBlock> texts = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        texts.Should().Contain(tb => tb.Text == "Welcome back");
        texts.Should().Contain(tb => tb.Text == "Sign in to your account");
        texts.Should().Contain(tb => tb.Text == "Don't have an account?");

        // TextFields for email and password
        List<TextBox> textBoxes = GalleryTestHelper.FindAll<TextBox>(result.RootControl);
        textBoxes.Should().HaveCountGreaterThanOrEqualTo(2);

        // Divider
        GalleryTestHelper.FindAll<Separator>(result.RootControl).Should().NotBeEmpty();

        // Buttons: Sign in and Sign up
        List<Button> buttons = GalleryTestHelper.FindAll<Button>(result.RootControl);
        buttons.Should().HaveCountGreaterThanOrEqualTo(2);

        // Click signup link button
        Button? signupBtn = buttons.FirstOrDefault(b =>
            GalleryTestHelper.FindFirst<TextBlock>(b)?.Text == "Sign up");
        signupBtn.Should().NotBeNull();
        GalleryTestHelper.ClickButton(signupBtn!);
        result.ActionLog.Should().ContainSingle(a => a.EventName == "signup");
    }

    // ──────────────────────────────────────────────────────────────────
    // 10 Notification Permission — Card+Column+Row+Icon+Text+Button
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Example_10_NotificationPermission_RendersCorrectStructure()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/10_notification-permission.json");

        result.RootControl.Should().BeOfType<Border>();

        List<TextBlock> texts = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        texts.Should().Contain(tb => tb.Text == "Enable notification");
        texts.Should().Contain(tb => tb.Text == "Get alerts for order status changes");
        // Icon check → ✔
        texts.Should().Contain(tb => tb.Text == "\u2714");

        // Buttons: Yes and No
        List<Button> buttons = GalleryTestHelper.FindAll<Button>(result.RootControl);
        buttons.Should().HaveCountGreaterThanOrEqualTo(2);

        Button? yesBtn = buttons.FirstOrDefault(b =>
            GalleryTestHelper.FindFirst<TextBlock>(b)?.Text == "Yes");
        yesBtn.Should().NotBeNull();
        GalleryTestHelper.ClickButton(yesBtn!);
        result.ActionLog.Should().ContainSingle(a => a.EventName == "accept");
    }

    // ──────────────────────────────────────────────────────────────────
    // 11 Purchase Complete — Card+Column+Row+Image+Icon+Text+Button+Divider, formatCurrency
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    [Trait("Gap", "FunctionCall")]
    public void Example_11_PurchaseComplete_RendersCorrectStructure()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/11_purchase-complete.json");

        result.RootControl.Should().BeOfType<Border>();

        List<TextBlock> texts = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        texts.Should().Contain(tb => tb.Text == "Purchase Complete");
        texts.Should().Contain(tb => tb.Text == "Wireless Headphones Pro");
        texts.Should().Contain(tb => tb.Text == "Sold by:");
        texts.Should().Contain(tb => tb.Text == "TechStore Official");

        // Image
        GalleryTestHelper.FindAll<Image>(result.RootControl).Should().NotBeEmpty();

        // Button: View Order Details
        List<Button> buttons = GalleryTestHelper.FindAll<Button>(result.RootControl);
        Button? viewBtn = buttons.FirstOrDefault(b =>
            GalleryTestHelper.FindFirst<TextBlock>(b)?.Text == "View Order Details");
        viewBtn.Should().NotBeNull();
        GalleryTestHelper.ClickButton(viewBtn!);
        result.ActionLog.Should().ContainSingle(a => a.EventName == "view_details");
    }

    // ──────────────────────────────────────────────────────────────────
    // 12 Chat Message — Card+Column+Row+Image+Icon+Divider+Text, formatDate + template
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    [Trait("Gap", "FunctionCall")]
    [Trait("Gap", "TemplateChildren")]
    public void Example_12_ChatMessage_RendersCorrectStructure()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/12_chat-message.json");

        result.RootControl.Should().BeOfType<Border>();

        List<TextBlock> texts = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        texts.Should().Contain(tb => tb.Text == "project-updates");

        // Divider
        GalleryTestHelper.FindAll<Separator>(result.RootControl).Should().NotBeEmpty();

        result.ActionLog.Should().BeEmpty();
    }

    // ──────────────────────────────────────────────────────────────────
    // 13 Coffee Order — Card+Column+Row+Icon+Text+Button+Divider, formatCurrency + template
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    [Trait("Gap", "FunctionCall")]
    [Trait("Gap", "TemplateChildren")]
    public void Example_13_CoffeeOrder_RendersCorrectStructure()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/13_coffee-order.json");

        result.RootControl.Should().BeOfType<Border>();

        List<TextBlock> texts = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        texts.Should().Contain(tb => tb.Text == "Sunrise Coffee");
        texts.Should().Contain(tb => tb.Text == "Subtotal");
        texts.Should().Contain(tb => tb.Text == "Tax");
        texts.Should().Contain(tb => tb.Text == "Total");

        // Buttons: Purchase and Add to cart
        List<Button> buttons = GalleryTestHelper.FindAll<Button>(result.RootControl);
        buttons.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    // ──────────────────────────────────────────────────────────────────
    // 14 Sports Player — Card+Column+Row+Image+Divider+Text, path bindings only
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Example_14_SportsPlayer_RendersCorrectStructure()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/14_sports-player.json");

        result.RootControl.Should().BeOfType<Border>();

        List<TextBlock> texts = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        texts.Should().Contain(tb => tb.Text == "Marcus Johnson");
        texts.Should().Contain(tb => tb.Text == "#23");
        texts.Should().Contain(tb => tb.Text == "LA Lakers");
        texts.Should().Contain(tb => tb.Text == "28.4");
        texts.Should().Contain(tb => tb.Text == "PPG");

        // Image
        GalleryTestHelper.FindAll<Image>(result.RootControl).Should().NotBeEmpty();

        // Divider
        GalleryTestHelper.FindAll<Separator>(result.RootControl).Should().NotBeEmpty();

        result.ActionLog.Should().BeEmpty();
    }

    // ──────────────────────────────────────────────────────────────────
    // 15 Account Balance — Card+Column+Row+Icon+Divider+Text+Button, formatCurrency
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    [Trait("Gap", "FunctionCall")]
    public void Example_15_AccountBalance_RendersCorrectStructure()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/15_account-balance.json");

        result.RootControl.Should().BeOfType<Border>();

        List<TextBlock> texts = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        texts.Should().Contain(tb => tb.Text == "Primary Checking");
        texts.Should().Contain(tb => tb.Text == "Updated just now");
        texts.Should().Contain(tb => tb.Text == "Transfer");
        texts.Should().Contain(tb => tb.Text == "Pay Bill");

        // Buttons
        List<Button> buttons = GalleryTestHelper.FindAll<Button>(result.RootControl);
        buttons.Should().HaveCountGreaterThanOrEqualTo(2);

        Button? transferBtn = buttons.FirstOrDefault(b =>
            GalleryTestHelper.FindFirst<TextBlock>(b)?.Text == "Transfer");
        transferBtn.Should().NotBeNull();
        GalleryTestHelper.ClickButton(transferBtn!);
        result.ActionLog.Should().ContainSingle(a => a.EventName == "transfer");
    }

    // ──────────────────────────────────────────────────────────────────
    // 16 Workout Summary — Card+Column+Row+Icon+Divider+Text, formatNumber etc.
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    [Trait("Gap", "FunctionCall")]
    public void Example_16_WorkoutSummary_RendersCorrectStructure()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/16_workout-summary.json");

        result.RootControl.Should().BeOfType<Border>();

        List<TextBlock> texts = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        texts.Should().Contain(tb => tb.Text == "Workout Complete");
        // "Morning Run" is in data model as /workoutType but no component binds to it
        texts.Should().Contain(tb => tb.Text == "32:15");
        texts.Should().Contain(tb => tb.Text == "Duration");
        texts.Should().Contain(tb => tb.Text == "Calories");
        texts.Should().Contain(tb => tb.Text == "Distance");

        // Divider
        GalleryTestHelper.FindAll<Separator>(result.RootControl).Should().NotBeEmpty();

        result.ActionLog.Should().BeEmpty();
    }

    // ──────────────────────────────────────────────────────────────────
    // 17 Event Detail — Card+Column+Row+Icon+Divider+Text+Button, formatString(formatDate)
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    [Trait("Gap", "FunctionCall")]
    public void Example_17_EventDetail_RendersCorrectStructure()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/17_event-detail.json");

        result.RootControl.Should().BeOfType<Border>();

        List<TextBlock> texts = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        texts.Should().Contain(tb => tb.Text == "Product Launch Meeting");
        texts.Should().Contain(tb => tb.Text == "Accept");
        texts.Should().Contain(tb => tb.Text == "Decline");

        // Buttons
        List<Button> buttons = GalleryTestHelper.FindAll<Button>(result.RootControl);
        buttons.Should().HaveCountGreaterThanOrEqualTo(2);

        Button? acceptBtn = buttons.FirstOrDefault(b =>
            GalleryTestHelper.FindFirst<TextBlock>(b)?.Text == "Accept");
        acceptBtn.Should().NotBeNull();
        GalleryTestHelper.ClickButton(acceptBtn!);
        result.ActionLog.Should().ContainSingle(a => a.EventName == "accept");
    }

    // ──────────────────────────────────────────────────────────────────
    // 18 Track List — Card+Column+Row+Icon+Image+Divider+Text, formatNumber + template
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    [Trait("Gap", "FunctionCall")]
    [Trait("Gap", "TemplateChildren")]
    public void Example_18_TrackList_RendersCorrectStructure()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/18_track-list.json");

        result.RootControl.Should().BeOfType<Border>();

        List<TextBlock> texts = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        texts.Should().Contain(tb => tb.Text == "Focus Flow");

        // Divider
        GalleryTestHelper.FindAll<Separator>(result.RootControl).Should().NotBeEmpty();

        result.ActionLog.Should().BeEmpty();
    }

    // ──────────────────────────────────────────────────────────────────
    // 19 Software Purchase — Card+Column+Row+ChoicePicker+Text+Button+Divider, formatString(formatCurrency)
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    [Trait("Gap", "FunctionCall")]
    public void Example_19_SoftwarePurchase_RendersCorrectStructure()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/19_software-purchase.json");

        result.RootControl.Should().BeOfType<Border>();

        List<TextBlock> texts = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        texts.Should().Contain(tb => tb.Text == "Design Suite Pro");
        texts.Should().Contain(tb => tb.Text == "Purchase License");
        texts.Should().Contain(tb => tb.Text == "Number of seats");
        texts.Should().Contain(tb => tb.Text == "10 seats");

        // ChoicePicker → ComboBox
        ComboBox? combo = GalleryTestHelper.FindFirst<ComboBox>(result.RootControl);
        combo.Should().NotBeNull();

        // Buttons: Confirm and Cancel
        List<Button> buttons = GalleryTestHelper.FindAll<Button>(result.RootControl);
        buttons.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    // ──────────────────────────────────────────────────────────────────
    // 20 Restaurant Card — Card+Column+Row+Image+Icon+Text, path bindings only
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Example_20_RestaurantCard_RendersCorrectStructure()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/20_restaurant-card.json");

        result.RootControl.Should().BeOfType<Border>();

        List<TextBlock> texts = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        texts.Should().Contain(tb => tb.Text == "The Italian Kitchen");
        texts.Should().Contain(tb => tb.Text == "$$$");
        texts.Should().Contain(tb => tb.Text == "Italian \u2022 Pasta \u2022 Wine Bar");

        // Image
        GalleryTestHelper.FindAll<Image>(result.RootControl).Should().NotBeEmpty();

        result.ActionLog.Should().BeEmpty();
    }

    // ──────────────────────────────────────────────────────────────────
    // 21 Shipping Status — Card+Column+Row+Icon+Divider+Text, template children
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    [Trait("Gap", "TemplateChildren")]
    public void Example_21_ShippingStatus_RendersCorrectStructure()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/21_shipping-status.json");

        result.RootControl.Should().BeOfType<Border>();

        List<TextBlock> texts = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        texts.Should().Contain(tb => tb.Text == "Package Status");
        texts.Should().Contain(tb => tb.Text == "Tracking: 1Z999AA10123456784");
        texts.Should().Contain(tb => tb.Text == "Estimated delivery: Today by 8 PM");

        result.ActionLog.Should().BeEmpty();
    }

    // ──────────────────────────────────────────────────────────────────
    // 22 Credit Card — Card+Column+Row+Icon+Text, path bindings only
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Example_22_CreditCard_RendersCorrectStructure()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/22_credit-card.json");

        result.RootControl.Should().BeOfType<Border>();

        List<TextBlock> texts = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        texts.Should().Contain(tb => tb.Text == "VISA");
        texts.Should().Contain(tb => tb.Text == "\u2022\u2022\u2022\u2022 \u2022\u2022\u2022\u2022 \u2022\u2022\u2022\u2022 4242");
        texts.Should().Contain(tb => tb.Text == "SARAH JOHNSON");
        texts.Should().Contain(tb => tb.Text == "CARD HOLDER");
        texts.Should().Contain(tb => tb.Text == "EXPIRES");

        result.ActionLog.Should().BeEmpty();
    }

    // ──────────────────────────────────────────────────────────────────
    // 23 Step Counter — Card+Column+Row+Icon+Divider+Text, formatNumber+formatString
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    [Trait("Gap", "FunctionCall")]
    public void Example_23_StepCounter_RendersCorrectStructure()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/23_step-counter.json");

        result.RootControl.Should().BeOfType<Border>();

        List<TextBlock> texts = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        texts.Should().Contain(tb => tb.Text == "Today's Steps");
        texts.Should().Contain(tb => tb.Text == "Distance");
        texts.Should().Contain(tb => tb.Text == "Calories");

        // Divider
        GalleryTestHelper.FindAll<Separator>(result.RootControl).Should().NotBeEmpty();

        result.ActionLog.Should().BeEmpty();
    }

    // ──────────────────────────────────────────────────────────────────
    // 24 Recipe Card — Card+Column+Row+Icon+Image+Tabs+Text, formatString+formatNumber+pluralize + template
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    [Trait("Gap", "FunctionCall")]
    [Trait("Gap", "TemplateChildren")]
    public void Example_24_RecipeCard_RendersCorrectStructure()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/24_recipe-card.json");

        // Root is a Card → Border
        result.RootControl.Should().BeOfType<Border>();

        // Card wraps a TabControl directly — GetChildren doesn't traverse TabItem content,
        // so we verify structure at the TabControl level
        TabControl? tabs = GalleryTestHelper.FindFirst<TabControl>(result.RootControl);
        tabs.Should().NotBeNull();
        tabs!.Items.Should().HaveCount(3);

        // Verify tab headers
        var tabItems = tabs.Items.Cast<TabItem>().ToList();
        tabItems[0].Header.Should().Be("Overview");
        tabItems[1].Header.Should().Be("Ingredients");
        tabItems[2].Header.Should().Be("Instructions");

        result.ActionLog.Should().BeEmpty();
    }

    // ──────────────────────────────────────────────────────────────────
    // 25 Contact Card — Card+Column+Row+Image+Icon+Divider+Text+Button
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Example_25_ContactCard_RendersCorrectStructure()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/25_contact-card.json");

        result.RootControl.Should().BeOfType<Border>();

        List<TextBlock> texts = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        texts.Should().Contain(tb => tb.Text == "David Park");
        texts.Should().Contain(tb => tb.Text == "Engineering Manager");
        texts.Should().Contain(tb => tb.Text == "+1 (555) 234-5678");
        texts.Should().Contain(tb => tb.Text == "Call");
        texts.Should().Contain(tb => tb.Text == "Message");

        // Image
        GalleryTestHelper.FindAll<Image>(result.RootControl).Should().NotBeEmpty();

        // Divider
        GalleryTestHelper.FindAll<Separator>(result.RootControl).Should().NotBeEmpty();

        // Click Call button
        List<Button> buttons = GalleryTestHelper.FindAll<Button>(result.RootControl);
        Button? callBtn = buttons.FirstOrDefault(b =>
            GalleryTestHelper.FindFirst<TextBlock>(b)?.Text == "Call");
        callBtn.Should().NotBeNull();
        GalleryTestHelper.ClickButton(callBtn!);
        result.ActionLog.Should().ContainSingle(a => a.EventName == "call");
    }

    // ──────────────────────────────────────────────────────────────────
    // 26 Podcast Episode — Card+Column+Row+Image+AudioPlayer+Text, formatDate
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    [Trait("Gap", "FunctionCall")]
    public void Example_26_PodcastEpisode_RendersCorrectStructure()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/26_podcast-episode.json");

        result.RootControl.Should().BeOfType<Border>();

        List<TextBlock> texts = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        texts.Should().Contain(tb => tb.Text == "Tech Talk Daily");
        texts.Should().Contain(tb => tb.Text == "The Future of AI in Product Design");
        texts.Should().Contain(tb => tb.Text == "45 min");

        // Image
        GalleryTestHelper.FindAll<Image>(result.RootControl).Should().NotBeEmpty();

        // AudioPlayer renders as TextBlock placeholder
        texts.Should().Contain(tb => tb.Text != null && tb.Text.Contains("[AudioPlayer:"));

        result.ActionLog.Should().BeEmpty();
    }

    // ──────────────────────────────────────────────────────────────────
    // 27 Stats Card — Card+Column+Row+Icon+Text, formatCurrency+formatString
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    [Trait("Gap", "FunctionCall")]
    public void Example_27_StatsCard_RendersCorrectStructure()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/27_stats-card.json");

        result.RootControl.Should().BeOfType<Border>();

        List<TextBlock> texts = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        texts.Should().Contain(tb => tb.Text == "Monthly Revenue");

        result.ActionLog.Should().BeEmpty();
    }

    // ──────────────────────────────────────────────────────────────────
    // 28 Countdown Timer — Card+Column+Row+Text, formatDate
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    [Trait("Gap", "FunctionCall")]
    public void Example_28_CountdownTimer_RendersCorrectStructure()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/28_countdown-timer.json");

        result.RootControl.Should().BeOfType<Border>();

        List<TextBlock> texts = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        texts.Should().Contain(tb => tb.Text == "Product Launch");
        texts.Should().Contain(tb => tb.Text == "14");
        texts.Should().Contain(tb => tb.Text == "08");
        texts.Should().Contain(tb => tb.Text == "Days");
        texts.Should().Contain(tb => tb.Text == "Hours");
        texts.Should().Contain(tb => tb.Text == "Minutes");

        result.ActionLog.Should().BeEmpty();
    }

    // ──────────────────────────────────────────────────────────────────
    // 29 Movie Card — Card+Column+Row+Image+Icon+Text+Button+Modal+Video
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Example_29_MovieCard_RendersCorrectStructure()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/29_movie-card.json");

        result.RootControl.Should().BeOfType<Border>();

        List<TextBlock> texts = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        texts.Should().Contain(tb => tb.Text == "Interstellar");
        texts.Should().Contain(tb => tb.Text == "(2014)");
        texts.Should().Contain(tb => tb.Text == "Sci-Fi \u2022 Adventure \u2022 Drama");
        texts.Should().Contain(tb => tb.Text == "Watch Trailer");

        // Image (poster)
        GalleryTestHelper.FindAll<Image>(result.RootControl).Should().NotBeEmpty();

        // Video renders as TextBlock placeholder
        texts.Should().Contain(tb => tb.Text != null && tb.Text.Contains("[Video:"));

        // Button: Watch Trailer → open_trailer
        List<Button> buttons = GalleryTestHelper.FindAll<Button>(result.RootControl);
        Button? trailerBtn = buttons.FirstOrDefault(b =>
            GalleryTestHelper.FindFirst<TextBlock>(b)?.Text == "Watch Trailer");
        trailerBtn.Should().NotBeNull();
        GalleryTestHelper.ClickButton(trailerBtn!);
        result.ActionLog.Should().ContainSingle(a => a.EventName == "open_trailer");
    }

    // ──────────────────────────────────────────────────────────────────
    // 30 Live Invitation Builder — Card+Column+Row+Image+TextField+DateTimeInput+ChoicePicker+Text, formatDate+formatString
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    [Trait("Gap", "FunctionCall")]
    public void Example_30_LiveInvitationBuilder_RendersCorrectStructure()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/30_live-invitation-builder.json");

        // Root is Column → StackPanel (not Card)
        result.RootControl.Should().BeOfType<StackPanel>();

        List<TextBlock> texts = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        texts.Should().Contain(tb => tb.Text == "# Invitation Builder");
        texts.Should().Contain(tb => tb.Text == "Live Preview");
        texts.Should().Contain(tb => tb.Text == "Celebrating");

        // TextField
        List<TextBox> textBoxes = GalleryTestHelper.FindAll<TextBox>(result.RootControl);
        textBoxes.Should().NotBeEmpty();

        // DateTimeInput → CalendarDatePicker
        CalendarDatePicker? picker = GalleryTestHelper.FindFirst<CalendarDatePicker>(result.RootControl);
        picker.Should().NotBeNull();

        // ChoicePicker → ComboBox
        ComboBox? combo = GalleryTestHelper.FindFirst<ComboBox>(result.RootControl);
        combo.Should().NotBeNull();
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
        result.RootControl.Should().BeOfType<StackPanel>();

        List<TextBlock> texts = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        texts.Should().Contain(tb => tb.Text == "System Dashboard");

        // List → ScrollViewer
        ScrollViewer? scrollViewer = GalleryTestHelper.FindFirst<ScrollViewer>(result.RootControl);
        scrollViewer.Should().NotBeNull();

        result.ActionLog.Should().BeEmpty();
    }

    // ──────────────────────────────────────────────────────────────────
    // 32 Advanced Form Validator — Card+Column+TextField+CheckBox+Button+Text, formatString+formatDate, HAS CHECKS
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    [Trait("Gap", "FunctionCall")]
    [Trait("Gap", "CheckValidation")]
    public void Example_32_AdvancedFormValidator_RendersCorrectStructure()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/32_advanced-form-validator.json");

        result.RootControl.Should().BeOfType<Border>();

        List<TextBlock> texts = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        texts.Should().Contain(tb => tb.Text == "Submit Registration");

        // TextFields
        List<TextBox> textBoxes = GalleryTestHelper.FindAll<TextBox>(result.RootControl);
        textBoxes.Should().NotBeEmpty();

        // CheckBox
        CheckBox? cb = GalleryTestHelper.FindFirst<CheckBox>(result.RootControl);
        cb.Should().NotBeNull();

        // Button: Submit Registration → register
        List<Button> buttons = GalleryTestHelper.FindAll<Button>(result.RootControl);
        buttons.Should().NotBeEmpty();
        Button? submitBtn = buttons.FirstOrDefault(b =>
            GalleryTestHelper.FindFirst<TextBlock>(b)?.Text == "Submit Registration");
        submitBtn.Should().NotBeNull();
        GalleryTestHelper.ClickButton(submitBtn!);
        result.ActionLog.Should().ContainSingle(a => a.EventName == "register");
    }

    // ──────────────────────────────────────────────────────────────────
    // 33 Financial Data Grid — Card+Column+Row+Icon+List+Text, formatCurrency+formatString + template
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    [Trait("Gap", "FunctionCall")]
    [Trait("Gap", "TemplateChildren")]
    public void Example_33_FinancialDataGrid_RendersCorrectStructure()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/33_financial-data-grid.json");

        result.RootControl.Should().BeOfType<Border>();

        List<TextBlock> texts = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        texts.Should().Contain(tb => tb.Text == "Asset");
        texts.Should().Contain(tb => tb.Text == "Price");
        texts.Should().Contain(tb => tb.Text == "24h Change");
        texts.Should().Contain(tb => tb.Text == "Market Cap");

        // List → ScrollViewer
        ScrollViewer? scrollViewer = GalleryTestHelper.FindFirst<ScrollViewer>(result.RootControl);
        scrollViewer.Should().NotBeNull();

        result.ActionLog.Should().BeEmpty();
    }
}
