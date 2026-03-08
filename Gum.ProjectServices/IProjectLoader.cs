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
    /// <summary>
    /// The loaded project, or null if a fatal error occurred.
    /// </summary>
    public GumProjectSave? Project { get; set; }

    /// <summary>
    /// Fatal error message when the project itself could not be loaded (file not found, malformed .gumx).
    /// Null when the project loaded successfully.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Non-fatal errors encountered while loading individual element or behavior files
    /// (malformed XML, missing files, name mismatches). These are structured as <see cref="ErrorResult"/>
    /// so they can be reported in the same format as checker errors.
    /// </summary>
    public List<ErrorResult> LoadErrors { get; set; }

    /// <summary>
    /// Whether the project loaded successfully. False when the .gumx file itself could not be loaded.
    /// May still be true when individual elements have errors (reported in <see cref="LoadErrors"/>).
    /// </summary>
    public bool Success => Project != null && ErrorMessage == null;

    public ProjectLoadResult()
    {
        LoadErrors = new List<ErrorResult>();
    }
}
