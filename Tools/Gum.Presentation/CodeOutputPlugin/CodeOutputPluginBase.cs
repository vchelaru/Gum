using CodeOutputPlugin.Manager;
using Gum.ProjectServices.CodeGeneration;
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
using Gum.SettingsWindow;
using ToolsUtilities;
using CommunityToolkit.Mvvm.Messaging;
using Gum.Messages;
using Gum.Localization;
using Gum.Extensions;

namespace CodeOutputPlugin;

/// <summary>
/// The Code Output plugin: the Code tab (a preview of the generated code and the code generation
/// settings) and code generation in response to project, element, instance, state, and variable
/// events. Each head exports a subclass named <c>MainCodeOutputPlugin</c> that supplies the tab's
/// view through <see cref="CreateTabHost"/>. The delete dialog's "delete custom code" option is
/// handled here for the neutral dialog, and by the WPF subclass for that head's own window.
/// </summary>
public abstract class CodeOutputPluginBase : PluginBase
{
    #region Fields/Properties

    public override string FriendlyName => "Code Output Plugin";

    public override Version Version => new Version(1, 0);

    ICodeOutputTabHost? control;
    private readonly CodeOutputSettingsMembers _settingsMembers;
    ViewModels.CodeWindowViewModel viewModel;
    CodeOutputProjectSettings codeOutputProjectSettings;

    private CodeGenerationFileLocationsService _codeGenerationFileLocationsService;
    private CodeGenerationService _codeGenerationService;
    private CodeOutputElementSettingsManager _elementSettingsManager;
    private readonly ISelectedState _selectedState;
    private RenameService _renameService;
    private readonly IMessenger _messenger;
    private readonly CodeFileLocationWatcher _locationWatcher;
    private readonly LocalizationService _localizationService;
    private readonly INameVerifier _nameVerifier;
    private readonly CodeGenerator _codeGenerator;
    private readonly ParentSetLogic _parentSetLogic;
    private readonly IFileCommands _codeOutputFileCommands;
    private CodeOutputProjectSettingsManager _codeOutputProjectSettingsManager;
    private readonly CodeFileDeleteService _codeFileDeleteService;
    private readonly NewElementCodeSettingsService _newElementCodeSettingsService;
    private readonly IDispatcher _dispatcher;

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

    protected CodeOutputPluginBase(
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
        IFileCommands fileCommands,
        IDispatcher dispatcher)
    {
        codeOutputProjectSettings = new CodeOutputProjectSettings();
        _dispatcher = dispatcher;

        _nameVerifier = nameVerifier;
        _localizationService = localizationService;

        _projectState = projectState;
        _projectDirectoryProvider = new ProjectStateDirectoryProvider(_projectState);

        _codeGenerationNameVerifier = new CodeGenerationNameVerifier(_nameVerifier);
        _elementSettingsManager = new CodeOutputElementSettingsManager(_projectDirectoryProvider);
        var typeStringResolver = new ToolTypeStringResolver(typeManager);

        var codeGenLoggerForDetection = new ToolCodeGenLogger(outputManager);
        var syntaxVersionDetectionService = new SyntaxVersionDetectionService(codeGenLoggerForDetection);

        _codeGenerator = new CodeGenerator(_codeGenerationNameVerifier, _localizationService, _elementSettingsManager, _projectDirectoryProvider, typeStringResolver, syntaxVersionDetectionService, new CSharpVersionDetectionService());

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
        _codeOutputFileCommands = fileCommands;
        _locationWatcher = new CodeFileLocationWatcher(messenger, new CodeFileLocationChange(), new CustomCodeHeaderChange());

        var codeGenLogger = new ToolCodeGenLogger(outputManager);
        _codeOutputProjectSettingsManager = new CodeOutputProjectSettingsManager(
            codeGenLogger, _projectDirectoryProvider);

        _parentSetLogic = new ParentSetLogic(_codeGenerator, _selectedState, dialogService, _codeOutputFileCommands);

        _codeFileDeleteService = new CodeFileDeleteService(
            _codeGenerationFileLocationsService,
            _elementSettingsManager,
            _codeGenerator,
            new CustomCodeStubDetector(),
            _codeOutputFileCommands,
            dialogService);

        _newElementCodeSettingsService = new NewElementCodeSettingsService(
            _elementSettingsManager, _codeOutputFileCommands, outputManager);

        _messenger.Register<RequestCodeGenerationMessage>(
            this,
            (_, message) => HandleRequestCodeGeneration(message));

        // The neutral delete dialog (the Avalonia head's): offer to delete hand-edited custom code
        // and act on the answer. The WPF head raises its own window's events instead, which
        // MainCodeOutputPlugin there handles the same way.
        DeleteOptionsShow += HandleDeleteOptionsShow;
        DeleteOptionsConfirmed += HandleDeleteOptionsConfirmed;
        // The project's code settings do not depend on the tab, so they load from here on.
        ProjectLoad += HandleProjectLoaded;
        ReactToFileChanged += HandleFileChanged;

        viewModel = new ViewModels.CodeWindowViewModel(
            projectState,
            fileCommands,
            dialogService,
            guiCommands,
            new CodeGenerationAutoSetupService());

        _settingsMembers = new CodeOutputSettingsMembers(projectState, syntaxVersionDetectionService, viewModel);
        viewModel.ResetProjectSettingsRequested += HandleResetProjectSettingsRequested;
    }

    private DeleteOptionCheckboxViewModel? _deleteCustomCodeOption;

    // One option covers the whole batch, so deleting a folder's worth of elements asks once. A
    // cancelled delete never confirms, so the previous dialog's option is dropped here.
    private void HandleDeleteOptionsShow(DeleteOptionsDialogViewModel dialog, Array objectsToDelete)
    {
        _deleteCustomCodeOption = _codeFileDeleteService.HandleDeleteOptionsWindowShow(objectsToDelete, codeOutputProjectSettings);
        if (_deleteCustomCodeOption != null)
        {
            dialog.CheckBoxes.Add(_deleteCustomCodeOption);
        }
    }

    private void HandleDeleteOptionsConfirmed(DeleteOptionsDialogViewModel dialog, Array deletedObjects)
    {
        bool deleteEditedCustomCode = _deleteCustomCodeOption?.IsChecked == true;
        _deleteCustomCodeOption = null;
        _codeFileDeleteService.HandleConfirmDelete(deletedObjects, deleteEditedCustomCode, codeOutputProjectSettings);
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
        AddProjectSettingsPage();
    }

    // The project-wide settings are a page of the Settings window; the Code tab links to it.
    private void AddProjectSettingsPage()
    {
        CodeGenerationSettingsViewModel page = new CodeGenerationSettingsViewModel(_settingsMembers);

        AddSettingsPage(new SettingsPage(
            SettingsPageIds.CodeGeneration, "Code generation", SettingsSection.Project, page, order: 4)
        {
            Activated = () =>
            {
                _settingsMembers.ProjectSettings = codeOutputProjectSettings;
                page.Reload();
            },
            Keywords = new[] { "namespace", "usings" },
            SearchTerms = () => page.BuildCategories().SelectMany(category => category.Members).Select(member => member.Name),
        });
    }

    private void AssignEvents()
    {
        this.InstanceSelected += HandleInstanceSelected;
        this.InstanceDelete += HandleInstanceDeleted;
        this.InstanceAdd += HandleInstanceAdd;
        this.InstanceReordered += HandleInstanceReordered;

        this.ElementSelected += HandleElementSelected;
        this.ElementRename += (element, oldName) => _renameService.HandleRename(element, oldName, codeOutputProjectSettings, _codeGenerator.GetVisualApiForElement(element),
            moveCodeFiles: !IsProjectSettingsReadOnly);
        this.ElementAdd += HandleElementAdd;
        this.ElementDuplicate += _newElementCodeSettingsService.HandleElementDuplicate;
        this.ElementDelete += HandleElementDeleted;
        this.BehaviorReferencesChanged += HandleBehaviorReferencesChanged;

        this.VariableAdd += HandleVariableAdd;
        this.VariableSet += HandleVariableSet;
        this.VariableSetThroughReference += HandleVariableSetThroughReference;
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

    }


    #endregion

    /// <summary>
    /// Removes the deleted element's derived files (.Generated.cs and .codsj) with no prompt. The
    /// user-authored custom .cs is handled by the head's delete-dialog option, where it has one.
    /// </summary>
    private void HandleElementDeleted(ElementSave element) =>
        _codeFileDeleteService.ReconcileFilesForDeletedElement(element, codeOutputProjectSettings);

    private bool HandleVariableExcluded(VariableSave variable, RecursiveVariableFinder rvf) => VariableExclusionLogic.GetIfVariableIsExcluded(variable, rvf);

    private void HandleProjectLoaded(GumProjectSave project)
    {
        // Services resolve ProjectDirectory lazily via IProjectDirectoryProvider,
        // so no reconstruction is needed here — just reload project-scoped settings.
        // A file that does not parse is the user's settings: the defaults stand in for display
        // only, and nothing is saved until the file is fixed (it reloads) or reset.
        CodeOutputProjectSettings? loaded = _codeOutputProjectSettingsManager.TryLoadSettingsForProject(out string? loadError);
        codeOutputProjectSettings = loaded ?? CodeOutputProjectSettingsManager.CreateDefaultSettings();
        viewModel.ProjectSettingsLoadError = loadError;
        _locationWatcher.Reset(codeOutputProjectSettings);
        _settingsMembers.HandleProjectLoaded();
        _settingsMembers.ProjectSettings = codeOutputProjectSettings;
        viewModel.InheritanceLocation = codeOutputProjectSettings.InheritanceLocation;
        HandleElementSelected(null);
    }

    /// <summary>
    /// Whether ProjectCodeSettings.codsj exists but could not be read. No settings are saved then,
    /// since that would write over the user's file, and no code is generated or moved.
    /// </summary>
    private bool IsProjectSettingsReadOnly => viewModel.HasProjectSettingsLoadError;

    /// <summary>
    /// Re-reads ProjectCodeSettings.codsj after it changes on disk (a pull, a branch switch, a hand
    /// edit). This only reloads: the change arrives with whatever regeneration it needed already
    /// done, so nothing is regenerated, migrated or written here. An unreadable file (half-written,
    /// merge conflict) keeps the current settings and makes the Code tab read-only until a later
    /// change makes it readable again, so no settings edit writes over it.
    /// </summary>
    private void HandleFileChanged(FilePath file)
    {
        ///////////////////Early Out///////////////////
        if (file != _codeOutputProjectSettingsManager.GetProjectCodeSettingsFilePath())
        {
            return;
        }

        CodeOutputProjectSettings? reloaded = _codeOutputProjectSettingsManager.TryLoadSettingsForProject(out string? loadError);
        if (loadError != null)
        {
            SetProjectSettingsLoadError(loadError);
            return;
        }
        if (reloaded == null)
        {
            return;
        }

        // Gum's own settings writes come back through the watcher too; skipping an unchanged file
        // keeps them from rebuilding the settings grid under the user.
        if (!IsProjectSettingsReadOnly && JsonConvert.SerializeObject(reloaded) == JsonConvert.SerializeObject(codeOutputProjectSettings))
        {
            return;
        }
        /////////////////End Early Out/////////////////

        ApplyProjectSettings(reloaded);
    }

    private void HandleResetProjectSettingsRequested()
    {
        const string question = "Replace ProjectCodeSettings.codsj with the default code generation settings? " +
            "Its current contents will be lost.";
        if (_dialogService.ShowMessage(question, "Reset code generation settings", MessageDialogStyle.YesNo) != MessageDialogResult.Affirmative)
        {
            return;
        }

        CodeOutputProjectSettings defaults = CodeOutputProjectSettingsManager.CreateDefaultSettings();
        _codeOutputProjectSettingsManager.WriteSettingsForProject(defaults);
        ApplyProjectSettings(defaults);
    }

    /// <summary>Makes <paramref name="settings"/>, just read from or written to disk, the project's settings.</summary>
    private void ApplyProjectSettings(CodeOutputProjectSettings settings)
    {
        codeOutputProjectSettings = settings;
        // The files on disk came with these settings, so a later edit compares against them.
        _locationWatcher.Reset(codeOutputProjectSettings);
        _settingsMembers.ProjectSettings = codeOutputProjectSettings;
        viewModel.InheritanceLocation = codeOutputProjectSettings.InheritanceLocation;
        viewModel.ProjectSettingsLoadError = null;
        _settingsMembers.RequestRebuild();
        RefreshCodeDisplay();
    }

    private void SetProjectSettingsLoadError(string? loadError)
    {
        viewModel.ProjectSettingsLoadError = loadError;
        // The rows are built read-only while the file is unreadable.
        _settingsMembers.RequestRebuild();
    }

    private void HandleStateSelected(StateSave? state)
    {
        if (control != null && _selectedState.SelectedElement != null)
        {
            LoadCodeSettingsFile(_selectedState.SelectedElement);

            RefreshCodeDisplay();
        }
    }

    private void HandleInstanceSelected(ElementSave? arg1, InstanceSave? instance)
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
        // Before anything reads the new element's settings, so a leftover .codsj is never applied.
        _newElementCodeSettingsService.HandleElementAdd(element);

        HandleRefreshAndExport();

        // The new element's own settings, not the tab's: the tab still shows whichever element was
        // selected before, and its Generated File Name would send this element's code into that file.
        // A duplicate's settings were already copied by ElementDuplicate, which fires before the add.
        CodeOutputElementSettings settings = _elementSettingsManager.LoadOrCreateSettingsFor(element);
        if (settings.GenerationBehavior == GenerationBehavior.GenerateAutomaticallyOnPropertyChange)
        {
            GenerateCodeForElement(showPopups: false, element, settings);
        }
    }

    private void LoadCodeSettingsFile(ElementSave? element) => _controller?.LoadCodeSettingsFile(element);


    private void HandleBehaviorReferencesChanged(ElementSave save)
    {
        HandleRefreshAndExport();
    }

    private void HandleVariableSet(ElementSave? element, InstanceSave? instance, string variableName, object? oldValue,
        bool isFullCommit)
    {
        _parentSetLogic.HandleVariableSet(element, instance, variableName, oldValue, codeOutputProjectSettings);

        if (!IsProjectSettingsReadOnly)
        {
            _renameService.HandleVariableSet(element, instance, variableName, oldValue, codeOutputProjectSettings);
        }

        HandleRefreshAndExport(isFullCommit);
    }

    private void HandleVariableSetThroughReference(ElementSave? element, InstanceSave? instance, string variableName,
        object? oldValue, bool isFullCommit)
    {
        _parentSetLogic.HandleVariableSet(element, instance, variableName, oldValue, codeOutputProjectSettings);

        if (!IsProjectSettingsReadOnly)
        {
            _renameService.HandleVariableSet(element, instance, variableName, oldValue, codeOutputProjectSettings);
        }

        if (IsProjectSettingsReadOnly)
        {
            RefreshCodeDisplay();
            return;
        }
        _controller?.HandleVariableSetThroughReference(element, codeOutputProjectSettings, isFullCommit);
    }
    private void HandleVariableAdd(ElementSave? elementSave, string variableName)
    {
        if(control != null)
        {
            RefreshCodeDisplay();
        }
        HandleRefreshAndExport();
    }
    //private void /*/*HandleVariableRemoved*/*/(ElementSave elementSave, string variableName) => HandleRefreshAndExport();
    private void HandleVariableDelete(ElementSave? arg1, string arg2)
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

    private void HandleInstanceDeleted(ElementSave? element, InstanceSave instance) =>
        HandleRefreshAndExportForElement(element);

    private void HandleInstanceAdd(ElementSave element, InstanceSave instance)
    {
        _parentSetLogic.HandleNewCreatedInstance(element, instance, codeOutputProjectSettings);

        HandleRefreshAndExportForElement(element);
    }
    private void HandleInstanceReordered(InstanceSave obj) => HandleRefreshAndExport();


    // While the project's code settings can't be read, edits only refresh the preview: generating
    // or moving code would use settings that may no longer match the file.
    private void HandleRefreshAndExport(bool isFullCommit = true)
    {
        if (IsProjectSettingsReadOnly)
        {
            RefreshCodeDisplay();
            return;
        }
        _controller?.HandleRefreshAndExport(codeOutputProjectSettings, isFullCommit);
    }

    // Refresh + auto-regenerate for an explicit owning element rather than the live
    // SelectedElement. Used by instance add/delete events because the affected instance
    // may live in an element that is not currently selected (e.g. delete fired from a
    // tree-view selection that doesn't match what the codegen tab is viewing), in
    // which case regenerating for SelectedElement writes the wrong file or NREs.
    private void HandleRefreshAndExportForElement(ElementSave? element)
    {
        if (IsProjectSettingsReadOnly)
        {
            RefreshCodeDisplay();
            return;
        }
        _controller?.HandleRefreshAndExportForElement(element, codeOutputProjectSettings);
    }

    private void RefreshCodeDisplay() => _controller?.RefreshCodeDisplay(codeOutputProjectSettings);

    private void CreateControl()
    {
        control = CreateTabHost(viewModel, _settingsMembers);

        control.CodeOutputSettingsPropertyChanged += (_, _) => HandleCodeOutputPropertyChanged();
        control.GenerateCodeClicked += (_, _) => HandleGenerateCodeButtonClicked();
        control.GenerateAllCodeClicked += (_, _) => HandleGenerateAllCodeButtonClicked();
        control.EditProjectSettingsClicked += (_, _) => ShowSettingsWindow(SettingsPageIds.CodeGeneration);
        viewModel.PropertyChanged += (sender, args) => HandleMainViewModelPropertyChanged(args.PropertyName);

        pluginTab = _tabManager.AddControl(control.Control, "Code", TabLocation.RightBottom);

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
            viewModel,
            _dispatcher);

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
                if (IsProjectSettingsReadOnly)
                {
                    break;
                }
                codeOutputProjectSettings.InheritanceLocation = viewModel.InheritanceLocation;
                _codeOutputProjectSettingsManager.WriteSettingsForProject(codeOutputProjectSettings);
                _locationWatcher.CheckAfterEdit(codeOutputProjectSettings);
                break;
            default:
                RefreshCodeDisplay();
                break;
        }
    }

    private void HandleCodeOutputPropertyChanged()
    {
        ///////////////////Early Out///////////////////
        if (IsProjectSettingsReadOnly)
        {
            return;
        }
        /////////////////End Early Out/////////////////
        _controller?.HandleCodeOutputPropertyChanged(codeOutputProjectSettings);
        // After the write, so the migration that may follow plans against the saved settings.
        _locationWatcher.CheckAfterEdit(codeOutputProjectSettings);
    }

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
                    GumProjectSave? gumProject = _projectState.GumProjectSave;
                    int numberOfElements = gumProject == null
                        ? 0
                        : _codeGenerationService.GenerateCodeForAllElements(gumProject, codeOutputProjectSettings);

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
        GumProjectSave? gumProject = _projectState.GumProjectSave;
        if (gumProject == null || IsProjectSettingsReadOnly)
        {
            // Nothing is loaded, or the settings to generate with can't be read; the caller still
            // completes.
            return;
        }
        _codeGenerationService.GenerateCodeForAllElements(gumProject, codeOutputProjectSettings);

        if(showPopups)
        {
            _dialogService.ShowMessage($"Generated code\nScreens: {gumProject.Screens.Count}\nComponents: {gumProject.Components.Count}");
        }
    }

    private void GenerateCodeForElement(bool showPopups, ElementSave? element, CodeOutputElementSettings? settings = null)
    {
        if (!IsProjectSettingsReadOnly)
        {
            _controller?.GenerateCodeForElement(showPopups, element, codeOutputProjectSettings, settings);
        }
    }

    public override bool ShutDown(PluginShutDownReason shutDownReason) => true;

    /// <summary>Builds this head's Code tab view over the shared view model and settings members.</summary>
    protected abstract ICodeOutputTabHost CreateTabHost(ViewModels.CodeWindowViewModel viewModel, CodeOutputSettingsMembers settingsMembers);

    /// <summary>Reconciles and deletes elements' code files; a head's delete-dialog option calls into it.</summary>
    protected CodeFileDeleteService CodeFileDeleteService => _codeFileDeleteService;

    /// <summary>The loaded project's code generation settings.</summary>
    protected CodeOutputProjectSettings ProjectSettings => codeOutputProjectSettings;
}
