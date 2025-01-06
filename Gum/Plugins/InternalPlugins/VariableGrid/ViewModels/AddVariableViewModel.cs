using Gum.Mvvm;
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

    public AddVariableViewModel()
    {
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
}
