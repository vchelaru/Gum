using Gum.DataTypes;
using System.Threading;

namespace Gum.ProjectServices.CodeGeneration;

/// <summary>
/// Compares the code output folder (and the per-element .codsj settings files alongside the element
/// XML) against a project's elements, reporting files that no element accounts for. Split in two so
/// the disk walk can run off the UI thread: <see cref="CreatePlan"/> reads the project,
/// <see cref="Execute"/> reads only the disk.
/// </summary>
public interface IOrphanCodeFileScanService
{
    /// <summary>
    /// Captures the files each element in <paramref name="project"/> accounts for. Reads the project,
    /// so call it on the thread that owns the project.
    /// </summary>
    OrphanCodeFileScanPlan CreatePlan(GumProjectSave project, CodeOutputProjectSettings projectSettings);

    /// <summary>
    /// Walks the disk for files the plan does not account for. Read-only, never touches the project,
    /// and safe to run on a worker thread. The walk of the code output folder is bounded, see
    /// <see cref="OrphanCodeFileScanResult.IsTruncated"/>.
    /// </summary>
    /// <exception cref="System.OperationCanceledException">When <paramref name="cancellationToken"/> is cancelled.</exception>
    OrphanCodeFileScanResult Execute(OrphanCodeFileScanPlan plan, CancellationToken cancellationToken);
}
