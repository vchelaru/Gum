using Gum.Plugins.BaseClasses;
using System.ComponentModel.Composition;
using Gum.ToolStates;
using Gum.DataTypes.Behaviors;
using System.Windows.Forms;
using Gum.Undo;
using Gum.ToolCommands;
using Gum.Managers;

namespace Gum.Plugins.Behaviors;

[Export(typeof(PluginBase))]
public class MainBehaviorsPlugin : PriorityPlugin
{
    BehaviorsControl control;
    private readonly ISelectedState _selectedState;
    private readonly IElementCommands _elementCommands;
    private readonly IUndoManager _undoManager;
    private readonly IProjectManager _projectManager;
    private readonly IPluginManager _pluginManager;

    BehaviorsViewModel viewModel;
    PluginTab behaviorsTab;
    BehaviorsLogic behaviorsLogic;

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
        viewModel = new BehaviorsViewModel(_selectedState, _projectManager);

        control = new BehaviorsControl();
        control.DataContext = viewModel;
        behaviorsTab = _tabManager.AddControl(control, "Behaviors", TabLocation.CenterBottom);
        behaviorsTab.Hide();

        // The bulk of this plugin's logic lives in BehaviorsLogic (Gum.Presentation, headless and
        // unit tested) - this plugin is just the WPF glue that builds the view/tab above and
        // forwards its own plugin events into that logic (ADR-0005 Phase 3, #3928).
        behaviorsLogic = new BehaviorsLogic(
            _selectedState, _elementCommands, _undoManager, _pluginManager,
            _guiCommands, _fileCommands, viewModel, behaviorsTab);

        viewModel.ApplyChangedValues += behaviorsLogic.HandleApplyBehaviorChanges;

        AssignEvents();
    }

    private void AssignEvents()
    {
        this.ElementSelected += behaviorsLogic.HandleElementSelected;
        this.InstanceSelected += behaviorsLogic.HandleInstanceSelected;
        this.BehaviorSelected += HandleBehaviorSelected;
        this.StateWindowTreeNodeSelected += HandleStateSelected;
        this.BehaviorReferencesChanged += behaviorsLogic.HandleBehaviorReferencesChanged;

        this.RefreshBehaviorView += behaviorsLogic.HandleRefreshBehaviorView;

        this.StateAdd += behaviorsLogic.HandleStateAdd;
        this.StateMovedToCategory += behaviorsLogic.HandleStateMovedToCategory;
    }

    // System.Windows.Forms.TreeNode is a real WinForms-glue signature baked into
    // PluginBase.StateWindowTreeNodeSelected itself, so this handler can't move into
    // BehaviorsLogic; it's a no-op today regardless.
    private void HandleStateSelected(TreeNode obj)
    {

    }

    private void HandleBehaviorSelected(BehaviorSave obj)
    {

    }
}
