using System;

namespace Gum.Bundle;

/// <summary>
/// Categories of files that can be included in a `.gumpkg` bundle. Used by
/// <see cref="GumProjectDependencyWalker"/> to determine which dependencies to enumerate
/// and by `gumcli pack` to scope the bundle output.
/// </summary>
[Flags]
public enum GumBundleInclusion
{
    /// <summary>
    /// The Gum project itself: `.gumx` plus all `.gusx`, `.gucx`, `.gutx`, and `.behx` files
    /// referenced by the project.
    /// </summary>
    Core = 1 << 0,

    /// <summary>
    /// Generated font cache files under `FontCache/` (typically `.fnt` plus their `.png` pages).
    /// </summary>
    FontCache = 1 << 1,

    /// <summary>
    /// Files referenced by the project but living outside the Core/FontCache categories:
    /// sprite source `.png` textures, custom font files outside `FontCache/`, and similar.
    /// </summary>
    ExternalFiles = 1 << 2,
}
