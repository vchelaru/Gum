using Gum.Bundle;
using Gum.DataTypes;

namespace Gum.ProjectServices;

/// <summary>
/// Hands out the <see cref="FontReferenceCache"/> that error checks, file walks and font generation use for
/// a project, so a host that knows when the project changes can let them share one collection.
/// </summary>
public interface IFontReferenceCacheSource
{
    /// <summary>
    /// A cache for <paramref name="project"/> as it is now. A host returns the same cache only while
    /// nothing in the project has changed since it made it.
    /// </summary>
    FontReferenceCache For(GumProjectSave project);
}

/// <summary>A new cache on every call, for hosts (the CLI) that cannot tell when a project changes.</summary>
public sealed class NewFontReferenceCacheEachCall : IFontReferenceCacheSource
{
    /// <inheritdoc/>
    public FontReferenceCache For(GumProjectSave project) => new FontReferenceCache();
}
