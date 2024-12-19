using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Plugins.BaseClasses;
using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Plugins.InternalPlugins.EditorTab;

[Export(typeof(PluginBase))]
internal class MainEditorTabPlugin : InternalPlugin
{
    public override void StartUp()
    {
        AssignEvents();
    }

    private void AssignEvents()
    {
        this.ReactToStateSaveSelected += HandleStateSelected;
        this.InstanceSelected += HandleInstanceSelected;
    }

    private void HandleInstanceSelected(ElementSave element, InstanceSave instance)
    {
        WireframeObjectManager.Self.RefreshAll(forceLayout: false);
    }

    private void HandleStateSelected(StateSave save)
    {
        WireframeObjectManager.Self.RefreshAll(forceLayout: true);
    }
}
