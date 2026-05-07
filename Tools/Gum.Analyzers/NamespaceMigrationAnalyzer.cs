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
        title: "Namespace has migrated to a new location",
        messageFormat: "Types from '{0}' have moved to '{1}'. Update your using directive.",
        category: "Migration",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Types from this namespace have been moved to a new namespace as part of the Gum namespace unification. Use the code fix to update automatically.");

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

        // Report a single diagnostic per using directive that points at a migrated namespace.
        // All migrations from the same old namespace currently share one new namespace, so the
        // user-facing message only needs the namespace pair (not every type name).
        var firstMigration = migrations[0];
        var diagnostic = Diagnostic.Create(
            MovedTypeRule,
            usingDirective.GetLocation(),
            firstMigration.OldNamespace,
            firstMigration.NewNamespace);

        context.ReportDiagnostic(diagnostic);
    }
}
