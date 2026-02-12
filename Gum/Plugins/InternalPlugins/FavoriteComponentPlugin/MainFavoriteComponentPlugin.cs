using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins.BaseClasses;
using Gum.Services;
using System.ComponentModel.Composition;

namespace Gum.Plugins.FavoriteComponentPlugin;

[Export(typeof(PluginBase))]
public class MainFavoriteComponentPlugin : InternalPlugin
{
    private IFavoriteComponentManager _favoriteComponentManager;

    public override void StartUp()
    {
        _favoriteComponentManager = Locator.GetRequiredService<IFavoriteComponentManager>();
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
