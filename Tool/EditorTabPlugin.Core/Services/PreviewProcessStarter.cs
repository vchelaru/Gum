using System.Diagnostics;

namespace Gum.Plugins.InternalPlugins.EditorTab.Services;

/// <summary>
/// Finds the GumPreview executable and starts it. <see cref="PreviewLauncher"/> goes through this
/// so a test can click Preview without opening a real preview window.
/// </summary>
public interface IPreviewProcessStarter
{
    /// <summary>The preview executable to launch, or null when none is installed or built.</summary>
    ResolvedPreviewExecutable? Resolve();

    /// <summary>Starts the preview; null when the process could not be started.</summary>
    IPreviewProcess? Start(ProcessStartInfo startInfo);
}

/// <summary>A started preview process.</summary>
public interface IPreviewProcess
{
    /// <summary>Whether the preview has closed.</summary>
    bool HasExited { get; }
}

/// <inheritdoc cref="IPreviewProcessStarter"/>
public class PreviewProcessStarter : IPreviewProcessStarter
{
    private readonly string _headBaseDirectory;

    /// <param name="headBaseDirectory">
    /// The running head's own base directory (<c>AppContext.BaseDirectory</c>), searched by
    /// <see cref="PreviewExecutableLocator"/>.
    /// </param>
    public PreviewProcessStarter(string headBaseDirectory)
    {
        _headBaseDirectory = headBaseDirectory;
    }

    /// <inheritdoc/>
    public ResolvedPreviewExecutable? Resolve() => PreviewExecutableLocator.Resolve(_headBaseDirectory);

    /// <inheritdoc/>
    public IPreviewProcess? Start(ProcessStartInfo startInfo) =>
        Process.Start(startInfo) is { } process ? new StartedProcess(process) : null;

    private sealed class StartedProcess : IPreviewProcess
    {
        private readonly Process _process;

        public StartedProcess(Process process)
        {
            _process = process;
        }

        public bool HasExited => _process.HasExited;
    }
}
