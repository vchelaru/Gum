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
    public static ProcessStartInfo Build(string executablePath, string gumxPath, string elementName, string selectionFilePath)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo(executablePath)
        {
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add("--project");
        startInfo.ArgumentList.Add(gumxPath);
        startInfo.ArgumentList.Add("--element");
        startInfo.ArgumentList.Add(elementName);
        startInfo.ArgumentList.Add("--selection-file");
        startInfo.ArgumentList.Add(selectionFilePath);
        return startInfo;
    }
}
