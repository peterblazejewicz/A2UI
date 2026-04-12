using System.Text.Json;
using A2Ui.Core.Messages;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Xunit;

namespace A2Ui.Avalonia.Tests.Integration.Basic;

/// <summary>
/// Integration tests for check validation on form components.
/// Verifies that failing checks produce inline error messages for input controls
/// and that buttons are disabled when their checks fail.
/// </summary>
public sealed class CheckValidationTests
{
    // ──────────────────────────────────────────────────────────────────
    // 09 Login Form — TextField checks + Button checks
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void LoginForm_EmptyFields_ShowsFirstValidationErrorPerField()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/09_login-form.json");

        // Only the FIRST failing check per field is shown (matches Lit reference).
        // Email: "required" fails first → shows "Email is required" (not the email format error)
        // Password: "required" fails first → shows "Password is required" (not the length error)
        List<TextBlock> allText = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);

        Assert.Contains(allText, tb => tb.Text == "Email is required");
        Assert.DoesNotContain(allText, tb => tb.Text == "Please enter a valid email address");
        Assert.Contains(allText, tb => tb.Text == "Password is required");
        Assert.DoesNotContain(allText, tb => tb.Text == "Password must be at least 8 characters long");
    }

    [AvaloniaFact]
    public void LoginForm_EmptyFields_DisablesLoginButton()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/09_login-form.json");

        List<Button> buttons = GalleryTestHelper.FindAll<Button>(result.RootControl);
        Button? loginBtn = buttons.FirstOrDefault(b => GalleryTestHelper.FindFirst<TextBlock>(b)?.Text == "Sign in");
        Assert.NotNull(loginBtn);
        Assert.False(loginBtn.IsEnabled);

        // Tooltip should show the failure message
        string? tip = ToolTip.GetTip(loginBtn) as string;
        Assert.Equal("Please fix errors before signing in", tip);
    }

    [AvaloniaFact]
    public void LoginForm_SignupButton_RemainsEnabled()
    {
        // The signup button has no checks, so it should stay enabled.
        RenderResult result = GalleryTestHelper.ReplayExample("basic/09_login-form.json");

        List<Button> buttons = GalleryTestHelper.FindAll<Button>(result.RootControl);
        Button? signupBtn = buttons.FirstOrDefault(b => GalleryTestHelper.FindFirst<TextBlock>(b)?.Text == "Sign up");
        Assert.NotNull(signupBtn);
        Assert.True(signupBtn.IsEnabled);
    }

    [AvaloniaFact]
    public void LoginForm_ValidData_ClearsErrorsAndEnablesButton()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/09_login-form.json");

        // Simulate user entering valid data via the data model.
        SetDataModelValue(result, "/email", "\"user@example.com\"");
        SetDataModelValue(result, "/password", "\"securepass123\"");

        // Re-render to pick up the new data model values.
        Control reRendered = result.ReRender();

        // Validation errors should be gone.
        List<TextBlock> allText = GalleryTestHelper.FindAll<TextBlock>(reRendered);
        Assert.DoesNotContain(allText, tb => tb.Text == "Email is required");
        Assert.DoesNotContain(allText, tb => tb.Text == "Please enter a valid email address");
        Assert.DoesNotContain(allText, tb => tb.Text == "Password is required");
        Assert.DoesNotContain(allText, tb => tb.Text == "Password must be at least 8 characters long");

        // Login button should now be enabled.
        List<Button> buttons = GalleryTestHelper.FindAll<Button>(reRendered);
        Button? loginBtn = buttons.FirstOrDefault(b => GalleryTestHelper.FindFirst<TextBlock>(b)?.Text == "Sign in");
        Assert.NotNull(loginBtn);
        Assert.True(loginBtn.IsEnabled);
    }

    // ──────────────────────────────────────────────────────────────────
    // 32 Advanced Form Validator — email/phone/zip checks + and/or logic
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void AdvancedFormValidator_EmptyFields_DisablesSubmitButton()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/32_advanced-form-validator.json");

        List<Button> buttons = GalleryTestHelper.FindAll<Button>(result.RootControl);
        Button? submitBtn = buttons.FirstOrDefault(b =>
            GalleryTestHelper.FindFirst<TextBlock>(b)?.Text == "Submit Registration"
        );
        Assert.NotNull(submitBtn);
        Assert.False(submitBtn.IsEnabled);
    }

    [AvaloniaFact]
    public void AdvancedFormValidator_EmptyFields_ShowsFieldValidationErrors()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/32_advanced-form-validator.json");

        List<TextBlock> allText = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);

        // Email check fails on empty value
        Assert.Contains(allText, tb => tb.Text == "Invalid email format");
        // Phone regex check fails on empty value
        Assert.Contains(allText, tb => tb.Text == "Invalid phone format");
        // Zip regex check fails on empty value
        Assert.Contains(allText, tb => tb.Text == "Must be exactly 5 digits");
    }

    [AvaloniaFact]
    public void AdvancedFormValidator_ValidData_EnablesSubmitButton()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/32_advanced-form-validator.json");

        // Fill in valid data
        SetDataModelValue(result, "/formData/email", "\"user@example.com\"");
        SetDataModelValue(result, "/formData/zip", "\"12345\"");
        SetDataModelValue(result, "/formData/agree", "true");

        Control reRendered = result.ReRender();

        List<Button> buttons = GalleryTestHelper.FindAll<Button>(reRendered);
        Button? submitBtn = buttons.FirstOrDefault(b =>
            GalleryTestHelper.FindFirst<TextBlock>(b)?.Text == "Submit Registration"
        );
        Assert.NotNull(submitBtn);
        Assert.True(submitBtn.IsEnabled);
    }

    // ──────────────────────────────────────────────────────────────────
    // Validation error TextBlocks have the correct style class
    // ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void ValidationErrors_HaveValidationErrorStyleClass()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("basic/09_login-form.json");

        List<TextBlock> allText = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        TextBlock? errorBlock = allText.FirstOrDefault(tb => tb.Text == "Email is required");
        Assert.NotNull(errorBlock);
        Assert.Contains("ValidationError", errorBlock.Classes);
    }

    /// <summary>
    /// Helper to set a value in the data model at a JSON Pointer path.
    /// <paramref name="jsonValue"/> must be valid JSON (e.g. <c>"\"hello\""</c> for a string).
    /// </summary>
    private static void SetDataModelValue(RenderResult result, string path, string jsonValue)
    {
        using JsonDocument doc = JsonDocument.Parse(jsonValue);
        result.Surface.DataModel.Apply(
            new UpdateDataModel
            {
                SurfaceId = result.Surface.SurfaceId,
                Path = path,
                Value = doc.RootElement.Clone(),
            }
        );
    }
}
