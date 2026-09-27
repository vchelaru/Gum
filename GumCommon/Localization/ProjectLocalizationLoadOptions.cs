using System;
using Gum.Bundle;

namespace Gum.Localization;

/// <summary>
/// How a host runs <see cref="ProjectLocalizationLoader"/>: where files come from and where problems
/// are reported. The load policy itself is the same for every host.
/// </summary>
public class ProjectLocalizationLoadOptions
{
    /// <summary>
    /// The bundle's provider when the project came from a <c>.gumpkg</c>, else <c>null</c>. A bundle
    /// has no directory to enumerate, so RESX satellites are found through the provider instead.
    /// </summary>
    public IGumFileProvider? BundleFileProvider { get; set; }

    /// <summary>
    /// Receives a configured file that was skipped: missing on disk, or a mix of file types that
    /// can't load together.
    /// </summary>
    public Action<string>? OnSkipped { get; set; }

    /// <summary>
    /// Receives a string ID defined in more than one RESX file. The files still load.
    /// </summary>
    public Action<string>? OnWarning { get; set; }
}
