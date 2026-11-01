using System;
using System.Collections.Generic;
using Gum.DataTypes;

namespace Gum.ProjectServices;

/// <summary>
/// The one list of project-level editor settings every new project starts with, whichever template it was
/// copied from. Templates contribute element, behavior and standard references plus their own canvas size and
/// font ranges; they never decide these (#7461). The values come from <see cref="GumProjectSave"/>'s own defaults.
/// </summary>
internal static class NewProjectDefaults
{
    /// <summary>Resets the owned settings of the project at <paramref name="gumProjectPath"/> and re-saves it if any changed.</summary>
    internal static void ApplyToFile(string gumProjectPath)
    {
        ProjectLoadResult loadResult = new ProjectLoader().Load(gumProjectPath);
        if (!loadResult.Success || loadResult.Project == null)
        {
            throw new InvalidOperationException(
                $"Failed to load the new project to apply default settings: {loadResult.ErrorMessage}");
        }

        if (Apply(loadResult.Project))
        {
            loadResult.Project.Save(gumProjectPath, saveElements: false);
        }
    }

    /// <returns>True when a value changed.</returns>
    internal static bool Apply(GumProjectSave project)
    {
        GumProjectSave defaults = new GumProjectSave();
        bool changed = false;

        void Set<T>(T current, T value, Action<T> assign)
        {
            if (!EqualityComparer<T>.Default.Equals(current, value))
            {
                assign(value);
                changed = true;
            }
        }

        Set(project.ShowOutlines, defaults.ShowOutlines, v => project.ShowOutlines = v);
        Set(project.ShowCanvasOutline, defaults.ShowCanvasOutline, v => project.ShowCanvasOutline = v);
        Set(project.ShowRuler, defaults.ShowRuler, v => project.ShowRuler = v);
        Set(project.ShowCheckerBackground, defaults.ShowCheckerBackground, v => project.ShowCheckerBackground = v);
        Set(project.SnapToGrid, defaults.SnapToGrid, v => project.SnapToGrid = v);
        Set(project.GridSize, defaults.GridSize, v => project.GridSize = v);
        Set(project.TextureFilter, defaults.TextureFilter, v => project.TextureFilter = v);
        Set(project.RestrictToUnitValues, defaults.RestrictToUnitValues, v => project.RestrictToUnitValues = v);

        return changed;
    }
}
