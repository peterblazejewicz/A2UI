using A2Ui.Avalonia.Functions;
using FluentAssertions;
using Xunit;

namespace A2Ui.Avalonia.Tests.Functions;

public sealed class ExpressionParserTests
{
    private readonly ExpressionParser _parser = new();

    // ── Parse: literal strings ───────────────────────────────────────

    [Fact]
    public void Parse_LiteralString_ReturnsSingleLiteral()
    {
        IReadOnlyList<ExpressionToken> result = this._parser.Parse("hello world");
        result.Should().HaveCount(1);
        result[0].Should().Be(new LiteralToken("hello world"));
    }

    // ── Parse: simple interpolation ──────────────────────────────────

    [Fact]
    public void Parse_SimpleInterpolation_ReturnsLiteralAndPath()
    {
        IReadOnlyList<ExpressionToken> result = this._parser.Parse("hello ${foo}");
        result.Should().HaveCount(2);
        result[0].Should().Be(new LiteralToken("hello "));
        result[1].Should().Be(new PathToken("foo"));
    }

    [Fact]
    public void Parse_NumberInterpolation_ReturnsLiteralAndPath()
    {
        IReadOnlyList<ExpressionToken> result = this._parser.Parse("number is ${num}");
        result.Should().HaveCount(2);
        result[0].Should().Be(new LiteralToken("number is "));
        result[1].Should().Be(new PathToken("num"));
    }

    // ── Parse: nested interpolation ──────────────────────────────────

    [Fact]
    public void Parse_NestedInterpolation_ResolvesInnerPath()
    {
        IReadOnlyList<ExpressionToken> result = this._parser.Parse("val is ${${nested}}");
        result.Should().HaveCount(2);
        result[0].Should().Be(new LiteralToken("val is "));
        result[1].Should().Be(new PathToken("nested"));
    }

    [Fact]
    public void Parse_DeepNestedStringLiteral_Resolves()
    {
        IReadOnlyList<ExpressionToken> result = this._parser.Parse("${${\"hello\"}}");
        result.Should().HaveCount(1);
        result[0].Should().Be(new LiteralToken("hello"));
    }

    // ── Parse: escaped interpolation ─────────────────────────────────

    [Fact]
    public void Parse_EscapedInterpolation_EmitsLiteralDollarBrace()
    {
        IReadOnlyList<ExpressionToken> result = this._parser.Parse("escaped \\${foo}");
        result.Should().HaveCount(3);
        result[0].Should().Be(new LiteralToken("escaped "));
        result[1].Should().Be(new LiteralToken("${"));
        result[2].Should().Be(new LiteralToken("foo}"));
    }

    // ── Parse: function calls ────────────────────────────────────────

    [Fact]
    public void Parse_FunctionCall_ReturnsFunctionCallToken()
    {
        IReadOnlyList<ExpressionToken> result = this._parser.Parse("sum is ${add(a: 10, b: 20)}");
        result.Should().HaveCount(2);
        result[0].Should().Be(new LiteralToken("sum is "));

        var fc = result[1].Should().BeOfType<FunctionCallToken>().Subject;
        fc.Name.Should().Be("add");
        fc.Args.Should().HaveCount(2);
        fc.Args["a"].Should().Be(new NumberToken(10));
        fc.Args["b"].Should().Be(new NumberToken(20));
    }

    [Fact]
    public void Parse_FunctionCallWithStringLiterals_ParsesCorrectly()
    {
        IReadOnlyList<ExpressionToken> result = this._parser.Parse("case is ${upper(text: \"hello\")}");
        result.Should().HaveCount(2);
        result[0].Should().Be(new LiteralToken("case is "));

        var fc = result[1].Should().BeOfType<FunctionCallToken>().Subject;
        fc.Name.Should().Be("upper");
        fc.Args["text"].Should().Be(new LiteralToken("hello"));
    }

    // ── Parse: keywords ──────────────────────────────────────────────

    [Fact]
    public void Parse_Keywords_ReturnsBoolAndEmptyForNull()
    {
        // "${true} ${false} ${null}" parses to:
        //   BoolToken(true), LiteralToken(" "), BoolToken(false), LiteralToken(" ")
        // null → empty LiteralToken which is filtered out by the empty-filter step
        IReadOnlyList<ExpressionToken> result = this._parser.Parse("${true} ${false} ${null}");
        result.Should().HaveCount(4);
        result[0].Should().Be(new BoolToken(true));
        result[1].Should().Be(new LiteralToken(" "));
        result[2].Should().Be(new BoolToken(false));
        result[3].Should().Be(new LiteralToken(" "));
    }

    // ── Parse: max depth ─────────────────────────────────────────────

    [Fact]
    public void Parse_MaxDepthExceeded_ThrowsA2UiExpressionException()
    {
        Action act = () => this._parser.Parse("depth", 11);
        act.Should().Throw<A2UiExpressionException>().WithMessage("*Max recursion depth*");
    }

    // ── Parse: unclosed interpolation ────────────────────────────────

    [Fact]
    public void Parse_UnclosedInterpolation_ThrowsA2UiExpressionException()
    {
        Action act = () => this._parser.Parse("hello ${world");
        act.Should().Throw<A2UiExpressionException>().WithMessage("*Unclosed interpolation*");
    }

    // ── Parse: invalid function syntax ───────────────────────────────

    [Fact]
    public void Parse_MissingClosingParenthesis_Throws()
    {
        Action act = () => this._parser.Parse("${add(a: 1, b: 2}");
        act.Should().Throw<A2UiExpressionException>().WithMessage("*Expected ')'*");
    }

    // ── Parse: unexpected characters at end ──────────────────────────

    [Fact]
    public void Parse_UnexpectedCharactersAtEnd_Throws()
    {
        Action act = () => this._parser.Parse("${true false}");
        act.Should().Throw<A2UiExpressionException>().WithMessage("*Unexpected characters*");
    }

    // ── ParseExpression: empty identifiers ───────────────────────────

    [Fact]
    public void Parse_EmptyParentheses_ReturnsFunctionCallWithNoArgs()
    {
        IReadOnlyList<ExpressionToken> result = this._parser.Parse("${()}");
        result.Should().HaveCount(1);
        var fc = result[0].Should().BeOfType<FunctionCallToken>().Subject;
        fc.Name.Should().Be("");
        fc.Args.Should().BeEmpty();
    }

    [Fact]
    public void ParseExpression_EmptyString_ReturnsEmptyLiteral()
    {
        ExpressionToken result = this._parser.ParseExpression("");
        result.Should().Be(new LiteralToken(""));
    }

    [Fact]
    public void ParseExpression_EmptyParentheses_ReturnsFunctionCall()
    {
        ExpressionToken result = this._parser.ParseExpression("()");
        var fc = result.Should().BeOfType<FunctionCallToken>().Subject;
        fc.Name.Should().Be("");
        fc.Args.Should().BeEmpty();
    }

    // ── ParseExpression: string literal escape sequences ─────────────

    [Fact]
    public void ParseExpression_StringLiteralEscapeSequences_ParsesCorrectly()
    {
        ExpressionToken result = this._parser.ParseExpression("'line1\\nline2\\t\\r\\'\\\\x'");
        result.Should().Be(new LiteralToken("line1\nline2\t\r'\\x"));
    }

    // ── ParseExpression: paths with special characters ───────────────

    [Fact]
    public void ParseExpression_PathWithSpecialChars_ReturnsPathToken()
    {
        ExpressionToken result = this._parser.ParseExpression("my-path.with_underscores");
        result.Should().Be(new PathToken("my-path.with_underscores"));
    }

    // ── ParseExpression: missing colon in function args ──────────────

    [Fact]
    public void ParseExpression_MissingColonInFunctionArgs_Throws()
    {
        Action act = () => this._parser.ParseExpression("add(a 10, b: 20)");
        act.Should().Throw<A2UiExpressionException>().WithMessage("*Expected ':'*");
    }

    // ── Parse: empty string ──────────────────────────────────────────

    [Fact]
    public void Parse_EmptyString_ReturnsLiteralOrEmpty()
    {
        // Parse("") has no "${" so it returns [LiteralToken("")] via the early-return path.
        // The empty-literal filter runs only inside the while-loop branch, not on this path.
        IReadOnlyList<ExpressionToken> result = this._parser.Parse("");
        result.Should().HaveCount(1);
        result[0].Should().Be(new LiteralToken(""));
    }

    // ── Parse: unterminated string literal ───────────────────────────

    [Fact]
    public void Parse_UnterminatedStringLiteral_Throws()
    {
        // An unterminated string literal inside ${ } means the closing } is never found,
        // so ExtractInterpolationContent raises A2UiExpressionException("Unclosed interpolation").
        // TODO(issue #4): a dedicated "Unterminated string literal" message would give a better
        // diagnostic. For now the test verifies that parsing does not silently succeed.
        Action act = () => this._parser.Parse("${'hello");
        act.Should()
            .Throw<A2UiExpressionException>(
                "an unterminated string literal inside an interpolation block must not silently succeed"
            );
    }

    // ── Parse: malformed number ──────────────────────────────────────

    [Fact]
    public void Parse_MalformedNumber_Throws()
    {
        // TODO(issue #3): ParseNumberLiteral calls double.Parse which throws FormatException,
        // not A2UiExpressionException. Once issue #3 is fixed in production code,
        // change the assertion to .Throw<A2UiExpressionException>().
        Action act = () => this._parser.Parse("${1.2.3}");
        act.Should().Throw<Exception>("issue #3 not yet fixed: malformed number bubbles up a raw FormatException");
    }

    // ── ResolveFormatString: empty template ──────────────────────────

    [Fact]
    public void ResolveFormatString_EmptyTemplate_ReturnsEmpty()
    {
        // The FunctionRegistry formatString function returns "" for a null/empty value arg.
        var registry = FunctionRegistry.CreateDefault();
        string? result = registry.Evaluate("formatString", new Dictionary<string, string?> { ["value"] = "" });
        result.Should().Be("", "formatString with empty value should return an empty string");
    }

    // ── Parse: formatString-style compound expression ────────────────

    [Fact]
    public void Parse_FormatStringTemplate_ParsesNestedFunctionCalls()
    {
        const string Template =
            "${formatDate(value: ${/start}, format: 'E, MMM d')} - ${formatDate(value: ${/end}, format: 'h:mm a')}";
        IReadOnlyList<ExpressionToken> result = this._parser.Parse(Template);

        result.Should().HaveCount(3);

        // First function call: formatDate(value: /start, format: 'E, MMM d')
        var fc1 = result[0].Should().BeOfType<FunctionCallToken>().Subject;
        fc1.Name.Should().Be("formatDate");
        fc1.Args["value"].Should().Be(new PathToken("/start"));
        fc1.Args["format"].Should().Be(new LiteralToken("E, MMM d"));

        // Literal separator
        result[1].Should().Be(new LiteralToken(" - "));

        // Second function call: formatDate(value: /end, format: 'h:mm a')
        var fc2 = result[2].Should().BeOfType<FunctionCallToken>().Subject;
        fc2.Name.Should().Be("formatDate");
        fc2.Args["value"].Should().Be(new PathToken("/end"));
        fc2.Args["format"].Should().Be(new LiteralToken("h:mm a"));
    }
}
