using CodeOutputPlugin.Manager;
using CodeOutputPlugin.Models;
using CodeOutputPlugin.Views;
using Gum;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;
using Gum.Services;
using Gum.ToolStates;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Gum.Services.Dialogs;
using ToolsUtilities;
using CommunityToolkit.Mvvm.Messaging;
using Gum.Messages;
using Gum.Localization;

namespace CodeOutputPlugin;

[Export(typeof(PluginBase))]
public class MainCodeOutputPlugin : PluginBase
{
    #region Fields/Properties

    public override string FriendlyName => "Code Output Plugin";

    public override Version Version => new Version(1, 0);

    Views.CodeWindow? control;
    ViewModels.CodeWindowViewModel viewModel;
    Models.CodeOutputProjectSettings codeOutputProjectSettings;

    private readonly CodeGenerationFileLocationsService _codeGenerationFileLocationsService;
    private readonly CodeGenerationService _codeGenerationService;
    private readonly ISelectedState _selectedState;
    private readonly RenameService _renameService;
    private readonly IMessenger _messenger;
    private readonly LocalizationService _localizationService;
    private readonly INameVerifier _nameVerifier;
    private readonly CodeGenerator _codeGenerator;
    private readonly ParentSetLogic _parentSetLogic;

    PluginTab pluginTab = default!;

    // Not sure why this is null..., so getting it from the builder instead
    //[Import("LocalizationService")]
    //public LocalizationService LocalizationService
    //{
    //    get;
    //    set;
    //}

    #endregion

    #region Init/Startup

    public MainCodeOutputPlugin()
    {
        codeOutputProjectSettings = new CodeOutputProjectSettings();

        _nameVerifier = Locator.GetRequiredService<INameVerifier>();
        _localizationService = Locator.GetRequiredService<LocalizationService>();

        CodeGenerationNameVerifier codeGenerationNameVerifier = new(_nameVerifier);

        _codeGenerator = new CodeGenerator(codeGenerationNameVerifier, _localizationService);

        _codeGenerationFileLocationsService = new CodeGenerationFileLocationsService(_codeGenerator, codeGenerationNameVerifier);

        _selectedState = Locator.GetRequiredService<ISelectedState>();

        var customCodeGenerator = new CustomCodeGenerator(_codeGenerator, codeGenerationNameVerifier);
        _codeGenerationService = new CodeGenerationService(_guiCommands, _codeGenerator, _dialogService, customCodeGenerator, codeGenerationNameVerifier);
        _renameService = new RenameService(
            _codeGenerationService, 
            _codeGenerator, 
            customCodeGenerator, 
            codeGenerationNameVerifier, 
            _dialogService);

        _messenger = Locator.GetRequiredService<IMessenger>();

        _parentSetLogic = new ParentSetLogic(_codeGenerator);

        _messenger.Register<RequestCodeGenerationMessage>(
            this, 
            (_, message) => HandleRequestCodeGeneration(message));

        viewModel = new ViewModels.CodeWindowViewModel();
    }

    private void HandleRequestCodeGeneration(RequestCodeGenerationMessage message)
    {
        HandleGenerateAllCodeButtonClicked(showPopups:false);

        message.TaskCompletionSource.SetResult(true);
    }

    public override void StartUp()
    {
        AssignEvents();

        CreateControl();
    }

    private void AssignEvents()
    {
        this.InstanceSelected += HandleInstanceSelected;
        this.InstanceDelete += HandleInstanceDeleted;
        this.InstanceAdd += HandleInstanceAdd;
        this.InstanceReordered += HandleInstanceReordered;

        this.ElementSelected += HandleElementSelected;
        this.ElementRename += (element, oldName) => _renameService.HandleRename(element, oldName, codeOutputProjectSettings, _codeGenerator.GetVisualApiForElement(element));
        this.ElementAdd += HandleElementAdd;
        this.ElementDelete += HandleElementDeleted;
        this.BehaviorReferencesChanged += HandleBehaviorReferencesChanged;

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


    #endregion

    private void HandleElementDeleted(ElementSave element)
    {
        var elementSettings = CodeOutputElementSettingsManager.LoadOrCreateSettingsFor(element);

        var visualApi = _codeGenerator.GetVisualApiForElement(element);

        // If it's deleted, ask the user if they also want to delete generated code files
        var generatedFile = _codeGenerationFileLocationsService.GetGeneratedFileName(element, elementSettings, codeOutputProjectSettings, visualApi);
        var customCodeFile = _codeGenerationFileLocationsService.GetCustomCodeFileName(element, elementSettings, codeOutputProjectSettings, visualApi: visualApi);

        if(generatedFile?.Exists() == true || customCodeFile?.Exists() == true)
        {
            var message = $"Would you like to delete the generated and custom code files for {element}?";

            if(_dialogService.ShowYesNoMessage(message, "Delete Code?"))
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
        viewModel.InheritanceLocation = codeOutputProjectSettings.InheritanceLocation;
        HandleElementSelected(null);
    }

    private void HandleStateSelected(TreeNode obj)
    {
        if (control != null && _selectedState.SelectedElement != null)
        {
            LoadCodeSettingsFile(_selectedState.SelectedElement);

            RefreshCodeDisplay();
        }
    }

    private void HandleInstanceSelected(ElementSave arg1, InstanceSave instance)
    {
        if(control != null && _selectedState.SelectedElement != null)
        {
            LoadCodeSettingsFile(_selectedState.SelectedElement);

            RefreshCodeDisplay();
        }
    }

    private void HandleElementSelected(ElementSave? element)
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

    private void LoadCodeSettingsFile(ElementSave? element)
    {
        ////////////////////////Early Out/////////////////////
        if (control == null) return;
        ///////////////////////End Early Out//////////////////

        if (element != null && GumState.Self.ProjectState.GumProjectSave?.FullFileName != null)
        {
            control.CodeOutputElementSettings = CodeOutputElementSettingsManager.LoadOrCreateSettingsFor(element);
        }
        else
        {
            control.CodeOutputElementSettings = new Models.CodeOutputElementSettings();
        }
    }


    private void HandleBehaviorReferencesChanged(ElementSave save)
    {
        HandleRefreshAndExport();
    }

    private void HandleVariableSet(ElementSave element, InstanceSave? instance, string variableName, object? oldValue)
    {
        _parentSetLogic.HandleVariableSet(element, instance, variableName, oldValue, codeOutputProjectSettings);

        _renameService.HandleVariableSet(element, instance, variableName, oldValue, codeOutputProjectSettings);

        HandleRefreshAndExport();
    }
    private void HandleVariableAdd(ElementSave elementSave, string variableName)
    {
        if(control != null)
        {
            RefreshCodeDisplay();
        }
        HandleRefreshAndExport();
    }
    //private void /*/*HandleVariableRemoved*/*/(ElementSave elementSave, string variableName) => HandleRefreshAndExport();
    private void HandleVariableDelete(ElementSave arg1, string arg2)
    {
        if (control != null)
        {
            RefreshCodeDisplay();
        }
        HandleRefreshAndExport();
    }

    private void HandleStateRename(StateSave arg1, string arg2) => HandleRefreshAndExport();
    private void HandleStateAdd(StateSave obj) => HandleRefreshAndExport();
    private void HandleStateDelete(StateSave obj) => HandleRefreshAndExport();

    private void HandleInstanceDeleted(ElementSave arg1, InstanceSave arg2) => HandleRefreshAndExport();
    private void HandleInstanceAdd(ElementSave element, InstanceSave instance)
    {
        _parentSetLogic.HandleNewCreatedInstance(element, instance, codeOutputProjectSettings);

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

    private void HandleViewCodeClicked(object? sender, EventArgs e)
    {
        //GumCommands.Self.GuiCommands.ShowControl(control);

        if(_selectedState.SelectedElement != null)
        {
            LoadCodeSettingsFile(_selectedState.SelectedElement);
        }

        RefreshCodeDisplay();

    }


    private void RefreshCodeDisplay()
    {
        var shouldShow = _selectedState.SelectedElement != null &&
            _selectedState.SelectedElement is not StandardElementSave;

        if(shouldShow)
        {
            pluginTab.Show();
        }
        else
        {
            pluginTab.Hide();
        }

        ///////////////////////early out////////////////////
        if(!pluginTab.IsSelected)
        {
            return;
        }

        if(control == null)
        {
            return;
        }

        /////////////////////end early out/////////////////

        control.CodeOutputProjectSettings = codeOutputProjectSettings;
        if(control.CodeOutputElementSettings == null)
        {
            control.CodeOutputElementSettings = new Models.CodeOutputElementSettings();
        }

        var instance = _selectedState.SelectedInstance;
        var selectedElement = _selectedState.SelectedElement!;

        viewModel.IsViewingStandardElement = selectedElement is StandardElementSave;

        var settings = control.CodeOutputElementSettings;

        if(settings.GenerationBehavior != Models.GenerationBehavior.NeverGenerate)
        {
            ObjectFinder.Self.EnableCache();
            try
            {
                switch(viewModel.WhatToView)
                {
                    case ViewModels.WhatToView.SelectedElement:

                        if (instance != null)
                        {
                            string code = _codeGenerator.GetCodeForInstance(instance, selectedElement, codeOutputProjectSettings );
                            viewModel.Code = code;
                        }
                        else if(selectedElement != null && selectedElement is not StandardElementSave)
                        {

                            string gumCode = _codeGenerator.GetGeneratedCodeForElement(selectedElement, settings, codeOutputProjectSettings);
                            viewModel.Code = $"//Code for {selectedElement.ToString()}\r\n{gumCode}";
                        }
                        break;
                    case ViewModels.WhatToView.SelectedState:
                        var state = _selectedState.SelectedStateSave;

                        if (state != null && selectedElement != null)
                        {
                            string gumCode = _codeGenerator.GetCodeForState(selectedElement, state, codeOutputProjectSettings);
                            viewModel.Code = $"//State Code for {state.Name ?? "Default"}:\r\n{gumCode}";
                        }
                        break;
                }
            }
            finally
            {
                ObjectFinder.Self.DisableCache();
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
        control = new Views.CodeWindow(viewModel);

        control.CodeOutputSettingsPropertyChanged += (not, used) => HandleCodeOutputPropertyChanged();
        control.GenerateCodeClicked += (not, used) => HandleGenerateCodeButtonClicked();
        control.GenerateAllCodeClicked += (not, used) => HandleGenerateAllCodeButtonClicked();
        viewModel.PropertyChanged += (sender, args) => HandleMainViewModelPropertyChanged(args.PropertyName);

        control.DataContext = viewModel;

        pluginTab = _tabManager.AddControl(control, "Code", TabLocation.RightBottom);
        pluginTab.GotFocus += () => RefreshCodeDisplay();
    }

    private void HandleMainViewModelPropertyChanged(string? propertyName)
    {
        /////////////////Early Out////////////////////
        if(GumState.Self.ProjectState.GumProjectSave == null)
        {
            return;
        }
        /////////////End Early Out////////////////////
        
        switch(propertyName)
        {
            case nameof(viewModel.WhichElementsToGenerate):
            case nameof(viewModel.IsSelectedOnlyGenerating):
                // do nothing
                break;
            case nameof(viewModel.InheritanceLocation):
                codeOutputProjectSettings.InheritanceLocation = viewModel.InheritanceLocation;
                CodeOutputProjectSettingsManager.WriteSettingsForProject(codeOutputProjectSettings);
                break;
            default:
                RefreshCodeDisplay();
                break;
        }
    }

    private void HandleCodeOutputPropertyChanged()
    {
        var element = _selectedState.SelectedElement;
        if(element != null && control?.CodeOutputElementSettings != null)
        {
            CodeOutputElementSettingsManager.WriteSettingsForElement(element, control.CodeOutputElementSettings);

            RefreshCodeDisplay();
        }
        CodeOutputProjectSettingsManager.WriteSettingsForProject(codeOutputProjectSettings);
    }

    private void HandleGenerateCodeButtonClicked()
    {
        if(string.IsNullOrEmpty(codeOutputProjectSettings.CodeProjectRoot))
        {
            var message = "To save generated code, you must specify a .csproj location.";

            var csprojAboveGumx = viewModel.GetCsprojDirectoryAboveGumx();
            if(csprojAboveGumx == null)
            {
                message += "\n\n" +
                    "Note: Your Gum project (.gumx) is currently not saved relative to a folder that contains a .csproj file. " +
                    "Saving your Gum project relative to your .csproj is the recommended approach";
            }

            _dialogService.ShowMessage(message);
        }
        else if (_selectedState.SelectedElement != null)
        {
            ObjectFinder.Self.EnableCache();
            try
            {
                if(viewModel.IsAllInProjectGenerating)
                {
                    int numberOfElements = 0;
                    foreach(var element in GumState.Self.ProjectState.GumProjectSave.AllElements)
                    {
                        if(element is StandardElementSave)
                        {
                            continue;
                        }

                        var elementOutputSettings = CodeOutputElementSettingsManager.LoadOrCreateSettingsFor(element);
                        if(elementOutputSettings.GenerationBehavior != Models.GenerationBehavior.NeverGenerate)
                        {
                            _codeGenerationService.GenerateCodeForElement(element, elementOutputSettings, codeOutputProjectSettings, showPopups: false);
                            numberOfElements++;
                        }
                    }

                    _dialogService.ShowMessage($"Generated code for {numberOfElements} element(s)");
                }
                else
                {
                    GenerateCodeForElement(showPopups:true, _selectedState.SelectedElement);
                }
            }
            finally
            {
                ObjectFinder.Self.DisableCache();
            }
        }
    }

    private void HandleGenerateAllCodeButtonClicked(bool showPopups = true)
    {
        var gumProject = GumState.Self.ProjectState.GumProjectSave;
        foreach (var screen in gumProject.Screens)
        {
            var screenOutputSettings = CodeOutputElementSettingsManager.LoadOrCreateSettingsFor(screen);
            _codeGenerationService.GenerateCodeForElement(
                screen, screenOutputSettings, codeOutputProjectSettings, showPopups: false);
        }
        foreach(var component in gumProject.Components)
        {
            var componentOutputSettings = CodeOutputElementSettingsManager.LoadOrCreateSettingsFor(component);
            _codeGenerationService.GenerateCodeForElement(
                component, componentOutputSettings, codeOutputProjectSettings, showPopups: false);
        }

        if(showPopups)
        {
            _dialogService.ShowMessage($"Generated code\nScreens: {gumProject.Screens.Count}\nComponents: {gumProject.Components.Count}");
        }
    }

    private void GenerateCodeForSelectedElement(bool showPopups)
    {
        var selectedElement = _selectedState.SelectedElement;
        GenerateCodeForElement(showPopups, selectedElement);
    }

    private void GenerateCodeForElement(bool showPopups, ElementSave? element, CodeOutputElementSettings? settings = null)
    {
        if (element != null && element is not StandardElementSave)
        {
            settings = settings ?? control?.CodeOutputElementSettings;

            // If user is using automatic generation, generate everything
            // If it's manual, don't check for missing files

            if(settings != null)
            {
                var checkForMissing = settings.GenerationBehavior == GenerationBehavior.GenerateAutomaticallyOnPropertyChange;
                _codeGenerationService.GenerateCodeForElement(element, settings, codeOutputProjectSettings, showPopups, checkForMissing: checkForMissing);
            }
        }
    }

    public override bool ShutDown(PluginShutDownReason shutDownReason) => true;
}
