using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using FluentAssertions;

namespace A2Ui.Avalonia.Tests.Integration.Minimal;

/// <summary>
/// Integration tests for minimal/4_login_form.json.
/// Column with h2 "Login", two TextFields (username, password), Button "Sign In".
/// Two-way binding and action context resolution.
/// </summary>
public sealed class LoginFormTests
{
    [AvaloniaFact]
    public void Root_IsStackPanel_ColumnWithJustifyStart()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/4_login_form.json");

        result.RootControl.Should().BeOfType<StackPanel>();
    }

    [AvaloniaFact]
    public void Contains_LoginTitle_AsH2()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/4_login_form.json");

        List<TextBlock> textBlocks = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        textBlocks.Should().Contain(tb => tb.Text == "Login" && tb.Classes.Contains("Heading2"));
    }

    [AvaloniaFact]
    public void Contains_TwoTextBoxes()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/4_login_form.json");

        List<TextBox> textBoxes = GalleryTestHelper.FindAll<TextBox>(result.RootControl);
        textBoxes.Should().HaveCount(2);
    }

    [AvaloniaFact]
    public void UsernameTextBox_HasWatermark()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/4_login_form.json");

        List<TextBox> textBoxes = GalleryTestHelper.FindAll<TextBox>(result.RootControl);
        textBoxes[0].Watermark.Should().Be("Username");
    }

    [AvaloniaFact]
    public void PasswordTextBox_IsObscured()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/4_login_form.json");

        List<TextBox> textBoxes = GalleryTestHelper.FindAll<TextBox>(result.RootControl);
        textBoxes[1].PasswordChar.Should().NotBe('\0');
    }

    [AvaloniaFact]
    public void Contains_Button_WithSignInLabel()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/4_login_form.json");

        Button? button = GalleryTestHelper.FindFirst<Button>(result.RootControl);
        button.Should().NotBeNull();
        button!.Content.Should().BeOfType<TextBlock>();
        var label = (TextBlock)button.Content!;
        label.Text.Should().Be("Sign In");
    }

    [AvaloniaFact]
    public void TwoWayBinding_SetUsername_UpdatesDataModel()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/4_login_form.json");

        List<TextBox> textBoxes = GalleryTestHelper.FindAll<TextBox>(result.RootControl);
        GalleryTestHelper.SetText(textBoxes[0], "alice");

        result.Surface.DataModel.Resolve(new Core.Messages.DynamicValue { Path = "/username" }).Should().Be("alice");
    }

    [AvaloniaFact]
    public void TwoWayBinding_SetPassword_UpdatesDataModel()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/4_login_form.json");

        List<TextBox> textBoxes = GalleryTestHelper.FindAll<TextBox>(result.RootControl);
        GalleryTestHelper.SetText(textBoxes[1], "secret123");

        result
            .Surface.DataModel.Resolve(new Core.Messages.DynamicValue { Path = "/password" })
            .Should()
            .Be("secret123");
    }

    [AvaloniaFact]
    public void ClickSubmit_FiresLoginSubmittedAction_WithResolvedContext()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/4_login_form.json");

        List<TextBox> textBoxes = GalleryTestHelper.FindAll<TextBox>(result.RootControl);
        GalleryTestHelper.SetText(textBoxes[0], "alice");
        GalleryTestHelper.SetText(textBoxes[1], "secret123");

        Button? button = GalleryTestHelper.FindFirst<Button>(result.RootControl);
        button.Should().NotBeNull();
        GalleryTestHelper.ClickButton(button!);

        UserActionEventArgs loginAction = result.ActionLog.Single(a => a.EventName == "login_submitted");
        loginAction.Payload.Should().BeOfType<Dictionary<string, string?>>();

        var payload = (Dictionary<string, string?>)loginAction.Payload!;
        payload["user"].Should().Be("alice");
        payload["pass"].Should().Be("secret123");
    }
}
