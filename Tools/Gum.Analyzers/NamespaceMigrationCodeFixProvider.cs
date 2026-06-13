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
/// Provides a code fix for GUM001 that replaces the old namespace in a <c>using</c> directive
/// with the new namespace.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NamespaceMigrationCodeFixProvider))]
[Shared]
public sealed class NamespaceMigrationCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(NamespaceMigrationAnalyzer.MovedTypeRule.Id);

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
            UsingDirectiveSyntax? usingDirective = node as UsingDirectiveSyntax;
            if (usingDirective == null)
            {
                // Walk up if needed — the diagnostic location might be on the name, not the using
                usingDirective = node.FirstAncestorOrSelf<UsingDirectiveSyntax>();
                if (usingDirective == null)
                {
                    continue;
                }
            }

            var oldNamespace = usingDirective.Name?.ToString();
            if (oldNamespace == null)
            {
                continue;
            }

            // Find the new namespace from the migration table
            if (!NamespaceMigrationMapping.ByOldNamespace.TryGetValue(oldNamespace, out var migrations) ||
                migrations.IsEmpty)
            {
                continue;
            }

            // All migrations from the same old namespace go to the same new namespace
            // (they should — if not, we take the first one)
            var newNamespace = migrations[0].NewNamespace;
            // When the old namespace still holds non-migrated types, keep its using and add
            // the new one; otherwise replace the old using outright.
            var oldNamespaceRetained = migrations[0].OldNamespaceRetained;

            var title = oldNamespaceRetained
                ? $"Add 'using {newNamespace}'"
                : $"Change to 'using {newNamespace}'";

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: title,
                    createChangedDocument: ct => RewriteUsings(context.Document, usingDirective, newNamespace, oldNamespaceRetained, ct),
                    equivalenceKey: $"GUM001_{oldNamespace}_{newNamespace}"),
                diagnostic);
        }
    }

    private static async Task<Document> RewriteUsings(
        Document document,
        UsingDirectiveSyntax usingDirective,
        string newNamespace,
        bool oldNamespaceRetained,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
        {
            return document;
        }

        // Check if the new using already exists in the file (dedupe).
        var existingUsings = root.DescendantNodes()
            .OfType<UsingDirectiveSyntax>()
            .Select(u => u.Name?.ToString())
            .Where(n => n != null)
            .ToImmutableHashSet();

        var newName = SyntaxFactory.ParseName(newNamespace)
            .WithTriviaFrom(usingDirective.Name!);

        SyntaxNode newRoot;
        if (oldNamespaceRetained)
        {
            // Partial move: keep the old using (other types still live there) and add the new
            // one alongside it, unless it is already imported.
            if (existingUsings.Contains(newNamespace))
            {
                newRoot = root;
            }
            else
            {
                // Clone the old using with the new name, but drop the leading trivia so the added
                // line sits flush under the original (the original keeps its own leading newline,
                // and its trailing newline separates the two) instead of leaving a blank line.
                var newUsing = usingDirective.WithName(newName).WithLeadingTrivia();
                newRoot = root.InsertNodesAfter(usingDirective, new[] { newUsing });
            }
        }
        else if (existingUsings.Contains(newNamespace))
        {
            // Full move and new namespace already imported — just remove the old using.
            newRoot = root.RemoveNode(usingDirective, SyntaxRemoveOptions.KeepNoTrivia)!;
        }
        else
        {
            // Full move — replace old namespace with new.
            var newUsing = usingDirective.WithName(newName);
            newRoot = root.ReplaceNode(usingDirective, newUsing);
        }

        return document.WithSyntaxRoot(newRoot);
    }
}
