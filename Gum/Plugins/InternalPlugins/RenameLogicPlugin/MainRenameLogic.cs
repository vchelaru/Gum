using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins.BaseClasses;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace Gum.Plugins.RenameLogicPlugin;


[Export(typeof(Gum.Plugins.BaseClasses.PluginBase))]
public class MainRenameLogic : InternalPlugin
{
    public override void StartUp()
    {
        this.CategoryRename += HandleCategoryRename;

        this.StateRename += HandleStateRename;
    }

    private void HandleStateRename(StateSave state, string oldName)
    {
        var elementSave = state.ParentContainer;

        if (elementSave != null)
        {
            StateSaveCategory category = elementSave?.Categories.FirstOrDefault(item =>item.States.Contains(state));
            ApplyVariableNameChangeFromStateRename(state, oldName, elementSave, category);
        }
        else
        {
            var behavior = ObjectFinder.Self.GetStateContainerOf(state) as BehaviorSave;

            if(behavior != null)
            {
                _fileCommands.TryAutoSaveBehavior(behavior);
            }
        }
    }

    private void ApplyVariableNameChangeFromStateRename(StateSave state, string oldName, ElementSave elementSave, StateSaveCategory category)
    {
        string variableName = "State";
        if (category != null)
        {
            variableName = category.Name + "State";
        }
        List<DataTypes.InstanceSave> instances = new List<DataTypes.InstanceSave>();
        ObjectFinder.Self.GetElementsReferencing(elementSave, foundInstances: instances);

        HashSet<ElementSave> elementsToSave = new HashSet<ElementSave>();

        foreach (var instance in instances)
        {
            var parentOfInstance = instance.ParentContainer;

            var variableNameToLookFor = $"{instance.Name}.{variableName}";

            var variablesToFix = parentOfInstance.AllStates
                .SelectMany(item => item.Variables)
                .Where(item => item.Name == variableNameToLookFor)
                .Where(item => (string)item.Value == oldName)
                .ToArray();

            if (variablesToFix.Any())
            {
                foreach (var variable in variablesToFix)
                {
                    variable.Value = state.Name;
                }
                if (elementsToSave.Contains(parentOfInstance) == false)
                {
                    elementsToSave.Add(parentOfInstance);
                }
            }
        }

        foreach (var elementToSave in elementsToSave)
        {
            _fileCommands.TryAutoSaveElement(elementToSave);
        }
    }

    private void HandleCategoryRename(StateSaveCategory category, string oldName)
    {
        var elementSave = ObjectFinder.Self.GetContainerOf(category);

        if(elementSave != null)
        {
            foreach (var state in elementSave.AllStates)
            {
                var variablesToChange = state.Variables.Where(
                    item => item.Type == oldName + "State");

                foreach (var variable in variablesToChange)
                {
                    variable.Name = category.Name + "State";
                    variable.Type = category.Name + "State";

#if GUM
                    variable.CustomTypeConverter =
                        new Gum.PropertyGridHelpers.Converters.AvailableStatesConverter(category.Name);
#endif
                }
                state.Variables.Sort((first, second) => first.Name.CompareTo(second.Name));

            }

        }
    }
}
