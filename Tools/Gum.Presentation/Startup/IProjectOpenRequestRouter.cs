using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gum.Startup;

/// <summary>
/// Routes a request from the operating system to open a project (for example a <c>.gumx</c>
/// double-clicked in the macOS Finder) to the right place: the startup project choice while the
/// tool is still starting, or an immediate load once it is running.
/// </summary>
public interface IProjectOpenRequestRouter
{
    /// <summary>
    /// Handles an open request for <paramref name="paths"/>. The first <c>.gumx</c>/<c>.gumj</c> path
    /// is used and other files are ignored. Before <see cref="CompleteStartupAsync"/> the project is
    /// held for startup; after it, the project loads right away. Call on the UI thread.
    /// </summary>
    Task RequestOpenAsync(IEnumerable<string> paths);

    /// <summary>
    /// Handles a request from another Gum launch: like <see cref="RequestOpenAsync(IEnumerable{string})"/>,
    /// then selects <see cref="SingleInstance.OpenRequest.Element"/> once its project is loaded. A project
    /// that is already open is not loaded again. The latest request received during startup wins.
    /// </summary>
    Task RequestOpenAsync(SingleInstance.OpenRequest request);

    /// <summary>
    /// Returns and clears the project requested during startup, or null if there is none. Called by
    /// the startup project load so the requested project replaces the last-opened one.
    /// </summary>
    string? TakePendingStartupProject();

    /// <summary>
    /// Marks startup as finished, then loads any project requested after the startup load had
    /// already chosen its project. Called once, at the end of the startup sequence.
    /// </summary>
    Task CompleteStartupAsync();
}
