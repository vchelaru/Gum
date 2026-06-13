using System.Collections.Immutable;

namespace Gum.Analyzers;

/// <summary>
/// Defines a single type migration: a type that has moved from one namespace to another.
/// </summary>
internal readonly struct TypeMigration
{
    public string OldNamespace { get; }
    public string NewNamespace { get; }
    public string TypeName { get; }

    /// <summary>
    /// True when the old namespace still contains other (non-migrated) types after this move,
    /// so the code fix must ADD the new <c>using</c> rather than replace the old one (replacing
    /// would break references to the types that stayed behind). False for a full evacuation,
    /// where the old <c>using</c> is replaced or removed.
    /// </summary>
    public bool OldNamespaceRetained { get; }

    public TypeMigration(string oldNamespace, string newNamespace, string typeName, bool oldNamespaceRetained = false)
    {
        OldNamespace = oldNamespace;
        NewNamespace = newNamespace;
        TypeName = typeName;
        OldNamespaceRetained = oldNamespaceRetained;
    }
}

/// <summary>
/// The central mapping table for namespace migrations. Add new entries here when types are moved.
/// The analyzer and code fix provider read from this table.
/// </summary>
internal static class NamespaceMigrationMapping
{
    /// <summary>
    /// All known type migrations. Each entry maps an old (namespace, type) pair to a new namespace.
    /// Entries are added when types move; the analyzer reads from this table to raise <c>GUM001</c>
    /// and the code fix uses it to rewrite <c>using</c> directives.
    /// </summary>
    public static readonly ImmutableArray<TypeMigration> Migrations = ImmutableArray.Create<TypeMigration>(
        // Runtime namespace unification (syntax version 1):
        // MonoGameGum.GueDeriving → Gum.GueDeriving
        new TypeMigration("MonoGameGum.GueDeriving", "Gum.GueDeriving", "TextRuntime"),
        new TypeMigration("MonoGameGum.GueDeriving", "Gum.GueDeriving", "ContainerRuntime"),
        new TypeMigration("MonoGameGum.GueDeriving", "Gum.GueDeriving", "SpriteRuntime"),
        new TypeMigration("MonoGameGum.GueDeriving", "Gum.GueDeriving", "ColoredRectangleRuntime"),
        new TypeMigration("MonoGameGum.GueDeriving", "Gum.GueDeriving", "NineSliceRuntime"),
        new TypeMigration("MonoGameGum.GueDeriving", "Gum.GueDeriving", "CircleRuntime"),
        new TypeMigration("MonoGameGum.GueDeriving", "Gum.GueDeriving", "PolygonRuntime"),
        new TypeMigration("MonoGameGum.GueDeriving", "Gum.GueDeriving", "RectangleRuntime"),
        // Apos shapes (also previously MonoGameGum.GueDeriving)
        new TypeMigration("MonoGameGum.GueDeriving", "Gum.GueDeriving", "AposShapeRuntime"),
        new TypeMigration("MonoGameGum.GueDeriving", "Gum.GueDeriving", "ArcRuntime"),
        new TypeMigration("MonoGameGum.GueDeriving", "Gum.GueDeriving", "ColoredCircleRuntime"),
        new TypeMigration("MonoGameGum.GueDeriving", "Gum.GueDeriving", "LineRuntime"),
        new TypeMigration("MonoGameGum.GueDeriving", "Gum.GueDeriving", "RoundedRectangleRuntime"),
        // SkiaGum.GueDeriving → Gum.GueDeriving (Skia-only types)
        new TypeMigration("SkiaGum.GueDeriving", "Gum.GueDeriving", "ArcRuntime"),
        new TypeMigration("SkiaGum.GueDeriving", "Gum.GueDeriving", "CircleRuntime"),
        new TypeMigration("SkiaGum.GueDeriving", "Gum.GueDeriving", "ColoredCircleRuntime"),
        new TypeMigration("SkiaGum.GueDeriving", "Gum.GueDeriving", "LineGridRuntime"),
        new TypeMigration("SkiaGum.GueDeriving", "Gum.GueDeriving", "LineRuntime"),
        new TypeMigration("SkiaGum.GueDeriving", "Gum.GueDeriving", "LottieAnimationRuntime"),
        new TypeMigration("SkiaGum.GueDeriving", "Gum.GueDeriving", "PolygonRuntime"),
        new TypeMigration("SkiaGum.GueDeriving", "Gum.GueDeriving", "RoundedRectangleRuntime"),
        new TypeMigration("SkiaGum.GueDeriving", "Gum.GueDeriving", "SkiaShapeRuntime"),
        new TypeMigration("SkiaGum.GueDeriving", "Gum.GueDeriving", "SolidRectangleRuntime"),
        new TypeMigration("SkiaGum.GueDeriving", "Gum.GueDeriving", "SvgRuntime"),
        // SkiaGum-side shims for shared types (Text, Container, Sprite, ColoredRectangle)
        new TypeMigration("SkiaGum.GueDeriving", "Gum.GueDeriving", "TextRuntime"),
        new TypeMigration("SkiaGum.GueDeriving", "Gum.GueDeriving", "ContainerRuntime"),
        new TypeMigration("SkiaGum.GueDeriving", "Gum.GueDeriving", "SpriteRuntime"),
        new TypeMigration("SkiaGum.GueDeriving", "Gum.GueDeriving", "ColoredRectangleRuntime"),
        // Input namespace unification (issue #3137): MonoGameGum.Input → Gum.Input.
        // This is a PARTIAL move — Cursor, CursorExtensions and KeyCombo stay in
        // MonoGameGum.Input — so each entry is marked oldNamespaceRetained: true and the
        // code fix ADDS `using Gum.Input;` while keeping `using MonoGameGum.Input;`.
        new TypeMigration("MonoGameGum.Input", "Gum.Input", "GamePad", oldNamespaceRetained: true),
        new TypeMigration("MonoGameGum.Input", "Gum.Input", "AnalogStick", oldNamespaceRetained: true),
        new TypeMigration("MonoGameGum.Input", "Gum.Input", "AnalogButton", oldNamespaceRetained: true),
        new TypeMigration("MonoGameGum.Input", "Gum.Input", "Keyboard", oldNamespaceRetained: true),
        new TypeMigration("MonoGameGum.Input", "Gum.Input", "KeyboardStateProcessor", oldNamespaceRetained: true),
        new TypeMigration("MonoGameGum.Input", "Gum.Input", "IInputReceiverKeyboardMonoGame", oldNamespaceRetained: true),
        new TypeMigration("MonoGameGum.Input", "Gum.Input", "DeadzoneType", oldNamespaceRetained: true),
        new TypeMigration("MonoGameGum.Input", "Gum.Input", "DeadzoneInterpolationType", oldNamespaceRetained: true)
    );

    /// <summary>
    /// Lookup: given a namespace, returns all type names in that namespace that have been migrated.
    /// Built lazily from <see cref="Migrations"/>.
    /// </summary>
    public static readonly ImmutableDictionary<string, ImmutableArray<TypeMigration>> ByOldNamespace =
        BuildByOldNamespace();

    private static ImmutableDictionary<string, ImmutableArray<TypeMigration>> BuildByOldNamespace()
    {
        var builder = ImmutableDictionary.CreateBuilder<string, ImmutableArray<TypeMigration>>();

        foreach (var migration in Migrations)
        {
            if (!builder.TryGetValue(migration.OldNamespace, out var existing))
            {
                existing = ImmutableArray<TypeMigration>.Empty;
            }
            builder[migration.OldNamespace] = existing.Add(migration);
        }

        return builder.ToImmutable();
    }
}
