using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.PropertyGridHelpers;
using Gum.Services;
using Gum.ToolStates;
using GumCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WpfDataUi.DataTypes;

namespace Gum.Plugins.InternalPlugins.VariableGrid;
public class ColorPickerLogic
{
    private readonly ISelectedState _selectedState;
    private readonly IExposeVariableService _exposeVariableService;

    public ColorPickerLogic()
    {
        _selectedState =
            Locator.GetRequiredService<ISelectedState>();
        _exposeVariableService =
            Locator.GetRequiredService<IExposeVariableService>();
    }

    public void UpdateColorCategory(List<MemberCategory> categories, ElementSave element, InstanceSave instance)
    {
        foreach (var category in categories)
        {
            if (category != null)
            {
                var membersBefore = category.Members.ToArray();

                foreach (var variable in membersBefore)
                {
                    VariableSave rootVariable = null;
                    if (instance != null)
                    {
                        rootVariable = ObjectFinder.Self.GetRootVariable(variable.Name, instance);
                    }
                    else
                    {
                        rootVariable = ObjectFinder.Self.GetRootVariable(variable.Name, element);
                    }

                    if (rootVariable?.Name == "Red")
                    {
                        AddColorVariable(element, instance, category, variable);

                    }
                }
            }
        }
    }

    private void AddColorVariable(ElementSave element, InstanceSave instance, MemberCategory category, InstanceMember? variable)
    {
        //var indexOfRed = variable.Name.IndexOf("Red");
        //var before = variable.Name.Substring(0, indexOfRed);
        //var after = variable.Name.Substring(indexOfRed + "Red".Length);

        //var redVariableName = variable.Name;
        //var greenVariableName = before + "Green" + after;
        //var blueVariableName = before + "Blue" + after;

        //var redVariable = variable;
        //var greenVariable = category.Members.FirstOrDefault(item => item.Name == greenVariableName);
        //var blueVariable = category.Members.FirstOrDefault(item => item.Name == blueVariableName);
        var redVariable = variable;
        string beforeRed, afterRed;
        InstanceMember greenVariable, blueVariable;

        TryGetGreenBlueVariables(element, instance, category, variable, out beforeRed, out afterRed, out greenVariable, out blueVariable);

        if (greenVariable != null && blueVariable != null)
        {
            var redVariableName = variable.Name;
            var greenVariableName = greenVariable.Name;
            var blueVariableName = blueVariable.Name;

            // These could be exposed... If so, we want to assign the name with the dot in it, not the exposed as name:
            if (instance == null && element != null)
            {
                var foundRed = element.GetVariableFromThisOrBase(redVariableName);
                if (foundRed?.ExposedAsName == redVariableName)
                {
                    redVariableName = foundRed.Name;
                }
                var foundGreen = element.GetVariableFromThisOrBase(greenVariableName);
                if (foundGreen?.ExposedAsName == greenVariableName)
                {
                    greenVariableName = foundGreen.Name;
                }
                var foundBlue = element.GetVariableFromThisOrBase(blueVariableName);
                if (foundBlue?.ExposedAsName == blueVariableName)
                {
                    blueVariableName = foundBlue.Name;
                }
            }



            InstanceMember instanceMember = new InstanceMember($"{beforeRed}Color{afterRed}", null);
            instanceMember.PreferredDisplayer = typeof(Gum.Controls.DataUi.ColorDisplay);

            instanceMember.CustomGetTypeEvent += (arg) => typeof(System.Drawing.Color);

            instanceMember.CustomGetEvent += (notUsed) => GetCurrentColor(redVariableName, greenVariableName, blueVariableName);
            instanceMember.CustomSetPropertyEvent += (sender, args) => SetCurrentColor(args, redVariableName, greenVariableName, blueVariableName);

            TryAddExposeColorVariableMenuItem(instance, instanceMember);

            // so color updates
            redVariable.PropertyChanged += (not, used) => instanceMember.SimulateValueChanged();
            greenVariable.PropertyChanged += (not, used) => instanceMember.SimulateValueChanged();
            blueVariable.PropertyChanged += (not, used) => instanceMember.SimulateValueChanged();

            var indexToInsertAfter = Math.Max(category.Members.IndexOf(redVariable), Math.Max(category.Members.IndexOf(greenVariable), category.Members.IndexOf(blueVariable)));
            category.Members.Insert(indexToInsertAfter + 1, instanceMember);

        }
    }

    private void TryAddExposeColorVariableMenuItem(InstanceSave instance, InstanceMember instanceMember)
    {
        if (instance != null)
        {
            var element = _selectedState.SelectedElement;
            var defaultState = element?.DefaultState;

            var redVariable = defaultState?.GetVariableSave($"{instance.Name}.Red");
            var greenVariable = defaultState?.GetVariableSave($"{instance.Name}.Green");
            var blueVariable = defaultState?.GetVariableSave($"{instance.Name}.Blue");

            var isAlreadyExposed =
                !string.IsNullOrEmpty(redVariable?.ExposedAsName) ||
                !string.IsNullOrEmpty(greenVariable?.ExposedAsName) ||
                !string.IsNullOrEmpty(blueVariable?.ExposedAsName);

            if (!isAlreadyExposed)
            {
                instanceMember.ContextMenuEvents.Add("Expose Color (Red, Green, Blue)", HandleExposeColorEvent);
            }
        }
    }

    private void HandleExposeColorEvent(object sender, RoutedEventArgs e)
    {
        // expose 3 variables: Red, Green, and Blue
        var instance = _selectedState.SelectedInstance;

        _exposeVariableService.HandleExposeVariableClick(
            instance, "Red");
        _exposeVariableService.HandleExposeVariableClick(
            instance, "Green");
        _exposeVariableService.HandleExposeVariableClick(
            instance, "Blue");
    }


    private static void TryGetGreenBlueVariables(ElementSave element, InstanceSave instance, MemberCategory category, InstanceMember? variable, out string beforeRed, out string afterRed, out InstanceMember greenVariable, out InstanceMember blueVariable)
    {
        List<InstanceMember> greenVariables = new List<InstanceMember>();
        List<InstanceMember> blueVariables = new List<InstanceMember>();
        if (instance != null)
        {
            greenVariables.AddRange(category.Members.Where(item =>
            {
                return
                    ObjectFinder.Self.GetRootVariable(item.Name, instance)?.Name == "Green";
            }));
            blueVariables.AddRange(category.Members.Where(item =>
            {
                return
                    ObjectFinder.Self.GetRootVariable(item.Name, instance)?.Name == "Blue";
            }));
        }
        else
        {
            greenVariables.AddRange(category.Members.Where(item =>
            {
                return
                    ObjectFinder.Self.GetRootVariable(item.Name, element)?.Name == "Green";
            }));
            blueVariables.AddRange(category.Members.Where(item =>
            {
                return
                    ObjectFinder.Self.GetRootVariable(item.Name, element)?.Name == "Blue";
            }));
        }

        var rootName = variable.DisplayName;

        beforeRed = "";
        afterRed = "";
        if (rootName.Contains("Red"))
        {
            beforeRed = rootName.Substring(0, rootName.IndexOf("Red"));
            afterRed = rootName.Substring(rootName.IndexOf("Red") + "Red".Length);
        }



        greenVariable = null;
        blueVariable = null;

        // local variables needed for lambdas
        var beforeRedLocal = beforeRed;
        var afterRedLocal = afterRed;

        if (greenVariables.Count > 0)
        {
            greenVariable = greenVariables.FirstOrDefault(item => item.DisplayName == $"{beforeRedLocal}Green{afterRedLocal}");
        }
        // In case there are exactly 1, or no matches were found:
        if (greenVariable == null)
        {
            greenVariable = greenVariables.FirstOrDefault();
        }

        if (blueVariables.Count > 0)
        {
            blueVariable = blueVariables.FirstOrDefault(item => item.DisplayName == $"{beforeRedLocal}Blue{afterRedLocal}");
        }
        // In case there are exactly 1, or no matches were found:
        if (blueVariable == null)
        {
            blueVariable = blueVariables.FirstOrDefault();
        }
    }

    System.Drawing.Color GetCurrentColor(string redVariableName, string greenVariableName, string blueVariableName)
    {
        var selectedState = _selectedState.SelectedStateSave;

        int red = 0;
        int green = 0;
        int blue = 0;
        if (selectedState != null)
        {
            object redAsObject = selectedState.GetValueRecursive(redVariableName);
            if (redAsObject != null)
            {
                red = (int)redAsObject;
            }

            object greenAsObject = selectedState.GetValueRecursive(greenVariableName);
            if (greenAsObject != null)
            {
                green = (int)greenAsObject;
            }

            object blueAsObject = selectedState.GetValueRecursive(blueVariableName);
            if (blueAsObject != null)
            {
                blue = (int)blueAsObject;
            }
        }

        return System.Drawing.Color.FromArgb(red, green, blue);
    }

    void SetCurrentColor(SetPropertyArgs args, string redVariableName, string greenVariableName, string blueVariableName)
    {
        var valueBeforeSet = GetCurrentColor(redVariableName, greenVariableName, blueVariableName);
        var state = _selectedState.SelectedStateSave;

        var color = (System.Drawing.Color)args.Value;

        state.SetValue(redVariableName, (int)color.R, "int");

        state.SetValue(greenVariableName, (int)color.G, "int");
        state.SetValue(blueVariableName, (int)color.B, "int");

        var instance = _selectedState.SelectedInstance;
        // These functions take unqualified:

        var element = _selectedState.SelectedElement;
        var defaultState = element.DefaultState;

        if (instance == null && redVariableName.Contains("."))
        {
            // This is an exposed:
            var foundDefaultRedVariable = defaultState.GetVariableSave(redVariableName);
            if (!string.IsNullOrEmpty(foundDefaultRedVariable.ExposedAsName) && foundDefaultRedVariable != null)
            {
                state.GetVariableSave(redVariableName).ExposedAsName = foundDefaultRedVariable.ExposedAsName;
            }
            var foundDefaultGreenVariable = defaultState.GetVariableSave(greenVariableName);
            if (!string.IsNullOrEmpty(foundDefaultGreenVariable.ExposedAsName) && foundDefaultGreenVariable != null)
            {
                state.GetVariableSave(greenVariableName).ExposedAsName = foundDefaultGreenVariable.ExposedAsName;
            }
            var foundDefaultBlueVariable = defaultState.GetVariableSave(blueVariableName);
            if (!string.IsNullOrEmpty(foundDefaultBlueVariable.ExposedAsName) && foundDefaultBlueVariable != null)
            {
                state.GetVariableSave(blueVariableName).ExposedAsName = foundDefaultBlueVariable.ExposedAsName;
            }
        }

        var unqualifiedRed = redVariableName.Substring(redVariableName.IndexOf('.') + 1);
        var unqualifiedGreen = greenVariableName.Substring(greenVariableName.IndexOf('.') + 1);
        var unqualifiedBlue = blueVariableName.Substring(blueVariableName.IndexOf('.') + 1);

        if (redVariableName.Contains(".") && instance == null)
        {
            // This is an exposed:
            instance = _selectedState.SelectedElement.GetInstance(redVariableName.Substring(0, redVariableName.IndexOf('.')));
        }

        var shouldSave = args.CommitType == SetPropertyCommitType.Full;

        SetVariableLogic.Self.PropertyValueChanged(unqualifiedRed, (int)valueBeforeSet.R, instance, defaultState, refresh: true, recordUndo: shouldSave, trySave: shouldSave);
        SetVariableLogic.Self.PropertyValueChanged(unqualifiedGreen, (int)valueBeforeSet.G, instance, defaultState, refresh: true, recordUndo: shouldSave, trySave: shouldSave);
        SetVariableLogic.Self.PropertyValueChanged(unqualifiedBlue, (int)valueBeforeSet.B, instance, defaultState, refresh: true, recordUndo: shouldSave, trySave: shouldSave);

        if (args.CommitType == SetPropertyCommitType.Full)
        {
            GumCommands.Self.GuiCommands.RefreshVariables();
        }
    }

}
