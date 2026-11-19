using System;
using System.Collections.Generic;
using Gum.DataTypes;
using RenderingLibrary.Graphics.Fonts;

namespace Gum.Bundle;

/// <summary>
/// Each element's fonts as <see cref="FontReferenceCollector"/> finds them, collected once and reused by
/// every walk or font scan given this cache (<see cref="GumProjectDependencyWalker.FontReferences"/>).
/// A project-wide walk then reuses what earlier single-element walks collected instead of collecting
/// every element again.
/// </summary>
/// <remarks>
/// Holds no file contents and never checks for edits: an element's fonts also depend on the components
/// it holds, its base types and the project's font settings, so share one only while nothing in the
/// project changes, and only between collections made the same way (no localization lookup; see
/// <see cref="GumProjectDependencyWalker.FontTranslationsOf"/>). Every read returns copies, so a caller
/// that edits a <see cref="BmfcSave"/> (font generation resolves its file path) changes nothing the
/// next caller reads.
/// </remarks>
public sealed class FontReferenceCache
{
    private readonly Dictionary<(ElementSave Element, bool InitializesProject), Dictionary<string, BmfcSave>> _fontsByElement;
    private readonly object _lock = new object();

    /// <summary>Creates an empty cache.</summary>
    public FontReferenceCache()
    {
        _fontsByElement = new Dictionary<(ElementSave, bool), Dictionary<string, BmfcSave>>();
    }

    /// <summary>How many single-element collections this cache has run; a reused element adds none.</summary>
    public int CollectionCount { get; private set; }

    /// <summary>
    /// Every font <paramref name="elements"/> need, merged in order with the last element winning a
    /// shared key, as <see cref="FontReferenceCollector.Collect"/> merges them.
    /// </summary>
    /// <param name="elements">The elements whose fonts to return, in merge order.</param>
    /// <param name="initializesProject">Whether the caller initialized the project before collecting (a
    /// walk that includes the font cache does), so its results are kept apart from those that didn't.</param>
    /// <param name="collectOne">Collects one element this cache has not seen.</param>
    public Dictionary<string, BmfcSave> Collect(IEnumerable<ElementSave> elements, bool initializesProject,
        Func<ElementSave, Dictionary<string, BmfcSave>> collectOne)
    {
        Dictionary<string, BmfcSave> fonts = new Dictionary<string, BmfcSave>();
        lock (_lock)
        {
            foreach (ElementSave element in elements)
            {
                if (!_fontsByElement.TryGetValue((element, initializesProject), out Dictionary<string, BmfcSave>? elementFonts))
                {
                    elementFonts = collectOne(element);
                    CollectionCount++;
                    _fontsByElement[(element, initializesProject)] = elementFonts;
                }

                foreach (KeyValuePair<string, BmfcSave> pair in elementFonts)
                {
                    fonts[pair.Key] = pair.Value.Clone();
                }
            }
        }
        return fonts;
    }
}
