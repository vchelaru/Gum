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
using WpfDataUi;
using System.Windows.Forms;

namespace Gum.Plugins.Behaviors
{
    [Export(typeof(PluginBase))]
    public class MainPlugin : InternalPlugin
    {
        BehaviorsControl control;
        BehaviorsViewModel viewModel = new BehaviorsViewModel();
        DataUiGrid stateDataUiGrid;

        public override void StartUp()
        {
            viewModel = new BehaviorsViewModel();
            viewModel.ApplyChangedValues += HandleApplyBehaviorChanges;

            control = new BehaviorsControl();
            control.DataContext = viewModel;
            GumCommands.Self.GuiCommands.AddControl(control, "Behaviors");
            GumCommands.Self.GuiCommands.RemoveControl(control);
            this.ElementSelected += HandleElementSelected;

            stateDataUiGrid = new DataUiGrid();

            this.BehaviorSelected += HandleBehaviorSelected;
            this.StateWindowTreeNodeSelected += HandleStateSelected;
        }

        private void HandleStateSelected(TreeNode obj)
        {
            RefreshStateVariables();
        }

        bool isStateTabShown;
        private void HandleBehaviorSelected(BehaviorSave obj)
        {
            RefreshStateVariables();
        }

        private void RefreshStateVariables()
        {
            //var shouldShow =
            //    SelectedState.Self.SelectedBehavior != null &&
            //    SelectedState.Self.SelectedStateSave != null;

            //if (!shouldShow)
            //{
            //    if(isStateTabShown)
            //    {
            //        GumCommands.Self.GuiCommands.RemoveControl(stateDataUiGrid);
            //        isStateTabShown = false;
            //    }
            //}
            //else
            //{
            //    if(!isStateTabShown)
            //    {
            //        GumCommands.Self.GuiCommands.AddControl(stateDataUiGrid, "State Properties");
            //        isStateTabShown = true;
            //    }
            //    stateDataUiGrid.Instance = SelectedState.Self.SelectedStateSave;
            //}
        }

        private void HandleApplyBehaviorChanges(object sender, EventArgs e)
        {
            var component = SelectedState.Self.SelectedComponent;

            if(component != null)
            {
                var selectedBehaviorNames = viewModel.AllBehaviors
                    .Where(item => item.IsChecked)
                    .Select(item => item.Name)
                    .ToList();

                var addedBehaviors = selectedBehaviorNames
                    .Except(component.Behaviors.Select(item => item.BehaviorName))
                    .ToList();

                var removedBehaviors = component.Behaviors.Select(item => item.BehaviorName)
                    .Except(selectedBehaviorNames)
                    .ToList();

                if(removedBehaviors.Any())
                {
                    // ask the user what to do
                }

                foreach(var behaviorName in addedBehaviors)
                {
                    var project = ProjectManager.Self.GumProjectSave;
                    var behaviorSave = project.Behaviors.FirstOrDefault(item => item.Name == behaviorName);

                    GumCommands.Self.ProjectCommands.ElementCommands.AddCategoriesFromBehavior(behaviorSave, component);
                }

                component.Behaviors.Clear();
                foreach (var behavior in viewModel.AllBehaviors.Where(item => item.IsChecked))
                {

                    GumCommands.Self.ProjectCommands.ElementCommands.AddBehaviorTo(behavior.Name, component, performSave:false);
                }

                GumCommands.Self.GuiCommands.RefreshStateTreeView();
                GumCommands.Self.FileCommands.TryAutoSaveCurrentElement();

                UpdateViewModelTo(component);

                if(removedBehaviors.Any() || addedBehaviors.Any())
                {
                    PluginManager.Self.BehaviorReferencesChanged(component);
                }
            }
        }


        bool hasBheaviorsControlBeenAdded = false;
        private void HandleElementSelected(ElementSave element)
        {
            var asComponent = element as ComponentSave;

            bool shouldShow = asComponent != null;

            // In case the user left without clicking "OK" on the previous edit:
            viewModel.IsEditing = false;

            if(asComponent != null)
            {
                UpdateViewModelTo(asComponent);
                if(!hasBheaviorsControlBeenAdded)
                {
                    GumCommands.Self.GuiCommands.AddControl(control, "Behaviors");
                    hasBheaviorsControlBeenAdded = true;
                }

            }
            else
            {
                GumCommands.Self.GuiCommands.RemoveControl(control);
                hasBheaviorsControlBeenAdded = false;
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
