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
}
