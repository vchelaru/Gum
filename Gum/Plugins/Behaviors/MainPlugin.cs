using Gum.Plugins.BaseClasses;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gum.DataTypes;
using Gum.ToolStates;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;

namespace Gum.Plugins.Behaviors
{
    [Export(typeof(PluginBase))]
    public class MainPlugin : InternalPlugin
    {
        BehaviorsControl control;
        BehaviorsViewModel viewModel = new BehaviorsViewModel();

        public override void StartUp()
        {
            viewModel = new BehaviorsViewModel();
            viewModel.ApplyChangedValues += HandleApplyBehaviorChanges;

            control = new BehaviorsControl();
            control.DataContext = viewModel;
            GumCommands.Self.GuiCommands.AddControl(control, "Behaviors");
            GumCommands.Self.GuiCommands.RemoveControl(control);
            this.ElementSelected += HandleElementSelected;
        }

        private void HandleApplyBehaviorChanges(object sender, EventArgs e)
        {
            var component = SelectedState.Self.SelectedComponent;

            if(component != null)
            {
                var selectedBehaviorNames = viewModel.AllBehaviors
                    .Where(item => item.IsChecked)
                    .Select(item => item.Name);

                var addedBehaviors = selectedBehaviorNames
                    .Except(component.Behaviors.Select(item => item.BehaviorName));

                var removedBehaviors = component.Behaviors.Select(item => item.BehaviorName)
                    .Except(selectedBehaviorNames);

                if(removedBehaviors.Any())
                {
                    // ask the user what to do
                }

                foreach(var behaviorName in addedBehaviors)
                {
                    var project = ProjectManager.Self.GumProjectSave;
                    var behaviorSave = project.Behaviors.FirstOrDefault(item => item.Name == behaviorName);

                    AddCategoriesFromBehavior(behaviorSave, component);
                }

                component.Behaviors.Clear();
                foreach (var behavior in viewModel.AllBehaviors.Where(item => item.IsChecked))
                {
                    var newBehavior = new ElementBehaviorReference();
                    newBehavior.BehaviorName = behavior.Name;
                    // for now, no multiple projects supported
                    component.Behaviors.Add(newBehavior);
                }

                GumCommands.Self.GuiCommands.RefreshStateTreeView();
                GumCommands.Self.FileCommands.TryAutoSaveCurrentElement();


                UpdateViewModelTo(component);
            }
        }

        private void AddCategoriesFromBehavior(BehaviorSave behaviorSave, ComponentSave component)
        {
            foreach(var behaviorCategory in behaviorSave.Categories)
            {
                StateSaveCategory matchingComponentCategory = 
                    component.Categories.FirstOrDefault(item => item.Name == behaviorCategory.Name);

                if(matchingComponentCategory == null)
                {
                    //category doesn't exist, so let's add a clone of it:
                    matchingComponentCategory = new StateSaveCategory();
                    matchingComponentCategory.Name = behaviorCategory.Name;
                    component.Categories.Add(matchingComponentCategory);
                }

                foreach(var behaviorState in behaviorCategory.States)
                {
                    var matchingComponentState = 
                        matchingComponentCategory.States.FirstOrDefault(item => item.Name == behaviorState.Name);

                    if(matchingComponentState == null)
                    {
                        // state doesn't exist, so add it:
                        var newState = new StateSave();
                        newState.Name = behaviorState.Name;
                        newState.ParentContainer = component;
                        matchingComponentCategory.States.Add(newState);
                    }
                }
            }
        }

        private void HandleElementSelected(ElementSave element)
        {
            var asComponent = element as ComponentSave;

            bool shouldShow = asComponent != null;

            // In case the user left without clicking "OK" on the previous edit:
            viewModel.IsEditing = false;

            if(asComponent != null)
            {
                UpdateViewModelTo(asComponent);
                GumCommands.Self.GuiCommands.AddControl(control, "Behaviors");

            }
            else
            {
                GumCommands.Self.GuiCommands.RemoveControl(control);
            }
        }

        private void UpdateViewModelTo(ComponentSave asComponent)
        {
            viewModel.AddedBehaviors.Clear();

            foreach(var behavior in asComponent.Behaviors)
            {
                viewModel.AddedBehaviors.Add(behavior.BehaviorName);
            }

            viewModel.AllBehaviors.Clear();
            foreach(var behavior in ProjectManager.Self.GumProjectSave.Behaviors)
            {
                var newItem = new CheckListBehaviorItem();

                newItem.Name = behavior.Name;
                newItem.IsChecked = viewModel.AddedBehaviors.Contains(behavior.Name);

                viewModel.AllBehaviors.Add(newItem);
            }


        }
    }
}
