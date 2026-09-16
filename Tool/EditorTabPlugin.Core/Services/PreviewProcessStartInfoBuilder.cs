using System.Diagnostics;

namespace Gum.Plugins.InternalPlugins.EditorTab.Services;

/// <summary>
/// Builds the <see cref="ProcessStartInfo"/> used to launch the GumPreview runtime host. Pure, so
/// it is unit-testable without spawning a process (mirrors <c>FileSystemRevealService</c>).
/// </summary>
public static class PreviewProcessStartInfoBuilder
{
    /// <summary>
    /// Builds the launch command. Arguments go through <see cref="ProcessStartInfo.ArgumentList"/>,
    /// so paths containing spaces need no manual quoting.
    /// </summary>
    /// <param name="contentRootDirectory">
    /// The directory GumPreview resolves relative content (fonts, textures) from (issue #4748) —
    /// the original project's own directory, even when <paramref name="projectPath"/> is a temporary
    /// converted copy elsewhere (a .gumx project served by the Native AOT build).
    /// </param>
    public static ProcessStartInfo Build(string executablePath, string projectPath, string elementName, string selectionFilePath, string contentRootDirectory)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo(executablePath)
        {
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add("--project");
        startInfo.ArgumentList.Add(projectPath);
        startInfo.ArgumentList.Add("--element");
        startInfo.ArgumentList.Add(elementName);
        startInfo.ArgumentList.Add("--selection-file");
        startInfo.ArgumentList.Add(selectionFilePath);
        startInfo.ArgumentList.Add("--content-root");
        startInfo.ArgumentList.Add(contentRootDirectory);
        return startInfo;
    }
}
