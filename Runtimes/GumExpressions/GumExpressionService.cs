using Gum.DataTypes.Variables;
using GumRuntime;
using Microsoft.CodeAnalysis.CSharp;

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
    }

    private static object EvaluateExpression(StateSave stateSave, string expression, string desiredType)
    {
        expression = EvaluatedSyntax.ConvertToCSharpSyntax(expression);

        var syntax = CSharpSyntaxTree.ParseText(expression).GetCompilationUnitRoot();

        if (syntax != null)
        {
            var evaluatedSyntax = EvaluatedSyntax.FromSyntaxNode(syntax, stateSave);

            if (evaluatedSyntax?.CastTo(desiredType) == true)
            {
                return evaluatedSyntax?.Value;
            }
        }
        return null;
    }
}
