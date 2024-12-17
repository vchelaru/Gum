using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins.BaseClasses;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Plugins.InternalPlugins.TreeView;

[Export(typeof(PluginBase))]
internal class MainTreeViewPlugin : InternalPlugin
{
    public override void StartUp()
    {
        this.InstanceSelected += MainTreeViewPlugin_InstanceSelected;
    }

    private void MainTreeViewPlugin_InstanceSelected(DataTypes.ElementSave element, DataTypes.InstanceSave instance)
    {
        if(element != null || instance != null)
        {
            ElementTreeViewManager.Self.Select(instance, element);

            if(instance == null && element != null)
            {
                ElementTreeViewManager.Self.Select(element);
            }
        }

    }
}
