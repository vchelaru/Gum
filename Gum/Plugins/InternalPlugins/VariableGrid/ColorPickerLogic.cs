using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.PropertyGridHelpers;
using Gum.Services;
using Gum.ToolStates;
using Gum.Undo;
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
    private readonly IUndoManager _undoManager;
    private readonly IGuiCommands _guiCommands;
    private readonly ObjectFinder _objectFinder;
    private readonly SetVariableLogic _setVariableLogic;

    public ColorPickerLogic(ISelectedState selectedState,
        IExposeVariableService exposeVariableService,
        IUndoManager undoManager,
        IGuiCommands guiCommands,
        ObjectFinder objectFinder,
        SetVariableLogic setVariableLogic)
    {
        _selectedState = selectedState;
        _exposeVariableService = exposeVariableService;
        _undoManager = undoManager;
        _guiCommands = guiCommands;
        _objectFinder = objectFinder;
        _setVariableLogic = setVariableLogic;
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
                    VariableSave? rootVariable = null;
                    if (instance != null)
                    {
                        rootVariable = _objectFinder.GetRootVariable(variable.Name, instance);
                    }
                    else
                    {
                        rootVariable = _objectFinder.GetRootVariable(variable.Name, element);
                    }

                    if (rootVariable?.Name == "Red")
                    {
                        AddColorVariable(element, instance, category, variable);

                    }
                }
            }
        }
    }

    private void AddColorVariable(ElementSave element, InstanceSave? instance, MemberCategory category, InstanceMember variable)
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
        string colorPrefix, colorSuffix;
        InstanceMember? greenVariable, blueVariable;

        TryGetGreenBlueVariables(element, instance, category, variable, out colorPrefix, out colorSuffix, out greenVariable, out blueVariable);

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

            InstanceMember instanceMember = new InstanceMember($"{colorPrefix}Color{colorSuffix}", null);
            instanceMember.PreferredDisplayer = typeof(Gum.Controls.DataUi.ColorDisplay);

            instanceMember.CustomGetTypeEvent += (arg) => typeof(System.Drawing.Color);

            instanceMember.CustomGetEvent += (notUsed) => GetCurrentColor(redVariableName, greenVariableName, blueVariableName);
            instanceMember.CustomSetPropertyEvent += (sender, args) => SetCurrentColor(args, redVariableName, greenVariableName, blueVariableName);
            instanceMember.SupportsMakeDefault = false;
            instanceMember.IsReadOnly = variable.IsReadOnly || greenVariable.IsReadOnly || blueVariable.IsReadOnly;

            instanceMember.ContextMenuEvents.Add("Make Default", (_,_) => HandleMakeColorDefault(redVariableName, greenVariableName, blueVariableName));

            TryAddExposeColorVariableMenuItem(instance, instanceMember);

            // so color updates
            redVariable.PropertyChanged += (not, used) => instanceMember.SimulateValueChanged();
            greenVariable.PropertyChanged += (not, used) => instanceMember.SimulateValueChanged();
            blueVariable.PropertyChanged += (not, used) => instanceMember.SimulateValueChanged();

            var indexToInsertAfter = Math.Max(category.Members.IndexOf(redVariable), Math.Max(category.Members.IndexOf(greenVariable), category.Members.IndexOf(blueVariable)));
            category.Members.Insert(indexToInsertAfter + 1, instanceMember);
        }
    }

    private void HandleMakeColorDefault(string redVariableName, string greenVariableName, string blueVariableName)
    {
        var state = _selectedState.SelectedElement?.DefaultState;

        //////////////////////////Early Out//////////////////////////
        if(state == null)
        {
            return;
        }
        ////////////////////////End Early Out////////////////////////

        using var undoLock = _undoManager.RequestLock();

        MakeDefault(state.GetVariableSave(redVariableName));
        MakeDefault(state.GetVariableSave(greenVariableName));
        MakeDefault(state.GetVariableSave(blueVariableName));

        _guiCommands.RefreshVariables();

        void MakeDefault(VariableSave variableSave)
        {
            var shouldPushValueChanged = true;

            object? oldValue = variableSave?.Value;

            if(variableSave == null)
            {
                // do nothing, doesn't exist anyway
                shouldPushValueChanged = false;
            }
            else if(!string.IsNullOrEmpty(variableSave.ExposedAsName))
            {
                // we can't delete the variable because if we did, we'd lose
                // the exposed variable too, so we just have to make it not set
                // the value:
                variableSave.SetsValue = false;
            }
            else
            {
                state.Variables.Remove(variableSave);
            }

            if(shouldPushValueChanged)
            {
                _setVariableLogic.PropertyValueChanged(
                    variableSave!.GetRootName(),
                    oldValue,
                    _selectedState.SelectedInstance, 
                    state,
                    recordUndo:false);

            }
        }
    }

    private void TryAddExposeColorVariableMenuItem(InstanceSave? instance, InstanceMember instanceMember)
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
        var element = _selectedState.SelectedElement;

        ////////////////////////Early Out////////////////////////

        if(instance == null || element == null)
        {
            return;
        }

        //////////////////////End Early Out/////////////////////

        using var undoLock = _undoManager.RequestLock();

        List<VariableSave> toRevert = new ();

        var redResponse = _exposeVariableService.HandleExposeVariableClick(
            instance, "Red");
        bool shouldRevert = false;

        if(redResponse.DidAttempt && redResponse.Succeeded == false)
        {
            shouldRevert = true;
        }
        if(redResponse.Data != null)
        {
            toRevert.Add(redResponse.Data);
        }

        if(!shouldRevert)
        {
            var greenResponse = _exposeVariableService.HandleExposeVariableClick(
                instance, "Green");

            if(greenResponse.DidAttempt && greenResponse.Succeeded == false)
            {
                shouldRevert = true;
            }
            if (greenResponse.Data != null)
            {
                toRevert.Add(greenResponse.Data);
            }
        }


        if(!shouldRevert)
        {
            var blueResponse = _exposeVariableService.HandleExposeVariableClick(
                instance, "Blue");

            if(blueResponse.DidAttempt && blueResponse.Succeeded == false)
            {
                shouldRevert = true;
            }

            if(blueResponse.Data != null)
            {
                toRevert.Add(blueResponse.Data);
            }
        }

        if(shouldRevert)
        {
            foreach(var item in toRevert)
            {
                _exposeVariableService.HandleUnexposeVariableClick(item, element);
            }
        }
    }


    private void TryGetGreenBlueVariables(ElementSave element, 
        InstanceSave? instance, 
        MemberCategory category, 
        InstanceMember variable, 
        out string beforeRed, 
        out string afterRed, 
        out InstanceMember? greenVariable, 
        out InstanceMember? blueVariable)
    {
        List<InstanceMember> greenVariables = new List<InstanceMember>();
        List<InstanceMember> blueVariables = new List<InstanceMember>();
        if (instance != null)
        {
            greenVariables.AddRange(category.Members.Where(item =>
            {
                return
                    _objectFinder.GetRootVariable(item.Name, instance)?.Name == "Green";
            }));
            blueVariables.AddRange(category.Members.Where(item =>
            {
                return
                    _objectFinder.GetRootVariable(item.Name, instance)?.Name == "Blue";
            }));
        }
        else
        {
            greenVariables.AddRange(category.Members.Where(item =>
            {
                return
                    _objectFinder.GetRootVariable(item.Name, element)?.Name == "Green";
            }));
            blueVariables.AddRange(category.Members.Where(item =>
            {
                return
                    _objectFinder.GetRootVariable(item.Name, element)?.Name == "Blue";
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

        return System.Drawing.Color.FromArgb(
            System.Math.Max(0, System.Math.Min(255, red)),
            System.Math.Max(0, System.Math.Min(255, green)),
            System.Math.Max(0, System.Math.Min(255, blue)));
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

        _setVariableLogic.PropertyValueChanged(unqualifiedRed, (int)valueBeforeSet.R, instance, defaultState, refresh: true, recordUndo: shouldSave, trySave: shouldSave);
        _setVariableLogic.PropertyValueChanged(unqualifiedGreen, (int)valueBeforeSet.G, instance, defaultState, refresh: true, recordUndo: shouldSave, trySave: shouldSave);
        _setVariableLogic.PropertyValueChanged(unqualifiedBlue, (int)valueBeforeSet.B, instance, defaultState, refresh: true, recordUndo: shouldSave, trySave: shouldSave);

        if (args.CommitType == SetPropertyCommitType.Full)
        {
            _guiCommands.RefreshVariables();
        }
    }
}


