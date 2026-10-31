using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Gum.ProjectServices;

/// <summary>
/// Creates a new Gum project, optionally with a code project around it. The one implementation behind
/// <c>gumcli new</c> and the New Project dialog.
/// </summary>
public interface INewProjectService
{
    /// <summary>
    /// Checks a request and resolves it to full paths, in the order and with the messages <c>gumcli new</c> uses.
    /// Touches nothing on disk except reading.
    /// </summary>
    NewProjectValidation Validate(NewProjectRequest request);

    /// <summary>
    /// Writes the files of an already validated request. On failure or cancellation removes only what this call
    /// created (a pre-existing file it overwrote stays as written).
    /// </summary>
    Task<NewProjectFilesResult> CreateFilesAsync(ResolvedNewProject project, CancellationToken cancellationToken);

    /// <summary>
    /// Runs <c>dotnet restore</c> on a code project; callable alone to retry a failed restore.
    /// </summary>
    /// <param name="csprojPath">The code project file.</param>
    /// <param name="onOutputLine">Receives each line restore prints, or <see langword="null"/>.</param>
    /// <param name="cancellationToken">Kills the restore.</param>
    Task<RestoreResult> RestoreAsync(string csprojPath, Action<string>? onOutputLine, CancellationToken cancellationToken);

    /// <summary>
    /// Runs <c>dotnet add package</c> for each package in order, stopping at the first that does not succeed;
    /// callable alone to retry a failed add. Adding an already referenced package is harmless.
    /// </summary>
    /// <param name="csprojPath">The code project file.</param>
    /// <param name="packages">The package ids, usually <see cref="ResolvedNewProject.RecommendedPackages"/>.</param>
    /// <param name="onOutputLine">Receives each line the process prints, or <see langword="null"/>.</param>
    /// <param name="cancellationToken">Kills the running add.</param>
    Task<AddPackagesResult> AddPackagesAsync(
        string csprojPath, IReadOnlyList<string> packages, Action<string>? onOutputLine, CancellationToken cancellationToken);

    /// <summary>
    /// Validates, writes the files, adds the <see cref="NewProjectRequest.IncludeRecommendedPackages"/> packages, then restores when the request has a platform and <see cref="NewProjectRequest.Restore"/>.
    /// A failed restore leaves the files in place; retry it with <see cref="RestoreAsync"/>.
    /// </summary>
    Task<NewProjectResult> ExecuteAsync(
        NewProjectRequest request, IProgress<NewProjectProgress>? progress, CancellationToken cancellationToken);

    /// <summary>The Gum checkout source-linked projects would reference, or <see langword="null"/> when there is none.</summary>
    string? FindGumSourceDirectory();

    /// <summary>Whether a source-linked project can be created (a Gum checkout was found).</summary>
    bool IsSourceLinkedAvailable { get; }
}
