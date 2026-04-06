// Copyright 2025 Google LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace A2Ui.Avalonia.Functions;

/// <summary>
/// Discriminated union for expression tokens produced by <see cref="ExpressionParser"/>.
/// </summary>
internal abstract record ExpressionToken;

internal sealed record LiteralToken(string Value) : ExpressionToken;
internal sealed record PathToken(string Path) : ExpressionToken;
internal sealed record BoolToken(bool Value) : ExpressionToken;
internal sealed record NumberToken(double Value) : ExpressionToken;
internal sealed record FunctionCallToken(string Name, IReadOnlyDictionary<string, ExpressionToken> Args) : ExpressionToken;

/// <summary>
/// Recursive descent parser for A2UI expressions with <c>${...}</c> interpolation.
/// Ported from the TypeScript <c>ExpressionParser</c> in <c>renderers/web_core</c>.
/// </summary>
internal sealed class ExpressionParser
{
    /// <summary>Maximum allowed recursion depth to prevent stack overflows.</summary>
    private const int MaxDepth = 10;

    /// <summary>
    /// Parse an input string into a list of expression tokens.
    /// Literal text between <c>${...}</c> markers becomes <see cref="LiteralToken"/>;
    /// interpolations are recursively parsed into typed tokens.
    /// </summary>
    public IReadOnlyList<ExpressionToken> Parse(string input, int depth = 0)
    {
        if (depth > MaxDepth)
            throw new A2UiExpressionException("Max recursion depth reached in parse");

        if (string.IsNullOrEmpty(input) || !input.Contains("${", StringComparison.Ordinal))
            return [new LiteralToken(input ?? "")];

        var parts = new List<ExpressionToken>();
        var scanner = new Scanner(input);

        while (!scanner.IsAtEnd)
        {
            if (scanner.Matches("${"))
            {
                scanner.Advance(2);
                string content = ExtractInterpolationContent(scanner);
                ExpressionToken parsed = ParseExpression(content, depth + 1);
                parts.Add(parsed);
            }
            else if (scanner.Peek() == '\\' && scanner.Peek(1) == '$' && scanner.Peek(2) == '{')
            {
                scanner.Advance(); // skip backslash
                parts.Add(new LiteralToken("${"));
                scanner.Advance(2); // skip ${
            }
            else
            {
                int start = scanner.Position;
                while (!scanner.IsAtEnd)
                {
                    if (scanner.Matches("${"))
                        break;
                    if (scanner.Peek() == '\\' && scanner.Peek(1) == '$' && scanner.Peek(2) == '{')
                        break;
                    scanner.Advance();
                }
                string literal = input[start..scanner.Position];
                if (literal.Length > 0)
                    parts.Add(new LiteralToken(literal));
            }
        }

        // Filter empty literals
        return parts.Where(p => p is not LiteralToken { Value.Length: 0 }).ToList();
    }

    /// <summary>
    /// Parse a single expression string (the content inside <c>${...}</c>) into a token.
    /// </summary>
    public ExpressionToken ParseExpression(string expr, int depth = 0)
    {
        expr = expr.Trim();
        if (expr.Length == 0)
            return new LiteralToken("");

        var scanner = new Scanner(expr);
        ExpressionToken result = ParseExpressionInternal(scanner, depth);

        if (!scanner.IsAtEnd)
        {
            throw new A2UiExpressionException(
                $"Unexpected characters at end of expression: '{expr[scanner.Position..]}'");
        }

        return result;
    }

    private ExpressionToken ParseExpressionInternal(Scanner scanner, int depth)
    {
        scanner.SkipWhitespace();
        if (scanner.IsAtEnd)
            return new LiteralToken("");

        // 0. Nested interpolation
        if (scanner.Matches("${"))
        {
            scanner.Advance(2);
            string content = ExtractInterpolationContent(scanner);
            return ParseExpression(content, depth + 1);
        }

        // 1. String literals
        char ch = scanner.Peek();
        if (ch is '\'' or '"')
            return ParseStringLiteral(scanner);

        // 2. Number literals
        if (IsDigit(ch))
            return ParseNumberLiteral(scanner);

        // 3. Keywords
        if (scanner.MatchesKeyword("true"))
            return new BoolToken(true);
        if (scanner.MatchesKeyword("false"))
            return new BoolToken(false);
        if (scanner.MatchesKeyword("null"))
            return new LiteralToken("");

        // 4. Identifier or path → possibly a function call
        string token = ScanPathOrIdentifier(scanner);
        scanner.SkipWhitespace();

        if (!scanner.IsAtEnd && scanner.Peek() == '(')
            return ParseFunctionCall(token, scanner, depth);

        if (token.Length == 0)
            return new LiteralToken("");

        return new PathToken(token);
    }

    private static string ScanPathOrIdentifier(Scanner scanner)
    {
        int start = scanner.Position;
        while (!scanner.IsAtEnd)
        {
            char c = scanner.Peek();
            if (IsAlNum(c) || c is '/' or '.' or '_' or '-')
                scanner.Advance();
            else
                break;
        }
        return scanner.Input[start..scanner.Position];
    }

    private static string ScanIdentifier(Scanner scanner)
    {
        int start = scanner.Position;
        while (!scanner.IsAtEnd && (IsAlNum(scanner.Peek()) || scanner.Peek() == '_'))
            scanner.Advance();
        return scanner.Input[start..scanner.Position];
    }

    private FunctionCallToken ParseFunctionCall(string funcName, Scanner scanner, int depth)
    {
        scanner.Match('(');
        scanner.SkipWhitespace();

        var args = new Dictionary<string, ExpressionToken>();

        while (!scanner.IsAtEnd && scanner.Peek() != ')')
        {
            string argName = ScanIdentifier(scanner);
            scanner.SkipWhitespace();

            if (!scanner.Match(':'))
            {
                throw new A2UiExpressionException(
                    $"Expected ':' after argument name '{argName}' in function '{funcName}'");
            }

            scanner.SkipWhitespace();
            args[argName] = ParseExpressionInternal(scanner, depth);
            scanner.SkipWhitespace();

            if (!scanner.IsAtEnd && scanner.Peek() == ',')
            {
                scanner.Advance();
                scanner.SkipWhitespace();
            }
        }

        if (!scanner.Match(')'))
        {
            throw new A2UiExpressionException(
                $"Expected ')' after function arguments for '{funcName}'");
        }

        return new FunctionCallToken(funcName, args);
    }

    private static LiteralToken ParseStringLiteral(Scanner scanner)
    {
        char quote = scanner.Advance();
        var sb = new System.Text.StringBuilder();
        bool closed = false;

        while (!scanner.IsAtEnd)
        {
            char c = scanner.Advance();
            if (c == '\\')
            {
                if (scanner.IsAtEnd)
                    break; // trailing backslash at end-of-input
                char next = scanner.Advance();
                sb.Append(next switch
                {
                    'n' => '\n',
                    't' => '\t',
                    'r' => '\r',
                    _ => next,
                });
            }
            else if (c == quote)
            {
                closed = true;
                break;
            }
            else
            {
                sb.Append(c);
            }
        }

        if (!closed)
            throw new A2UiExpressionException($"Unterminated string literal: missing closing '{quote}'");

        return new LiteralToken(sb.ToString());
    }

    private static NumberToken ParseNumberLiteral(Scanner scanner)
    {
        int start = scanner.Position;
        while (!scanner.IsAtEnd && (IsDigit(scanner.Peek()) || scanner.Peek() == '.'))
            scanner.Advance();

        string text = scanner.Input[start..scanner.Position];
        if (!double.TryParse(text, System.Globalization.CultureInfo.InvariantCulture, out double value))
            throw new A2UiExpressionException($"Invalid number literal: '{text}'");
        return new NumberToken(value);
    }

    private static string ExtractInterpolationContent(Scanner scanner)
    {
        int start = scanner.Position;
        int braceBalance = 1;

        while (!scanner.IsAtEnd && braceBalance > 0)
        {
            char c = scanner.Advance();
            if (c == '{')
            {
                braceBalance++;
            }
            else if (c == '}')
            {
                braceBalance--;
            }
            else if (c is '\'' or '"')
            {
                // Skip over quoted strings so we don't count braces inside them
                char quoteChar = c;
                while (!scanner.IsAtEnd)
                {
                    char sc = scanner.Advance();
                    if (sc == '\\')
                        scanner.Advance();
                    else if (sc == quoteChar)
                        break;
                }
            }
        }

        if (braceBalance > 0)
            throw new A2UiExpressionException("Unclosed interpolation: missing '}'");

        // Exclude the closing brace from the content
        return scanner.Input[start..(scanner.Position - 1)];
    }

    private static bool IsAlNum(char c) =>
        (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9');

    private static bool IsDigit(char c) =>
        c >= '0' && c <= '9';
}

/// <summary>
/// Position-based string scanner used by <see cref="ExpressionParser"/>.
/// </summary>
internal sealed class Scanner(string input)
{
    public string Input { get; } = input;
    public int Position { get; private set; }

    public bool IsAtEnd => Position >= Input.Length;

    public char Peek(int offset = 0)
    {
        int idx = Position + offset;
        return idx < Input.Length ? Input[idx] : '\0';
    }

    public char Advance(int count = 1)
    {
        char c = Position < Input.Length ? Input[Position] : '\0';
        Position = Math.Min(Position + count, Input.Length);
        return c;
    }

    public bool Match(char expected)
    {
        if (Peek() == expected)
        {
            Advance();
            return true;
        }
        return false;
    }

    public bool Matches(string expected) =>
        Input.AsSpan(Position).StartsWith(expected);

    public bool MatchesKeyword(string keyword)
    {
        if (!Input.AsSpan(Position).StartsWith(keyword))
            return false;

        int afterEnd = Position + keyword.Length;
        if (afterEnd < Input.Length)
        {
            char next = Input[afterEnd];
            if ((next >= 'a' && next <= 'z') || (next >= 'A' && next <= 'Z') ||
                (next >= '0' && next <= '9') || next == '_')
            {
                return false;
            }
        }

        Position += keyword.Length;
        return true;
    }

    public void SkipWhitespace()
    {
        while (!IsAtEnd && char.IsWhiteSpace(Peek()))
            Advance();
    }
}

/// <summary>
/// Exception thrown when an A2UI expression cannot be parsed.
/// </summary>
public sealed class A2UiExpressionException : Exception
{
    public A2UiExpressionException() { }
    public A2UiExpressionException(string message) : base(message) { }
    public A2UiExpressionException(string message, Exception innerException) : base(message, innerException) { }
}
