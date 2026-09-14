using Gum.Commands;
using Gum.Plugins.BaseClasses;
using Gum.ToolStates;
using System.ComponentModel.Composition;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <summary>The WPF head's export of <see cref="ExclusionsPluginBase"/>.</summary>
[Export(typeof(PluginBase))]
public class ExclusionsPlugin : ExclusionsPluginBase
{
    [ImportingConstructor]
    public ExclusionsPlugin(ISelectedState selectedState, IGuiCommands guiCommands)
        : base(selectedState, guiCommands)
    {
    }
}
