using System.Collections.Generic;
using Gum.DataTypes;

namespace Gum.ProjectServices;

/// <summary>
/// Loads Gum projects from disk without requiring the Gum tool UI.
/// </summary>
public interface IProjectLoader
{
    /// <summary>
    /// Loads a Gum project from the specified .gumx file path.
    /// </summary>
    ProjectLoadResult Load(string filePath);
}

/// <summary>
/// Result of loading a Gum project.
/// </summary>
public class ProjectLoadResult
{
    public GumProjectSave? Project { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> MissingFiles { get; set; }
    public bool Success => Project != null && ErrorMessage == null;

    public ProjectLoadResult()
    {
        MissingFiles = new List<string>();
    }
}
