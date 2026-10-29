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
/// argument, cast, is/as, member access, declaration), names a Forms control such as <c>Button</c> that a
/// generated class is registered for (<c>GumElement.FormsType</c>), or passes a string literal to an ObjectFinder
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
    private const string FrameworkElementTypeName = "Gum.Forms.Controls.FrameworkElement";
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

    // What one syntax node uses: an element by name, a Forms control type (which reaches the element
    // registered for it), or a lookup whose name is not a literal (a location for GUM004).
    private readonly struct Use : IEquatable<Use>
    {
        public Use(string? elementName, Location? dynamicLookup, string? methodName, string? formsType = null)
        {
            ElementName = elementName;
            DynamicLookup = dynamicLookup;
            MethodName = methodName;
            FormsType = formsType;
        }

        public string? ElementName { get; }
        public Location? DynamicLookup { get; }
        public string? MethodName { get; }
        public string? FormsType { get; }

        public bool IsEmpty => ElementName == null && DynamicLookup == null && FormsType == null;

        public bool Equals(Use other) =>
            ElementName == other.ElementName && MethodName == other.MethodName && FormsType == other.FormsType
            && Equals(DynamicLookup, other.DynamicLookup);
        public override bool Equals(object? obj) => obj is Use other && Equals(other);
        public override int GetHashCode() => (ElementName ?? FormsType ?? MethodName ?? string.Empty).GetHashCode();
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<Use> uses = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: static (node, _) => node is SimpleNameSyntax || node is InvocationExpressionSyntax,
                transform: static (syntaxContext, cancellationToken) => GetUse(syntaxContext, cancellationToken))
            .Where(static use => !use.IsEmpty);

        // Each generated class registered as the default visual for a Forms type: (Forms type, element name).
        IncrementalValuesProvider<(string FormsType, string ElementName)> registrations = context.SyntaxProvider
            .ForAttributeWithMetadataName(GumElementAttributeName,
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (attributeContext, _) => GetRegistration(attributeContext))
            .Where(static registration => registration.FormsType.Length > 0);

        IncrementalValueProvider<bool> runtimeSupportsRoots = context.CompilationProvider
            .Select(static (compilation, _) => compilation.GetTypeByMetadataName(GumElementRootAttributeName) != null);

        // GUM004 only matters to a game that packs with --shake, so it is opt-in through <GumShake>true</GumShake>
        // (made visible to analyzers by RuntimePackage.Analyzers.props). A dynamic lookup is fine otherwise.
        IncrementalValueProvider<bool> gameShakes = context.AnalyzerConfigOptionsProvider
            .Select(static (options, _) =>
                options.GlobalOptions.TryGetValue(GumShakePropertyKey, out string? value)
                && string.Equals(value, "true", StringComparison.OrdinalIgnoreCase));

        context.RegisterSourceOutput(uses.Collect().Combine(registrations.Collect()).Combine(runtimeSupportsRoots).Combine(gameShakes), static (sourceContext, input) =>
        {
            (((ImmutableArray<Use> allUses, ImmutableArray<(string FormsType, string ElementName)> allRegistrations), bool supportsRoots), bool shakes) = input;
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

            HashSet<string> usedFormsTypes = new HashSet<string>(
                allUses.Where(use => use.FormsType != null).Select(use => use.FormsType!), StringComparer.Ordinal);
            List<string> names = allUses
                .Where(use => use.ElementName != null)
                .Select(use => use.ElementName!)
                .Concat(allRegistrations
                    .Where(registration => usedFormsTypes.Contains(registration.FormsType))
                    .Select(registration => registration.ElementName))
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

        // A Forms control such as Button builds its visual from the element generated code registered for it.
        for (INamedTypeSymbol? baseType = type; baseType != null; baseType = baseType.BaseType)
        {
            if (baseType.ToDisplayString() == FrameworkElementTypeName)
            {
                return new Use(null, null, null, formsType: type.ToDisplayString());
            }
        }
        return default;
    }

    private static (string FormsType, string ElementName) GetRegistration(GeneratorAttributeSyntaxContext attributeContext)
    {
        foreach (AttributeData attribute in attributeContext.Attributes)
        {
            if (attribute.ConstructorArguments.Length == 1
                && attribute.ConstructorArguments[0].Value is string elementName)
            {
                foreach (KeyValuePair<string, TypedConstant> namedArgument in attribute.NamedArguments)
                {
                    if (namedArgument.Key == "FormsType" && namedArgument.Value.Value is INamedTypeSymbol formsType)
                    {
                        return (formsType.ToDisplayString(), elementName);
                    }
                }
            }
        }
        return (string.Empty, string.Empty);
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
