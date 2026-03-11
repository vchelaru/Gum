using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Services;
using Gum.ToolStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace Gum.DataTypes;

public static class ElementSaveExtensionMethodsGumTool
{
    private static IProjectManager ProjectManager => Locator.GetRequiredService<IProjectManager>();

    public static FilePath? GetFullPathXmlFile(this ElementSave? elementSave)
    {
        return elementSave?.GetFullPathXmlFile(elementSave.Name);
    }


    public static FilePath? GetFullPathXmlFile(this ElementSave elementSave, string elementSaveName)
    {
        var gumProject = ProjectManager.GumProjectSave;
        if (string.IsNullOrEmpty(gumProject?.FullFileName))
        {
            return null;
        }

        var extension = elementSave.FileExtension;

        var reference =
            gumProject.ScreenReferences.FirstOrDefault(item => item.Name == elementSave.Name) ??
            gumProject.ComponentReferences.FirstOrDefault(item => item.Name == elementSave.Name) ??
            gumProject.StandardElementReferences.FirstOrDefault(item => item.Name == elementSave.Name);

        FilePath gumDirectory = FileManager.GetDirectory(gumProject.FullFileName);
        if (!string.IsNullOrWhiteSpace(reference?.Link))
        {
            return gumDirectory.Original + reference.Link;
        }
        else
        {

            return gumDirectory.Original + elementSave.Subfolder + "\\" + elementSaveName + "." + extension;
        }
    }

    private static void FillWithDefaultRecursively(ElementSave element, StateSave stateSave)
    {
        foreach (var variable in element.DefaultState.Variables)
        {
            var alreadyExists = stateSave.Variables.Any(item => item.Name == variable.Name);

            if (!alreadyExists)
            {
                stateSave.Variables.Add(variable);
            }
        }

        if (!string.IsNullOrEmpty(element.BaseType))
        {
            var baseElement = ObjectFinder.Self.GetElementSave(element.BaseType);
            if (baseElement != null)
            {
                FillWithDefaultRecursively(baseElement, stateSave);
            }
        }
    }

    /// <summary>
    /// Returns the VariableSave from the argument element or its base element using an explicit state.
    /// </summary>
    public static VariableSave GetVariableFromThisOrBase(this ElementSave element, string variable, StateSave stateToPullFrom)
    {
        return stateToPullFrom.GetVariableRecursive(variable);
    }

    /// <summary>
    /// Returns the VariableSave from the argument element or its base element.
    /// Uses the currently selected state when the element is selected, unless forceDefault is true.
    /// </summary>
    public static VariableSave GetVariableFromThisOrBase(this ElementSave element, string variable, bool forceDefault = false)
    {
        if (forceDefault)
            return element.GetVariableFromThisOrBase(variable, element.DefaultState);

        var selectedState = Locator.GetRequiredService<ISelectedState>();
        var stateToPullFrom = (element == selectedState.SelectedElement && selectedState.SelectedStateSave != null)
            ? selectedState.SelectedStateSave
            : element.DefaultState;

        return element.GetVariableFromThisOrBase(variable, stateToPullFrom);
    }

    /// <summary>
    /// Returns the VariableListSave from the argument element or its base element using an explicit state.
    /// </summary>
    public static VariableListSave GetVariableListFromThisOrBase(this ElementSave element, string variable, StateSave stateToPullFrom)
    {
        return stateToPullFrom.GetVariableListRecursive(variable);
    }

    /// <summary>
    /// Returns the VariableListSave from the argument element or its base element.
    /// Uses the currently selected state when the element is selected, unless forceDefault is true.
    /// </summary>
    public static VariableListSave GetVariableListFromThisOrBase(this ElementSave element, string variable, bool forceDefault = false)
    {
        if (forceDefault)
            return element.GetVariableListFromThisOrBase(variable, element.DefaultState);

        var selectedState = Locator.GetRequiredService<ISelectedState>();
        var stateToPullFrom = (element == selectedState.SelectedElement && selectedState.SelectedStateSave != null)
            ? selectedState.SelectedStateSave
            : element.DefaultState;

        return element.GetVariableListFromThisOrBase(variable, stateToPullFrom);
    }

    /// <summary>
    /// Returns the value from the argument element or its base element using an explicit state.
    /// </summary>
    public static object GetValueFromThisOrBase(this ElementSave element, string variable, StateSave stateToPullFrom)
    {
        return stateToPullFrom.GetVariableRecursive(variable)?.Value;
    }

    /// <summary>
    /// Returns the value from the argument element or its base element.
    /// Uses the currently selected state when the element is selected, unless forceDefault is true.
    /// </summary>
    public static object GetValueFromThisOrBase(this ElementSave element, string variable, bool forceDefault = false)
    {
        if (forceDefault)
            return element.GetValueFromThisOrBase(variable, element.DefaultState);

        var selectedState = Locator.GetRequiredService<ISelectedState>();
        var stateToPullFrom = (element == selectedState.SelectedElement && selectedState.SelectedStateSave != null)
            ? selectedState.SelectedStateSave
            : element.DefaultState;

        return element.GetValueFromThisOrBase(variable, stateToPullFrom);
    }

}
