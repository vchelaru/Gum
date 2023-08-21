using CodeOutputPlugin.Manager;
using CodeOutputPlugin.Models;
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
            this.ElementRename += (element, oldName) => RenameManager.HandleRename(element, oldName, codeOutputProjectSettings);
            this.ElementAdd += HandleElementAdd;
            this.ElementDelete += HandleElementDeleted;

            this.VariableAdd += HandleVariableAdd;
            this.VariableSet += HandleVariableSet;
            this.VariableDelete += HandleVariableDelete;
            this.VariableExcluded += HandleVariableExcluded;
            this.AddAndRemoveVariablesForType += CustomVariableManager.HandleAddAndRemoveVariablesForType;

            this.AfterUndo += () => HandleRefreshAndExport();

            this.StateWindowTreeNodeSelected += HandleStateSelected;
            this.StateRename += HandleStateRename;
            this.StateAdd += HandleStateAdd;
            this.StateDelete += HandleStateDelete;

            this.CategoryRename += (category,newName) => HandleRefreshAndExport();
            this.CategoryAdd += (category) => HandleRefreshAndExport();
            this.CategoryDelete += (category) => HandleRefreshAndExport();
            this.VariableRemovedFromCategory += (name, category) => HandleRefreshAndExport();

            this.ProjectLoad += HandleProjectLoaded;
        }

        private void HandleElementDeleted(ElementSave element)
        {
            var elementSettings = CodeOutputElementSettingsManager.LoadOrCreateSettingsFor(element);

            // If it's deleted, ask the user if they also want to delete generated code files
            var generatedFile = CodeGenerator.GetGeneratedFileName(element, elementSettings, codeOutputProjectSettings);
            var customCodeFile = CodeGenerator.GetCustomCodeFileName(element, elementSettings, codeOutputProjectSettings);

            if(generatedFile?.Exists() == true || customCodeFile?.Exists() == true)
            {
                var message = $"Would you like to delete the generated and custom code files for {element}?";

                var result = System.Windows.MessageBox.Show(message, "Delete Code?", System.Windows.MessageBoxButton.YesNo);

                if(result == System.Windows.MessageBoxResult.Yes)
                {
                    if(generatedFile?.Exists() == true)
                    {
                        System.IO.File.Delete(generatedFile.FullPath);
                    }

                    if(customCodeFile?.Exists() == true)
                    {
                        System.IO.File.Delete(customCodeFile.FullPath);
                    }
                }
            }
        }

        private bool HandleVariableExcluded(VariableSave variable, RecursiveVariableFinder rvf) => VariableExclusionLogic.GetIfVariableIsExcluded(variable, rvf);

        private void HandleProjectLoaded(GumProjectSave project)
        {
            codeOutputProjectSettings = CodeOutputProjectSettingsManager.CreateOrLoadSettingsForProject();
            viewModel.IsCodeGenPluginEnabled = codeOutputProjectSettings.IsCodeGenPluginEnabled;
            viewModel.IsShowCodegenPreviewChecked = codeOutputProjectSettings.IsShowCodegenPreviewChecked;
            CustomVariableManager.ViewModel = viewModel;
            HandleElementSelected(null);
        }

        private void HandleStateSelected(TreeNode obj)
        {
            if (control != null)
            {
                LoadCodeSettingsFile(GumState.Self.SelectedState.SelectedElement);

                RefreshCodeDisplay();
            }
        }

        private void HandleInstanceSelected(ElementSave arg1, InstanceSave instance)
        {
            if(control != null)
            {
                LoadCodeSettingsFile(GumState.Self.SelectedState.SelectedElement);

                RefreshCodeDisplay();
            }
        }

        private void HandleElementSelected(ElementSave element)
        {
            if (control != null)
            {
                LoadCodeSettingsFile(element);

                RefreshCodeDisplay();
            }
        }


        private void HandleElementAdd(ElementSave element)
        {
            HandleRefreshAndExport();
            GenerateCodeForElement(showPopups: false, element);
        }

        private void LoadCodeSettingsFile(ElementSave element)
        {
            if(element != null)
            {
                control.CodeOutputElementSettings = CodeOutputElementSettingsManager.LoadOrCreateSettingsFor(element);
            }
            else
            {
                control.CodeOutputElementSettings = new Models.CodeOutputElementSettings();
            }
        }

        private void HandleVariableSet(ElementSave element, InstanceSave instance, string variableName, object oldValue)
        {
            ParentSetLogic.HandleVariableSet(element, instance, variableName, oldValue, codeOutputProjectSettings);

            RenameManager.HandleVariableSet(element, instance, variableName, oldValue, codeOutputProjectSettings);

            HandleRefreshAndExport();
        }
        private void HandleVariableAdd(ElementSave elementSave, string variableName) => HandleRefreshAndExport();
        //private void /*/*HandleVariableRemoved*/*/(ElementSave elementSave, string variableName) => HandleRefreshAndExport();
        private void HandleVariableDelete(ElementSave arg1, string arg2) => HandleRefreshAndExport();

        private void HandleStateRename(StateSave arg1, string arg2) => HandleRefreshAndExport();
        private void HandleStateAdd(StateSave obj) => HandleRefreshAndExport();
        private void HandleStateDelete(StateSave obj) => HandleRefreshAndExport();

        private void HandleInstanceDeleted(ElementSave arg1, InstanceSave arg2) => HandleRefreshAndExport();
        private void HandleInstanceAdd(ElementSave element, InstanceSave instance)
        {
            ParentSetLogic.HandleNewCreatedInstance(element, instance, codeOutputProjectSettings);

            HandleRefreshAndExport();
        }
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

            LoadCodeSettingsFile(GumState.Self.SelectedState.SelectedElement);

            RefreshCodeDisplay();

        }


        private void RefreshCodeDisplay()
        {
            control.CodeOutputProjectSettings = codeOutputProjectSettings;
            if(control.CodeOutputElementSettings == null)
            {
                control.CodeOutputElementSettings = new Models.CodeOutputElementSettings();
            }
            ///////////////////////early out////////////////////
            if(!viewModel.IsShowCodegenPreviewChecked)
            {
                return;
            }
            /////////////////////end early out/////////////////

            var instance = SelectedState.Self.SelectedInstance;
            var selectedElement = SelectedState.Self.SelectedElement;


            var settings = control.CodeOutputElementSettings;

            if(settings.GenerationBehavior != Models.GenerationBehavior.NeverGenerate)
            {
                switch(viewModel.WhatToView)
                {
                    case ViewModels.WhatToView.SelectedElement:

                        if (instance != null)
                        {
                            string code = CodeGenerator.GetCodeForInstance(instance, selectedElement, codeOutputProjectSettings );
                            viewModel.Code = code;
                        }
                        else if(selectedElement != null)
                        {

                            string gumCode = CodeGenerator.GetGeneratedCodeForElement(selectedElement, settings, codeOutputProjectSettings);
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
            else if(selectedElement == null)
            {
                viewModel.Code = "// Select a Screen, Component, or Standard to see generated code";
            }
            else
            {
                viewModel.Code = "// code generation disabled for this object";
            }


        }

        private void CreateControl()
        {
            control = new Views.CodeWindow();
            viewModel = new ViewModels.CodeWindowViewModel();

            control.CodeOutputSettingsPropertyChanged += (not, used) => HandleCodeOutputPropertyChanged();
            control.GenerateCodeClicked += (not, used) => HandleGenerateCodeButtonClicked();
            control.GenerateAllCodeClicked += (not, used) => HandleGenerateAllCodeButtonClicked();
            viewModel.PropertyChanged += (sender, args) => HandleMainViewModelPropertyChanged(args.PropertyName);

            control.DataContext = viewModel;

            // We don't actually want it to show, just associate, so add and immediately remove.
            // Eventually we want this to be done with a single call but I don't know if there's Gum
            // support for it yet
            GumCommands.Self.GuiCommands.AddControl(control, "Code", TabLocation.RightBottom);
        }

        private void HandleMainViewModelPropertyChanged(string propertyName)
        {
            /////////////////Early Out////////////////////
            if(GumState.Self.ProjectState.GumProjectSave == null)
            {
                return;
            }
            /////////////End Early Out////////////////////
            
            switch(propertyName)
            {
                case nameof(viewModel.IsCodeGenPluginEnabled):
                    codeOutputProjectSettings.IsCodeGenPluginEnabled = viewModel.IsCodeGenPluginEnabled;
                    CodeOutputProjectSettingsManager.WriteSettingsForProject(codeOutputProjectSettings);
                    break;
                case nameof(viewModel.IsShowCodegenPreviewChecked):
                    codeOutputProjectSettings.IsShowCodegenPreviewChecked = viewModel.IsShowCodegenPreviewChecked;
                    CodeOutputProjectSettingsManager.WriteSettingsForProject(codeOutputProjectSettings);
                    break;
                default:
                    RefreshCodeDisplay();
                    break;
            }
        }

        private void HandleCodeOutputPropertyChanged()
        {
            var element = SelectedState.Self.SelectedElement;
            if(element != null)
            {
                CodeOutputElementSettingsManager.WriteSettingsForElement(element, control.CodeOutputElementSettings);

                RefreshCodeDisplay();
            }
            CodeOutputProjectSettingsManager.WriteSettingsForProject(codeOutputProjectSettings);
        }

        private void HandleGenerateCodeButtonClicked()
        {
            if(SelectedState.Self.SelectedElement != null)
            {
                GenerateCodeForSelectedElement(showPopups:true);
            }
        }

        private void HandleGenerateAllCodeButtonClicked()
        {
            var gumProject = GumState.Self.ProjectState.GumProjectSave;
            foreach (var screen in gumProject.Screens)
            {
                var screenOutputSettings = CodeOutputElementSettingsManager.LoadOrCreateSettingsFor(screen);
                CodeGenerator.GenerateCodeForElement(screen, screenOutputSettings, codeOutputProjectSettings, showPopups: false);
            }
            foreach(var component in gumProject.Components)
            {
                var componentOutputSettings = CodeOutputElementSettingsManager.LoadOrCreateSettingsFor(component);
                CodeGenerator.GenerateCodeForElement(component, componentOutputSettings, codeOutputProjectSettings, showPopups: false);
            }

            GumCommands.Self.GuiCommands.ShowMessage($"Generated code\nScreens: {gumProject.Screens.Count}\nComponents: {gumProject.Components.Count}");
        }

        private void GenerateCodeForSelectedElement(bool showPopups)
        {
            var selectedElement = SelectedState.Self.SelectedElement;
            GenerateCodeForElement(showPopups, selectedElement);
        }

        private void GenerateCodeForElement(bool showPopups, ElementSave element)
        {
            var settings = control.CodeOutputElementSettings;
            if (element != null)
            {
                CodeGenerator.GenerateCodeForElement(element, settings, codeOutputProjectSettings, showPopups);
            }
        }
    }
}
