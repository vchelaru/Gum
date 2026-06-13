using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Gum.Analyzers;

/// <summary>
/// Reports a diagnostic when XNA's <c>Microsoft.Xna.Framework.Input.Buttons</c> is passed to a
/// Gum gamepad query method (<c>ButtonDown</c>, <c>ButtonPushed</c>, <c>ButtonReleased</c>,
/// <c>ButtonRepeatRate</c>). The MonoGame runtime now reads the platform-neutral
/// <c>Gum.Input.GamePad</c>, whose query methods take <c>Gum.Input.GamepadButton</c> rather than
/// the XNA <c>Buttons</c> enum (issue #3137). Same-named members line up by value, so the GUM003
/// code fix rewrites <c>Buttons.A</c> to <c>GamepadButton.A</c>.
/// </summary>
/// <remarks>
/// This is a separate diagnostic from <see cref="NamespaceMigrationAnalyzer"/> (GUM001) because it
/// is an argument-type change inside a call, not a namespace migration on a <c>using</c> directive.
/// The migrated call is also a CS1503 compile error after the break, so the analyzer keys on the
/// argument's own type (which still resolves) plus the method name, not the failed invocation symbol.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class GamepadButtonMigrationAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "GUM003";

    internal const string XnaButtonsTypeName = "Buttons";
    internal const string XnaInputNamespace = "Microsoft.Xna.Framework.Input";
    internal const string GamepadButtonTypeName = "GamepadButton";
    internal const string GumInputNamespace = "Gum.Input";

    // The IGamePad / GamePad query methods whose button parameter changed from XNA Buttons to
    // Gum.Input.GamepadButton.
    internal static readonly ImmutableHashSet<string> QueryMethodNames = ImmutableHashSet.Create(
        "ButtonDown", "ButtonPushed", "ButtonReleased", "ButtonRepeatRate");

    public static readonly DiagnosticDescriptor GamepadButtonRule = new DiagnosticDescriptor(
        id: DiagnosticId,
        title: "Gamepad query takes Gum.Input.GamepadButton",
        messageFormat: "'{0}' takes a Gum.Input.GamepadButton; pass GamepadButton.{1} instead of the XNA Buttons value",
        category: "Migration",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The MonoGame runtime now uses the platform-neutral Gum.Input.GamePad, whose button query methods take Gum.Input.GamepadButton instead of Microsoft.Xna.Framework.Input.Buttons. Same-named members share values, so the code fix rewrites Buttons.X to GamepadButton.X.",
        helpLinkUri: "https://docs.flatredball.com/gum/gum-tool/upgrading/migrating-to-2026-may");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(GamepadButtonRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        // The query methods are always called on a receiver (gamepad.ButtonDown(...)).
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return;
        }

        if (!QueryMethodNames.Contains(memberAccess.Name.Identifier.ValueText))
        {
            return;
        }

        // Only flag calls on a Gum gamepad (the concrete GamePad or anything implementing
        // Gum.Input.IGamePad), so an unrelated user method named ButtonDown(Buttons) is left alone.
        var receiverType = context.SemanticModel.GetTypeInfo(memberAccess.Expression, context.CancellationToken).Type;
        if (!IsGumGamePad(receiverType))
        {
            return;
        }

        foreach (var argument in invocation.ArgumentList.Arguments)
        {
            var argType = context.SemanticModel.GetTypeInfo(argument.Expression, context.CancellationToken).Type;
            if (argType is INamedTypeSymbol named &&
                named.Name == XnaButtonsTypeName &&
                named.ContainingNamespace?.ToDisplayString() == XnaInputNamespace)
            {
                var memberName = (argument.Expression as MemberAccessExpressionSyntax)?.Name.Identifier.ValueText
                    ?? string.Empty;

                var diagnostic = Diagnostic.Create(
                    GamepadButtonRule,
                    argument.Expression.GetLocation(),
                    memberAccess.Name.Identifier.ValueText,
                    memberName);

                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    private static bool IsGumGamePad(ITypeSymbol? type)
    {
        if (type == null)
        {
            return false;
        }

        if (IsGumInputType(type))
        {
            return true;
        }

        return type.AllInterfaces.Any(IsGumInputType);
    }

    private static bool IsGumInputType(ITypeSymbol type) =>
        (type.Name == "GamePad" || type.Name == "IGamePad") &&
        type.ContainingNamespace?.ToDisplayString() == GumInputNamespace;
}
