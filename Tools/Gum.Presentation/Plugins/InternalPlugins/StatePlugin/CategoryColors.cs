using System;
using System.Collections.Generic;
using Gum.DataTypes;
using Gum.DataTypes.Variables;

namespace Gum.Plugins.InternalPlugins.StatePlugin;

/// <summary>
/// The fixed palette a state category can be marked with, and the rules that find the color of a
/// category, of a "Category/State" name and of a variable. A category stores only a palette
/// <em>name</em> (<see cref="StateSaveCategory.ColorName"/>); each head maps the name to its own
/// theme's color through the resource <see cref="ResourceKey"/> names. A name this build does not
/// know (written by a newer tool) shows as uncolored but is never rewritten.
/// </summary>
public static class CategoryColors
{
    /// <summary>The palette names, in the order the color menu lists them.</summary>
    public static IReadOnlyList<string> Names { get; } = new[]
    {
        "Indigo", "Violet", "Magenta", "Pink", "Rose", "Orange", "Brown", "Green", "Mint", "Teal", "Sky", "Slate",
    };

    /// <summary>The theme resource prefix; the full key is <see cref="ResourceKey"/>.</summary>
    public const string ResourceKeyPrefix = "Frb.CategoryColor.";

    /// <summary>The theme resource key of the brush for palette color <paramref name="name"/>.</summary>
    public static string ResourceKey(string name) => ResourceKeyPrefix + name;

    /// <summary>True when <paramref name="name"/> is one of <see cref="Names"/>.</summary>
    public static bool IsKnown(string? name)
    {
        if (name == null)
        {
            return false;
        }
        for (int i = 0; i < Names.Count; i++)
        {
            if (string.Equals(Names[i], name, StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>The palette name of <paramref name="category"/>, or null when it has none or an unknown one.</summary>
    public static string? GetName(StateSaveCategory? category) =>
        IsKnown(category?.ColorName) ? category!.ColorName : null;

    /// <summary>
    /// The palette name of the category in <paramref name="fullStateName"/> ("Category/State", the form
    /// animation keyframes use) among <paramref name="container"/>'s categories, or null for an
    /// uncategorized, missing or uncolored one.
    /// </summary>
    public static string? GetNameOfState(IStateContainer? container, string? fullStateName)
    {
        int slash = fullStateName?.IndexOf('/') ?? -1;
        if (container == null || slash <= 0)
        {
            return null;
        }
        string categoryName = fullStateName!.Substring(0, slash);
        foreach (StateSaveCategory category in container.Categories)
        {
            if (category.Name == categoryName)
            {
                return GetName(category);
            }
        }
        return null;
    }

    /// <summary>
    /// The palette name of the first colored category, in category order, in <paramref name="categories"/>.
    /// A row set by several categories takes the first one's color (the "Set by X, Y" text lists them all).
    /// </summary>
    public static string? GetFirstName(IEnumerable<StateSaveCategory> categories)
    {
        foreach (StateSaveCategory category in categories)
        {
            if (GetName(category) is { } name)
            {
                return name;
            }
        }
        return null;
    }
}
