using System;
using System.Collections.Generic;
using System.Linq;
using Gum.Bundle;
using Gum.DataTypes;
using Gum.Managers;
using RenderingLibrary.Graphics.Fonts;

namespace Gum.ProjectServices;

/// <summary>
/// Which fonts a project's Texts use, and which per-font range entries
/// (<see cref="GumProjectSave.FontRangeEntries"/>) match none of them. Shared by the Errors checks
/// (GUM0010) and the Fonts tab. Reads <see cref="ObjectFinder.Self"/>, so the project must be set on it.
/// </summary>
public static class FontRangeEntryChecker
{
    /// <summary>
    /// The font each Text in the project uses, as a range entry names it: a font file's project-relative
    /// path, else the family name. Each appears once.
    /// </summary>
    public static IReadOnlyList<string> GetUsedFontNames(GumProjectSave project) =>
        GetUsedFontNames(CollectFonts(project));

    /// <summary>
    /// The font-specific entries that no Text's font reaches, so they never apply. Collects the project's
    /// fonts only when such an entry exists; pass <paramref name="collectedFonts"/> to reuse a collection.
    /// </summary>
    public static IReadOnlyList<FontRangeEntry> FindUnmatchedEntries(GumProjectSave project, IEnumerable<BmfcSave>? collectedFonts = null)
    {
        List<FontRangeEntry> fontEntries = project.FontRangeEntries
            .Where(entry => !string.IsNullOrWhiteSpace(entry.FontFamily))
            .ToList();
        if (fontEntries.Count == 0)
        {
            return fontEntries;
        }

        HashSet<FontRangeEntry> reachedEntries = new HashSet<FontRangeEntry>();
        foreach (BmfcSave font in collectedFonts ?? CollectFonts(project))
        {
            FontRangeEntry? reached = project.FindFontSpecificRangeEntry(GetEntryKey(font));
            if (reached != null)
            {
                reachedEntries.Add(reached);
            }
        }

        return fontEntries.Where(entry => !reachedEntries.Contains(entry)).ToList();
    }

    /// <summary>Every font the project's Texts need, as <see cref="FontReferenceCollector"/> collects them.</summary>
    public static IReadOnlyList<BmfcSave> CollectFonts(GumProjectSave project)
    {
        FontReferenceCollector collector = new FontReferenceCollector(instance => ObjectFinder.Self.GetElementSave(instance));
        return collector.Collect(project, project.AllElements).Values.ToList();
    }

    /// <summary>The names <see cref="GetUsedFontNames(GumProjectSave)"/> returns, from an existing collection.</summary>
    public static IReadOnlyList<string> GetUsedFontNames(IEnumerable<BmfcSave> collectedFonts) =>
        collectedFonts.Select(GetEntryKey).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

    private static string GetEntryKey(BmfcSave font) => font.FontFile ?? font.FontName;
}
