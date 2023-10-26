using Gum.Plugins.BaseClasses;
using System.ComponentModel.Composition;
using Gum.DataTypes;
using Gum.ToolStates;

namespace Gum.Plugins.Errors
{
    [Export(typeof(PluginBase))]
    public class MainErrorsPlugin : InternalPlugin
    {
        AllErrorsViewModel viewModel;
        ErrorChecker errorChecker;
        ErrorDisplay control;
        PluginTab tabPage;

        public override void StartUp()
        {
            viewModel = new AllErrorsViewModel();

            errorChecker = new ErrorChecker();

            control = new ErrorDisplay();
            control.DataContext = viewModel;

            tabPage = GumCommands.Self.GuiCommands.AddControl(control, "Errors", TabLocation.RightBottom);

            this.ElementSelected += HandleElementSelected;
            this.InstanceAdd += HandleInstanceAdd;
            this.InstanceDelete += HandleInstanceDelete;
            this.VariableSet += HandleVariableSet;
            this.BehaviorReferencesChanged += HandleBehaviorReferencesChanged;
        }

        private void HandleBehaviorReferencesChanged(ElementSave element)
        {
            UpdateErrorsForElement(element);
        }

        private void HandleVariableSet(ElementSave element, InstanceSave instance, string variableName, object oldValue)
        {
            UpdateErrorsForElement(element);
        }

        private void HandleInstanceDelete(ElementSave element, InstanceSave instance)
        {
            UpdateErrorsForElement(element);
        }

        private void HandleInstanceAdd(ElementSave element, InstanceSave instance)
        {
            UpdateErrorsForElement(element);
        }

        private void HandleElementSelected(ElementSave element)
        {
            UpdateErrorsForElement(element);
        }

        private void UpdateErrorsForElement(ElementSave element)
        {
            var errors = errorChecker.GetErrorsFor(element, ProjectState.Self.GumProjectSave);

            viewModel.Errors.Clear();
            foreach (var item in errors)
            {
                viewModel.Errors.Add(item);
            }

            tabPage.Title = $"Errors ({errors.Length})";
        }
    }
}
