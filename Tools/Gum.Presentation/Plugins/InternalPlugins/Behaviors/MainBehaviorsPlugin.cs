using Gum.Plugins.BaseClasses;
using System.ComponentModel.Composition;
using Gum.ToolStates;
using Gum.Undo;
using Gum.ToolCommands;
using Gum.Managers;

namespace Gum.Plugins.Behaviors;

/// <summary>
/// The Behaviors tab, shared by both heads. The logic is <see cref="BehaviorsLogic"/>; this plugin
/// forwards its events there and hands <see cref="BehaviorsViewModel"/> to the tab manager, where
/// each head supplies the view (TabViewRegistry).
/// </summary>
[Export(typeof(PluginBase))]
public class MainBehaviorsPlugin : CorePriorityPlugin
{
    private readonly ISelectedState _selectedState;
    private readonly IElementCommands _elementCommands;
    private readonly IUndoManager _undoManager;
    private readonly IProjectManager _projectManager;
    private readonly IPluginManager _pluginManager;

    private BehaviorsViewModel _viewModel = null!;
    private BehaviorsLogic _behaviorsLogic = null!;

    [ImportingConstructor]
    public MainBehaviorsPlugin(
        ISelectedState selectedState,
        IElementCommands elementCommands,
        IUndoManager undoManager,
        IProjectManager projectManager,
        IPluginManager pluginManager)
    {
        _selectedState = selectedState;
        _elementCommands = elementCommands;
        _undoManager = undoManager;
        _projectManager = projectManager;
        _pluginManager = pluginManager;
    }

    public override void StartUp()
    {
        _viewModel = new BehaviorsViewModel(_selectedState, _projectManager);

        // Each head resolves the view model to its own view (TabViewRegistry).
        IPluginTab behaviorsTab = _tabManager.AddControl(_viewModel, "Behaviors", TabLocation.CenterBottom);
        behaviorsTab.Hide();

        _behaviorsLogic = new BehaviorsLogic(
            _selectedState, _elementCommands, _undoManager, _pluginManager,
            _guiCommands, _fileCommands, _viewModel, behaviorsTab);

        _viewModel.ApplyChangedValues += _behaviorsLogic.HandleApplyBehaviorChanges;

        AssignEvents();
    }

    private void AssignEvents()
    {
        this.ElementSelected += _behaviorsLogic.HandleElementSelected;
        this.InstanceSelected += _behaviorsLogic.HandleInstanceSelected;
        this.BehaviorReferencesChanged += _behaviorsLogic.HandleBehaviorReferencesChanged;

        this.RefreshBehaviorView += _behaviorsLogic.HandleRefreshBehaviorView;

        this.StateAdd += _behaviorsLogic.HandleStateAdd;
        this.StateMovedToCategory += _behaviorsLogic.HandleStateMovedToCategory;
    }
}
