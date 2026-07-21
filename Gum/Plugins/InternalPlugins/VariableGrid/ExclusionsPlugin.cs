using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Plugins.BaseClasses;
using Gum.ToolStates;
using System.ComponentModel.Composition;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <summary>
/// Thin MEF entry point wiring the VariableExcluded/VariableSet plugin events to
/// <see cref="ExclusionsLogic"/>, which owns the actual variable-exclusion decision logic and
/// lives in the headless Gum.Presentation assembly.
/// </summary>
[Export(typeof(PluginBase))]
public class ExclusionsPlugin : PriorityPlugin
{
    private readonly ExclusionsLogic _logic;

    [ImportingConstructor]
    public ExclusionsPlugin(ISelectedState selectedState, IGuiCommands guiCommands)
    {
        _logic = new ExclusionsLogic(selectedState, guiCommands);
    }

    public override void StartUp()
    {
        this.VariableExcluded += _logic.GetIfVariableIsExcluded;
        this.VariableSet += _logic.HandleVariableSet;
    }
}
