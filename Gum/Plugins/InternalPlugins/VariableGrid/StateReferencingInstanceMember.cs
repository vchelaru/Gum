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
using WpfDataUi.Controls;
using WpfDataUi.DataTypes;

namespace Gum.PropertyGridHelpers;

/// <summary>
/// Thin WPF adapter over <see cref="VariableGridEntry"/> - the headless heir holding this class's
/// former decision logic (see ADR-0005 and the "ui-decoupling-plan.md" known-gotchas list). This
/// class still derives from WpfDataUi's <see cref="InstanceMember"/> (required so the live
/// Variables tab grid can render it) and owns the WPF-only glue that has no headless equivalent -
/// the go-to-definition <c>KeyDown</c> wiring and <see cref="HandleUiCreated"/> - while every other
/// member forwards to <see cref="_entry"/>, translating only at the WPF boundary
/// (<see cref="VariableDisplayerKind"/> &lt;-&gt; <see cref="Type"/>,
/// <see cref="VariablePropertyCommitType"/> &lt;-&gt; <see cref="SetPropertyCommitType"/>,
/// <see cref="VariableContextMenuAction"/> -&gt; <see cref="InstanceMember.ContextMenuEvents"/>).
/// </summary>
public class StateReferencingInstanceMember : InstanceMember
{
    #region Fields

    private readonly VariableGridEntry _entry;

    #endregion

    #region Properties

    public StateSaveCategory? StateSaveCategory
    {
        get => _entry.StateSaveCategory;
        set => _entry.StateSaveCategory = value;
    }

    public StateSave StateSave => _entry.StateSave;

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
    /// back to the WPF control <see cref="Type"/> the live grid needs. An explicit override is
    /// passed through unchanged (it's already the concrete WPF control type); only the "no explicit
    /// override" case needs mapping from the neutral <see cref="VariableDisplayerKind"/>.
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
                VariableDisplayerKind.ComboBox => typeof(ComboBoxDisplay),
                VariableDisplayerKind.FileSelection => typeof(FileSelectionDisplay),
                VariableDisplayerKind.ListBox => typeof(ListBoxDisplay),
                VariableDisplayerKind.MultiLineTextBox => typeof(MultiLineTextBoxDisplay),
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

        if (isReadOnly)
        {
            // don't assign it (can't null it)
            //this.CustomSetEvent = null;
        }
        else
        {
            this.CustomSetPropertyEvent += HandleCustomSet;
            this.SetToDefault += HandleSetToDefault;
        }
        this.CustomGetEvent += HandleCustomGet;
        this.CustomGetTypeEvent += HandleCustomGetType;

        this.UiCreated += HandleUiCreated;

        this.Instance = _entry.Instance;
        this.DisplayName = _entry.DisplayName;
        this.DetailText = _entry.DetailText;
        this.ToolTipText = _entry.ToolTipText;
        this.SupportsMakeDefault = _entry.SupportsMakeDefault;

        foreach (var kvp in _entry.PropertiesToSetOnDisplayer)
        {
            this.PropertiesToSetOnDisplayer[kvp.Key] = kvp.Value;
        }

        PopulateContextMenu();
    }

    private void HandleUiCreated(System.Windows.Controls.UserControl obj)
    {
        if (RootVariableName == "VariableReferences" && obj is StringListTextBoxDisplay asTextBox)
        {
            asTextBox.KeyDown += (s, e) =>
            {
                _entry.HandleReferenceTextEditKeyDown(e.ToGumKeyEventArgs(), asTextBox.GetCurrentLineText());
            };
        }
    }

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
