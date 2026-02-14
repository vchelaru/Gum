using Gum.DataTypes;
using Gum.Services;
using System.Collections.Generic;
using System.Linq;

namespace Gum.Managers;

public class FavoriteComponentManager : IFavoriteComponentManager
{
    private readonly IProjectManager _projectManager;

    public FavoriteComponentManager(IProjectManager projectManager)
    {
        _projectManager = projectManager;
    }

    public void AddToFavorites(ComponentSave component)
    {
        if (component == null) return;

        var project = ObjectFinder.Self.GumProjectSave;
        if (project?.FavoriteComponents == null) return;

        if (!project.FavoriteComponents.Contains(component.Name))
        {
            project.FavoriteComponents.Add(component.Name);
            _projectManager.SaveProject();
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

    public List<ComponentSave> GetFilteredFavoritedComponentsFor(ElementSave parent, ICircularReferenceManager circularReferenceManager)
    {
        if (parent == null || circularReferenceManager == null)
        {
            return new List<ComponentSave>();
        }

        var favorites = GetFavoritedComponentsForCurrentProject();
        return favorites
            .Where(c => circularReferenceManager.CanTypeBeAddedToElement(parent, c.Name))
            .ToList();
    }

    public void HandleComponentDeleted(ComponentSave component)
    {
        System.Diagnostics.Debug.Assert(component != null, "Component cannot be null when handling deletion.");

        var project = ObjectFinder.Self.GumProjectSave;
        if (project?.FavoriteComponents == null) return;

        if (project.FavoriteComponents.Remove(component.Name))
        {
            _projectManager.SaveProject();
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
            _projectManager.SaveProject();
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
        System.Diagnostics.Debug.Assert(component != null, "Component cannot be null when removing from favorites.");
        var project = ObjectFinder.Self.GumProjectSave;
        if (project?.FavoriteComponents == null) return;

        if (project.FavoriteComponents.Remove(component.Name))
        {
            _projectManager.SaveProject();
        }
    }
}
