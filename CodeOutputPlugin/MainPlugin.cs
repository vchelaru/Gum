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

            if (control == null)
            {
                CreateControl();
            }
        }

        private void AssignEvents()
        {
            this.InstanceSelected += HandleInstanceSelected;
            this.InstanceDelete += HandleInstanceDeleted;
            this.InstanceAdd += HandleInstanceAdd;
            this.InstanceReordered += HandleInstanceReordered;

            this.ElementSelected += HandleElementSelected;

            this.VariableSet += HandleVariableSet;
            
            this.StateWindowTreeNodeSelected += HandleStateSelected;
            this.StateRename += HandleStateRename;
            this.StateAdd += HandleStateAdd;
            this.StateDelete += HandleStateDelete;

            this.CategoryRename += (category,newName) => HandleRefreshAndExport();
            this.CategoryAdd += (category) => HandleRefreshAndExport();
            this.CategoryDelete += (category) => HandleRefreshAndExport();
            this.VariableRemovedFromCategory += (name, category) => HandleRefreshAndExport();

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
                LoadCodeSettingsFile();

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

        private void HandleVariableSet(ElementSave element, InstanceSave instance, string arg3, object arg4) => HandleRefreshAndExport();

        private void HandleStateRename(StateSave arg1, string arg2) => HandleRefreshAndExport();
        private void HandleStateAdd(StateSave obj) => HandleRefreshAndExport();
        private void HandleStateDelete(StateSave obj) => HandleRefreshAndExport();

        private void HandleInstanceDeleted(ElementSave arg1, InstanceSave arg2) => HandleRefreshAndExport();
        private void HandleInstanceAdd(ElementSave arg1, InstanceSave arg2) => HandleRefreshAndExport();
        private void HandleInstanceReordered(InstanceSave obj) => HandleRefreshAndExport();


        private void HandleRefreshAndExport()
        {
            if (control != null)
            {
                RefreshCodeDisplay();

                if (control.CodeOutputElementSettings == null)
                {
                    control.CodeOutputElementSettings = new Models.CodeOutputElementSettings();
                }

                var elementSettings = control.CodeOutputElementSettings;

                if (elementSettings.AutoGenerateOnChange)
                {
                    GenerateCodeForSelectedElement(showPopups: false);
                }
            }
        }

        private void HandleViewCodeClicked(object sender, EventArgs e)
        {
            //GumCommands.Self.GuiCommands.ShowControl(control);

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

            // We don't actually want it to show, just associate, so add and immediately remove.
            // Eventually we want this to be done with a single call but I don't know if there's Gum
            // support for it yet
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
            GenerateCodeForSelectedElement(showPopups:true);
        }

        private void GenerateCodeForSelectedElement(bool showPopups)
        {
            var settings = control.CodeOutputElementSettings;
            if (string.IsNullOrEmpty(settings.GeneratedFileName))
            {
                if(showPopups)
                {
                    GumCommands.Self.GuiCommands.ShowMessage("Generated file name must be set first");
                }
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

                string message;
                if (System.IO.File.Exists(fileName))
                {

                    System.IO.File.WriteAllText(fileName, contents);

                    // show a message somewhere?
                    message = $"Generated code to {FileManager.RemovePath(fileName)}";
                }
                else
                {
                    message = $"Could not find destination file on disk";
                }
                if (showPopups)
                {
                    GumCommands.Self.GuiCommands.ShowMessage(message);
                }
                else
                {
                    GumCommands.Self.GuiCommands.PrintOutput(message); 
                }
            }
        }
    }
}
