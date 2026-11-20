using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Managers;
using Gum.Mvvm;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Undo;
using System;
using System.Collections.Generic;
using System.Linq;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.ProjectServices.Editing;
using ToolsUtilities;

namespace Gum.Plugins.InternalPlugins.VariableGrid.ViewModels;

public enum RenameType
{
    NormalName,
    ExposedName
}

public class AddVariableViewModel : DialogViewModel
{
    #region Fields/Properties

    public string Title { get; set; } = "Add Variable";
    public string EnteredName
    {
        get => Get<string>();
        set => Set(value);
    }

    public string DetailText
    {
        get => Get<string>();
        set => Set(value);
    }

    public string? ErrorMessage
    {
        get => Get<string>();
        private set => Set(value);
    }

    private readonly IGuiCommands _guiCommands;
    private readonly ISelectedState _selectedState;
    private readonly IUndoManager _undoManager;
    private readonly IElementCommands _elementCommands;
    private readonly IFileCommands _fileCommands;
    private readonly INameVerifier _nameVerifier;
    private readonly IPluginManager _pluginManager;
    private readonly IRenameLogic _renameLogic;
    private readonly ICustomVariableLogic _customVariableLogic;

    public List<string> AvailableTypes
    {
        get; private set;
    }

    public string SelectedItem
    {
        get => Get<string>();
        set => Set(value);
    }

    [DependsOn(nameof(SelectedItem))]
    public object? DefaultValue => _customVariableLogic.GetDefaultValue(SelectedItem);

    public ElementSave? Element { get; set; }
    public VariableSave? Variable { get; set; }

    public RenameType RenameType { get; set; }
    public VariableChangeResponse VariableChangeResponse 
    {
        get => Get<VariableChangeResponse>();
        set => Set(value);
    }

    [DependsOn(nameof(VariableChangeResponse))]
    public string TypeChangeMessage => VariableChangeResponse?.VariableReferenceChanges.Count > 0
        ? "This variable is used in a variable reference assignment so its type cannot be changed"
        : string.Empty;

    [DependsOn(nameof(VariableChangeResponse))]
    public bool IsTypeChangeUiEnabled => (VariableChangeResponse?.VariableReferenceChanges.Count > 0) == false;

    #endregion

    public AddVariableViewModel(IGuiCommands guiCommands,
        IUndoManager undoManager,
        IElementCommands elementCommands,
        IFileCommands fileCommands,
        INameVerifier nameVerifier,
        ISelectedState selectedState,
        IPluginManager pluginManager,
        IRenameLogic renameLogic,
        ICustomVariableLogic customVariableLogic)
    {
        _guiCommands = guiCommands;
        _undoManager = undoManager;
        _elementCommands = elementCommands;
        _fileCommands = fileCommands;
        _nameVerifier = nameVerifier;
        _selectedState = selectedState;
        _pluginManager = pluginManager;
        _renameLogic = renameLogic;
        _customVariableLogic = customVariableLogic;

        AvailableTypes = new List<string>(customVariableLogic.AvailableTypes);

        SelectedItem = "float";

        PropertyChanged += (_, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(EnteredName):
                case nameof(SelectedItem):
                    RunValidation();
                    break;
                case nameof(ErrorMessage):
                    AffirmativeCommand.NotifyCanExecuteChanged();
                    break;
            }
        };
    }

    public override bool CanExecuteAffirmative() => ErrorMessage is null;

    public void RunValidation()
    {
        GeneralResponse response = Validate();
        ErrorMessage = response.Succeeded ? null : response.Message;
    }

    public override void OnAffirmative()
    {
        RunValidation();
        if (ErrorMessage != null)
        {
            return;
        }

        if (Variable is null)
        {
            AddVariableToSelectedItem();
        }
        else
        {
            DoEdit(Variable, VariableChangeResponse);
        }
        base.OnAffirmative();
    }

    public GeneralResponse Validate()
    {
        if (SelectedItem == null)
        {
            return GeneralResponse.UnsuccessfulWith("You must select a type");
        }
        if (string.IsNullOrEmpty(EnteredName))
        {
            return GeneralResponse.UnsuccessfulWith("You must enter a name");
        }

        if (!_nameVerifier.IsVariableNameValid(EnteredName, Element, Variable, out string? whyNotValid))
        {
            return GeneralResponse.UnsuccessfulWith(whyNotValid);
        }

        // If it's a new variable, then it won't already exist:
        if(Variable != null)
        {
            // it must either be exposed or a custom variable to be renamed:
            if(Variable.IsCustomVariable == false && string.IsNullOrEmpty(Variable.ExposedAsName) && Element != null)
            {
                return GeneralResponse.UnsuccessfulWith(
                    $"The variable {Variable} cannot be changed because it is not an exposed or custom variable");
            }
        }

        return GeneralResponse.SuccessfulResponse;
    }

    private void AddVariableToSelectedItem()
    {
        var type = SelectedItem;
        var name = EnteredName;

        var behavior = _selectedState.SelectedBehavior;

        using var undoLock = _undoManager.RequestLock();

        var element = _selectedState.SelectedElement;
        if (behavior != null)
        {
            behavior.RequiredVariables.Variables.Add(new VariableSave
            {
                Name = name,
                Type = type,
                Value = DefaultValue
            });
            _elementCommands.SortVariables(behavior);
            _fileCommands.TryAutoSaveBehavior(behavior);
        }
        else if (element != null)
        {
            _customVariableLogic.AddCustomVariable(element, name, type, DefaultValue);
            _fileCommands.TryAutoSaveElement(element);
        }
        _guiCommands.RefreshVariables(force: true);

        _pluginManager.VariableAdd(element, name);
    }

    private void DoEdit(VariableSave variable, Logic.VariableChangeResponse changes)
    {
        var type = SelectedItem;
        var newName = EnteredName;

        var behavior = _selectedState.SelectedBehavior;

        using var undoLock = _undoManager.RequestLock();

        if (behavior != null)
        {
            var changedType = variable.Type != type;
            if (changedType)
            {
                // todo - need to fix this by converting?
                variable.Value = null;
            }

            // Variables in behaviors cannot have expoed names, so 
            // we don't need to check the RenameType
            variable.Name = newName;

            variable.Type = type;

            _fileCommands.TryAutoSaveBehavior(behavior);
        }
        else if (_selectedState.SelectedElement != null)
        {
            DoEditsToVariableOnElement(variable, _selectedState.SelectedElement, changes, type, newName);
        }
        _guiCommands.RefreshVariables(force: true);
    }

    private void DoEditsToVariableOnElement(VariableSave variable, ElementSave element, Logic.VariableChangeResponse changes, string type, string newName)
    {
        var oldName = RenameType == RenameType.NormalName 
            ? variable.Name 
            : variable.ExposedAsName;

        // An exposed-name rename of a variable that isn't exposed has nothing to rename. Going on
        // would match every non-exposed variable, since they all have a null exposed name.
        if (oldName == null)
        {
            return;
        }

        HashSet<ElementSave> elementsToSave = new HashSet<ElementSave>();
        // If we're changing the variable on this element, we should respect whether it's
        // a full rename or just an exposed rename
        new RenameReferenceApplier().RenameVariable(element, oldName, newName, type,
            RenameType == RenameType.ExposedName, changes, elementsToSave);
        if (elementsToSave.Contains(element))
        {
            _elementCommands.SortVariables(element);
        }

        foreach (var elementToSave in elementsToSave)
        {
            _fileCommands.TryAutoSaveElement(elementToSave);
        }

    }
}
