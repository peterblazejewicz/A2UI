namespace A2Ui.Core.Tests;

public sealed class OneOfTests
{
    // --- OneOf<T1, T2> ---

    [Fact]
    public void ImplicitConversion_T1_SetsIsT1True()
    {
        OneOf<int, string> value = 42;

        Assert.True(value.IsT1);
        Assert.False(value.IsT2);
    }

    [Fact]
    public void ImplicitConversion_T2_SetsIsT2True()
    {
        OneOf<int, string> value = "hello";

        Assert.False(value.IsT1);
        Assert.True(value.IsT2);
    }

    [Fact]
    public void Match_Case1_ReturnsCorrectBranch()
    {
        OneOf<int, string> value = 42;

        string result = value.Match(i => $"int:{i}", s => $"str:{s}");

        Assert.Equal("int:42", result);
    }

    [Fact]
    public void Match_Case2_ReturnsCorrectBranch()
    {
        OneOf<int, string> value = "hello";

        string result = value.Match(i => $"int:{i}", s => $"str:{s}");

        Assert.Equal("str:hello", result);
    }

    [Fact]
    public void Switch_Case1_InvokesCorrectAction()
    {
        OneOf<int, string> value = 42;
        int? captured = null;

        value.Switch(i => captured = i, _ => Assert.Fail("Wrong branch"));

        Assert.Equal(42, captured);
    }

    [Fact]
    public void Switch_Case2_InvokesCorrectAction()
    {
        OneOf<int, string> value = "hello";
        string? captured = null;

        value.Switch(_ => Assert.Fail("Wrong branch"), s => captured = s);

        Assert.Equal("hello", captured);
    }

    [Fact]
    public void TryGetAsT1_WhenCase1_ReturnsTrueAndValue()
    {
        OneOf<int, string> value = 42;

        bool result = value.TryGetAsT1(out int extracted);

        Assert.True(result);
        Assert.Equal(42, extracted);
    }

    [Fact]
    public void TryGetAsT1_WhenCase2_ReturnsFalse()
    {
        OneOf<int, string> value = "hello";

        bool result = value.TryGetAsT1(out int extracted);

        Assert.False(result);
        Assert.Equal(default, extracted);
    }

    [Fact]
    public void TryGetAsT2_WhenCase2_ReturnsTrueAndValue()
    {
        OneOf<int, string> value = "hello";

        bool result = value.TryGetAsT2(out string? extracted);

        Assert.True(result);
        Assert.Equal("hello", extracted);
    }

    [Fact]
    public void TryGetAsT2_WhenCase1_ReturnsFalse()
    {
        OneOf<int, string> value = 42;

        bool result = value.TryGetAsT2(out string? extracted);

        Assert.False(result);
        Assert.Null(extracted);
    }

    [Fact]
    public void Equality_SameCaseAndValue_AreEqual()
    {
        OneOf<int, string> a = 42;
        OneOf<int, string> b = 42;

        Assert.Equal(a, b);
    }

    [Fact]
    public void Equality_DifferentCases_AreNotEqual()
    {
        OneOf<int, string> a = 42;
        OneOf<int, string> b = "42";

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Equality_SameCaseDifferentValues_AreNotEqual()
    {
        OneOf<int, string> a = 1;
        OneOf<int, string> b = 2;

        Assert.NotEqual(a, b);
    }

    // --- OneOf<T1, T2, T3> ---

    [Fact]
    public void ThreeArg_ImplicitConversion_T1_SetsCorrectFlags()
    {
        OneOf<int, string, bool> value = 42;

        Assert.True(value.IsT1);
        Assert.False(value.IsT2);
        Assert.False(value.IsT3);
    }

    [Fact]
    public void ThreeArg_ImplicitConversion_T2_SetsCorrectFlags()
    {
        OneOf<int, string, bool> value = "hello";

        Assert.False(value.IsT1);
        Assert.True(value.IsT2);
        Assert.False(value.IsT3);
    }

    [Fact]
    public void ThreeArg_ImplicitConversion_T3_SetsCorrectFlags()
    {
        OneOf<int, string, bool> value = true;

        Assert.False(value.IsT1);
        Assert.False(value.IsT2);
        Assert.True(value.IsT3);
    }

    [Fact]
    public void ThreeArg_Match_EachCase_ReturnsCorrectBranch()
    {
        OneOf<int, string, bool> v1 = 42;
        OneOf<int, string, bool> v2 = "hi";
        OneOf<int, string, bool> v3 = true;

        string Fmt(OneOf<int, string, bool> v) => v.Match(i => $"int:{i}", s => $"str:{s}", b => $"bool:{b}");

        Assert.Equal("int:42", Fmt(v1));
        Assert.Equal("str:hi", Fmt(v2));
        Assert.Equal("bool:True", Fmt(v3));
    }

    [Fact]
    public void ThreeArg_Switch_Case3_InvokesCorrectAction()
    {
        OneOf<int, string, bool> value = true;
        bool? captured = null;

        value.Switch(_ => Assert.Fail("Wrong branch"), _ => Assert.Fail("Wrong branch"), b => captured = b);

        Assert.True(captured);
    }

    [Fact]
    public void ThreeArg_TryGetAsT3_WhenCase3_ReturnsTrueAndValue()
    {
        OneOf<int, string, bool> value = true;

        bool result = value.TryGetAsT3(out bool extracted);

        Assert.True(result);
        Assert.True(extracted);
    }

    [Fact]
    public void ThreeArg_TryGetAsT3_WhenNotCase3_ReturnsFalse()
    {
        OneOf<int, string, bool> value = 42;

        bool result = value.TryGetAsT3(out bool extracted);

        Assert.False(result);
        Assert.Equal(default, extracted);
    }

    [Fact]
    public void ThreeArg_Equality_SameCaseAndValue_AreEqual()
    {
        OneOf<int, string, bool> a = true;
        OneOf<int, string, bool> b = true;

        Assert.Equal(a, b);
    }

    [Fact]
    public void ThreeArg_Equality_DifferentCases_AreNotEqual()
    {
        OneOf<int, string, bool> a = 42;
        OneOf<int, string, bool> b = "42";

        Assert.NotEqual(a, b);
    }
}
