using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Mvvm;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Services;
using Gum.ViewModels;
using System.Collections.ObjectModel;
using System.Drawing;

namespace Gum.Plugins.VariableGrid;

/// <summary>
/// Presentation state for the Variables tab's main control (ADR-0004/0005, #3754). Visibility is
/// exposed as <c>bool</c> and the state-label background as <see cref="Color"/> (Gum's native
/// color type) so it stays eligible for the headless Gum.Presentation assembly; the WPF view binds
/// these through <c>BoolToVisibilityConverter</c> and a color-to-brush conversion. The behavior
/// variable right-click menu is exposed as <see cref="ContextMenuItemViewModel"/>s rather than WPF
/// MenuItems, mirroring the pattern established for <c>ElementAnimationsViewModel</c> (#3786); the
/// WPF view rebuilds real MenuItems from these.
/// </summary>
public class MainControlViewModel : ViewModel
{
    public bool HasStateInformation
    {
        get => Get<bool>();
        set => Set(value);
    }

    public bool HasErrors
    {
        get => Get<bool>();
        set => Set(value);
    }

    public string StateInformation
    {
        get => Get<string>();
        set => Set(value);
    }

    public Color StateBackground
    {
        get => Get<Color>();
        set => Set(value);
    }

    public string ErrorInformation
    {
        get => Get<string>();
        set => Set(value);
    }

    #region Behaviors

    public bool ShowBehaviorUi
    {
        get => Get<bool>();
        set => Set(value);
    }

    public BehaviorSave BehaviorSave { get; set; }

    public ObservableCollection<VariableSave> BehaviorVariables
    {
        get;
        private set;
    } = new ObservableCollection<VariableSave>();

    public VariableSave SelectedBehaviorVariable
    {
        get => Get<VariableSave>();
        set
        {
            if (Set(value))
            {
                RefreshBehaviorVariablesContextMenuItems();
            }
        }
    }

    public ObservableCollection<ContextMenuItemViewModel> BehaviorVariablesContextMenuItems
    {
        get;
        private set;
    } = new ObservableCollection<ContextMenuItemViewModel>();

    private readonly IDeleteVariableService _deleteVariableService;
    private readonly IEditVariableService _editVariableService;

    #endregion

    public bool HasCategoryNotification
    {
        get => Get<bool>();
        set => Set(value);
    }

    public string CategoryNotification
    {
        get => Get<string>();
        set => Set(value);
    }

    public bool ShowVariableGrid
    {
        get => Get<bool>();
        set => Set(value);
    }

    public bool IsAddVariableButtonVisible
    {
        get => Get<bool>();
        set => Set(value);
    }

    public VariableSave EffectiveSelectedBehaviorVariable
    {
        get
        {
            if (ShowBehaviorUi)
            {
                return SelectedBehaviorVariable;
            }
            else
            {
                return null;
            }
        }
    }

    public MainControlViewModel(IDeleteVariableService deleteVariableService, IEditVariableService editVariableService)
    {
        _deleteVariableService = deleteVariableService;
        _editVariableService = editVariableService;
    }

    private void RefreshBehaviorVariablesContextMenuItems()
    {
        BehaviorVariablesContextMenuItems.Clear();

        if (SelectedBehaviorVariable != null)
        {
            BehaviorVariablesContextMenuItems.Add(new ContextMenuItemViewModel
            {
                Text = "Edit Variable",
                Action = HandleEditVariableClicked
            });

            BehaviorVariablesContextMenuItems.Add(new ContextMenuItemViewModel
            {
                Text = "Delete Variable",
                Action = HandleDeleteVariableClicked
            });
        }
    }

    private void HandleDeleteVariableClicked()
    {
        if (BehaviorSave != null)
        {
            _deleteVariableService.DeleteVariable(SelectedBehaviorVariable, BehaviorSave);
        }
    }

    private void HandleEditVariableClicked()
    {
        var editModes =
            _editVariableService.GetAvailableEditModeFor(SelectedBehaviorVariable, BehaviorSave);

        if (editModes != VariableEditMode.None)
        {
            _editVariableService.ShowEditVariableWindow(SelectedBehaviorVariable, BehaviorSave);
        }
    }
}
