using Gum.Plugins.BaseClasses;
using System.ComponentModel.Composition;

namespace Gum.Plugins.RenameLogicPlugin;


[Export(typeof(Gum.Plugins.BaseClasses.PluginBase))]
public class MainRenameLogic : InternalPlugin
{
    public override void StartUp()
    {
    }
}
