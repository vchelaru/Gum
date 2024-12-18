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
    }

    private void HandleStateSelected(StateSave save)
    {
        WireframeObjectManager.Self.RefreshAll(forceLayout: true);
    }
}
