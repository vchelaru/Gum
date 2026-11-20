using Gum.DataTypes;

namespace Gum.ProjectServices.Editing;

/// <summary>
/// Keeps the project's <see cref="GumProjectSave.FavoriteComponents"/> in step with its Components: the data
/// change behind the tool's favorites handling, shared with the headless edit.
/// </summary>
public static class FavoriteComponentList
{
    /// <summary>Drops <paramref name="componentName"/> from the favorites. Returns whether it was listed.</summary>
    public static bool Remove(GumProjectSave? project, string componentName) =>
        project?.FavoriteComponents?.Remove(componentName) ?? false;

    /// <summary>Renames a favorite in place. Returns whether <paramref name="oldName"/> was listed.</summary>
    public static bool Rename(GumProjectSave? project, string oldName, string newName)
    {
        int index = project?.FavoriteComponents?.IndexOf(oldName) ?? -1;
        if (index < 0)
        {
            return false;
        }
        project!.FavoriteComponents[index] = newName;
        return true;
    }
}
