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
        // Edit > Remove names the selected state or category.
        ReactToStateSaveSelected += HandleStateSelected;
        ReactToStateSaveCategorySelected += HandleCategorySelected;
        // A deleted behavior variable must drop out of Edit > Remove.
        VariableDelete += HandleVariableDelete;
        AfterUndo += HandleAfterUndo;
    }

    private void HandleVariableDelete(ElementSave? element, string variableName) => _menuBuilder.RefreshUI();

    private void HandleStateSelected(StateSave? state) => _menuBuilder.RefreshUI();

    private void HandleCategorySelected(StateSaveCategory? category) => _menuBuilder.RefreshUI();

    private void HandleAfterUndo() => _menuBuilder.RefreshUI();

    private void HandleBehaviorVariableSelected(VariableSave? save) => _menuBuilder.RefreshUI();

    private void HandleInstanceSelected(ElementSave? element, InstanceSave? instance) => _menuBuilder.RefreshUI();

    private void HandleBehaviorSelected(BehaviorSave? behavior) => _menuBuilder.RefreshUI();

    private void HandleElementSelected(ElementSave? element) => _menuBuilder.RefreshUI();
}
