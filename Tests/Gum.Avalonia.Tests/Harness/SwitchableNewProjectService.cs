using Gum.ProjectServices;

namespace Gum.Avalonia.Tests.Harness;

/// <summary>
/// The test container's <see cref="INewProjectService"/>: the real one, until a test sets <see cref="CreateFiles"/> to
/// make file creation block, fail or finish at its own pace.
/// </summary>
internal sealed class SwitchableNewProjectService : INewProjectService
{
    private readonly INewProjectService _inner;

    public SwitchableNewProjectService(INewProjectService inner) => _inner = inner;

    /// <summary>Replaces file creation while set; reset to null after the test.</summary>
    public Func<ResolvedNewProject, CancellationToken, Task<NewProjectFilesResult>>? CreateFiles { get; set; }

    public bool IsSourceLinkedAvailable => _inner.IsSourceLinkedAvailable;

    public NewProjectValidation Validate(NewProjectRequest request) => _inner.Validate(request);

    public NewProjectPlan? GetPlan(NewProjectRequest request) => _inner.GetPlan(request);

    public Task<NewProjectFilesResult> CreateFilesAsync(ResolvedNewProject project, CancellationToken cancellationToken) =>
        CreateFiles != null ? CreateFiles(project, cancellationToken) : _inner.CreateFilesAsync(project, cancellationToken);

    public Task<RestoreResult> RestoreAsync(string csprojPath, Action<string>? onOutputLine, CancellationToken cancellationToken) =>
        _inner.RestoreAsync(csprojPath, onOutputLine, cancellationToken);

    public Task<NewProjectResult> ExecuteAsync(NewProjectRequest request, IProgress<NewProjectProgress>? progress, CancellationToken cancellationToken) =>
        _inner.ExecuteAsync(request, progress, cancellationToken);

    public string? FindGumSourceDirectory() => _inner.FindGumSourceDirectory();
}
