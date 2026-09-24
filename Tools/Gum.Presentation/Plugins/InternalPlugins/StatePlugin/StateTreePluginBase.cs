using System;
using Gum.Commands;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins.BaseClasses;
using Gum.Plugins.InternalPlugins.StatePlugin.ViewModels;
using Gum.PropertyGridHelpers;
using Gum.Services.Dialogs;
using Gum.ToolCommands;
using Gum.ToolStates;

namespace Gum.Plugins.InternalPlugins.StatePlugin;

/// <summary>
/// The States tab: the states tree's reactions to selection, rename, delete and variable-set
/// events (<see cref="StateTreeController"/>), its right-click menu and hotkeys, and the tab
/// registration. Each head completes it with <see cref="CreateView"/>, its own tree control over the
/// shared view model.
/// </summary>
public abstract class StateTreePluginBase : PluginBase, IPriorityPlugin
{
    private readonly StateTreeController _controller;
    private IPluginTab? _tab;

    /// <summary>Creates the plugin over the services its menu, hotkeys and controller use.</summary>
    protected StateTreePluginBase(ISelectedState selectedState, IGuiCommands guiCommands, IFileCommands fileCommands,
        IElementCommands elementCommands, IEditCommands editCommands, IDialogService dialogService,
        IHotkeyManager hotkeyManager, IVariableInCategoryPropagationLogic variableInCategoryPropagationLogic,
        ICopyPasteLogic copyPasteLogic)
    {
        RightClickService = new StateTreeRightClickService(
            selectedState,
            elementCommands,
            editCommands,
            dialogService,
            guiCommands,
            fileCommands,
            copyPasteLogic);
        KeyboardHandler = new StateTreeKeyboardHandler(RightClickService, hotkeyManager, selectedState, copyPasteLogic);
        _controller = new StateTreeController(
            RightClickService,
            selectedState,
            ObjectFinder.Self,
            variableInCategoryPropagationLogic,
            dialogService);
    }

    /// <summary>The right-click menu the view renders.</summary>
    protected IStateTreeViewRightClickService RightClickService { get; }

    /// <summary>The hotkeys the view passes its key presses to.</summary>
    protected StateTreeKeyboardHandler KeyboardHandler { get; }

    /// <inheritdoc/>
    public override string FriendlyName => GetType().Name;

    /// <inheritdoc/>
    public override Version Version => new Version();

    /// <inheritdoc/>
    public override bool ShutDown(PluginShutDownReason shutDownReason) => false;

    /// <inheritdoc/>
    public override void StartUp()
    {
        AssignEvents();
        _tab = _tabManager.AddControl(CreateView(_controller.ViewModel), "States", TabLocation.CenterTop);
    }

    /// <summary>
    /// Builds this head's states tree over <paramref name="viewModel"/>, rendering
    /// <see cref="RightClickService"/>'s menu and passing key presses to <see cref="KeyboardHandler"/>.
    /// </summary>
    protected abstract object CreateView(StateTreeViewModel viewModel);

    private void AssignEvents()
    {
        TreeNodeSelected += _ => _controller.HandleTreeNodeSelected();

        RefreshStateTreeView += _controller.HandleRefreshStateTreeView;

        ReactToStateSaveSelected += _controller.HandleStateSelected;
        ReactToStateSaveCategorySelected += _controller.HandleStateSaveCategorySelected;

        StateRename += _controller.HandleStateRename;
        StateDelete += _controller.HandleStateDelete;
        StateMovedToCategory += _controller.HandleStateMovedToCategory;

        CategoryRename += _controller.HandleCategoryRename;
        BehaviorSelected += _controller.HandleBehaviorSelected;
        BehaviorReferenceSelected += _controller.HandleBehaviorReferenceSelected;
        InstanceSelected += _controller.HandleInstanceSelected;
        ElementSelected += _controller.HandleElementSelected;
        ElementDelete += _controller.HandleElementDeleted;
        VariableSet += _controller.HandleVariableSet;

        _controller.TabTitleChanged += title =>
        {
            if (_tab != null)
            {
                _tab.Title = title;
            }
        };
    }
}
