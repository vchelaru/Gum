using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Mvvm;
using Gum.ToolCommands;
using Gum.ToolStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace Gum.Plugins.InternalPlugins.VariableGrid.ViewModels;
public class AddVariableViewModel : ViewModel
{
    public string EnteredName
    {
        get => Get<string>();
        set => Set(value);
    }

    private readonly Commands.GuiCommands _guiCommands;
    private readonly ISelectedState _selectedState;

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
    public object DefaultValue
    {
        get
        {
            switch (SelectedItem)
            {
                case "float":
                    return 1.0f;
                case "int":
                    return 10;
                case "string":
                    return "Hello";
                case "bool":
                    return true;
                default:
                    return null;
            }
        }
    }

    public AddVariableViewModel(Commands.GuiCommands guiCommands,
        ISelectedState selectedState)
    {
        _guiCommands = guiCommands;
        _selectedState = selectedState;

        AvailableTypes = new List<string>();
        AvailableTypes.Add("float");
        AvailableTypes.Add("int");
        AvailableTypes.Add("string");
        AvailableTypes.Add("bool");

        SelectedItem = "float";
    }

    public GeneralResponse Validate()
    {
        if(SelectedItem == null)
        {
            return GeneralResponse.UnsuccessfulWith("You must select a type");
        }
        if(string.IsNullOrEmpty(EnteredName))
        {
            return GeneralResponse.UnsuccessfulWith("You must enter a name");
        }
        return GeneralResponse.SuccessfulResponse;
    }

    public void AddVariableToSelectedItem()
    {
        var type = SelectedItem;
        var name = EnteredName;

        string whyNotValid;
        bool isValid = NameVerifier.Self.IsVariableNameValid(
            name, out whyNotValid);

        if (!isValid)
        {
            _guiCommands.ShowMessage(whyNotValid);
        }
        else
        {
            var behavior = _selectedState.SelectedBehavior;

            var newVariable = new VariableSave();

            newVariable.Name = name;
            newVariable.Type = type;
            newVariable.Value = DefaultValue;

            if (behavior != null)
            {
                behavior.RequiredVariables.Variables.Add(newVariable);
                ElementCommands.Self.SortVariables(behavior);
                GumCommands.Self.FileCommands.TryAutoSaveBehavior(behavior);
            }
            else if (_selectedState.SelectedElement != null)
            {
                var element = _selectedState.SelectedElement;
                newVariable.IsCustomVariable = true;
                element.DefaultState.Variables.Add(newVariable);
                ElementCommands.Self.SortVariables(element);
                GumCommands.Self.FileCommands.TryAutoSaveElement(element);
            }
            GumCommands.Self.GuiCommands.RefreshVariables(force: true);

        }
    }
}
