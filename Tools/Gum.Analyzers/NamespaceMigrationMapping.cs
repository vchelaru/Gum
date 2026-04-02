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

    public TypeMigration(string oldNamespace, string newNamespace, string typeName)
    {
        OldNamespace = oldNamespace;
        NewNamespace = newNamespace;
        TypeName = typeName;
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
    /// This table is empty during Phase 1 — entries will be added in Phase 2 when enums are moved.
    /// </summary>
    public static readonly ImmutableArray<TypeMigration> Migrations = ImmutableArray.Create<TypeMigration>(
        // Phase 2 entries will look like:
        // new TypeMigration("Gum.DataTypes", "Gum.Layout", "DimensionUnitType"),
        // new TypeMigration("Gum.Managers", "Gum.Layout", "ChildrenLayout"),
        // new TypeMigration("RenderingLibrary.Graphics", "Gum.Layout", "HorizontalAlignment"),
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
