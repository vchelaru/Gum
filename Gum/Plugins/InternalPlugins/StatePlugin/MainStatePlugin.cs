using Gum.Commands;
using Gum.Managers;
using Gum.Plugins.BaseClasses;
using Gum.Plugins.InternalPlugins.StatePlugin.Views;
using Gum.PropertyGridHelpers;
using Gum.ToolStates;
using System.ComponentModel.Composition;
using Gum.Services.Dialogs;
using Gum.ToolCommands;
using Gum.Logic;

namespace Gum.Plugins.StatePlugin;

// This is new as of Oct 30, 2020
// I'd like to move all state logic to this plugin over time.
//
// As of issue #3926, the WPF-free reactions to state/element/instance selection, rename, delete,
// and variable-set events live in StateTreeController (Gum.Presentation). This plugin owns only the
// real platform glue that has no headless seam: constructing the WPF StateTreeView and registering
// it with the tab manager, and pushing the controller's tab title onto the WPF-typed PluginTab.Title.
[Export(typeof(Gum.Plugins.BaseClasses.PluginBase))]
public class MainStatePlugin : PriorityPlugin
{
    #region Fields/Properties

    StateTreeView stateTreeView;

    PluginTab newPluginTab;
    private readonly StateTreeViewRightClickService _stateTreeViewRightClickService;
    private readonly IHotkeyManager _hotkeyManager;
    private readonly ISelectedState _selectedState;
    private readonly ICopyPasteLogic _copyPasteLogic;
    private readonly StateTreeController _controller;

    #endregion

    #region Initialize

    [ImportingConstructor]
    public MainStatePlugin(ISelectedState selectedState, IGuiCommands guiCommands, IFileCommands fileCommands,
        IElementCommands elementCommands, IEditCommands editCommands, IDialogService dialogService,
        IHotkeyManager hotkeyManager, IVariableInCategoryPropagationLogic variableInCategoryPropagationLogic,
        ICopyPasteLogic copyPasteLogic)
    {
        _selectedState = selectedState;
        _hotkeyManager = hotkeyManager;
        _copyPasteLogic = copyPasteLogic;
        _stateTreeViewRightClickService = new StateTreeViewRightClickService(
            _selectedState,
            elementCommands,
            editCommands,
            dialogService,
            guiCommands,
            fileCommands,
            _copyPasteLogic);

        _controller = new StateTreeController(
            _stateTreeViewRightClickService,
            _selectedState,
            ObjectFinder.Self,
            variableInCategoryPropagationLogic);
    }

    public override void StartUp()
    {
        AssignEvents();

        CreateNewStateTab();

        // State Tree ViewManager needs init before MenuStripManager
    }

    private void AssignEvents()
    {
        this.TreeNodeSelected += _ => _controller.HandleTreeNodeSelected();

        this.RefreshStateTreeView += _controller.HandleRefreshStateTreeView;

        this.ReactToStateSaveSelected += _controller.HandleStateSelected;
        this.ReactToStateSaveCategorySelected += _controller.HandleStateSaveCategorySelected;

        this.StateRename += _controller.HandleStateRename;
        this.StateDelete += _controller.HandleStateDelete;
        this.StateMovedToCategory += _controller.HandleStateMovedToCategory;

        this.CategoryRename += _controller.HandleCategoryRename;
        this.BehaviorSelected += _controller.HandleBehaviorSelected;
        this.BehaviorReferenceSelected += _controller.HandleBehaviorReferenceSelected;
        this.InstanceSelected += _controller.HandleInstanceSelected;
        this.ElementSelected += _controller.HandleElementSelected;
        this.ElementDelete += _controller.HandleElementDeleted;
        this.VariableSet += _controller.HandleVariableSet;

        _controller.TabTitleChanged += title => newPluginTab.Title = title;
    }

    private void CreateNewStateTab()
    {
        stateTreeView = new StateTreeView(
            _controller.ViewModel,
            _stateTreeViewRightClickService,
            _hotkeyManager,
            _selectedState,
            _copyPasteLogic);
        _stateTreeViewRightClickService.SetContextMenu(stateTreeView.TreeViewContextMenu, stateTreeView);

        newPluginTab = _tabManager.AddControl(stateTreeView, "States", TabLocation.CenterTop);
    }

    #endregion
}
