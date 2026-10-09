using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Concurrent;

namespace Gum.Expressions;

/// <summary>
/// Parses variable-reference expressions once and reuses the syntax tree for repeated evaluations
/// of the same text. Syntax trees are immutable, so sharing them is safe. An edited expression is
/// a different string and parses fresh. The cache is cleared when it reaches its size limit so
/// long-running editing sessions do not accumulate stale text.
/// </summary>
internal sealed class ParsedExpressionCache
{
    private readonly ConcurrentDictionary<string, ExpressionSyntax> _parsedByText =
        new ConcurrentDictionary<string, ExpressionSyntax>();

    private readonly int _maxEntries;

    public ParsedExpressionCache(int maxEntries = 4096)
    {
        _maxEntries = maxEntries;
    }

    public int Count => _parsedByText.Count;

    /// <summary>
    /// Returns the parsed form of <paramref name="expression"/> (slash syntax is converted to C# first).
    /// </summary>
    public ExpressionSyntax GetOrParse(string expression)
    {
        if (_parsedByText.TryGetValue(expression, out ExpressionSyntax? cached))
        {
            return cached;
        }

        // Parse as an expression rather than a compilation unit so top-level constructs
        // like ternaries (`a ? b : c`) are not mis-parsed as nullable variable declarations
        // (Roslyn treats `Foo? bar` at statement scope as a NullableTypeSyntax + declarator).
        ExpressionSyntax parsed = SyntaxFactory.ParseExpression(EvaluatedSyntax.ConvertToCSharpSyntax(expression));

        if (_parsedByText.Count >= _maxEntries)
        {
            _parsedByText.Clear();
        }

        _parsedByText[expression] = parsed;
        return parsed;
    }
}
