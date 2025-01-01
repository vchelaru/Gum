using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.Managers;
using Gum.Plugins.BaseClasses;
using Gum.ToolStates;
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
        AssignEvents();
    }

    private void AssignEvents()
    {
        this.InstanceSelected += MainTreeViewPlugin_InstanceSelected;
        this.ElementSelected += HandleElementSelected;
        this.BehaviorSelected += HandleBehaviorSelected;
    }

    private void HandleBehaviorSelected(BehaviorSave save)
    {
        if(save != null)
        {
            ElementTreeViewManager.Self.Select(save);
        }
    }

    private void HandleElementSelected(ElementSave save)
    {
        if(save != null)
        {
            ElementTreeViewManager.Self.Select(save);
        }
    }

    private void MainTreeViewPlugin_InstanceSelected(DataTypes.ElementSave element, DataTypes.InstanceSave instance)
    {
        if(element != null || instance != null)
        {
            if(instance != null)
            {

                ElementTreeViewManager.Self.Select(SelectedState.Self.SelectedInstances);
            }

            if(instance == null && element != null)
            {
                ElementTreeViewManager.Self.Select(element);
            }
        }

    }
}
