using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gum.Analyzers;

/// <summary>
/// Records the Gum elements a game's own code uses as <c>[assembly: GumElementRoot("...")]</c>, so
/// <c>gumcli pack --shake --roots-assembly</c> keeps elements that only code creates. An element is used
/// when hand-written code names a class carrying <c>[GumElement]</c> (creation, typeof, generic
/// argument, cast, is/as, member access, declaration) or passes a string literal to an ObjectFinder
/// element lookup. Gum's generated files (<c>*.Generated.cs</c>) do not count: they reference every
/// element. A non-literal name passed to a lookup cannot be rooted and raises GUM004 in a game that sets
/// <c>&lt;GumShake&gt;true&lt;/GumShake&gt;</c>.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class GumElementRootGenerator : IIncrementalGenerator
{
    public const string DynamicLookupDiagnosticId = "GUM004";

    private const string GumShakePropertyKey = "build_property.GumShake";
    private const string GumElementAttributeName = "Gum.DataTypes.GumElementAttribute";
    private const string GumElementRootAttributeName = "Gum.DataTypes.GumElementRootAttribute";

    private static readonly HashSet<string> LookupMethodNames = new HashSet<string>(StringComparer.Ordinal)
    {
        "GetElementSave", "GetRequiredElementSave", "GetComponent", "GetScreen",
    };

    private static readonly DiagnosticDescriptor DynamicLookupRule = new DiagnosticDescriptor(
        id: DynamicLookupDiagnosticId,
        title: "Element name is not a literal",
        messageFormat: "'{0}' is called with a name that is not a string literal, so gumcli pack --shake cannot keep that element; pass a literal or add the name with --keep",
        category: "Gum",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    // A root found in one syntax node: an element name, or a location for GUM004.
    private readonly struct Use : IEquatable<Use>
    {
        public Use(string? elementName, Location? dynamicLookup, string? methodName)
        {
            ElementName = elementName;
            DynamicLookup = dynamicLookup;
            MethodName = methodName;
        }

        public string? ElementName { get; }
        public Location? DynamicLookup { get; }
        public string? MethodName { get; }

        public bool Equals(Use other) =>
            ElementName == other.ElementName && MethodName == other.MethodName && Equals(DynamicLookup, other.DynamicLookup);
        public override bool Equals(object? obj) => obj is Use other && Equals(other);
        public override int GetHashCode() => (ElementName ?? MethodName ?? string.Empty).GetHashCode();
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<Use> uses = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: static (node, _) => node is SimpleNameSyntax || node is InvocationExpressionSyntax,
                transform: static (syntaxContext, cancellationToken) => GetUse(syntaxContext, cancellationToken))
            .Where(static use => use.ElementName != null || use.DynamicLookup != null);

        IncrementalValueProvider<bool> runtimeSupportsRoots = context.CompilationProvider
            .Select(static (compilation, _) => compilation.GetTypeByMetadataName(GumElementRootAttributeName) != null);

        // GUM004 only matters to a game that packs with --shake, so it is opt-in through <GumShake>true</GumShake>
        // (made visible to analyzers by RuntimePackage.Analyzers.props). A dynamic lookup is fine otherwise.
        IncrementalValueProvider<bool> gameShakes = context.AnalyzerConfigOptionsProvider
            .Select(static (options, _) =>
                options.GlobalOptions.TryGetValue(GumShakePropertyKey, out string? value)
                && string.Equals(value, "true", StringComparison.OrdinalIgnoreCase));

        context.RegisterSourceOutput(uses.Collect().Combine(runtimeSupportsRoots).Combine(gameShakes), static (sourceContext, input) =>
        {
            ((ImmutableArray<Use> allUses, bool supportsRoots), bool shakes) = input;
            if (!supportsRoots)
            {
                return;
            }

            if (shakes)
            {
                foreach (Use use in allUses.Where(use => use.DynamicLookup != null))
                {
                    sourceContext.ReportDiagnostic(Diagnostic.Create(DynamicLookupRule, use.DynamicLookup, use.MethodName));
                }
            }

            List<string> names = allUses
                .Where(use => use.ElementName != null)
                .Select(use => use.ElementName!)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();
            if (names.Count == 0)
            {
                return;
            }

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("// <auto-generated/>");
            foreach (string name in names)
            {
                builder.Append("[assembly: global::Gum.DataTypes.GumElementRoot(")
                    .Append(SymbolDisplay.FormatLiteral(name, quote: true))
                    .AppendLine(")]");
            }
            sourceContext.AddSource("GumElementRoots.g.cs", builder.ToString());
        });
    }

    private static Use GetUse(GeneratorSyntaxContext syntaxContext, CancellationToken cancellationToken)
    {
        if (IsGeneratedFile(syntaxContext.Node.SyntaxTree.FilePath))
        {
            return default;
        }

        if (syntaxContext.Node is InvocationExpressionSyntax invocation)
        {
            return GetLookupUse(invocation, syntaxContext.SemanticModel, cancellationToken);
        }

        ISymbol? symbol = syntaxContext.SemanticModel.GetSymbolInfo(syntaxContext.Node, cancellationToken).Symbol;
        if (symbol is IMethodSymbol { MethodKind: MethodKind.Constructor } constructor)
        {
            symbol = constructor.ContainingType;
        }
        if (symbol is not INamedTypeSymbol type)
        {
            return default;
        }

        foreach (AttributeData attribute in type.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() == GumElementAttributeName
                && attribute.ConstructorArguments.Length == 1
                && attribute.ConstructorArguments[0].Value is string elementName)
            {
                return new Use(elementName, null, null);
            }
        }
        return default;
    }

    private static Use GetLookupUse(InvocationExpressionSyntax invocation, SemanticModel model, CancellationToken cancellationToken)
    {
        string? methodName = invocation.Expression switch
        {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.ValueText,
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
            _ => null,
        };
        if (methodName == null || !LookupMethodNames.Contains(methodName) || invocation.ArgumentList.Arguments.Count != 1)
        {
            return default;
        }

        if (model.GetSymbolInfo(invocation, cancellationToken).Symbol is not IMethodSymbol method
            || method.ContainingType?.Name is not ("ObjectFinder" or "IObjectFinder")
            || method.Parameters.Length != 1
            || method.Parameters[0].Type.SpecialType != SpecialType.System_String)
        {
            return default;
        }

        ExpressionSyntax argument = invocation.ArgumentList.Arguments[0].Expression;
        Optional<object?> constant = model.GetConstantValue(argument, cancellationToken);
        if (constant.HasValue && constant.Value is string literal)
        {
            return new Use(literal, null, null);
        }
        return new Use(null, argument.GetLocation(), methodName);
    }

    private static bool IsGeneratedFile(string? filePath) =>
        filePath != null
        && (filePath.EndsWith(".Generated.cs", StringComparison.OrdinalIgnoreCase)
            || filePath.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase)
            || filePath.EndsWith(".designer.cs", StringComparison.OrdinalIgnoreCase));
}
