using Gum.DataTypes;

namespace Gum.Plugins.InternalPlugins.EditorTab.Services;

/// <summary>
/// Produces (and refreshes) a throwaway JSON copy of a .gumx project so the Native AOT preview build
/// can serve it (issue #4748) - XmlSerializer, which .gumx loading needs, is not Native-AOT-safe.
/// </summary>
public interface IPreviewGumxProjectionService
{
    /// <summary>
    /// Converts <paramref name="project"/> (a .gumx project) to JSON under a temporary directory
    /// deterministic for that project's own file path - repeated calls for the same project (e.g. a
    /// live preview session refreshing after every save) overwrite the same temp copy, so a preview
    /// process watching that directory for changes picks up the update.
    /// </summary>
    PreviewGumxProjection Project(GumProjectSave project);
}

/// <summary>Result of <see cref="IPreviewGumxProjectionService.Project"/>.</summary>
/// <param name="ProjectFilePath">The temporary .gumj path to launch/reload the preview against.</param>
/// <param name="ContentRootDirectory">
/// The original .gumx project's own directory - relative content (fonts, textures) must still
/// resolve from here, not from the temporary copy's directory.
/// </param>
public readonly record struct PreviewGumxProjection(string ProjectFilePath, string ContentRootDirectory);
