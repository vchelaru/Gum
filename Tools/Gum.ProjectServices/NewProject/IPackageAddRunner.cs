using System;
using System.Threading;
using System.Threading.Tasks;

namespace Gum.ProjectServices;

/// <summary>
/// Runs <c>dotnet add package</c> on a code project.
/// </summary>
public interface IPackageAddRunner
{
    /// <summary>
    /// Adds <paramref name="packageId"/> to <paramref name="csprojPath"/> without restoring and without opening a console
    /// window. The reference is written as <c>Version="*"</c>, like the Gum package the host template references, and
    /// works from any current folder. Cancelling kills the process tree.
    /// </summary>
    /// <param name="csprojPath">The project to change.</param>
    /// <param name="packageId">The NuGet package id.</param>
    /// <param name="onOutputLine">Receives each line the process prints, or <see langword="null"/>.</param>
    /// <param name="cancellationToken">Cancels the add.</param>
    Task<RestoreResult> AddAsync(string csprojPath, string packageId, Action<string>? onOutputLine, CancellationToken cancellationToken);
}
