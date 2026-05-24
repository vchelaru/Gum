using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gum.Analyzers;

/// <summary>
/// Code fix for GUM002: rewrites references to obsolete shape-runtime types
/// (<c>ColoredCircleRuntime</c>, <c>ColoredRectangleRuntime</c>, <c>RoundedRectangleRuntime</c>,
/// <c>SolidRectangleRuntime</c>) to their unified replacement (<c>CircleRuntime</c> /
/// <c>RectangleRuntime</c>) and renames <c>Color</c> accesses on rewritten instances to
/// <c>FillColor</c> or <c>StrokeColor</c> as appropriate.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ObsoleteShapeRuntimeCodeFixProvider))]
[Shared]
public sealed class ObsoleteShapeRuntimeCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(ObsoleteShapeRuntimeMapping.DiagnosticId);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
        {
            return;
        }

        foreach (var diagnostic in context.Diagnostics)
        {
            var node = root.FindNode(diagnostic.Location.SourceSpan);
            var identifier = node as IdentifierNameSyntax ?? node.FirstAncestorOrSelf<IdentifierNameSyntax>();
            if (identifier == null)
            {
                continue;
            }

            var oldTypeName = identifier.Identifier.ValueText;
            if (!ObsoleteShapeRuntimeMapping.ByOldTypeName.TryGetValue(oldTypeName, out var migration))
            {
                continue;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: $"Replace '{migration.OldTypeName}' with '{migration.NewTypeName}'",
                    createChangedDocument: ct => ApplyMigrationAsync(context.Document, migration, ct),
                    equivalenceKey: $"GUM002_{migration.OldTypeName}_{migration.NewTypeName}"),
                diagnostic);
        }
    }

    private static async Task<Document> ApplyMigrationAsync(
        Document document,
        ObsoleteShapeRuntimeMigration migration,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (root == null || semanticModel == null)
        {
            return document;
        }

        // Collect every replacement up front so we can ReplaceNodes in one pass — editing the tree
        // incrementally invalidates the SemanticModel and breaks subsequent symbol lookups.
        var replacements = new Dictionary<SyntaxNode, SyntaxNode>();

        foreach (var identifier in root.DescendantNodes().OfType<IdentifierNameSyntax>())
        {
            if (identifier.Identifier.ValueText != migration.OldTypeName)
            {
                continue;
            }

            // Skip member-access right-hand sides (those aren't type references).
            if (identifier.Parent is MemberAccessExpressionSyntax memberAccess &&
                memberAccess.Name == identifier)
            {
                continue;
            }

            var typeSymbol = semanticModel.GetSymbolInfo(identifier, cancellationToken).Symbol as INamedTypeSymbol;
            if (typeSymbol == null)
            {
                continue;
            }

            var ns = typeSymbol.ContainingNamespace?.ToDisplayString();
            if (!ObsoleteShapeRuntimeMapping.IsEligibleNamespace(ns))
            {
                continue;
            }

            var renamedTypeIdentifier = SyntaxFactory.IdentifierName(migration.NewTypeName)
                .WithTriviaFrom(identifier);
            replacements[identifier] = renamedTypeIdentifier;
        }

        // Rewrite '<receiver>.Color' to '<receiver>.<NewPropertyName>' when the receiver's type is
        // the obsolete type. Limited to the migration's documented property rename — we don't
        // touch other identifiers named 'Color'.
        if (migration.OldPropertyName != null && migration.NewPropertyName != null)
        {
            foreach (var memberAccess in root.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
            {
                if (memberAccess.Name.Identifier.ValueText != migration.OldPropertyName)
                {
                    continue;
                }

                var receiverType = semanticModel.GetTypeInfo(memberAccess.Expression, cancellationToken).Type;
                if (receiverType == null)
                {
                    continue;
                }

                if (receiverType.Name != migration.OldTypeName)
                {
                    continue;
                }

                var ns = receiverType.ContainingNamespace?.ToDisplayString();
                if (!ObsoleteShapeRuntimeMapping.IsEligibleNamespace(ns))
                {
                    continue;
                }

                var renamedName = SyntaxFactory.IdentifierName(migration.NewPropertyName)
                    .WithTriviaFrom(memberAccess.Name);
                replacements[memberAccess.Name] = renamedName;
            }
        }

        if (replacements.Count == 0)
        {
            return document;
        }

        var newRoot = root.ReplaceNodes(replacements.Keys, (original, _) => replacements[original]);
        return document.WithSyntaxRoot(newRoot);
    }
}
