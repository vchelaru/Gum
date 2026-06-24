using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins.BaseClasses;
using System.ComponentModel.Composition;

namespace Gum.Plugins.FavoriteComponentPlugin;

[Export(typeof(PluginBase))]
public class MainFavoriteComponentPlugin : PriorityPlugin
{
    private readonly IFavoriteComponentManager _favoriteComponentManager;

    [ImportingConstructor]
    public MainFavoriteComponentPlugin(IFavoriteComponentManager favoriteComponentManager)
    {
        _favoriteComponentManager = favoriteComponentManager;
    }

    public override void StartUp()
    {
        this.ElementDelete += HandleElementDelete;
        this.ElementRename += HandleElementRename;
    }

    private void HandleElementDelete(ElementSave elementSave)
    {
        if (elementSave is ComponentSave component)
        {
            _favoriteComponentManager.HandleComponentDeleted(component);
        }
    }

    private void HandleElementRename(ElementSave elementSave, string oldName)
    {
        if (elementSave is ComponentSave)
        {
            _favoriteComponentManager.HandleComponentRenamed(oldName, elementSave.Name);
        }
    }
}
