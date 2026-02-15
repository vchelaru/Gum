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
    private static readonly ISelectedState _selectedState = Locator.GetRequiredService<ISelectedState>();
    private static readonly IProjectManager _projectManager = Locator.GetRequiredService<IProjectManager>();

    public static FilePath? GetFullPathXmlFile(this ElementSave? elementSave)
    {
        return elementSave?.GetFullPathXmlFile(elementSave.Name);
    }


    public static FilePath? GetFullPathXmlFile(this ElementSave elementSave, string elementSaveName)
    {
        var gumProject = _projectManager.GumProjectSave;
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
    /// Returns the VariableSave from the argument element or its base element.  
    /// If forceDefault is set to true, then only the element's default state will be checked.
    /// </summary>
    /// <param name="element"></param>
    /// <param name="variable"></param>
    /// <param name="forceDefault"></param>
    /// <returns></returns>
    public static VariableSave GetVariableFromThisOrBase(this ElementSave element, string variable, bool forceDefault = false)
    {
        StateSave stateToPullFrom = element.DefaultState;

        if (element == _selectedState.SelectedElement &&
            _selectedState.SelectedStateSave != null &&
            !forceDefault)
        {
            stateToPullFrom = _selectedState.SelectedStateSave;
        }

        return stateToPullFrom.GetVariableRecursive(variable);
    }

    public static VariableListSave GetVariableListFromThisOrBase(this ElementSave element, string variable, bool forceDefault = false)
    {
        var stateToPullFrom = element.DefaultState;

        if (element == _selectedState.SelectedElement &&
            _selectedState.SelectedStateSave != null &&
            !forceDefault)
        {
            stateToPullFrom = _selectedState.SelectedStateSave;
        }

        return stateToPullFrom.GetVariableListRecursive(variable);
    }





    public static object GetValueFromThisOrBase(this ElementSave element, string variable, bool forceDefault = false)
    {
        StateSave stateToPullFrom = element.DefaultState;

        if (element == _selectedState.SelectedElement &&
            _selectedState.SelectedStateSave != null &&
            !forceDefault)
        {
            stateToPullFrom = _selectedState.SelectedStateSave;
        }

        VariableSave variableSave = stateToPullFrom.GetVariableRecursive(variable);
        if (variableSave != null)
        {
            return variableSave.Value;
        }
        else
        {
            return null;
        }
    }

}
