using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins.BaseClasses;
using System.ComponentModel.Composition;

namespace Gum.Plugins.FavoriteComponentPlugin;

[Export(typeof(PluginBase))]
public class MainFavoriteComponentPlugin : InternalPlugin
{
    public override void StartUp()
    {
        this.ElementDelete += HandleElementDelete;
        this.ElementRename += HandleElementRename;
    }

    private void HandleElementDelete(ElementSave elementSave)
    {
        if (elementSave is ComponentSave component)
        {
            FavoriteComponentManager.Self.HandleComponentDeleted(component);
        }
    }

    private void HandleElementRename(ElementSave elementSave, string oldName)
    {
        if (elementSave is ComponentSave)
        {
            FavoriteComponentManager.Self.HandleComponentRenamed(oldName, elementSave.Name);
        }
    }
}
