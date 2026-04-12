using Avalonia.Headless.XUnit;
using Xunit;

namespace A2Ui.Avalonia.Tests.Integration;

public sealed class AllExamplesSmokeTests
{
    public static IEnumerable<object[]> MinimalExamples()
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "Specs", "minimal");
        if (!Directory.Exists(dir))
        {
            yield break;
        }

        foreach (var f in Directory.GetFiles(dir, "*.json").Order())
        {
            yield return [Path.Combine("minimal", Path.GetFileName(f))];
        }
    }

    public static IEnumerable<object[]> BasicExamples()
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "Specs", "basic");
        if (!Directory.Exists(dir))
        {
            yield break;
        }

        foreach (var f in Directory.GetFiles(dir, "*.json").Order())
        {
            yield return [Path.Combine("basic", Path.GetFileName(f))];
        }
    }

    [AvaloniaTheory]
    [MemberData(nameof(MinimalExamples))]
    [MemberData(nameof(BasicExamples))]
    public void Example_LoadsAndRenders_WithoutException(string specPath)
    {
        var result = GalleryTestHelper.ReplayExample(specPath);

        Assert.NotNull(result.RootControl);
        Assert.NotEmpty(result.Surface.Components);
    }
}
