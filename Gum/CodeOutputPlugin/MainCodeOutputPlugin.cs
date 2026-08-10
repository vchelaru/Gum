using CodeOutputPlugin.Manager;
using Gum.ProjectServices.CodeGeneration;
using CodeOutputPlugin.Views;
using Gum;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;
using Gum.Reflection;
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
using Gum.Services.Dialogs;
using ToolsUtilities;
using CommunityToolkit.Mvvm.Messaging;
using Gum.Messages;
using Gum.Localization;
using Gum.Extensions;
using Gum.Gui.Windows;
using System.Windows.Controls;

namespace CodeOutputPlugin;

[Export(typeof(PluginBase))]
public class MainCodeOutputPlugin : WpfPluginBase
{
    #region Fields/Properties

    public override string FriendlyName => "Code Output Plugin";

    public override Version Version => new Version(1, 0);

    Views.CodeWindow? control;
    ViewModels.CodeWindowViewModel viewModel;
    CodeOutputProjectSettings codeOutputProjectSettings;

    private CodeGenerationFileLocationsService _codeGenerationFileLocationsService;
    private CodeGenerationService _codeGenerationService;
    private CodeOutputElementSettingsManager _elementSettingsManager;
    private readonly ISelectedState _selectedState;
    private RenameService _renameService;
    private readonly IMessenger _messenger;
    private readonly LocalizationService _localizationService;
    private readonly INameVerifier _nameVerifier;
    private readonly CodeGenerator _codeGenerator;
    private readonly ParentSetLogic _parentSetLogic;
    private readonly IFileCommands _fileCommands;
    private CodeOutputProjectSettingsManager _codeOutputProjectSettingsManager;
    private readonly CodeFileDeleteService _codeFileDeleteService;

    // Lives only between DeleteOptionsWindowShow and DeleteConfirmed; null when the deleted
    // elements had no user-authored custom code, in which case nothing was asked.
    private CheckBox? _deleteCustomCodeCheckBox;

    private readonly IProjectState _projectState;
    private readonly IProjectDirectoryProvider _projectDirectoryProvider;
    private readonly CodeGenerationNameVerifier _codeGenerationNameVerifier;

    IPluginTab pluginTab = default!;

    // Built in CreateControl (called from StartUp), not the ctor: it needs the just-constructed
    // control/pluginTab instances. Null until then - every call site below goes through the
    // null-conditional thin wrappers, mirroring the old "control != null" guards.
    private CodeOutputTabController? _controller;

    // Not sure why this is null..., so getting it from the builder instead
    //[Import("LocalizationService")]
    //public LocalizationService LocalizationService
    //{
    //    get;
    //    set;
    //}

    #endregion

    #region Init/Startup

    [ImportingConstructor]
    public MainCodeOutputPlugin(
        IGuiCommands guiCommands,
        IDialogService dialogService,
        INameVerifier nameVerifier,
        LocalizationService localizationService,
        IProjectState projectState,
        ITypeManager typeManager,
        IOutputManager outputManager,
        ISelectedState selectedState,
        IRetryService retryService,
        IMessenger messenger,
        IFileCommands fileCommands)
    {
        codeOutputProjectSettings = new CodeOutputProjectSettings();

        _nameVerifier = nameVerifier;
        _localizationService = localizationService;

        _projectState = projectState;
        _projectDirectoryProvider = new ProjectStateDirectoryProvider(_projectState);

        _codeGenerationNameVerifier = new CodeGenerationNameVerifier(_nameVerifier);
        _elementSettingsManager = new CodeOutputElementSettingsManager(_projectDirectoryProvider);
        var typeStringResolver = new ToolTypeStringResolver(typeManager);

        var codeGenLoggerForDetection = new ToolCodeGenLogger(outputManager);
        var syntaxVersionDetectionService = new SyntaxVersionDetectionService(codeGenLoggerForDetection);

        _codeGenerator = new CodeGenerator(_codeGenerationNameVerifier, _localizationService, _elementSettingsManager, _projectDirectoryProvider, typeStringResolver, syntaxVersionDetectionService);

        _codeGenerationFileLocationsService = new CodeGenerationFileLocationsService(_codeGenerator, _codeGenerationNameVerifier, _projectDirectoryProvider);

        _selectedState = selectedState;

        var customCodeGenerator = new CustomCodeGenerator(_codeGenerator, _codeGenerationNameVerifier);
        _codeGenerationService = new CodeGenerationService(guiCommands, _codeGenerator, dialogService, customCodeGenerator, _codeGenerationNameVerifier, _projectDirectoryProvider, retryService);
        _renameService = new RenameService(
            _codeGenerationService,
            _codeGenerator,
            customCodeGenerator,
            _codeGenerationNameVerifier,
            dialogService,
            _projectDirectoryProvider,
            fileCommands);

        _messenger = messenger;
        _fileCommands = fileCommands;

        var codeGenLogger = new ToolCodeGenLogger(outputManager);
        _codeOutputProjectSettingsManager = new CodeOutputProjectSettingsManager(
            codeGenLogger, _projectDirectoryProvider);

        _parentSetLogic = new ParentSetLogic(_codeGenerator, _selectedState, dialogService, _fileCommands);

        _codeFileDeleteService = new CodeFileDeleteService(
            _codeGenerationFileLocationsService,
            _elementSettingsManager,
            _codeGenerator,
            new CustomCodeStubDetector(),
            _fileCommands,
            dialogService);

        _messenger.Register<RequestCodeGenerationMessage>(
            this,
            (_, message) => HandleRequestCodeGeneration(message));

        viewModel = new ViewModels.CodeWindowViewModel(
            projectState,
            fileCommands,
            dialogService,
            guiCommands,
            new CodeGenerationAutoSetupService());
    }

    private void HandleRequestCodeGeneration(RequestCodeGenerationMessage message)
    {
        HandleGenerateAllCodeButtonClicked(showPopups:false);

        message.TaskCompletionSource.SetResult(true);
    }

    public override void StartUp()
    {
        // CreateControl builds _controller, which several of AssignEvents' handlers rely on -
        // must run first.
        CreateControl();

        AssignEvents();
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
        this.DeleteOptionsWindowShow += HandleDeleteOptionsWindowShow;
        this.DeleteConfirmed += HandleDeleteConfirmed;
        this.BehaviorReferencesChanged += HandleBehaviorReferencesChanged;

        this.VariableAdd += HandleVariableAdd;
        this.VariableSet += HandleVariableSet;
        this.VariableDelete += HandleVariableDelete;
        this.VariableExcluded += HandleVariableExcluded;
        this.AddAndRemoveVariablesForType += CustomVariableManager.HandleAddAndRemoveVariablesForType;

        this.AfterUndo += () => HandleRefreshAndExport();

        this.ReactToStateSaveSelected += HandleStateSelected;
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

    /// <summary>
    /// Removes the deleted element's derived files (.Generated.cs and .codsj) with no prompt. The
    /// user-authored custom .cs is handled per-batch by the DeleteOptionsWindow checkbox below.
    /// </summary>
    private void HandleElementDeleted(ElementSave element) =>
        _codeFileDeleteService.ReconcileFilesForDeletedElement(element, codeOutputProjectSettings);

    /// <summary>
    /// Materializes the code-file option into a real WPF checkbox on the delete dialog. One
    /// checkbox covers the whole batch, so deleting a folder's worth of elements asks once.
    /// </summary>
    private void HandleDeleteOptionsWindowShow(DeleteOptionsWindow deleteWindow, Array objectsToDelete)
    {
        // A cancelled delete never fires DeleteConfirmed, so clear the previous dialog's checkbox
        // here - otherwise a later delete that adds no checkbox would read the stale checked state.
        _deleteCustomCodeCheckBox = null;

        var checkboxViewModel = _codeFileDeleteService.HandleDeleteOptionsWindowShow(
            objectsToDelete, codeOutputProjectSettings);

        if (checkboxViewModel != null)
        {
            _deleteCustomCodeCheckBox = checkboxViewModel.ToCheckBox();
            deleteWindow.MainStackPanel.Children.Add(_deleteCustomCodeCheckBox);
        }
    }

    /// <summary>
    /// Reads back the checkbox added by <see cref="HandleDeleteOptionsWindowShow"/> (if any) and
    /// removes it from the window afterward, mirroring the other DeleteOptionsWindow contributors.
    /// </summary>
    private void HandleDeleteConfirmed(DeleteOptionsWindow deleteOptionsWindow, Array deletedObjects)
    {
        bool isChecked = _deleteCustomCodeCheckBox?.IsChecked == true;

        _codeFileDeleteService.HandleConfirmDelete(deletedObjects, isChecked, codeOutputProjectSettings);

        if (_deleteCustomCodeCheckBox != null)
        {
            if (deleteOptionsWindow.MainStackPanel.Children.Contains(_deleteCustomCodeCheckBox))
            {
                deleteOptionsWindow.MainStackPanel.Children.Remove(_deleteCustomCodeCheckBox);
            }
            _deleteCustomCodeCheckBox = null;
        }
    }

    private bool HandleVariableExcluded(VariableSave variable, RecursiveVariableFinder rvf) => VariableExclusionLogic.GetIfVariableIsExcluded(variable, rvf);

    private void HandleProjectLoaded(GumProjectSave project)
    {
        // Services resolve ProjectDirectory lazily via IProjectDirectoryProvider,
        // so no reconstruction is needed here — just reload project-scoped settings.
        codeOutputProjectSettings = _codeOutputProjectSettingsManager.CreateOrLoadSettingsForProject();
        viewModel.InheritanceLocation = codeOutputProjectSettings.InheritanceLocation;
        HandleElementSelected(null);
    }

    private void HandleStateSelected(StateSave? state)
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

    private void LoadCodeSettingsFile(ElementSave? element) => _controller?.LoadCodeSettingsFile(element);


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

    private void HandleInstanceDeleted(ElementSave element, InstanceSave instance) =>
        HandleRefreshAndExportForElement(element);

    private void HandleInstanceAdd(ElementSave element, InstanceSave instance)
    {
        _parentSetLogic.HandleNewCreatedInstance(element, instance, codeOutputProjectSettings);

        HandleRefreshAndExportForElement(element);
    }
    private void HandleInstanceReordered(InstanceSave obj) => HandleRefreshAndExport();


    private void HandleRefreshAndExport() => _controller?.HandleRefreshAndExport(codeOutputProjectSettings);

    // Refresh + auto-regenerate for an explicit owning element rather than the live
    // SelectedElement. Used by instance add/delete events because the affected instance
    // may live in an element that is not currently selected (e.g. delete fired from a
    // tree-view selection that doesn't match what the codegen tab is viewing), in
    // which case regenerating for SelectedElement writes the wrong file or NREs.
    private void HandleRefreshAndExportForElement(ElementSave element) =>
        _controller?.HandleRefreshAndExportForElement(element, codeOutputProjectSettings);

    private void RefreshCodeDisplay() => _controller?.RefreshCodeDisplay(codeOutputProjectSettings);

    private void CreateControl()
    {
        control = new Views.CodeWindow(viewModel);

        control.CodeOutputSettingsPropertyChanged += (_, _) => HandleCodeOutputPropertyChanged();
        control.GenerateCodeClicked += (_, _) => HandleGenerateCodeButtonClicked();
        control.GenerateAllCodeClicked += (_, _) => HandleGenerateAllCodeButtonClicked();
        viewModel.PropertyChanged += (sender, args) => HandleMainViewModelPropertyChanged(args.PropertyName);

        control.DataContext = viewModel;

        pluginTab = _tabManager.AddControl(control, "Code", TabLocation.RightBottom);

        _controller = new CodeOutputTabController(
            control,
            pluginTab,
            (ITabSelectionState)pluginTab,
            _selectedState,
            _projectState,
            _codeGenerator,
            _codeGenerationService,
            _elementSettingsManager,
            _codeOutputProjectSettingsManager,
            viewModel);

        pluginTab.GotFocus += () => RefreshCodeDisplay();
    }

    private void HandleMainViewModelPropertyChanged(string? propertyName)
    {
        /////////////////Early Out////////////////////
        if(_projectState.GumProjectSave == null)
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
                _codeOutputProjectSettingsManager.WriteSettingsForProject(codeOutputProjectSettings);
                break;
            default:
                RefreshCodeDisplay();
                break;
        }
    }

    private void HandleCodeOutputPropertyChanged() => _controller?.HandleCodeOutputPropertyChanged(codeOutputProjectSettings);

    private void HandleGenerateCodeButtonClicked()
    {
        if(string.IsNullOrEmpty(codeOutputProjectSettings.CodeProjectRoot))
        {
            var message = "To save generated code, you must specify a Code Project Root.";

            var csprojAboveGumx = viewModel.GetCsprojDirectoryAboveGumx();
            if(csprojAboveGumx == null)
            {
                message += "\n\n" +
                    "Note: Specify a Code Project Root above (Project-Wide Code Generation section). " +
                    "This can be any folder - it does not need to contain a .csproj.";
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
                    foreach(var element in _projectState.GumProjectSave.AllElements)
                    {
                        if(element is StandardElementSave)
                        {
                            continue;
                        }

                        var elementOutputSettings = _elementSettingsManager.LoadOrCreateSettingsFor(element);
                        if(elementOutputSettings.GenerationBehavior != GenerationBehavior.NeverGenerate)
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
        var gumProject = _projectState.GumProjectSave;
        foreach (var screen in gumProject.Screens)
        {
            var screenOutputSettings = _elementSettingsManager.LoadOrCreateSettingsFor(screen);
            _codeGenerationService.GenerateCodeForElement(
                screen, screenOutputSettings, codeOutputProjectSettings, showPopups: false);
        }
        foreach(var component in gumProject.Components)
        {
            var componentOutputSettings = _elementSettingsManager.LoadOrCreateSettingsFor(component);
            _codeGenerationService.GenerateCodeForElement(
                component, componentOutputSettings, codeOutputProjectSettings, showPopups: false);
        }

        _codeGenerationService.GenerateStandardElementsFallbackFile(gumProject, codeOutputProjectSettings);

        if(showPopups)
        {
            _dialogService.ShowMessage($"Generated code\nScreens: {gumProject.Screens.Count}\nComponents: {gumProject.Components.Count}");
        }
    }

    private void GenerateCodeForElement(bool showPopups, ElementSave? element, CodeOutputElementSettings? settings = null) =>
        _controller?.GenerateCodeForElement(showPopups, element, codeOutputProjectSettings, settings);

    public override bool ShutDown(PluginShutDownReason shutDownReason) => true;
}
