using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Input;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Reflection;
using Gum.Services;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using ToolsUtilities;
using WpfDataUi.DataTypes;

namespace Gum.PropertyGridHelpers;

/// <summary>
/// The Variables tab's row: an <see cref="InstanceMember"/> (the grid's neutral member model) over a
/// <see cref="VariableGridEntry"/>, which holds the decision logic. Every member forwards to the
/// entry, translating only at the model boundary (<see cref="VariableDisplayerKind"/> -&gt; a
/// displayer key, <see cref="VariablePropertyCommitType"/> &lt;-&gt; <see cref="SetPropertyCommitType"/>,
/// <see cref="VariableContextMenuAction"/> -&gt; <see cref="InstanceMember.ContextMenuEvents"/>).
/// </summary>
public class StateReferencingInstanceMember : InstanceMember
{
    #region Fields

    private VariableGridEntry _entry;

    #endregion

    internal VariableGridEntry Entry => _entry;

    /// <summary>
    /// Whether <see cref="Retarget"/> can safely re-point this row at <paramref name="newEntry"/>
    /// without recreating the bound WPF control. Requires the same <see cref="PreferredDisplayer"/>
    /// type - a mismatch means the existing control can't render the new entry's data even if the
    /// variable name is the same (e.g. differing available-states drive a ComboBox in one instance
    /// but not another).
    /// </summary>
    internal bool CanRetargetTo(StateReferencingInstanceMember other) =>
        RootVariableName == other.RootVariableName && PreferredDisplayer == other.PreferredDisplayer;

    /// <summary>
    /// Re-points this row at a freshly built entry for a different instance, in place, instead of
    /// being replaced by a new <see cref="StateReferencingInstanceMember"/> - avoids the WPF
    /// container recreation/layout cost of swapping the row out entirely. Callers must have already
    /// verified <see cref="CanRetargetTo"/>.
    /// </summary>
    internal void Retarget(VariableGridEntry newEntry)
    {
        bool wasReadOnly = _entry.IsReadOnly;

        _entry = newEntry;

        if (wasReadOnly != _entry.IsReadOnly)
        {
            if (wasReadOnly)
            {
                this.CustomSetPropertyEvent += HandleCustomSet;
                this.SetToDefault += HandleSetToDefault;
            }
            else
            {
                this.CustomSetPropertyEvent -= HandleCustomSet;
                this.SetToDefault -= HandleSetToDefault;
            }
        }

        this.Instance = _entry.Instance;
        this.DisplayName = _entry.DisplayName;
        this.DetailText = _entry.DetailText;
        this.ToolTipText = _entry.ToolTipText;
        this.SupportsMakeDefault = _entry.SupportsMakeDefault;

        this.PropertiesToSetOnDisplayer.Clear();
        foreach (var kvp in _entry.PropertiesToSetOnDisplayer)
        {
            this.PropertiesToSetOnDisplayer[kvp.Key] = kvp.Value;
        }

        ContextMenuEvents.Clear();
        PopulateContextMenu();

        SimulateValueChanged();
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(DetailText));
        OnPropertyChanged(nameof(ToolTipText));
        OnPropertyChanged(nameof(IsReadOnly));
    }

    #region Properties

    public StateSaveCategory? StateSaveCategory
    {
        get => _entry.StateSaveCategory;
        set => _entry.StateSaveCategory = value;
    }

    /// <inheritdoc cref="Gum.Plugins.InternalPlugins.VariableGrid.VariableGridEntry.StateSave"/>
    public StateSave? StateSave => _entry.StateSave;

    public InstanceSave? InstanceSave => _entry.InstanceSave;

    public ElementSave? ElementSave => _entry.ElementSave;

    public object? LastOldFullCommitValue => _entry.LastOldFullCommitValue;

    /// <summary>
    /// Optional fallback consulted by the value getter when neither the selected
    /// state nor any inherited state has a value for this variable. Used by the
    /// behavior-FormsProperty surfacing path so a declared default (e.g.
    /// <c>IsEnabled = true</c>) appears in the grid without writing into state.
    /// </summary>
    public Func<object?>? DefaultValueFallback
    {
        get => _entry.DefaultValueFallback;
        set => _entry.DefaultValueFallback = value;
    }

    public string RootVariableName => _entry.RootVariableName;

    /// <inheritdoc cref="VariableGridEntry.IsAssignedByReference"/>
    public bool IsAssignedByReference => _entry.IsAssignedByReference;

    /// <inheritdoc cref="VariableGridEntry.IsStateSelection"/>
    public bool IsStateSelection => _entry.IsStateSelection;

    public int SortValue
    {
        get => _entry.SortValue;
        set => _entry.SortValue = value;
    }

    // Prior to April 10 2023 this was always true. Now that we have multi-select, we don't want to
    // call it here if editing multiple objects. Instead, we want to have the multi-select call it and pass
    // the list of variables so that a single undo can be performed.
    public bool IsCallingRefresh
    {
        get => _entry.IsCallingRefresh;
        set => _entry.IsCallingRefresh = value;
    }

    public override bool IsReadOnly => _entry.IsReadOnly;

    public override bool IsDefault
    {
        get => _entry.IsDefault;
        set
        {
            if (value)
            {
                _entry.ResetToDefault();
            }
        }
    }

    public override IList<object> CustomOptions => _entry.CustomOptions ?? base.CustomOptions;

    /// <summary>
    /// Translates <see cref="VariableGridEntry.PreferredDisplayerKind"/>/<see cref="VariableGridEntry.PreferredDisplayerOverride"/>
    /// to the displayer the grid shows. An explicit override (a displayer key or a head control) is
    /// passed through unchanged; otherwise the neutral <see cref="VariableDisplayerKind"/> maps to a
    /// <see cref="StandardDisplayers"/> key that each head resolves to its own control.
    /// </summary>
    public override Type PreferredDisplayer
    {
        get
        {
            if (_entry.PreferredDisplayerOverride != null)
            {
                return _entry.PreferredDisplayerOverride;
            }

            return _entry.PreferredDisplayerKind switch
            {
                VariableDisplayerKind.ComboBox => typeof(StandardDisplayers.ComboBox),
                VariableDisplayerKind.FileSelection => typeof(StandardDisplayers.FileSelection),
                VariableDisplayerKind.ListBox => typeof(StandardDisplayers.ListBox),
                VariableDisplayerKind.MultiLineTextBox => typeof(StandardDisplayers.MultiLineTextBox),
                _ => null,
            };
        }
        set
        {
            _entry.PreferredDisplayerOverride = value;
            OnPropertyChanged(nameof(PreferredDisplayer));
        }
    }

    #endregion

    #region Constructor/Initialization

    public StateReferencingInstanceMember(
        Attribute[] attributes,
        TypeConverter? converter,
        Type? componentType,
        bool isReadOnly,
        bool isAssignedByReference,
        bool isVariable,
        StateSave stateSave,
        StateSaveCategory? stateSaveCategory,
        string variableName,
        InstanceSave? instanceSave,
        IStateContainer stateListCategoryContainer,
        ISelectedState selectedState,
        IUndoManager undoManager,
        IGuiCommands guiCommands,
        IFileCommands fileCommands,
        ISetVariableLogic setVariableLogic,
        IWireframeObjectManager wireframeObjectManager,
        IPluginManager pluginManager,
        IHotkeyManager hotkeyManager,
        IDeleteVariableService deleteVariableService,
        IExposeVariableService exposeVariableService,
        IEditVariableService editVariableService,
        ITypeManager typeManager,
        IClipboardService clipboardService) :
        base(variableName, stateSave)
    {
        _entry = new VariableGridEntry(
            attributes,
            converter,
            componentType,
            isReadOnly,
            isAssignedByReference,
            isVariable,
            stateSave,
            stateSaveCategory,
            variableName,
            instanceSave,
            stateListCategoryContainer,
            selectedState,
            undoManager,
            guiCommands,
            fileCommands,
            setVariableLogic,
            wireframeObjectManager,
            pluginManager,
            hotkeyManager,
            deleteVariableService,
            exposeVariableService,
            editVariableService,
            typeManager,
            clipboardService);

        Initialize();
    }

    /// <summary>
    /// Wraps an already-built <see cref="VariableGridEntry"/> - used by
    /// <c>PropertyGridManager.ToWpf</c> to materialize a headless <see cref="Gum.Plugins.InternalPlugins.VariableGrid.VariableCategoryDescriptor"/>
    /// (built by the relocated <c>ElementSaveDisplayer</c>) into the real WPF row the live grid needs.
    /// </summary>
    public StateReferencingInstanceMember(VariableGridEntry entry) :
        // entry.StateSave is genuinely null for a behavior's required-instance entries (Name/BaseType
        // only). InstanceMember.Instance already tolerates a null Instance elsewhere in its own logic
        // (see its "Instance != null" checks), so this is a real, accepted null - not an assertion.
        base(entry.Name, entry.StateSave!)
    {
        _entry = entry;

        Initialize();
    }

    private void Initialize()
    {
        if (!_entry.IsReadOnly)
        {
            this.CustomSetPropertyEvent += HandleCustomSet;
            this.SetToDefault += HandleSetToDefault;
        }
        this.CustomGetEvent += HandleCustomGet;
        this.CustomGetTypeEvent += HandleCustomGetType;

        this.Instance = _entry.Instance;
        this.DisplayName = _entry.DisplayName;
        this.DetailText = _entry.DetailText;
        this.ToolTipText = _entry.ToolTipText;
        this.SupportsMakeDefault = _entry.SupportsMakeDefault;

        foreach (var kvp in _entry.PropertiesToSetOnDisplayer)
        {
            this.PropertiesToSetOnDisplayer[kvp.Key] = kvp.Value;
        }

        // Generic hook for a headless caller (e.g. ElementSaveDisplayer's XUnits/YUnits subtext) that
        // needs DetailText to live-update as the value changes - something only the WPF-bound Value
        // property change notification (below) can observe.
        if (_entry.RecomputeDetailTextOnValueChanged != null)
        {
            this.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == "Value")
                {
                    _entry.RecomputeDetailTextOnValueChanged();
                    this.DetailText = _entry.DetailText;
                }
            };
        }

        PopulateContextMenu();
    }

    /// <summary>
    /// A key press in this row's variable-reference editor, with the text of the line under the
    /// caret. Forwards to the current entry (rows are retargeted), which navigates on the
    /// go-to-definition hotkey and ignores every variable but VariableReferences.
    /// </summary>
    public void HandleReferenceTextEditKeyDown(GumKeyEventArgs e, string currentLineText) =>
        _entry.HandleReferenceTextEditKeyDown(e, currentLineText);

    #endregion

    private void PopulateContextMenu()
    {
        foreach (var action in _entry.BuildContextMenuActions())
        {
            ContextMenuEvents.Add(action.Label, (sender, e) => action.Execute());
        }
    }

    #region Get Value

    private object? HandleCustomGet(object instance) => _entry.GetValue(instance);

    #endregion

    #region Set Value

    private void HandleCustomSet(object gumElementOrInstanceSaveAsObject, SetPropertyArgs setPropertyArgs)
    {
        var response = _entry.SetValue(gumElementOrInstanceSaveAsObject, setPropertyArgs.Value, MapCommitType(setPropertyArgs.CommitType));

        if (!response.Succeeded)
        {
            setPropertyArgs.IsAssignmentCancelled = true;
        }
    }

    #endregion

    #region Set to default

    public event Action<string>? SetToDefault;
    private void HandleSetToDefault(string obj) => _entry.ResetToDefault();

    #endregion

    public GeneralResponse NotifyVariableLogic(object gumElementOrInstanceSaveAsObject, SetPropertyCommitType commitType, bool trySave = true) =>
        _entry.NotifyVariableLogic(gumElementOrInstanceSaveAsObject, MapCommitType(commitType), trySave);

    private Type? HandleCustomGetType(object instance) => _entry.GetValueType(instance);

    public VariableSave? GetRootVariableSave() => _entry.GetRootVariableSave();

    public Type? TryGetTypeFromVariableListSave() => _entry.TryGetTypeFromVariableListSave();

    private static VariablePropertyCommitType MapCommitType(SetPropertyCommitType commitType) =>
        commitType == SetPropertyCommitType.Full ? VariablePropertyCommitType.Full : VariablePropertyCommitType.Intermediate;
}
