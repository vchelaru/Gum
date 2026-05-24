using System.Collections.Generic;
using System.Collections.Immutable;

namespace Gum.Analyzers;

/// <summary>
/// Describes how a single obsolete shape-runtime type rewrites to its replacement: which new type
/// to use, and which property (if any) should be renamed on rewritten instances.
/// </summary>
internal readonly struct ObsoleteShapeRuntimeMigration
{
    public string OldTypeName { get; }
    public string NewTypeName { get; }
    public string? OldPropertyName { get; }
    public string? NewPropertyName { get; }

    public ObsoleteShapeRuntimeMigration(
        string oldTypeName,
        string newTypeName,
        string? oldPropertyName = null,
        string? newPropertyName = null)
    {
        OldTypeName = oldTypeName;
        NewTypeName = newTypeName;
        OldPropertyName = oldPropertyName;
        NewPropertyName = newPropertyName;
    }
}

/// <summary>
/// Mapping table for the GUM002 obsolete-shape-runtime rewrite. Each entry covers a legacy type
/// (<c>ColoredCircleRuntime</c>, <c>ColoredRectangleRuntime</c>, <c>RoundedRectangleRuntime</c>,
/// <c>SolidRectangleRuntime</c>) that was collapsed into <c>CircleRuntime</c> or <c>RectangleRuntime</c>
/// via the two-slot fill/stroke model.
/// </summary>
/// <remarks>
/// The analyzer matches by simple type name plus containing namespace. The legacy types live in
/// either <c>Gum.GueDeriving</c> (the real obsolete sources) or in <c>MonoGameGum.GueDeriving</c> /
/// <c>SkiaGum.GueDeriving</c> (compatibility shims that derive from the real obsolete types — those
/// shims also carry the <c>[Obsolete]</c> attribute via GUM001).
///
/// <para>The <c>Color</c> property renames are split: <c>ColoredCircleRuntime.Color</c> historically
/// painted the *outline*, so it maps to <c>StrokeColor</c>. <c>ColoredRectangleRuntime.Color</c>
/// and <c>SolidRectangleRuntime.Color</c> painted the fill, so they map to <c>FillColor</c>.
/// <c>RoundedRectangleRuntime</c> has no property rename — <c>CornerRadius</c> carries over verbatim.</para>
/// </remarks>
internal static class ObsoleteShapeRuntimeMapping
{
    public const string DiagnosticId = "GUM002";

    public static readonly ImmutableArray<string> EligibleNamespaces = ImmutableArray.Create(
        "Gum.GueDeriving",
        "MonoGameGum.GueDeriving",
        "SkiaGum.GueDeriving");

    public static readonly ImmutableArray<ObsoleteShapeRuntimeMigration> Migrations = ImmutableArray.Create(
        new ObsoleteShapeRuntimeMigration(
            oldTypeName: "ColoredCircleRuntime",
            newTypeName: "CircleRuntime",
            oldPropertyName: "Color",
            newPropertyName: "StrokeColor"),
        new ObsoleteShapeRuntimeMigration(
            oldTypeName: "ColoredRectangleRuntime",
            newTypeName: "RectangleRuntime",
            oldPropertyName: "Color",
            newPropertyName: "FillColor"),
        new ObsoleteShapeRuntimeMigration(
            oldTypeName: "RoundedRectangleRuntime",
            newTypeName: "RectangleRuntime"),
        new ObsoleteShapeRuntimeMigration(
            oldTypeName: "SolidRectangleRuntime",
            newTypeName: "RectangleRuntime",
            oldPropertyName: "Color",
            newPropertyName: "FillColor"));

    public static readonly ImmutableDictionary<string, ObsoleteShapeRuntimeMigration> ByOldTypeName =
        BuildByOldTypeName();

    private static ImmutableDictionary<string, ObsoleteShapeRuntimeMigration> BuildByOldTypeName()
    {
        var builder = ImmutableDictionary.CreateBuilder<string, ObsoleteShapeRuntimeMigration>();
        foreach (var migration in Migrations)
        {
            builder[migration.OldTypeName] = migration;
        }
        return builder.ToImmutable();
    }

    public static bool IsEligibleNamespace(string? fullyQualifiedNamespace)
    {
        if (fullyQualifiedNamespace == null)
        {
            return false;
        }
        foreach (var candidate in EligibleNamespaces)
        {
            if (fullyQualifiedNamespace == candidate)
            {
                return true;
            }
        }
        return false;
    }
}
