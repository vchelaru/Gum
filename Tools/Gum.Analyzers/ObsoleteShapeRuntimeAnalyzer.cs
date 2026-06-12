using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Gum.Analyzers;

/// <summary>
/// Reports a diagnostic when source references one of the obsolete shape-runtime types
/// (<c>ColoredCircleRuntime</c>, <c>ColoredRectangleRuntime</c>, <c>RoundedRectangleRuntime</c>,
/// <c>SolidRectangleRuntime</c>) that were collapsed into <c>CircleRuntime</c> / <c>RectangleRuntime</c>
/// by the two-slot fill/stroke unification.
/// </summary>
/// <remarks>
/// This is a separate diagnostic from <see cref="NamespaceMigrationAnalyzer"/> (GUM001) because
/// it covers a type-rename within the same namespace, not a namespace migration. Industry
/// convention is one diagnostic ID per category so suppressions stay targeted.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ObsoleteShapeRuntimeAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// GUM002: An obsolete shape-runtime type should be replaced with the unified two-slot equivalent.
    /// </summary>
    public static readonly DiagnosticDescriptor ObsoleteShapeRuntimeRule = new DiagnosticDescriptor(
        id: ObsoleteShapeRuntimeMapping.DiagnosticId,
        title: "Obsolete shape runtime type",
        messageFormat: "'{0}' has been collapsed into '{1}'. Use the code fix to migrate.",
        category: "Migration",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "ColoredCircleRuntime / ColoredRectangleRuntime / RoundedRectangleRuntime / SolidRectangleRuntime were collapsed into CircleRuntime / RectangleRuntime by the two-slot fill/stroke model. The code fix rewrites the type name and (where mechanically safe) the Color property to FillColor or StrokeColor.",
        helpLinkUri: "https://docs.flatredball.com/gum/gum-tool/upgrading/migrating-to-2026-may");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(ObsoleteShapeRuntimeRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        if (ObsoleteShapeRuntimeMapping.Migrations.IsEmpty)
        {
            return;
        }

        context.RegisterSyntaxNodeAction(AnalyzeIdentifier, SyntaxKind.IdentifierName);
    }

    private static void AnalyzeIdentifier(SyntaxNodeAnalysisContext context)
    {
        var identifier = (IdentifierNameSyntax)context.Node;
        var simpleName = identifier.Identifier.ValueText;

        if (!ObsoleteShapeRuntimeMapping.ByOldTypeName.TryGetValue(simpleName, out var migration))
        {
            return;
        }

        // Skip the identifier-half of a qualified member access like 'x.ColoredCircleRuntime' —
        // those aren't type references and we'd misreport. The type-position identifier in
        // 'Gum.GueDeriving.ColoredCircleRuntime' is the right-hand side of a QualifiedNameSyntax,
        // which we want; but the right-hand side of a MemberAccessExpressionSyntax we don't.
        if (identifier.Parent is MemberAccessExpressionSyntax memberAccess &&
            memberAccess.Name == identifier)
        {
            return;
        }

        var symbolInfo = context.SemanticModel.GetSymbolInfo(identifier, context.CancellationToken);
        var typeSymbol = symbolInfo.Symbol as INamedTypeSymbol;
        if (typeSymbol == null)
        {
            return;
        }

        var ns = typeSymbol.ContainingNamespace?.ToDisplayString();
        if (!ObsoleteShapeRuntimeMapping.IsEligibleNamespace(ns))
        {
            return;
        }

        var diagnostic = Diagnostic.Create(
            ObsoleteShapeRuntimeRule,
            identifier.GetLocation(),
            migration.OldTypeName,
            migration.NewTypeName);

        context.ReportDiagnostic(diagnostic);
    }
}
