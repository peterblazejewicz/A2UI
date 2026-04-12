using A2Ui.Avalonia.Functions;
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
        Assert.Single(result);
        Assert.Equal(new LiteralToken("hello world"), result[0]);
    }

    // ── Parse: simple interpolation ──────────────────────────────────

    [Fact]
    public void Parse_SimpleInterpolation_ReturnsLiteralAndPath()
    {
        IReadOnlyList<ExpressionToken> result = this._parser.Parse("hello ${foo}");
        Assert.Equal(2, result.Count);
        Assert.Equal(new LiteralToken("hello "), result[0]);
        Assert.Equal(new PathToken("foo"), result[1]);
    }

    [Fact]
    public void Parse_NumberInterpolation_ReturnsLiteralAndPath()
    {
        IReadOnlyList<ExpressionToken> result = this._parser.Parse("number is ${num}");
        Assert.Equal(2, result.Count);
        Assert.Equal(new LiteralToken("number is "), result[0]);
        Assert.Equal(new PathToken("num"), result[1]);
    }

    // ── Parse: nested interpolation ──────────────────────────────────

    [Fact]
    public void Parse_NestedInterpolation_ResolvesInnerPath()
    {
        IReadOnlyList<ExpressionToken> result = this._parser.Parse("val is ${${nested}}");
        Assert.Equal(2, result.Count);
        Assert.Equal(new LiteralToken("val is "), result[0]);
        Assert.Equal(new PathToken("nested"), result[1]);
    }

    [Fact]
    public void Parse_DeepNestedStringLiteral_Resolves()
    {
        IReadOnlyList<ExpressionToken> result = this._parser.Parse("${${\"hello\"}}");
        Assert.Single(result);
        Assert.Equal(new LiteralToken("hello"), result[0]);
    }

    // ── Parse: escaped interpolation ─────────────────────────────────

    [Fact]
    public void Parse_EscapedInterpolation_EmitsLiteralDollarBrace()
    {
        IReadOnlyList<ExpressionToken> result = this._parser.Parse("escaped \\${foo}");
        Assert.Equal(3, result.Count);
        Assert.Equal(new LiteralToken("escaped "), result[0]);
        Assert.Equal(new LiteralToken("${"), result[1]);
        Assert.Equal(new LiteralToken("foo}"), result[2]);
    }

    // ── Parse: function calls ────────────────────────────────────────

    [Fact]
    public void Parse_FunctionCall_ReturnsFunctionCallToken()
    {
        IReadOnlyList<ExpressionToken> result = this._parser.Parse("sum is ${add(a: 10, b: 20)}");
        Assert.Equal(2, result.Count);
        Assert.Equal(new LiteralToken("sum is "), result[0]);

        var fc = Assert.IsType<FunctionCallToken>(result[1]);
        Assert.Equal("add", fc.Name);
        Assert.Equal(2, fc.Args.Count);
        Assert.Equal(new NumberToken(10), fc.Args["a"]);
        Assert.Equal(new NumberToken(20), fc.Args["b"]);
    }

    [Fact]
    public void Parse_FunctionCallWithStringLiterals_ParsesCorrectly()
    {
        IReadOnlyList<ExpressionToken> result = this._parser.Parse("case is ${upper(text: \"hello\")}");
        Assert.Equal(2, result.Count);
        Assert.Equal(new LiteralToken("case is "), result[0]);

        var fc = Assert.IsType<FunctionCallToken>(result[1]);
        Assert.Equal("upper", fc.Name);
        Assert.Equal(new LiteralToken("hello"), fc.Args["text"]);
    }

    // ── Parse: keywords ──────────────────────────────────────────────

    [Fact]
    public void Parse_Keywords_ReturnsBoolAndEmptyForNull()
    {
        // "${true} ${false} ${null}" parses to:
        //   BoolToken(true), LiteralToken(" "), BoolToken(false), LiteralToken(" ")
        // null → empty LiteralToken which is filtered out by the empty-filter step
        IReadOnlyList<ExpressionToken> result = this._parser.Parse("${true} ${false} ${null}");
        Assert.Equal(4, result.Count);
        Assert.Equal(new BoolToken(true), result[0]);
        Assert.Equal(new LiteralToken(" "), result[1]);
        Assert.Equal(new BoolToken(false), result[2]);
        Assert.Equal(new LiteralToken(" "), result[3]);
    }

    // ── Parse: max depth ─────────────────────────────────────────────

    [Fact]
    public void Parse_MaxDepthExceeded_ThrowsA2UiExpressionException()
    {
        Action act = () => this._parser.Parse("depth", 11);
        var ex = Assert.Throws<A2UiExpressionException>(act);
        Assert.Contains("Max recursion depth", ex.Message);
    }

    // ── Parse: unclosed interpolation ────────────────────────────────

    [Fact]
    public void Parse_UnclosedInterpolation_ThrowsA2UiExpressionException()
    {
        Action act = () => this._parser.Parse("hello ${world");
        var ex = Assert.Throws<A2UiExpressionException>(act);
        Assert.Contains("Unclosed interpolation", ex.Message);
    }

    // ── Parse: invalid function syntax ───────────────────────────────

    [Fact]
    public void Parse_MissingClosingParenthesis_Throws()
    {
        Action act = () => this._parser.Parse("${add(a: 1, b: 2}");
        var ex = Assert.Throws<A2UiExpressionException>(act);
        Assert.Contains("Expected ')'", ex.Message);
    }

    // ── Parse: unexpected characters at end ──────────────────────────

    [Fact]
    public void Parse_UnexpectedCharactersAtEnd_Throws()
    {
        Action act = () => this._parser.Parse("${true false}");
        var ex = Assert.Throws<A2UiExpressionException>(act);
        Assert.Contains("Unexpected characters", ex.Message);
    }

    // ── ParseExpression: empty identifiers ───────────────────────────

    [Fact]
    public void Parse_EmptyParentheses_ReturnsFunctionCallWithNoArgs()
    {
        IReadOnlyList<ExpressionToken> result = this._parser.Parse("${()}");
        Assert.Single(result);
        var fc = Assert.IsType<FunctionCallToken>(result[0]);
        Assert.Equal("", fc.Name);
        Assert.Empty(fc.Args);
    }

    [Fact]
    public void ParseExpression_EmptyString_ReturnsEmptyLiteral()
    {
        ExpressionToken result = this._parser.ParseExpression("");
        Assert.Equal(new LiteralToken(""), result);
    }

    [Fact]
    public void ParseExpression_EmptyParentheses_ReturnsFunctionCall()
    {
        ExpressionToken result = this._parser.ParseExpression("()");
        var fc = Assert.IsType<FunctionCallToken>(result);
        Assert.Equal("", fc.Name);
        Assert.Empty(fc.Args);
    }

    // ── ParseExpression: string literal escape sequences ─────────────

    [Fact]
    public void ParseExpression_StringLiteralEscapeSequences_ParsesCorrectly()
    {
        ExpressionToken result = this._parser.ParseExpression("'line1\\nline2\\t\\r\\'\\\\x'");
        Assert.Equal(new LiteralToken("line1\nline2\t\r'\\x"), result);
    }

    // ── ParseExpression: paths with special characters ───────────────

    [Fact]
    public void ParseExpression_PathWithSpecialChars_ReturnsPathToken()
    {
        ExpressionToken result = this._parser.ParseExpression("my-path.with_underscores");
        Assert.Equal(new PathToken("my-path.with_underscores"), result);
    }

    // ── ParseExpression: missing colon in function args ──────────────

    [Fact]
    public void ParseExpression_MissingColonInFunctionArgs_Throws()
    {
        Action act = () => this._parser.ParseExpression("add(a 10, b: 20)");
        var ex = Assert.Throws<A2UiExpressionException>(act);
        Assert.Contains("Expected ':'", ex.Message);
    }

    // ── Parse: empty string ──────────────────────────────────────────

    [Fact]
    public void Parse_EmptyString_ReturnsLiteralOrEmpty()
    {
        // Parse("") has no "${" so it returns [LiteralToken("")] via the early-return path.
        // The empty-literal filter runs only inside the while-loop branch, not on this path.
        IReadOnlyList<ExpressionToken> result = this._parser.Parse("");
        Assert.Single(result);
        Assert.Equal(new LiteralToken(""), result[0]);
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
        // an unterminated string literal inside an interpolation block must not silently succeed
        Assert.Throws<A2UiExpressionException>(act);
    }

    // ── Parse: malformed number ──────────────────────────────────────

    [Fact]
    public void Parse_MalformedNumber_Throws()
    {
        // TODO(issue #3): ParseNumberLiteral calls double.Parse which throws FormatException,
        // not A2UiExpressionException. Once issue #3 is fixed in production code,
        // change the assertion to Assert.Throws<A2UiExpressionException>().
        Action act = () => this._parser.Parse("${1.2.3}");
        // issue #3 not yet fixed: malformed number bubbles up a raw FormatException
        Assert.ThrowsAny<Exception>(act);
    }

    // ── ResolveFormatString: empty template ──────────────────────────

    [Fact]
    public void ResolveFormatString_EmptyTemplate_ReturnsEmpty()
    {
        // The FunctionRegistry formatString function returns "" for a null/empty value arg.
        var registry = FunctionRegistry.CreateDefault();
        string? result = registry.Evaluate("formatString", new Dictionary<string, string?> { ["value"] = "" });
        // formatString with empty value should return an empty string
        Assert.Equal("", result);
    }

    // ── Parse: formatString-style compound expression ────────────────

    [Fact]
    public void Parse_FormatStringTemplate_ParsesNestedFunctionCalls()
    {
        const string Template =
            "${formatDate(value: ${/start}, format: 'E, MMM d')} - ${formatDate(value: ${/end}, format: 'h:mm a')}";
        IReadOnlyList<ExpressionToken> result = this._parser.Parse(Template);

        Assert.Equal(3, result.Count);

        // First function call: formatDate(value: /start, format: 'E, MMM d')
        var fc1 = Assert.IsType<FunctionCallToken>(result[0]);
        Assert.Equal("formatDate", fc1.Name);
        Assert.Equal(new PathToken("/start"), fc1.Args["value"]);
        Assert.Equal(new LiteralToken("E, MMM d"), fc1.Args["format"]);

        // Literal separator
        Assert.Equal(new LiteralToken(" - "), result[1]);

        // Second function call: formatDate(value: /end, format: 'h:mm a')
        var fc2 = Assert.IsType<FunctionCallToken>(result[2]);
        Assert.Equal("formatDate", fc2.Name);
        Assert.Equal(new PathToken("/end"), fc2.Args["value"]);
        Assert.Equal(new LiteralToken("h:mm a"), fc2.Args["format"]);
    }
}
