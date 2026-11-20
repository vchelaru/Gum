using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes;

namespace Gum.Bundle;

/// <summary>
/// Output of <see cref="GumProjectDependencyWalker.Walk(GumProjectSave, string, GumBundleInclusion)"/>: the set of files the project depends on,
/// split by category, plus warnings for any references whose files were not on disk.
/// </summary>
/// <remarks>
/// All paths in the lists are bundle-relative (relative to the project root directory),
/// forward-slash-separated, with no leading slash. Each file appears in at most one of the
/// three category lists; the precedence is Core &gt; FontCache &gt; External.
/// Lists are sorted lexicographically for determinism.
/// </remarks>
public class WalkResult
{
    /// <summary>Project files: `.gumx`, `.gusx`, `.gucx`, `.gutx`, `.behx`.</summary>
    public IReadOnlyList<string> CoreFiles { get; }

    /// <summary>Files under `FontCache/` (typically `.fnt` and their `.png` pages).</summary>
    public IReadOnlyList<string> FontCacheFiles { get; }

    /// <summary>Other referenced files: sprite textures, custom fonts outside `FontCache/`, etc.</summary>
    public IReadOnlyList<string> ExternalFiles { get; }

    /// <summary>References to files that were not present on disk during the walk.</summary>
    public IReadOnlyList<DependencyWarning> MissingFiles { get; }

    /// <summary>
    /// Who referenced each collected path, keyed by the path: <c>Element</c> or <c>Element.Instance</c> for a
    /// variable, <c>(project)</c> for the project's single pixel texture, or the path of the file (a
    /// <c>.fnt</c> or <c>.achx</c>) that loads it. The first reference wins. Empty for a result built by hand.
    /// </summary>
    public IReadOnlyDictionary<string, string> ReferencedFrom { get; }

    /// <summary>Convenience: all included files across all three categories, in the same per-category order.</summary>
    public IEnumerable<string> AllIncludedFiles => CoreFiles.Concat(FontCacheFiles).Concat(ExternalFiles);

    /// <summary>Initializes a new <see cref="WalkResult"/> with the given file lists and warnings.</summary>
    public WalkResult(
        IReadOnlyList<string> coreFiles,
        IReadOnlyList<string> fontCacheFiles,
        IReadOnlyList<string> externalFiles,
        IReadOnlyList<DependencyWarning> missingFiles,
        IReadOnlyDictionary<string, string>? referencedFrom = null)
    {
        ReferencedFrom = referencedFrom ?? new Dictionary<string, string>();
        CoreFiles = coreFiles;
        FontCacheFiles = fontCacheFiles;
        ExternalFiles = externalFiles;
        MissingFiles = missingFiles;
    }
}
