using Gum.Managers;
using Gum.Plugins.BaseClasses;
using Gum.ToolStates;
using System.ComponentModel.Composition;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <summary>The WPF head's export of <see cref="VariableGridPluginBase"/>.</summary>
[Export(typeof(PluginBase))]
public class MainVariableGridPlugin : VariableGridPluginBase
{
    [ImportingConstructor]
    public MainVariableGridPlugin(ISelectedState selectedState, PropertyGridManager propertyGridManager, IVariableReferenceLogic variableReferenceLogic)
        : base(selectedState, propertyGridManager, variableReferenceLogic)
    {
    }
}
