using System;
using System.Collections.Generic;
using Gum.DataTypes;
using RenderingLibrary.Graphics.Fonts;

namespace Gum.Bundle;

/// <summary>
/// Each element's fonts as <see cref="FontReferenceCollector"/> finds them, collected once and reused by
/// every <see cref="GumProjectDependencyWalker"/> given this cache through
/// <see cref="GumProjectDependencyWalker.FontReferences"/>. A project-wide walk then reuses what earlier
/// single-element walks collected instead of collecting every element again.
/// </summary>
/// <remarks>
/// Holds no file contents and never checks for edits: share one only between walks of one project that
/// nothing changes in between (for example the walks of a single error check), with walkers configured
/// alike (<see cref="GumProjectDependencyWalker.FontTranslationsOf"/>), and drop it afterwards.
/// </remarks>
public sealed class FontReferenceCache
{
    private readonly Dictionary<(ElementSave Element, bool InitializesProject), Dictionary<string, BmfcSave>> _fontsByElement;

    /// <summary>Creates an empty cache.</summary>
    public FontReferenceCache()
    {
        _fontsByElement = new Dictionary<(ElementSave, bool), Dictionary<string, BmfcSave>>();
    }

    /// <summary>How many single-element collections this cache has run; a reused element adds none.</summary>
    public int CollectionCount { get; private set; }

    // A walk that includes the font cache initializes the project before collecting, so its results are
    // kept apart from those of walks that don't.
    internal Dictionary<string, BmfcSave> GetOrCollect(ElementSave element, bool initializesProject,
        Func<ElementSave, Dictionary<string, BmfcSave>> collect)
    {
        if (!_fontsByElement.TryGetValue((element, initializesProject), out Dictionary<string, BmfcSave>? fonts))
        {
            fonts = collect(element);
            CollectionCount++;
            _fontsByElement[(element, initializesProject)] = fonts;
        }
        return fonts;
    }
}
