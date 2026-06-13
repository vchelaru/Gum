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
using Microsoft.CodeAnalysis.Simplification;

namespace Gum.Analyzers;

/// <summary>
/// Code fix for GUM003: rewrites an XNA <c>Buttons.X</c> argument passed to a Gum gamepad query
/// method into <c>Gum.Input.GamepadButton.X</c>. The replacement is emitted fully qualified with a
/// <see cref="Simplifier.Annotation"/> so it is shortened to <c>GamepadButton.X</c> when
/// <c>using Gum.Input;</c> is in scope, and stays qualified (still compiling) when it is not.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(GamepadButtonMigrationCodeFixProvider))]
[Shared]
public sealed class GamepadButtonMigrationCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(GamepadButtonMigrationAnalyzer.DiagnosticId);

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
            // getInnermostNodeForTie: the argument and its expression share a span; we want the
            // inner Buttons.X member access, not the enclosing ArgumentSyntax.
            var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
            var memberAccess = node as MemberAccessExpressionSyntax
                ?? node.FirstAncestorOrSelf<MemberAccessExpressionSyntax>()
                ?? node.DescendantNodes().OfType<MemberAccessExpressionSyntax>().FirstOrDefault();
            if (memberAccess == null)
            {
                continue;
            }

            var memberName = memberAccess.Name.Identifier.ValueText;

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: $"Use GamepadButton.{memberName}",
                    createChangedDocument: ct => ReplaceWithGamepadButton(context.Document, memberAccess, memberName, ct),
                    equivalenceKey: $"GUM003_{memberName}"),
                diagnostic);
        }
    }

    private static async Task<Document> ReplaceWithGamepadButton(
        Document document,
        MemberAccessExpressionSyntax memberAccess,
        string memberName,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
        {
            return document;
        }

        var replacement = SyntaxFactory
            .ParseExpression($"{GamepadButtonMigrationAnalyzer.GumInputNamespace}.{GamepadButtonMigrationAnalyzer.GamepadButtonTypeName}.{memberName}")
            .WithTriviaFrom(memberAccess)
            .WithAdditionalAnnotations(Simplifier.Annotation);

        var newRoot = root.ReplaceNode(memberAccess, replacement);
        return document.WithSyntaxRoot(newRoot);
    }
}
