using Gum.DataTypes;
using Gum.Services;
using System.Collections.Generic;

namespace Gum.Managers;

public interface IFavoriteComponentManager
{
    void AddToFavorites(ComponentSave component);
    List<ComponentSave> GetFavoritedComponentsForCurrentProject();
    List<ComponentSave> GetFilteredFavoritedComponentsFor(ElementSave parent, ICircularReferenceManager circularReferenceManager);
    void HandleComponentDeleted(ComponentSave component);
    void HandleComponentRenamed(string oldName, string newName);
    bool IsFavorite(ComponentSave component);
    void RemoveFromFavorites(ComponentSave component);
}
