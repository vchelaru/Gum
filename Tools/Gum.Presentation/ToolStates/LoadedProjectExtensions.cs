using System;
using Gum.DataTypes;
using Gum.Managers;

namespace Gum.ToolStates;

/// <summary>
/// For code that only runs while a project is open, such as commands that edit the project. The
/// tool creates a project at startup, so GumProjectSave is only null before that.
/// </summary>
public static class LoadedProjectExtensions
{
    public static GumProjectSave GetLoadedProject(this IProjectState projectState) =>
        projectState.GumProjectSave ?? throw new InvalidOperationException("No Gum project is loaded.");

    public static GumProjectSave GetLoadedProject(this IProjectManager projectManager) =>
        projectManager.GumProjectSave ?? throw new InvalidOperationException("No Gum project is loaded.");

    /// <summary>
    /// The project's file name, for code that only runs once the project is on disk. A project
    /// that was never saved (the New Project dialog was cancelled) has no file name.
    /// </summary>
    public static string GetSavedFileName(this GumProjectSave project) =>
        project.FullFileName ?? throw new InvalidOperationException("The Gum project has not been saved.");
}
