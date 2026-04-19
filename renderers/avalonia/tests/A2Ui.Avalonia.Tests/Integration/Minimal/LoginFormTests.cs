using A2Ui.Core.Actions;
using A2Ui.Core.Bindings;
using A2Ui.Core.Surfaces;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Xunit;

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

        Assert.IsType<StackPanel>(result.RootControl);
    }

    [AvaloniaFact]
    public void Contains_LoginTitle_AsH2()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/4_login_form.json");

        List<TextBlock> textBlocks = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        Assert.Contains(textBlocks, tb => tb.Text == "Login" && tb.Classes.Contains("Heading2"));
    }

    [AvaloniaFact]
    public void Contains_TwoTextBoxes()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/4_login_form.json");

        List<TextBox> textBoxes = GalleryTestHelper.FindAll<TextBox>(result.RootControl);
        Assert.Equal(2, textBoxes.Count);
    }

    [AvaloniaFact]
    public void UsernameTextBox_HasPlaceholder()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/4_login_form.json");

        List<TextBox> textBoxes = GalleryTestHelper.FindAll<TextBox>(result.RootControl);
        Assert.Equal("Username", textBoxes[0].PlaceholderText);
    }

    [AvaloniaFact]
    public void PasswordTextBox_IsObscured()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/4_login_form.json");

        List<TextBox> textBoxes = GalleryTestHelper.FindAll<TextBox>(result.RootControl);
        Assert.NotEqual('\0', textBoxes[1].PasswordChar);
    }

    [AvaloniaFact]
    public void Contains_Button_WithSignInLabel()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/4_login_form.json");

        Button? button = GalleryTestHelper.FindFirst<Button>(result.RootControl);
        Assert.NotNull(button);
        var label = Assert.IsType<TextBlock>(button.Content);
        Assert.Equal("Sign In", label.Text);
    }

    [AvaloniaFact]
    public void TwoWayBinding_SetUsername_UpdatesDataModel()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/4_login_form.json");

        List<TextBox> textBoxes = GalleryTestHelper.FindAll<TextBox>(result.RootControl);
        GalleryTestHelper.SetText(textBoxes[0], "alice");

        Assert.Equal("alice", result.Surface.DataModel.Resolve(Core.Bindings.DynamicValue.FromPath("/username")));
    }

    [AvaloniaFact]
    public void TwoWayBinding_SetPassword_UpdatesDataModel()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/4_login_form.json");

        List<TextBox> textBoxes = GalleryTestHelper.FindAll<TextBox>(result.RootControl);
        GalleryTestHelper.SetText(textBoxes[1], "secret123");

        Assert.Equal("secret123", result.Surface.DataModel.Resolve(Core.Bindings.DynamicValue.FromPath("/password")));
    }

    [AvaloniaFact]
    public void ClickSubmit_FiresLoginSubmittedAction_WithResolvedContext()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/4_login_form.json");

        List<TextBox> textBoxes = GalleryTestHelper.FindAll<TextBox>(result.RootControl);
        GalleryTestHelper.SetText(textBoxes[0], "alice");
        GalleryTestHelper.SetText(textBoxes[1], "secret123");

        Button? button = GalleryTestHelper.FindFirst<Button>(result.RootControl);
        Assert.NotNull(button);
        GalleryTestHelper.ClickButton(button);

        UserActionEventArgs loginAction = result.ActionLog.Single(a => a.EventName == "login_submitted");
        var payload = Assert.IsType<Dictionary<string, string?>>(loginAction.Payload);
        Assert.Equal("alice", payload["user"]);
        Assert.Equal("secret123", payload["pass"]);
    }
}
