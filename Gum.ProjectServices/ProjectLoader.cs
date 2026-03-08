using System;
using Gum.DataTypes;

namespace Gum.ProjectServices;

/// <inheritdoc/>
public class ProjectLoader : IProjectLoader
{
    /// <inheritdoc/>
    public ProjectLoadResult Load(string filePath)
    {
        var result = new ProjectLoadResult();

        if (!System.IO.File.Exists(filePath))
        {
            result.ErrorMessage = $"Project file not found: {filePath}";
            return result;
        }

        var gumLoadResult = new GumLoadResult();
        try
        {
            var project = GumProjectSave.Load(filePath, out gumLoadResult);
            result.Project = project;
            result.ErrorMessage = string.IsNullOrEmpty(gumLoadResult.ErrorMessage)
                ? null
                : gumLoadResult.ErrorMessage;
            result.MissingFiles.AddRange(gumLoadResult.MissingFiles);
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"Failed to load project: {ex.Message}";
        }

        return result;
    }
}
