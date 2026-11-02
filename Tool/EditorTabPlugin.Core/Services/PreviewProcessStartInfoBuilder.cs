using System.Diagnostics;
using System.Globalization;

namespace Gum.Plugins.InternalPlugins.EditorTab.Services;

/// <summary>
/// Builds the <see cref="ProcessStartInfo"/> used to launch the GumPreview runtime host. Pure, so
/// it is unit-testable without spawning a process (mirrors <c>FileSystemRevealService</c>).
/// </summary>
public static class PreviewProcessStartInfoBuilder
{
    /// <summary>When set to a file path in the tool's environment, Preview is launched with <c>--log</c> pointing at it.</summary>
    public const string LogPathEnvironmentVariable = "GUM_PREVIEW_LOG";

    /// <summary>
    /// Builds the launch command. Arguments go through <see cref="ProcessStartInfo.ArgumentList"/>,
    /// so paths containing spaces need no manual quoting.
    /// </summary>
    /// <param name="contentRootDirectory">
    /// The directory GumPreview resolves relative content (fonts, textures) from (issue #4748) —
    /// the original project's own directory, even when <paramref name="projectPath"/> is a temporary
    /// converted copy elsewhere (a .gumx project served by the Native AOT build).
    /// </param>
    /// <param name="parentProcessId">
    /// The launching tool's process id. GumPreview exits when that process does, so a killed or
    /// crashed tool doesn't leave the preview open (issue #6011).
    /// </param>
    /// <param name="logPath">
    /// Where GumPreview writes its diagnostic log (<c>--log</c>), or null/empty for no log.
    /// <see cref="PreviewLauncher"/> fills it from <see cref="LogPathEnvironmentVariable"/>.
    /// </param>
    public static ProcessStartInfo Build(string executablePath, string projectPath, string elementName, string selectionFilePath, string contentRootDirectory, int parentProcessId, string? logPath = null)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo(executablePath)
        {
            UseShellExecute = false,
            // GumPreview has no console; its one-line startup error is only reachable through this pipe.
            RedirectStandardError = true,
        };
        startInfo.ArgumentList.Add("--project");
        startInfo.ArgumentList.Add(projectPath);
        startInfo.ArgumentList.Add("--element");
        startInfo.ArgumentList.Add(elementName);
        startInfo.ArgumentList.Add("--selection-file");
        startInfo.ArgumentList.Add(selectionFilePath);
        startInfo.ArgumentList.Add("--content-root");
        startInfo.ArgumentList.Add(contentRootDirectory);
        startInfo.ArgumentList.Add("--parent-pid");
        startInfo.ArgumentList.Add(parentProcessId.ToString(CultureInfo.InvariantCulture));
        if (!string.IsNullOrEmpty(logPath))
        {
            startInfo.ArgumentList.Add("--log");
            startInfo.ArgumentList.Add(logPath);
        }
        return startInfo;
    }
}
