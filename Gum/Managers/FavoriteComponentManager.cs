using Gum.DataTypes;
using System.Collections.Generic;

namespace Gum.Managers;

public class FavoriteComponentManager : Singleton<FavoriteComponentManager>
{
    public void AddToFavorites(ComponentSave component)
    {
        if (component == null) return;

        var project = ObjectFinder.Self.GumProjectSave;
        if (project?.FavoriteComponents == null) return;

        if (!project.FavoriteComponents.Contains(component.Name))
        {
            project.FavoriteComponents.Add(component.Name);
            Gum.ProjectManager.Self.SaveProject();
        }
    }

    public List<ComponentSave> GetFavoritedComponentsForCurrentProject()
    {
        var project = ObjectFinder.Self.GumProjectSave;
        if (project?.FavoriteComponents == null) return new List<ComponentSave>();

        var favorited = new List<ComponentSave>();

        foreach (var favoriteName in project.FavoriteComponents)
        {
            var element = ObjectFinder.Self.GetElementSave(favoriteName);
            if (element is ComponentSave component)
            {
                favorited.Add(component);
            }
        }

        return favorited;
    }

    public void HandleComponentDeleted(ComponentSave component)
    {
        if (component == null) return;

        var project = ObjectFinder.Self.GumProjectSave;
        if (project?.FavoriteComponents == null) return;

        if (project.FavoriteComponents.Remove(component.Name))
        {
            Gum.ProjectManager.Self.SaveProject();
        }
    }

    public void HandleComponentRenamed(string oldName, string newName)
    {
        var project = ObjectFinder.Self.GumProjectSave;
        if (project?.FavoriteComponents == null) return;

        var index = project.FavoriteComponents.IndexOf(oldName);
        if (index >= 0)
        {
            project.FavoriteComponents[index] = newName;
            Gum.ProjectManager.Self.SaveProject();
        }
    }

    public bool IsFavorite(ComponentSave component)
    {
        if (component == null) return false;

        var project = ObjectFinder.Self.GumProjectSave;
        if (project?.FavoriteComponents == null) return false;

        return project.FavoriteComponents.Contains(component.Name);
    }

    public void RemoveFromFavorites(ComponentSave component)
    {
        if (component == null) return;

        var project = ObjectFinder.Self.GumProjectSave;
        if (project?.FavoriteComponents == null) return;

        if (project.FavoriteComponents.Remove(component.Name))
        {
            Gum.ProjectManager.Self.SaveProject();
        }
    }
}
