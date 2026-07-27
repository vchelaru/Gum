using Gum.DataTypes.Variables;
using Gum.Wireframe;
using GumRuntime;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;

namespace Gum.Expressions;

/// <summary>
/// Provides Roslyn-based expression evaluation for Gum variable references.
/// Call Initialize() at startup to enable expression support (e.g., "Width + 10").
/// Without initialization, variable references fall back to simple dot-path lookups.
/// </summary>
public static class GumExpressionService
{
    /// <summary>
    /// Wires up the Roslyn expression evaluator for variable references.
    /// Call this once at startup, after GumService.Initialize().
    /// </summary>
    public static void Initialize()
    {
        ElementSaveExtensions.CustomEvaluateExpression = EvaluateExpression;
        ElementSaveExtensions.CustomEvaluateExpressionAllBranches = EvaluateExpressionAllBranches;
    }

    private static object EvaluateExpression(StateSave stateSave, string expression, string desiredType, GraphicalUiElement? liveRoot)
    {
        expression = EvaluatedSyntax.ConvertToCSharpSyntax(expression);

        // Parse as an expression rather than a compilation unit so top-level constructs
        // like ternaries (`a ? b : c`) are not mis-parsed as nullable variable declarations
        // (Roslyn treats `Foo? bar` at statement scope as a NullableTypeSyntax + declarator).
        var syntax = SyntaxFactory.ParseExpression(expression);

        if (syntax != null)
        {
            var evaluatedSyntax = EvaluatedSyntax.FromSyntaxNode(syntax, stateSave, liveRoot: liveRoot);

            if (evaluatedSyntax?.CastTo(desiredType) == true)
            {
                return evaluatedSyntax?.Value;
            }
        }
        return null;
    }

    /// <summary>
    /// Enumerates every value <paramref name="expression"/> could resolve to (all ternary
    /// branches, not just the one the condition currently selects). See
    /// <see cref="EvaluatedSyntax.EnumerateAllBranches"/>.
    /// </summary>
    private static IEnumerable<object> EvaluateExpressionAllBranches(StateSave stateSave, string expression, string desiredType, GraphicalUiElement? liveRoot)
    {
        expression = EvaluatedSyntax.ConvertToCSharpSyntax(expression);
        var syntax = SyntaxFactory.ParseExpression(expression);

        if (syntax == null)
        {
            yield break;
        }

        foreach (var branch in EvaluatedSyntax.EnumerateAllBranches(syntax, stateSave, liveRoot: liveRoot))
        {
            if (branch.CastTo(desiredType))
            {
                yield return branch.Value;
            }
        }
    }
}
