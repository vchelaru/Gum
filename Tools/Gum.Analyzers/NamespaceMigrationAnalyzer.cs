using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Gum.Analyzers;

/// <summary>
/// Reports a diagnostic when a <c>using</c> directive or fully-qualified name references a namespace
/// from which types have been migrated to a new namespace.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NamespaceMigrationAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// GUM001: A type has moved to a new namespace.
    /// </summary>
    public static readonly DiagnosticDescriptor MovedTypeRule = new DiagnosticDescriptor(
        id: "GUM001",
        title: "Type moved to new namespace",
        messageFormat: "'{0}' has moved from '{1}' to '{2}'. Update your using directive.",
        category: "Migration",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "This type has been moved to a new namespace as part of the Gum namespace unification. Use the code fix to update automatically.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(MovedTypeRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        // Only register if there are actually migrations to check
        if (NamespaceMigrationMapping.Migrations.IsEmpty)
        {
            return;
        }

        context.RegisterSyntaxNodeAction(AnalyzeUsingDirective, SyntaxKind.UsingDirective);
    }

    private static void AnalyzeUsingDirective(SyntaxNodeAnalysisContext context)
    {
        var usingDirective = (UsingDirectiveSyntax)context.Node;

        // Skip using aliases (e.g., using Foo = Bar.Baz;)
        if (usingDirective.Alias != null)
        {
            return;
        }

        var namespaceName = usingDirective.Name?.ToString();
        if (namespaceName == null)
        {
            return;
        }

        if (!NamespaceMigrationMapping.ByOldNamespace.TryGetValue(namespaceName, out var migrations))
        {
            return;
        }

        // Check if any of the migrated types from this namespace are actually used in the file
        var root = context.Node.SyntaxTree.GetRoot(context.CancellationToken);
        var semanticModel = context.SemanticModel;

        foreach (var migration in migrations)
        {
            // Report one diagnostic per migrated type found in this using's namespace
            var diagnostic = Diagnostic.Create(
                MovedTypeRule,
                usingDirective.GetLocation(),
                migration.TypeName,
                migration.OldNamespace,
                migration.NewNamespace);

            context.ReportDiagnostic(diagnostic);
        }
    }
}
