using System;
using System.Threading;
using System.Threading.Tasks;

namespace Gum.ProjectServices;

/// <summary>
/// Runs <c>dotnet restore</c> on a code project.
/// </summary>
public interface IRestoreRunner
{
    /// <summary>
    /// Restores <paramref name="csprojPath"/> without opening a console window. Cancelling kills the process tree.
    /// </summary>
    /// <param name="csprojPath">The project to restore.</param>
    /// <param name="onOutputLine">Receives each line the process prints, or <see langword="null"/>.</param>
    /// <param name="cancellationToken">Cancels the restore.</param>
    Task<RestoreResult> RestoreAsync(string csprojPath, Action<string>? onOutputLine, CancellationToken cancellationToken);
}
