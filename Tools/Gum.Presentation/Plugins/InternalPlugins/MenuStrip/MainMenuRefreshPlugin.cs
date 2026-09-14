using System.ComponentModel.Composition;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Menus;
using Gum.Plugins.BaseClasses;

namespace Gum.Plugins.InternalPlugins.MenuStrip;

/// <summary>
/// Keeps the standard menus' selection-dependent state (the Remove headers and enabled flags,
/// Undo/Redo) in step with the selection and the undo stack, in both heads: each head renders the
/// same <see cref="MenuModel"/>, so refreshing the model refreshes its menu.
/// </summary>
[Export(typeof(PluginBase))]
public class MainMenuRefreshPlugin : CorePriorityPlugin
{
    private readonly StandardMenuModelBuilder _menuBuilder;

    [ImportingConstructor]
    public MainMenuRefreshPlugin(StandardMenuModelBuilder menuBuilder)
    {
        _menuBuilder = menuBuilder;
    }

    /// <inheritdoc/>
    public override void StartUp()
    {
        ElementSelected += HandleElementSelected;
        BehaviorSelected += HandleBehaviorSelected;
        InstanceSelected += HandleInstanceSelected;
        BehaviorVariableSelected += HandleBehaviorVariableSelected;
        AfterUndo += HandleAfterUndo;
    }

    private void HandleAfterUndo() => _menuBuilder.RefreshUI();

    private void HandleBehaviorVariableSelected(VariableSave save) => _menuBuilder.RefreshUI();

    private void HandleInstanceSelected(ElementSave element, InstanceSave instance) => _menuBuilder.RefreshUI();

    private void HandleBehaviorSelected(BehaviorSave? behavior) => _menuBuilder.RefreshUI();

    private void HandleElementSelected(ElementSave? element) => _menuBuilder.RefreshUI();
}
