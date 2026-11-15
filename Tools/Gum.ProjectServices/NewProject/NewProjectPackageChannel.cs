namespace Gum.ProjectServices;

/// <summary>
/// Which Gum packages a new code project uses: the one choice that replaces separate source-linked and preview switches.
/// </summary>
public enum NewProjectPackageChannel
{
    /// <summary>The published (stable) NuGet packages. The default.</summary>
    Stable,

    /// <summary>The newest preview (prerelease) NuGet packages, which may be unstable.</summary>
    Preview,

    /// <summary>The Gum source code on this computer, referenced as projects instead of NuGet packages.</summary>
    LocalSource
}
