using Gum.DataTypes;
using System.Collections.Generic;

namespace Gum.ProjectServices.CodeGeneration;

/// <summary>
/// Compares the code output folder (and the per-element .codsj settings files alongside the element
/// XML) against a project's elements, reporting files that no element accounts for.
/// </summary>
public interface IOrphanCodeFileScanService
{
    /// <summary>
    /// Returns every file under the project's code output folder — plus every per-element .codsj
    /// settings file — with no matching element in <paramref name="project"/>. Read-only: nothing is
    /// modified or deleted. Returns an empty list when no code output folder is configured.
    /// </summary>
    IReadOnlyList<OrphanCodeFile> Scan(GumProjectSave project, CodeOutputProjectSettings projectSettings);
}
