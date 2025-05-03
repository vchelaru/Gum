using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.Managers;
using Gum.Plugins.BaseClasses;
using Gum.ToolStates;
using Gum.Wireframe;
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
        this.InstanceAdd += HandleInstanceAdd;
        this.ElementSelected += HandleElementSelected;

        this.BehaviorSelected += HandleBehaviorSelected;
        this.BehaviorDeleted += HandleBehaviorDeleted;

        this.ElementDelete += HandleElementDeleted;
        this.ElementAdd += HandleElementAdd;

        this.BehaviorCreated += HandleBehaviorCreated;

        this.ProjectLoad += HandleProjectLoad;
    }

    private void HandleInstanceAdd(ElementSave save1, InstanceSave save2)
    {
        ElementTreeViewManager.Self.RefreshUi();
    }

    private void HandleBehaviorDeleted(BehaviorSave save)
    {
        ElementTreeViewManager.Self.RefreshUi();
    }

    private void HandleProjectLoad(GumProjectSave save)
    {
        ElementTreeViewManager.Self.RefreshUi();
    }

    private void HandleElementAdd(ElementSave save)
    {
        ElementTreeViewManager.Self.RefreshUi();
    }

    private void HandleBehaviorCreated(BehaviorSave save)
    {
        ElementTreeViewManager.Self.RefreshUi();
    }

    private void HandleElementDeleted(ElementSave save)
    {
        ElementTreeViewManager.Self.RefreshUi();
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
