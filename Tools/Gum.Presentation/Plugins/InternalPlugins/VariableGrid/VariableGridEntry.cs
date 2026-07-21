using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Input;
using Gum.Managers;
using Gum.PropertyGridHelpers.Converters;
using Gum.Reflection;
using Gum.Services;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Wireframe;
using GumRuntime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using ToolsUtilities;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <summary>
/// Headless heir to <c>StateReferencingInstanceMember</c>'s decision logic - decides how a single
/// variable row in the Variables tab reads, writes, resets to default, and offers context-menu
/// actions. Deliberately does NOT derive from WpfDataUi's <c>InstanceMember</c> (a WPF-only base
/// type); a WPF-side adapter (added in a follow-up PR) is expected to wrap an instance of this class
/// and forward to/from the real <c>InstanceMember</c> surface. See ADR-0005 and the
/// "ui-decoupling-plan.md" known-gotchas list.
/// </summary>
public class VariableGridEntry
{
    #region Fields

    private readonly ISelectedState _selectedState;
    private readonly IUndoManager _undoManager;
    private readonly IGuiCommands _guiCommands;
    private readonly IFileCommands _fileCommands;
    private readonly ISetVariableLogic _setVariableLogic;
    private readonly IWireframeObjectManager _wireframeObjectManager;
    private readonly IPluginManager _pluginManager;
    private readonly IHotkeyManager _hotkeyManager;
    private readonly IDeleteVariableService _deleteVariableService;
    private readonly IExposeVariableService _exposeVariableService;
    private readonly IEditVariableService _editVariableService;
    private readonly ITypeManager _typeManager;
    private readonly IClipboardService _clipboardService;

    private readonly IStateContainer _stateListCategoryContainer;
    private readonly StateSave _stateSave;
    private readonly string _variableName;
    private readonly bool _isVariable;
    private readonly bool _isReadOnlyFromDescriptor;
    private readonly Attribute[] _attributes;
    private readonly TypeConverter? _converter;
    private readonly Type? _componentType;

    #endregion

    #region Properties

    /// <summary>The category this variable belongs to, if any (drives the "part of a category" reset/remove rules).</summary>
    public StateSaveCategory? StateSaveCategory { get; set; }

    /// <summary>The state this entry reads/writes non-recursively (see <see cref="GetValue"/>).</summary>
    public StateSave StateSave => _stateSave;

    /// <summary>The selected instance this entry belongs to, or null when it belongs to the element/behavior itself.</summary>
    public InstanceSave? InstanceSave { get; }

    /// <summary>The element this entry belongs to (directly, or via <see cref="InstanceSave"/>'s container).</summary>
    public ElementSave? ElementSave { get; }

    /// <summary>The (possibly dotted, instance-qualified) variable name, e.g. "SpriteInstance.X".</summary>
    public string Name => _variableName;

    /// <summary>The container this variable is currently read from/written to (an instance, element, or behavior).</summary>
    public object? Instance { get; }

    /// <summary>The unqualified variable name - the portion of <see cref="Name"/> after the last dot, if any.</summary>
    public string RootVariableName
    {
        get
        {
            if (_variableName.Contains('.'))
            {
                return _variableName.Substring(_variableName.LastIndexOf('.') + 1);
            }
            return _variableName;
        }
    }

    /// <summary>The user-facing row label (defaults to a space-inserted <see cref="RootVariableName"/>).</summary>
    public string DisplayName { get; set; }

    /// <summary>Help text shown under the row in the Variables tab.</summary>
    public string? DetailText { get; set; }

    /// <summary>Tooltip text shown for the row.</summary>
    public string? ToolTipText { get; set; }

    /// <summary>Desired sort order among sibling rows (lower sorts first). Defaults to <see cref="int.MaxValue"/> (unsorted/last).</summary>
    public int SortValue { get; set; } = int.MaxValue;

    /// <summary>Whether the row's context menu should offer "Make Default" (false for the Name variable and reference-assigned rows).</summary>
    public bool SupportsMakeDefault { get; }

    // Prior to April 10 2023 this was always true. Now that we have multi-select, we don't want to
    // call it here if editing multiple objects. Instead, we want to have the multi-select call it and
    // pass the list of variables so that a single undo can be performed.
    /// <summary>Whether <see cref="SetValue"/>/<see cref="ResetToDefault"/> should refresh/record undo immediately, or defer to a multi-select caller.</summary>
    public bool IsCallingRefresh { get; set; } = true;

    /// <summary>The value prior to the most recent full (non-intermediate) commit, used when reacting to the change.</summary>
    public object? LastOldFullCommitValue { get; private set; }

    /// <summary>
    /// Optional fallback consulted by <see cref="GetValue"/> when neither the selected state nor any
    /// inherited state has a value for this variable. Used by the behavior-FormsProperty surfacing
    /// path so a declared default (e.g. <c>IsEnabled = true</c>) appears in the grid without writing
    /// into state.
    /// </summary>
    public Func<object?>? DefaultValueFallback { get; set; }

    /// <summary>Named property overrides a view-side displayer should reflectively apply after creating its control (e.g. "IsEditable").</summary>
    public Dictionary<string, object> PropertiesToSetOnDisplayer { get; } = new();

    /// <summary>
    /// Optional hook a headless caller can set so a live "Value changed" notification recomputes
    /// <see cref="DetailText"/> (e.g. the XUnits/YUnits parent-sizing hint, which depends on the
    /// current value rather than only on construction-time state). Consumed generically by the WPF
    /// adapter, which re-reads <see cref="DetailText"/> after invoking this.
    /// </summary>
    public Action? RecomputeDetailTextOnValueChanged { get; set; }

    /// <summary>Whether this row should reject edits (a locked instance's other variables, or a descriptor-read-only variable).</summary>
    public bool IsReadOnly
    {
        get
        {
            if (InstanceSave?.Locked == true && RootVariableName != "Locked")
            {
                return true;
            }
            if (_isVariable)
            {
                return _isReadOnlyFromDescriptor;
            }
            return false;
        }
    }

    /// <summary>Whether the variable is absent from the selected state (i.e. inheriting its value rather than authoring one explicitly).</summary>
    public bool IsDefault
    {
        get
        {
            if (RootVariableName == "Name")
            {
                // this can never be default, and if it is that causes all kinds of weirdness in variable displays.
                return false;
            }
            return GetValueStrictlyOnSelectedState() == null;
        }
    }

    /// <summary>
    /// The raw <see cref="Type"/> override flowing from the variable's data-model
    /// <c>VariableSave.PreferredDisplayer</c> (a WPF control <see cref="Type"/> set by
    /// <c>StandardElementsManager</c>). Held opaquely here - see <see cref="PreferredDisplayerKind"/>
    /// for the neutral classification a headless caller should use instead.
    /// </summary>
    public Type? PreferredDisplayerOverride { get; set; }

    /// <summary>
    /// Explicit kind a headless caller can force when it wants a specific displayer but has no
    /// concrete WPF control <see cref="Type"/> to reference directly (e.g. <c>ElementSaveDisplayer</c>'s
    /// variable-list default of "ListBox"). Checked before the <see cref="PreferredDisplayerOverride"/>/
    /// <see cref="CustomOptions"/>-derived classification below.
    /// </summary>
    public VariableDisplayerKind? PreferredDisplayerKindOverride { get; set; }

    public VariableDisplayerKind PreferredDisplayerKind
    {
        get
        {
            if (PreferredDisplayerKindOverride != null)
            {
                return PreferredDisplayerKindOverride.Value;
            }

            bool shouldBeComboBox = false;
            // we want to still give priority to the base displayer since
            // we may want to replace combo boxes with something like toggles:
            if (CustomOptions != null && PreferredDisplayerOverride == null)
            {
                shouldBeComboBox =
                    CustomOptions.Count != 0 ||
                    // If this is a state, still show the combo box even if there are no
                    // available states to select from. Otherwise it's a confusing text box
                    _converter is AvailableStatesConverter;
            }

            if (shouldBeComboBox)
            {
                return VariableDisplayerKind.ComboBox;
            }
            if (IsFile && PreferredDisplayerOverride == null)
            {
                return VariableDisplayerKind.FileSelection;
            }
            return MapTypeToKind(PreferredDisplayerOverride);
        }
    }

    /// <summary>The converter's standard values list, when a non-boolean <see cref="TypeConverter"/> supplies one (drives the combo-box classification in <see cref="PreferredDisplayerKind"/>).</summary>
    public IList<object>? CustomOptions
    {
        get
        {
            if (_converter != null && _converter is not BooleanConverter)
            {
                var values = _converter.GetStandardValues(null);
                if (values != null)
                {
                    var toReturn = new List<object>();
                    foreach (var item in values)
                    {
                        toReturn.Add(item);
                    }
                    return toReturn;
                }
            }
            return null;
        }
    }

    private bool IsFile
    {
        get
        {
            if (_attributes != null)
            {
                foreach (var attribute in _attributes)
                {
                    if (attribute is EditorAttribute editorAttribute)
                    {
                        return editorAttribute.EditorTypeName.StartsWith("System.Windows.Forms.Design.FileNameEditor");
                    }
                }
            }
            return false;
        }
    }

    private VariableSave? VariableSave =>
        _stateSave?.Variables.FirstOrDefault(item => item.Name == _variableName || item.ExposedAsName == _variableName);

    #endregion

    #region Constructor

    /// <summary>
    /// Same constructor dependency closure as <c>StateReferencingInstanceMember</c>: the row's own
    /// data (variable name/state/instance/container) plus the services its decision logic delegates
    /// to (undo, save, plugin notification, expose/delete/edit variable, hotkeys, type resolution,
    /// clipboard).
    /// </summary>
    public VariableGridEntry(
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
        IClipboardService clipboardService)
    {
        _selectedState = selectedState;
        _undoManager = undoManager;
        _guiCommands = guiCommands;
        _fileCommands = fileCommands;
        _setVariableLogic = setVariableLogic;
        _wireframeObjectManager = wireframeObjectManager;
        _pluginManager = pluginManager;
        _hotkeyManager = hotkeyManager;
        _deleteVariableService = deleteVariableService;
        _exposeVariableService = exposeVariableService;
        _editVariableService = editVariableService;
        _typeManager = typeManager;
        _clipboardService = clipboardService;

        _stateListCategoryContainer = stateListCategoryContainer;
        _stateSave = stateSave;
        _variableName = variableName;
        _attributes = attributes;
        _converter = converter;
        _componentType = componentType;
        _isReadOnlyFromDescriptor = isReadOnly;
        _isVariable = isVariable;

        StateSaveCategory = stateSaveCategory;
        InstanceSave = instanceSave;
        ElementSave = stateListCategoryContainer as ElementSave;

        Instance = instanceSave != null ? instanceSave : stateListCategoryContainer;

        SupportsMakeDefault = variableName != "Name" && !isAssignedByReference;

        var alreadyHasSpaces = RootVariableName.Contains(' ');
        DisplayName = alreadyHasSpaces
            ? RootVariableName
            : StringFunctions.InsertSpacesInCamelCaseString(RootVariableName);

        VariableSave? standardVariable = null;
        var elementSave = stateListCategoryContainer as ElementSave;

        if (elementSave != null)
        {
            standardVariable = ObjectFinder.Self.GetRootVariable(_variableName, elementSave);
        }

        if (RootVariableName == "BaseType" && instanceSave != null && ObjectFinder.Self.GetElementSave(instanceSave) == null)
        {
            // special case - if it's a base type and we have an instance,
            // and if that instance references an invalid type, let's make
            // this editable so the user can see their current value:
            PropertiesToSetOnDisplayer["IsEditable"] = true;
        }

        // todo - this needs to go to the standard elements manager
        if (standardVariable != null)
        {
            var standardElement = ObjectFinder.Self.GetContainerOf(standardVariable);

            if (standardElement is StandardElementSave)
            {
                try
                {
                    var defaultState = StandardElementsManager.Self.GetDefaultStateFor(standardElement.Name);
                    var definingVariable = defaultState?.Variables.FirstOrDefault(item => item.Name == standardVariable.Name);

                    if (definingVariable != null)
                    {
                        if (definingVariable.PreferredDisplayer != null)
                        {
                            PreferredDisplayerOverride = definingVariable.PreferredDisplayer;
                        }
                        DetailText = definingVariable.DetailText;
                        ToolTipText = definingVariable.ToolTipText;

                        foreach (var kvp in definingVariable.PropertiesToSetOnDisplayer)
                        {
                            PropertiesToSetOnDisplayer[kvp.Key] = kvp.Value;
                        }

                        SortValue = definingVariable.DesiredOrder;
                    }
                }
                catch (Exception e)
                {
                    // this could be a missing standard element save, print output but tolerate it:
                    _guiCommands.PrintOutput("Error getting standard element variable:\n" + e);
                }
            }
        }
        else
        {
            VariableListSave? standardVariableList = null;
            if (elementSave != null)
            {
                standardVariableList = ObjectFinder.Self.GetRootVariableList(_variableName, elementSave);
            }
            ElementSave? standardElement = null;

            if (standardVariableList != null)
            {
                standardElement = ObjectFinder.Self.GetElementContainerOf(standardVariableList);
            }

            VariableListSave? definingVariableList = null;
            if (standardElement is StandardElementSave)
            {
                var defaultState = StandardElementsManager.Self.GetDefaultStateFor(standardElement.Name);
                definingVariableList = defaultState?.VariableLists.FirstOrDefault(item => item.Name == standardVariableList!.Name);
            }

            // Fallback: when the container is a Screen or Component (not a StandardElementSave),
            // search all standard elements for a matching variable list definition (e.g. VariableReferences).
            if (definingVariableList == null && standardVariableList != null)
            {
                var rootName = standardVariableList.GetRootName();
                foreach (var se in ObjectFinder.Self.GumProjectSave?.StandardElements ?? Enumerable.Empty<StandardElementSave>())
                {
                    var defaultState = StandardElementsManager.Self.GetDefaultStateFor(se.Name);
                    var match = defaultState?.VariableLists.FirstOrDefault(vl => vl.GetRootName() == rootName);
                    if (match != null)
                    {
                        definingVariableList = match;
                        break;
                    }
                }
            }

            if (definingVariableList != null)
            {
                if (definingVariableList.PreferredDisplayer != null)
                {
                    PreferredDisplayerOverride = definingVariableList.PreferredDisplayer;
                }
                DetailText = definingVariableList.DetailText;
            }
        }
    }

    #endregion

    private static VariableDisplayerKind MapTypeToKind(Type? type)
    {
        if (type == null)
        {
            return VariableDisplayerKind.Default;
        }

        return type.FullName switch
        {
            "WpfDataUi.Controls.ComboBoxDisplay" => VariableDisplayerKind.ComboBox,
            "WpfDataUi.Controls.FileSelectionDisplay" => VariableDisplayerKind.FileSelection,
            "WpfDataUi.Controls.ListBoxDisplay" => VariableDisplayerKind.ListBox,
            "WpfDataUi.Controls.MultiLineTextBoxDisplay" => VariableDisplayerKind.MultiLineTextBox,
            _ => VariableDisplayerKind.Default
        };
    }

    private object? GetValueStrictlyOnSelectedState() => _stateSave?.GetValue(_variableName);

    /// <summary>
    /// Headless replacement for <c>ElementSaveExtensionMethodsGumTool.GetVariableFromThisOrBase</c>
    /// (which resolves <c>ISelectedState</c> via <c>Locator</c> instead of injection): same
    /// "use the selected state when this element is the one currently selected, else its default
    /// state" rule, resolved from the already-injected <see cref="_selectedState"/>.
    /// </summary>
    private VariableSave? GetVariableFromThisOrBase(ElementSave element, string variable)
    {
        var stateToPullFrom = element == _selectedState.SelectedElement && _selectedState.SelectedStateSave != null
            ? _selectedState.SelectedStateSave
            : element.DefaultState;

        return stateToPullFrom.GetVariableRecursive(variable);
    }

    /// <summary>See <see cref="GetVariableFromThisOrBase(ElementSave, string)"/> - the VariableListSave equivalent.</summary>
    private VariableListSave? GetVariableListFromThisOrBase(ElementSave element, string variable)
    {
        var stateToPullFrom = element == _selectedState.SelectedElement && _selectedState.SelectedStateSave != null
            ? _selectedState.SelectedStateSave
            : element.DefaultState;

        return stateToPullFrom.GetVariableListRecursive(variable);
    }

    #region Get Value

    /// <summary>
    /// Reads the variable's current value, mirroring <c>StateReferencingInstanceMember.HandleCustomGet</c>:
    /// special-cases Name/BaseType, then falls back through the selected state, the recursive
    /// (inherited) state, and finally <see cref="DefaultValueFallback"/>.
    /// </summary>
    /// <param name="instance">The element/instance this value is being read for.</param>
    public object? GetValue(object instance)
    {
        if (RootVariableName == "Name")
        {
            if (instance is InstanceSave asInstanceSave)
            {
                return asInstanceSave.Name;
            }
            if (instance is ElementSave elementSave)
            {
                // strip off prefixed folder name
                if (elementSave.Name.Contains('/'))
                {
                    int slashIndex = elementSave.Name.LastIndexOf('/');
                    return elementSave.Name.Substring(slashIndex + 1);
                }
                return elementSave.Name;
            }
        }

        // don't else-if it, if name doesn't handle it, keep going:
        if (RootVariableName == "BaseType" && instance is InstanceSave asInstanceForBehavior)
        {
            return asInstanceForBehavior.BaseType;
        }

        if (_isVariable)
        {
            var toReturn = GetValueStrictlyOnSelectedState();

            if (toReturn == null)
            {
                var effectiveVariableName = VariableSave?.Name ?? _variableName;

                if (_stateSave != null)
                {
                    toReturn = _stateSave.GetValueRecursive(effectiveVariableName);
                }
            }

            // Final tier: behavior-promoted FormsProperty defaults. When neither the
            // selected state nor any inherited state authors a value, fall back to the
            // declared FormsProperty.Value so the grid reflects the implicit default
            // (e.g. IsEnabled = true) without polluting state. IsDefault remains true
            // because nothing is explicitly authored.
            if (toReturn == null)
            {
                toReturn = DefaultValueFallback?.Invoke();
            }

            return toReturn;
        }
        else
        {
            var effectiveVariableName = VariableSave?.Name ?? _variableName;
            return _stateSave.GetValueRecursive(effectiveVariableName);
        }
    }

    #endregion

    #region Set Value

    /// <summary>
    /// Writes a new value for this variable, mirroring <c>StateReferencingInstanceMember.HandleCustomSet</c>:
    /// special-cases Name/BaseType, otherwise sets the value on the appropriate <see cref="Gum.DataTypes.Variables.StateSave"/>,
    /// then calls <see cref="NotifyVariableLogic"/> to react to the change.
    /// </summary>
    /// <param name="gumElementOrInstanceSaveAsObject">The element/instance the value is being set on.</param>
    /// <param name="newValue">The value to assign.</param>
    /// <param name="commitType">Whether this is a final commit or an in-progress edit.</param>
    /// <returns>A response whose <c>Succeeded</c> is false when the assignment was rejected/cancelled.</returns>
    public GeneralResponse SetValue(object gumElementOrInstanceSaveAsObject, object? newValue, VariablePropertyCommitType commitType)
    {
        ////////////////////Early Out/////////////////////////
        if (!CanSetValue(newValue))
        {
            return GeneralResponse.UnsuccessfulResponse;
        }
        /////////////////End Early Out////////////////////////

        var stateSave = _selectedState.SelectedStateSave;
        var instanceSave = gumElementOrInstanceSaveAsObject as InstanceSave;
        var elementSave = instanceSave?.ParentContainer ?? gumElementOrInstanceSaveAsObject as ElementSave;

        if (_isVariable)
        {
            StoreLastOldValue(gumElementOrInstanceSaveAsObject, commitType, instanceSave, elementSave);

            // <None> is a reserved value for when we want to allow the user to reset a value through a
            // combo box. If the value is "<None>" then let's set it to null:
            if (newValue is "<None>")
            {
                newValue = null;
            }

            // the stateSave.SetValue method handles Name and Base Type internally just fine,
            // but if we are on an instance in a behavior, that won't have a state save, so let's do that out here:
            // This is a variable on an instance
            if (instanceSave != null && RootVariableName == "Name")
            {
                if (newValue is string newValueAsString)
                {
                    instanceSave.Name = newValueAsString;
                }
            }
            else if (elementSave != null && RootVariableName == "Name")
            {
                if (newValue is string newValueAsString)
                {
                    if (elementSave.Name.Contains('/'))
                    {
                        var slash = elementSave.Name.LastIndexOf('/');
                        var folder = elementSave.Name.Substring(0, slash + 1);

                        elementSave.Name = folder + newValueAsString;

                        // so that later logic can use the qualified name:
                        newValue = elementSave.Name;
                    }
                    else
                    {
                        elementSave.Name = newValueAsString;
                    }
                }
            }
            else if (instanceSave != null && RootVariableName == "BaseType")
            {
                instanceSave.BaseType = newValue?.ToString();
            }
            else if (stateSave != null && elementSave != null)
            {
                // If we are creating a new variable, we need to make sure it carries the same exposed
                // name as the variable in base that defines it. We need to first get that variable...
                VariableSave? variableDefinedInThisOrBase = null;
                var existingVariable = GetVariableFromThisOrBase(elementSave, Name);
                if (!string.IsNullOrEmpty(existingVariable?.ExposedAsName))
                {
                    variableDefinedInThisOrBase = GetVariableDefinedInThisOrBase(existingVariable);
                }
                string? variableType = existingVariable?.Type ?? GetVariableFromThisOrBase(elementSave, Name)?.Type;
                if (string.IsNullOrEmpty(variableType))
                {
                    var rootVariable = ObjectFinder.Self.GetRootVariable(Name, elementSave);
                    variableType = rootVariable?.Type;
                }
                // ...set variable after getting it from base, or else we'd get the variable we just set...
                stateSave.SetValue(Name, newValue, instanceSave, variableType);
                if (!string.IsNullOrEmpty(existingVariable?.ExposedAsName) && variableDefinedInThisOrBase != null)
                {
                    //... then assign it here if we found it:
                    var variable = stateSave.GetVariableSave(Name);
                    variable.ExposedAsName = existingVariable.ExposedAsName;
                }
            }

            return NotifyVariableLogic(gumElementOrInstanceSaveAsObject, commitType, trySave: commitType == VariablePropertyCommitType.Full);
        }
        else
        {
            StoreLastOldValue(gumElementOrInstanceSaveAsObject, commitType, instanceSave, elementSave);

            var existingVariableList = elementSave != null ? GetVariableListFromThisOrBase(elementSave, Name) : null;
            var type = existingVariableList?.Type ?? TryGetTypeFromVariableListSave()?.ToString();
            _stateSave.SetValue(_variableName, newValue, instanceSave, type);

            return NotifyVariableLogic(gumElementOrInstanceSaveAsObject, commitType, trySave: commitType == VariablePropertyCommitType.Full);
        }
    }

    private void StoreLastOldValue(object gumElementOrInstanceSaveAsObject, VariablePropertyCommitType commitType, InstanceSave? instanceSave, ElementSave? elementSave)
    {
        object? oldValue = GetValue(gumElementOrInstanceSaveAsObject);

        if (elementSave != null && instanceSave == null && RootVariableName == "Name")
        {
            // We want to treat the old value as having folder because
            // we display without the folder, but the actual name includes it:
            oldValue = elementSave.Name;
        }

        if (commitType == VariablePropertyCommitType.Full)
        {
            LastOldFullCommitValue = oldValue;

            // if the value that changed was a list, we want to store off a copy of it or else the modified
            // list will simply add to the existing list and later checks for equality will always return true:
            if (oldValue is IList oldList)
            {
                var newList = (IList)Activator.CreateInstance(oldList.GetType())!;

                foreach (var item in oldList)
                {
                    newList.Add(item);
                }
                LastOldFullCommitValue = newList;
            }
        }
    }

    private bool CanSetValue(object? newValue)
    {
        if (RootVariableName == "Points")
        {
            var value = newValue as List<System.Numerics.Vector2>;

            if (value?.Count < 4)
            {
                return false;
            }
        }

        return true;
    }

    #endregion

    #region Set to default

    /// <summary>
    /// Resets the variable to its default (usually removing it so an inherited/base value takes
    /// over), mirroring <c>StateReferencingInstanceMember.HandleSetToDefault</c>'s reset/remove
    /// rules.
    /// </summary>
    public void ResetToDefault()
    {
        string variableName = Name;

        bool shouldReset = false;
        bool affectsTreeView = false;

        var selectedElement = _selectedState.SelectedElement;
        var selectedInstance = _selectedState.SelectedInstance;

        if (selectedInstance != null)
        {
            affectsTreeView = variableName == "Parent";
            shouldReset = true;
        }
        else if (selectedElement != null)
        {
            shouldReset =
                // Don't let the user reset standard element variables, they have to have some actual value
                (selectedElement is StandardElementSave) == false ||
                // ... unless it's not the default
                _selectedState.SelectedStateSave != selectedElement.DefaultState;
        }

        // now we reset, but we don't remove the variable:
        StateSave? state = _selectedState.SelectedStateSave;
        VariableSave? variable = state?.GetVariableSave(variableName);
        var oldValue = variable?.Value;
        LastOldFullCommitValue = oldValue;

        bool wasChangeMade = false;
        if (shouldReset)
        {
            bool isPartOfCategory = StateSaveCategory != null;

            if (variable != null)
            {
                // Don't remove the variable if it's part of an element - we still want it there
                // so it can be set, we just don't want it to set a value. Also, don't remove it if
                // it's an exposed variable, this un-exposes things.
                bool shouldRemove = string.IsNullOrEmpty(variable.ExposedAsName) && !isPartOfCategory && !variable.IsCustomVariable;

                // Any variable can be removed so long as the current state isn't the "base definition"
                // for it. For elements, no variables are the base variable definitions except for
                // variables that are categorized state variables for categories defined in this element.
                if (shouldRemove)
                {
                    var isState = variable.IsState(selectedElement, out ElementSave categoryContainer, out StateSaveCategory categoryForVariable);

                    if (isState)
                    {
                        var isDefinedHere = categoryForVariable != null && categoryContainer == selectedElement;

                        shouldRemove = !isDefinedHere;
                    }
                }

                if (shouldRemove)
                {
                    state!.Variables.Remove(variable);
                }
                else if (isPartOfCategory)
                {
                    var variableInDefault = selectedElement!.DefaultState.GetVariableSave(variable.Name);
                    if (variableInDefault != null)
                    {
                        _guiCommands.PrintOutput(
                            $"The variable {variable.Name} is part of the category {StateSaveCategory!.Name} so it cannot be removed. Instead, the value has been set to the value in the default state");

                        variable.Value = variableInDefault.Value;
                    }
                    else
                    {
                        // If it's a state, we can un-set that back to null, that's okay:
                        if (variable.IsState(selectedElement))
                        {
                            variable.Value = null;
                            variable.SetsValue = true;
                        }
                        else
                        {
                            _guiCommands.PrintOutput("Could not set value to default because the default state doesn't set this value");
                        }
                    }
                }
                else
                {
                    variable.Value = null;
                    variable.SetsValue = false;
                }

                wasChangeMade = true;
                // We need to refresh the property grid and the wireframe display
            }
            else if ((DisplayName == "BaseType" || DisplayName == "Base Type") && ElementSave != null)
            {
                ElementSave.BaseType = null;
                wasChangeMade = true;
            }
            else
            {
                // Maybe this is a variable list?
                VariableListSave? variableList = state?.GetVariableListSave(variableName);
                if (variableList != null)
                {
                    state!.VariableLists.Remove(variableList);
                    wasChangeMade = true;
                }
            }

            if (selectedElement != null)
            {
                ElementSaveExtensions.ApplyVariableReferences(selectedElement, state);
            }
        }

        if (wasChangeMade)
        {
            _undoManager.RecordUndo();
            _guiCommands.RefreshVariables(force: true);
            _wireframeObjectManager.RefreshAll(true);

            _pluginManager.VariableSet(selectedElement, selectedInstance, variableName, oldValue);

            if (affectsTreeView)
            {
                _guiCommands.RefreshElementTreeView(_selectedState.SelectedElement);
            }

            _fileCommands.TryAutoSaveElement(_selectedState.SelectedElement);
        }

        NotifyVariableLogic(Instance!, VariablePropertyCommitType.Full, trySave: true);
    }

    #endregion

    private VariableSave? GetVariableDefinedInThisOrBase(VariableSave? existingVariable)
    {
        VariableSave? variableDefinedInThisOrBase = null;

        if (!string.IsNullOrEmpty(existingVariable?.ExposedAsName))
        {
            // need to set the exposed name on the variable, but only if it is defined in this component or in
            // a base component:
            variableDefinedInThisOrBase = _selectedState.SelectedStateSave!.GetVariableSave(Name);
            if (variableDefinedInThisOrBase == null && _selectedState.SelectedStateSave != _selectedState.SelectedElement!.DefaultState)
            {
                variableDefinedInThisOrBase = _selectedState.SelectedElement.DefaultState.GetVariableSave(Name);
            }

            if (variableDefinedInThisOrBase == null)
            {
                var allBase = ObjectFinder.Self.GetBaseElements(_selectedState.SelectedElement);
                foreach (var baseElement in allBase)
                {
                    variableDefinedInThisOrBase = baseElement.DefaultState.GetVariableSave(Name);
                    if (variableDefinedInThisOrBase != null)
                    {
                        break;
                    }
                }
            }
        }

        return variableDefinedInThisOrBase;
    }

    /// <summary>
    /// Reacts to a variable value having changed - resolving exposed/tunneled variables, deciding
    /// which state to react against, and delegating to <see cref="ISetVariableLogic"/>. Called by
    /// <see cref="SetValue"/> and <see cref="ResetToDefault"/>.
    /// </summary>
    /// <param name="gumElementOrInstanceSaveAsObject">The element/instance the change applies to.</param>
    /// <param name="commitType">Whether this is a final commit or an in-progress edit.</param>
    /// <param name="trySave">Whether the change should attempt to auto-save/record undo.</param>
    public GeneralResponse NotifyVariableLogic(object gumElementOrInstanceSaveAsObject, VariablePropertyCommitType commitType, bool trySave = true)
    {
        GeneralResponse response = GeneralResponse.SuccessfulResponse;

        string name = RootVariableName;

        bool handledByExposedVariable = false;

        bool effectiveRefresh = commitType == VariablePropertyCommitType.Full || IsCallingRefresh;

        bool effectiveRecordUndo = IsCallingRefresh && trySave;

        // This might be a tunneled variable, and we want to react to the
        // change using the underlying variable if so:
        if (gumElementOrInstanceSaveAsObject is ElementSave elementSave)
        {
            var variable = elementSave.DefaultState.Variables
                .FirstOrDefault(item => item.ExposedAsName == RootVariableName);
            if (variable != null)
            {
                var sourceObjectName = variable.SourceObject;

                name = variable.Name;
                var instanceInElement = elementSave.Instances
                    .FirstOrDefault(item => item.Name == sourceObjectName);

                if (instanceInElement != null)
                {
                    handledByExposedVariable = true;

                    response = _setVariableLogic.ReactToPropertyValueChanged(variable.GetRootName(), LastOldFullCommitValue, elementSave, instanceInElement, StateSave, refresh: effectiveRefresh, recordUndo: effectiveRecordUndo, isFullCommit: commitType == VariablePropertyCommitType.Full);
                }
            }
        }

        if (!handledByExposedVariable)
        {
            var element = gumElementOrInstanceSaveAsObject as ElementSave ??
                (gumElementOrInstanceSaveAsObject as InstanceSave)?.ParentContainer;

            StateSave? stateToSet;

            // If we are viewing the current element, then use the selected state so we can set
            // values on categorized states...
            if (_selectedState.SelectedElement == element && _selectedState.SelectedStateSave != null)
            {
                stateToSet = _selectedState.SelectedStateSave;
            }
            // ... otherwise, we may not be viewing the current element, like if we are doing a drag+drop...
            else if (element != null)
            {
                stateToSet = element.DefaultState;
            }
            // ... otherwise, there is no valid state so don't do anything:
            else
            {
                stateToSet = null;
            }

            if (stateToSet != null)
            {
                response = _setVariableLogic.PropertyValueChanged(
                    name,
                    LastOldFullCommitValue,
                    gumElementOrInstanceSaveAsObject as InstanceSave,
                    stateToSet,
                    refresh: effectiveRefresh,
                    recordUndo: effectiveRecordUndo,
                    trySave: trySave,
                    isFullCommit: commitType == VariablePropertyCommitType.Full);
            }
            else if (_selectedState.SelectedBehavior != null &&
                     gumElementOrInstanceSaveAsObject is BehaviorInstanceSave behaviorInstance)
            {
                response = _setVariableLogic.PropertyValueChangedOnBehaviorInstance(
                    name,
                    LastOldFullCommitValue,
                    _selectedState.SelectedBehavior,
                    behaviorInstance);
            }
        }

        return response;
    }

    #region Type resolution

    /// <summary>
    /// Resolves the CLR type this variable should be edited/displayed as, mirroring
    /// <c>StateReferencingInstanceMember.HandleCustomGetType</c>.
    /// </summary>
    /// <param name="instance">Unused - kept for parity with the value-getter/setter signatures.</param>
    public Type? GetValueType(object instance)
    {
        if (_componentType != null)
        {
            return _componentType;
        }

        return GetTypeFromVariableRecursively();
    }

    private Type? GetTypeFromVariableRecursively()
    {
        VariableSave? variableSave = GetRootVariableSave();

        if (variableSave?.Type != null)
        {
            return _typeManager.GetTypeFromString(variableSave.Type);
        }

        return TryGetTypeFromVariableListSave() ?? typeof(string);
    }

    // Vic asks - why does this exist? Why aren't we using ObjectFinder?
    /// <summary>Finds the <see cref="VariableSave"/> that defines this row's type/metadata (walking to the instance's/element's base as needed).</summary>
    public VariableSave? GetRootVariableSave()
    {
        VariableSave? variableSave;

        if (InstanceSave != null)
        {
            if (InstanceSave.ParentContainer == null)
            {
                // this is an instance in a behavior
                var elementBaseType = ObjectFinder.Self.GetElementSave(InstanceSave);

                variableSave = elementBaseType != null ? GetVariableFromThisOrBase(elementBaseType, RootVariableName) : null;
            }
            else
            {
                variableSave = InstanceSave.GetVariableFromThisOrBase(
                    new ElementWithState(InstanceSave.ParentContainer), RootVariableName);
            }
        }
        else
        {
            variableSave = ElementSave != null ? GetVariableFromThisOrBase(ElementSave, RootVariableName) : null;
        }

        return variableSave;
    }

    /// <summary>Resolves this row's element type as a <c>List&lt;T&gt;</c>, when it's a variable list rather than a scalar variable.</summary>
    public Type? TryGetTypeFromVariableListSave()
    {
        string? typeName;

        if (InstanceSave != null)
        {
            var variableList = InstanceSave.GetVariableListFromThisOrBase(
                InstanceSave.ParentContainer, RootVariableName);

            typeName = variableList?.Type;
        }
        else
        {
            typeName = ElementSave != null ? GetVariableListFromThisOrBase(ElementSave, RootVariableName)?.Type : null;
        }

        if (!string.IsNullOrEmpty(typeName))
        {
            return _typeManager.GetTypeFromString($"List<{typeName}>");
        }

        return null;
    }

    #endregion

    #region Context Menu

    /// <summary>
    /// Builds the current set of right-click actions for this row, mirroring
    /// <c>StateReferencingInstanceMember.ModifyContextMenu</c>. Called on demand rather than cached,
    /// since the available actions can change as the underlying variable/expose state changes.
    /// </summary>
    public List<VariableContextMenuAction> BuildContextMenuActions()
    {
        var actions = new List<VariableContextMenuAction>();

        TryAddExposeVariableMenuOptions(actions, InstanceSave);
        TryAddHideFromInstancesMenuOptions(actions, InstanceSave, _stateListCategoryContainer);
        TryAddCopyVariableReferenceMenuOptions(actions);

        // Replaces StateReferencingInstanceMember's call into IEditVariableMenuService (which takes a
        // WPF-typed InstanceMember and can't be called from headless code) with the already-headless
        // IEditVariableService split-interface counterpart (see the ui-decoupling-plan.md
        // IEditVariableService/WpfDataUi split gotcha).
        if (_editVariableService.GetEditVariableMenuLabel(VariableSave, _stateListCategoryContainer) is { } label)
        {
            var variableToEdit = VariableSave!;
            actions.Add(new VariableContextMenuAction(label, () =>
                _editVariableService.ShowEditVariableWindow(variableToEdit, _stateListCategoryContainer)));
        }

        return actions;
    }

    private void TryAddCopyVariableReferenceMenuOptions(List<VariableContextMenuAction> actions)
    {
        if (_variableName != "Name" && _variableName != "BaseType")
        {
            actions.Add(new VariableContextMenuAction("Copy Qualified Variable Name", () =>
            {
                if (ElementSave == null)
                {
                    return;
                }

                // No need to append the instance: mVariableName already contains the instance name when there
                // is an instance.
                var qualifiedName = ObjectFinder.Self.GetQualifiedElementName(ElementSave) + "." + _variableName;

                _clipboardService.SetText(qualifiedName);
            }));
        }

        if (VariableSave != null && _deleteVariableService.CanDeleteVariable(VariableSave))
        {
            var variableToDelete = VariableSave;
            actions.Add(new VariableContextMenuAction($"Delete Variable [{variableToDelete.Name}]", () =>
                _deleteVariableService.DeleteVariable(variableToDelete, ElementSave!)));
        }
    }

    #region Expose/Unexpose

    private void TryAddExposeVariableMenuOptions(List<VariableContextMenuAction> actions, InstanceSave? instance)
    {
        bool canExpose = false;
        bool canUnExpose = false;

        if (VariableSave != null)
        {
            if (string.IsNullOrEmpty(VariableSave.ExposedAsName))
            {
                if (instance != null)
                {
                    canExpose = true;
                }
            }
            else
            {
                canUnExpose = true;
            }
        }
        else
        {
            var rootName = Gum.DataTypes.Variables.VariableSave.GetRootName(_variableName);

            var isVariableList = false;
            if (instance?.ParentContainer != null)
            {
                var rootVariableList = ObjectFinder.Self.GetRootVariableList(_variableName, instance.ParentContainer);
                isVariableList = rootVariableList != null;
            }

            canExpose = !isVariableList && rootName != "Name" && rootName != "BaseType"
                && instance != null;
        }

        if (canExpose)
        {
            // Variable doesn't exist, so they can only expose it, not unexpose it.
            actions.Add(new VariableContextMenuAction("Expose Variable", () =>
                _exposeVariableService.HandleExposeVariableClick(_selectedState.SelectedInstance!, RootVariableName)));
        }
        if (canUnExpose)
        {
            var variableToUnexpose = VariableSave!;
            actions.Add(new VariableContextMenuAction($"Un-expose Variable {variableToUnexpose.ExposedAsName} ({variableToUnexpose.Name})", () =>
                _exposeVariableService.HandleUnexposeVariableClick(variableToUnexpose, ElementSave!)));
        }
    }

    #endregion

    #region Hide from Instances

    private void TryAddHideFromInstancesMenuOptions(List<VariableContextMenuAction> actions, InstanceSave? instanceSave, IStateContainer stateListCategoryContainer)
    {
        if (instanceSave != null)
        {
            return;
        }

        if (stateListCategoryContainer is not ComponentSave and not ScreenSave)
        {
            return;
        }

        if (RootVariableName == "Name" || RootVariableName == "BaseType")
        {
            return;
        }

        var element = stateListCategoryContainer as ElementSave;
        var rootVariableName = RootVariableName;

        if (element!.VariablesHiddenFromInstances.Contains(rootVariableName))
        {
            actions.Add(new VariableContextMenuAction("Show on Instances", () =>
            {
                using var undoLock = _undoManager.RequestLock();
                element.VariablesHiddenFromInstances.Remove(rootVariableName);
                _fileCommands.TryAutoSaveCurrentElement();
                _guiCommands.RefreshVariables(force: true);
            }));
        }
        else
        {
            actions.Add(new VariableContextMenuAction("Hide from Instances", () =>
            {
                using var undoLock = _undoManager.RequestLock();
                element.VariablesHiddenFromInstances.Add(rootVariableName);
                _fileCommands.TryAutoSaveCurrentElement();
                _guiCommands.RefreshVariables(force: true);
            }));
        }
    }

    #endregion

    #endregion

    #region Go To Definition (VariableReferences)

    /// <summary>
    /// Handles a key press inside the VariableReferences text editor, mirroring
    /// <c>StateReferencingInstanceMember.HandleUiCreated</c>'s go-to-definition wiring. Only
    /// meaningful for the VariableReferences row; navigates to the referenced element/instance when
    /// the "go to definition" hotkey is pressed on the current line's text. Takes the
    /// framework-neutral <see cref="GumKeyEventArgs"/> (rather than a WPF <c>KeyEventArgs</c>) so it
    /// can be called from a headless caller; a WPF-side adapter translates its own key event before
    /// calling this.
    /// </summary>
    public void HandleReferenceTextEditKeyDown(GumKeyEventArgs e, string currentLineText)
    {
        if (RootVariableName != "VariableReferences")
        {
            return;
        }

        if (_hotkeyManager.GoToDefinition.IsPressed(e))
        {
            TryGoToDefinition(currentLineText);
        }
    }

    private void TryGoToDefinition(string text)
    {
        if (text?.Contains("=") == true)
        {
            var rightSideOfEquals = text.Substring(text.IndexOf("=") + 1).Trim();

            if (rightSideOfEquals.Contains("."))
            {
                var beforeDot = rightSideOfEquals.Substring(0, rightSideOfEquals.IndexOf("."));

                if (beforeDot.Contains("/"))
                {
                    beforeDot = beforeDot.Substring(beforeDot.LastIndexOf("/") + 1);
                }

                var element = ObjectFinder.Self.GetElementSave(beforeDot);

                if (element != null)
                {
                    var afterDot = rightSideOfEquals.Substring(rightSideOfEquals.IndexOf(".") + 1);

                    var instanceName = afterDot;

                    if (afterDot.Contains("."))
                    {
                        instanceName = afterDot.Substring(0, afterDot.IndexOf("."));
                    }

                    InstanceSave? instance = null;
                    if (!string.IsNullOrEmpty(instanceName))
                    {
                        instance = element.GetInstance(instanceName);
                    }
                    if (instance != null)
                    {
                        _selectedState.SelectedInstance = instance;
                    }
                    else
                    {
                        _selectedState.SelectedElement = element;
                    }
                }
            }
        }
    }

    #endregion
}
