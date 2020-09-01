using CodeOutputPlugin.Manager;
using Gum;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;
using Gum.ToolStates;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ToolsUtilities;

namespace CodeOutputPlugin
{
    [Export(typeof(PluginBase))]
    public class MainPlugin : PluginBase
    {
        #region Fields/Properties

        public override string FriendlyName => "Code Output Plugin";

        public override Version Version => new Version(1, 0);

        Views.CodeWindow control;
        ViewModels.CodeWindowViewModel viewModel;
        Models.CodeOutputProjectSettings codeOutputProjectSettings;

        #endregion

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
        }

        public override void StartUp()
        {
            AssignEvents();

            var item = this.AddMenuItem("Plugins", "View Code");
            item.Click += HandleViewCodeClicked;


        }

        private void AssignEvents()
        {
            this.InstanceSelected += HandleInstanceSelected;
            this.ElementSelected += HandleElementSelected;
            this.VariableSet += HandleVariableSet;
            this.StateWindowTreeNodeSelected += HandleStateSelected;
            this.AddAndRemoveVariablesForType += HandleAddAndRemoveVariablesForType;
            this.ProjectLoad += HandleProjectLoaded;
        }

        private void HandleProjectLoaded(GumProjectSave project)
        {
            codeOutputProjectSettings = CodeOutputProjectSettingsManager.CreateOrLoadSettingsForProject();
        }

        private void HandleStateSelected(TreeNode obj)
        {
            if (control != null)
            {
                RefreshCodeDisplay();
            }
        }

        private void HandleInstanceSelected(ElementSave arg1, InstanceSave instance)
        {
            if(control != null)
            {
                RefreshCodeDisplay();
            }
        }

        private void HandleElementSelected(ElementSave element)
        {
            if (control != null)
            {
                LoadCodeSettingsFile();

                RefreshCodeDisplay();
            }
        }

        private void LoadCodeSettingsFile()
        {
            var element = SelectedState.Self.SelectedElement;
            if(element != null)
            {
                control.CodeOutputElementSettings = CodeOutputElementSettingsManager.LoadOrCreateSettingsFor(element);
            }
        }

        private void HandleVariableSet(ElementSave arg1, InstanceSave instance, string arg3, object arg4)
        {
            if (control != null)
            {
                RefreshCodeDisplay();
            }
        }

        private void HandleViewCodeClicked(object sender, EventArgs e)
        {

            if (control == null)
            {
                CreateControl();
            }

            GumCommands.Self.GuiCommands.ShowControl(control);

            LoadCodeSettingsFile();

            RefreshCodeDisplay();

        }

        private void HandleAddAndRemoveVariablesForType(string type, StateSave stateSave)
        {
            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "bool", Value = false, Name = "IsXamarinFormsControl"});

        }

        private void RefreshCodeDisplay()
        {
            var instance = SelectedState.Self.SelectedInstance;
            var selectedElement = SelectedState.Self.SelectedElement;

            control.CodeOutputProjectSettings = codeOutputProjectSettings;
            if(control.CodeOutputElementSettings == null)
            {
                control.CodeOutputElementSettings = new Models.CodeOutputElementSettings();
            }

            var settings = control.CodeOutputElementSettings;

            switch(viewModel.WhatToView)
            {
                case ViewModels.WhatToView.SelectedElement:

                    if (instance != null)
                    {
                        string gumCode = CodeGenerator.GetCodeForInstance(instance, selectedElement, VisualApi.Gum);
                        string xamarinFormsCode = CodeGenerator.GetCodeForInstance(instance, selectedElement, VisualApi.XamarinForms);
                        viewModel.Code = $"//Gum Code:\n{gumCode}\n\n//Xamarin Forms Code:\n{xamarinFormsCode}";
                    }
                    else if(selectedElement != null)
                    {
                        string gumCode = CodeGenerator.GetCodeForElement(selectedElement, settings, codeOutputProjectSettings);
                        viewModel.Code = $"//Code for {selectedElement.ToString()}\n{gumCode}";
                    }
                    break;
                case ViewModels.WhatToView.SelectedState:
                    var state = SelectedState.Self.SelectedStateSave;

                    if (state != null && selectedElement != null)
                    {
                        string gumCode = CodeGenerator.GetCodeForState(selectedElement, state, VisualApi.Gum);
                        viewModel.Code = $"//State Code for {state.Name ?? "Default"}:\n{gumCode}";
                    }
                    break;
            }


        }

        private void CreateControl()
        {
            control = new Views.CodeWindow();
            viewModel = new ViewModels.CodeWindowViewModel();

            control.CodeOutputSettingsPropertyChanged += (not, used) => HandleCodeOutputPropertyChanged();
            control.GenerateCodeClicked += (not, used) => HandleGenerateCodeButtonClicked();
            viewModel.PropertyChanged += (not, used) => RefreshCodeDisplay();

            control.DataContext = viewModel;

            GumCommands.Self.GuiCommands.AddControl(control, "Code", TabLocation.Right);

        }

        private void HandleCodeOutputPropertyChanged()
        {
            var element = SelectedState.Self.SelectedElement;
            if(element != null)
            {
                CodeOutputElementSettingsManager.WriteSettingsForElement(element, control.CodeOutputElementSettings);
                CodeOutputProjectSettingsManager.WriteSettingsForProject(codeOutputProjectSettings);

                RefreshCodeDisplay();
            }
        }

        private void HandleGenerateCodeButtonClicked()
        {
            var settings = control.CodeOutputElementSettings;
            if (string.IsNullOrEmpty(settings.GeneratedFileName))
            {
                GumCommands.Self.GuiCommands.ShowMessage("Generated file name must be set first");
            }
            else
            {
                // We used to use the view model code, but the viewmodel may have
                // an instance within the element selected. Instead, we want to output
                // the code for the whole selected element.
                //var contents = ViewModel.Code;
                var selectedElement = SelectedState.Self.SelectedElement;

                string contents = CodeGenerator.GetCodeForElement(selectedElement, settings, codeOutputProjectSettings);
                contents = $"//Code for {selectedElement.ToString()}\n{contents}";
                var fileName = settings.GeneratedFileName;
                if (FileManager.IsRelative(fileName))
                {
                    fileName = ProjectState.Self.ProjectDirectory + fileName;
                }

                if(System.IO.File.Exists(fileName))
                {

                    System.IO.File.WriteAllText(fileName, contents);

                    // show a message somewhere?
                    GumCommands.Self.GuiCommands.ShowMessage($"Generated code to {FileManager.RemovePath(fileName)}");
                }
                else
                {
                    GumCommands.Self.GuiCommands.ShowMessage($"Could not find destination file on disk");

                }
            }
        }

    }
}
